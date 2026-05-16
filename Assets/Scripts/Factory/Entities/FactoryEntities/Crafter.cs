using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class Crafter : FactoryEntity<EntityData, EntitySaveData>
    {
        [Space]
        [SerializeField] private Animator[] _animators;
        [SerializeField] private Transform _resourceOutput;
        [SerializeField] private Transform _resourceItemLocator;
        [SerializeField] private float _craftTime;

        private readonly Dictionary<ResourceItemType, int> _resourcesDict = new Dictionary<ResourceItemType, int>();
        private bool _isCrafting;

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            if (_resourceItem == null)
            {
                await UniTask.NextFrame(cancellationToken);
                return true;
            }

            _resourcesDict.TryAdd(_resourceItem.Type, 0);
            _resourcesDict[_resourceItem.Type]++;
            ObjectPoolingSystem.Instance.ReleaseObject(_resourceItem);
            _resourceItem = null;

            if (TryGetOutConveyor(out _) && TryGetRecipe(out CrafterRecipeDefinition recipe))
            {
                _isCrafting = true;

                var craftingTime = _craftTime;
                while (craftingTime > 0f)
                {
                    craftingTime -= SimulationClock.DeltaTime;
                    await UniTask.NextFrame(cancellationToken);
                }

                Conveyor conveyor = null;
                while (SimulationClock.IsPaused || !TryGetOutConveyor(out conveyor) || !conveyor.CanAcceptIncomingResourceItem())
                    await UniTask.NextFrame(cancellationToken);

                foreach (var input in recipe.inputs)
                    _resourcesDict[input.resourceItem.type] -= input.amount;

                var resourceItem = ObjectPoolingSystem.Instance.GetObject<ResourceItem>(recipe.output.ToString(), transform);
                resourceItem.transform.SetPositionAndRotation(_resourceItemLocator.position, _resourceItemLocator.rotation);
                resourceItem.UpdateSavedData();

                var closest = Utils.GetClosest(resourceItem.transform, conveyor.ResourceInputs).position;
                await resourceItem.MoveToAsync(new List<Vector3>() { resourceItem.transform.position, closest }, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    ObjectPoolingSystem.Instance.ReleaseObject(resourceItem);
                    _isCrafting = false;
                    return false;
                }

                if (resourceItem == null)
                {
                    _isCrafting = false;
                    return true;
                }

                _resourceItem = resourceItem;
                PassResourceItem(conveyor);

                _isCrafting = false;
            }

            await UniTask.NextFrame(cancellationToken);
            return true;
        }

        public override bool CanAcceptIncomingResourceItem()
        {
            return base.CanAcceptIncomingResourceItem() && _isCrafting == false;
        }

        private bool TryGetRecipe(out CrafterRecipeDefinition recipeOut)
        {
            recipeOut = null;
            var recipes = FactoryConfig.Instance.crafterRecipes;

            foreach (var recipe in recipes)
            {
                var canCraft = true;
                foreach (var input in recipe.inputs)
                {
                    if (!_resourcesDict.TryGetValue(input.resourceItem.type, out var available) || available < input.amount)
                    {
                        canCraft = false;
                        break;
                    }
                }

                if (canCraft)
                {
                    recipeOut = recipe;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetOutConveyor(out Conveyor conveyor)
        {
            conveyor = null;
            foreach (var output in _outputs)
                if (FactoryMap.Instance.TryGetEntity(FactoryUtils.GetGridPos(output), out conveyor))
                    return true;
            return false;
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