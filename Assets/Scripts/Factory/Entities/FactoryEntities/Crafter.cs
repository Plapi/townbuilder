using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Crafter : FactoryEntity<EntitySaveData>
    {
        [Space]
        [SerializeField] private Animator[] _animators;

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

        protected override void OnSimulationPaused(bool paused)
        {
            SetEnabledAnimators(!paused);
        }

        private void SetEnabledAnimators(bool enabled)
        {
            foreach (var animator in _animators)
                animator.enabled = enabled;
        }
    }
}