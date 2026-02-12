using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConveyorBeltMesh : MonoBehaviour {
    
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private ConveyorType _conveyorType;
    [SerializeField] private ConveyorDirection _direction;
    [SerializeField] private Object _targetFolder;
    
    public void UpdateMesh(ConveyorType conveyorType, ConveyorDirection direction)
    {
        if (_meshFilter.sharedMesh == null)
            _meshFilter.sharedMesh = new Mesh();
        
        var mesh = _meshFilter.sharedMesh;

        if (conveyorType == ConveyorType.Straight)
        {
            mesh.SetVertices(new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
            });
            mesh.SetTriangles(new int[]
            {
                0, 3, 1, 
                0, 2, 3
            }, 0);
            
            if (direction == ConveyorDirection.Front)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                };    
            }
            else if (direction == ConveyorDirection.Back)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(1, 1),
                    new Vector2(0, 1),
                    new Vector2(1, 0),
                    new Vector2(0, 0)
                };  
            }
            else if (direction == ConveyorDirection.Left)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                    new Vector2(1, 1),
                    new Vector2(1, 0)
                };
            }
            else if (direction == ConveyorDirection.Right)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 0),
                    new Vector2(0, 1)
                };
            }
        }
        else
        {
            if (direction == ConveyorDirection.Front)
                direction = ConveyorDirection.Right;
            else if (direction == ConveyorDirection.Back)
                direction = ConveyorDirection.Left;
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            if (direction == ConveyorDirection.Right)
            {
                vertices.Add(new Vector3(1, 0, 0));
                vertices.AddRange(Bezier.GetPoints(
                    new List<Vector3>()
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 1),
                        new Vector3(1, 0, 1)
                    }, 4));
                triangles = new List<int>()
                {
                    0, 1, 2,
                    0, 2, 3,
                    0, 3, 4,
                    0, 4, 5
                };

            } else if (direction == ConveyorDirection.Left)
            {
                vertices.Add(new Vector3(0, 0, 0));
                vertices.AddRange(Bezier.GetPoints(
                    new List<Vector3>()
                    {
                        new Vector3(1, 0, 0),
                        new Vector3(1, 0, 1),
                        new Vector3(0, 0, 1)
                    }, 4));
                triangles = new List<int>()
                {
                    0, 2, 1,
                    0, 3, 2,
                    0, 4, 3,
                    0, 5, 4
                };
            }
            
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            mesh.uv = new Vector2[]
            {
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0.4f),
                new Vector2(0f, 0.8f),
                new Vector2(0f, 1.2f),
                new Vector2(1f, 1.6f),
            };
        }
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    public void UpdateMesh()
    {
        UpdateMesh(_conveyorType, _direction);
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Create")]
    private void Create()
    {
        var folderPath = AssetDatabase.GetAssetPath(_targetFolder);
        
        var meshPath = $"{folderPath}/Mesh.asset";
        AssetDatabase.CreateAsset(_meshFilter.sharedMesh, meshPath);

        var obj = Instantiate(gameObject);
        DestroyImmediate(obj.GetComponent<ConveyorBeltMesh>());
        obj.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        
        PrefabUtility.SaveAsPrefabAsset(obj, $"{folderPath}/{_targetFolder.name}.prefab");
    }
    
    public void OnSceneGUI()
    {
        if (_meshFilter == null || _meshFilter.sharedMesh == null)
            return;
        
        var mesh = _meshFilter.sharedMesh;
        var vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            Handles.Label(transform.position + vertices[i], i.ToString());
    }
    #endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(ConveyorBeltMesh))]
public class ConveyorBeltMeshEditor : Editor
{
    private ConveyorBeltMesh _target;

    private void OnEnable()
    {
        _target = (ConveyorBeltMesh)target;
    }

    private void OnSceneGUI()
    {
        if (Application.isPlaying == false)
            _target.UpdateMesh();

        _target.OnSceneGUI();
    }
}

#endif
