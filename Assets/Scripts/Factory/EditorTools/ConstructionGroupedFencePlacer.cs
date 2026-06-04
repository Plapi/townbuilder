using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace com.Plapamaru.TownCrafter.Factory
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("TownCrafter/Editor/Construction Grouped Fence Placer")]
    public class ConstructionGroupedFencePlacer : MonoBehaviour
    {
        [SerializeField] private Construction _construction;
        [SerializeField] private ConstructionGroupedFenceCatalog _fenceCatalog;
        [SerializeField] private int _selectedGroupIndex;
        [SerializeField] private bool _useCustomSize;
        [SerializeField] private Vector2Int _customSize = Vector2Int.one;
        [SerializeField] private float _perimeterPadding;
        [SerializeField] private bool _placeFront = true;
        [SerializeField] private bool _placeBack = true;
        [SerializeField] private bool _placeRight = true;
        [SerializeField] private bool _placeLeft = true;

        public Construction Construction => _construction;
        public ConstructionGroupedFenceCatalog FenceCatalog => _fenceCatalog;
        public int SelectedGroupIndex => Mathf.Max(0, _selectedGroupIndex);
        public bool UseCustomSize => _useCustomSize;
        public Vector2Int Size => _useCustomSize ? ClampSize(_customSize) : (_construction != null ? _construction.Size : Vector2Int.one);
        public float PerimeterPadding => Mathf.Max(0f, _perimeterPadding);
        public bool PlaceFront => _placeFront;
        public bool PlaceBack => _placeBack;
        public bool PlaceRight => _placeRight;
        public bool PlaceLeft => _placeLeft;

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
            _selectedGroupIndex = Mathf.Max(0, _selectedGroupIndex);
        }

        private void TryAssignConstructionFromParents()
        {
            _construction = GetComponentInParent<Construction>();
        }

        private void TryAssignFenceCatalog()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:ConstructionGroupedFenceCatalog");
            if (guids.Length == 0)
                return;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _fenceCatalog = AssetDatabase.LoadAssetAtPath<ConstructionGroupedFenceCatalog>(path);
#endif
        }

        private static Vector2Int ClampSize(Vector2Int size)
        {
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionGroupedFencePlacer))]
    public class ConstructionGroupedFencePlacerEditor : Editor
    {
        private const string GENERATED_NAME_SUFFIX = " (Generated Grouped Fence)";

        private SerializedProperty _constructionProperty;
        private SerializedProperty _fenceCatalogProperty;
        private SerializedProperty _selectedGroupIndexProperty;
        private SerializedProperty _useCustomSizeProperty;
        private SerializedProperty _customSizeProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _fenceCatalogProperty = serializedObject.FindProperty("_fenceCatalog");
            _selectedGroupIndexProperty = serializedObject.FindProperty("_selectedGroupIndex");
            _useCustomSizeProperty = serializedObject.FindProperty("_useCustomSize");
            _customSizeProperty = serializedObject.FindProperty("_customSize");

            TryAssignDefaultCatalog();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "_selectedGroupIndex", "_useCustomSize", "_customSize");
            DrawSizeOverride();
            DrawFenceGroupSelector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Instantiate Grouped Fence"))
                    GenerateFence();
            }

            if (GUILayout.Button("Clear Generated Grouped Fence"))
                ClearGeneratedFence();
        }

        private void DrawSizeOverride()
        {
            EditorGUILayout.PropertyField(_useCustomSizeProperty, new GUIContent("Use Custom Size"));

            using (new EditorGUI.DisabledScope(!_useCustomSizeProperty.boolValue))
            {
                EditorGUILayout.PropertyField(_customSizeProperty, new GUIContent("Custom Size"));
            }
        }

        private void DrawFenceGroupSelector()
        {
            var catalog = _fenceCatalogProperty.objectReferenceValue as ConstructionGroupedFenceCatalog;
            if (catalog == null || catalog.Groups.Count == 0)
                return;

            var names = new string[catalog.Groups.Count];
            for (int i = 0; i < names.Length; i++)
            {
                var group = catalog.Groups[i];
                names[i] = group != null ? group.Name : "Missing Group";
            }

            _selectedGroupIndexProperty.intValue = Mathf.Clamp(_selectedGroupIndexProperty.intValue, 0, names.Length - 1);

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            _selectedGroupIndexProperty.intValue = EditorGUILayout.Popup("Fence Group", _selectedGroupIndexProperty.intValue, names);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                RegenerateFenceIfPossible();
                serializedObject.Update();
            }
        }

        private void TryAssignDefaultCatalog()
        {
            serializedObject.Update();
            if (_fenceCatalogProperty.objectReferenceValue != null)
                return;

            var guids = AssetDatabase.FindAssets("t:ConstructionGroupedFenceCatalog");
            if (guids.Length == 0)
                return;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _fenceCatalogProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConstructionGroupedFenceCatalog>(path);
            serializedObject.ApplyModifiedProperties();
        }

        private bool CanGenerate()
        {
            if (!_useCustomSizeProperty.boolValue && _constructionProperty.objectReferenceValue == null)
                return false;

            var placer = (ConstructionGroupedFencePlacer)target;
            var group = GetSelectedGroup(placer);
            return group != null && group.IsValid &&
                   (placer.PlaceFront || placer.PlaceBack || placer.PlaceRight || placer.PlaceLeft);
        }

        private void GenerateFence()
        {
            var placer = (ConstructionGroupedFencePlacer)target;
            var construction = placer.Construction;
            if (!placer.UseCustomSize && construction == null)
                return;

            var group = GetSelectedGroup(placer);
            if (group == null || !group.IsValid)
                return;

            ClearGeneratedFence(placer.transform);

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var size = placer.Size;
            var origin = placer.UseCustomSize ? placer.transform.position : construction.transform.position;
            var right = placer.UseCustomSize ? FlattenDirection(placer.transform.right) : ToWorld(construction.Right);
            var forward = placer.UseCustomSize ? FlattenDirection(placer.transform.forward) : ToWorld(construction.Forward);
            var placedPosts = new System.Collections.Generic.HashSet<Vector2Int>();

            if (placer.PlaceBack)
                CreateSide(placer.transform, "Back", group, origin, right, -forward, size.x, placer.PerimeterPadding, 0f, scale, placedPosts);

            if (placer.PlaceFront)
                CreateSide(placer.transform, "Front", group, origin, right, forward, size.x, size.y + placer.PerimeterPadding, 1f, scale, placedPosts);

            if (placer.PlaceLeft)
                CreateSide(placer.transform, "Left", group, origin, forward, -right, size.y, placer.PerimeterPadding, 1f, scale, placedPosts);

            if (placer.PlaceRight)
                CreateSide(placer.transform, "Right", group, origin, forward, right, size.y, size.x + placer.PerimeterPadding, 0f, scale, placedPosts);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private void RegenerateFenceIfPossible()
        {
            if (!CanGenerate())
                return;

            GenerateFence();
        }

        private static void CreateSide(
            Transform root,
            string sideName,
            ConstructionGroupedFence group,
            Vector3 origin,
            Vector3 tangent,
            Vector3 outward,
            int tileCount,
            float outwardOffset,
            float fenceLocalZOffset,
            float scale,
            System.Collections.Generic.ISet<Vector2Int> placedPosts)
        {
            var sideRotation = Quaternion.LookRotation(tangent, Vector3.up);
            var childLocalRotation = Quaternion.Inverse(sideRotation) * Quaternion.LookRotation(outward, Vector3.up);
            var sideRoot = new GameObject($"{sideName}{GENERATED_NAME_SUFFIX}");
            Undo.RegisterCreatedObjectUndo(sideRoot, "Instantiate Construction Grouped Fence");
            sideRoot.transform.SetParent(root, false);
            sideRoot.transform.SetPositionAndRotation(origin + outward * outwardOffset, sideRotation);

            for (int i = 0; i <= tileCount; i++)
            {
                var localPostPosition = Vector3.forward * i;
                CreatePostIfNeeded(sideRoot.transform, group.Post, localPostPosition, childLocalRotation, scale, placedPosts);
            }

            for (int i = 0; i < tileCount; i++)
            {
                var localFencePosition = Vector3.forward * (i + fenceLocalZOffset);
                var fence = InstantiatePrefab(group.Fence, sideRoot.transform, "Fence");
                fence.transform.localPosition = localFencePosition;
                fence.transform.localRotation = childLocalRotation;
                fence.transform.localScale = Vector3.one * scale;
            }
        }

        private static void CreatePostIfNeeded(
            Transform root,
            GameObject postPrefab,
            Vector3 localPosition,
            Quaternion localRotation,
            float scale,
            System.Collections.Generic.ISet<Vector2Int> placedPosts)
        {
            if (!placedPosts.Add(ToPositionKey(root.TransformPoint(localPosition))))
                return;

            var post = InstantiatePrefab(postPrefab, root, "Post");
            post.transform.localPosition = localPosition;
            post.transform.localRotation = localRotation;
            post.transform.localScale = Vector3.one * scale;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform root, string label)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, root) as GameObject;
            if (instance == null)
                instance = Instantiate(prefab, root);

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Construction Grouped Fence");
            instance.name = $"{label}_{prefab.name}{GENERATED_NAME_SUFFIX}";
            return instance;
        }

        private static Vector2Int ToPositionKey(Vector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x * 1000f), Mathf.RoundToInt(position.z * 1000f));
        }

        private void ClearGeneratedFence()
        {
            var placer = (ConstructionGroupedFencePlacer)target;
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

        private static ConstructionGroupedFence GetSelectedGroup(ConstructionGroupedFencePlacer placer)
        {
            var catalog = placer.FenceCatalog;
            if (catalog == null || catalog.Groups.Count == 0)
                return null;

            var index = Mathf.Clamp(placer.SelectedGroupIndex, 0, catalog.Groups.Count - 1);
            return catalog.Groups[index];
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
