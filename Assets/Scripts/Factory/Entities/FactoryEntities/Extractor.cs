using System.Collections.Generic;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Extractor : FactoryEntity
    {
        [Space]
        [SerializeField] private Animator[] _animators;

        protected override bool CheckIsCorrectlyPlaced(Dictionary<Vector2Int, Entity> map)
        {
            if (!base.CheckIsCorrectlyPlaced(map))
                return false;

            foreach (var input in _inputs)
            {
                var gridPos = FactoryUtils.GetGridPos(input);
                if (map.TryGetValue(gridPos, out var entity) && entity is ResourceNode)
                    return true;
            }

            return false;
        }

        public void SetEnabledAnimators(bool enabled)
        {
            foreach (var animator in _animators)
                animator.enabled = enabled;
        }
    }
}