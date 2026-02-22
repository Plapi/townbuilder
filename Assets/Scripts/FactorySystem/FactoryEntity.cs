using System.Collections.Generic;
using UnityEngine;

public abstract class FactoryEntity : Entity
{
    [SerializeField] private GameObject _graphic;
    [SerializeField] private Transform[] _inputs;
    [SerializeField] private Transform[] _outputs;
    
    public bool HasCorrectPlacement { get; private set; }
    
    private readonly List<Material> _graphicMaterials = new List<Material>();
    
    protected override void Awake()
    {
        base.Awake();
        var renderers = _graphic.GetComponentsInChildren<Renderer>(true); 
        foreach (var rend in renderers)
            _graphicMaterials.AddRange(rend.materials);
        SetActiveInputsOutputs(false);
    }
    
    public void PlaceOnCenter(Vector3 worldPos)
    {
        Vector3 offset = new Vector3(Size.x * 0.5f, 0f, Size.y * 0.5f);
        Vector3 backLeftPos = worldPos - offset;
        Place(Utils.WorldToGrid(backLeftPos));
    }
    
    public void Place(Vector2Int gridPos)
    {
        transform.position = new Vector3(gridPos.x, 0f, gridPos.y);
        GridPos = gridPos;
    }
    
    public void Rotate()
    {
        Vector3 centerBefore = transform.position + 
                               transform.right * (Size.x * 0.5f) + 
                               transform.forward * (Size.y * 0.5f);
        transform.Rotate(0f, 90f, 0f);
        Vector3 centerAfter = transform.position + 
                              transform.right * (Size.x * 0.5f) + 
                              transform.forward * (Size.y * 0.5f);
        transform.position += centerBefore - centerAfter;
        GridPos = Utils.WorldToGrid(transform.position);
    }

    public override void ApplyCorrectPlacement(bool hasCorrectPlacement)
    {
        base.ApplyCorrectPlacement(hasCorrectPlacement);
        var color = hasCorrectPlacement ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor;
        SetColor(color);
        HasCorrectPlacement = hasCorrectPlacement;
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
