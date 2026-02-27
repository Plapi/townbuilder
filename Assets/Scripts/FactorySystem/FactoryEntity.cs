using System.Collections.Generic;
using UnityEngine;

public abstract class FactoryEntity : Entity
{
    [SerializeField] protected GameObject _graphic;
    
    public bool IsCorrectlyPlaced { get; private set; }
    
    protected override void Awake()
    {
        base.Awake();
        SetActiveInputsOutputs(false);
    }
    
    public override void ApplyCorrectPlacement(bool hasCorrectPlacement)
    {
        IsCorrectlyPlaced = hasCorrectPlacement;
    }
    
    public void SetActiveInputsOutputs(bool active)
    {
        foreach (var input in _inputs)
            input.gameObject.SetActive(active);
        foreach (var output in _outputs)
            output.gameObject.SetActive(active);
    }
}
