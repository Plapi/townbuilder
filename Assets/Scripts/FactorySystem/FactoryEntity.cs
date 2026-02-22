using System.Collections.Generic;
using UnityEngine;

public abstract class FactoryEntity : Entity
{
    public Vector2Int GridPos { get; private set; }
    public List<Vector2Int> GridPositions { get; private set; } 
    public Vector2Int Forward
    {
        get
        {
            Vector3 f = transform.forward;
            return new Vector2Int(Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.z));
        }
    }
    public Vector2Int Right
    {
        get
        {
            Vector3 r = transform.right;
            return new Vector2Int(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.z));
        }
    }
    public bool HasCorrectPlacement { get; private set; }
    
    private readonly List<Material> _materials = new List<Material>();
    
    private void Awake()
    {
        GridPositions = new List<Vector2Int>();
        
        var renderers = gameObject.GetComponentsInChildren<Renderer>(true); 
        foreach (var rend in renderers)
            _materials.AddRange(rend.materials);
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

    public void ApplyCorrectPlacement(bool hasCorrectPlacement)
    {
        var color = hasCorrectPlacement ? FactoryConfig.Instance.correctColor : FactoryConfig.Instance.wrongColor;
        SetColor(color);
        HasCorrectPlacement = hasCorrectPlacement;
    }
    
    public void SetColor(Color color)
    {
        foreach (var material in _materials)
            material.SetColor("_BaseColor", color);   
    }
}
