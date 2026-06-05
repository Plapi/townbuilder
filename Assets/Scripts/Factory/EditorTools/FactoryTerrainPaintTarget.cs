using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory.EditorTools
{
    [DisallowMultipleComponent]
    public class FactoryTerrainPaintTarget : MonoBehaviour
    {
        [SerializeField] private bool _canBePainted = true;

        public bool CanBePainted => _canBePainted;
    }
}
