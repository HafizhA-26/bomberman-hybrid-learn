using System;
using UnityEngine;

namespace BombermanRL
{
    public struct GridPos
    {
        public int row, col;
        public GridPos(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public int Distance(GridPos pos)
        {
            return Mathf.Abs(pos.row - row) + Mathf.Abs(pos.col - col);
        }

        public Vector2 ToVector2()
        {
            return new Vector2(col, row * -1);
        }

        public static GridPos operator -(GridPos a, GridPos b)
        {
            return new GridPos(a.row - b.row, a.col - b.col);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GridPos)) return false;
            GridPos pos = (GridPos)obj;
            return row == pos.row && col == pos.col;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(row, col);
        }

        public override readonly string ToString()
        {
            return $"[{row},{col}]";
        }

    }
}
