using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IPoolableObject
{
    [SerializeField] private string _id;
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    
    public string Id => _id;
    public MonoBehaviour Behaviour => this;
    public Vector2Int Size => _size;
    public Vector2Int GridPos { get; protected set; }
    public List<Vector2Int> GridPositions { get; private set; } 
    public Vector2Int Forward
    {
        get
        {
            Vector3 f = transform.forward;
            return new Vector2Int(Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.z));
        }
    }
    public Vector2Int Right
    {
        get
        {
            Vector3 r = transform.right;
            return new Vector2Int(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.z));
        }
    }
    
    protected virtual void Awake()
    {
        GridPos = Utils.WorldToGrid(transform.position);
        GridPositions = new List<Vector2Int>();
    }
    
    public virtual void ApplyCorrectPlacement(bool hasCorrectPlacement) { }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        var backLeft = transform.position;
        var forwardLeft = backLeft + transform.forward * _size.y;
        var backRight = backLeft + transform.right * _size.x;
        var forwardRight = forwardLeft + transform.right * _size.x;
        
        Gizmos.DrawLine(backLeft, forwardLeft);
        Gizmos.DrawLine(forwardLeft, forwardRight);
        Gizmos.DrawLine(backLeft, backRight);
        Gizmos.DrawLine(backRight, forwardRight);
    }
}
