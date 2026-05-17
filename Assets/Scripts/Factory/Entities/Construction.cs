using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Construction : FactoryEntity<EntityData, EntitySaveData>
    {
        public Action<ResourceItemType, int> ResourceReceived;

        [Space]
        [SerializeField] private GameObject[] _stages;

        [Header("Runtime Properties")]
        [SerializeField] private Conveyor _connectedConveyor;

        private readonly Dictionary<ResourceItemType, int> _resourcesDict = new Dictionary<ResourceItemType, int>();

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            if (_resourceItem != null)
            {
                var resourceType = _resourceItem.Type;
                var canStoreResource = NeedsResource(resourceType);

                ObjectPoolingSystem.Instance.ReleaseObject(_resourceItem);
                _resourceItem = null;

                if (canStoreResource)
                {
                    _resourcesDict.TryAdd(resourceType, 0);
                    _resourcesDict[resourceType]++;
                    ResourceReceived?.Invoke(resourceType, _resourcesDict[resourceType]);
                }
            }

            await UniTask.NextFrame(cancellationToken);
            return true;
        }

        public override bool CanAcceptIncomingResourceItem(ResourceItem incomingResourceItem = null)
        {
            return base.CanAcceptIncomingResourceItem(incomingResourceItem) &&
                   incomingResourceItem != null &&
                   NeedsResource(incomingResourceItem.Type);
        }

        public int GetResourceCount(ResourceItemType type)
        {
            return _resourcesDict.GetValueOrDefault(type, 0);
        }

        private bool NeedsResource(ResourceItemType type)
        {
            if (!TryGetRequiredResourceTarget(type, out var target))
                return false;

            return GetResourceCount(type) < target;
        }

        private bool TryGetRequiredResourceTarget(ResourceItemType type, out int target)
        {
            target = 0;

            if (Data is not ConstructionData constructionData)
                return false;

            foreach (var requiredResource in constructionData.requiredResources)
            {
                if (requiredResource.resourceItem.type == type)
                {
                    target = requiredResource.amount;
                    return true;
                }
            }


            return false;
        }

        public override void OnDispose()
        {
            ResourceReceived = null;
            base.OnDispose();
        }
    }

    public enum ConstructionState
    {
        NotStarted,
        Started,
        Finished
    }
}