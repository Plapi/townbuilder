using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameStateMain : GameState<GameStateMain.Context>
{
    [SerializeField] private MobileTouchCamera _mobileTouchCamera;

    private UIMainPanel _mainPanel;
    
    public new class Context : GameStateBase.Context
    {
        
    }

    private void Start()
    {
        InitUI();
        GameSystem.Instance.EnqueueState<GameStateMain, Context>(new Context(), true);
    }
    
    private void InitUI()
    {
        _mainPanel = UIController.Instance.GetPanel<UIMainPanel>();
        _mainPanel.Init(new UIMainPanel.Data());
    }
    
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        await _mainPanel.Show(cancellationToken);
        
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>
        {
            { WaitForSelectedButton, ProcessSelectedButton },
            { WaitForCameraMovement, ProcessCameraMovement }
        }, cancellationToken);
    }
    
    private async UniTask WaitForCameraMovement(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.enabled = true;
        await UniTask.WaitUntil(() => _mobileTouchCamera.IsDragging || _mobileTouchCamera.IsPinching, cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessCameraMovement(CancellationToken cancellationToken)
    {
        await UniTask.WaitUntil(() => _mobileTouchCamera.IsDragging == false && _mobileTouchCamera.IsPinching == false, cancellationToken: cancellationToken);
    }
    
    private async UniTask WaitForSelectedButton(CancellationToken cancellationToken)
    {
        _mainPanel.ResetSelectedButton();
        await UniTask.WaitUntil(() => _mainPanel.SelectedButton != null, cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessSelectedButton(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.enabled = false;
        
        if (_mainPanel.SelectedButton == UIButtonType.Extractor)
        {
            await _mainPanel.Close(true, cancellationToken: cancellationToken);
            GameSystem.Instance.EnqueueState<GameStateBuildExtractor, GameStateBuildExtractor.Context>(new GameStateBuildExtractor.Context(), false);
        }
        else if (_mainPanel.SelectedButton == UIButtonType.Conveyor)
        {
            
        }
        else if (_mainPanel.SelectedButton == UIButtonType.Crafting)
        {
            
        }
    }
}
