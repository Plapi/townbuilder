using System.Collections.Generic;
using com.Plapamaru.Singletons;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [CreateAssetMenu(fileName = "FactoryConfig", menuName = "Scriptable Objects/FactoryConfig")]
    public class FactoryConfig : ScriptableObjectSingleton<FactoryConfig>
    {
        public Color correctColor;
        public Color wrongColor;
        public Color previewColor;
        public float constructionPropsScale = 0.4f;
        public List<CrafterRecipeDefinition> crafterRecipes;
    }
}
