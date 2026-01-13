namespace BombermanRL
{
    public struct GridPos
    {
        public GridPos(int row, int col)
        {
            this.row = row;
            this.col = col;
        }
        public int row, col;

        public override readonly string ToString()
        {
            return $"[{row},{col}]";
        }

    }
}
