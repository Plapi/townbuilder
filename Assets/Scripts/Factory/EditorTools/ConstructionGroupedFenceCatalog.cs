using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ConstructionGroupedFenceCatalog", menuName = "Scriptable Objects/Construction Grouped Fence Catalog")]
    public class ConstructionGroupedFenceCatalog : ScriptableObject
    {
        [SerializeField] private List<ConstructionGroupedFence> _groups = new List<ConstructionGroupedFence>();

        public IReadOnlyList<ConstructionGroupedFence> Groups => _groups;
    }

    [Serializable]
    public class ConstructionGroupedFence
    {
        [SerializeField] private string _name;
        [SerializeField] private GameObject _post;
        [SerializeField] private GameObject _fence;

        public string Name => string.IsNullOrEmpty(_name) ? ResolveDisplayName() : _name;
        public GameObject Post => _post;
        public GameObject Fence => _fence;
        public bool IsValid => _post != null && _fence != null;

        private string ResolveDisplayName()
        {
            if (_post != null && _fence != null)
                return $"{_post.name} + {_fence.name}";

            if (_post != null)
                return _post.name;

            if (_fence != null)
                return _fence.name;

            return "Grouped Fence";
        }
    }
}
