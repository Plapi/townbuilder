using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class GameState<TContext> : GameStateBase where TContext : GameStateBase.Context
{
    protected new TContext Context { get; private set; }
    
    public override void SetContext(Context context)
    {
        Context = (TContext)context;
    }
}

public abstract class GameStateBase : MonoBehaviour
{
    public class Context
    {
        public bool isRootState;
    }
    
    public abstract void SetContext(Context context);
    
    public abstract UniTask Run(CancellationToken cancellationToken);
}
