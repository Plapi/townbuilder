using com.Plapamaru.Pooling;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceItem : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private ResourceItemType _type;
        
        public string Id => _type.ToString();
        public MonoBehaviour Behaviour => this;
        
        public void OnRelease()
        {
            
        }
    }
}