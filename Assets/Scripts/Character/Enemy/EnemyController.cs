using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL.Character
{
    public class EnemyController : MonoBehaviour, IBombermanCharacter
    {
        [Header("References")]
        [SerializeField] private CharacterView _view;

        [Header("Action Parameter")]
        [SerializeField] private AIType _AIType = AIType.RuleBased;
        [SerializeField] private float _cooldown = 0.5f;
        [SerializeField] private float _moveDuration = 1f;
        [SerializeField] private int _bombLimit = 1;
        [SerializeField] private int _nearbyObserveRadius = 2;

        private IDecisionProvider _decisionProvider;
        private bool _isDead;
        private bool _isWalk;

        public Vector3 OffsetMovement { get; set; } = new Vector3();
        public int BombCount { get; set; }
        public UnityEvent<Vector2> OnRequestMove { get; set; } = new();
        public UnityEvent OnRequestPlaceBomb { get; set; } = new();
        public Func<GameplayState> OnRequestGameplayState { get; set; }
        public int NearbyObserveRadius { get => _nearbyObserveRadius; }
        public int BombLimit { get => _bombLimit; set => _bombLimit = value; }

        private void Start()
        {
            switch (_AIType)
            {
                case AIType.RuleBased:
                    _decisionProvider = new RuleBasedDecision();
                    break;
                case AIType.OfflineOnly:
                    break;
                case AIType.OnlineOnly:
                    break;
                case AIType.HybrilRL:
                    break;
            }
        }

        public void Move(Vector3 targetPos, bool canMove, Action onCompleteMove)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            Quaternion faceRotation = Quaternion.LookRotation(direction);
            transform.rotation = faceRotation;

            if (canMove)
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

        public void Dead()
        {
            if (_isDead) return;
            Debug.Log(OnRequestGameplayState?.Invoke());
            _isDead = true;
            _view.SetGoodDeath();
        }

    }
}
