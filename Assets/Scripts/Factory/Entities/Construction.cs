using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Construction : Entity
    {
        [Header("Runtime Properties")]
        [SerializeField] private Conveyor _connectedConveyor;

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            if (SimulationClock.Instance.IsPaused)
            {
                await UniTask.NextFrame(cancellationToken);
                return true;
            }

            if (_resourceItem != null)
            {
                ObjectPoolingSystem.Instance.ReleaseObject(_resourceItem);
                _resourceItem = null;
            }

            if (_connectedConveyor != null)
            {
                if (!FactoryMap.Instance.TryGetEntity(_connectedConveyor.GridPos, out _connectedConveyor))
                    _connectedConveyor = null;
                await UniTask.NextFrame(cancellationToken);
                return true;
            }

            foreach (var input in _inputs)
            {
                var gridPos = FactoryUtils.GetGridPos(input);
                if (FactoryMap.Instance.TryGetEntity(gridPos, out _connectedConveyor))
                {
                    _connectedConveyor.ConnectConstruction(this);
                    await UniTask.NextFrame(cancellationToken);
                    return true;
                }
            }

            await UniTask.NextFrame(cancellationToken);
            return true;
        }
    }
}