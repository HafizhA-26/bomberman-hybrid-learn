using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace BombermanRL.Character
{
    /// <summary>
    /// ML-Agents <see cref="Agent"/> implementation for the AI opponent. Turns a
    /// <see cref="GameplayState"/> snapshot (set by <see cref="RLDecisionProvider.Decide"/>)
    /// into a per-tile vector observation for the neural network, and converts the
    /// network's single discrete action index back into an <see cref="ActionType"/> via
    /// <see cref="OnActionDecided"/>. Also supports a heuristic (manually driven) mode for
    /// testing without a trained model, via <see cref="Heuristic"/>/<see cref="OnHeuristicInput"/>.
    public class AgentBomber : Agent
    {
        // The observation this agent will report on its next CollectObservations pass; supplied externally via SetGameplayState.
        private GameplayState _currentState;
        // Buffered move/bomb input for heuristic (manual) control, consumed once by Heuristic() and then cleared for bomb.
        private Vector2 _disceteMove;
        private bool _discretePlaceBomb;

        // Raised once ML-Agents (or the heuristic path) has resolved a discrete action for the current step.
        public event Action<ActionType> OnActionDecided;

        /// <summary>
        /// ML-Agents lifecycle hook: sets a sensible frame rate cap once per agent instantiation (lower on lower-end devices) and ensures normal time scale.
        /// </summary>
        public override void Initialize()
        {
            Time.timeScale = 1f;
            if (GameInstance.Instance.DeviceType == 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = 30;
        }

        /// <summary>
        /// Builds the observation vector for the current <see cref="_currentState"/>: for
        /// every tile in a (2r+1)x(2r+1) square around the agent (r = <see cref="GameplayState.ObservationRadius"/>),
        /// adds one-hot-ish flags for wall / crate / bomb / explosion / other-character
        /// presence plus the tile's normalized bomb timer (or all-zero-except-wall for any
        /// tile outside the tracked grid), then appends the player's row/column offset
        /// normalized by the radius. No-ops (adds nothing) if no state has been set yet.
        /// </summary>
        /// <param name="sensor">ML-Agents vector sensor to append observations to.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (_currentState == null) return;

            // Calculate player dis
            int r = _currentState.ObservationRadius;
            float playerRowDis = (float) (_currentState.PlayerPos.row - _currentState.EntityPos.row) / r;
            float playerColDis = (float) (_currentState.PlayerPos.col - _currentState.EntityPos.col) / r;

            for (int rowOffset = -r; rowOffset <= r; rowOffset++)
            {
                for (int colOffset = -r; colOffset <= r; colOffset++)
                {
                    GridPos pos = _currentState.EntityPos + (rowOffset, colOffset);

                    if(_currentState.TryGetTileState(pos, out TileState tile))
                    {
                        bool hasOtherCharacrer = tile.HasSubstate(TileSubState.OnCharacter) && !pos.Equals(_currentState.EntityPos);
                        sensor.AddObservation(tile.Type == TileType.Wall ? 1f : 0f);
                        sensor.AddObservation(tile.Type == TileType.Crate ? 1f : 0f);
                        sensor.AddObservation(tile.HasSubstate(TileSubState.OnBomb) ? 1f : 0f);
                        sensor.AddObservation(tile.HasSubstate(TileSubState.OnExplosion) ? 1f : 0f);
                        sensor.AddObservation(hasOtherCharacrer ? 1f : 0f);
                        sensor.AddObservation(_currentState.BombTimerNorm.ContainsKey(pos) ? _currentState.BombTimerNorm[pos] : 0f);
                    }
                    else
                    {
                        // Tile is outside the tracked grid (off the edge of the arena) — treat it as a wall so the agent learns to avoid walking off-grid.
                        sensor.AddObservation(1f);
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                        sensor.AddObservation(0f);
                    }
                }
            }
            sensor.AddObservation(playerRowDis);
            sensor.AddObservation(playerColDis);
        }

        /// <summary>
        /// ML-Agents lifecycle hook: receives the network's chosen discrete action index for this step and raises <see cref="OnActionDecided"/> with it converted to an <see cref="ActionType"/>.
        /// </summary>
        /// <param name="actions">Action buffer containing the single discrete action chosen by the policy.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            int action = actions.DiscreteActions[0];
            ActionType type = (ActionType)action;
            OnActionDecided?.Invoke(type);
        }

        /// <summary>
        /// ML-Agents lifecycle hook for manual/heuristic control (used when no trained
        /// model is assigned, or heuristic mode is forced): translates the buffered
        /// <see cref="_disceteMove"/>/<see cref="_discretePlaceBomb"/> input into the same
        /// discrete action index space <see cref="OnActionReceived"/> expects. Movement
        /// takes priority over bombing, and only one action is emitted per call — vertical
        /// move is checked before horizontal, so a simultaneous vertical+horizontal input
        /// resolves to vertical.
        /// </summary>
        /// <param name="actionsOut">Action buffer to write the resolved discrete action into.</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

            // Only take one action at the same time for heuristic
            if (_disceteMove.y > 0)
                discreteActions[0] = 1;
            else if (_disceteMove.y < 0)
                discreteActions[0] = 2;
            else if (_disceteMove.x < 0)
                discreteActions[0] = 3;
            else if (_disceteMove.x > 0)
                discreteActions[0] = 4;
            else if (_discretePlaceBomb)
            {
                discreteActions[0] = 5;
                _discretePlaceBomb = false;
            }
            else
                discreteActions[0] = 0;
        }

        /// <summary>
        /// Supplies the observation this agent should report on its next <see cref="CollectObservations"/> pass. Called once per decision step by <see cref="RLDecisionProvider.Decide"/>.
        /// </summary>
        /// <param name="state">The current local grid observation around this agent.</param>
        public void SetGameplayState(GameplayState state)
        {
            _currentState = state;
        }

        /// <summary>
        /// Buffers manual input for the next <see cref="Heuristic"/> call — used by <see cref="EnemyMLController"/>'s heuristic input callbacks.
        /// </summary>
        /// <param name="moveInput">Desired move direction, or zero for no movement.</param>
        /// <param name="placeBomb">Whether a bomb placement was requested this call.</param>
        public void OnHeuristicInput(Vector2 moveInput, bool placeBomb)
        {
            _disceteMove = moveInput;
            _discretePlaceBomb = placeBomb;
        }
    }

}
