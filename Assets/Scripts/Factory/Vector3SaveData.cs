using System;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [Serializable]
    public struct Vector3SaveData
    {
        public float x;
        public float y;
        public float z;

        public Vector3SaveData(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // From Vector3 → SaveData
        public static implicit operator Vector3SaveData(Vector3 v)
        {
            return new Vector3SaveData(v.x, v.y, v.z);
        }

        // From SaveData → Vector3
        public static implicit operator Vector3(Vector3SaveData v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }
}