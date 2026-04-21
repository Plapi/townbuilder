using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySimulationSystem : MonoBehaviour
    {
        [Header("Runtime Properties")]
        [SerializeField] private List<ResourceNode> _resourceNodes = new List<ResourceNode>();
        [SerializeField] private List<Extractor> _extractors = new List<Extractor>();
        [SerializeField] private List<Conveyor> _conveyors = new List<Conveyor>();
        [SerializeField] private List<Construction> _constructions = new List<Construction>();

        private readonly List<Distribution> _distributions = new List<Distribution>();
        private readonly List<ResourceItem> _resourceItems = new List<ResourceItem>();

        private CancellationTokenSource _cancellationTokenSource;

        public async UniTask Run()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                while (_cancellationTokenSource.Token.IsCancellationRequested == false)
                {
                    foreach (var distribution in _distributions)
                    {
                        var resourceItem = ObjectPoolingSystem.Instance.GetObject<ResourceItem>(distribution.resourceNode.OutputResourceType.ToString(), transform);
                        RunItemRoute(resourceItem, distribution, _cancellationTokenSource.Token).Forget();
                    }

                    await UniTask.Delay(FactoryConstants.PRODUCTION_STEP_TIME, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async UniTaskVoid RunItemRoute(ResourceItem item, Distribution distribution, CancellationToken cancellationToken)
        {
            try
            {
                await item.RunAlongRoute(distribution.path, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            finally
            {
                _resourceItems.Remove(item);
            }
        }

        public void UpdateDistributions()
        {
            _distributions.Clear();

            SplitMapElementsByType();

            foreach (var resourceNode in _resourceNodes)
            {
                var extractors = resourceNode.GetConnectedExtractors();
                foreach (var extractor in extractors)
                {
                    if (!extractor.TryGetConnectedConveyorChains(out var conveyors) ||
                        !TryGetConstruction(conveyors[^1], out var construction))
                        continue;

                    var distribution = new Distribution(resourceNode, extractor, conveyors, construction);
                    _distributions.Add(distribution);
                }
            }

            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
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
        }

        private bool TryGetConstruction(Conveyor conveyor, out Construction inputConstruction)
        {
            inputConstruction = null;

            foreach (var construction in _constructions)
            {
                foreach (var input in construction.Inputs)
                {
                    var gridPos = FactoryUtils.GetGridPos(input);
                    if (FactoryMap.Instance.TryGetEntity(gridPos, out Conveyor outConveyor) && outConveyor == conveyor)
                    {
                        inputConstruction = construction;
                        return true;
                    }
                }
            }

            return false;
        }

        private void SplitMapElementsByType()
        {
            _resourceNodes.Clear();
            _extractors.Clear();
            _conveyors.Clear();
            _constructions.Clear();

            var map = FactoryMap.Instance.Map;

            foreach (var mapElement in map)
            {
                if (mapElement.Value is ResourceNode resourceNode)
                {
                    if (!_resourceNodes.Contains(resourceNode))
                        _resourceNodes.Add(resourceNode);
                }
                else if (mapElement.Value is Extractor extractor)
                {
                    if (!_extractors.Contains(extractor))
                        _extractors.Add(extractor);
                }
                else if (mapElement.Value is Conveyor conveyor)
                {
                    if (!_conveyors.Contains(conveyor))
                        _conveyors.Add(conveyor);
                }
                else if (mapElement.Value is Construction construction)
                {
                    if (!_constructions.Contains(construction))
                        _constructions.Add(construction);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            foreach (var distribution in _distributions)
            {
                var path = distribution.path;
                for (int i = 0; i < path.PointGroups.Count; i++)
                {
                    var points = path.PointGroups[i].points;
                    for (int j = 0; j < points.Count - 1; j++)
                        Gizmos.DrawLine(points[j], points[j + 1]);
                }
            }
        }
    }
}