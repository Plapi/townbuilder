using System.Collections.Generic;
using com.Plapamaru.TownCrafter.Factory;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UICraftingRecipeItem : MonoBehaviour
    {
        [SerializeField] private List<UIResourceItem> _inputResourceItems;
        [SerializeField] private UIResourceItem _outputResourceItem;

        public void Init(CrafterRecipeDefinition recipe)
        {
            for (var i = 0; i < recipe.inputs.Count && i < _inputResourceItems.Count; i++)
                _inputResourceItems[i].Init(recipe.inputs[i].resourceItem, recipe.inputs[i].amount);

            _outputResourceItem.Init(recipe.output);
        }
    }
}