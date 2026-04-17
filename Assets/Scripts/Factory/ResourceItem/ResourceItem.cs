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

        public async UniTask RunAlongRoute(Conveyor firstConveyor, Transform constructionInput, CancellationToken cancellationToken)
        {
            try
            {
                if (constructionInput == null || firstConveyor == null)
                    return;

                Conveyor current = firstConveyor;
                while (!cancellationToken.IsCancellationRequested && current != null)
                {
                    var exitDir = GetExitDirectionFromConveyor(current, current.NextConveyor, constructionInput);
                    var targetPos = GetConveyorSlotPosition(current);
                    await MoveToAsync(targetPos, exitDir, cancellationToken);
                    current = current.NextConveyor;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                var toConstruction = constructionInput.position - transform.position;
                toConstruction.y = 0f;
                await MoveToAsync(constructionInput.position, toConstruction, cancellationToken);
            }
            finally
            {
                ObjectPoolingSystem.Instance.ReleaseObject(this);
            }
        }

        private static Vector3 GetExitDirectionFromConveyor(Conveyor conveyor, Conveyor nextOnBelt, Transform constructionInput)
        {
            if (nextOnBelt != null)
            {
                var d = nextOnBelt.GridPos - conveyor.GridPos;
                return new Vector3(d.x, 0f, d.y);
            }

            var beltCenter = new Vector3(conveyor.GridPos.x + 0.5f, constructionInput.position.y, conveyor.GridPos.y + 0.5f);
            var flat = constructionInput.position - beltCenter;
            flat.y = 0f;
            return flat;
        }

        private static Vector3 GetConveyorSlotPosition(Conveyor conveyor)
        {
            return new Vector3(conveyor.GridPos.x + 0.5f, 1f, conveyor.GridPos.y + 0.5f);
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
