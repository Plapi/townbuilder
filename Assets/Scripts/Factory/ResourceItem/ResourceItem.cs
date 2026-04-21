using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ResourceItem : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private ResourceItemType _type;

        public string Id => _type.ToString();
        public MonoBehaviour Behaviour => this;

        public void OnRelease()
        {

        }

        public async UniTask RunAlongRoute(DistributionPath path, CancellationToken cancellationToken)
        {
            try
            {
                transform.position = path.PointGroups[0].points[0];
                var end = path.PointGroups[0].points[1];
                var direction = end - transform.position;
                transform.rotation = Quaternion.LookRotation(direction);
                
                for (int i = 1; i < path.PointGroups.Count; i++)
                {
                    await MoveToAsync(path.PointGroups[i].points, cancellationToken);
                }
            }
            finally
            {
                ObjectPoolingSystem.Instance.ReleaseObject(this);
            }
        }

        private async UniTask MoveToAsync(List<Vector3> points, CancellationToken cancellationToken)
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

                var direction = end - transform.position;
                var startRotation = transform.rotation;
                var endRotation = Quaternion.LookRotation(direction);

                while (segmentElapsed < segmentDuration && cancellationToken.IsCancellationRequested == false)
                {
                    segmentElapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(segmentElapsed / segmentDuration);

                    transform.position = Vector3.Lerp(start, end, t);

                    transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transform.position = end;
            }

            transform.position = points[^1];
        }
    }
}