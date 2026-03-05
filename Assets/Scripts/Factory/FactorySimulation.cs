using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySimulationSystem : MonoBehaviour, IFactoryListener
    {
        public async UniTask Run(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.NextFrame(cancellationToken: cancellationToken);
            }
        }
        
        public void OnEntityPlaced(Entity entity)
        {
            
        }
        
        public void OnEntityRemoved(Entity entity)
        {
            
        }
    }
}
