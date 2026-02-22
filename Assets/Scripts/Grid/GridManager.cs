using BombermanRL.Character;
using BombermanRL.Props;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private LevelTilemapData _levelData;
        [SerializeField] private TilePrefabsData _tilePrefabsData;
        [SerializeField] private Transform _floorsParent;
        [SerializeField] private Transform _objectsTileParent;
        [SerializeField] private bool _isOnTrainingAgent = true;

        private GameObject[,] _floors;
        private GameObject[,] _tiles;
        private TileState[,] _grid;
        private PlayerController _player;
        private List<GridPos> _validRespawnPos = new List<GridPos>();

        private readonly List<EnemyController> _enemies = new List<EnemyController>();
        private readonly Dictionary<GridPos, BombHandler> _placedBomb = new Dictionary<GridPos, BombHandler>();
        private readonly Dictionary<GridPos, IDestroyableProps> _destroyableProps = new Dictionary<GridPos, IDestroyableProps>(); 
        private readonly Dictionary<IBombermanCharacter, GridPos> _entityPositions = new Dictionary<IBombermanCharacter, GridPos>();


        private Vector3 _tileSize;
        private bool _isOnReset;

        private void Awake()
        {
            Application.targetFrameRate = 30;
            _floors = new GameObject[_levelData.GridWidth, _levelData.GridHeight];
            _tiles = new GameObject[_levelData.GridWidth, _levelData.GridHeight];
            _grid = new TileState[_levelData.GridWidth, _levelData.GridHeight];
        }

        private void Start()
        {
            CreateFloor();
            LoadLevelTile();
        }


        private void CreateFloor()
        {
            if (_floorsParent == null)
                return;

            _tileSize = _tilePrefabsData.FloorPrefab.transform.lossyScale;
            for (int i = 0; i < _floors.GetLength(0); i++)
            {
                for (int j = 0; j < _floors.GetLength(1); j++)
                {
                    GameObject floor = Instantiate(_tilePrefabsData.FloorPrefab, _floorsParent, true);
                    Vector3 newPos = new Vector3(_tileSize.x * j + transform.parent.position.x, _tileSize.y * 0.5f, _tileSize.z * i * -1 + transform.parent.position.z);
                    floor.transform.position = newPos;
                    floor.name = $"Floor[{i}-{j}]";
                    _floors[i, j] = floor;
                }
            }
        }

        private void LoadLevelTile()
        {
            if (_floors[0, 0] == null) return;

            Dictionary<TileType, TilePrefabsData.TilePrefab> tilePrefabDict = _tilePrefabsData.TilePrefabDict;
            int enemyCount = 0;

            for (int i = 0; i < _levelData.LevelTiles.Count; i++)
            {
                int row = i / _levelData.GridWidth;
                int col = i % _levelData.GridHeight;

                TileType type = _levelData.LevelTiles[i];
                GridPos tileGridPos = new GridPos(row, col);
                _grid[row, col] = new(type);
                if (type == TileType.Empty)
                {
                    _validRespawnPos.Add(tileGridPos);
                    continue;
                }

                GameObject tile = Instantiate(tilePrefabDict[type].PrefabObject, _objectsTileParent, true);
                Vector3 newPos = GridToWorld(tileGridPos);

                newPos += tilePrefabDict[type].OffsetSpawn;
                tile.transform.position = newPos;

                // Save instantiated object referensce
                switch (type)
                {
                    case TileType.Wall:
                        tile.name = $"{type.ToString()}[{row}-{col}]";
                        _tiles[row, col] = tile;
                        break;
                    case TileType.Crate:
                        tile.name = $"{type.ToString()}[{row}-{col}]";
                        _tiles[row, col] = tile;
                        _destroyableProps[tileGridPos] = tile.GetComponent<CrateHandler>();
                        break;
                    case TileType.PlayerSpawn:
                        tile.name = "Player";
                        _grid[row, col] = new(TileType.Empty);
                        _grid[row, col].AddSubstate(TileSubState.OnCharacter);
                        _player = tile.GetComponent<PlayerController>();
                        _entityPositions[_player] = tileGridPos;
                        _player.OffsetMovement = tilePrefabDict[type].OffsetSpawn;
                        _player.OnRequestMove.AddListener((Vector2 direction) => MoveEntity(_player, _entityPositions[_player], direction));
                        _player.OnRequestPlaceBomb.AddListener(() => PlaceBomb(_player, _entityPositions[_player]));
                        _player.OnRequestGameplayState += () => GetCurrentState(_player, _player.NearbyObserveRadius);
                        break;
                    case TileType.EnemySpawn:
                        tile.name = $"Enemy{_enemies.Count}";
                        Debug.Log("Spawned "+tile.name);
                        _grid[row, col] = new(TileType.Empty);
                        _grid[row, col].AddSubstate(TileSubState.OnCharacter);
                        EnemyController enemy = tile.GetComponent<EnemyController>();
                        _enemies.Add(enemy);
                        _entityPositions[enemy] = tileGridPos;
                        enemy.OffsetMovement = tilePrefabDict[type].OffsetSpawn;
                        enemy.OnRequestMove.AddListener((Vector2 direction) => MoveEntity(enemy, _entityPositions[enemy], direction));
                        enemy.OnRequestPlaceBomb.AddListener(() => PlaceBomb(enemy, _entityPositions[enemy]));
                        enemy.OnRequestGameplayState += () => GetCurrentState(enemy, enemy.NearbyObserveRadius);
                        enemyCount++;
                        break;
                }
                
            }

        }

        private void ResetGrid(bool isEnemyKilledPlayer, bool isPlayerSuicide)
        {
            if (_isOnReset) return;
            _isOnReset = true;

            foreach (GameObject item in _floors)
            {
                MeshRenderer floorRenderer = item.GetComponent<MeshRenderer>();
                if (isEnemyKilledPlayer)
                    floorRenderer.material = _tilePrefabsData.AgentSuccessFloorMat;
                else if (isPlayerSuicide)
                    floorRenderer.material = _tilePrefabsData.AgentNeutralFloorMat;
                else
                    floorRenderer.material = _tilePrefabsData.AgentFailedFloorMat;
            }

            foreach (KeyValuePair<GridPos, BombHandler> item in _placedBomb)
            {
                if(item.Value == null) continue;
                item.Value.StopExplosion();
            }

            // Search random valid respawn point
            GridPos playerSpawnPos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)];
            _player.ResetEntity(GridToWorld(playerSpawnPos), 2f);

            GridPos enemySpawnPos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)];
            while(enemySpawnPos.Distance(playerSpawnPos) <= 2)
            {
                enemySpawnPos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)];
            }
            _enemies[0].ResetEntity(GridToWorld(enemySpawnPos), 2f);

            DOVirtual.DelayedCall(2f, () =>
            {
                // Reset entity pos
                if (_entityPositions.ContainsKey(_player)) _entityPositions[_player] = playerSpawnPos;
                else Debug.LogError("Can't find player in entity pos");

                if (_entityPositions.ContainsKey(_enemies[0])) _entityPositions[_enemies[0]] = enemySpawnPos;
                else Debug.LogError("Can't find enemy in entity pos");

                foreach (KeyValuePair<GridPos, BombHandler> item in _placedBomb)
                {
                    if (item.Value == null) continue;
                    Destroy(item.Value.gameObject);
                    //Debug.Log("[Reset Grid] Destroy unexploded bomb at " + item.Key);
                }

                // Reset Tile States
                for (int i = 0; i < _levelData.LevelTiles.Count; i++)
                {
                    int row = i / _levelData.GridWidth;
                    int col = i % _levelData.GridHeight;

                    TileType type = _levelData.LevelTiles[i];
                    _grid[row, col] = new(type);
                    _grid[row, col].ResetSubstate();
                    GridPos tileGridPos = new GridPos(row, col);

                    switch (type)
                    {
                        case TileType.Crate:
                            _tiles[row, col].GetComponent<IDestroyableProps>().ResetProp(GridToWorld(tileGridPos));
                            break;
                        case TileType.PlayerSpawn:
                        case TileType.EnemySpawn:
                            _grid[row, col] = new(TileType.Empty);

                            break;
                    }
                }

                // Reset floor color material
                foreach (GameObject item in _floors)
                {
                    MeshRenderer floorRenderer = item.GetComponent<MeshRenderer>();
                    if (floorRenderer) floorRenderer.material = _tilePrefabsData.FloorPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                }

                _isOnReset = false;
            });
        }

        /// <summary>
        /// Check if character can move to target grid pos
        /// </summary>
        /// <param name="fromPos">Origin position, X for grid row, Y for grid column</param>
        /// <param name="direction">X for move horizontal (>0 right, <0 left), Y for vertical (>0 top, <0 bottom)</param>
        /// <return>Is target tile to move available</return>
        private bool CanMove(GridPos fromPos, Vector2 direction)
        {
            TileState nextTile = new TileState(TileType.Empty);
            if(direction.y > 0)
            {
                if (fromPos.row == 0) return false;
                nextTile = _grid[fromPos.row - 1, fromPos.col];
            }
            else if(direction.y < 0)
            {
                if (fromPos.row == _grid.GetLength(0) - 1) return false;
                nextTile = _grid[fromPos.row + 1, fromPos.col];
            }
            else if(direction.x > 0)
            {
                if (fromPos.col == _grid.GetLength(1) - 1) return false;
                nextTile = _grid[fromPos.row, fromPos.col + 1];
            }
            else if(direction.x < 0)
            {
                if (fromPos.col == 0) return false;
                nextTile = _grid[fromPos.row, fromPos.col - 1];
            }

            bool isMovable = nextTile.Type == TileType.Empty &&
                !nextTile.HasSubstate(TileSubState.OnCharacter) &&
                !nextTile.HasSubstate(TileSubState.OnBomb);

            return isMovable;
        }
        private bool CanPlaceBomb(GridPos tilePos) 
            => !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnBomb) &&
            !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnExplosion);
        private bool CanExplosionSpawn(GridPos tilePos) =>
            tilePos.row < _grid.GetLength(0) && 
            tilePos.row >= 0 &&
            tilePos.col < _grid.GetLength(1) &&
            tilePos.col >= 0 &&
            _grid[tilePos.row, tilePos.col].Type != TileType.Wall;

        private bool IsExplosionBlocked(GridPos tilePos) => _grid[tilePos.row, tilePos.col].Type == TileType.Crate;
        private bool IsDeadly(GridPos tilePos) => _grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnExplosion);

        private void PlaceBomb(IBombermanCharacter entity, GridPos tilePos)
        {
            if (_isOnReset) return;
            if(!CanPlaceBomb(tilePos)) return;

            List<GridPos> explosionGridPos = new List<GridPos>();
            GameObject bombObject = Instantiate(_tilePrefabsData.BombPrefab, _objectsTileParent, true);
            bombObject.transform.position = GridToWorld(tilePos);
            bombObject.name = $"Bomb[{tilePos.row}-{tilePos.col}]";
            _grid[tilePos.row, tilePos.col].AddSubstate(TileSubState.OnBomb);
            
            // Initialize bomb component
            if(bombObject.TryGetComponent(out BombHandler bomb))
            {
                List<Vector3> explosionWorldPos = new List<Vector3>();
                int explosionRadius = bomb.ExplosionRadius;

                explosionWorldPos.Add(GridToWorld(tilePos)); // Add bomb self position into first explosion pos
                explosionGridPos.Add(new GridPos(tilePos.row, tilePos.col));

                // Propagate explosion tile with + shape based on bomb's explosion radius
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 1; j <= explosionRadius; j++)
                    {
                        GridPos explosionPos = new();
                        if(i == 0) explosionPos = new GridPos(tilePos.row + j, tilePos.col);
                        else if (i == 1) explosionPos = new GridPos(tilePos.row, tilePos.col + j);
                        else if (i == 2) explosionPos = new GridPos(tilePos.row - j, tilePos.col);
                        else if (i == 3) explosionPos = new GridPos(tilePos.row, tilePos.col - j);

                        if (CanExplosionSpawn(explosionPos))
                        {
                            explosionGridPos.Add(explosionPos);
                            explosionWorldPos.Add(GridToWorld(explosionPos));
                            // Skipped further propagation if tile is blocked by object
                            if (IsExplosionBlocked(explosionPos)) break; 
                        }
                        else break;
                    }
                }


                bomb.OnBombExplode.AddListener(() => OnBombExplode(entity, explosionGridPos));
                bomb.OnTickExplosion.AddListener(() => OnTickExplosion(entity, explosionGridPos));
                bomb.OnExplosionFinish.AddListener(() => OnExplosionFinish(entity, explosionGridPos));
                bomb.Initalize(explosionWorldPos);
                _placedBomb[tilePos] = bomb;
                entity.BombCount++;
                //Debug.Log($"[Bomb Placed] from {entity.Name} on Tile [{tilePos.row},{tilePos.col}]");
            }
        }

        private void OnBombExplode(IBombermanCharacter placer, List<GridPos> explosionGridPos)
        {
            if (_isOnReset) return;

            // Change substate OnBomb and OnExplosion to exploding tiles
            foreach (GridPos item in explosionGridPos)
            {
                //Debug.Log($"[Bomb Explode] Tile [{item.row}-{item.col}]" + _grid[item.row, item.col]);
                if(_grid[item.row, item.col].HasSubstate(TileSubState.OnBomb)) _grid[item.row, item.col].RemoveSubstate(TileSubState.OnBomb);
                _grid[item.row, item.col].AddSubstate(TileSubState.OnExplosion);

                // Check destroyable props to destroy
                if (_destroyableProps.ContainsKey(item) && !_destroyableProps[item].IsDestroyed && _destroyableProps[item].CanBeDestroyedBy(placer.Type))
                {
                    if(_grid[item.row, item.col].Type == TileType.Crate)
                    {
                        _grid[item.row, item.col].Type = TileType.Empty;
                        _grid[item.row, item.col].AddSubstate(TileSubState.OnDestroyedCrate);
                    }
                    _destroyableProps[item].DestroyProps();
                    placer.DestroyProps(_destroyableProps[item]);
                }
            }
            
        }

        private void OnTickExplosion(IBombermanCharacter placer, List<GridPos> explosionGridPos)
        {
            // Check bomberman characters deadly tiles 
            foreach (KeyValuePair<IBombermanCharacter, GridPos> entityPos in _entityPositions)
            {
                if (IsDeadly(entityPos.Value) && !entityPos.Key.IsDead)
                {
                    entityPos.Key.Dead(placer.Name.Equals(entityPos.Key.Name));
                    placer.Kill(entityPos.Key);

                    bool isEnemyKilledPlayer = placer.Type == CharacterType.Bandit && entityPos.Key.Type == CharacterType.Player;
                    bool isPlayerSuicide = placer.Type == CharacterType.Player && entityPos.Key.Type == CharacterType.Player;

                    if (isEnemyKilledPlayer || isPlayerSuicide)
                        _enemies[0].Win();
                    else
                        _player.Win();

                    if (_isOnTrainingAgent) ResetGrid(isEnemyKilledPlayer, isPlayerSuicide);
                }
            }
        }

        /// <summary>
        /// Event listener on any finished bomb explosion
        /// </summary>
        /// <param name="entity">Entity that placed the bomb</param>
        /// <param name="explosionGridPos">Bomb Explosion Tiles position</param>
        private void OnExplosionFinish(IBombermanCharacter entity, List<GridPos> explosionGridPos)
        {
            foreach (GridPos item in explosionGridPos)
                _grid[item.row, item.col].RemoveSubstate(TileSubState.OnExplosion);

            if (_placedBomb.ContainsKey(explosionGridPos[0])) _placedBomb.Remove(explosionGridPos[0]);
            entity.BombCount--;
        }


        /// <summary>
        /// Event listener on any movable entity request to move between tiles in grid
        /// </summary>
        /// <param name="entity">Movable Character</param>
        /// <param name="fromPos">Origin position on grid</param>
        /// <param name="moveDirection">Direction to move. -1 to left/down, 1 to right/up</param>
        /// <returns>True if success move</returns>
        private void MoveEntity(IBombermanCharacter entity, GridPos fromPos, Vector2 moveDirection)
        {
            //Debug.Log($"Try move Entity {entity.Type} {fromPos} with Direction {moveDirection}");
            if (Math.Abs(moveDirection.x) <= 0.1f && Math.Abs(moveDirection.y) <= 0.1f) return;
            if (_isOnReset) return;

            bool canMove = CanMove(fromPos, moveDirection);

            moveDirection.x = Math.Sign(moveDirection.x);
            moveDirection.y = Math.Sign(moveDirection.y * -1);

            GridPos targetGridPos = new GridPos((int)(fromPos.row + moveDirection.y), (int)(fromPos.col + moveDirection.x));
            Vector3 targetWorldPos = GridToWorld(targetGridPos);
            targetWorldPos += entity.OffsetMovement;
            //Debug.Log($"Move Entity {fromPos} to {targetGridPos} | CanMove {canMove}");

            if (canMove) _grid[targetGridPos.row, targetGridPos.col].AddSubstate(TileSubState.OnCharacter);
            entity.Move(targetWorldPos, canMove, onTileChanged: () =>
            {
                //Debug.Log($"{entity.Name} tile changed");
                _grid[fromPos.row, fromPos.col].RemoveSubstate(TileSubState.OnCharacter);
                _entityPositions[entity] = targetGridPos;
            });
        }

        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.col * _tileSize.x + transform.parent.position.x, _tileSize.y * 1.5f, tilePos.row * _tileSize.z * -1 + transform.parent.position.z);

        private GameplayState GetCurrentState(IBombermanCharacter entity, int NearbyRadius)
        {
            Dictionary<GridPos, TileState> nearbyCondition = new Dictionary<GridPos, TileState>();
            Dictionary<GridPos, float> bombTimerNorm = new Dictionary<GridPos, float>();

            // Get nearby tiles condition based on radius
            GridPos entityPos = _entityPositions[entity];
            int startRow = Mathf.Clamp(entityPos.row - NearbyRadius, 0, _grid.GetLength(0) - 1);
            int endRow = Mathf.Clamp(entityPos.row + NearbyRadius, 0, _grid.GetLength(0) - 1);
            int startCol = Mathf.Clamp(entityPos.col - NearbyRadius, 0, _grid.GetLength(1) - 1);
            int endCol = Mathf.Clamp(entityPos.col + NearbyRadius, 0, _grid.GetLength(1) - 1);

            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startCol; j <= endCol; j++)
                {
                    GridPos pos = new GridPos(i, j);
                    nearbyCondition[pos] = _grid[i, j];

                    if (_grid[i, j].HasSubstate(TileSubState.OnBomb))
                        bombTimerNorm[new GridPos(i, j)] = _placedBomb.ContainsKey(pos) ? _placedBomb[pos].GetCurrentTimerNorm() : -1;
                }
            }

            GridPos enemyPos = new GridPos(-1, -1);

            switch (entity.Type)
            {
                case CharacterType.None:
                    break;
                case CharacterType.Player:
                    enemyPos = _entityPositions.FirstOrDefault(item => item.Key.Type!= CharacterType.Player).Value;
                    break;
                case CharacterType.Bandit:
                    enemyPos = _entityPositions[_player];
                    break;
            }

            return new
                (
                    _entityPositions[entity],
                    nearbyCondition,
                    enemyPos,
                    bombTimerNorm,
                    NearbyRadius
                );
        }
    }

}
