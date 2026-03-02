using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Conveyor : FactoryEntity
{
    [Space]
    [SerializeField] private GameObject _pillar;
    
    [Header("Runtime Properties")]
    [SerializeField] private Conveyor _prevConveyor;
    [SerializeField] private Conveyor _nextConveyor;
    [SerializeField] private List<EntityHighlightObject> _allowedHighlightObjects;
    
    public Conveyor PrevConveyor => _prevConveyor;
    public Conveyor NextConveyor => _nextConveyor;
    
    public void Connect(Conveyor next)
    {
        _nextConveyor = next;
        next._prevConveyor = this;
        next._pillar.gameObject.SetActive(!_pillar.activeSelf);
    }

    public void Disconnect(Conveyor next)
    {
        if (_nextConveyor != next)
        {
            Debug.LogError("Disconnection failed");
            return;
        }
        
        _nextConveyor = null;
        next._prevConveyor = null;
    }

    public void SetPillarActive(bool active)
    {
        _pillar.SetActive(active);
    }

    public void ShowAllowedHighlights()
    {
        var adjacentPositions = GetAdjacentGridPositions();
        foreach (var gridPos in adjacentPositions)
        {
            if (FactorySystem.Instance.HasEntity(gridPos))
                continue;
            var allowedHighlight = ObjectPoolingSystem.Instance.GetObject<EntityHighlightObject>(FactoryConstants.ENTITY_HIGHLIGHT_NAME);
            allowedHighlight.Place(gridPos, FactoryConfig.Instance.previewColor);
            _allowedHighlightObjects.Add(allowedHighlight);
        }
    }
    
    public void ReleaseAllowedHighlights()
    {
        foreach (var allowedHighlight in _allowedHighlightObjects)
            ObjectPoolingSystem.Instance.ReleaseObject(allowedHighlight);
        _allowedHighlightObjects.Clear();
    }
    
    public override void OnConfirmPlacement()
    {
        base.OnConfirmPlacement();
        ReleaseAllowedHighlights();
    }
    
    public override void OnRelease()
    {
        base.OnRelease();
        _prevConveyor = null;
        _nextConveyor = null;
        ReleaseAllowedHighlights();
    }

    public bool TryGetAjdConveyor(Func<Conveyor, bool> func, out Conveyor conveyor)
    {
        conveyor = null;
        var adjacentPositions = GetAdjacentGridPositions();
        foreach (var gridPos in adjacentPositions)
            if (FactorySystem.Instance.TryGetEntity(gridPos, out conveyor) && func(conveyor))
                return true;
        return false;
    }
    
    private void OnDrawGizmos()
    {
        if (_nextConveyor != null)
        {
            Gizmos.color = Color.red;
            var from = new Vector3(GridPos.x, 0f, GridPos.y) + new Vector3(0.5f, 1f, 0.5f);
            var to = new Vector3(_nextConveyor.GridPos.x, 0f, _nextConveyor.GridPos.y) + new Vector3(0.5f, 1.1f, 0.5f);
            Gizmos.DrawLine(from, to);
            Utils.DrawArrowHead(from, to, 0.5f);
        }
    }
}
