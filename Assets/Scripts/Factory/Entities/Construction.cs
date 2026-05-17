using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Construction : FactoryEntity<EntityData, EntitySaveData>
    {
        [Space]
        [SerializeField] private GameObject[] _stages;

        [Header("Runtime Properties")]
        [SerializeField] private Conveyor _connectedConveyor;

        private readonly Dictionary<ResourceItemType, int> _resourcesDict = new Dictionary<ResourceItemType, int>();

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            if (_resourceItem != null)
            {
                _resourcesDict.TryAdd(_resourceItem.Type, 0);
                _resourcesDict[_resourceItem.Type]++;

                ObjectPoolingSystem.Instance.ReleaseObject(_resourceItem);
                _resourceItem = null;
            }

            await UniTask.NextFrame(cancellationToken);
            return true;
        }

        public int GetResourceCount(ResourceItemType type)
        {
            return _resourcesDict.GetValueOrDefault(type, 0);
        }
    }

    public enum ConstructionState
    {
        NotStarted,
        Started,
        Finished
    }
}