using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIResourceNodeContent : MonoBehaviour
    {
        [SerializeField] private UIResourceItem _resourceItem;

        public UIResourceItem ResourceItem => _resourceItem;
    }
}