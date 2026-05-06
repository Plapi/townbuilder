using System;
using System.Collections.Generic;
using System.Threading;
using com.Plapamaru.Pooling;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public abstract class Conveyor : FactoryEntity<EntityData, ConveyorSaveData>
    {
        [Space]
        [SerializeField] private GameObject _pillar;

        [Header("Belt")]
        [SerializeField] private MeshRenderer _beltRenderer;
        [SerializeField] private int _beltMaterialIndex;

        [Space]
        [SerializeField] private Transform[] _resourceInputs;

        [Header("Runtime Properties")]
        [SerializeField] private Conveyor _prevConveyor;
        [SerializeField] private Conveyor _nextConveyor;
        [SerializeField] private FactoryEntity _connectedFeedTarget;
        [SerializeField] private List<EntityHighlightObject> _allowedHighlightObjects;
        [SerializeField] private List<Vector3> _distributionPoints;

        private float _beltSpeed;
        private int _beltDirection = 1;

        public Conveyor PrevConveyor => _prevConveyor;
        public Conveyor NextConveyor => _nextConveyor;
        public Transform[] ResourceInputs => _resourceInputs;

        public override ConveyorSaveData ToSaveData()
        {
            var saveData = base.ToSaveData();
            if (_nextConveyor != null)
                saveData.nextConveyorGridPos = _nextConveyor.GridPos;
            saveData.beltDirection = _beltDirection;
            saveData.pilarIsActive = _pillar.activeSelf;
            return saveData;
        }

        protected override void OnInit()
        {
            base.OnInit();

            if (_saveData?.nextConveyorGridPos != null)
            {
                var gridPos = _saveData.nextConveyorGridPos.Value;
                if (FactoryMap.Instance.TryGetEntity(gridPos, out Conveyor nextConveyor))
                    Connect(nextConveyor, false);
                else
                    Debug.LogError($"Failed to find conveyor grid pos at {gridPos}");
                SetBeltDirection(_saveData.beltDirection);
                _pillar.SetActive(_saveData.pilarIsActive);
            }

            if (_nextConveyor == null && FactoryMap.Instance.TryGetFactoryEntityFromInput(GridPos, out var feedTarget, out _))
                _connectedFeedTarget = feedTarget;
        }

        protected override async UniTask<bool> ProcessLoop(CancellationToken cancellationToken)
        {
            while (_resourceItem == null)
                await UniTask.NextFrame(cancellationToken);

            _distributionPoints = GetResourceDistributionPoints();
            await _resourceItem.MoveToAsync(_distributionPoints, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return false;
            if (_resourceItem == null)
                return true;

            var entity = (FactoryEntity)null;
            while (!TryGetConnectedEntity(out entity))
                await UniTask.NextFrame(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return false;
            if (_resourceItem == null)
                return true;

            PassResourceItem(entity);

            return true;
        }

        protected override void OnSimulationPaused(bool paused)
        {
            SetBeltSpeed(paused ? 0f : 1f);
        }

        protected abstract List<Vector3> GetResourceDistributionPoints();

        public void Connect(Conveyor next, bool setActiveNextPilar = true)
        {
            _nextConveyor = next;
            next._prevConveyor = this;

            if (setActiveNextPilar)
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

        public void Disconnect()
        {
            if (_prevConveyor != null)
            {
                if (_prevConveyor._nextConveyor == this)
                    _prevConveyor._nextConveyor = null;
                else
                    Debug.LogError("Error on release");
            }
            _prevConveyor = null;

            if (_nextConveyor != null)
            {
                if (_nextConveyor._prevConveyor == this)
                    _nextConveyor._prevConveyor = null;
                else
                    Debug.LogError("Error on release");
            }
            _nextConveyor = null;
        }

        public void ConnectFeedTarget(FactoryEntity feedTarget)
        {
            _connectedFeedTarget = feedTarget;
        }

        public void SetPillarActive(bool active)
        {
            _pillar.SetActive(active);
        }

        public void TryShowAllowedHighlights()
        {
            if (_allowedHighlightObjects.Count > 0)
                return;

            var adjacentPositions = GetAdjacentGridPositions();
            foreach (var gridPos in adjacentPositions)
            {
                if (FactoryMap.Instance.HasEntity(gridPos))
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

        public override void OnDispose()
        {
            base.OnDispose();
            Disconnect();
            ReleaseAllowedHighlights();
            _connectedFeedTarget = null;
        }

        public bool TryGetAjdConveyor(Func<Conveyor, bool> func, out Conveyor conveyor)
        {
            conveyor = null;
            var adjacentPositions = GetAdjacentGridPositions();
            foreach (var gridPos in adjacentPositions)
                if (FactoryMap.Instance.TryGetEntity(gridPos, out conveyor) && func(conveyor))
                    return true;
            return false;
        }

        public List<Conveyor> GetConnectedConveyorsChain()
        {
            var conveyors = new List<Conveyor>() { this };

            var nextConveyor = _nextConveyor;
            while (nextConveyor != null && !conveyors.Contains(nextConveyor))
            {
                conveyors.Add(nextConveyor);
                nextConveyor = nextConveyor.NextConveyor;
            }

            return conveyors;
        }

        private void SetBeltSpeed(float speed)
        {
            _beltRenderer.materials[_beltMaterialIndex].SetFloat("_Speed", speed * _beltDirection);
            _beltSpeed = speed;
        }

        public void SetBeltDirection(int beltDirection)
        {
            _beltDirection = beltDirection;
            SetBeltSpeed(_beltSpeed);
        }

        private bool TryGetConnectedEntity(out FactoryEntity entity)
        {
            entity = _nextConveyor != null ? _nextConveyor :
                _connectedFeedTarget != null ? _connectedFeedTarget : null;
            return entity?.CanAcceptIncomingResourceItem() ?? false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < _distributionPoints.Count - 1; i++)
                Gizmos.DrawLine(_distributionPoints[i], _distributionPoints[i + 1]);

            /*if (_nextConveyor != null)
            {
                Gizmos.color = Color.red;
                var from = new Vector3(GridPos.x, 0f, GridPos.y) + new Vector3(0.5f, 1.1f, 0.5f);
                var to = new Vector3(_nextConveyor.GridPos.x, 0f, _nextConveyor.GridPos.y) + new Vector3(0.5f, 1.1f, 0.5f);
                Gizmos.DrawLine(from, to);
                Utils.DrawArrowHead(from, to, 0.5f);
            }*/
        }
    }
}