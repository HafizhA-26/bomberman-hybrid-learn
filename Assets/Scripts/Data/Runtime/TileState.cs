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

        public TileState(TileType defaultType, TileSubState subsState)
        {
            _type = defaultType;
            _subState = subsState;
        }

        public TileType Type { get => _type; set => _type = value; }
        public TileSubState SubState { get => _subState; }
        public bool HasSubstate(TileSubState subState) => _subState.HasFlag(subState);
        public void AddSubstate(TileSubState subState) => _subState |= subState;
        public void RemoveSubstate(TileSubState subState) => _subState &= ~subState;
        public void ResetSubstate() => _subState = TileSubState.None;
        public override string ToString()
        {
            return $"Type: {_type} | Substate {_subState}";
        }

        public static bool IsWalkable(TileState tileState) =>
            !tileState.HasSubstate(TileSubState.OnCharacter) &&
            !tileState.HasSubstate(TileSubState.OnBomb) &&
            !tileState.HasSubstate(TileSubState.OnExplosion) &&
            tileState.Type == TileType.Empty;
    }
}
