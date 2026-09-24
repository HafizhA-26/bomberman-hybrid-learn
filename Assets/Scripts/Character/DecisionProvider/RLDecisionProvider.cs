using BombermanRL.Props;
using System;
using Unity.MLAgents;
using UnityEngine;

namespace BombermanRL.Character
{
    /// <summary>
    /// ML-Agents-backed opponent brain. Bridges <see cref="IDecisionProvider"/> lifecycle
    /// callbacks (move, bomb, kill, death, win, reset) to <see cref="AgentBomber"/> reward
    /// shaping and episode stats, and forwards each requested observation to the agent for
    /// inference via <see cref="Decide"/>. The actual action selection happens inside
    /// ML-Agents; <see cref="OnRequestDecided"/> just relays the result onward.
    /// </summary>
    public class RLDecisionProvider : IDecisionProvider
    {
        private readonly AgentBomber _agent;
        private readonly AgentParameter _agentParameter;
        private GameplayState _currentState;
        private int _bumpedMoveCount = 0; // Consecutive count of moves blocked by a wall/obstacle; used to penalize repeated bumping instead of a single bump.

        // Stats variable
        private int _bombPlacedCount = 0;
        private int _bombPlacedNearPlayer = 0;
        private int _stepsAlive = 0;
        private int _destroyCrateCount = 0;
        private int _dodgeBombCount = 0;
        private int _bumpedMoveStatCount = 0;
        private int _killCount = 0;
        private int _deadCount = 0;
        private int _suicideCount = 0;
        private int _winCount = 0;
        private Action<ActionType> _onDecidedAction;

        public Action<ActionType> OnDecidedAction { get => _onDecidedAction; set => _onDecidedAction = value; }

        /// <param name="agent">The ML-Agents component this provider drives; subscribes to its decided-action callback for the lifetime of this provider.</param>
        /// <param name="agentParameter">Tunable agent action parameters</param>
        /// <param name="onDecidedAction">Callback invoked once ML-Agents resolves an action for the current step.</param>
        public RLDecisionProvider(AgentBomber agent, AgentParameter agentParameter, Action<ActionType> onDecidedAction) 
        {
            _agent = agent;
            _agent.OnActionDecided += OnRequestDecided;
            _agentParameter = agentParameter;
            _onDecidedAction = onDecidedAction;
        }

        /// <summary>
        /// Applies a small per-step penalty (encourages efficient play), hands the current
        /// observation to the agent, and requests an ML-Agents decision. The actual action
        /// arrives asynchronously via <see cref="OnRequestDecided"/>.
        /// </summary>
        public void Decide(GameplayState state)
        {
            _currentState = state;
            _stepsAlive++;
            _agent.AddReward(-0.003f); // Step penalty
            _agent.SetGameplayState(state);

            _agent.RequestDecision();
        }

        public void OnDestroy()
        {
            _agent.OnActionDecided -= OnRequestDecided;
        }

        /// <summary>
        /// Rewards destroying a crate specifically (as opposed to other destructible prop types, which grant no reward here).
        /// </summary>
        /// <param name="prop">Destroyed prop</param>
        public void OnDestroyProps(IDestroyableProps prop)
        {
            if(prop.PropType == TileType.Crate)
            {
                _agent.AddReward(0.1f);
                _destroyCrateCount++;
            }
        }

        /// <summary>
        /// Rewards a normal kill heavily, penalizes friendly fire, and treats suicide as neutral here (suicide's own penalty is applied in <see cref="OnDead"/>).
        /// </summary>
        /// <param name="killType">Type of kill by entity bomb</param>
        public void OnKillSomeone(KillType killType)
        {
            switch (killType)
            {
                case KillType.NormalKill:
                    _killCount++;
                    _agent.AddReward(4f);
                    break;
                case KillType.FriendlyFire:
                    _agent.AddReward(-0.5f);
                    break;
                case KillType.Suicide:
                    break;
            }
        }

        /// <summary>
        /// ML-Agents' decided-action callback; simply forwards the chosen action to whoever is listening on <see cref="OnDecidedAction"/> (typically <see cref="MatchDirector"/>/the entity).
        /// </summary>
        /// <param name="action">Action to take</param>
        public void OnRequestDecided(ActionType action)
        {
            _onDecidedAction?.Invoke(action);
        }

        /// <summary>
        /// Penalizes death, more heavily for suicide than being killed by an opponent.
        /// </summary>
        /// <param name="isSuicide">Is suicidal dead?</param>
        public void OnDead(bool isSuicide)
        {
            if (isSuicide)
            {
                _agent.AddReward(-4f);
                _suicideCount++;
            }
            else _agent.AddReward(-1f);
            _deadCount++;
        }

        /// <summary>
        /// Small reward for placing a bomb at all, plus a bonus if it was placed near the player.
        /// </summary>
        public void OnPlaceBomb()
        {
            _agent.AddReward(0.01f);
            if (_currentState.EntityPos.Distance(_currentState.EntityPos) <= _agentParameter.OffensiveDistance)
            {
                _agent.AddReward(0.15f);
                _bombPlacedNearPlayer++;
            }

            _bombPlacedCount++;
        }

        /// <summary>
        /// Penalizes repeatedly bumping into an obstacle (2+ blocked moves in a row) rather than a single blocked move, to avoid over-punishing one-off misclicks/random exploration.
        /// </summary>
        /// <param name="canMove">Can actually move or bumped something</param>
        public void OnMove(bool canMove)
        {
            // Check rewarding bumped move
            if (!canMove)
            {
                _bumpedMoveCount++;
                _bumpedMoveStatCount++;
            }
            else
                _bumpedMoveCount = 0;

            if (_bumpedMoveCount >= 2)
                _agent.AddReward(-0.02f);

        }

        /// <summary>
        /// Small penalty for attempting an action the game rejected (currently only tracked for bomb placement).
        /// </summary>
        /// <param name="actionType">Action to take</param>
        public void OnInvalidAction(ActionType actionType)
        {
            switch(actionType)
            {
                case ActionType.PlaceBomb:
                    _agent.AddReward(-0.01f);
                    break;
            }
        }

        /// <summary>
        /// End-of-episode hook: flushes this episode's stats to the ML-Agents
        /// <see cref="StatsRecorder"/> for TensorBoard, ends the ML-Agents episode,
        /// </summary>
        public void OnReset()
        {
            // Record custom stats to tensorboard
            Academy.Instance.StatsRecorder.Add("Enemy/StepsAlive", _stepsAlive);
            Academy.Instance.StatsRecorder.Add("Enemy/BumpedMove", _bumpedMoveStatCount);
            Academy.Instance.StatsRecorder.Add("Enemy/BombPlaced", _bombPlacedCount);
            Academy.Instance.StatsRecorder.Add("Enemy/GoodBombPlaced", _bombPlacedNearPlayer);
            Academy.Instance.StatsRecorder.Add("Enemy/DodgedBomb", _dodgeBombCount);
            Academy.Instance.StatsRecorder.Add("Enemy/DestroyCrates", _destroyCrateCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Kills", _killCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Deaths", _deadCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Suicides", _suicideCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Win", _winCount);

            _agent.EndEpisode();

            // Reset stats
            _bombPlacedCount = 0;
            _bombPlacedNearPlayer = 0;
            _stepsAlive = 0;
            _bumpedMoveStatCount = 0;
            _destroyCrateCount = 0;
            _dodgeBombCount = 0;
            _killCount = 0;
            _deadCount = 0;
            _suicideCount = 0;
            _winCount = 0;
            
        }

        /// <summary>
        /// Rewards winning the episode (last one standing).
        /// </summary>
        public void OnWin()
        {
            _winCount++;
            _agent.AddReward(1.5f);
        }

    }
}
