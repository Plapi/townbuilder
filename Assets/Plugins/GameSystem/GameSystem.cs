using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameSystem : MonoBehaviourSingleton<GameSystem>
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly Dictionary<Type, GameStateBase> _dictStates = new Dictionary<Type, GameStateBase>();
    private readonly Queue<GameStateBase> queueStates = new Queue<GameStateBase>();

    private GameStateBase _newState;
    
    protected override void Awake() {
        base.Awake();
        
        Application.targetFrameRate = 60;
        
        var states = GetComponentsInChildren<GameStateBase>();
        
        foreach (var state in states)
            _dictStates.Add(state.GetType(), state);
        
        Run().Forget();
    }
    
    private async UniTask Run()
    {
        while (_cancellationTokenSource.IsCancellationRequested == false)
        {
            await UniTask.NextFrame();
            
            if (_newState != null)
            {
                queueStates.Enqueue(_newState);
                _newState = null;
            }
            
            if (queueStates.Count == 0)
                break;

            try
            {
                await UniTask.WhenAny(
                    queueStates.Peek().Run(_cancellationTokenSource.Token),
                    UniTask.WaitUntil(() => _newState != null, cancellationToken: _cancellationTokenSource.Token)
                );
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested) { }
            catch (Exception e)
            {
                if (e is not OperationCanceledException || _cancellationTokenSource.IsCancellationRequested == false)
                    Debug.LogException(e);
                break;
            }
        }
    }
    
    private void OnDestroy()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
    
    public void EnqueueState<TState, TContext>(TContext context) 
        where TContext : GameStateBase.Context
        where TState : GameState<TContext>
    {
        _newState = _dictStates[typeof(TState)];
        _newState.SetContext(context);
    }
}
