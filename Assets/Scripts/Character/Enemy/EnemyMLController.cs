using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class EnemyMLController : EnemyController, PlayerInputActions.IEnemyHeuristicActions
    {
        [SerializeField] private bool _enableHeuristicAction = true;

        private PlayerInputActions _inputAction;
        private AgentBomber _mlAgent;

        private void Awake()
        {
            _AIType = AIType.MLAgent;
            _mlAgent = GetComponent<AgentBomber>();
            _inputAction = new PlayerInputActions();
            if(_enableHeuristicAction) _inputAction.EnemyHeuristic.SetCallbacks(this);

            _decisionProvider = new RLDecisionProvider(_mlAgent, _agentParameter);
        }

        private void OnEnable()
        {
            if (_enableHeuristicAction) _inputAction.EnemyHeuristic.Enable();
        }

        private void OnDisable()
        {
            if (_enableHeuristicAction) _inputAction.EnemyHeuristic.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_currentState != EntityState.Idle && !_actionCooldown.CanAction()) return;
            _mlAgent.OnHeuristicInput(context.ReadValue<Vector2>(), false);
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if (_currentState != EntityState.Idle || BombCount >= _agentParameter.BombLimit || !_actionCooldown.CanAction()) return;
            if (context.performed)
                _mlAgent.OnHeuristicInput(Vector2.zero, true);
        }
    }
}
