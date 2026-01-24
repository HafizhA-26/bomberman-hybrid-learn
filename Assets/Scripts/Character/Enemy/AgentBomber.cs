using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace BombermanRL.Character
{
    public class AgentBomber : Agent
    {
        private GameplayState _currentState;
        public event Action<ActionType> OnActionDecided;

        public override void Initialize()
        {
            
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (_currentState == null) return;

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
                        sensor.AddObservation(tile.Type == TileType.Wall ? 1f : 0f);
                        sensor.AddObservation(tile.Type == TileType.Crate ? 1f : 0f);
                        sensor.AddObservation(tile.HasSubstate(TileSubState.OnBomb) ? 1f : 0f);
                        sensor.AddObservation(tile.HasSubstate(TileSubState.OnExplosion) ? 1f : 0f);
                        sensor.AddObservation(_currentState.BombTimerNorm.ContainsKey(pos) ? _currentState.BombTimerNorm[pos] : 0f);
                    }
                    else
                    {
                        sensor.AddObservation(1f);
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

        public override void OnActionReceived(ActionBuffers actions)
        {
            int action = actions.DiscreteActions[0];
            ActionType type = (ActionType)action;
            OnActionDecided?.Invoke(type);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

            // Get Arrow input for movement (avoid using WASD, prevent conflicted input with player input)
            bool moveUp = Input.GetKey(KeyCode.UpArrow);
            bool moveDown = Input.GetKey(KeyCode.DownArrow);
            bool moveLeft = Input.GetKey(KeyCode.LeftArrow);
            bool moveRight = Input.GetKey(KeyCode.RightArrow);
            bool bombInput = Input.GetKey(KeyCode.Backslash);

            // Only take one action at the same time for heuristic
            if (moveUp)
                discreteActions[0] = 1;
            else if (moveDown)
                discreteActions[0] = 2;
            else if (moveLeft)
                discreteActions[0] = 3;
            else if (moveRight)
                discreteActions[0] = 4;
            else if (bombInput)
                discreteActions[0] = 5;
            else
                discreteActions[0] = 0;

        }

        public override void OnEpisodeBegin()
        {
            
        }

        public void SetGameplayState(GameplayState state)
        {
            _currentState = state;
        }

    }

}
