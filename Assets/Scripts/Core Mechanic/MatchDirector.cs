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
    public class MatchDirector : MonoBehaviour
    {
        [SerializeField] private LevelBuilder _levelBuilder;
        [SerializeField] private GridStateManager _gridStateManager;
        [SerializeField] private BombManager _bombManager;
        [SerializeField] private UIManager _uiManager;
        [Header("Shake Camera Effects")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeStrength = 1f;
        [SerializeField] private int _shakeVibrato = 1;
        [Header("Training Paramaters")]
        [SerializeField] private bool _isOnTrainingAgent = true;
        [SerializeField] private CharacterType _trainingAgentType = CharacterType.Bandit;
        [SerializeField] private float _resetDelay = 2f;

        private readonly Dictionary<CharacterType, List<BombermanEntity>> _entityTypeGroup = new Dictionary<CharacterType, List<BombermanEntity>>();
        private PlayerController _player;
        private GameObject[,] _floors;
        private bool _isOnReset;

        public Action OnPlayerWin;
        public Action OnEnemyWin;
        public Action<CharacterType> OnCharacterWin;

        private void Awake()
        {
            Application.targetFrameRate = 60;

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

            _bombManager.OnBombExplode -= OnBombExplode;
            _bombManager.OnTickExplosion -= CheckExplosionVictim;
            _bombManager.OnExplosionFinish -= OnExplosionFinish;
        }

        private void Start()
        {
            _floors = _levelBuilder.CreateFloor();
            (GameObject[,] tiles, TileState[,] grid) = _levelBuilder.LoadLevelTile();

            InitializeEntities(tiles, grid);

            _gridStateManager.PreSetup(_levelBuilder.ParentPos, _levelBuilder.TileSize);
            _gridStateManager.Initialize(tiles, grid);

            _bombManager.Initialize(_levelBuilder.BombPrefab, _levelBuilder.ExplosionPrefab, _levelBuilder.ObjectsTileParent);
            _bombManager.OnBombExplode += OnBombExplode;
            _bombManager.OnTickExplosion += CheckExplosionVictim;
            _bombManager.OnExplosionFinish += OnExplosionFinish;

            _uiManager.Initialize(_player);
            
        }

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
                        if(state.Type == TileType.PlayerSpawn) _player = (PlayerController) entity;

                        entity.Initialize(_gridStateManager);
                        entity.RequestMove += MoveEntity;
                        entity.RequestPlaceBomb += PlaceBomb;

                        _entityTypeGroup[entity.CharacterType].Add(entity);
                    }
                }
            }
        }
        
        private void MoveEntity(BombermanEntity entity, Vector2 direction)
        {
            if (_isOnReset) return;
            if (Math.Abs(direction.x) <= 0.1f && Math.Abs(direction.y) <= 0.1f) return;

            bool CanMove = _gridStateManager.CanMove(entity, direction);
            (GridPos gridPos, Vector3 worldPos) = _gridStateManager.GetNextMovePosition(entity, direction);
            Action onTileChanged = CanMove ? _gridStateManager.OnEntityMove(entity, gridPos) : null;

            entity.Move(worldPos, CanMove, onTileChanged);
        }

        private void PlaceBomb(BombermanEntity entity)
        {
            if (_isOnReset || !_gridStateManager.CanPlaceBomb(entity)) return;

            (List<GridPos> bombingGridPos, List<Vector3> bombingWorldPos) = _gridStateManager.OnPlaceBomb(entity);
            BombHandler newBomb = _bombManager.SpawnBomb(entity, bombingGridPos, bombingWorldPos);
            _gridStateManager.RegisterActiveBomb(newBomb, bombingGridPos[0]);
            entity.BombCount--;
        }

        private void OnBombExplode(BombermanEntity placer, List<GridPos> explosionGridPos)
        {
            if (_isOnReset) return;

            _gridStateManager.OnBombExplode(explosionGridPos);
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

        private void CheckExplosionVictim(BombermanEntity placer, List<GridPos> explosionGridPos)
        {
            bool deadVictimExists = false;
            foreach (GridPos tilePos in explosionGridPos)
            {
                BombermanEntity victim = _gridStateManager.GetCharacterAt(tilePos);
                if(victim != null && victim.State != EntityState.Dead)
                {
                    deadVictimExists = true;

                    // Check kill type on someone died
                    KillType killType = KillType.NormalKill;
                    if (victim == placer) killType = KillType.Suicide;
                    else if (victim.CharacterType == placer.CharacterType) killType = KillType.FriendlyFire;

                    // Trigger dead and kill event on placer and victim
                    victim.Dead(killType == KillType.Suicide);
                    placer.Kill(killType);

                    CheckWinCondition();

                    if (_isOnTrainingAgent) ResetGrid(placer.CharacterType, killType);
                }
            }
            if(deadVictimExists) _cameraTransform.DOShakeRotation(_shakeDuration, _shakeStrength, _shakeVibrato, 90, true, ShakeRandomnessMode.Full);
        }

        private void CheckWinCondition()
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
                OnCharacterWin?.Invoke(winnerType[0]);
            }
        }

        private void OnExplosionFinish(BombermanEntity entity, List<GridPos> explosionGridPos)
        {
            _gridStateManager.OnExplosionFinish(explosionGridPos);
            entity.BombCount++;
        }

        private void ResetGrid(CharacterType placerType, KillType killType)
        {
            if (_isOnReset) return;
            _isOnReset = true;

            List<Material> floorMaterials = _levelBuilder.FloorMaterials;

            // Visualize episode result on floor
            foreach (GameObject item in _floors)
            {
                MeshRenderer floorRenderer = item.GetComponent<MeshRenderer>();
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
                }
            }

            // Delay reset to give some time for observing arena result
            DOVirtual.DelayedCall(_resetDelay, () =>
            {
                _bombManager.ResetAllBombs();

                // Reset all props
                List<(IDestroyableProps prop, Vector3 originPos)> originProps = _gridStateManager.GetPropsOriginPos();
                originProps.ForEach(item => item.prop.ResetProp(item.originPos));

                // Reset floor color material
                foreach (GameObject item in _floors)
                {
                    MeshRenderer floorRenderer = item.GetComponent<MeshRenderer>();
                    if (floorRenderer) floorRenderer.material = floorMaterials[0];
                }

                _isOnReset = false;
            });
        }

    }

}
