using UnityEngine;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class PlayerController : BombermanEntity, PlayerInputActions.IGameplayActions
    {
        private PlayerInputActions _inputAction;

        private void Awake()
        {
            _inputAction = new PlayerInputActions();
            _actionCooldown = new ActionCooldown(_agentParameter.ActionCooldown);
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
            if (_currentState != EntityState.Idle && !_actionCooldown.CanAction()) return;

            Vector2 moveInput = context.ReadValue<Vector2>();
            OnRequestMove(moveInput);
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(_currentState != EntityState.Idle || BombCount >= _agentParameter.BombLimit || !_actionCooldown.CanAction()) return;
            OnRequestPlaceBomb();
        }
    }
}
