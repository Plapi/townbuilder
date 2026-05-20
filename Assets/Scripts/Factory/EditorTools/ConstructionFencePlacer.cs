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
    [AddComponentMenu("TownCrafter/Editor/Construction Fence Placer")]
    public class ConstructionFencePlacer : MonoBehaviour
    {
        [SerializeField] private Construction _construction;
        [SerializeField] private ConstructionFenceCatalog _fenceCatalog;
        [SerializeField] private List<GameObject> _fences = new List<GameObject>();
        [SerializeField] private float _perimeterPadding;
        [SerializeField] private float _defaultFenceLength = 1f;
        [SerializeField] private bool _randomizeFences = true;

        public Construction Construction => _construction;
        public ConstructionFenceCatalog FenceCatalog => _fenceCatalog;
        public IReadOnlyList<GameObject> Fences => _fences;
        public float PerimeterPadding => Mathf.Max(0f, _perimeterPadding);
        public float DefaultFenceLength => Mathf.Max(0.01f, _defaultFenceLength);
        public bool RandomizeFences => _randomizeFences;

        private void Reset()
        {
            TryAssignConstructionFromParents();
            TryAssignFenceCatalog();
        }

        private void OnValidate()
        {
            if (_construction == null)
                TryAssignConstructionFromParents();

            if (_fenceCatalog == null)
                TryAssignFenceCatalog();
        }

        private void TryAssignConstructionFromParents()
        {
            _construction = GetComponentInParent<Construction>();
        }

        private void TryAssignFenceCatalog()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:ConstructionFenceCatalog");
            if (guids.Length == 0)
                return;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _fenceCatalog = AssetDatabase.LoadAssetAtPath<ConstructionFenceCatalog>(path);
#endif
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionFencePlacer))]
    public class ConstructionFencePlacerEditor : Editor
    {
        private const string GENERATED_FENCE_NAME_SUFFIX = " (Generated Fence)";

        private SerializedProperty _constructionProperty;
        private SerializedProperty _fenceCatalogProperty;
        private SerializedProperty _fencesProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _fenceCatalogProperty = serializedObject.FindProperty("_fenceCatalog");
            _fencesProperty = serializedObject.FindProperty("_fences");

            TryAssignDefaultCatalog();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_fences");
            DrawFenceCatalogSelector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Instantiate Fences"))
                    GenerateFences();
            }

            if (GUILayout.Button("Clear Generated Fences"))
                ClearGeneratedFences();
        }

        private bool CanGenerate()
        {
            if (_constructionProperty.objectReferenceValue == null)
                return false;

            for (int i = 0; i < _fencesProperty.arraySize; i++)
            {
                var prefab = _fencesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (prefab != null)
                    return true;
            }

            return false;
        }

        private void DrawFenceCatalogSelector()
        {
            var catalog = _fenceCatalogProperty.objectReferenceValue as ConstructionFenceCatalog;
            if (catalog == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Randomized Fences", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
                SetAllCatalogFencesSelected(catalog, true);
            if (GUILayout.Button("Clear"))
                SetAllCatalogFencesSelected(catalog, false);
            EditorGUILayout.EndHorizontal();

            foreach (var fence in catalog.Fences)
            {
                if (fence == null)
                    continue;

                var selected = ContainsFence(fence);
                var nextSelected = EditorGUILayout.ToggleLeft(fence.name, selected);
                if (nextSelected != selected)
                    SetFenceSelected(fence, nextSelected);
            }
        }

        private void TryAssignDefaultCatalog()
        {
            serializedObject.Update();
            if (_fenceCatalogProperty.objectReferenceValue != null)
                return;

            var guids = AssetDatabase.FindAssets("t:ConstructionFenceCatalog");
            if (guids.Length == 0)
                return;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _fenceCatalogProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConstructionFenceCatalog>(path);
            serializedObject.ApplyModifiedProperties();
        }

        private bool ContainsFence(GameObject fence)
        {
            for (int i = 0; i < _fencesProperty.arraySize; i++)
            {
                if (_fencesProperty.GetArrayElementAtIndex(i).objectReferenceValue == fence)
                    return true;
            }

            return false;
        }

        private void SetFenceSelected(GameObject fence, bool selected)
        {
            if (selected)
            {
                if (ContainsFence(fence))
                    return;

                _fencesProperty.arraySize++;
                _fencesProperty.GetArrayElementAtIndex(_fencesProperty.arraySize - 1).objectReferenceValue = fence;
                return;
            }

            for (int i = _fencesProperty.arraySize - 1; i >= 0; i--)
            {
                if (_fencesProperty.GetArrayElementAtIndex(i).objectReferenceValue == fence)
                    DeleteFenceAt(i);
            }
        }

        private void DeleteFenceAt(int index)
        {
            _fencesProperty.DeleteArrayElementAtIndex(index);
            if (index < _fencesProperty.arraySize && _fencesProperty.GetArrayElementAtIndex(index).objectReferenceValue == null)
                _fencesProperty.DeleteArrayElementAtIndex(index);
        }

        private void SetAllCatalogFencesSelected(ConstructionFenceCatalog catalog, bool selected)
        {
            foreach (var fence in catalog.Fences)
            {
                if (fence != null)
                    SetFenceSelected(fence, selected);
            }
        }

        private void GenerateFences()
        {
            var placer = (ConstructionFencePlacer)target;
            var construction = placer.Construction;
            if (construction == null)
                return;

            ClearGeneratedFences(placer.transform);

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var measuredLengths = new Dictionary<GameObject, float>();
            var fenceIndex = 0;

            CreateSide(placer, placer.transform, construction, construction.Right, -construction.Forward, construction.Size.x, placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);
            CreateSide(placer, placer.transform, construction, construction.Right, construction.Forward, construction.Size.x, construction.Size.y + placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);
            CreateSide(placer, placer.transform, construction, construction.Forward, -construction.Right, construction.Size.y, placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);
            CreateSide(placer, placer.transform, construction, construction.Forward, construction.Right, construction.Size.y, construction.Size.x + placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void CreateSide(
            ConstructionFencePlacer placer,
            Transform root,
            Construction construction,
            Vector2Int tangent,
            Vector2Int outward,
            float sideLength,
            float outwardOffset,
            float scale,
            Dictionary<GameObject, float> measuredLengths,
            ref int fenceIndex)
        {
            var cursor = 0f;

            while (cursor < sideLength)
            {
                var fencePrefab = GetNextFence(placer, fenceIndex);
                if (fencePrefab == null)
                    return;

                var fenceLength = ResolveFenceLength(fencePrefab, placer.DefaultFenceLength, scale, measuredLengths);
                if (cursor + fenceLength > sideLength)
                    break;

                var center = construction.transform.position +
                             ToWorld(tangent) * (cursor + fenceLength * 0.5f) +
                             ToWorld(outward) * outwardOffset;

                var rotation = Quaternion.LookRotation(ToWorld(outward), Vector3.up);
                var instance = PrefabUtility.InstantiatePrefab(fencePrefab, root) as GameObject;
                if (instance == null)
                    instance = Instantiate(fencePrefab, root);

                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Construction Fence");
                instance.transform.SetPositionAndRotation(center, rotation);
                instance.transform.localScale = Vector3.one * scale;
                instance.name = $"{fencePrefab.name}{GENERATED_FENCE_NAME_SUFFIX}";

                cursor += fenceLength;
                fenceIndex++;
            }
        }

        private static float ResolveFenceLength(
            GameObject fencePrefab,
            float defaultLength,
            float scale,
            Dictionary<GameObject, float> measuredLengths)
        {
            if (!measuredLengths.TryGetValue(fencePrefab, out var measuredLength))
            {
                measuredLength = MeasurePrefabLength(fencePrefab) * scale;
                measuredLengths.Add(fencePrefab, measuredLength);
            }

            return Mathf.Max(0.01f, measuredLength > 0f ? measuredLength : defaultLength);
        }

        private static float MeasurePrefabLength(GameObject prefab)
        {
            var renderer = prefab.GetComponent<MeshRenderer>();
            if (renderer == null)
                return 0f;

            var rootToLocal = prefab.transform.worldToLocalMatrix;
            var hasBounds = false;
            var bounds = new Bounds();
            var matrix = rootToLocal * renderer.transform.localToWorldMatrix;
            var rendererBounds = renderer.localBounds;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var corner = rendererBounds.center + Vector3.Scale(rendererBounds.extents, new Vector3(x, y, z));
                        var localCorner = matrix.MultiplyPoint3x4(corner);

                        if (!hasBounds)
                        {
                            bounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localCorner);
                        }
                    }
                }
            }

            return Mathf.Max(bounds.size.x, bounds.size.z);
        }

        private static GameObject GetNextFence(ConstructionFencePlacer placer, int index)
        {
            var availableFences = new List<GameObject>();
            foreach (var fence in placer.Fences)
            {
                if (fence != null)
                    availableFences.Add(fence);
            }

            if (availableFences.Count == 0)
                return null;

            if (!placer.RandomizeFences || availableFences.Count == 1)
                return availableFences[index % availableFences.Count];

            return availableFences[Random.Range(0, availableFences.Count)];
        }

        private void ClearGeneratedFences()
        {
            var placer = (ConstructionFencePlacer)target;
            ClearGeneratedFences(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void ClearGeneratedFences(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_FENCE_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static Vector3 ToWorld(Vector2Int gridDirection)
        {
            return new Vector3(gridDirection.x, 0f, gridDirection.y);
        }
    }
#endif
}
