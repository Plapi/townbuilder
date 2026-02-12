#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshCreator : MonoBehaviour
{
    [Header("Mesh Components")]
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private bool _autoUpdate = true;
    
    [Header("Save Obj")]
    [SerializeField] private Object _objFolder;
    [SerializeField] private string _objName;
    
    [Header("Mesh Data")]
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _material;
    [SerializeField] private List<Vector3> _vertices;
    [SerializeField] private List<Vector3> _triangles;
    
    [Header("Source")]
    [SerializeField] private MeshFilter _sourceMeshFilter;
    
    private readonly List<int> _trianglesInt = new List<int>();
    
    private void OnValidate()
    {
        if (!_autoUpdate) 
            return;
        
        UpdateMesh();
    }
    
    public void UpdateMesh()
    {
        if (_mesh == null)
            _mesh = new Mesh { name = "GeneratedMesh" };
        
        if (_meshFilter == null && TryGetComponent(out _meshFilter) == false)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        
        if (_meshRenderer == null && TryGetComponent(out _meshRenderer) == false)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        if (_vertices == null || _vertices.Count < 3 || _triangles == null || _triangles.Count <= 0)
        {
            _mesh.Clear();
            return;
        }
        
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        
        _trianglesInt.Clear();
        foreach (var triangle in _triangles)
        {
            _trianglesInt.Add((int)triangle.x);
            _trianglesInt.Add((int)triangle.y);
            _trianglesInt.Add((int)triangle.z);
        }
        
        _mesh.SetTriangles(_trianglesInt, 0);
        
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        
        _meshFilter.sharedMesh = _mesh;
        _meshRenderer.sharedMaterial = _material;
    }
    
    public void OnSceneGUI()
    {
        if (_vertices == null)
            return;
        
        for (int i = 0; i < _vertices.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            
            Vector3 worldPos = transform.TransformPoint(_vertices[i]);
            
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gameObject, "Move Vertex");
                
                _vertices[i] = transform.InverseTransformPoint(newWorldPos);
                
                UpdateMesh();
                EditorUtility.SetDirty(this);
            }
            
            Handles.Label(newWorldPos, i.ToString());
        }
    }

    public void CreateFromSource()
    {
        if (_sourceMeshFilter == null)
            return;
        
        var sourceMesh = _sourceMeshFilter.sharedMesh;
        
        _vertices = new List<Vector3>(sourceMesh.vertices);
        
        _triangles = new List<Vector3>();
        var triangles = sourceMesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
            _triangles.Add(new Vector3(triangles[i], triangles[i + 1], triangles[i + 2]));
        
        UpdateMesh();
    }
    
    public void SaveObj()
    {
        var folderPath = AssetDatabase.GetAssetPath(_objFolder);
        
        var meshPath = $"{folderPath}/{_objName}.asset";
        AssetDatabase.CreateAsset(_meshFilter.sharedMesh, meshPath);
        
        var obj = Instantiate(gameObject);
        
        DestroyImmediate(obj.GetComponent<MeshCreator>());
        foreach (Transform child in obj.transform)
            DestroyImmediate(child.gameObject);
        
        obj.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;
        
        var prefabPath = $"{folderPath}/{_objName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        DestroyImmediate(obj);

        _mesh = null;
        UpdateMesh();
        
        AssetDatabase.Refresh();
    }
}

[CustomEditor(typeof(MeshCreator))]
public class MeshCreatorEditor : Editor
{
    private MeshCreator _meshCreator;
    
    private void OnEnable()
    {
        _meshCreator = (MeshCreator)target;
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("Update Mesh"))
            _meshCreator.UpdateMesh();
        
        if (GUILayout.Button("Create From Source"))
            _meshCreator.CreateFromSource();
        
        if (GUILayout.Button("Save Obj"))
            _meshCreator.SaveObj();
    }
    
    private void OnSceneGUI()
    {
        _meshCreator.OnSceneGUI();
    }
}
#endif