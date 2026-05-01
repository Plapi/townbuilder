using System;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [Serializable]
    public struct QuaternionSaveData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionSaveData(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        // From Quaternion → SaveData
        public static implicit operator QuaternionSaveData(Quaternion q)
        {
            return new QuaternionSaveData(q.x, q.y, q.z, q.w);
        }

        // From SaveData → Quaternion
        public static implicit operator Quaternion(QuaternionSaveData q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z}, {w})";
        }
    }
}