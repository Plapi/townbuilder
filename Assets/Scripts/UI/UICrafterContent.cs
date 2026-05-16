using System.Collections.Generic;
using com.Plapamaru.TownCrafter.Factory;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UICrafterContent : MonoBehaviour
    {
        [SerializeField] private List<UICraftingRecipeItem> recipeItems;

        public void Init()
        {
            var recipes = FactoryConfig.Instance.crafterRecipes;
            var prefab = recipeItems[0];
            var parent = prefab.transform.parent;

            while (recipeItems.Count < recipes.Count)
                recipeItems.Add(Instantiate(prefab, parent));

            for (var i = 0; i < recipeItems.Count; i++)
            {
                var hasRecipe = i < recipes.Count;
                recipeItems[i].gameObject.SetActive(hasRecipe);

                if (hasRecipe)
                    recipeItems[i].Init(recipes[i]);
            }
        }
    }
}