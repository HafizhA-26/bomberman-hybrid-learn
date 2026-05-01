using BombermanRL.Character;
using BombermanRL.Props;
using DG.Tweening;
using System;
using UnityEngine;

namespace BombermanRL
{
    public class BombermanEntity : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected CharacterView _view;
        [SerializeField] protected CharacterController _controller;

        [Header("Action Parameter")]
        [SerializeField] protected AgentParameter _agentParameter;

        [Header("Data")]
        [SerializeField] protected CharacterType _characterType;

        protected ActionCooldown _actionCooldown;
        protected Tween _moveTween;
        protected EntityState _currentState;

        public string Name { get => gameObject.name; }
        public EntityState State { get { return _currentState; } }
        public int BombLimit { get => _agentParameter.BombLimit; }
        public int BombCount { get; set; }
        public int NearbyObservationRadius { get => _agentParameter.NearbyObservationRadius; }
        public Vector3 OffSetMovement { get; set; }
        public event Action<Vector2> RequestMove;
        public event Action RequestPlaceBomb;
        public event Func<GameplayState> RequestGameplayState;

        private void OnDestroy()
        {
            RequestMove = null;
            RequestPlaceBomb = null;
            RequestGameplayState = null;
        }

        protected virtual void OnRequestMove(Vector2 moveDirection) => RequestMove?.Invoke(moveDirection);
        protected virtual void OnRequestPlaceBomb() => RequestPlaceBomb?.Invoke();
        protected virtual GameplayState OnRequestGameplayState() => RequestGameplayState?.Invoke();
        public virtual void Move(Vector3 targetPos, bool canMove, Action onTileChanged)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            Quaternion faceRotation = Quaternion.LookRotation(direction);
            transform.rotation = faceRotation;

            // Check if entity able to move to the target tile
            if(canMove)
            {
                _currentState = EntityState.Walking;
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
                        if(_currentState != EntityState.Dead) _currentState = EntityState.Idle;
                    });
            }
        }

        public virtual void Dead(bool isSuicide)
        {
            if(_currentState == EntityState.Dead) return;
            _currentState = EntityState.Dead;

            _moveTween?.Kill();
            switch (_characterType)
            {
                case CharacterType.None:
                    _view.SetBadDeath();
                    break;
                case CharacterType.GoodMan:
                    _view.SetGoodDeath();
                    break;
                case CharacterType.Bandit:
                    _view.SetBadDeath();
                    break;
            }
        }

        public virtual void DestroyProps(IDestroyableProps prop)
        {
            Debug.Log($"{Name} Destroy {prop.Name}");
        }

        public virtual void Kill(KillType killType)
        {
            if(_currentState == EntityState.Dead || _currentState ==  EntityState.Resetting) return;

            Debug.Log($"{Name} has done a {killType}");
        }

        public virtual void Win()
        {
            Debug.Log($"{Name} Win!");
        }

        public virtual void StartReset(Vector3 resetWorldPos, float resetDelay)
        {
            _currentState = EntityState.Resetting;

            DOVirtual.DelayedCall(resetDelay, () =>
            {
                ResetEntity(resetWorldPos);
            });
        }

        protected virtual void ResetEntity(Vector3 resetWorldPos)
        {
            BombCount = 0;
            transform.position = resetWorldPos + OffSetMovement;
            _currentState = EntityState.Idle;
            _view.SetIdle();
        }
    }

}
