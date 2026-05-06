using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "EntityData", menuName = "Scriptable Objects/Entity Data")]
    public class EntityData : ScriptableObject
    {
        public new string name;
        [TextArea(3, 8)] public string description;
        public Sprite icon;
        public float imageScale = 1f;
    }
}