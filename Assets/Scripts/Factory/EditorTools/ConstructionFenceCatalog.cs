using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ConstructionFenceCatalog", menuName = "Scriptable Objects/Construction Fence Catalog")]
    public class ConstructionFenceCatalog : ScriptableObject
    {
        [SerializeField] private List<GameObject> _fences = new List<GameObject>();

        public IReadOnlyList<GameObject> Fences => _fences;
    }
}
