using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    public class ConveyorCorner : Conveyor
    {
        public void SetSpeedSign(int speedSign)
        {
            _graphic.transform.GetChild(0).GetComponent<MeshRenderer>().materials[0].SetFloat("_Speed", speedSign);
        }
    }
}