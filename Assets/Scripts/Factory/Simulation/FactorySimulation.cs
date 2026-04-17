using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using com.Plapamaru.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySimulationSystem : MonoBehaviour, IFactoryListener
    {
        [Header("Runtime Properties")]
        [SerializeField] private List<ResourceNode> _resourceNodes = new List<ResourceNode>();
        [SerializeField] private List<Extractor> _extractors = new List<Extractor>();
        [SerializeField] private List<Conveyor> _conveyors = new List<Conveyor>();
        [SerializeField] private List<Construction> _constructions = new List<Construction>();

        private readonly List<Distribution> _distributions = new List<Distribution>();
        private readonly List<ResourceItem> _resourceItems = new List<ResourceItem>();

        private Dictionary<Vector2Int, Entity> _map;
        private bool _updateDistributions = true;

        public async UniTask Run(Dictionary<Vector2Int, Entity> map, CancellationToken cancellationToken)
        {
            _map = map;

            while (cancellationToken.IsCancellationRequested == false)
            {
                if (_updateDistributions)
                {
                    UpdateDistributions();

                    var staticExtractors = new List<Extractor>(_extractors);
                    var staticConveyors = new List<Conveyor>(_conveyors);

                    foreach (var distribution in _distributions)
                    {
                        distribution.extractor.SetEnabledAnimators(true);
                        staticExtractors.Remove(distribution.extractor);

                        foreach (var conveyor in distribution.conveyors)
                        {
                            conveyor.SetBeltSpeed(1f);
                            staticConveyors.Remove(conveyor);
                        }
                    }

                    foreach (var extractor in staticExtractors)
                        extractor.SetEnabledAnimators(false);
                    foreach (var conveyor in staticConveyors)
                        conveyor.SetBeltSpeed(0f);

                    _updateDistributions = false;
                }

                foreach (var distribution in _distributions)
                {
                    var resourceItem = InstantiateResourceItem(distribution);
                    _resourceItems.Add(resourceItem);
                    RunItemRoute(resourceItem, distribution, cancellationToken).Forget();
                }

                await UniTask.Delay(FactoryConstants.PRODUCTION_STEP_TIME, cancellationToken);
            }
        }

        private ResourceItem InstantiateResourceItem(Distribution distribution)
        {
            var resourceItem = ObjectPoolingSystem.Instance.GetObject<ResourceItem>(distribution.resourceNode.OutputResourceType.ToString(), transform);
            resourceItem.transform.position = distribution.path[0];
            return resourceItem;
        }

        private async UniTaskVoid RunItemRoute(ResourceItem item, Distribution distribution, CancellationToken cancellationToken)
        {
            try
            {
                await item.RunAlongRoute(distribution.path, cancellationToken);
            }
            finally
            {
                _resourceItems.Remove(item);
            }
        }

        public void OnEntityPlaced(Entity entity)
        {
            if (entity is ResourceNode resourceNode && !_resourceNodes.Contains(resourceNode))
                _resourceNodes.Add(resourceNode);
            else if (entity is Extractor extractor && !_extractors.Contains(extractor))
                _extractors.Add(extractor);
            else if (entity is Conveyor conveyor && !_conveyors.Contains(conveyor))
                _conveyors.Add(conveyor);
            else if (entity is Construction construction && !_constructions.Contains(construction))
                _constructions.Add(construction);

            if (_map != null)
                _updateDistributions = true;
        }

        public void OnEntityRemoved(Entity entity)
        {
            if (entity is Extractor extractor)
                _extractors.Remove(extractor);
            else if (entity is Conveyor conveyor)
                _conveyors.Remove(conveyor);

            if (_map != null)
                _updateDistributions = true;
        }

        private void UpdateDistributions()
        {
            _distributions.Clear();

            foreach (var resourceNode in _resourceNodes)
            {
                var extractors = GetConnectedExtractors(resourceNode);
                foreach (var extractor in extractors)
                {
                    if (!TryGetConnectedConveyorChainsFromExtractor(extractor, out var conveyors) ||
                        !TryGetConstruction(conveyors[^1], out var construction))
                        continue;

                    var distribution = new Distribution(resourceNode, extractor, conveyors, construction);
                    _distributions.Add(distribution);
                }
            }
        }

        private List<Extractor> GetConnectedExtractors(ResourceNode resourceNode)
        {
            var extractors = new List<Extractor>();
            var adjPositions = resourceNode.GetAdjacentGridPositions();
            foreach (var gridPos in adjPositions)
                if (_map.ContainsKey(gridPos) && _map[gridPos] is Extractor extractor && !extractors.Contains(extractor))
                    extractors.Add(extractor);
            return extractors;
        }

        private bool TryGetConnectedConveyorChainsFromExtractor(Extractor extractor, out List<Conveyor> conveyors)
        {
            conveyors = null;
            foreach (var output in extractor.Outputs)
            {
                var gridPos = FactoryUtils.GetGridPos(output);
                if (_map.ContainsKey(gridPos) && _map[gridPos] is Conveyor conveyor)
                {
                    conveyors = GetConnectedConveyors(conveyor);
                    return true;
                }
            }
            return false;
        }

        private static List<Conveyor> GetConnectedConveyors(Conveyor conveyor)
        {
            var conveyors = new List<Conveyor>()
            {
                conveyor
            };

            var nextConveyor = conveyor.NextConveyor;
            while (nextConveyor != null && !conveyors.Contains(nextConveyor))
            {
                conveyors.Add(nextConveyor);
                nextConveyor = nextConveyor.NextConveyor;
            }

            return conveyors;
        }

        private bool TryGetConstruction(Conveyor conveyor, out Construction inputConstruction)
        {
            inputConstruction = null;

            foreach (var construction in _constructions)
            {
                foreach (var input in construction.Inputs)
                {
                    var gridPos = FactoryUtils.GetGridPos(input);
                    if (_map.TryGetValue(gridPos, out var entity) && entity == conveyor)
                    {
                        inputConstruction = construction;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}