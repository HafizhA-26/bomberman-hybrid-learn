using System.Collections.Generic;

namespace BombermanRL
{
    public class GameplayState
    {
        public GridPos EntityPos { get; private set; } 
        public Dictionary<GridPos, TileState> NearbyCondition { get; private set; } // Nearby tiles condition (Includes Current Tiles)
        public GridPos PlayerPos { get; private set; }
        public float BombTimerNorm { get; private set; }
    }
}
