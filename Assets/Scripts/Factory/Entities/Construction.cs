using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Construction : FactoryEntity<EntityData, EntitySaveData>
    {
        [Header("Runtime Properties")]
        [SerializeField] private Conveyor _connectedConveyor;

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            if (_resourceItem != null)
            {
                ObjectPoolingSystem.Instance.ReleaseObject(_resourceItem);
                _resourceItem = null;
            }

            await UniTask.NextFrame(cancellationToken);
            return true;
        }
    }
}