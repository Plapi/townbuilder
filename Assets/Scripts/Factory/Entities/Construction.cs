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

#if UNITY_EDITOR
        [Header("Editor")]
        [SerializeField] private UnityEngine.Object _exportFolder;
#endif

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

            TryCompleteConstruction();
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
                    TryCompleteConstruction();
                    UpdateStages();
                    ResourceReceived?.Invoke(resourceType, _resourcesDict[resourceType]);
                }
            }

            await UniTask.NextFrame(cancellationToken);
            return true;
        }

        public override bool CanAcceptIncomingResourceItem(ResourceItem incomingResourceItem = null)
        {
            return base.CanAcceptIncomingResourceItem(incomingResourceItem) &&
                   _state == ConstructionState.InProgress &&
                   incomingResourceItem != null &&
                   NeedsResource(incomingResourceItem.Type);
        }

        public int GetResourceCount(ResourceItemType type)
        {
            return _resourcesDict.GetValueOrDefault(type, 0);
        }

        public float CalculateConstructionProgress()
        {
            var requiredAmount = 0;
            var deliveredAmount = 0;

            foreach (var requiredResource in Data.requiredResources)
            {
                var target = requiredResource.amount;
                if (target <= 0)
                    continue;

                requiredAmount += target;
                deliveredAmount += Mathf.Min(target, GetResourceCount(requiredResource.resourceItem.type));
            }

            if (requiredAmount <= 0)
                return 1f;

            return Mathf.Clamp01((float)deliveredAmount / requiredAmount);
        }

        private bool HasAllRequiredResources()
        {
            foreach (var requiredResource in Data.requiredResources)
            {
                if (GetResourceCount(requiredResource.resourceItem.type) < requiredResource.amount)
                    return false;
            }

            return true;
        }

        public void StartConstruction()
        {
            _state = ConstructionState.InProgress;
            TryCompleteConstruction();
            UpdateStages();
        }

#if UNITY_EDITOR
        public void SetEditorStage(int stageIndex)
        {
            if (_stages.Length == 0)
                return;

            stageIndex = Mathf.Clamp(stageIndex, 0, _stages.Length - 1);

            UnityEditor.Undo.RecordObjects(_stages, "Set Construction Debug Stage");

            foreach (var stage in _stages)
                stage.SetActive(false);

            _stages[stageIndex].SetActive(true);

            foreach (var stage in _stages)
                UnityEditor.EditorUtility.SetDirty(stage);
        }
#endif

        private void UpdateStages()
        {
            if (_stages.Length == 0)
                return;

            var stageIndex = 0;

            if (_state == ConstructionState.InProgress)
            {
                stageIndex = CalculateInProgressStageIndex();
            }
            else if (_state == ConstructionState.Finished)
            {
                stageIndex = _stages.Length - 1;
            }

            foreach (var stage in _stages)
                stage.SetActive(false);
            _stages[stageIndex].SetActive(true);
        }

        private int CalculateInProgressStageIndex()
        {
            if (_stages.Length <= 2)
                return Mathf.Min(1, _stages.Length - 1);

            var inProgressStageCount = _stages.Length - 2;
            var progress = CalculateConstructionProgress();
            var stageIndex = 1 + Mathf.FloorToInt(progress * inProgressStageCount);

            return Mathf.Clamp(stageIndex, 1, _stages.Length - 2);
        }

        private bool NeedsResource(ResourceItemType type)
        {
            if (!TryGetRequiredResourceTarget(type, out var target))
                return false;

            return GetResourceCount(type) < target;
        }

        private void TryCompleteConstruction()
        {
            if (_state != ConstructionState.InProgress)
                return;

            if (!HasAllRequiredResources())
                return;

            _state = ConstructionState.Finished;
            UpdateStages();
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
        InProgress,
        Finished
    }
}
