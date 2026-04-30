using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using com.Plapamaru.Singletons;
using com.Plapamaru.TownCrafter.Factory;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.Plapamaru.TownCrafter.Game
{
    public class GameSystem : MonoBehaviourSingleton<GameSystem>
    {
        private class StateHandle
        {
            public bool isRootState;
            public GameStateBase state;
            public CancellationTokenSource cancellationTokenSource;
        }

        [SerializeField] private FactorySystem _factorySystem;

        private readonly CancellationTokenSource _destroyCTS = new CancellationTokenSource();
        private readonly Dictionary<Type, GameStateBase> _dictStates = new Dictionary<Type, GameStateBase>();
        private readonly Stack<StateHandle> _stateStack = new Stack<StateHandle>();

        private StateHandle _currentState;
        private StateHandle _newState;

        protected override void Awake()
        {
            base.Awake();

            Application.targetFrameRate = 60;

            var states = GetComponentsInChildren<GameStateBase>();

            foreach (var state in states)
                _dictStates.Add(state.GetType(), state);

            Run().Forget();
        }

        private void Start()
        {
            _factorySystem.Init(_destroyCTS.Token);

            EnqueueState<GameStateMain, GameStateMain.Context>(new GameStateMain.Context(), true);
        }

        private async UniTask Run()
        {
            while (_destroyCTS.IsCancellationRequested == false)
            {
                await UniTask.NextFrame();

                if (_newState != null)
                {
                    _stateStack.Push(_newState);
                    _newState = null;
                }

                if (_stateStack.Count == 0)
                    break;

                _currentState = _stateStack.Peek();
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_currentState.cancellationTokenSource.Token, _destroyCTS.Token).Token;

                try
                {
                    var race = await UniTask.WhenAny(
                        _currentState.state.Run(linkedToken),
                        UniTask.WaitUntil(() => _newState != null, cancellationToken: _destroyCTS.Token),
                        UniTask.WaitUntil(() => Input.GetKeyUp(KeyCode.R), cancellationToken: _destroyCTS.Token)
                    );

                    if (race == 0 && !linkedToken.IsCancellationRequested)
                        await _currentState.state.Exit(linkedToken);
                }
                catch (OperationCanceledException) when (_destroyCTS.IsCancellationRequested) { }
                catch (Exception e)
                {
                    if (e is not OperationCanceledException || _destroyCTS.IsCancellationRequested == false)
                        Debug.LogException(e);
                    break;
                }

                if (Input.GetKeyUp(KeyCode.R))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    return;
                }

                if (_newState == null)
                    _stateStack.Pop();
            }
        }

        private void OnDestroy()
        {
            _destroyCTS.Cancel();
            _destroyCTS.Dispose();
        }

        public void EnqueueState<TState, TContext>(TContext context, bool isRootState)
            where TContext : GameStateBase.Context
            where TState : GameState<TContext>
        {
            if (_currentState != null)
            {
                _currentState.cancellationTokenSource.Cancel();
                _currentState.cancellationTokenSource.Dispose();
                if (isRootState == false)
                    _currentState.cancellationTokenSource = new CancellationTokenSource();
            }

            _newState = new StateHandle()
            {
                isRootState = isRootState,
                state = _dictStates[typeof(TState)],
                cancellationTokenSource = new CancellationTokenSource()
            };
            _newState.state.SetContext(context);
        }

        public List<string> GetCurrentStateNames()
        {
            var stackList = _stateStack.ToList();
            var stackListString = new List<string>();
            foreach (var state in stackList)
                stackListString.Add(state.state.ToString());
            return stackListString;
        }
    }
}