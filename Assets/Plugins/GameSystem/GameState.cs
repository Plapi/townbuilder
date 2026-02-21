using System;
using System.Collections.Generic;
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
    public bool ShouldExit { get; protected set; }
    
    public abstract class Context { }
    
    public abstract void SetContext(Context context);
    
    public abstract UniTask Run(CancellationToken cancellationToken);
    
    protected async UniTask BaseRun(Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>> raceTasks, CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            var raceCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(raceCts.Token, cancellationToken);
            
            var tasks = UniTaskUtils.CreateRaceTasks(raceTasks, linkedCts.Token);
            var (wasCancelled, result) = await UniTask.WhenFirst(tasks, raceCts)
                .SuppressCancellationThrow();
            
            if (wasCancelled)
                break;
            
            await result.Invoke(cancellationToken);

            if (ShouldExit)
            {
                ShouldExit = false;
                break;
            }
        }
    }
}
