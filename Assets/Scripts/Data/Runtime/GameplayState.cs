using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL
{
    public class GameplayState
    {
        public GameplayState(GridPos entityPos, Dictionary<GridPos, TileState> nearbyCondition, GridPos playerPos, Dictionary<GridPos, float> bombTimerNorm, int observationRadius)
        {
            EntityPos = entityPos;
            NearbyCondition = nearbyCondition;
            PlayerPos = playerPos;
            BombTimerNorm = bombTimerNorm;
            ObservationRadius = observationRadius;
        }

        public int ObservationRadius { get; private set; }
        public GridPos EntityPos { get; private set; } // Related entity position that requested gameplay state
        public Dictionary<GridPos, TileState> NearbyCondition { get; private set; } // Nearby tiles condition (Includes Current Tiles)
        public GridPos PlayerPos { get; private set; } // Player position inside grid
        public Dictionary<GridPos, float> BombTimerNorm { get; private set; } // Nearby placed bomb timer

        public bool TryGetTileState(GridPos pos, out TileState state)
        {
            if(NearbyCondition.ContainsKey(pos))
            {
                state = NearbyCondition[pos];
                return true;
            }
            else
            {
                state = null;
                return false;
            }
        }

        public override string ToString()
        {
            string strNearbyCon = "";
            string strBombTimerNorm = "";
            foreach (KeyValuePair<GridPos, TileState> item in NearbyCondition)
            {
                strNearbyCon += $"|{item.Key} - {item.Value}| ";
            }
            foreach (KeyValuePair<GridPos, float> item in BombTimerNorm)
            {
                strBombTimerNorm += $"|Bomb {item.Key} : {item.Value}| ";
            }

            return $"Entity Pos : {EntityPos}\n" +
                $"Nearby Condition : {strNearbyCon}\n" +
                $"Player Pos: {PlayerPos}\n" +
                $"Bomb Timer Norm: {strBombTimerNorm}\n";
        }
    }
}
