using System;
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [Serializable]
    public struct GridPosition
    {
        public int x;
        public int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator GridPosition(Vector2Int v)
        {
            return new GridPosition(v.x, v.y);
        }

        public static implicit operator Vector2Int(GridPosition p)
        {
            return new Vector2Int(p.x, p.y);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}