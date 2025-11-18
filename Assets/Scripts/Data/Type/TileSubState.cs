using System;

namespace BombermanRL
{
    [Flags]
    public enum TileSubState
    {
        None = 0,
        OnPlayer = 1 << 0,
        OnBomb = 1 << 1,
        OnExplosion = 1 << 2,
        OnDestroyedCrate = 1 << 3,
    }
}
