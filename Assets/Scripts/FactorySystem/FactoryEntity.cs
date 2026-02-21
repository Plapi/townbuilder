using UnityEngine;

public abstract class FactoryEntity : MonoBehaviour, IFactoryEntity
{
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    
    public Vector2Int GridPos { get; protected set; }
    
    public void Place(Vector3 worldPos)
    {
        // worldPos -= transform.forward * _size.y / 2f + transform.right * _size.x / 2f;
        Place(Utils.WorldToGrid(worldPos));
    }
    
    public void Place(Vector2Int gridPos)
    {
        transform.position = new Vector3(gridPos.x, 0f, gridPos.y);
        GridPos = gridPos;
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
