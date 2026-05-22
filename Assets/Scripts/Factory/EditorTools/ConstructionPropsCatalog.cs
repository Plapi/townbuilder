using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ConstructionPropsCatalog", menuName = "Scriptable Objects/Construction Props Catalog")]
    public class ConstructionPropsCatalog : ScriptableObject
    {
        [SerializeField] private List<GameObject> _props = new List<GameObject>();
        [SerializeField] private List<Material> _materials = new List<Material>();

        public IReadOnlyList<GameObject> Props => _props;
        public IReadOnlyList<Material> Materials => _materials;
    }
}
