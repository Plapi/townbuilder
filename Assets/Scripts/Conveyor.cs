using UnityEngine;

public class Conveyor : MonoBehaviour
{
    [SerializeField] private GameObject _straightConveyorGraphicWithPillar;
    [SerializeField] private GameObject _straightConveyorGraphicWithoutPillar;
    [SerializeField] private GameObject _roundConveyorGraphicWithPillar;
    [SerializeField] private GameObject _roundConveyorGraphicWithoutPillar;
    
    [Header("Runtime Properties")]
    [SerializeField] private ConveyorType _conveyorType;
    [SerializeField] private Vector3Int _gridPos;
    [SerializeField] private Conveyor _nextConveyor;
    [SerializeField] private Conveyor _prevConveyor;
    [SerializeField] private GameObject _currentConveyorGraphic;

    private bool _hasPillar;
    
    public Vector3Int GridPos => _gridPos;
    public int Connexions => _prevConveyor != null && _nextConveyor != null ? 2 :
        _prevConveyor != null || _nextConveyor != null ? 1 : 0;
    
    public void Init(Vector3Int gridPos)
    {
        _gridPos = gridPos;
        transform.position = gridPos;
    }
    
    public void SetVisual(ConveyorType conveyorType, Vector3Int localPosition, float angle, int speedSign = 1)
    {
        if (_conveyorType != conveyorType || _currentConveyorGraphic == null)
        {
            if (_currentConveyorGraphic != null)
                Destroy(_currentConveyorGraphic);
        
            _conveyorType = conveyorType;
            _hasPillar = _prevConveyor == null || _prevConveyor._hasPillar == false;
            var conveyorGraphic = conveyorType == ConveyorType.Straight ? 
                _hasPillar ? _straightConveyorGraphicWithPillar : _straightConveyorGraphicWithoutPillar : 
                _hasPillar ? _roundConveyorGraphicWithPillar : _roundConveyorGraphicWithoutPillar;
            _currentConveyorGraphic = Instantiate(conveyorGraphic, transform);
        }
        
        _currentConveyorGraphic.transform.SetLocalXZ(localPosition.x, localPosition.z);
        _currentConveyorGraphic.transform.SetLocalAngleY(angle);

        if (conveyorType == ConveyorType.Round)
            _currentConveyorGraphic.transform.GetChild(0).GetComponent<MeshRenderer>().material.SetFloat("_Speed", speedSign);
    }
    
    public void SetPrevConveyor(Conveyor conveyor)
    {
        _prevConveyor = conveyor;
        _prevConveyor._nextConveyor = this;
        
        SetVisual(ConveyorType.Straight, Vector3Int.zero, 0f);
        
        UpdateVisuals();
    }
    
    public void UpdateVisuals(bool logs = false)
    {
        if (_prevConveyor == null)
            return;
        
        var direction = GetPrevDirection();
        var direction1 = _prevConveyor._prevConveyor != null ? _prevConveyor.GetPrevDirection() : ConveyorDirection.Front;
        
        if (logs)
        {
            if (_prevConveyor._prevConveyor != null)
                Debug.LogError(direction + " " + direction1);
            else
                Debug.LogError(direction);
        }
        
        if (direction == ConveyorDirection.Front)
        {
            SetVisual(ConveyorType.Straight, Vector3Int.zero, 0f);
            if (_prevConveyor._prevConveyor != null)
            {
                if (direction1 == ConveyorDirection.Right)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.one, 180, -1);
                else if (direction1 == ConveyorDirection.Left)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.right, -90f);
            }
        } 
        else if (direction == ConveyorDirection.Back)
        {
            SetVisual(ConveyorType.Straight, Vector3Int.one, 180);
            if (_prevConveyor._prevConveyor != null)
            {
                if (direction1 == ConveyorDirection.Right)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.forward, 90);
                else if (direction1 == ConveyorDirection.Left)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.zero, 0, -1);
            }
            else
            {
                _prevConveyor.SetVisual(ConveyorType.Straight, Vector3Int.one, 180);
            }
        }
        else if (direction == ConveyorDirection.Right)
        {
            SetVisual(ConveyorType.Straight, Vector3Int.forward, 90);
            if (_prevConveyor._prevConveyor != null)
            {
                if (direction1 == ConveyorDirection.Front)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.zero, 0);
                else if (direction1 == ConveyorDirection.Back)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.right, -90, -1);
            }
            else
            {
                _prevConveyor.SetVisual(ConveyorType.Straight, Vector3Int.forward, 90f);    
            }
        }
        else if (direction == ConveyorDirection.Left)
        {
            SetVisual(ConveyorType.Straight, Vector3Int.right, -90);
            if (_prevConveyor._prevConveyor != null)
            {
                if (direction1 == ConveyorDirection.Front)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.forward, 90, -1);
                else if (direction1 == ConveyorDirection.Back)
                    _prevConveyor.SetVisual(ConveyorType.Round, Vector3Int.one, 180);
            }
            else
            {
                _prevConveyor.SetVisual(ConveyorType.Straight, Vector3Int.right, -90f);
            }
        }
    }

    private ConveyorDirection GetPrevDirection()
    {
        var dif = _prevConveyor._gridPos - _gridPos;
        if (dif.x == 0 && dif.z == -1)
            return ConveyorDirection.Front;
        if (dif.x == 0 && dif.z == 1)
            return ConveyorDirection.Back;
        if (dif.x == -1 && dif.z == 0)
            return ConveyorDirection.Right;
        if (dif.x == 1 && dif.z == 0)
            return ConveyorDirection.Left;
        
        Debug.LogError("Direction not found");
        return ConveyorDirection.Left;
    }
    
    private void OnDrawGizmos()
    {
        if (_nextConveyor != null)
        {
            Gizmos.color = Color.red;
            var from = transform.position + new Vector3(0.5f, 1f, 0.5f);
            var to = _nextConveyor.transform.position + new Vector3(0.5f, 1f, 0.5f);
            Gizmos.DrawLine(from, to);
            Utils.DrawArrowHead(from, to, 0.5f);
        }
    }
    
    
}

public enum ConveyorType
{
    Straight,
    Round
}

public enum ConveyorDirection
{
    Front,
    Back,
    Right,
    Left
}
