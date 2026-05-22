#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("TownCrafter/Editor/Construction House Placer")]
    public class ConstructionHousePlacer : MonoBehaviour
    {
        [SerializeField] private Construction _construction;
        [SerializeField] private ConstructionPropsCatalog _houseCatalog;
        [SerializeField] private GameObject _housePrefab;
        [SerializeField] private Material _material;
        [SerializeField] private bool _resizeConstruction = true;
        [SerializeField] private bool _updateGround = true;
        [SerializeField] private int _paddingCells = 2;

        public Construction Construction => _construction;
        public ConstructionPropsCatalog HouseCatalog => _houseCatalog;
        public GameObject HousePrefab => _housePrefab;
        public Material Material => _material;
        public bool ResizeConstruction => _resizeConstruction;
        public bool UpdateGround => _updateGround;
        public int PaddingCells => Mathf.Max(0, _paddingCells);

        private void Reset()
        {
            TryAssignConstructionFromParents();
            TryAssignHouseCatalog();
        }

        private void OnValidate()
        {
            if (_construction == null)
                TryAssignConstructionFromParents();

            if (_houseCatalog == null)
                TryAssignHouseCatalog();

            _paddingCells = Mathf.Max(0, _paddingCells);
        }

        private void TryAssignConstructionFromParents()
        {
            _construction = GetComponentInParent<Construction>();
        }

        private void TryAssignHouseCatalog()
        {
#if UNITY_EDITOR
            _houseCatalog = ConstructionHouseCatalogEditorUtility.FindDefaultCatalog();
#endif
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionHousePlacer))]
    public class ConstructionHousePlacerEditor : Editor
    {
        private const string GENERATED_HOUSE_NAME_SUFFIX = " (Generated House)";
        private const string SIZE_PROPERTY = "_size";
        private const string GROUND_NAME = "Ground";

        private SerializedProperty _constructionProperty;
        private SerializedProperty _houseCatalogProperty;
        private SerializedProperty _housePrefabProperty;
        private SerializedProperty _materialProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _houseCatalogProperty = serializedObject.FindProperty("_houseCatalog");
            _housePrefabProperty = serializedObject.FindProperty("_housePrefab");
            _materialProperty = serializedObject.FindProperty("_material");

            TryAssignDefaultCatalog();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            DrawHouseCatalogSelector();
            DrawMaterialCatalogSelector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanPlaceHouse()))
            {
                if (GUILayout.Button("Place House"))
                    PlaceHouse();
            }

            if (GUILayout.Button("Clear Generated House"))
                ClearGeneratedHouse();
        }

        private bool CanPlaceHouse()
        {
            return _constructionProperty.objectReferenceValue != null &&
                   _housePrefabProperty.objectReferenceValue != null;
        }

        private void TryAssignDefaultCatalog()
        {
            serializedObject.Update();
            if (_houseCatalogProperty.objectReferenceValue != null)
                return;

            _houseCatalogProperty.objectReferenceValue = ConstructionHouseCatalogEditorUtility.FindDefaultCatalog();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawHouseCatalogSelector()
        {
            var catalog = _houseCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Catalog Houses", EditorStyles.boldLabel);

            foreach (var house in catalog.Props)
            {
                if (house == null)
                    continue;

                var selected = _housePrefabProperty.objectReferenceValue == house;
                var nextSelected = EditorGUILayout.ToggleLeft(house.name, selected);
                if (nextSelected != selected)
                    _housePrefabProperty.objectReferenceValue = nextSelected ? house : null;
            }
        }

        private void DrawMaterialCatalogSelector()
        {
            var catalog = _houseCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Catalog Materials", EditorStyles.boldLabel);

            var keepPrefabMaterial = _materialProperty.objectReferenceValue == null;
            var nextKeepPrefabMaterial = EditorGUILayout.ToggleLeft("Use Prefab Material", keepPrefabMaterial);
            if (nextKeepPrefabMaterial != keepPrefabMaterial)
                _materialProperty.objectReferenceValue = null;

            foreach (var material in catalog.Materials)
            {
                if (material == null)
                    continue;

                var selected = _materialProperty.objectReferenceValue == material;
                var nextSelected = EditorGUILayout.ToggleLeft(material.name, selected);
                if (nextSelected != selected)
                    _materialProperty.objectReferenceValue = nextSelected ? material : null;
            }
        }

        private void PlaceHouse()
        {
            var placer = (ConstructionHousePlacer)target;
            var construction = placer.Construction;
            var housePrefab = placer.HousePrefab;
            if (construction == null || housePrefab == null)
                return;

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            if (!TryGetLocalFootprint(housePrefab, scale, out var min, out var max))
            {
                Debug.LogError($"Place house failed: '{housePrefab.name}' has no MeshRenderer bounds.", housePrefab);
                return;
            }

            var footprintSize = max - min;
            if (placer.ResizeConstruction)
                ResizeConstructionIfNeeded(construction, footprintSize, placer.PaddingCells);

            ClearGeneratedHouse(placer.transform);

            var instance = PrefabUtility.InstantiatePrefab(housePrefab, placer.transform) as GameObject;
            if (instance == null)
                instance = Instantiate(housePrefab, placer.transform);

            Undo.RegisterCreatedObjectUndo(instance, "Place Construction House");
            instance.name = $"{housePrefab.name}{GENERATED_HOUSE_NAME_SUFFIX}";
            ApplySelectedMaterial(instance, placer.Material);

            var footprintCenter = (min + max) * 0.5f;
            var constructionCenter = construction.transform.position +
                                     ToWorld(construction.Right) * (construction.Size.x * 0.5f) +
                                     ToWorld(construction.Forward) * (construction.Size.y * 0.5f);
            var housePivotOffset = ToWorld(construction.Right) * footprintCenter.x +
                                   ToWorld(construction.Forward) * footprintCenter.y;
            var houseRotation = Quaternion.LookRotation(ToWorld(construction.Forward), Vector3.up);

            instance.transform.SetPositionAndRotation(constructionCenter - housePivotOffset, houseRotation);
            instance.transform.localScale = Vector3.one * scale;

            if (placer.UpdateGround)
                UpdateSiblingGround(placer.transform, construction.Size);

            EditorUtility.SetDirty(placer);
            EditorUtility.SetDirty(construction);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void ResizeConstructionIfNeeded(Construction construction, Vector2 footprintSize, int paddingCells)
        {
            var requiredSize = new Vector2Int(
                Mathf.Max(1, Mathf.CeilToInt(footprintSize.x) + paddingCells * 2),
                Mathf.Max(1, Mathf.CeilToInt(footprintSize.y) + paddingCells * 2));

            var currentSize = construction.Size;
            if (requiredSize == currentSize)
                return;

            Undo.RecordObject(construction, "Resize Construction For House");

            var serializedConstruction = new SerializedObject(construction);
            var sizeProperty = serializedConstruction.FindProperty(SIZE_PROPERTY);
            if (sizeProperty != null)
                sizeProperty.vector2IntValue = requiredSize;
            serializedConstruction.ApplyModifiedProperties();
        }

        private static void ApplySelectedMaterial(GameObject instance, Material selectedMaterial)
        {
            if (instance == null || selectedMaterial == null)
                return;

            var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                var changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (!CanReplaceWithHouseMaterial(materials[i]))
                        continue;

                    materials[i] = selectedMaterial;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        private static bool CanReplaceWithHouseMaterial(Material material)
        {
            if (material == null)
                return false;

            return material.name.StartsWith("PolygonTown_") &&
                   !material.name.Contains("Glass") &&
                   !material.name.Contains("Road");
        }

        private void ClearGeneratedHouse()
        {
            var placer = (ConstructionHousePlacer)target;
            ClearGeneratedHouse(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void ClearGeneratedHouse(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_HOUSE_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void UpdateSiblingGround(Transform environment, Vector2Int constructionSize)
        {
            var stage = environment.parent;
            if (stage == null)
                return;

            var ground = stage.Find(GROUND_NAME);
            if (ground == null)
                return;

            Undo.RecordObject(ground, "Update Construction Ground");
            ground.localPosition = new Vector3(constructionSize.x * 0.5f, 0f, constructionSize.y * 0.5f);
            ground.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.localScale = new Vector3(constructionSize.x, constructionSize.y, 1f);
            EditorUtility.SetDirty(ground);
        }

        private static bool TryGetLocalFootprint(GameObject prefab, float scale, out Vector2 min, out Vector2 max)
        {
            min = default;
            max = default;

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
                return false;

            var rootToLocal = prefab.transform.worldToLocalMatrix;
            var hasBounds = false;

            foreach (var renderer in renderers)
            {
                var matrix = rootToLocal * renderer.transform.localToWorldMatrix;
                var rendererBounds = renderer.localBounds;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var corner = rendererBounds.center + Vector3.Scale(rendererBounds.extents, new Vector3(x, y, z));
                            var localCorner = matrix.MultiplyPoint3x4(corner) * scale;
                            var point = new Vector2(localCorner.x, localCorner.z);

                            if (!hasBounds)
                            {
                                min = point;
                                max = point;
                                hasBounds = true;
                            }
                            else
                            {
                                min = Vector2.Min(min, point);
                                max = Vector2.Max(max, point);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private static Vector3 ToWorld(Vector2Int gridDirection)
        {
            return new Vector3(gridDirection.x, 0f, gridDirection.y);
        }
    }

    public static class ConstructionHouseCatalogEditorUtility
    {
        private const string DEFAULT_CATALOG_NAME = "SyntyHousePrefabsCatalog";

        public static ConstructionPropsCatalog FindDefaultCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:ConstructionPropsCatalog");
            if (guids.Length == 0)
                return null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<ConstructionPropsCatalog>(path);
                if (catalog != null && catalog.name == DEFAULT_CATALOG_NAME)
                    return catalog;
            }

            return null;
        }
    }
#endif
}
