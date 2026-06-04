using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class OldProjectConstructionLodImporter : EditorWindow
{
    private const string DefaultDestinationRoot = "Assets/Graphic/OldProject";
    private const string DefaultImportedMaterialsFolder = "Assets/Graphic/OldProject/Materials";
    private const string MenuPath = "TownCrafter/Import Old Project Construction LODs";
    private const string SharedPrefabFolderName = "_SharedPrefabGroups";
    private const string SourceFolderPrefsKey = "TownCrafter.OldProjectConstructionLodImporter.SourceFolder";
    private const string DestinationRootPrefsKey = "TownCrafter.OldProjectConstructionLodImporter.DestinationRoot";
    private const string OverwriteExistingPrefsKey = "TownCrafter.OldProjectConstructionLodImporter.OverwriteExisting";

    private static readonly Regex GuidRegex = new Regex(@"^guid: ([0-9a-f]{32})\s*$", RegexOptions.Multiline);
    private static readonly Regex NameRegex = new Regex(@"^\s*m_Name:\s*(.+?)\s*$", RegexOptions.Multiline);
    private static readonly Regex LodGuidRegex = new Regex(@"guid:\s*(?<guid>[0-9a-f]{32})");
    private static readonly Regex MaterialReferenceRegex = new Regex(@"(?<prefix>\{fileID:\s*2100000,\s*guid:\s*)(?<guid>[0-9a-f]{32})(?<suffix>,\s*type:\s*2\})");

    [SerializeField] private string _sourceFolder = "";
    [SerializeField] private string _destinationRoot = DefaultDestinationRoot;
    [SerializeField] private bool _overwriteExisting = false;

    private Vector2 _scroll;
    private string _lastResult = "";

    [MenuItem(MenuPath)]
    public static void Open()
    {
        GetWindow<OldProjectConstructionLodImporter>("Old Construction LODs");
    }

    private void OnEnable()
    {
        _sourceFolder = EditorPrefs.GetString(SourceFolderPrefsKey, _sourceFolder);
        _destinationRoot = EditorPrefs.GetString(DestinationRootPrefsKey, DefaultDestinationRoot);
        _overwriteExisting = EditorPrefs.GetBool(OverwriteExistingPrefsKey, _overwriteExisting);
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Old Project Construction LOD Importer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select a folder from another Unity project that contains construction .asset files. " +
            "The importer reads each asset's lodGroups and frontFences, copies those referenced prefab folders into this project, " +
            "repairs unresolved material references by name, and creates a parent prefab containing LOD and front-fence prefab instances.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            _sourceFolder = EditorGUILayout.TextField("Source Folder", _sourceFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Old Construction Folder", _sourceFolder, "");
                if (!string.IsNullOrEmpty(selected))
                    _sourceFolder = selected;
            }
        }

        _destinationRoot = EditorGUILayout.TextField("Destination Root", _destinationRoot);
        _overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing construction folders", _overwriteExisting);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!CanImport()))
        {
            if (GUILayout.Button("Import Construction LOD Prefabs"))
                Import();
        }

        if (!string.IsNullOrEmpty(_lastResult))
        {
            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_lastResult, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        if (EditorGUI.EndChangeCheck())
            SavePrefs();
    }

    private bool CanImport()
    {
        return Directory.Exists(_sourceFolder) &&
               !string.IsNullOrWhiteSpace(_destinationRoot) &&
               _destinationRoot.StartsWith("Assets/", StringComparison.Ordinal);
    }

    private void Import()
    {
        SavePrefs();

        var sourceFolder = Path.GetFullPath(_sourceFolder);
        var sourceAssetsRoot = FindAssetsRoot(sourceFolder);
        if (string.IsNullOrEmpty(sourceAssetsRoot))
        {
            _lastResult = $"Could not find an Assets folder above:\n{sourceFolder}";
            return;
        }

        var constructionAssets = FindConstructionAssets(sourceFolder);
        if (constructionAssets.Count == 0)
        {
            _lastResult = $"No construction .asset files with lodGroups or frontFences found under:\n{sourceFolder}";
            return;
        }

        try
        {
            EnsureAssetFolder(_destinationRoot);
            var guidMap = BuildGuidMap(sourceAssetsRoot);
            var referencedGuidUseCounts = CountReferencedGuidUses(constructionAssets);
            var copiedPrefabPathsByGuid = new Dictionary<string, string>();
            var materialResolver = new MaterialReferenceResolver(guidMap);
            var log = new List<string>();

            foreach (var constructionAssetPath in constructionAssets)
                ImportConstruction(constructionAssetPath, guidMap, referencedGuidUseCounts, copiedPrefabPathsByGuid, materialResolver, log);

            _lastResult = string.Join("\n", log);
        }
        catch (Exception exception)
        {
            _lastResult = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            AssetDatabase.Refresh();
        }
    }

    private void ImportConstruction(
        string constructionAssetPath,
        IReadOnlyDictionary<string, string> guidMap,
        IReadOnlyDictionary<string, int> referencedGuidUseCounts,
        IDictionary<string, string> copiedPrefabPathsByGuid,
        MaterialReferenceResolver materialResolver,
        List<string> log)
    {
        var constructionName = GetConstructionName(constructionAssetPath);
        var lodGuids = ReadGuidList(constructionAssetPath, "lodGroups");
        var frontFenceGuids = ReadGuidList(constructionAssetPath, "frontFences");

        if (lodGuids.Count == 0)
        {
            log.Add($"Skipped {constructionName}: no lodGroups.");
            return;
        }

        var constructionFolder = AssetPathCombine(_destinationRoot, SanitizeFileName(constructionName));
        if (AssetDatabase.IsValidFolder(constructionFolder))
        {
            if (!_overwriteExisting)
            {
                log.Add($"Skipped {constructionName}: destination exists.");
                return;
            }

            FileUtil.DeleteFileOrDirectory(constructionFolder);
            FileUtil.DeleteFileOrDirectory($"{constructionFolder}.meta");
        }

        EnsureAssetFolder(constructionFolder);

        var copiedLodPrefabPaths = new List<string>();
        for (int i = 0; i < lodGuids.Count; i++)
        {
            if (TryCopyReferencedPrefab(
                    lodGuids[i],
                    $"LOD {i}",
                    constructionName,
                    constructionFolder,
                    guidMap,
                    referencedGuidUseCounts,
                    copiedPrefabPathsByGuid,
                    materialResolver,
                    log,
                    out var copiedPrefabPath))
                copiedLodPrefabPaths.Add(copiedPrefabPath);
        }

        var copiedFrontFencePrefabPaths = new List<string>();
        for (int i = 0; i < frontFenceGuids.Count; i++)
        {
            if (TryCopyReferencedPrefab(
                    frontFenceGuids[i],
                    $"front fence {i}",
                    constructionName,
                    constructionFolder,
                    guidMap,
                    referencedGuidUseCounts,
                    copiedPrefabPathsByGuid,
                    materialResolver,
                    log,
                    out var copiedPrefabPath))
                copiedFrontFencePrefabPaths.Add(copiedPrefabPath);
        }

        AssetDatabase.ImportAsset(constructionFolder, ImportAssetOptions.ImportRecursive);
        CreateParentPrefab(constructionName, constructionFolder, copiedLodPrefabPaths, copiedFrontFencePrefabPaths, log);
    }

    private bool TryCopyReferencedPrefab(
        string guid,
        string label,
        string constructionName,
        string constructionFolder,
        IReadOnlyDictionary<string, string> guidMap,
        IReadOnlyDictionary<string, int> referencedGuidUseCounts,
        IDictionary<string, string> copiedPrefabPathsByGuid,
        MaterialReferenceResolver materialResolver,
        List<string> log,
        out string copiedPrefabPath)
    {
        copiedPrefabPath = "";

        if (copiedPrefabPathsByGuid.TryGetValue(guid, out var alreadyCopiedPath))
        {
            copiedPrefabPath = alreadyCopiedPath;
            return true;
        }

        var existingAssetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (!_overwriteExisting && !string.IsNullOrEmpty(existingAssetPath))
        {
            copiedPrefabPathsByGuid[guid] = existingAssetPath;
            copiedPrefabPath = existingAssetPath;
            return true;
        }

        if (!guidMap.TryGetValue(guid, out var oldPrefabPath) || !File.Exists(oldPrefabPath))
        {
            log.Add($"  Missing {label} for {constructionName}: {guid}");
            return false;
        }

        var sourceFolder = Path.GetDirectoryName(oldPrefabPath);
        if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            log.Add($"  Missing {label} folder for {constructionName}: {oldPrefabPath}");
            return false;
        }

        var folderName = SanitizeFileName(Path.GetFileName(sourceFolder));
        if (string.IsNullOrEmpty(folderName))
            folderName = SanitizeFileName(label.Replace(" ", ""));

        var shouldUseSharedFolder = referencedGuidUseCounts.TryGetValue(guid, out var useCount) && useCount > 1;
        var parentFolder = shouldUseSharedFolder
            ? AssetPathCombine(_destinationRoot, SharedPrefabFolderName)
            : constructionFolder;
        EnsureAssetFolder(parentFolder);

        var destinationFolderName = shouldUseSharedFolder
            ? $"{folderName}_{guid.Substring(0, 8)}"
            : folderName;
        var destinationFolder = AssetPathCombine(parentFolder, destinationFolderName);
        if (AssetDatabase.IsValidFolder(destinationFolder) || Directory.Exists(destinationFolder))
        {
            FileUtil.DeleteFileOrDirectory(destinationFolder);
            FileUtil.DeleteFileOrDirectory($"{destinationFolder}.meta");
        }

        FileUtil.CopyFileOrDirectory(sourceFolder, destinationFolder);

        var sourceMetaPath = $"{sourceFolder}.meta";
        if (File.Exists(sourceMetaPath))
            FileUtil.CopyFileOrDirectory(sourceMetaPath, $"{destinationFolder}.meta");

        RepairMaterialReferences(destinationFolder, materialResolver, log);

        copiedPrefabPath = AssetPathCombine(destinationFolder, Path.GetFileName(oldPrefabPath));
        AssetDatabase.ImportAsset(destinationFolder, ImportAssetOptions.ImportRecursive);
        copiedPrefabPathsByGuid[guid] = copiedPrefabPath;
        return true;
    }

    private static void CreateParentPrefab(
        string constructionName,
        string constructionFolder,
        IReadOnlyList<string> lodPrefabPaths,
        IReadOnlyList<string> frontFencePrefabPaths,
        List<string> log)
    {
        var root = new GameObject(constructionName);

        try
        {
            var createdCount = 0;
            createdCount += InstantiatePrefabChildren(root.transform, lodPrefabPaths, "LOD", log);

            var frontFenceCount = 0;
            if (frontFencePrefabPaths.Count > 0)
            {
                var frontFencesRoot = new GameObject("FrontFences");
                frontFencesRoot.transform.SetParent(root.transform, false);
                frontFenceCount = InstantiatePrefabChildren(
                    frontFencesRoot.transform,
                    frontFencePrefabPaths,
                    "FrontFence",
                    log,
                    true,
                    Vector3.back * 10f);
            }

            var prefabPath = AssetPathCombine(constructionFolder, $"{SanitizeFileName(constructionName)}.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            log.Add($"Imported {constructionName}: {createdCount} LOD prefab children, {frontFenceCount} front fence prefab children -> {prefabPath}");
        }
        finally
        {
            DestroyImmediate(root);
        }
    }

    private static int InstantiatePrefabChildren(
        Transform parent,
        IReadOnlyList<string> prefabPaths,
        string label,
        List<string> log)
    {
        return InstantiatePrefabChildren(parent, prefabPaths, label, log, false, Vector3.zero);
    }

    private static int InstantiatePrefabChildren(
        Transform parent,
        IReadOnlyList<string> prefabPaths,
        string label,
        List<string> log,
        bool onlyFirstActive,
        Vector3 localPosition)
    {
        var createdCount = 0;
        for (int i = 0; i < prefabPaths.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            if (prefab == null)
            {
                log.Add($"  Could not load copied {label} prefab: {prefabPaths[i]}");
                continue;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                instance = Instantiate(prefab);

            instance.name = $"{label}{i}_{prefab.name}";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            if (onlyFirstActive)
                instance.SetActive(i == 0);

            createdCount++;
        }

        return createdCount;
    }

    private static List<string> FindConstructionAssets(string sourceFolder)
    {
        var result = new List<string>();
        foreach (var assetPath in Directory.EnumerateFiles(sourceFolder, "*.asset", SearchOption.AllDirectories))
        {
            if (ReadGuidList(assetPath, "lodGroups").Count > 0 || ReadGuidList(assetPath, "frontFences").Count > 0)
                result.Add(assetPath);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static Dictionary<string, string> BuildGuidMap(string assetsRoot)
    {
        var result = new Dictionary<string, string>();
        foreach (var metaPath in Directory.EnumerateFiles(assetsRoot, "*.meta", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(metaPath);
            var match = GuidRegex.Match(text);
            if (!match.Success)
                continue;

            var assetPath = metaPath.Substring(0, metaPath.Length - ".meta".Length);
            result[match.Groups[1].Value] = assetPath;
        }

        return result;
    }

    private static Dictionary<string, int> CountReferencedGuidUses(IEnumerable<string> constructionAssets)
    {
        var result = new Dictionary<string, int>();
        foreach (var constructionAsset in constructionAssets)
        {
            CountGuidUses(ReadGuidList(constructionAsset, "lodGroups"), result);
            CountGuidUses(ReadGuidList(constructionAsset, "frontFences"), result);
        }

        return result;
    }

    private static void CountGuidUses(IEnumerable<string> guids, IDictionary<string, int> result)
    {
        foreach (var guid in guids)
        {
            if (!result.ContainsKey(guid))
                result[guid] = 0;

            result[guid]++;
        }
    }

    private static List<string> ReadGuidList(string constructionAssetPath, string fieldName)
    {
        var result = new List<string>();
        var foundField = false;

        foreach (var line in File.ReadLines(constructionAssetPath))
        {
            var trimmedLine = line.Trim();

            if (!foundField)
            {
                foundField = trimmedLine == $"{fieldName}:";
                continue;
            }

            if (!trimmedLine.StartsWith("-", StringComparison.Ordinal))
                break;

            var guidMatch = LodGuidRegex.Match(trimmedLine);
            if (guidMatch.Success)
                result.Add(guidMatch.Groups["guid"].Value);
        }

        return result;
    }

    private static string GetConstructionName(string constructionAssetPath)
    {
        var text = File.ReadAllText(constructionAssetPath);
        var match = NameRegex.Match(text);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            return match.Groups[1].Value.Trim();

        return Path.GetFileNameWithoutExtension(constructionAssetPath);
    }

    private static string FindAssetsRoot(string path)
    {
        var directory = new DirectoryInfo(path);
        while (directory != null)
        {
            if (string.Equals(directory.Name, "Assets", StringComparison.Ordinal))
                return directory.FullName;

            directory = directory.Parent;
        }

        return "";
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        var parts = assetFolder.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static string AssetPathCombine(string first, string second)
    {
        return $"{first.TrimEnd('/')}/{second.TrimStart('/')}";
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalidCharacter, '_');

        return fileName.Trim();
    }

    private static void RepairMaterialReferences(string assetFolder, MaterialReferenceResolver materialResolver, List<string> log)
    {
        if (!Directory.Exists(assetFolder))
            return;

        foreach (var prefabPath in Directory.EnumerateFiles(assetFolder, "*.prefab", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(prefabPath);
            var changed = false;

            var repairedText = MaterialReferenceRegex.Replace(text, match =>
            {
                var oldGuid = match.Groups["guid"].Value;
                var replacementGuid = materialResolver.Resolve(oldGuid, log);
                if (string.IsNullOrEmpty(replacementGuid) || replacementGuid == oldGuid)
                    return match.Value;

                changed = true;
                return $"{match.Groups["prefix"].Value}{replacementGuid}{match.Groups["suffix"].Value}";
            });

            if (changed)
                File.WriteAllText(prefabPath, repairedText);
        }
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(SourceFolderPrefsKey, _sourceFolder);
        EditorPrefs.SetString(DestinationRootPrefsKey, _destinationRoot);
        EditorPrefs.SetBool(OverwriteExistingPrefsKey, _overwriteExisting);
    }

    private sealed class MaterialReferenceResolver
    {
        private readonly IReadOnlyDictionary<string, string> _oldGuidMap;
        private readonly Dictionary<string, string> _replacementGuidsByOldGuid = new Dictionary<string, string>();

        public MaterialReferenceResolver(IReadOnlyDictionary<string, string> oldGuidMap)
        {
            _oldGuidMap = oldGuidMap;
        }

        public string Resolve(string oldGuid, List<string> log)
        {
            if (_replacementGuidsByOldGuid.TryGetValue(oldGuid, out var cachedGuid))
                return cachedGuid;

            var automaticallyMatchedPath = AssetDatabase.GUIDToAssetPath(oldGuid);
            if (!string.IsNullOrEmpty(automaticallyMatchedPath))
            {
                _replacementGuidsByOldGuid[oldGuid] = oldGuid;
                return oldGuid;
            }

            if (!_oldGuidMap.TryGetValue(oldGuid, out var oldMaterialPath) ||
                !string.Equals(Path.GetExtension(oldMaterialPath), ".mat", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(oldMaterialPath))
            {
                _replacementGuidsByOldGuid[oldGuid] = oldGuid;
                return oldGuid;
            }

            var materialName = Path.GetFileNameWithoutExtension(oldMaterialPath);
            var matchedMaterialPath = FindMaterialByExactName(materialName, DefaultImportedMaterialsFolder) ??
                                      FindMaterialByExactName(materialName, "Assets");

            if (!string.IsNullOrEmpty(matchedMaterialPath))
            {
                var matchedGuid = AssetDatabase.AssetPathToGUID(matchedMaterialPath);
                _replacementGuidsByOldGuid[oldGuid] = matchedGuid;
                log.Add($"  Material matched by name: {materialName} -> {matchedMaterialPath}");
                return matchedGuid;
            }

            var importedMaterialPath = ImportMaterial(oldMaterialPath, materialName);
            if (!string.IsNullOrEmpty(importedMaterialPath))
            {
                var importedGuid = AssetDatabase.AssetPathToGUID(importedMaterialPath);
                _replacementGuidsByOldGuid[oldGuid] = importedGuid;
                log.Add($"  Material imported: {materialName} -> {importedMaterialPath}");
                return importedGuid;
            }

            _replacementGuidsByOldGuid[oldGuid] = oldGuid;
            return oldGuid;
        }

        private static string FindMaterialByExactName(string materialName, string searchFolder)
        {
            if (!AssetDatabase.IsValidFolder(searchFolder))
                return null;

            var guids = AssetDatabase.FindAssets($"{materialName} t:Material", new[] { searchFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && material.name == materialName)
                    return path;
            }

            return null;
        }

        private static string ImportMaterial(string oldMaterialPath, string materialName)
        {
            EnsureAssetFolder(DefaultImportedMaterialsFolder);

            var destinationPath = AssetPathCombine(DefaultImportedMaterialsFolder, $"{SanitizeFileName(materialName)}.mat");
            if (File.Exists(destinationPath))
                destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

            FileUtil.CopyFileOrDirectory(oldMaterialPath, destinationPath);

            var sourceMetaPath = $"{oldMaterialPath}.meta";
            if (File.Exists(sourceMetaPath))
                FileUtil.CopyFileOrDirectory(sourceMetaPath, $"{destinationPath}.meta");

            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
            return destinationPath;
        }
    }
}
