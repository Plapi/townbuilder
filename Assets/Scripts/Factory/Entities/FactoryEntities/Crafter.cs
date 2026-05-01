using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Crafter : FactoryEntity<EntitySaveData>
    {
        [Space]
        [SerializeField] private Animator[] _animators;

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