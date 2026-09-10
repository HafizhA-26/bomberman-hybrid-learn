using BombermanRL.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Character
{
    public class RuleBasedDecision : IDecisionProvider
    {
        private readonly AgentParameter _agentParameter;
        private Action<ActionType> _onDecidedAction;
        public Action<ActionType> OnDecidedAction { get => _onDecidedAction; set => _onDecidedAction = value; }

        public RuleBasedDecision() { }
        public RuleBasedDecision(AgentParameter agentParameter, Action<ActionType> onDecidedAction) 
        {
            _agentParameter = agentParameter;
            _agentParameter.RandomizeParameter();
            _onDecidedAction = onDecidedAction;
        }


        public void Decide(GameplayState state)
        {
            ActionType actionToTake;
            bool mustIdle;
            //Debug.Log("Check Survival");
            // Priority 1: Survival
            (actionToTake, mustIdle) = CheckSurvival(state);
            if (actionToTake != ActionType.Idle || mustIdle)
            {
                _onDecidedAction?.Invoke(actionToTake);
                return;
            }

            //Debug.Log("Check Offensive");
            // Priority 2: Offensive
            actionToTake = CheckOffensive(state);
            if (actionToTake != ActionType.Idle)
            {
                _onDecidedAction?.Invoke(actionToTake);
                return;
            }

            //Debug.Log("Check Destrcutive");
            // Priority 3: Destroy Environment
            actionToTake = CheckDestructive(state);
            if (actionToTake != ActionType.Idle)
            {
                _onDecidedAction?.Invoke(actionToTake);
                return;
            }

            //Debug.Log("Check Exploration");
            // Priority 4: Exploration
            actionToTake = CheckExploration(state);
            if(actionToTake != ActionType.Idle)
            {
                _onDecidedAction?.Invoke(actionToTake);
            }
        }

        private (ActionType, bool) CheckSurvival(GameplayState state)
        {
            bool isMustIdle = false;
            ActionType actionToTake = ActionType.Idle;
            List<GridPos> safeTiles = new List<GridPos>();
            List<GridPos> dangerousTiles = new List<GridPos>();
            Dictionary<GridPos, TileState> nearby = state.NearbyCondition;

            // Find dangerous and safe tiles in nearby observed tiles
            foreach (KeyValuePair<GridPos, TileState> tile in nearby)
            {
                bool isExplosion = tile.Value.HasSubstate(TileSubState.OnExplosion);
                bool isBombDanger = tile.Value.HasSubstate(TileSubState.OnBomb) && state.BombTimerNorm[tile.Key] > _agentParameter.DangerBombThreshold;

                if (isExplosion || isBombDanger)
                    dangerousTiles.Add(tile.Key);
                else if (tile.Value.Type == TileType.Empty && state.EntityPos.Distance(tile.Key) == 1 && TileState.IsWalkable(tile.Value))
                    safeTiles.Add(tile.Key);
            }

            if (dangerousTiles.Count > 0)
            {
                GridPos safestTile = state.EntityPos;
                int bestSafeScore = int.MinValue;

                foreach (GridPos candidateTile in safeTiles)
                {
                    int currentScore = 0;
                    // Scoring safe tiles based on dangerous tile distance
                    foreach (GridPos danger in dangerousTiles)
                    {
                        currentScore += candidateTile.Distance(danger);
                        if (candidateTile.row == danger.row || candidateTile.col == danger.col)
                            currentScore -= 100;
                    }

                    // Scoring safe tiles based on available escape routes
                    int escapeRoutes = nearby.Count(t => t.Key.Distance(candidateTile) == 1 && TileState.IsWalkable(t.Value));
                    currentScore += escapeRoutes * 5;

                    if (currentScore > bestSafeScore)
                    {
                        safestTile = candidateTile;
                        bestSafeScore = currentScore;
                    }
                }

                if (!safestTile.Equals(state.EntityPos))
                {
                    Vector2 direction = (safestTile - state.EntityPos).ToVector2();
                    actionToTake = DirectionToActionMove(direction);
                }
                else
                {
                    isMustIdle = true;
                }
            }

            return (actionToTake, isMustIdle);
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
                if (state.EntityPos.Distance(item.Key) <= _agentParameter.OffensiveDistance && item.Key.Equals(state.PlayerPos))
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

            if (nearby.Count > 0)
            {
                int randomMove = UnityEngine.Random.Range(0, nearby.Count);
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

        public void OnInvalidAction(ActionType actionType) { }

        public void OnDestroy() { }

        public void OnDestroyProps(IDestroyableProps prop) { }

        public void OnKillSomeone(KillType character) { }

        public void OnDead(bool isSuicide) { }

        public void OnPlaceBomb() { }

        public void OnMove(bool canMove) { }

        public void OnReset() 
        {
            _agentParameter.RandomizeParameter();
        }

        public void OnWin() { }

        
    }
}
