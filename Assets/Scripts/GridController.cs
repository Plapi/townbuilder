using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridController : MonoBehaviour
{
    private const float CELL_GRID_SIZE = 1f;
    private const int VERTICES_PER_CORNER = 6;
    
    [SerializeField] private Vector2 _cornerGridSize;
    [SerializeField] private Vector2 _length;
    [SerializeField] private bool _updateMesh;
    [SerializeField] private bool _showVertices;
    
    [SerializeField] private List<Vector3> _vertices = new List<Vector3>();
    
    private void OnValidate()
    {
        if (_updateMesh == false)
            return;
        
        if (TryGetComponent(out MeshFilter meshFilter) == false)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        
        if (meshFilter.sharedMesh == null)
            meshFilter.sharedMesh = new Mesh();
        
        var mesh = meshFilter.sharedMesh;
        mesh.Clear();
        
        _vertices.Clear();
        var triangles = new List<int>();
        
        int vertexOffset = 0;

        for (int y = 0; y < _length.y; y++)
        {
            for (int x = 0; x < _length.x; x++)
            {
                Vector3 offset = new Vector3(
                    x * CELL_GRID_SIZE,
                    0,
                    y * CELL_GRID_SIZE
                );

                AddCell(offset, _vertices, triangles, ref vertexOffset);
            }
        }
        
        mesh.SetVertices(_vertices);
        mesh.SetTriangles(triangles.ToArray(), 0);
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    private void AddCell(Vector3 cellOffset, List<Vector3> vertices, List<int> triangles, ref int vertexOffset)
    {
        var cellVertices = new List<Vector3>();

        cellVertices.AddRange(GetCornerVertices(0, 0, 1, 1));
        cellVertices.AddRange(GetCornerVertices(1, 0, -1, 1));
        cellVertices.AddRange(GetCornerVertices(0, 1, 1, -1));
        cellVertices.AddRange(GetCornerVertices(1, 1, -1, -1));

        for (int i = 0; i < cellVertices.Count; i++)
            vertices.Add(cellVertices[i] + cellOffset);

        AddCornerTriangles(0, false, triangles, vertexOffset);
        AddCornerTriangles(1, true, triangles, vertexOffset);
        AddCornerTriangles(2, true, triangles, vertexOffset);
        AddCornerTriangles(3, false, triangles, vertexOffset);

        vertexOffset += VERTICES_PER_CORNER * 4;
    }
    
    private static void AddCornerTriangles(
        int multiplier,
        bool reverse,
        List<int> triangles,
        int vertexOffset)
    {
        var o = vertexOffset + VERTICES_PER_CORNER * multiplier;

        var t = new[]
        {
            o, o + 1, o + 2,
            o, o + 2, o + 3,
            o + 5, o, o + 3,
            o + 5, o + 3, o + 4
        };

        if (reverse)
            for (int i = 0; i < t.Length; i += 3)
                (t[i], t[i + 2]) = (t[i + 2], t[i]);
        
        triangles.AddRange(t);
    }
    
    private List<Vector3> GetCornerVertices(int startX, int startY, int signX, int signY)
    {
        return new List<Vector3>()
        {
            new Vector3(startX, 0, startY),
            new Vector3(startX, 0, startY + _cornerGridSize.y * signY),
            new Vector3(startX + _cornerGridSize.x * signX, 0, startY + _cornerGridSize.y * signY),
            new Vector3(startX + _cornerGridSize.x * signX, 0, startY + _cornerGridSize.x * signY),
            new Vector3(startX + _cornerGridSize.y * signX, 0, startY + _cornerGridSize.x * signY),
            new Vector3(startX + _cornerGridSize.y * signX, 0, startY),
        };
    }
    
    [ContextMenu("Instantiate Separate Cell")]
    private void InstantiateSeparateCell()
    {
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        
        var vertexOffset = 0;
        AddCell(Vector3.zero, vertices, triangles, ref vertexOffset);
        
        mesh.SetVertices(_vertices);
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        var obj = new GameObject("Cell"); 
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>();
    }
    
    public void OnSceneGUI()
    {
        if (_showVertices == false)
            return;
        for (int i = 0; i < _vertices.Count; i++)
            Handles.Label(_vertices[i], i.ToString());
    }
}

[CustomEditor(typeof(GridController))]
public class GridControllerEditor : Editor
{
    private GridController _target;
    
    private void OnEnable()
    {
        _target = (GridController)target;
    }

    private void OnSceneGUI()
    {
        _target.OnSceneGUI();
    }
}