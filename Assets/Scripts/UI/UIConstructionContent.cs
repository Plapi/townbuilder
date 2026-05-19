using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.Utilities;

namespace com.Plapamaru.TownCrafter.UI
{
    public class UIConstructionContent : MonoBehaviour
    {
        [SerializeField] private List<UIResourceItem> _requireResourceItems;

        [Space]
        [SerializeField] private Button _startConstructionButton;
        [SerializeField] private Slider _constructionProgressSlider;
        [SerializeField] private TextMeshProUGUI _constructionProgressText;
        [SerializeField] private GameObject _constructionComplete;

        private readonly Dictionary<ResourceItemType, UIResourceItem> _resourceItemsByType = new Dictionary<ResourceItemType, UIResourceItem>();
        private readonly Dictionary<ResourceItemType, ResourceItemData> _resourceDataByType = new Dictionary<ResourceItemType, ResourceItemData>();
        private readonly Dictionary<ResourceItemType, int> _resourceTargetsByType = new Dictionary<ResourceItemType, int>();

        private Construction _construction;

        public void Init(Construction construction, UnityAction onStartConstruction)
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

            _startConstructionButton.gameObject.SetActive(false);
            _constructionProgressSlider.gameObject.SetActive(false);
            _constructionComplete.SetActive(false);

            if (_construction.State == ConstructionState.NotStarted)
            {
                _startConstructionButton.gameObject.SetActive(true);
                _startConstructionButton.SetExclusiveListener(onStartConstruction);
            }
            else if (_construction.State == ConstructionState.InProgress)
            {
                _constructionProgressSlider.gameObject.SetActive(true);
                UpdateConstructionProgress();
            }
            else if (_construction.State == ConstructionState.Finished)
            {
                _constructionComplete.SetActive(true);
            }

            ResetRequiredResourcesScroll();
        }

        private void OnEnable()
        {
            ResetRequiredResourcesScroll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResetRequiredResourcesScroll()
        {
            if (_requireResourceItems.Count == 0)
                return;

            var contentRect = _requireResourceItems[0].transform.parent as RectTransform;
            if (contentRect == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            var scrollRect = contentRect.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
                return;

            scrollRect.StopMovement();
            scrollRect.velocity = Vector2.zero;
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        private void OnConstructionResourceReceived(ResourceItemType resourceType, int amount)
        {
            if (_resourceItemsByType.TryGetValue(resourceType, out var resourceItem))
            {
                var target = _resourceTargetsByType[resourceType];
                resourceItem.Init(_resourceDataByType[resourceType], Mathf.Min(target, amount), target);
            }

            UpdateConstructionProgress();
        }

        private void UpdateConstructionProgress()
        {
            var progress = _construction.CalculateConstructionProgress();

            _constructionProgressSlider.value = progress;
            _constructionProgressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void Unsubscribe()
        {
            if (_construction != null)
                _construction.ResourceReceived -= OnConstructionResourceReceived;

            _construction = null;
        }
    }
}
