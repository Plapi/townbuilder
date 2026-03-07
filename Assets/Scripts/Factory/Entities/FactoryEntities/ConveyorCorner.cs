using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ConveyorCorner : Conveyor
    {
        private int _beltDirection;

        public override int BeltDirection => _beltDirection;

        public override void SetBeltDirection(int beltDirection)
        {
            base.SetBeltDirection(beltDirection);
            _graphic.transform.GetChild(0).GetComponent<MeshRenderer>().materials[0].SetFloat("_Speed", beltDirection);
            _beltDirection = beltDirection;
        }
    }
}