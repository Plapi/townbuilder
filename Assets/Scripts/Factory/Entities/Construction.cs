using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Construction : FactoryEntity<ConstructionData, ConstructionSaveData>
    {
        public Action<ResourceItemType, int> ResourceReceived;

        [Space]
        [SerializeField] private GameObject[] _stages;

        [Header("Runtime Properties")]
        [SerializeField] private Conveyor _connectedConveyor;
        [SerializeField] private ConstructionState _state;

        private Dictionary<ResourceItemType, int> _resourcesDict = new Dictionary<ResourceItemType, int>();

        public ConstructionState State => _state;

        public override ConstructionSaveData ToSaveData()
        {
            var saveData = base.ToSaveData();
            saveData.resource = null; // no need to save the resource
            saveData.state = _state;
            saveData.resources = _resourcesDict;
            return saveData;
        }

        protected override void OnInit()
        {
            base.OnInit();

            if (_saveData != null)
            {
                _state = _saveData.state;
                _resourcesDict = _saveData.resources;
            }

            UpdateStages();
        }

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
                   _state == ConstructionState.Started &&
                   incomingResourceItem != null &&
                   NeedsResource(incomingResourceItem.Type);
        }

        public int GetResourceCount(ResourceItemType type)
        {
            return _resourcesDict.GetValueOrDefault(type, 0);
        }

        public void StartConstruction()
        {
            _state = ConstructionState.Started;
            UpdateStages();
        }

        private void UpdateStages()
        {
            var stageIndex = 0;

            if (_state == ConstructionState.Started)
            {
                stageIndex = 1;
            }
            else
            {
                stageIndex = _stages.Length - 1;
            }

            foreach (var stage in _stages)
                stage.SetActive(false);
            _stages[stageIndex].SetActive(true);
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

            foreach (var requiredResource in Data.requiredResources)
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