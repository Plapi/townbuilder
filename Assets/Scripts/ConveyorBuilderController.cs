using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class ConveyorBuilderController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Conveyor _conveyor;
    
    [Header("Cell Selector")]
    [SerializeField] private GameObject _cellSelectorBuild;
    [SerializeField] private GameObject _cellSelectorRemove;
    
    private readonly Dictionary<Vector3Int, Conveyor> _conveyors = new Dictionary<Vector3Int, Conveyor>();
    private Vector3Int? _prevGridPos;
    
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        if (Input.GetMouseButton(0))
        {
            if (TryGetMouseWorldPosition(out Vector3 worldPos) == false)
                return;
            
            var gridPos = WorldToGrid(worldPos);
            
            _cellSelectorBuild.SetActive(true);
            _cellSelectorBuild.transform.SetXZ(gridPos.x, gridPos.z);
            
            TryCreateNewConveyor(gridPos);
        } 
        else if (Input.GetMouseButtonUp(0))
        {
            _cellSelectorBuild.SetActive(false);
            _prevGridPos = null;
        }
        else if (Input.GetMouseButton(1))
        {
            if (TryGetMouseWorldPosition(out Vector3 worldPos) == false)
                return;
            
            var gridPos = WorldToGrid(worldPos);
            
            _cellSelectorRemove.SetActive(true);
            _cellSelectorRemove.transform.SetXZ(gridPos.x, gridPos.z);

            if (_conveyors.TryGetValue(gridPos, out var conveyor))
            {
                DestroyImmediate(conveyor.gameObject);
                _conveyors.Remove(gridPos);
                
                var neighbors = GetNearConveyors(gridPos);
                foreach (var neighbor in neighbors)
                    if (neighbor.Connexions == 1)
                        neighbor.UpdateVisuals();
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            _cellSelectorRemove.SetActive(false);
        }
    }

    private void TryCreateNewConveyor(Vector3Int gridPos)
    {
        if (_prevGridPos != null)
        {
            var direction = gridPos - _prevGridPos.Value;
            if (IsCardinal(direction) == false)
                return;
        }
        
        if (_conveyors.ContainsKey(gridPos))
            return;
        
        var newConveyor = Instantiate(_conveyor, transform);
        newConveyor.name = $"Conveyor{_conveyors.Count}";
        newConveyor.Init(gridPos);
        _conveyors[gridPos] = newConveyor;
        
        _prevGridPos ??= TrGetNearConveyorPos(gridPos);
        
        if (_prevGridPos != null)
            newConveyor.SetPrevConveyor(_conveyors[_prevGridPos.Value]);
        else
            newConveyor.SetVisual(ConveyorType.Straight, Vector3Int.zero, 0);
        
        _prevGridPos = gridPos;
    }

    private Vector3Int? TrGetNearConveyorPos(Vector3Int gridPos)
    {
        var nearConveyors = GetNearConveyors(gridPos);
        var nearConveyor = nearConveyors.OrderBy(c => c.Connexions).FirstOrDefault();
        if (nearConveyor?.Connexions <= 1)
            return nearConveyor.GridPos;
        return null;
    }

    private List<Conveyor> GetNearConveyors(Vector3Int gridPos)
    {
        var nearConveyors = new List<Conveyor>();
        TryAddNewConveyor(ref nearConveyors, gridPos + Vector3Int.forward);
        TryAddNewConveyor(ref nearConveyors, gridPos + Vector3Int.back);
        TryAddNewConveyor(ref nearConveyors, gridPos + Vector3Int.right);
        TryAddNewConveyor(ref nearConveyors, gridPos + Vector3Int.left);
        return nearConveyors;
    }
    
    private void TryAddNewConveyor(ref List<Conveyor> conveyors, Vector3Int gridPos)
    {
        if (_conveyors.TryGetValue(gridPos, out var nearConveyor))
            conveyors.Add(nearConveyor);
    }
    
    private bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 100f, _groundLayer))
        {
            worldPos = hit.point;
            return true;
        }
        
        worldPos = Vector3.zero;
        return false;
    }
    
    private static Vector3Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x),
            0,
            Mathf.FloorToInt(worldPos.z)
        );
    }
    
    private static bool IsCardinal(Vector3Int dir)
    {
        return Mathf.Abs(dir.x) + Mathf.Abs(dir.z) == 1;
    }
}
