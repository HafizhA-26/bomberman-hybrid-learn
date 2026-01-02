using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class PlayerController : MonoBehaviour, PlayerInputActions.IGameplayActions
    {
        [Header("References")]
        [SerializeField] private CharacterView _view;
        [SerializeField] private CharacterController _controller;

        [Header("Action Parameter")]
        [SerializeField] private float _cooldown = 0.5f;

        private PlayerInputActions _inputAction;
        private ActionCooldown _actionCooldown;
        private bool _isDead;

        public readonly UnityEvent<Vector2> OnRequestMove = new UnityEvent<Vector2>();

        private void Awake()
        {
            _inputAction = new PlayerInputActions();
            _actionCooldown = new ActionCooldown(_cooldown);
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

        private void OnTriggerEnter(Collider other)
        {
            if(_isDead) return;

            if(other.CompareTag("Explosion"))
            {
                _isDead = true;
                _view.SetGoodDeath();
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_isDead & !_actionCooldown.CanAction()) return;
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnRequestMove?.Invoke(moveInput);
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(_isDead & !_actionCooldown.CanAction()) return;

            // TODO: Instantiate Bomb Object
        }
    }
}
