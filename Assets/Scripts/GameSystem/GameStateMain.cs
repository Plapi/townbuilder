using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameStateMain : GameState<GameStateMain.Context>
{
    [SerializeField] private MobileTouchCamera _mobileTouchCamera;
    [SerializeField] private Button _extractorButton;
    [SerializeField] private Button _conveyorButton;
    [SerializeField] private Button _craftingButton;
    
    private bool _extractorTapped;
    private bool _conveyorTapped;
    private bool _craftingTapped;
    
    public new class Context : GameStateBase.Context
    {
        
    }
    
    private void Awake()
    {
        InitializeUI();
        GameSystem.Instance.EnqueueState<GameStateMain, Context>(new Context(), true);
    }
    
    private void InitializeUI()
    {
        _extractorButton.onClick.RemoveAllListeners();
        _extractorButton.onClick.AddListener(() => _extractorTapped = true);
        _conveyorButton.onClick.RemoveAllListeners();
        _conveyorButton.onClick.AddListener(() => _conveyorTapped = true);
        _craftingButton.onClick.RemoveAllListeners();
        _craftingButton.onClick.AddListener(() => _craftingTapped = true);
    }
    
    public override async UniTask Run(CancellationToken cancellationToken)
    {
        await BaseRun(new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>
        {
            { WaitForButtonTapped, ProcessButtonTapped },
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
    
    private async UniTask WaitForButtonTapped(CancellationToken cancellationToken)
    {
        _extractorTapped = false;
        _conveyorTapped = false;
        _craftingTapped = false;
        
        await UniTask.WaitUntil(() => _extractorTapped || _conveyorTapped || _craftingTapped, 
            cancellationToken: cancellationToken);
    }
    
    private async UniTask ProcessButtonTapped(CancellationToken cancellationToken)
    {
        _mobileTouchCamera.enabled = false;
        
        if (_extractorTapped)
        {
            Debug.LogError("Extractor tapped");
            GameSystem.Instance.EnqueueState<GameStateBuildExtractor, GameStateBuildExtractor.Context>(new GameStateBuildExtractor.Context(), false);
        }
        else if (_conveyorTapped)
        {
            Debug.LogError("Conveyor tapped");
        }
        else
        {
            Debug.LogError("Crafting tapped");
        }
    }
}
