using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

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
                    await MoveToAsync(path[i], Vector3.zero, cancellationToken);
                }
            }
            finally
            {
                ObjectPoolingSystem.Instance.ReleaseObject(this);
            }
        }

        private async UniTask MoveToAsync(Vector3 worldPos, Vector3 lookDirXZ, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            lookDirXZ.y = 0f;
            if (lookDirXZ.sqrMagnitude < 1e-6f)
                lookDirXZ = new Vector3(transform.forward.x, 0f, transform.forward.z);
            lookDirXZ.Normalize();

            var rotation = Quaternion.LookRotation(lookDirXZ, Vector3.up);

            var sequence = DOTween.Sequence()
                .Join(transform.DOMove(worldPos, FactoryConstants.PRODUCTION_STEP_TIME).SetEase(Ease.Linear))
                .Join(transform.DORotateQuaternion(rotation, FactoryConstants.PRODUCTION_STEP_TIME).SetEase(Ease.Linear));

            await sequence.AsyncWaitForCompletion();
        }
    }
}