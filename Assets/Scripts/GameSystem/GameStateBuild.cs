using System;
using System.Collections.Generic;
using System.Threading;
using BitBenderGames;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameStateBuild : GameState<GameStateBuild.Context>
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
        GameSystem.Instance.EnqueueState<GameStateBuild, Context>(new Context() { isRootState = true });
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
        var raceTasks = new Dictionary<Func<CancellationToken, UniTask>, Func<CancellationToken, UniTask>>
        {
            { WaitForButtonTapped, ProcessButtonTapped },
            { WaitForCameraMovement, ProcessCameraMovement }
        };
        
        while (cancellationToken.IsCancellationRequested == false)
        {
            _mobileTouchCamera.enabled = true;
            
            var raceCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(raceCts.Token, cancellationToken);
            
            var tasks = UniTaskUtils.CreateRaceTasks(raceTasks, linkedCts.Token);
            var (wasCancelled, result) = await UniTask.WhenFirst(tasks, raceCts)
                .SuppressCancellationThrow();
            
            if (wasCancelled)
                break;

            if (result != ProcessCameraMovement)
                _mobileTouchCamera.enabled = false;
            
            await result.Invoke(cancellationToken);
        }
    }

    private async UniTask WaitForCameraMovement(CancellationToken cancellationToken)
    {
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
        if (_extractorTapped)
        {
            Debug.LogError("Extractor tapped");
        }
        else if (_conveyorTapped)
        {
            Debug.LogError("Conveyor tapped");
        }
        else
        {
            Debug.LogError("Crafting tapped");
        }
        
        await UniTask.Delay(2f, cancellationToken: cancellationToken);
        
        Debug.LogError("Done");
    }
}
