using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "ResourceItemData", menuName = "Scriptable Objects/Resource Item Data")]
    public class ResourceItemData : ScriptableObject
    {
        public ResourceItemType type;
        public new string name;
        public Sprite icon;
        public float imageScale = 1f;
        public float imageOffsetY;

        public string Id => type.ToString();
    }
}
