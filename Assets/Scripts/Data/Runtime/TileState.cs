namespace BombermanRL
{
    public class TileState
    {
        private TileType _type;
        private TileSubState _subState;

        public TileState(TileType defaultType)
        {
            _type = defaultType;
        }

        public TileType Type { get => _type; set => _type = value; }

        public bool HasSubstate(TileSubState subState) => _subState.HasFlag(subState);
        public void AddSubstate(TileSubState subState) => _subState |= subState;
        public void RemoveSubstate(TileSubState subState) => _subState &= ~subState;
    }
}
