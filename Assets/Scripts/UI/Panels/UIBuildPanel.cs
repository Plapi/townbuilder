using UnityEngine;
using UnityEngine.UI;

public class UIBuildPanel : UIPanel<UIBuildPanel.Data>
{
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _rotateButton;
    [SerializeField] private Button _cancelButton;
    
    protected override void OnInit()
    {
        _confirmButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Confirm);
        _rotateButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Rotate);
        _cancelButton.SetExclusiveListener(() => SelectedButton = UIButtonType.Close);
    }
    
    public new class Data : UIPanelBase.Data
    {
        
    }
}