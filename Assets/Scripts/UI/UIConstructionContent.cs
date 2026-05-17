using System.Collections.Generic;
using com.Plapamaru.TownCrafter.Factory;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIConstructionContent : MonoBehaviour
    {
        [SerializeField] private List<UIResourceItem> _requireResourceItems;

        public void Init(Construction construction)
        {
            var constructionData = (ConstructionData)construction.Data;
            var resources = constructionData.requiredResources;
            var prefab = _requireResourceItems[0];
            var parent = prefab.transform.parent;

            while (_requireResourceItems.Count < resources.Count)
                _requireResourceItems.Add(Instantiate(prefab, parent));

            for (var i = 0; i < _requireResourceItems.Count; i++)
            {
                var hasRecipe = i < resources.Count;
                _requireResourceItems[i].gameObject.SetActive(hasRecipe);

                if (hasRecipe)
                    _requireResourceItems[i].Init(resources[i].resourceItem, 0, resources[i].amount);
            }
        }
    }
}