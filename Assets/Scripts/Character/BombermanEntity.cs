using BombermanRL.Props;
using DG.Tweening;
using System;
using UnityEngine;

namespace BombermanRL.Character
{
    /// <summary>
    /// Base class for anything that occupies a tile and can move/place bombs — both
    /// <see cref="PlayerController"/> (human input) and <see cref="EnemyController"/>
    /// (AI-driven) derive from this. Owns movement tweening/animation, bomb count and
    /// lifecycle state (idle/walking/dead/resetting), and raises
    /// <see cref="RequestMove"/>/<see cref="RequestPlaceBomb"/> events for
    /// <see cref="Grid.MatchDirector"/> to validate against the grid and grant/deny.
    /// Subclasses never touch the grid directly — they only ever request actions.
    /// </summary>
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
        // Running count of actions this entity has actually taken (moves + bomb placements); used for stats/training diagnostics.
        protected string _characterName;
        private int _executedActionCount;

        /// <summary>
        /// Display name — the custom name set via <see cref="SetEntityName"/> if one was set, otherwise falls back to the GameObject's name.
        /// </summary>
        public string Name { 
            get
            {
                if (_characterName == null) return gameObject.name;
                else return _characterName;
            }
        }
        public EntityState State { get { return _currentState; } }
        public int BombLimit { get => _agentParameter.BombLimit; }

        private int _bombCount;

        /// <summary>
        /// Bombs this entity currently has left to place. Setting it also fires <see cref="OnBombCountChanged"/> (used to update UI).
        /// </summary>
        public int BombCount { 
            get => _bombCount; 
            set { 
                _bombCount = value;
                OnBombCountChanged?.Invoke(value);
            } 
        }
        public int ExecutedActionCount { 
            get => _executedActionCount; 
            set
            {
                _executedActionCount = value;
            }
        }
        public int BombExplodeRadius { get => _agentParameter.BombExplosionRadius; }
        public int NearbyObservationRadius { get => _agentParameter.NearbyObservationRadius; }
        
        /// <summary>
        /// Per-entity visual offset applied on top of a tile's world position (so different prefabs can be centered/aligned correctly on the same grid cell).
        /// </summary>
        public Vector3 OffsetMovement { get; set; }
        public CharacterType CharacterType { get => _characterType; }

        /// <summary>
        /// Raised when this entity wants to move one tile in <c>direction</c>; <see cref="Grid.MatchDirector"/> validates and resolves it.
        /// </summary>
        public event Action<BombermanEntity, Vector2> RequestMove;
        
        /// <summary>
        /// Raised when this entity wants to place a bomb on its current tile; <see cref="Grid.MatchDirector"/> validates and resolves it.
        /// </summary>
        public event Action<BombermanEntity> RequestPlaceBomb;
        
        /// <summary>
        /// Raised whenever <see cref="BombCount"/> changes, so UI can stay in sync.
        /// </summary>
        public event Action<int> OnBombCountChanged;

        protected void Awake()
        {
            _charaAudioSource = GetComponent<AudioSource>();
            _charaAudioSource.mute = GameInstance.Instance.AudioHandler.IsMuteSFX;
            GameInstance.Instance.AudioHandler.OnSFXMute += OnMuteSFX;

            _bombCount = _agentParameter.BombLimit;
        }

        protected void OnDestroy()
        {
            if(GameInstance.Instance) GameInstance.Instance.AudioHandler.OnSFXMute -= OnMuteSFX;
        }

        /// <summary>
        /// Fires <see cref="RequestMove"/> for this entity. Called by subclasses (player input or AI decision) — does not move anything itself.
        /// </summary>
        /// <param name="moveDirection">Desired single-axis move direction (e.g. Vector2.up).</param>
        protected virtual void OnRequestMove(Vector2 moveDirection) => RequestMove?.Invoke(this, moveDirection);

        /// <summary>
        /// Fires <see cref="RequestPlaceBomb"/> for this entity. Called by subclasses — does not place a bomb itself.
        /// </summary>
        protected virtual void OnRequestPlaceBomb() => RequestPlaceBomb?.Invoke(this);

        /// <summary>
        /// Overrides the display name shown via <see cref="Name"/> (falls back to the GameObject name if never called).
        /// </summary>
        /// <param name="characterName">The name to display for this entity.</param>
        public virtual void SetEntityName(string characterName) => _characterName = characterName;

        /// <summary>
        /// Hooks this entity up to the grid so it can query tile state via <see cref="IGameplayStateProvider"/>. 
        /// Must be called before the entity can act.
        /// </summary>
        /// <param name="provider">The grid state provider (typically <see cref="Grid.GridStateManager"/>).</param>
        public virtual void Initialize(IGameplayStateProvider provider)
        {
            _stateProvider = provider;
        }


        /// <summary>
        /// Called once a requested bomb placement is granted: consumes one bomb charge and counts the action.
        /// </summary>
        public virtual void OnAblePlaceBomb() 
        {
            BombCount--;
            ExecutedActionCount++;
            //Debug.Log($"{name} Able Place Bomb");
        }

        /// <summary>
        /// Called when a requested action was rejected by the grid (e.g. moved into a wall, tried to place a bomb where one already exists). 
        /// No-op at this base level — overridden where the rejection needs to be surfaced (e.g. to an AI decision provider).
        /// </summary>
        /// <param name="action">The action type that was rejected.</param>
        public virtual void OnInvalidAction(ActionType action) { }

        /// <summary>
        /// Faces the entity toward <paramref name="targetPos"/> and, if the move is
        /// legal, tweens it there while playing the walk animation/SFX.
        /// <paramref name="onTileChanged"/> fires partway through the tween (at 40%
        /// progress) rather than on arrival, so the entity's logical grid position updates
        /// slightly ahead of the visual — this is what lets the origin tile be released for
        /// other entities/bombs before the walk animation fully completes.
        /// </summary>
        /// <param name="targetPos">World position of the destination tile.</param>
        /// <param name="canMove">Whether the move was actually approved by the grid — if false, the entity only turns to face the direction and does not move.</param>
        /// <param name="onTileChanged">Callback to commit the entity's new logical grid position; invoked once the move is ~40% complete.</param>
        public virtual void Move(Vector3 targetPos, bool canMove, Action onTileChanged)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;

            Quaternion faceRotation = Quaternion.LookRotation(direction);
            transform.rotation = faceRotation;

            // Check if entity able to move to the target tile
            if(canMove)
            {
                ExecutedActionCount++;
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

        /// <summary>
        /// Kills this entity: stops audio/movement and plays the character-type-appropriate death animation. No-ops if already dead.
        /// </summary>
        /// <param name="isSuicide">Whether this entity died from its own bomb (currently only affects overriding subclasses' reward/stat handling, not the animation played here).</param>
        public virtual void Dead(bool isSuicide)
        {
            if(_currentState == EntityState.Dead) return;
            _charaAudioSource.Stop();
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

        /// <summary>
        /// Called when this entity's bomb destroys <paramref name="prop"/>. Base implementation just logs — overridden where destroying props needs to matter (e.g. AI reward).
        /// </summary>
        /// <param name="prop">The prop that was destroyed.</param>
        public virtual void DestroyProps(IDestroyableProps prop)
        {
            Debug.Log($"{Name} Destroy {prop.Name}");
        }

        /// <summary>
        /// Called when this entity's bomb kills someone (possibly itself). Base implementation just logs — overridden where kills need to matter (e.g. AI reward).
        /// </summary>
        /// <param name="killType">Classification of the kill (normal / friendly-fire / suicide).</param>
        public virtual void Kill(KillType killType)
        {
            if(_currentState == EntityState.Dead || _currentState ==  EntityState.Resetting) return;

            Debug.Log($"{Name} has done a {killType}");
        }

        /// <summary>
        /// Called when this entity's side wins the match/episode. Base implementation just logs.
        /// </summary>
        public virtual void Win()
        {
            Debug.Log($"{Name} Win!");
        }

        /// <summary>
        /// Begins an episode reset for this entity: stops any in-progress move, marks it
        /// as resetting (so it stops acting), refills its bomb count immediately, then
        /// after <paramref name="resetDelay"/> seconds teleports it to its new spawn via
        /// <see cref="ResetEntity"/>.
        /// </summary>
        /// <param name="resetWorldPos">World position to teleport to once the delay elapses.</param>
        /// <param name="resetDelay">Seconds to wait (showing the episode's outcome) before actually teleporting.</param>
        public virtual void StartReset(Vector3 resetWorldPos, float resetDelay)
        {
            _moveTween?.Kill();
            _currentState = EntityState.Resetting;
            _executedActionCount = 0;
            _bombCount = _agentParameter.BombLimit;

            DOVirtual.DelayedCall(resetDelay, () =>
            {
                ResetEntity(resetWorldPos);
            });
        }

        /// <summary>
        /// Teleports the entity to its new spawn position (plus its <see cref="OffsetMovement"/>), refills bombs, and returns it to idle. Called after <see cref="StartReset"/>'s delay elapses.
        /// </summary>
        /// <param name="resetWorldPos">World position of the new spawn tile.</param>
        protected virtual void ResetEntity(Vector3 resetWorldPos)
        {
            _bombCount = _agentParameter.BombLimit;
            transform.position = resetWorldPos + OffsetMovement;
            _currentState = EntityState.Idle;
            _view.SetIdle();
        }

        /// <summary>
        /// Mutes/unmutes this entity's audio source in response to the global SFX mute toggle.
        /// </summary>
        /// <param name="mute">True to mute.</param>
        protected virtual void OnMuteSFX(bool mute) => _charaAudioSource.mute = mute;

        /// <summary>
        /// Starts the looping walk sound effect on this entity's audio source.
        /// </summary>
        protected virtual void PlayWalkSFX()
        {
            if (_charaAudioSource == null) Debug.Log("Audio Source Null");
            _charaAudioSource.clip = _walkSFX;
            _charaAudioSource.loop = true;
            _charaAudioSource.Play();
        }

        /// <summary>
        /// Pauses/resumes this entity. Base implementation just logs — overridden by subclasses to actually suspend input/AI decision loops.
        /// </summary>
        /// <param name="pause">True to pause, false to resume.</param>
        public virtual void PauseCharacter(bool pause)
        {
            Debug.Log($"Character {Name} is paused? {pause}");
        }
    }

}
