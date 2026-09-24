using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    /// <summary>
    /// Human-controlled entity. Binds Unity's generated Input System actions
    /// (<see cref="PlayerInputActions.IGameplayActions"/>) to move/bomb requests, gated by
    /// the entity being <see cref="EntityState.Idle"/> and the action cooldown having
    /// elapsed — so input during a walk/bomb animation is simply ignored rather than queued.
    /// </summary>
    public class PlayerController : BombermanEntity, PlayerInputActions.IGameplayActions
    {
        private PlayerInputActions _inputAction;
        // Latest raw move input, snapped to a single cardinal axis; consumed every frame in Update.
        private Vector2 _currentMoveInput;

        protected new void Awake()
        {
            base.Awake();
            _inputAction = new PlayerInputActions();
            _actionCooldown = new ActionCooldown(_agentParameter.ActionCooldown);
            _inputAction.Gameplay.SetCallbacks(this);
        }

        private void OnEnable()
        {
            _inputAction.Gameplay.Enable();
        }

        private void OnDisable()
        {
            _inputAction.Gameplay.Disable();
        }

        /// <summary>
        /// Input System move callback. Snaps the raw analog/digital input to a single
        /// cardinal direction: horizontal input takes priority, vertical is only used when
        /// there's no meaningful horizontal input — so a diagonal input never produces a
        /// diagonal move.
        /// </summary>
        /// <param name="context">Input System callback context carrying the raw Vector2 move value.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            float x = Mathf.Round(moveInput.x);
            float y = Mathf.Abs(x) < 0.5 ? Mathf.Round(moveInput.y) : 0;
            _currentMoveInput = new Vector2(x, y);
        }

        /// <summary>
        /// Input System bomb-placement callback. Ignored unless the entity is idle, has bombs left, and the action cooldown has elapsed.
        /// </summary>
        /// <param name="context">Input System callback context for the bomb-placement action.</param>
        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(_currentState != EntityState.Idle || BombCount <= 0 || !_actionCooldown.CanAction()) return;
            OnRequestPlaceBomb();
        }

        private void Update()
        {
            if(_currentMoveInput != Vector2.zero)
            {
                if (_currentState != EntityState.Idle || !_actionCooldown.CanAction()) return;
                OnRequestMove(_currentMoveInput);
            }
        }

        /// <summary>
        /// Pauses/resumes this entity, additionally disabling/enabling gameplay input entirely while paused.
        /// </summary>
        /// <param name="pause">True to pause, false to resume.</param>
        public override void PauseCharacter(bool pause)
        {
            base.PauseCharacter(pause);
            if (pause) _inputAction.Gameplay.Disable();
            else _inputAction.Gameplay.Enable();
        }
    }
}
