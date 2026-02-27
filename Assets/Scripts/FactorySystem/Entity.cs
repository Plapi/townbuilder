using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IPoolableObject
{
    [SerializeField] private string _id;
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    [SerializeField] private Vector2Int _gridPos;
    [SerializeField] protected Transform[] _inputs;
    [SerializeField] protected Transform[] _outputs;
    
    public Transform[] Inputs => _inputs;
    public Transform[] Outputs => _outputs;
    
    public string Id => _id;
    public MonoBehaviour Behaviour => this;
    public Vector2Int Size => _size;
    public int AngleY
    {
        get
        {
            var angleY = Mathf.RoundToInt(transform.eulerAngles.y);
            if (angleY == 270)
                angleY = -90;
            return angleY;
        }
    }
    public Vector2Int GridPos
    {
        get => _gridPos;
        private set => _gridPos = value;
    }
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
        GridPos = FactoryUtils.GetGridPos(transform);
        GridPositions = new List<Vector2Int>();
        SetEntityColliders();
    }
    
    public void SnapToGridOnCenter(Vector3 worldPos)
    {
        Vector3 offset = new Vector3(Size.x * 0.5f, 0f, Size.y * 0.5f);
        GridPos = FactoryUtils.WorldToGrid(worldPos - offset, RoundType.Floor);
        FactoryUtils.PlaceToGrid(this);
    }
    
    public void SnapToGrid(Vector2Int gridPos)
    {
        GridPos = gridPos;
        FactoryUtils.PlaceToGrid(this);
    }
    
    public void Rotate()
    {
        transform.Rotate(0f, 90f, 0f);
        
        Vector2Int offset = Vector2Int.zero;
        var angleY = AngleY;
        if (Size.x > 1 || Size.y > 1)
        {
            if (angleY == 90)
                offset = new Vector2Int(-1, 1);
            else if (angleY == 180)
                offset = new Vector2Int(1, 1);
            else if (angleY == -90)
                offset = new Vector2Int(1, -1);
            else
                offset = new Vector2Int(-1, -1);
            GridPos += offset * _size / 2;
        }
        
        FactoryUtils.PlaceToGrid(this);
    }

    private void SetEntityColliders()
    {
        var colliders = transform.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].AddComponent<EntityCollider>().SetEntity(this);
    }
    
    public abstract void ApplyCorrectPlacement(bool hasCorrectPlacement);
    
    public virtual bool HasCorrectPlacement(Dictionary<Vector2Int, Entity> map)
    {
        return Size.x * Size.y == GridPositions.Count;
    }
    
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