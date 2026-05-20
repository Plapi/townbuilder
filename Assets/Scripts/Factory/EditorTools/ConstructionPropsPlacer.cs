using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("TownCrafter/Editor/Construction Props Placer")]
    public class ConstructionPropsPlacer : MonoBehaviour
    {
        [SerializeField] private Construction _construction;
        [SerializeField] private ConstructionPropsCatalog _propsCatalog;
        [SerializeField] private int _propsCount = 8;
        [SerializeField] private bool _randomizeRotation = true;

        public Construction Construction => _construction;
        public ConstructionPropsCatalog PropsCatalog => _propsCatalog;
        public int PropsCount => Mathf.Max(0, _propsCount);
        public bool RandomizeRotation => _randomizeRotation;

        private void Reset()
        {
            TryAssignConstructionFromParents();
            TryAssignPropsCatalog();
        }

        private void OnValidate()
        {
            if (_construction == null)
                TryAssignConstructionFromParents();

            if (_propsCatalog == null)
                TryAssignPropsCatalog();
        }

        private void TryAssignConstructionFromParents()
        {
            _construction = GetComponentInParent<Construction>();
        }

        private void TryAssignPropsCatalog()
        {
#if UNITY_EDITOR
            _propsCatalog = ConstructionPropsCatalogEditorUtility.FindDefaultCatalog();
#endif
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionPropsPlacer))]
    public class ConstructionPropsPlacerEditor : Editor
    {
        private const string GENERATED_PROP_NAME_SUFFIX = " (Generated Construction Prop)";
        private const int MAX_PLACEMENT_ATTEMPTS_PER_PROP = 40;

        private SerializedProperty _constructionProperty;
        private SerializedProperty _propsCatalogProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _propsCatalogProperty = serializedObject.FindProperty("_propsCatalog");

            TryAssignDefaultCatalog();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Instantiate Props"))
                    GenerateProps();
            }

            if (GUILayout.Button("Clear Generated Props"))
                ClearGeneratedProps();
        }

        private bool CanGenerate()
        {
            if (_constructionProperty.objectReferenceValue == null)
                return false;

            var catalog = _propsCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return false;

            foreach (var prop in catalog.Props)
            {
                if (prop != null)
                    return true;
            }

            return false;
        }

        private void TryAssignDefaultCatalog()
        {
            serializedObject.Update();
            if (_propsCatalogProperty.objectReferenceValue != null)
                return;

            _propsCatalogProperty.objectReferenceValue = ConstructionPropsCatalogEditorUtility.FindDefaultCatalog();
            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateProps()
        {
            var placer = (ConstructionPropsPlacer)target;
            var construction = placer.Construction;
            var catalog = placer.PropsCatalog;
            if (construction == null || catalog == null)
                return;

            var availableProps = GetAvailableProps(catalog);
            if (availableProps.Count == 0)
                return;

            ClearGeneratedProps(placer.transform);

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var placedRects = new List<Rect>();
            var placedCount = 0;
            var attempts = 0;
            var maxAttempts = Mathf.Max(placer.PropsCount * MAX_PLACEMENT_ATTEMPTS_PER_PROP, MAX_PLACEMENT_ATTEMPTS_PER_PROP);

            while (placedCount < placer.PropsCount && attempts < maxAttempts)
            {
                attempts++;
                var propPrefab = availableProps[Random.Range(0, availableProps.Count)];
                var rotation = GetRotation(placer);
                var worldRotation = Quaternion.LookRotation(ToWorld(construction.Forward), Vector3.up) * rotation;
                if (!TryGetRandomPlacement(construction, propPrefab, rotation, scale, placedRects, out var position, out var rect))
                    continue;

                var instance = PrefabUtility.InstantiatePrefab(propPrefab, placer.transform) as GameObject;
                if (instance == null)
                    instance = Instantiate(propPrefab, placer.transform);

                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Construction Prop");
                instance.transform.SetPositionAndRotation(position, worldRotation);
                instance.transform.localScale = Vector3.one * scale;
                instance.name = $"{propPrefab.name}{GENERATED_PROP_NAME_SUFFIX}";

                placedRects.Add(rect);
                placedCount++;
            }

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static List<GameObject> GetAvailableProps(ConstructionPropsCatalog catalog)
        {
            var availableProps = new List<GameObject>();
            foreach (var prop in catalog.Props)
            {
                if (prop != null)
                    availableProps.Add(prop);
            }

            return availableProps;
        }

        private static bool TryGetRandomPlacement(
            Construction construction,
            GameObject propPrefab,
            Quaternion rotation,
            float scale,
            List<Rect> placedRects,
            out Vector3 position,
            out Rect rect)
        {
            position = default;
            rect = default;

            if (!TryGetLocalFootprint(propPrefab, rotation, scale, out var min, out var max))
                return false;

            var minPivotX = -min.x;
            var maxPivotX = construction.Size.x - max.x;
            var minPivotZ = -min.y;
            var maxPivotZ = construction.Size.y - max.y;

            if (minPivotX > maxPivotX || minPivotZ > maxPivotZ)
                return false;

            for (int i = 0; i < MAX_PLACEMENT_ATTEMPTS_PER_PROP; i++)
            {
                var pivot = new Vector2(
                    Random.Range(minPivotX, maxPivotX),
                    Random.Range(minPivotZ, maxPivotZ));

                rect = Rect.MinMaxRect(
                    pivot.x + min.x,
                    pivot.y + min.y,
                    pivot.x + max.x,
                    pivot.y + max.y);

                if (OverlapsAny(rect, placedRects))
                    continue;

                position = construction.transform.position +
                           ToWorld(construction.Right) * pivot.x +
                           ToWorld(construction.Forward) * pivot.y;
                return true;
            }

            return false;
        }

        private static bool TryGetLocalFootprint(
            GameObject prefab,
            Quaternion rotation,
            float scale,
            out Vector2 min,
            out Vector2 max)
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
                            var localCorner = rotation * (matrix.MultiplyPoint3x4(corner) * scale);
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

        private static bool OverlapsAny(Rect rect, List<Rect> placedRects)
        {
            foreach (var placedRect in placedRects)
            {
                if (rect.Overlaps(placedRect))
                    return true;
            }

            return false;
        }

        private static Quaternion GetRotation(ConstructionPropsPlacer placer)
        {
            if (!placer.RandomizeRotation)
                return Quaternion.identity;

            return Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
        }

        private void ClearGeneratedProps()
        {
            var placer = (ConstructionPropsPlacer)target;
            ClearGeneratedProps(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void ClearGeneratedProps(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_PROP_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static Vector3 ToWorld(Vector2Int gridDirection)
        {
            return new Vector3(gridDirection.x, 0f, gridDirection.y);
        }
    }

    public static class ConstructionPropsCatalogEditorUtility
    {
        private const string DEFAULT_CATALOG_NAME = "ConstructionPropsCatalog";

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

            var firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ConstructionPropsCatalog>(firstPath);
        }
    }
#endif
}
