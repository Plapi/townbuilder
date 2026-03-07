using System.Collections.Generic;
using System.Threading;
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
        private Dictionary<Vector2Int, Entity> _map;
        private bool _updateDistributions = true;

        public async UniTask Run(Dictionary<Vector2Int, Entity> map, CancellationToken cancellationToken)
        {
            _map = map;

            while (cancellationToken.IsCancellationRequested == false)
            {
                if (_updateDistributions)
                {
                    foreach (var conveyor in _conveyors)
                        conveyor.SetBeltSpeed(0f);

                    UpdateDistributions();

                    foreach (var distribution in _distributions)
                        foreach (var conveyor in distribution.conveyors)
                            conveyor.SetBeltSpeed(1f);

                    _updateDistributions = false;
                }

                await UniTask.Delay(FactoryConstants.PRODUCTION_STEP_TIME, cancellationToken);
            }
        }

        public void OnEntityPlaced(Entity entity)
        {
            if (entity is ResourceNode resourceNode)
                _resourceNodes.Add(resourceNode);
            else if (entity is Extractor extractor)
                _extractors.Add(extractor);
            else if (entity is Conveyor conveyor)
                _conveyors.Add(conveyor);
            else if (entity is Construction construction)
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
                        if (TryGetConstruction(conveyors[^1], _map, out var construction))
                        {
                            _distributions.Add(new Distribution()
                            {
                                resourceNode = resourceNode,
                                extractor = extractor,
                                conveyors = conveyors,
                                construction = construction
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

        private bool TryGetConstruction(Conveyor conveyor, Dictionary<Vector2Int, Entity> map, out Construction inputConstruction)
        {
            inputConstruction = null;

            foreach (var construction in _constructions)
            {
                foreach (var input in construction.Inputs)
                {
                    var gridPos = FactoryUtils.GetGridPos(input);
                    if (map.ContainsKey(gridPos) && map[gridPos] == conveyor)
                    {
                        inputConstruction = construction;
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
        }
    }
}