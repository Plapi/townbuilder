using System.Collections.Generic;
using System.Text.RegularExpressions;
using com.Plapamaru.TownCrafter.Factory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(Construction))]
public class ConstructionEditor : Editor
{
    private const string EXPORT_FOLDER_PROPERTY = "_exportFolder";
    private const string STAGES_PROPERTY = "_stages";
    private const string GRAPHIC_NAME = "Graphic";
    private const string INPUTS_NAME = "Inputs";
    private const string GROUND_NAME = "Ground";
    private const string ENVIRONMENT_NAME = "Environment";
    private const string NOT_OPTIMIZED_SUFFIX = "NotOptimized";
    private const string OPTIMIZED_SUFFIX = "Optimized";
    private const string ENVIRONMENT_MESH_NAME = "EnvironmentMesh";
    private const string DEFAULT_CONSTRUCTION_NAME = "NewConstructionNotOptimized";
    private const string GROUND_MATERIAL_PATH = "Assets/Materials/Dirt.mat";
    private const string SIZE_PROPERTY = "_size";

    private static readonly Regex StageRegex = new Regex(@"^Stage(\d+)NotOptimized$", RegexOptions.Compiled);

    private SerializedProperty _exportFolderProperty;

    private void OnEnable()
    {
        _exportFolderProperty = serializedObject.FindProperty(EXPORT_FOLDER_PROPERTY);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_exportFolderProperty == null || _exportFolderProperty.objectReferenceValue == null))
        {
            if (GUILayout.Button("Export Construction"))
                ExportSelectedConstruction();
        }

        if (GUILayout.Button("Update Grounds From Size"))
            UpdateGroundsFromSize();
    }

    [MenuItem("TownCrafter/Construction/Create Not Optimized Construction")]
    private static void CreateNotOptimizedConstruction()
    {
        var root = new GameObject(DEFAULT_CONSTRUCTION_NAME);
        SetLayerRecursively(root, ENVIRONMENT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Create Not Optimized Construction");

        var construction = root.AddComponent<Construction>();
        var graphic = CreateChild(root.transform, GRAPHIC_NAME);
        var inputs = CreateChild(root.transform, INPUTS_NAME);

        var stages = new List<GameObject>();
        for (var i = 0; i < 3; i++)
        {
            var stage = CreateChild(graphic.transform, $"Stage{i}{NOT_OPTIMIZED_SUFFIX}");
            stage.transform.localPosition = new Vector3(8f, 0f, 0f);

            CreateDefaultGround(stage.transform);
            var environment = CreateChild(stage.transform, ENVIRONMENT_NAME);
            SetLayerRecursively(environment, ENVIRONMENT_NAME);

            stages.Add(stage);
        }

        var input0 = CreateChild(inputs.transform, "Input0");
        input0.transform.localPosition = new Vector3(0f, 0f, -1f);

        var input1 = CreateChild(inputs.transform, "Input1");
        input1.transform.localPosition = new Vector3(3f, 0f, -1f);

        AssignTemplateReferences(construction, stages, new[] { input0.transform, input1.transform });

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    private void ExportSelectedConstruction()
    {
        var construction = (Construction)target;
        if (!TryGetExportFolderPath(out var exportFolderPath))
            return;

        if (!TryValidateConstruction(construction, out var graphic, out var inputs, out var stages))
            return;

        if (!TryValidateLayer(ENVIRONMENT_NAME) || !TryValidateLayer(GROUND_NAME))
            return;

        var constructionName = construction.gameObject.name;
        var rootFolder = EnsureFolder(exportFolderPath, constructionName);
        if (string.IsNullOrEmpty(rootFolder))
            return;

        var optimizedFolder = EnsureFolder(rootFolder, $"{constructionName}{OPTIMIZED_SUFFIX}");
        if (string.IsNullOrEmpty(optimizedFolder))
            return;

        try
        {
            SaveNotOptimizedPrefab(construction.gameObject, rootFolder, constructionName);

            var stagePrefabPaths = new List<string>();
            foreach (var stage in stages)
            {
                var stagePrefabPath = ExportStage(stage, optimizedFolder);
                if (string.IsNullOrEmpty(stagePrefabPath))
                    return;

                stagePrefabPaths.Add(stagePrefabPath);
            }

            ExportOptimizedRoot(construction.gameObject, graphic, inputs, stagePrefabPaths, optimizedFolder, constructionName);
        }
        finally
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Construction export completed: {rootFolder}", construction);
    }

    private void UpdateGroundsFromSize()
    {
        var construction = (Construction)target;
        var sizeProperty = serializedObject.FindProperty(SIZE_PROPERTY);
        var constructionSize = sizeProperty != null ? sizeProperty.vector2IntValue : construction.Size;

        var graphic = FindDirectChild(construction.transform, GRAPHIC_NAME);
        if (graphic == null)
        {
            Debug.LogError($"Update grounds failed: missing direct child '{GRAPHIC_NAME}'.", construction);
            return;
        }

        var updatedCount = 0;
        foreach (Transform stage in graphic)
        {
            var ground = FindDirectChild(stage, GROUND_NAME);
            if (ground == null)
                continue;

            Undo.RecordObject(ground, "Update Construction Ground");
            ApplyGroundTransform(ground, constructionSize);
            SetLayerRecursively(ground.gameObject, GROUND_NAME);
            EditorUtility.SetDirty(ground);
            updatedCount++;
        }

        if (updatedCount == 0)
        {
            Debug.LogWarning($"Update grounds found no direct '{GROUND_NAME}' children under '{GRAPHIC_NAME}' stages.", construction);
            return;
        }

        EditorSceneManager.MarkSceneDirty(construction.gameObject.scene);
        Debug.Log($"Updated {updatedCount} construction ground object(s) from size {constructionSize.x}x{constructionSize.y}.", construction);
    }

    private bool TryGetExportFolderPath(out string folderPath)
    {
        folderPath = null;

        var folder = _exportFolderProperty?.objectReferenceValue;
        if (folder == null)
        {
            Debug.LogError("Construction export failed: export folder is not assigned.", target);
            return false;
        }

        folderPath = AssetDatabase.GetAssetPath(folder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("Construction export failed: export folder must be a valid project folder asset.", target);
            return false;
        }

        return true;
    }

    private static bool TryValidateConstruction(
        Construction construction,
        out Transform graphic,
        out Transform inputs,
        out List<StageInfo> stages)
    {
        graphic = null;
        inputs = null;
        stages = new List<StageInfo>();

        graphic = FindDirectChild(construction.transform, GRAPHIC_NAME);
        if (graphic == null)
        {
            Debug.LogError($"Construction export failed: missing direct child '{GRAPHIC_NAME}'.", construction);
            return false;
        }

        inputs = FindDirectChild(construction.transform, INPUTS_NAME);
        if (inputs == null)
        {
            Debug.LogError($"Construction export failed: missing direct child '{INPUTS_NAME}'.", construction);
            return false;
        }

        var stageByIndex = new SortedDictionary<int, StageInfo>();
        foreach (Transform child in graphic)
        {
            var match = StageRegex.Match(child.name);
            if (!match.Success)
            {
                Debug.LogError(
                    $"Construction export failed: '{GRAPHIC_NAME}' child '{child.name}' must be named like Stage0NotOptimized.",
                    construction);
                return false;
            }

            var stageIndex = int.Parse(match.Groups[1].Value);
            if (stageByIndex.ContainsKey(stageIndex))
            {
                Debug.LogError($"Construction export failed: duplicate Stage{stageIndex}NotOptimized under '{GRAPHIC_NAME}'.", construction);
                return false;
            }

            if (!TryValidateStage(child, stageIndex, construction, out var stageInfo))
                return false;

            stageByIndex.Add(stageIndex, stageInfo);
        }

        if (stageByIndex.Count == 0)
        {
            Debug.LogError($"Construction export failed: '{GRAPHIC_NAME}' must contain at least Stage0NotOptimized.", construction);
            return false;
        }

        var expectedIndex = 0;
        foreach (var pair in stageByIndex)
        {
            if (pair.Key != expectedIndex)
            {
                Debug.LogError($"Construction export failed: missing Stage{expectedIndex}NotOptimized under '{GRAPHIC_NAME}'.", construction);
                return false;
            }

            stages.Add(pair.Value);
            expectedIndex++;
        }

        return true;
    }

    private static bool TryValidateStage(Transform stage, int stageIndex, Construction construction, out StageInfo stageInfo)
    {
        stageInfo = default;

        Transform ground = null;
        Transform environment = null;

        foreach (Transform child in stage)
        {
            if (child.name == GROUND_NAME)
            {
                if (ground != null)
                {
                    Debug.LogError($"Construction export failed: Stage{stageIndex}NotOptimized has multiple '{GROUND_NAME}' children.", construction);
                    return false;
                }

                ground = child;
            }
            else if (child.name == ENVIRONMENT_NAME)
            {
                if (environment != null)
                {
                    Debug.LogError($"Construction export failed: Stage{stageIndex}NotOptimized has multiple '{ENVIRONMENT_NAME}' children.", construction);
                    return false;
                }

                environment = child;
            }
            else
            {
                Debug.LogError(
                    $"Construction export failed: Stage{stageIndex}NotOptimized contains unsupported child '{child.name}'. Expected optional '{GROUND_NAME}' and required '{ENVIRONMENT_NAME}'.",
                    construction);
                return false;
            }
        }

        if (environment == null)
        {
            Debug.LogError($"Construction export failed: Stage{stageIndex}NotOptimized is missing required child '{ENVIRONMENT_NAME}'.", construction);
            return false;
        }

        if (!HasCombinableMeshes(environment))
        {
            Debug.LogError($"Construction export failed: Stage{stageIndex}NotOptimized/{ENVIRONMENT_NAME} must contain at least one MeshFilter with a MeshRenderer and mesh.", construction);
            return false;
        }

        stageInfo = new StageInfo(stageIndex, stage, ground, environment);
        return true;
    }

    private static GameObject CreateChild(Transform parent, string childName)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        SetLayerRecursively(child, ENVIRONMENT_NAME);
        return child;
    }

    private static GameObject CreateDefaultGround(Transform parent)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ground.name = GROUND_NAME;
        ground.transform.SetParent(parent, false);
        ApplyGroundTransform(ground.transform, new Vector2Int(8, 8));

        var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GROUND_MATERIAL_PATH);
        if (groundMaterial != null && ground.TryGetComponent<MeshRenderer>(out var meshRenderer))
            meshRenderer.sharedMaterial = groundMaterial;

        SetLayerRecursively(ground, GROUND_NAME);
        return ground;
    }

    private static void AssignTemplateReferences(Construction construction, List<GameObject> stages, Transform[] inputs)
    {
        var serializedConstruction = new SerializedObject(construction);

        var sizeProperty = serializedConstruction.FindProperty(SIZE_PROPERTY);
        if (sizeProperty != null)
            sizeProperty.vector2IntValue = new Vector2Int(8, 8);

        var stagesProperty = serializedConstruction.FindProperty(STAGES_PROPERTY);
        stagesProperty.arraySize = stages.Count;
        for (var i = 0; i < stages.Count; i++)
            stagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];

        var inputsProperty = serializedConstruction.FindProperty("_inputs");
        inputsProperty.arraySize = inputs.Length;
        for (var i = 0; i < inputs.Length; i++)
            inputsProperty.GetArrayElementAtIndex(i).objectReferenceValue = inputs[i];

        var outputsProperty = serializedConstruction.FindProperty("_outputs");
        if (outputsProperty != null)
            outputsProperty.arraySize = 0;

        serializedConstruction.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(construction);
    }

    private static void ApplyGroundTransform(Transform ground, Vector2Int constructionSize)
    {
        ground.localPosition = new Vector3(-constructionSize.x * 0.5f, 0f, constructionSize.y * 0.5f);
        ground.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ground.localScale = new Vector3(constructionSize.x, constructionSize.y, 1f);
    }

    private static void SaveNotOptimizedPrefab(GameObject sourceRoot, string rootFolder, string constructionName)
    {
        var prefabPath = $"{rootFolder}/{constructionName}{NOT_OPTIMIZED_SUFFIX}.prefab";
        ReplaceAsset(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(sourceRoot, prefabPath);
    }

    private static string ExportStage(StageInfo stage, string optimizedFolder)
    {
        var stageName = $"Stage{stage.Index}";
        var stageFolder = EnsureFolder(optimizedFolder, stageName);
        if (string.IsNullOrEmpty(stageFolder))
            return null;

        var environmentMeshPath = $"{stageFolder}/{ENVIRONMENT_MESH_NAME}.mesh";
        var environmentPrefabPath = $"{stageFolder}/{ENVIRONMENT_NAME}.prefab";
        var stagePrefabPath = $"{stageFolder}/{stageName}.prefab";

        ReplaceAsset(environmentMeshPath);
        ReplaceAsset(environmentPrefabPath);
        ReplaceAsset(stagePrefabPath);

        var environmentPrefab = CreateEnvironmentPrefab(stage, environmentMeshPath, environmentPrefabPath);
        if (environmentPrefab == null)
            return null;

        var stageRoot = new GameObject(stageName);

        try
        {
            CopyLocalTransform(stage.Source, stageRoot.transform);

            if (stage.Ground != null)
            {
                var groundCopy = Object.Instantiate(stage.Ground.gameObject, stageRoot.transform);
                groundCopy.name = GROUND_NAME;
                CopyLocalTransform(stage.Ground, groundCopy.transform);
                SetLayerRecursively(groundCopy, GROUND_NAME);
            }

            var environmentInstance = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab);
            environmentInstance.name = ENVIRONMENT_NAME;
            environmentInstance.transform.SetParent(stageRoot.transform, false);
            CopyLocalTransform(stage.Environment, environmentInstance.transform);
            SetLayerRecursively(environmentInstance, ENVIRONMENT_NAME);

            PrefabUtility.SaveAsPrefabAsset(stageRoot, stagePrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(stageRoot);
        }

        return stagePrefabPath;
    }

    private static GameObject CreateEnvironmentPrefab(StageInfo stage, string meshPath, string prefabPath)
    {
        var combineRoot = new GameObject($"{stage.Name}_EnvironmentCombineRoot");
        var combinedEnvironment = default(GameObject);

        try
        {
            var environmentCopy = Object.Instantiate(stage.Environment.gameObject, combineRoot.transform);
            environmentCopy.name = ENVIRONMENT_NAME;
            environmentCopy.transform.localPosition = Vector3.zero;
            environmentCopy.transform.localRotation = Quaternion.identity;
            environmentCopy.transform.localScale = Vector3.one;
            SetLayerRecursively(environmentCopy, ENVIRONMENT_NAME);

            combinedEnvironment = MeshUtils.Combine(combineRoot.transform);
            combinedEnvironment.name = ENVIRONMENT_NAME;
            combinedEnvironment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            combinedEnvironment.transform.localScale = Vector3.one;
            SetLayerRecursively(combinedEnvironment, ENVIRONMENT_NAME);

            var meshFilter = combinedEnvironment.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogError($"Construction export failed: '{stage.Name}/{ENVIRONMENT_NAME}' does not contain any mesh to combine.");
                return null;
            }

            meshFilter.sharedMesh.name = ENVIRONMENT_MESH_NAME;
            AssetDatabase.CreateAsset(meshFilter.sharedMesh, meshPath);
            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            return PrefabUtility.SaveAsPrefabAsset(combinedEnvironment, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(combineRoot);
            if (combinedEnvironment != null)
                Object.DestroyImmediate(combinedEnvironment);
        }
    }

    private static void ExportOptimizedRoot(
        GameObject sourceRoot,
        Transform sourceGraphic,
        Transform sourceInputs,
        List<string> stagePrefabPaths,
        string optimizedFolder,
        string constructionName)
    {
        var optimizedRootPath = $"{optimizedFolder}/{constructionName}.prefab";
        ReplaceAsset(optimizedRootPath);

        var rootCopy = Object.Instantiate(sourceRoot);
        rootCopy.name = constructionName;

        try
        {
            var graphicCopy = FindDirectChild(rootCopy.transform, GRAPHIC_NAME);
            var inputsCopy = FindDirectChild(rootCopy.transform, INPUTS_NAME);

            if (graphicCopy != null)
                Object.DestroyImmediate(graphicCopy.gameObject);
            if (inputsCopy != null)
                Object.DestroyImmediate(inputsCopy.gameObject);

            graphicCopy = new GameObject(GRAPHIC_NAME).transform;
            graphicCopy.SetParent(rootCopy.transform, false);
            CopyLocalTransform(sourceGraphic, graphicCopy);

            inputsCopy = Object.Instantiate(sourceInputs.gameObject, rootCopy.transform).transform;
            inputsCopy.name = INPUTS_NAME;
            CopyLocalTransform(sourceInputs, inputsCopy);

            var stageObjects = new List<GameObject>();
            foreach (var stagePrefabPath in stagePrefabPaths)
            {
                var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stagePrefabPath);
                if (stagePrefab == null)
                    continue;

                var stageInstance = (GameObject)PrefabUtility.InstantiatePrefab(stagePrefab);
                stageInstance.transform.SetParent(graphicCopy, false);
                stageObjects.Add(stageInstance);
            }

            AssignStages(rootCopy, stageObjects);

            PrefabUtility.SaveAsPrefabAsset(rootCopy, optimizedRootPath);
        }
        finally
        {
            Object.DestroyImmediate(rootCopy);
        }
    }

    private static void AssignStages(GameObject root, List<GameObject> stages)
    {
        var construction = root.GetComponent<Construction>();
        if (construction == null)
            return;

        var serializedConstruction = new SerializedObject(construction);
        var stagesProperty = serializedConstruction.FindProperty(STAGES_PROPERTY);
        stagesProperty.arraySize = stages.Count;

        for (var i = 0; i < stages.Count; i++)
            stagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];

        var exportFolderProperty = serializedConstruction.FindProperty(EXPORT_FOLDER_PROPERTY);
        if (exportFolderProperty != null)
            exportFolderProperty.objectReferenceValue = null;

        serializedConstruction.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static bool HasCombinableMeshes(Transform root)
    {
        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh != null && meshFilter.GetComponent<MeshRenderer>() != null)
                return true;
        }

        return false;
    }

    private static bool TryValidateLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0)
            return true;

        Debug.LogError($"Construction export failed: layer '{layerName}' does not exist.");
        return false;
    }

    private static string EnsureFolder(string parentFolder, string folderName)
    {
        var folderPath = $"{parentFolder}/{folderName}";
        if (AssetDatabase.IsValidFolder(folderPath))
            return folderPath;

        AssetDatabase.CreateFolder(parentFolder, folderName);
        return AssetDatabase.IsValidFolder(folderPath) ? folderPath : null;
    }

    private static void ReplaceAsset(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
    }

    private static void CopyLocalTransform(Transform source, Transform destination)
    {
        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private static void SetLayerRecursively(GameObject gameObject, string layerName)
    {
        var layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogError($"Construction export failed: layer '{layerName}' does not exist.");
            return;
        }

        foreach (var child in gameObject.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    private readonly struct StageInfo
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Transform Source;
        public readonly Transform Ground;
        public readonly Transform Environment;

        public StageInfo(int index, Transform source, Transform ground, Transform environment)
        {
            Index = index;
            Name = source.name;
            Source = source;
            Ground = ground;
            Environment = environment;
        }
    }
}
