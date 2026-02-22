using BombermanRL.Props;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Character
{
    public class RuleBasedDecision : IDecisionProvider
    {
        private int _offensiveDistance = 3;
        private float _dangerBombThreshold = 0.4f;
        //private float _foolsChance = 0.4f;

        public RuleBasedDecision() { }
        public RuleBasedDecision(int offensiveDistance, float dangerBombThreshold) 
        {
            _offensiveDistance = offensiveDistance;
            _dangerBombThreshold = dangerBombThreshold;
        }


        public ActionType Decide(GameplayState state)
        {
            ActionType actionToTake;

            // Fools Chance
            //if (Random.value < _foolsChance)
            //    return (ActionType)Random.Range(0, 5);

            //Debug.Log("Check Survival");
            // Priority 1: Survival
            actionToTake = CheckSurvival(state);
            if (actionToTake != ActionType.Idle) return actionToTake;

            //Debug.Log("Check Offensive");
            // Priority 2: Offensive
            actionToTake = CheckOffensive(state);
            if (actionToTake != ActionType.Idle) return actionToTake;

            //Debug.Log("Check Destrcutive");
            // Priority 3: Destroy Environment
            actionToTake = CheckDestructive(state);
            if (actionToTake != ActionType.Idle) return actionToTake;

            //Debug.Log("Check Exploration");
            // Priority 4: Exploration
            actionToTake = CheckExploration(state);
            return actionToTake;
        }

        private ActionType CheckSurvival(GameplayState state)
        {
            ActionType actionToTake = ActionType.Idle;
            List<GridPos> safeTiles = new List<GridPos>();
            List<GridPos> dangerousTiles = new List<GridPos>();
            Dictionary<GridPos, TileState> nearby = state.NearbyCondition;

            // Find dangerous and safe tiles in nearby observed tiles
            foreach (KeyValuePair<GridPos, TileState> tile in nearby)
            {
                bool isExplosion = tile.Value.HasSubstate(TileSubState.OnExplosion);
                bool isBombDanger = tile.Value.HasSubstate(TileSubState.OnBomb) && state.BombTimerNorm[tile.Key] > _dangerBombThreshold;

                if (isExplosion || isBombDanger)
                    dangerousTiles.Add(tile.Key);
                else if (tile.Value.Type == TileType.Empty && state.EntityPos.Distance(tile.Key) == 1 && TileState.IsWalkable(tile.Value))
                    safeTiles.Add(tile.Key);
            }

            GridPos dangerousTile = new GridPos(-1, -1);
            int closestDis = int.MaxValue;
            if (dangerousTiles.Count > 0)
            {
                // Search for most closest distance dangerous tile
                foreach (GridPos item in dangerousTiles)
                {
                    int dis = state.EntityPos.Distance(item);
                    if (dis < closestDis)
                    {
                        dangerousTile = item;
                        closestDis = dis;
                    }
                }

                GridPos safestTile = state.EntityPos;
                int farthestDis = state.EntityPos.Distance(dangerousTile);
                // Search for safe tile that farthest from dangerous tile
                foreach (GridPos item in safeTiles)
                {
                    int dis = item.Distance(dangerousTile);
                    if (dis > farthestDis )
                    {
                        safestTile = item;
                        farthestDis = dis;
                    }
                }
                //Debug.Log($"Safest Tile is {safestTile} with distance from {dangerousTile}: {farthestDis}");

                // Get entity direction to move into safest tile
                Vector2 direction = (safestTile - state.EntityPos).ToVector2();
                actionToTake = DirectionToActionMove(direction);
            }

            return actionToTake;
        }

        private ActionType CheckOffensive(GameplayState state)
        {
            ActionType actionToTake = ActionType.Idle;
            bool isPlayerNearby = false;
            bool isSafeTileExists = false;

            // Filter nearby condition based on _offensiveRadius
            Dictionary<GridPos, TileState> nearby = state.NearbyCondition;
            foreach (KeyValuePair<GridPos, TileState> item in nearby)
            {
                if (state.EntityPos.Distance(item.Key) <= _offensiveDistance && item.Key.Equals(state.PlayerPos))
                    isPlayerNearby = true;
                if (state.EntityPos.Distance(item.Key) == 1 && item.Value.Type == TileType.Empty)
                    isSafeTileExists = true;
            }

            TileState curTileState = nearby[state.EntityPos];

            // Place Bomb if player nearby
            if (isPlayerNearby && isSafeTileExists && !curTileState.HasSubstate(TileSubState.OnBomb))
                actionToTake = ActionType.PlaceBomb;

            return actionToTake;
        }

        private ActionType CheckDestructive(GameplayState state)
        {
            ActionType actionToTake = ActionType.Idle;

            // Filter only get top,right,bottom,left tile condition
            Dictionary<GridPos, TileState> nearby = state.NearbyCondition
               .Where(item => item.Key.Distance(state.EntityPos) <= 1 && item.Value.Type == TileType.Crate)
               .ToDictionary(pair => pair.Key, pair => pair.Value);

            if (nearby.Count > 0)
                actionToTake = ActionType.PlaceBomb;

            return actionToTake;
        }

        private ActionType CheckExploration(GameplayState state)
        {
            ActionType actionToTake = ActionType.Idle;

            List<GridPos> nearby = state.NearbyCondition
              .Where(item => item.Key.Distance(state.EntityPos) == 1 && item.Value.Type == TileType.Empty)
              .Select(item => item.Key)
              .ToList();

            //nearby = nearby.OrderBy(item => item.Distance(state.PlayerPos)).Take(3).ToList();

            if (nearby.Count > 0)
            {
                int randomMove = Random.Range(0, nearby.Count);
                Vector2 direction = (nearby[randomMove] - state.EntityPos).ToVector2();
                actionToTake = DirectionToActionMove(direction);
            }
            return actionToTake;
        }

        public static ActionType DirectionToActionMove(Vector2 direction)
        {
            ActionType actionToTake = ActionType.Idle;
            if (direction == Vector2.up) actionToTake = ActionType.MoveUp;
            else if (direction == Vector2.down) actionToTake = ActionType.MoveDown;
            else if (direction == Vector2.left) actionToTake = ActionType.MoveLeft;
            else if (direction == Vector2.right) actionToTake = ActionType.MoveRight;
            else actionToTake = ActionType.Idle;
            return actionToTake;
        }

        public void OnDestroy() { }

        public void OnDestroyProps(IDestroyableProps prop) { }

        public void OnKillSomeone(IBombermanCharacter character) { }

        public void OnDead(bool isSuicide) { }

        public void OnPlaceBomb() { }

        public void OnMove(bool canMove) { }

        public void OnReset() { }

        public void OnWin() { }

    }
}
