using BombermanRL.Props;
using DG.Tweening;
using System;
using UnityEngine;

namespace BombermanRL.Character
{
    [RequireComponent(typeof(AudioSource))]
    public class BombermanEntity : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected CharacterView _view;
        [SerializeField] protected CharacterController _controller;

        [Header("Action Parameter")]
        [SerializeField] protected AgentParameter _agentParameter;

        [Header("Data")]
        [SerializeField] private CharacterType _characterType;
        [SerializeField] private AudioClip _walkSFX;

        protected AudioSource _charaAudioSource;
        protected ActionCooldown _actionCooldown;
        protected Tween _moveTween;
        protected IGameplayStateProvider _stateProvider;
        protected EntityState _currentState;

        public string Name { get => gameObject.name; }
        public EntityState State { get { return _currentState; } }
        public int BombLimit { get => _agentParameter.BombLimit; }

        private int _bombCount;
        public int BombCount { 
            get => _bombCount; 
            set { 
                _bombCount = value;
                OnBombCountChanged?.Invoke(value);
            } 
        }
        public int BombExplodeRadius { get => _agentParameter.BombExplosionRadius; }
        public int NearbyObservationRadius { get => _agentParameter.NearbyObservationRadius; }
        public Vector3 OffsetMovement { get; set; }
        public CharacterType CharacterType { get => _characterType; }

        public event Action<BombermanEntity, Vector2> RequestMove;
        public event Action<BombermanEntity> RequestPlaceBomb;
        public event Action<int> OnBombCountChanged;

        protected void Awake()
        {
            _charaAudioSource = GetComponent<AudioSource>();
            GameInstance.Instance.AudioHandler.OnSFXMute += OnMuteSFX;

            _bombCount = _agentParameter.BombLimit;
        }

        protected void OnDestroy()
        {
            if(GameInstance.Instance) GameInstance.Instance.AudioHandler.OnSFXMute -= OnMuteSFX;
        }

        protected virtual void OnRequestMove(Vector2 moveDirection) => RequestMove?.Invoke(this, moveDirection);
        protected virtual void OnRequestPlaceBomb() => RequestPlaceBomb?.Invoke(this);
        public virtual void Initialize(IGameplayStateProvider provider)
        {
            _stateProvider = provider;
        }
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
                PlayWalkSFX();
                bool tileChangedTriggered = false;
                _moveTween = transform.DOMove(targetPos, _agentParameter.MoveDuration)
                    .OnUpdate(() =>
                    {
                        if (!tileChangedTriggered && _moveTween.ElapsedPercentage() >= 0.4f)
                        {
                            tileChangedTriggered = true;
                            onTileChanged?.Invoke();
                        }
                    })
                    .OnComplete(() =>
                    {
                        _charaAudioSource.Stop();
                        if(_currentState != EntityState.Dead)
                        {
                            _currentState = EntityState.Idle;
                            _view.SetIdle();
                        }
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
            _bombCount = _agentParameter.BombLimit;
            transform.position = resetWorldPos + OffsetMovement;
            _currentState = EntityState.Idle;
            _view.SetIdle();
        }

        protected virtual void OnMuteSFX(bool mute) => _charaAudioSource.mute = mute;

        protected virtual void PlayWalkSFX()
        {
            if (_charaAudioSource == null) Debug.Log("Audio Source Null");
            _charaAudioSource.clip = _walkSFX;
            _charaAudioSource.loop = true;
            _charaAudioSource.Play();
        }
    }

}
