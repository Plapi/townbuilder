using System.Collections.Generic;
using com.Plapamaru.TownCrafter.Factory;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UICrafterContent : MonoBehaviour
    {
        [SerializeField] private List<UICraftingRecipeItem> _recipeItems;

        public void Init()
        {
            var recipes = FactoryConfig.Instance.crafterRecipes;
            var prefab = _recipeItems[0];
            var parent = prefab.transform.parent;

            while (_recipeItems.Count < recipes.Count)
                _recipeItems.Add(Instantiate(prefab, parent));

            for (var i = 0; i < _recipeItems.Count; i++)
            {
                var hasRecipe = i < recipes.Count;
                _recipeItems[i].gameObject.SetActive(hasRecipe);

                if (hasRecipe)
                    _recipeItems[i].Init(recipes[i]);
            }
        }
    }
}