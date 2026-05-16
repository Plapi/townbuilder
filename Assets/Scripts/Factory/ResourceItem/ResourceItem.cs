using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceItem : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private ResourceItemData _data;
        [SerializeField] private bool _rotate;

        public string Id => _data.Id;
        public MonoBehaviour Behaviour => this;
        public ResourceItemType Type => _data.type;

        public ResourceItemSaveData SavedData { get; private set; }

        public void SetSavedData(ResourceItemSaveData saveData)
        {
            SavedData = saveData;
        }

        public void UpdateSavedData()
        {
            SavedData ??= new ResourceItemSaveData();
            SavedData.id = Id;
            SavedData.position = transform.position;
            SavedData.rotation = transform.rotation;
        }

        public async UniTask MoveToAsync(List<Vector3> points, CancellationToken cancellationToken)
        {
            const float totalDuration = FactoryConstants.PRODUCTION_STEP_TIME;

            var path = new List<Vector3> { transform.position };
            path.AddRange(points);

            var totalLength = 0f;
            for (int i = 0; i < path.Count - 1; i++)
                totalLength += Vector3.Distance(path[i], path[i + 1]);

            if (totalLength <= Mathf.Epsilon)
            {
                transform.position = points[^1];
                return;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                var start = path[i];
                var end = path[i + 1];

                var segmentLength = Vector3.Distance(start, end);
                var segmentDuration = (segmentLength / totalLength) * totalDuration;

                var segmentElapsed = 0f;

                var segmentDirection = end - start;

                var startRotation = Quaternion.identity;
                var endRotation = Quaternion.identity;
                if (_rotate)
                {
                    startRotation = transform.rotation;
                    endRotation = segmentDirection.sqrMagnitude > 1e-6f
                        ? Quaternion.LookRotation(segmentDirection)
                        : startRotation;
                }

                while (segmentElapsed < segmentDuration && cancellationToken.IsCancellationRequested == false)
                {
                    segmentElapsed += SimulationClock.DeltaTime;
                    var t = Mathf.Clamp01(segmentElapsed / segmentDuration);

                    transform.position = Vector3.Lerp(start, end, t);

                    if (_rotate)
                        transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transform.position = end;
            }

            transform.position = points[^1];
        }

        public void OnDispose()
        {
            SavedData = null;
        }
    }
}
