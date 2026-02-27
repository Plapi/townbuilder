using UnityEngine;

public class EntityCollider : MonoBehaviour
{
    [SerializeField] private Entity _entity;
    
    public Entity Entity => _entity;
    
    public void SetEntity(Entity entity)
    {
        _entity = entity;
    }
}
