using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Extractor : FactoryEntity<ExtractorSaveData>
    {
        [Space]
        [SerializeField] private Animator[] _animators;
        [SerializeField] private Transform _resourceOutput;
        [SerializeField] private Transform _resourceItemLocator;
        [SerializeField] private float _extractTime;

        [Header("Runtime Properties")]
        [SerializeField] private float _currentExtractTime;

        public override ExtractorSaveData ToSaveData()
        {
            var saveData = base.ToSaveData();
            saveData.currentExtractTime = _currentExtractTime;
            return saveData;
        }

        protected override void OnInit()
        {
            base.OnInit();
            _currentExtractTime = _saveData?.currentExtractTime ?? _extractTime;
        }

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            if (_resourceItem == null)
            {
                while (SimulationClock.IsPaused || !TrySetResourceItem())
                    await UniTask.NextFrame(cancellationToken);
            }

            _resourceItem.gameObject.SetActive(false);

            while (_currentExtractTime > 0f)
            {
                _currentExtractTime -= SimulationClock.DeltaTime;
                await UniTask.NextFrame(cancellationToken);
            }
            _currentExtractTime = _extractTime;

            _resourceItem.gameObject.SetActive(true);

            Conveyor conveyor = null;
            while (SimulationClock.IsPaused || !TryGetConveyor(out conveyor) || !conveyor.CanAcceptIncomingResourceItem())
                await UniTask.NextFrame(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return false;
            if (_resourceItem == null)
                return true;

            var closest = Utils.GetClosest(_resourceItem.transform, conveyor.ResourceInputs).position;
            await _resourceItem.MoveToAsync(new List<Vector3>() { _resourceItem.transform.position, closest }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return false;
            if (_resourceItem == null)
                return true;

            PassResourceItem(conveyor);

            return true;
        }

        protected override void OnSimulationPaused(bool paused)
        {
            SetEnabledAnimators(!paused);
        }

        private bool TrySetResourceItem()
        {
            foreach (var input in _inputs)
            {
                var gridPos = FactoryUtils.GetGridPos(input);
                if (FactoryMap.Instance.TryGetEntity(gridPos, out ResourceNode resourceNode))
                {
                    _resourceItem = ObjectPoolingSystem.Instance.GetObject<ResourceItem>(resourceNode.OutputResourceType.ToString(), transform);
                    _resourceItem.transform.SetPositionAndRotation(_resourceItemLocator.position, _resourceItemLocator.rotation);
                    _resourceItem.UpdateSavedData();
                    return true;
                }
            }
            return false;
        }

        private bool TryGetConveyor(out Conveyor conveyor)
        {
            conveyor = null;
            foreach (var output in _outputs)
                if (FactoryMap.Instance.TryGetEntity(FactoryUtils.GetGridPos(output), out conveyor))
                    return true;
            return false;
        }

        protected override bool CheckIsCorrectlyPlaced()
        {
            if (!base.CheckIsCorrectlyPlaced())
                return false;

            foreach (var input in _inputs)
            {
                var gridPos = FactoryUtils.GetGridPos(input);
                if (FactoryMap.Instance.TryGetEntity(gridPos, out ResourceNode _))
                    return true;
            }

            return false;
        }

        private void SetEnabledAnimators(bool enabled)
        {
            foreach (var animator in _animators)
                animator.enabled = enabled;
        }

        public bool TryGetConnectedConveyorChains(out List<Conveyor> conveyors)
        {
            conveyors = null;
            foreach (var output in _outputs)
            {
                var gridPos = FactoryUtils.GetGridPos(output);
                if (FactoryMap.Instance.TryGetEntity(gridPos, out Conveyor conveyor))
                {
                    conveyors = conveyor.GetConnectedConveyorsChain();
                    return true;
                }
            }
            return false;
        }
    }
}