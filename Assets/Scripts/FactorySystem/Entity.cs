using UnityEngine;

public abstract class Entity : MonoBehaviour, IPoolableObject
{
    [SerializeField] private string _id;
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    
    public string Id => _id;
    public MonoBehaviour Behaviour => this;
    public Vector2Int Size => _size;
    
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
