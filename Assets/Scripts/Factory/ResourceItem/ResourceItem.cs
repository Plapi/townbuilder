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

        public async UniTask RunAlongRoute(List<Vector3> path, CancellationToken cancellationToken)
        {
            try
            {
                for (int i = 1; i < path.Count; i++)
                {
                    await MoveToAsync(path[i], cancellationToken);
                }
            }
            finally
            {
                ObjectPoolingSystem.Instance.ReleaseObject(this);
            }
        }

        private async UniTask MoveToAsync(Vector3 worldPos, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var startPos = transform.position;
            var elapsed = 0f;

            while (elapsed < FactoryConstants.PRODUCTION_STEP_TIME)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FactoryConstants.PRODUCTION_STEP_TIME);

                transform.position = Vector3.Lerp(startPos, worldPos, t);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.position = worldPos;
        }
    }
}