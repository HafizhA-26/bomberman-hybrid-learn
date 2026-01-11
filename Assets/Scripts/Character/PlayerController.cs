using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace BombermanRL.Character
{
    public class PlayerController : MonoBehaviour, PlayerInputActions.IGameplayActions, IBombermanCharacter
    {
        [Header("References")]
        [SerializeField] private CharacterView _view;
        [SerializeField] private CharacterController _controller;

        [Header("Action Parameter")]
        [SerializeField] private GameObject _bombPrefab;
        [SerializeField] private float _cooldown = 0.5f;
        [SerializeField] private float _moveDuration = 1f;
        [SerializeField] private int _bombLimit = 1;

        private PlayerInputActions _inputAction;
        private ActionCooldown _actionCooldown;
        private int _curBombCount;
        private bool _isDead;
        private bool _isWalk;

        public UnityEvent<Vector2> OnRequestMove { get;  set; } = new();
        public UnityEvent OnRequestPlaceBomb { get; set; } = new();

        public Vector3 OffsetMovement { get; set; }
        public int BombCount { get => _curBombCount; set => _curBombCount = value; }

        private void Awake()
        {
            _inputAction = new PlayerInputActions();
            _actionCooldown = new ActionCooldown(_cooldown);
            _inputAction.Gameplay.SetCallbacks(this);
        }

        private void OnDestroy()
        {
            OnRequestMove.RemoveAllListeners();
            OnRequestPlaceBomb.RemoveAllListeners();
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
            if (_isDead || _isWalk || !_actionCooldown.CanAction()) return;
            Vector2 moveInput = context.ReadValue<Vector2>();
            Debug.Log("Action "+moveInput);
            OnRequestMove?.Invoke(moveInput);
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(_isDead || _isWalk || _curBombCount >= _bombLimit || !_actionCooldown.CanAction()) return;
            OnRequestPlaceBomb?.Invoke();
        }

        public void Move(Vector3 targetPos, bool canMove, Action onCompleteMove)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            Quaternion faceRotation = Quaternion.LookRotation(direction);
            transform.rotation = faceRotation;

            if(canMove)
            {
                _isWalk = true;
                _view.SetWalk();
                transform.DOMove(targetPos, _moveDuration).OnComplete(() =>
                {
                    _isWalk = false;
                    _view.SetIdle();
                    onCompleteMove?.Invoke();
                });
            }
        }
    }
}
