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
            for (int i = 0; i < placer.PropsCount; i++)
            {
                var propPrefab = availableProps[Random.Range(0, availableProps.Count)];
                var instance = PrefabUtility.InstantiatePrefab(propPrefab, placer.transform) as GameObject;
                if (instance == null)
                    instance = Instantiate(propPrefab, placer.transform);

                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Construction Prop");
                instance.transform.SetPositionAndRotation(GetRandomPosition(construction), GetRotation(placer));
                instance.transform.localScale = Vector3.one * scale;
                instance.name = $"{propPrefab.name}{GENERATED_PROP_NAME_SUFFIX}";
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

        private static Vector3 GetRandomPosition(Construction construction)
        {
            var x = Random.Range(0f, construction.Size.x);
            var z = Random.Range(0f, construction.Size.y);
            return construction.transform.position +
                   ToWorld(construction.Right) * x +
                   ToWorld(construction.Forward) * z;
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
