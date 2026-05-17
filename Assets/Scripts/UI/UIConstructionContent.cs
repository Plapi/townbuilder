using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIConstructionContent : MonoBehaviour
    {
        [SerializeField] private List<UIResourceItem> _requireResourceItems;

        public void Init(Construction construction)
        {
            var constructionData = (ConstructionData)construction.Data;
            var requiredResoures = constructionData.requiredResources;
            var prefab = _requireResourceItems[0];
            var parent = prefab.transform.parent;

            while (_requireResourceItems.Count < requiredResoures.Count)
                _requireResourceItems.Add(Instantiate(prefab, parent));

            for (var i = 0; i < _requireResourceItems.Count; i++)
            {
                var hasRecipe = i < requiredResoures.Count;
                _requireResourceItems[i].gameObject.SetActive(hasRecipe);

                var resourceItem = requiredResoures[i].resourceItem;
                var target = requiredResoures[i].amount;
                var amount = Mathf.Min(target, construction.GetResourceCount(resourceItem.type));

                if (hasRecipe)
                    _requireResourceItems[i].Init(resourceItem, amount, requiredResoures[i].amount);
            }

            UniTask.Action(async ct =>
            {
                await UniTask.WaitForEndOfFrame(ct);
                var rect = parent.GetComponent<RectTransform>();
                rect.SetAnchorPosX(rect.sizeDelta.x / 2f);
            }, CancellationToken.None).Invoke();
        }
    }
}