using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class PlayerController : BombermanEntity, PlayerInputActions.IGameplayActions
    {
        private PlayerInputActions _inputAction;

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
            if (_currentState != EntityState.Idle && !_actionCooldown.CanAction()) return;

            OnRequestMove(moveInput);
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(_currentState != EntityState.Idle || BombCount >= _agentParameter.BombLimit || !_actionCooldown.CanAction()) return;
            OnRequestPlaceBomb();
        }
    }
}
