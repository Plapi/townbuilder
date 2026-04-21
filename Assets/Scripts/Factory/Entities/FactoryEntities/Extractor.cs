using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Extractor : FactoryEntity
    {
        [Space]
        [SerializeField] private Animator[] _animators;
        [SerializeField] private Transform _resourceOutput;
        [SerializeField] private float _extractTime;

        public Transform ResourceOutput => _resourceOutput;

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

        public void SetEnabledAnimators(bool enabled)
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