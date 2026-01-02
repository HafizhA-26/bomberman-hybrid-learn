using System;

namespace BombermanRL
{
    [Flags]
    public enum TileSubState
    {
        None = 0,
        OnPlayer = 1 << 0,
        OnEnemy = 1 << 1,
        OnBomb = 1 << 2,
        OnExplosion = 1 << 3,
        OnDestroyedCrate = 1 << 4,
    }
}
