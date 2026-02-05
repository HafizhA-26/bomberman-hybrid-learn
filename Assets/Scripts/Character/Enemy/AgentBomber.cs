using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class AgentBomber : Agent, PlayerInputActions.IEnemyHeuristicActions
    {
        private GameplayState _currentState;
        private PlayerInputActions _inputAction;

        public event Action<ActionType> OnActionDecided;

        private Vector2 _moveInput;
        private bool _placeBomb;

        protected override void Awake()
        {
            base.Awake();
            _inputAction = new PlayerInputActions();
            _inputAction.EnemyHeuristic.SetCallbacks(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _inputAction.EnemyHeuristic.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _inputAction.EnemyHeuristic.Disable();
        }

        public override void Initialize()
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
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


        public override void OnActionReceived(ActionBuffers actions)
        {
            int action = actions.DiscreteActions[0];
            ActionType type = (ActionType)action;
            //Debug.Log("Action Received : " + action);
            OnActionDecided?.Invoke(type);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
            // Only take one action at the same time for heuristic
            if (_moveInput.y > 0)
                discreteActions[0] = 1;
            else if (_moveInput.y < 0)
                discreteActions[0] = 2;
            else if (_moveInput.x < 0)
                discreteActions[0] = 3;
            else if (_moveInput.x > 0)
                discreteActions[0] = 4;
            else if (_placeBomb)
            {
                discreteActions[0] = 5;
                _placeBomb = false;
            }
            else
                discreteActions[0] = 0;

            int action = discreteActions[0];
            //Debug.Log("Action Heuristic : " + action);
            //ActionType type = (ActionType)action;
            //OnActionDecided?.Invoke(type);

        }

        public override void OnEpisodeBegin()
        {
            
        }

        public void SetGameplayState(GameplayState state)
        {
            _currentState = state;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(context.performed)
                _placeBomb = true;
        }
    }

}
