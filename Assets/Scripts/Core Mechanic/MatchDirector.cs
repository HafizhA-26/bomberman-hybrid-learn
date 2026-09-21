using BombermanRL.Character;
using BombermanRL.Props;
using BombermanRL.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Grid
{
    /// <summary>
    /// Orchestrates a full match/episode. Builds the level via <see cref="LevelBuilder"/>,
    /// wires every entity's move/bomb request events to <see cref="GridStateManager"/>,
    /// listens for bomb/explosion events from <see cref="BombManager"/> to resolve prop
    /// destruction and character deaths, tracks the win condition, and either auto-resets
    /// the arena (ML training episodes) or ends the session (player-vs-agent play).
    /// </summary>
    public class MatchDirector : MonoBehaviour
    {
        [SerializeField] private LevelBuilder _levelBuilder;
        [SerializeField] private GridStateManager _gridStateManager;
        [SerializeField] private BombManager _bombManager;
        [SerializeField] private UIManager _uiManager;
        [Header("Shake Camera Effects")]
        [SerializeField] private bool _useCameraShake;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeStrength = 1f;
        [SerializeField] private int _shakeVibrato = 1;
        [Header("Training Paramaters")]
        // When true, a death auto-resets the arena for another training episode instead of ending the session (see ResetGrid vs EndPlayableSession).
        [SerializeField] private bool _isOnTrainingAgent = true;
        // Which character type is treated as "the agent" for floor-color feedback on reset (success/neutral/failed materials).
        [SerializeField] private CharacterType _trainingAgentType = CharacterType.Bandit;
        // Seconds to hold on the post-death arena (with result-colored floors) before actually resetting, so the outcome is observable.
        [SerializeField] private float _resetDelay = 2f;

        // All tracked entities grouped by character type (e.g. Player vs Bandit), used to evaluate win conditions.
        private readonly Dictionary<CharacterType, List<BombermanEntity>> _entityTypeGroup = new Dictionary<CharacterType, List<BombermanEntity>>();
        private readonly List<EnemyController> _enemies = new();
        private PlayerController _player;
        private GameObject[,] _floors;
        // True while an episode-reset sequence is in progress; move/bomb/explosion handling is suppressed during this window.
        private bool _isOnReset;

        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            GameInstance.Instance.DeviceType = Util.DetectPlatform();
#else
            GameInstance.Instance.DeviceType = 0;
#endif
            if (GameInstance.Instance.DeviceType == 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = 30;

            _uiManager.OnStartMatch += StartMatch;

            foreach (CharacterType type in Enum.GetValues(typeof(CharacterType)))
            {
                if (type == CharacterType.None) continue;
                _entityTypeGroup[type] = new List<BombermanEntity>();
            }
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<CharacterType, List<BombermanEntity>> entities in _entityTypeGroup)
            {
                foreach (BombermanEntity entity in entities.Value)
                {
                    entity.RequestMove -= MoveEntity;
                    entity.RequestPlaceBomb -= PlaceBomb;
                }
            }

            _uiManager.OnStartMatch -= StartMatch;
            _bombManager.OnBombExplode -= OnBombExplode;
            _bombManager.OnTickExplosion -= CheckExplosionVictim;
            _bombManager.OnExplosionFinish -= OnExplosionFinish;
        }

        /// <summary>
        /// Main entry point for beginning a match (bound to the UI's start button/event).
        /// Builds the floor and level tiles, registers all entities and their event
        /// handlers, initializes the grid state and bomb manager, hooks up bomb/explosion
        /// callbacks, and starts enemy AI.
        /// </summary>
        public async void StartMatch()
        {
            GameInstance.Instance.ShowLoading(true, 0.3f);

            _floors = await _levelBuilder.CreateFloor();
            (GameObject[,] tiles, TileState[,] grid) = await _levelBuilder.LoadLevelTile();

            InitializeEntities(tiles, grid);

            _gridStateManager.PreSetup(_levelBuilder.ParentPos, _levelBuilder.TileSize);
            _gridStateManager.Initialize(tiles, grid);

            _bombManager.Initialize(_levelBuilder.BombPrefab, _levelBuilder.ExplosionPrefab, _levelBuilder.ObjectsTileParent);
            _bombManager.OnBombExplode += OnBombExplode;
            _bombManager.OnTickExplosion += CheckExplosionVictim;
            _bombManager.OnExplosionFinish += OnExplosionFinish;

            _uiManager.SetupPlayerListener(_player);

            _enemies.ForEach(e => e.StartAI());

            GameInstance.Instance.ShowLoading(false);
        }

        /// <summary>
        /// Scans the instantiated tile grid for the player/enemy spawn objects, sorts each
        /// into <see cref="_player"/>/<see cref="_enemies"/> and <see cref="_entityTypeGroup"/>,
        /// initializes them against <see cref="_gridStateManager"/>, and subscribes to their
        /// move/bomb-placement request events.
        /// </summary>
        /// <param name="tiles">All instantiated gameobjects on grid</param>
        /// <param name="tileStates">All tile states of the grid</param>
        private void InitializeEntities(GameObject[,] tiles, TileState[,] tileStates)
        {
            if (tiles.GetLength(0) != tileStates.GetLength(0) || tiles.GetLength(1) != tileStates.GetLength(1))
            {
                Debug.LogWarning("Invalid tile objects and tile states to initialize");
                return;
            }

            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    GameObject tile = tiles[i, j];
                    TileState state = tileStates[i, j];

                    if(state.Type == TileType.PlayerSpawn || state.Type == TileType.EnemySpawn)
                    {
                        BombermanEntity entity = tile.GetComponent<BombermanEntity>();

                        if(entity is PlayerController player) _player = player;
                        else if(entity is EnemyController enemy) _enemies.Add(enemy);

                        if (_isOnTrainingAgent) entity.name = $"[{gameObject.name}] {entity.name}";
                        entity.Initialize(_gridStateManager);
                        entity.RequestMove += MoveEntity;
                        entity.RequestPlaceBomb += PlaceBomb;

                        _entityTypeGroup[entity.CharacterType].Add(entity);
                    }
                }
            }
        }

        /// <summary>
        /// Handles a move request from any entity (player input or an AI agent's action).
        /// Validates the move against <see cref="GridStateManager.CanMove"/>, reserves the
        /// destination tile, and tells the entity to play its move animation — invalid moves
        /// still call <see cref="BombermanEntity.OnInvalidAction"/> so the entity (and any
        /// ML training signal) knows the action failed. No-ops while an episode reset is in progress.
        /// </summary>
        /// <param name="entity">Target entity to move</param>
        /// <param name="direction">Move direction</param>
        private void MoveEntity(BombermanEntity entity, Vector2 direction)
        {
            if (_isOnReset) return;
            if (Math.Abs(direction.x) <= 0.1f && Math.Abs(direction.y) <= 0.1f) return;

            bool CanMove = _gridStateManager.CanMove(entity, direction);
            (GridPos gridPos, Vector3 worldPos) = _gridStateManager.GetNextMovePosition(entity, direction);
            Action onTileChanged = CanMove ? _gridStateManager.OnEntityMove(entity, gridPos) : null;

            if (!CanMove) entity.OnInvalidAction(Util.GetActionFromDirection(direction));
            entity.Move(worldPos, CanMove, onTileChanged);
        }

        /// <summary>
        /// Handles a bomb-placement request. Rejects it (and notifies the entity via
        /// <see cref="BombermanEntity.OnInvalidAction"/>) if the entity's tile already has a
        /// bomb, or while an episode reset is in progress; otherwise computes the blast
        /// tiles, spawns the bomb, and registers it with the grid state.
        /// </summary>
        /// <param name="entity">Target entity to place the bomb</param>
        private void PlaceBomb(BombermanEntity entity)
        {
            bool canPlaceBomb = _gridStateManager.CanPlaceBomb(entity);
            if (_isOnReset || !canPlaceBomb)
            {
                if(!canPlaceBomb) entity.OnInvalidAction(ActionType.PlaceBomb);
                return;
            }

            (List<GridPos> bombingGridPos, List<Vector3> bombingWorldPos) = _gridStateManager.OnPlaceBomb(entity);
            BombHandler newBomb = _bombManager.SpawnBomb(entity, bombingGridPos, bombingWorldPos);
            _gridStateManager.RegisterActiveBomb(newBomb, bombingGridPos[0]);
            entity.OnAblePlaceBomb();
        }

        /// <summary>
        /// Fired the instant a bomb detonates. Updates blast tiles to the "exploding" grid
        /// state, then destroys any destructible prop caught in the blast that the placer's
        /// character type is allowed to destroy.
        /// </summary>
        /// <param name="placer">Bomb placer entity</param>
        /// <param name="explosionGridPos">Explosions position</param>
        private void OnBombExplode(BombermanEntity placer, List<GridPos> explosionGridPos)
        {
            if (_isOnReset) return;

            _gridStateManager.OnBombExplode(explosionGridPos);

            // Destroy all props that on the tile of the explosions
            foreach (GridPos item in explosionGridPos)
            {
                IDestroyableProps prop = _gridStateManager.GetPropAt(item);
                if(prop != null && !prop.IsDestroyed && prop.CanBeDestroyedBy(placer.CharacterType))
                {
                    _gridStateManager.OnPropDestroyedAt(item);
                    prop.DestroyProps();
                    placer.DestroyProps(prop);
                }
            }
        }

        /// <summary>
        /// Called on each explosion tick to check whether any character is standing in the
        /// blast. For every victim found: determines the kill type (normal / suicide /
        /// friendly-fire), fires death/kill callbacks on both victim and placer, then checks
        /// the win condition — if the match is decided, either auto-resets the arena
        /// (training mode) or ends the playable session, and triggers a camera shake if any
        /// death occurred.
        /// </summary>
        /// <param name="placer">Bomb placer entity</param>
        /// <param name="explosionGridPos">Explosions position</param>
        private void CheckExplosionVictim(BombermanEntity placer, List<GridPos> explosionGridPos)
        {
            if(_isOnReset) return;
            bool deadVictimExists = false;

            int victimCount = 0;

            foreach (GridPos tilePos in explosionGridPos)
            {
                BombermanEntity victim = _gridStateManager.GetCharacterAt(tilePos);
                if(victim != null && victim.State != EntityState.Dead)
                {
                    victimCount++;
                    deadVictimExists = true;

                    // Check kill type on someone died
                    KillType killType = KillType.NormalKill;
                    if (victim == placer) killType = KillType.Suicide;
                    else if (victim.CharacterType == placer.CharacterType) killType = KillType.FriendlyFire;

                    // Trigger dead and kill event on placer and victim
                    victim.Dead(killType == KillType.Suicide);
                    placer.Kill(killType);

                    Debug.Log($"[{victim.name}-{victim.CharacterType}] at {tilePos} dead as victim of [{placer.name}-{placer.CharacterType}]");

                    CharacterType winnerType = CheckWinCondition();

                    // Ensure only reset if one type group alive
                    if (winnerType != CharacterType.None)
                    {
                        _uiManager.OnCharacterWin(winnerType);
                        // Auto reset grid if in training gameplay
                        if (_isOnTrainingAgent) ResetGrid(placer.CharacterType, killType);
                        else EndPlayableSession(winnerType);
                        break;
                    }
                }
            }

            if(deadVictimExists && _useCameraShake) _cameraTransform.DOShakeRotation(_shakeDuration, _shakeStrength, _shakeVibrato, 90, true, ShakeRandomnessMode.Full);
        }

        /// <summary>
        /// Returns the character type that alone still has living members, or <see cref="CharacterType.None"/> if more than one group is still alive. Triggers <see cref="BombermanEntity.Win"/> on the winning group's entities as a side effect
        /// </summary>
        /// <returns>Character type that win</returns>
        private CharacterType CheckWinCondition()
        {
            // Check current alive character group
            List<CharacterType> winnerType = new List<CharacterType>();
            foreach (KeyValuePair<CharacterType, List<BombermanEntity>> entityGroup in _entityTypeGroup)
            {
                int liveCount = entityGroup.Value.Count(entity => entity.State != EntityState.Dead);
                if (liveCount > 0) winnerType.Add(entityGroup.Key);
            }

            // Trigger win if currently only one alive group
            if(winnerType.Count == 1)
            {
                _entityTypeGroup[winnerType[0]].ForEach(entity => entity.Win());
                return winnerType[0];
            }

            return CharacterType.None;
        }

        /// <summary>
        /// Called once an explosion's hazard window ends: clears the grid's exploding state and replenishes the placer's available bomb count.
        /// </summary>
        /// <param name="entity">Bomb placer entity</param>
        /// <param name="explosionGridPos">Explosions position</param>
        private void OnExplosionFinish(BombermanEntity entity, List<GridPos> explosionGridPos)
        {
            _gridStateManager.OnExplosionFinish(explosionGridPos);
            entity.BombCount++;
        }

        /// <summary>
        /// Training-mode episode reset, triggered when a training match is decided.
        /// Immediately: pauses explosions and recolors every floor tile based on the
        /// outcome (agent success / agent suicide / other), then respawns all entities at
        /// mutually-distant random valid tiles. After <see cref="_resetDelay"/> seconds
        /// (so the result is observable), it clears bombs, resets all destructible props
        /// and the logical grid back to defaults, and restores neutral floor color.
        /// Guarded by <see cref="_isOnReset"/> against re-entrancy. 
        /// </summary>
        /// <param name="placerType">Bomb placer entity CharacterType</param>
        /// <param name="killType">Kill type that triggered reset grid</param>
        private void ResetGrid(CharacterType placerType, KillType killType)
        {
            if (_isOnReset) return;
            _isOnReset = true;

            List<Material> floorMaterials = _levelBuilder.FloorMaterials;

            // Visualize episode result on floor
            foreach (GameObject item in _floors)
            {
                MeshRenderer floorRenderer = item.transform.GetComponentInChildren<MeshRenderer>();
                if (placerType == _trainingAgentType && killType == KillType.NormalKill)
                    floorRenderer.material = floorMaterials[1];
                else if (placerType != _trainingAgentType && killType == KillType.Suicide)
                    floorRenderer.material = floorMaterials[2];
                else
                    floorRenderer.material = floorMaterials[3];
            }

            _bombManager.PauseExplosions();

            // Search random valid respawn point
            List<GridPos> newEntityPos = new List<GridPos>();
            foreach (KeyValuePair<CharacterType, List<BombermanEntity>> group in _entityTypeGroup)
            {
                foreach (BombermanEntity entity in group.Value)
                {
                    GridPos? newPos = _gridStateManager.GetRandomValidRespawn(newEntityPos, out Vector3 worldPos);
                    if (!newPos.HasValue)
                    {
                        Debug.LogWarning("Can't find valid pos for respawning : "+entity.Name);
                        Debug.Break();
                    }

                    newEntityPos.Add(newPos.Value);
                    entity.StartReset(worldPos, _resetDelay);
                    _gridStateManager.OnEntityTeleported(entity, newPos.Value);
                }
            }

            // Delay reset to give some time for observing arena result
            DOVirtual.DelayedCall(_resetDelay, () =>
            {
                _bombManager.ResetAllBombs();
                _gridStateManager.ResetGridBombStates();

                // Reset all props
                List<(IDestroyableProps prop, Vector3 originPos)> originProps = _gridStateManager.GetPropsOriginPos();
                originProps.ForEach(item => item.prop.ResetProp(item.originPos));
                _gridStateManager.ResetGridState();

                // Reset floor color material
                foreach (GameObject item in _floors)
                {
                    MeshRenderer floorRenderer = item.transform.GetComponentInChildren<MeshRenderer>();
                    if (floorRenderer) floorRenderer.material = floorMaterials[0];
                }

                _isOnReset = false;
            });
        }

        /// <summary>
        /// Player-mode match end (non-training): pauses explosions and freezes every entity in place, without respawning or reloading the arena.
        /// </summary>
        /// <param name="winnerSession">Character type that win the session</param>
        private void EndPlayableSession(CharacterType winnerSession)
        {
            _bombManager.PauseExplosions();
            foreach (KeyValuePair<CharacterType, List<BombermanEntity>> group in _entityTypeGroup)
            {
                foreach (BombermanEntity entity in group.Value)
                {
                    entity.PauseCharacter(true);
                }
            }
            _isOnReset = true;
        }

    }

}
