using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySimulation : MonoBehaviour
    {
        
        public async UniTask Run(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.NextFrame(cancellationToken: cancellationToken);
            }
        }

        public void UpdateEntities()
        {
            
        }
    }
}
