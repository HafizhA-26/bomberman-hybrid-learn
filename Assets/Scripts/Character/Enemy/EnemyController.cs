using BombermanRL.Props;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

namespace BombermanRL.Character
{
    public class EnemyController : MonoBehaviour, IBombermanCharacter
    {
        [Header("References")]
        [SerializeField] private CharacterView _view;

        [Header("Action Parameter")]
        [SerializeField] private CharacterType _type = CharacterType.Bandit;
        [SerializeField] private AIType _AIType = AIType.RuleBased;
        [SerializeField] private float _cooldown = 0.5f;
        [SerializeField] private float _moveDuration = 1f;
        [SerializeField] private int _bombLimit = 1;
        [SerializeField] private int _nearbyObserveRadius = 2;

        private IDecisionProvider _decisionProvider;
        private Tween _decisionTween;
        private Tween _moveTween;
        private bool _isDead;
        private bool _isWalk;

        public string Name => gameObject.name;
        public CharacterType Type => _type;
        public Vector3 OffsetMovement { get; set; } = new Vector3();
        public int BombCount { get; set; }
        public UnityEvent<Vector2> OnRequestMove { get; set; } = new();
        public UnityEvent OnRequestPlaceBomb { get; set; } = new();
        public Func<GameplayState> OnRequestGameplayState { get; set; }
        public int NearbyObserveRadius { get => _nearbyObserveRadius; }
        public int BombLimit { get => _bombLimit; set => _bombLimit = value; }


        private void Awake()
        {
            
        }

        private void Start()
        {
            switch (_AIType)
            {
                case AIType.RuleBased:
                    _decisionProvider = new RuleBasedDecision();
                    break;
                case AIType.OfflineOnly:
                    _decisionProvider = new RLDecisionProvider(GetComponent<AgentBomber>(), LearningType.OfflineLearning);
                    break;
                case AIType.OnlineOnly:
                    _decisionProvider = new RLDecisionProvider(GetComponent<AgentBomber>(), LearningType.OnlineLearning);
                    break;
                case AIType.HybrilRL:
                    _decisionProvider = new RLDecisionProvider(GetComponent<AgentBomber>(), LearningType.HybridLearning);
                    break;
            }

            _decisionTween = DOVirtual.DelayedCall(_cooldown, DecisionCallback).SetLoops(-1);
        }

        private void OnDestroy()
        {
            OnRequestMove.RemoveAllListeners();
            OnRequestPlaceBomb.RemoveAllListeners();
            _decisionProvider?.OnDestroy();
            _decisionTween?.Kill();
        }

        private void DecisionCallback()
        {
            if (!_isDead && !_isWalk)
            {
                GameplayState currState = OnRequestGameplayState?.Invoke();
                ActionType actionToTake = _decisionProvider.Decide(currState);
                Debug.Log("[Enemy] Get Decision");
                //Debug.Log("Curr State : " + currState);
                //Debug.Log("Action to Take : "+actionToTake);
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

        public void Move(Vector3 targetPos, bool canMove, Action onTileChanged)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            Quaternion faceRotation = Quaternion.LookRotation(direction);
            transform.rotation = faceRotation;

            if (canMove)
            {
                _isWalk = true;
                _view.SetWalk();
                bool tileChangedTriggered = false;
                _moveTween = transform.DOMove(targetPos, _moveDuration)
                    .OnUpdate(() =>
                    {
                        if(!tileChangedTriggered && _moveTween.ElapsedPercentage() >= 0.5f)
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

        public void Dead()
        {
            if (_isDead) return;

            _decisionTween?.Kill();
            _isDead = true;
            _decisionProvider.OnDead();
            _view.SetBadDeath();

        }

        public void Kill(IBombermanCharacter character)
        {
            Debug.Log($"{Name} Kills {character.Name}");
            _decisionProvider.OnKillSomeone(character);
        }

        public void DestroyProps(IDestroyableProps prop)
        {
            Debug.Log($"{Name} Destroy {prop.Name}");
            _decisionProvider?.OnDestroyProps(prop);
        }

        public void ResetEntity(Vector3 resetWorldPos, float resetDelay)
        {
            _decisionTween?.Kill();
            Debug.Log("Reset Enemy");

            DOVirtual.DelayedCall(resetDelay, () =>
            {
                _isDead = false;
                _isWalk = false;
                BombCount = 0;
                transform.position = resetWorldPos;
                _view.SetIdle();

                _decisionTween = DOVirtual.DelayedCall(_cooldown, DecisionCallback).SetLoops(-1);
            });
        }
    }
}
