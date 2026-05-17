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

        private readonly Dictionary<ResourceItemType, UIResourceItem> _resourceItemsByType = new Dictionary<ResourceItemType, UIResourceItem>();
        private readonly Dictionary<ResourceItemType, ResourceItemData> _resourceDataByType = new Dictionary<ResourceItemType, ResourceItemData>();
        private readonly Dictionary<ResourceItemType, int> _resourceTargetsByType = new Dictionary<ResourceItemType, int>();

        private Construction _construction;

        public void Init(Construction construction)
        {
            Unsubscribe();

            _construction = construction;
            var constructionData = construction.Data;
            var requiredResources = constructionData.requiredResources;
            var prefab = _requireResourceItems[0];
            var parent = prefab.transform.parent;

            _resourceItemsByType.Clear();
            _resourceDataByType.Clear();
            _resourceTargetsByType.Clear();

            while (_requireResourceItems.Count < requiredResources.Count)
                _requireResourceItems.Add(Instantiate(prefab, parent));

            for (var i = 0; i < _requireResourceItems.Count; i++)
            {
                var hasRecipe = i < requiredResources.Count;
                _requireResourceItems[i].gameObject.SetActive(hasRecipe);

                if (!hasRecipe)
                    continue;

                var requiredResource = requiredResources[i];
                var resourceItem = requiredResource.resourceItem;
                var target = requiredResource.amount;
                var amount = Mathf.Min(target, construction.GetResourceCount(resourceItem.type));

                _requireResourceItems[i].Init(resourceItem, amount, target);
                _resourceItemsByType[resourceItem.type] = _requireResourceItems[i];
                _resourceDataByType[resourceItem.type] = resourceItem;
                _resourceTargetsByType[resourceItem.type] = target;
            }

            _construction.ResourceReceived += OnConstructionResourceReceived;

            UniTask.Action(async ct =>
            {
                await UniTask.WaitForEndOfFrame(ct);
                var rect = parent.GetComponent<RectTransform>();
                rect.SetAnchorPosX(rect.sizeDelta.x / 2f);
            }, CancellationToken.None).Invoke();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnConstructionResourceReceived(ResourceItemType resourceType, int amount)
        {
            if (!_resourceItemsByType.TryGetValue(resourceType, out var resourceItem))
                return;

            var target = _resourceTargetsByType[resourceType];
            resourceItem.Init(_resourceDataByType[resourceType], Mathf.Min(target, amount), target);
        }

        private void Unsubscribe()
        {
            if (_construction != null)
                _construction.ResourceReceived -= OnConstructionResourceReceived;

            _construction = null;
        }
    }
}
