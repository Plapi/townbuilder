using UnityEngine;

public abstract class FactoryEntity : MonoBehaviour, IFactoryEntity, IPoolableObject
{
    [SerializeField] private string _id;
    [SerializeField] private Vector2Int _size = Vector2Int.one;
    
    public string Id => _id;
    public MonoBehaviour Behaviour => this;
    public Vector2Int GridPos { get; private set; }
    
    public void Place(Vector3 worldPos)
    {
        Place(Utils.WorldToGrid(worldPos));
    }
    
    public void PlaceOnCenter(Vector3 worldPos)
    {
        Vector3 offset = new Vector3(_size.x * 0.5f, 0f, _size.y * 0.5f);
        Vector3 backLeftPos = worldPos - offset;
        Place(Utils.WorldToGrid(backLeftPos));
    }
    
    private void Place(Vector2Int gridPos)
    {
        transform.position = new Vector3(gridPos.x, 0f, gridPos.y);
        GridPos = gridPos;
    }
    
    public void Rotate()
    {
        Vector3 centerBefore = transform.position + 
                               transform.right * (_size.x * 0.5f) + 
                               transform.forward * (_size.y * 0.5f);
        transform.Rotate(0f, 90f, 0f);
        Vector3 centerAfter = transform.position + 
                              transform.right * (_size.x * 0.5f) + 
                              transform.forward * (_size.y * 0.5f);
        transform.position += centerBefore - centerAfter;
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
