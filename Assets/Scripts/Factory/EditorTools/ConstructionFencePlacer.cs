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
        [SerializeField] private bool _useCustomSize;
        [SerializeField] private Vector2Int _customSize = Vector2Int.one;
        [SerializeField] private float _perimeterPadding;
        [SerializeField] private float _defaultFenceLength = 1f;
        [SerializeField] private bool _randomizeFences = true;

        public Construction Construction => _construction;
        public IReadOnlyList<GameObject> Fences => _fences;
        public bool UseCustomSize => _useCustomSize;
        public Vector2Int Size => _useCustomSize ? ClampSize(_customSize) : (_construction != null ? _construction.Size : Vector2Int.one);
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

            _customSize = ClampSize(_customSize);
        }

        private void OnDrawGizmos()
        {
            if (!_useCustomSize)
                return;

            var right = transform.right;
            var forward = transform.forward;
            right.y = 0f;
            forward.y = 0f;

            if (right.sqrMagnitude <= 0f || forward.sqrMagnitude <= 0f)
                return;

            right.Normalize();
            forward.Normalize();

            var size = ClampSize(_customSize);
            var center = transform.position +
                         right * (size.x * 0.5f) +
                         forward * (size.y * 0.5f) +
                         Vector3.up * 0.03f;

            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(forward, Vector3.up), new Vector3(size.x, 0.02f, size.y));
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.15f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
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

        private static Vector2Int ClampSize(Vector2Int size)
        {
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
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
        private SerializedProperty _useCustomSizeProperty;
        private SerializedProperty _customSizeProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _fenceCatalogProperty = serializedObject.FindProperty("_fenceCatalog");
            _fencesProperty = serializedObject.FindProperty("_fences");
            _useCustomSizeProperty = serializedObject.FindProperty("_useCustomSize");
            _customSizeProperty = serializedObject.FindProperty("_customSize");

            TryAssignDefaultCatalog();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_fences", "_useCustomSize", "_customSize");
            DrawSizeOverride();
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

        private void DrawSizeOverride()
        {
            EditorGUILayout.PropertyField(_useCustomSizeProperty, new GUIContent("Use Custom Size"));

            using (new EditorGUI.DisabledScope(!_useCustomSizeProperty.boolValue))
            {
                EditorGUILayout.PropertyField(_customSizeProperty, new GUIContent("Custom Size"));
            }
        }

        private bool CanGenerate()
        {
            if (!_useCustomSizeProperty.boolValue && _constructionProperty.objectReferenceValue == null)
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
            if (!placer.UseCustomSize && construction == null)
                return;

            ClearGeneratedFences(placer.transform);

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var measuredLengths = new Dictionary<GameObject, float>();
            var fenceIndex = 0;
            var size = placer.Size;
            var origin = placer.UseCustomSize ? placer.transform.position : construction.transform.position;
            var right = placer.UseCustomSize ? FlattenDirection(placer.transform.right) : ToWorld(construction.Right);
            var forward = placer.UseCustomSize ? FlattenDirection(placer.transform.forward) : ToWorld(construction.Forward);

            CreateSide(placer, placer.transform, origin, right, -forward, size.x, placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);
            CreateSide(placer, placer.transform, origin, right, forward, size.x, size.y + placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);
            CreateSide(placer, placer.transform, origin, forward, -right, size.y, placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);
            CreateSide(placer, placer.transform, origin, forward, right, size.y, size.x + placer.PerimeterPadding, scale, measuredLengths, ref fenceIndex);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void CreateSide(
            ConstructionFencePlacer placer,
            Transform root,
            Vector3 origin,
            Vector3 tangent,
            Vector3 outward,
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

                var center = origin +
                             tangent * (cursor + fenceLength * 0.5f) +
                             outward * outwardOffset;

                var rotation = Quaternion.LookRotation(outward, Vector3.up);
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

        private static Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
        }
    }
#endif
}
