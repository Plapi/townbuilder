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
                    var resourceItem = InstantiateResourceItem(distribution.conveyors[0], distribution.resourceNode, distribution.extractor);
                    _resourceItems.Add(resourceItem);
                    RunItemRoute(resourceItem, distribution.conveyors[0], distribution.constructionInput, cancellationToken).Forget();
                }

                await UniTask.Delay(FactoryConstants.PRODUCTION_STEP_TIME, cancellationToken);
            }
        }

        private ResourceItem InstantiateResourceItem(Conveyor conveyor, ResourceNode resourceNode, Extractor extractor)
        {
            var resourceItem = ObjectPoolingSystem.Instance.GetObject<ResourceItem>(resourceNode.OutputResourceType.ToString(), transform);

            var from = Vector3.zero;
            if (conveyor.PrevConveyor == null)
            {
                var gridPos = FactoryUtils.GetGridPos(extractor.Outputs[0]);
                from = new Vector3(gridPos.x + 0.5f, 1f, gridPos.y + 0.5f);
            }
            else
            {
                var gridPos = conveyor.PrevConveyor.GridPos;
                from = new Vector3(gridPos.x, 0f, gridPos.y) + new Vector3(0.5f, 1f, 0.5f);
            }

            resourceItem.transform.position = from;

            Vector2Int gridDelta;
            if (conveyor.PrevConveyor == null)
                gridDelta = conveyor.GridPos - FactoryUtils.GetGridPos(extractor.Outputs[0]);
            else
                gridDelta = conveyor.GridPos - conveyor.PrevConveyor.GridPos;

            var worldDir = new Vector3(gridDelta.x, 0f, gridDelta.y);
            if (worldDir.sqrMagnitude > 1e-6f)
                resourceItem.transform.forward = worldDir.normalized;

            return resourceItem;
        }

        private async UniTaskVoid RunItemRoute(ResourceItem item, Conveyor firstConveyor, Transform constructionInput, CancellationToken cancellationToken)
        {
            try
            {
                await item.RunAlongRoute(firstConveyor, constructionInput, cancellationToken);
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
                    var conveyorsGroups = GetConnectedConveyors(extractor);
                    foreach (var conveyors in conveyorsGroups)
                    {
                        if (TryGetConstruction(conveyors[^1], _map, out var construction, out var constructionInput))
                        {
                            _distributions.Add(new Distribution()
                            {
                                resourceNode = resourceNode,
                                extractor = extractor,
                                conveyors = conveyors,
                                construction = construction,
                                constructionInput = constructionInput
                            });
                        }
                    }
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

        private List<List<Conveyor>> GetConnectedConveyors(Extractor extractor)
        {
            var conveyorsGroups = new List<List<Conveyor>>();
            foreach (var output in extractor.Outputs)
            {
                var gridPos = FactoryUtils.GetGridPos(output);
                if (_map.ContainsKey(gridPos) && _map[gridPos] is Conveyor conveyor)
                    conveyorsGroups.Add(GetConnectedConveyors(conveyor));
            }
            return conveyorsGroups;
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

        private bool TryGetConstruction(Conveyor conveyor, Dictionary<Vector2Int, Entity> map, out Construction inputConstruction, out Transform constructionInput)
        {
            inputConstruction = null;
            constructionInput = null;

            foreach (var construction in _constructions)
            {
                foreach (var input in construction.Inputs)
                {
                    var gridPos = FactoryUtils.GetGridPos(input);
                    if (map.TryGetValue(gridPos, out var entity) && entity == conveyor)
                    {
                        inputConstruction = construction;
                        constructionInput = input;
                        return true;
                    }
                }
            }

            return false;
        }

        private class Distribution
        {
            public ResourceNode resourceNode;
            public Extractor extractor;
            public List<Conveyor> conveyors;
            public Construction construction;
            public Transform constructionInput;
        }
    }
}