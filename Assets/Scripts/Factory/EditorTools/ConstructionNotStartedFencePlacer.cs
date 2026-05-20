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
    [AddComponentMenu("TownCrafter/Editor/Construction Not Started Fence Placer")]
    public class ConstructionNotStartedFencePlacer : MonoBehaviour
    {
        [SerializeField] private Construction _construction;
        [SerializeField] private GameObject _stick;
        [SerializeField] private GameObject _rope;
        [SerializeField] private float _heightOffset;
        [SerializeField] private float _ropePositionY = 0.6f;
        [SerializeField] private float _maxRopeLength = 4f;

        public Construction Construction => _construction;
        public GameObject Stick => _stick;
        public GameObject Rope => _rope;
        public float HeightOffset => _heightOffset;
        public float RopePositionY => _ropePositionY;
        public float MaxRopeLength => Mathf.Max(0.01f, _maxRopeLength);

        private void Reset()
        {
            TryAssignConstructionFromParents();
        }

        private void OnValidate()
        {
            if (_construction == null)
                TryAssignConstructionFromParents();
        }

        private void TryAssignConstructionFromParents()
        {
            _construction = GetComponentInParent<Construction>();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionNotStartedFencePlacer))]
    public class ConstructionNotStartedFencePlacerEditor : Editor
    {
        private const string GENERATED_NAME_SUFFIX = " (Generated Not Started Fence)";

        private SerializedProperty _constructionProperty;
        private SerializedProperty _stickProperty;
        private SerializedProperty _ropeProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _stickProperty = serializedObject.FindProperty("_stick");
            _ropeProperty = serializedObject.FindProperty("_rope");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Instantiate Not Started Fence"))
                    GenerateFence();
            }

            if (GUILayout.Button("Clear Generated Not Started Fence"))
                ClearGeneratedFence();
        }

        private bool CanGenerate()
        {
            return _constructionProperty.objectReferenceValue != null &&
                   _stickProperty.objectReferenceValue != null &&
                   _ropeProperty.objectReferenceValue != null;
        }

        private void GenerateFence()
        {
            var placer = (ConstructionNotStartedFencePlacer)target;
            var construction = placer.Construction;
            if (construction == null || placer.Stick == null || placer.Rope == null)
                return;

            ClearGeneratedFence(placer.transform);

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var corners = GetCorners(construction, placer.HeightOffset);

            for (int i = 0; i < corners.Length; i++)
                InstantiateStick(placer, corners[i], scale, i);

            for (int i = 0; i < corners.Length; i++)
            {
                var from = corners[i];
                var to = corners[(i + 1) % corners.Length];
                InstantiateSide(placer, from, to, scale, i);
            }

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static Vector3[] GetCorners(Construction construction, float heightOffset)
        {
            var origin = construction.transform.position + Vector3.up * heightOffset;
            var right = ToWorld(construction.Right) * construction.Size.x;
            var forward = ToWorld(construction.Forward) * construction.Size.y;

            return new[]
            {
                origin,
                origin + right,
                origin + right + forward,
                origin + forward
            };
        }

        private static void InstantiateStick(ConstructionNotStartedFencePlacer placer, Vector3 position, float scale, int index)
        {
            var instance = PrefabUtility.InstantiatePrefab(placer.Stick, placer.transform) as GameObject;
            if (instance == null)
                instance = Instantiate(placer.Stick, placer.transform);

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Construction Stick");
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.transform.localScale = Vector3.one * scale;
            instance.name = $"{placer.Stick.name} Corner {index}{GENERATED_NAME_SUFFIX}";
        }

        private static void InstantiateSide(ConstructionNotStartedFencePlacer placer, Vector3 from, Vector3 to, float scale, int sideIndex)
        {
            var sideLength = Vector3.Distance(from, to);
            var segmentCount = Mathf.Max(1, Mathf.CeilToInt(sideLength / placer.MaxRopeLength));
            var previousStickPosition = from;

            for (int segmentIndex = 1; segmentIndex <= segmentCount; segmentIndex++)
            {
                var nextStickPosition = Vector3.Lerp(from, to, (float)segmentIndex / segmentCount);

                if (segmentIndex < segmentCount)
                    InstantiateStick(placer, nextStickPosition, scale, sideIndex * 100 + segmentIndex);

                var ropeFrom = previousStickPosition;
                var ropeTo = nextStickPosition;
                ropeFrom.y = placer.RopePositionY;
                ropeTo.y = placer.RopePositionY;
                InstantiateRope(placer, ropeFrom, ropeTo, scale, sideIndex, segmentIndex - 1);

                previousStickPosition = nextStickPosition;
            }
        }

        private static void InstantiateRope(ConstructionNotStartedFencePlacer placer, Vector3 from, Vector3 to, float scale, int sideIndex, int segmentIndex)
        {
            var instance = PrefabUtility.InstantiatePrefab(placer.Rope, placer.transform) as GameObject;
            if (instance == null)
                instance = Instantiate(placer.Rope, placer.transform);

            var direction = to - from;
            var length = direction.magnitude;
            var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var ropeBaseLength = Mathf.Max(0.01f, MeasureLocalLengthZ(placer.Rope));
            var ropeScale = new Vector3(scale, scale, length / ropeBaseLength);

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Construction Rope");
            instance.transform.SetPositionAndRotation(Vector3.Lerp(from, to, 0.5f), rotation);
            instance.transform.localScale = ropeScale;
            instance.name = $"{placer.Rope.name} Side {sideIndex} Segment {segmentIndex}{GENERATED_NAME_SUFFIX}";
        }

        private void ClearGeneratedFence()
        {
            var placer = (ConstructionNotStartedFencePlacer)target;
            ClearGeneratedFence(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void ClearGeneratedFence(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static float MeasureLocalLengthZ(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
                return 1f;

            var rootToLocal = prefab.transform.worldToLocalMatrix;
            var hasBounds = false;
            var bounds = new Bounds();

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
            }

            return hasBounds ? bounds.size.z : 1f;
        }

        private static Vector3 ToWorld(Vector2Int gridDirection)
        {
            return new Vector3(gridDirection.x, 0f, gridDirection.y);
        }
    }
#endif
}
