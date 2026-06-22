using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class PlayerController : BombermanEntity, PlayerInputActions.IGameplayActions
    {
        private PlayerInputActions _inputAction;
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

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            float x = Mathf.Round(moveInput.x);
            float y = Mathf.Abs(x) < 0.5 ? Mathf.Round(moveInput.y) : 0;
            _currentMoveInput = new Vector2(x, y);
        }

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
    }
}
