using BombermanRL.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Character
{
    /// <summary>
    /// Scripted (non-ML) opponent brain. Each decision step evaluates a fixed priority
    /// ladder — survival, then offense, then destroying crates, then exploring — and takes
    /// the first non-idle action found. Driven entirely by <see cref="GameplayState"/>
    /// snapshots supplied by <see cref="GridStateManager.GetNearbyState"/>; holds no
    /// reference to the grid itself.
    /// </summary>
    public class RuleBasedDecision : IDecisionProvider
    {
        private readonly AgentParameter _agentParameter;
        private Action<ActionType> _onDecidedAction;
        public Action<ActionType> OnDecidedAction { get => _onDecidedAction; set => _onDecidedAction = value; }

        /// <param name="agentParameter">Tunable thresholds (danger sensitivity, offensive range, etc.); randomized on construction and on every reset for behavioral variety.</param>
        /// <param name="onDecidedAction">Callback invoked with the chosen action once <see cref="Decide"/> resolves it.</param>
        public RuleBasedDecision(AgentParameter agentParameter, Action<ActionType> onDecidedAction) 
        {
            _agentParameter = agentParameter;
            _agentParameter.RandomizeParameter();
            _onDecidedAction = onDecidedAction;
        }

        /// <summary>
        /// Runs the priority ladder against the current observation and fires
        /// <see cref="OnDecidedAction"/> with the first non-idle result: survival beats
        /// offense beats crate-clearing beats free exploration. <see cref="CheckSurvival"/>
        /// can also force a deliberate idle (<c>mustIdle</c>) when staying put is itself
        /// the safest option.
        /// </summary>
        /// <param name="state">Nearby observed tile condition. Provided by <see cref="IGameplayStateProvider"/></param>
        public void Decide(GameplayState state)
        {
            ActionType actionToTake;
            bool mustIdle;
            // Priority 1: Survival
            (actionToTake, mustIdle) = CheckSurvival(state);
            if (actionToTake != ActionType.Idle || mustIdle)
            {
                _onDecidedAction?.Invoke(actionToTake);
                return;
            }

            // Priority 2: Offensive
            actionToTake = CheckOffensive(state);
            if (actionToTake != ActionType.Idle)
            {
                _onDecidedAction?.Invoke(actionToTake);
                return;
            }

            // Priority 3: Destroy Environment
            actionToTake = CheckDestructive(state);
            if (actionToTake != ActionType.Idle)
            {
                _onDecidedAction?.Invoke(actionToTake);
                return;
            }

            // Priority 4: Exploration
            actionToTake = CheckExploration(state);
            if(actionToTake != ActionType.Idle)
            {
                _onDecidedAction?.Invoke(actionToTake);
            }
        }

        /// <summary>
        /// Highest-priority check: if any tile is currently exploding, or holds a
        /// soon-to-detonate bomb, this steers the agent toward the best reachable safe
        /// neighbor tile — or deliberately idles if the current tile is already the safest
        /// option available.
        /// </summary>
        /// <param name="state">Nearby observed tile condition. Provided by <see cref="IGameplayStateProvider"/></param>
        /// <returns>Action to take</returns>
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

            // Find the safest tiles with simple A* Algorithm
            if (dangerousTiles.Count > 0)
            {
                GridPos safestTile = state.EntityPos;
                int bestSafeScore = int.MinValue;

                // If current tiles not dangerous, add it also as safe tile
                if (!dangerousTiles.Contains(state.EntityPos))
                    safeTiles.Add(state.EntityPos);

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

                // Calculate move direction or must idle to be safe
                if (!safestTile.Equals(state.EntityPos))
                {
                    Vector2 direction = (safestTile - state.EntityPos).ToVector2();
                    actionToTake = Util.GetActionFromDirection(direction);
                }
                else
                {
                    isMustIdle = true;
                }
            }

            return (actionToTake, isMustIdle);
        }

        /// <summary>
        /// Second priority: if the (tracked) player is within offensive range and the
        /// agent has an adjacent safe tile to retreat to and hasn't already got a bomb
        /// down, place a bomb.
        /// </summary>
        /// <param name="state">Nearby observed tile condition. Provided by <see cref="IGameplayStateProvider"/></param>
        /// <returns>Action to take</returns>
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

        /// <summary>
        /// Third priority: place a bomb if any orthogonally-adjacent tile is a destructible crate.
        /// </summary>
        /// <param name="state">Nearby observed tile condition. Provided by <see cref="IGameplayStateProvider"/></param>
        /// <returns>Action to take</returns>
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

        /// <summary>
        /// Lowest priority fallback: pick a random adjacent empty tile to move into, so the agent keeps exploring when nothing more urgent applies
        /// </summary>
        /// <param name="state">Nearby observed tile condition. Provided by <see cref="IGameplayStateProvider"/></param>
        /// <returns>Action to take</returns>
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
                actionToTake = Util.GetActionFromDirection(direction);
            }
            return actionToTake;
        }

        public void OnInvalidAction(ActionType actionType) { }

        public void OnDestroy() { }

        public void OnDestroyProps(IDestroyableProps prop) { }

        public void OnKillSomeone(KillType killType) { }

        public void OnDead(bool isSuicide) { }

        public void OnPlaceBomb() { }

        public void OnMove(bool canMove) { }

        public void OnReset() 
        {
            // Re-rolls this agent's behavioral parameters at the start of a new episode, for training/variety.
            _agentParameter.RandomizeParameter();
        }

        public void OnWin() { }

        
    }
}
