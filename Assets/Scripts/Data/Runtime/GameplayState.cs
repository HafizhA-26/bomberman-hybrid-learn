using System.Collections.Generic;

namespace BombermanRL
{
    public class GameplayState
    {
        public GameplayState(GridPos entityPos, Dictionary<GridPos, TileState> nearbyCondition, GridPos playerPos, List<float> bombTimerNorm)
        {
            EntityPos = entityPos;
            NearbyCondition = nearbyCondition;
            PlayerPos = playerPos;
            BombTimerNorm = bombTimerNorm;
        }

        public GridPos EntityPos { get; private set; } 
        public Dictionary<GridPos, TileState> NearbyCondition { get; private set; } // Nearby tiles condition (Includes Current Tiles)
        public GridPos PlayerPos { get; private set; }
        public List<float> BombTimerNorm { get; private set; } // In List form, incase bomberman chara can spawn > 1 bomb

        public override string ToString()
        {
            string strNearbyCon = "";
            string strBombTimerNorm = "";
            foreach (KeyValuePair<GridPos, TileState> item in NearbyCondition)
            {
                strNearbyCon += $"|{item.Key} - {item.Value}| ";
            }
            for (int i = 0; i < BombTimerNorm.Count; i++)
            {
                strBombTimerNorm += $"|Bomb {i} : {BombTimerNorm[i]}| ";
            }

            return $"Entity Pos : {EntityPos}\n" +
                $"Nearby Condition : {strNearbyCon}\n" +
                $"Player Pos: {PlayerPos}\n" +
                $"Bomb Timer Norm: {strBombTimerNorm}\n";
        }
    }
}
