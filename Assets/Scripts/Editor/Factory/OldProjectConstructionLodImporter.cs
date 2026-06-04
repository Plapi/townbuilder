using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class OldProjectConstructionLodImporter : EditorWindow
{
    private const string DefaultDestinationRoot = "Assets/Graphic/OldProject";
    private const string MenuPath = "TownCrafter/Import Old Project Construction LODs";
    private const string SharedLodFolderName = "_SharedLodGroups";
    private const string SourceFolderPrefsKey = "TownCrafter.OldProjectConstructionLodImporter.SourceFolder";
    private const string DestinationRootPrefsKey = "TownCrafter.OldProjectConstructionLodImporter.DestinationRoot";
    private const string OverwriteExistingPrefsKey = "TownCrafter.OldProjectConstructionLodImporter.OverwriteExisting";

    private static readonly Regex GuidRegex = new Regex(@"^guid: ([0-9a-f]{32})\s*$", RegexOptions.Multiline);
    private static readonly Regex NameRegex = new Regex(@"^\s*m_Name:\s*(.+?)\s*$", RegexOptions.Multiline);
    private static readonly Regex LodGuidRegex = new Regex(@"guid:\s*(?<guid>[0-9a-f]{32})");

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
            "The importer reads each asset's lodGroups, copies those referenced prefab folders into this project, " +
            "and creates a parent prefab containing one child prefab instance per LOD group.",
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
            _lastResult = $"No construction .asset files with lodGroups found under:\n{sourceFolder}";
            return;
        }

        try
        {
            EnsureAssetFolder(_destinationRoot);
            var guidMap = BuildGuidMap(sourceAssetsRoot);
            var lodGuidUseCounts = CountLodGuidUses(constructionAssets);
            var copiedLodPrefabPathsByGuid = new Dictionary<string, string>();
            var log = new List<string>();

            foreach (var constructionAssetPath in constructionAssets)
                ImportConstruction(constructionAssetPath, guidMap, lodGuidUseCounts, copiedLodPrefabPathsByGuid, log);

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
        IReadOnlyDictionary<string, int> lodGuidUseCounts,
        IDictionary<string, string> copiedLodPrefabPathsByGuid,
        List<string> log)
    {
        var constructionName = GetConstructionName(constructionAssetPath);
        var lodGuids = ReadLodGuids(constructionAssetPath);

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
            var guid = lodGuids[i];
            if (copiedLodPrefabPathsByGuid.TryGetValue(guid, out var alreadyCopiedPath))
            {
                copiedLodPrefabPaths.Add(alreadyCopiedPath);
                continue;
            }

            var existingAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!_overwriteExisting && !string.IsNullOrEmpty(existingAssetPath))
            {
                copiedLodPrefabPathsByGuid[guid] = existingAssetPath;
                copiedLodPrefabPaths.Add(existingAssetPath);
                continue;
            }

            if (!guidMap.TryGetValue(guid, out var oldPrefabPath) || !File.Exists(oldPrefabPath))
            {
                log.Add($"  Missing LOD {i} for {constructionName}: {guid}");
                continue;
            }

            var sourceLodFolder = Path.GetDirectoryName(oldPrefabPath);
            if (string.IsNullOrEmpty(sourceLodFolder) || !Directory.Exists(sourceLodFolder))
            {
                log.Add($"  Missing LOD folder for {constructionName}: {oldPrefabPath}");
                continue;
            }

            var lodFolderName = SanitizeFileName(Path.GetFileName(sourceLodFolder));
            if (string.IsNullOrEmpty(lodFolderName))
                lodFolderName = $"LOD{i}";

            var shouldUseSharedFolder = lodGuidUseCounts.TryGetValue(guid, out var useCount) && useCount > 1;
            var lodParentFolder = shouldUseSharedFolder
                ? AssetPathCombine(_destinationRoot, SharedLodFolderName)
                : constructionFolder;
            EnsureAssetFolder(lodParentFolder);

            var destinationLodFolderName = shouldUseSharedFolder
                ? $"{lodFolderName}_{guid.Substring(0, 8)}"
                : lodFolderName;
            var destinationLodFolder = AssetPathCombine(lodParentFolder, destinationLodFolderName);
            if (AssetDatabase.IsValidFolder(destinationLodFolder) || Directory.Exists(destinationLodFolder))
            {
                FileUtil.DeleteFileOrDirectory(destinationLodFolder);
                FileUtil.DeleteFileOrDirectory($"{destinationLodFolder}.meta");
            }

            FileUtil.CopyFileOrDirectory(sourceLodFolder, destinationLodFolder);

            var sourceMetaPath = $"{sourceLodFolder}.meta";
            if (File.Exists(sourceMetaPath))
                FileUtil.CopyFileOrDirectory(sourceMetaPath, $"{destinationLodFolder}.meta");

            var copiedPrefabPath = AssetPathCombine(destinationLodFolder, Path.GetFileName(oldPrefabPath));
            AssetDatabase.ImportAsset(destinationLodFolder, ImportAssetOptions.ImportRecursive);
            copiedLodPrefabPathsByGuid[guid] = copiedPrefabPath;
            copiedLodPrefabPaths.Add(copiedPrefabPath);
        }

        AssetDatabase.ImportAsset(constructionFolder, ImportAssetOptions.ImportRecursive);
        CreateParentPrefab(constructionName, constructionFolder, copiedLodPrefabPaths, log);
    }

    private static void CreateParentPrefab(string constructionName, string constructionFolder, IReadOnlyList<string> lodPrefabPaths, List<string> log)
    {
        var root = new GameObject(constructionName);

        try
        {
            var createdCount = 0;
            for (int i = 0; i < lodPrefabPaths.Count; i++)
            {
                var lodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lodPrefabPaths[i]);
                if (lodPrefab == null)
                {
                    log.Add($"  Could not load copied LOD prefab: {lodPrefabPaths[i]}");
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(lodPrefab) as GameObject;
                if (instance == null)
                    instance = Instantiate(lodPrefab);

                instance.name = $"LOD{i}_{lodPrefab.name}";
                instance.transform.SetParent(root.transform, false);
                createdCount++;
            }

            var prefabPath = AssetPathCombine(constructionFolder, $"{SanitizeFileName(constructionName)}.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            log.Add($"Imported {constructionName}: {createdCount} LOD prefab children -> {prefabPath}");
        }
        finally
        {
            DestroyImmediate(root);
        }
    }

    private static List<string> FindConstructionAssets(string sourceFolder)
    {
        var result = new List<string>();
        foreach (var assetPath in Directory.EnumerateFiles(sourceFolder, "*.asset", SearchOption.AllDirectories))
        {
            if (ReadLodGuids(assetPath).Count > 0)
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

    private static Dictionary<string, int> CountLodGuidUses(IEnumerable<string> constructionAssets)
    {
        var result = new Dictionary<string, int>();
        foreach (var constructionAsset in constructionAssets)
        {
            foreach (var guid in ReadLodGuids(constructionAsset))
            {
                if (!result.ContainsKey(guid))
                    result[guid] = 0;

                result[guid]++;
            }
        }

        return result;
    }

    private static List<string> ReadLodGuids(string constructionAssetPath)
    {
        var result = new List<string>();
        var foundLodGroups = false;

        foreach (var line in File.ReadLines(constructionAssetPath))
        {
            var trimmedLine = line.Trim();

            if (!foundLodGroups)
            {
                foundLodGroups = trimmedLine == "lodGroups:";
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

    private void SavePrefs()
    {
        EditorPrefs.SetString(SourceFolderPrefsKey, _sourceFolder);
        EditorPrefs.SetString(DestinationRootPrefsKey, _destinationRoot);
        EditorPrefs.SetBool(OverwriteExistingPrefsKey, _overwriteExisting);
    }
}
