using BombermanRL.Props;
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
        [SerializeField] private AgentParameter _agentParameter;

        private IDecisionProvider _decisionProvider;
        private PlayerInputActions _inputAction;
        private ActionCooldown _actionCooldown;
        private Tween _moveTween;
        private Tween _decisionTween;
        private bool _isDead;
        private bool _isWalk;

        public string Name => gameObject.name;
        public CharacterType Type => CharacterType.Player;
        public UnityEvent<Vector2> OnRequestMove { get;  set; } = new();
        public UnityEvent OnRequestPlaceBomb { get; set; } = new();
        public Vector3 OffsetMovement { get; set; }
        public int BombCount { get; set; }
        public int BombLimit { get => _agentParameter.BombLimit; }
        public Func<GameplayState> OnRequestGameplayState { get; set; }
        public int NearbyObserveRadius { get => _agentParameter.NearbyObservationRadius; }
        public bool IsDead => _isDead;

        private void Awake()
        {
            _inputAction = new PlayerInputActions();
            _actionCooldown = new ActionCooldown(_agentParameter.ActionCooldown);
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

        private void StartDecisionLoop()
        {
            _decisionProvider = new RuleBasedDecision(_agentParameter);
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }

        private void DecisionCallback()
        {
            if (!_isDead && !_isWalk)
            {
                GameplayState currState = OnRequestGameplayState?.Invoke();
                ActionType actionToTake = _decisionProvider.Decide(currState);
                //Debug.Log("Action to Take : "+actionToTake);
                //Debug.Log("[Player] Curr State : " + currState);
                //Debug.Log($"[Player] Get Decision {actionToTake}");
                switch (actionToTake)
                {
                    case ActionType.Idle:
                        break;
                    case ActionType.MoveUp:
                        OnRequestMove.Invoke(Vector2.up);
                        break;
                    case ActionType.MoveDown:
                        OnRequestMove.Invoke(Vector2.down);
                        break;
                    case ActionType.MoveLeft:
                        OnRequestMove.Invoke(Vector2.left);
                        break;
                    case ActionType.MoveRight:
                        OnRequestMove.Invoke(Vector2.right);
                        break;
                    case ActionType.PlaceBomb:
                        if (BombCount < BombLimit) OnRequestPlaceBomb.Invoke();
                        break;
                }
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_isDead || _isWalk || !_actionCooldown.CanAction()) return;
            Vector2 moveInput = context.ReadValue<Vector2>();
            //Debug.Log("Action "+moveInput);
            OnRequestMove?.Invoke(moveInput);
        }

        public void OnPlaceBomb(InputAction.CallbackContext context)
        {
            if(_isDead || _isWalk || BombCount >= _agentParameter.BombLimit || !_actionCooldown.CanAction()) return;
            OnRequestPlaceBomb?.Invoke();
        }

        public void Move(Vector3 targetPos, bool canMove, Action onTileChanged)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            Quaternion faceRotation = Quaternion.LookRotation(direction);
            transform.rotation = faceRotation;

            if(canMove)
            {
                _isWalk = true;
                _view.SetWalk();
                bool tileChangedTriggered = false;
                _moveTween = transform.DOMove(targetPos, _agentParameter.MoveDuration)
                    .OnUpdate(() =>
                    {
                        if (!tileChangedTriggered && _moveTween.ElapsedPercentage() >= 0.3f)
                        {
                            tileChangedTriggered = true;
                            onTileChanged?.Invoke();
                        }
                    })
                    .OnComplete(() =>
                    {
                        _isWalk = false;
                        if(!_isDead) _view.SetIdle();
                    });
            }
        }

        public void Dead(bool isSuicide)
        {
            if(_isDead) return;
            _isDead = true;

            _decisionTween?.Kill();
            _moveTween?.Kill();
            _isWalk = false;
            _view.SetGoodDeath();
        }

        public void Kill(IBombermanCharacter character)
        {
            Debug.Log($"{Name} Kills {character.Name}");
        }

        public void DestroyProps(IDestroyableProps prop)
        {
            Debug.Log($"{Name} Destroy {prop.Name}");
        }

        public void ResetEntity(Vector3 resetWorldPos, float resetDelay)
        {
            _decisionTween?.Kill();

            DOVirtual.DelayedCall(resetDelay, () =>
            {
                _isDead = false;
                _isWalk = false;
                BombCount = 0;
                transform.position = resetWorldPos + OffsetMovement;
                _view.SetIdle();

                StartDecisionLoop();
            });
        }

        public void Win()
        {
            Debug.Log($"{Name} Win!");
        }
    }
}
