using System.Collections.Generic;
using UnityEngine;

public abstract class FactoryEntity : Entity
{
    [SerializeField] protected GameObject _graphic;
    
    public bool IsCorrectlyPlaced { get; private set; }
    
    private readonly List<Material> _graphicMaterials = new List<Material>();
    
    protected override void Awake()
    {
        base.Awake();
        var renderers = _graphic.GetComponentsInChildren<Renderer>(true); 
        foreach (var rend in renderers)
            _graphicMaterials.AddRange(rend.materials);
        SetActiveInputsOutputs(false);
    }
    
    public override void ApplyCorrectPlacement(bool hasCorrectPlacement)
    {
        var color = hasCorrectPlacement ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor;
        SetColor(color);
        IsCorrectlyPlaced = hasCorrectPlacement;
    }
    
    public void SetColor(Color color)
    {
        foreach (var material in _graphicMaterials)
            material.SetColor("_BaseColor", color);   
    }
    
    public void SetActiveInputsOutputs(bool active)
    {
        foreach (var input in _inputs)
            input.gameObject.SetActive(active);
        foreach (var output in _outputs)
            output.gameObject.SetActive(active);
    }
}
