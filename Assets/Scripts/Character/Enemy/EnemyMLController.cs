using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    /// <summary>
    /// ML-Agents-driven enemy. Wires an <see cref="AgentBomber"/> up through
    /// <see cref="RLDecisionProvider"/> instead of the base class's rule-based provider,
    /// and optionally also accepts manual "heuristic" input
    /// (<see cref="PlayerInputActions.IEnemyHeuristicActions"/>) so a human can drive this
    /// enemy directly — useful for testing/demoing without waiting on the trained model.
    /// </summary>
    public class EnemyMLController : EnemyController, PlayerInputActions.IEnemyHeuristicActions
    {
        // Toggle for whether this enemy also listens to manual heuristic input at all (independent of whether the ML model is actually driving decisions).
        [SerializeField] private bool _enableHeuristicAction = true;

        private PlayerInputActions _inputAction;
        private AgentBomber _mlAgent;

        protected new void Awake()
        {
            base.Awake();
            _actionCooldown = new ActionCooldown(_agentParameter.ActionCooldown);
        }

        private void OnEnable()
        {
            if (_enableHeuristicAction) _inputAction.EnemyHeuristic.Enable();
        }

        private void OnDisable()
        {
            if (_enableHeuristicAction) _inputAction.EnemyHeuristic.Disable();
        }

        /// <summary>
        /// Overrides the base rule-based setup: fetches the sibling <see cref="AgentBomber"/>,
        /// optionally wires up heuristic input callbacks, and constructs an
        /// <see cref="RLDecisionProvider"/> as this entity's decision provider instead of
        /// <see cref="RuleBasedDecision"/>.
        /// </summary>
        protected override void InitializeAI()
        {
            _AIType = AIType.MLAgent;
            _mlAgent = GetComponent<AgentBomber>();
            _inputAction = new PlayerInputActions();
            _agentParameter = Instantiate(_agentParameter);
            if (_enableHeuristicAction) _inputAction.EnemyHeuristic.SetCallbacks(this);

            _decisionProvider = new RLDecisionProvider(_mlAgent, _agentParameter, OnDecisionDecided);
        }

        /// <summary>
        /// Manual heuristic move input callback — forwards raw move input straight to the agent's heuristic buffer rather than through the AI decision loop.
        /// </summary>
        /// <param name="context">Input System callback context carrying the raw Vector2 move value.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            if (_currentState != EntityState.Idle || !_actionCooldown.CanAction()) return;
            _mlAgent.OnHeuristicInput(context.ReadValue<Vector2>(), false);
        }

        /// <summary>
        /// Manual heuristic bomb-placement input callback. Only reacts to the input's <c>performed</c> phase, gated by idle state, bomb availability, and cooldown.
        /// </summary>
        /// <param name="context">Input System callback context for the bomb-placement action.</param>
        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if (_currentState != EntityState.Idle || BombCount <= 0 || !_actionCooldown.CanAction()) return;
            if (context.performed)
                _mlAgent.OnHeuristicInput(Vector2.zero, true);
        }
    }
}
