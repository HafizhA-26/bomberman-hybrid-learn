using BombermanRL.Character;
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

        private GameObject[,] _floors;
        private GameObject[,] _tiles;
        private TileState[,] _grid;
        private List<EnemyController> _enemies = new List<EnemyController>();
        private PlayerController _player;

        private GridPos _playerGridPos;
        private Vector3 _tileSize;

        public struct GridPos
        {
            public GridPos(int row, int col)
            {
                this.row = row;
                this.col = col;
            }
            public int row, col;
            public override string ToString()
            {
                return $"[{row},{col}]";
            }

        }

        private void Awake()
        {
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
                    Vector3 newPos = new Vector3(_tileSize.x * j, _tileSize.y * 0.5f, _tileSize.z * i * -1);
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
            for (int i = 0; i < _levelData.LevelTiles.Count; i++)
            {
                int row = i / _levelData.GridWidth;
                int col = i % _levelData.GridHeight;

                TileType type = _levelData.LevelTiles[i];
                _grid[row, col] = new(type);
                if (type == TileType.Empty) continue; // Skip instantiation if empty

                GameObject tile = Instantiate(tilePrefabDict[type].PrefabObject, _objectsTileParent, true);
                Vector3 newPos = GridToWorld(row, col);
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
                        tile.GetComponent<CrateHandler>().OnCrateDestroyed.AddListener(() => OnCrateDestroyed(new GridPos(row, col)));
                        break;
                    case TileType.PlayerSpawn:
                        tile.name = "Player";
                        _grid[row, col] = new(TileType.Empty);
                        _grid[row, col].AddSubstate(TileSubState.OnPlayer);
                        _player = tile.GetComponent<PlayerController>();
                        _playerGridPos = new GridPos(row, col);
                        _player.OffsetMovement = tilePrefabDict[type].OffsetSpawn;
                        _player.OnRequestMove.AddListener((Vector2 direction) => MoveEntity(_player, _playerGridPos, direction));
                        _player.OnRequestPlaceBomb.AddListener(() => PlaceBomb(_player, _playerGridPos));
                        break;
                    case TileType.EnemySpawn:
                        tile.name = $"Enemy{_enemies.Count}";
                        _grid[row, col] = new(TileType.Empty);
                        _grid[row, col].AddSubstate(TileSubState.OnEnemy);
                        _enemies.Add(tile.GetComponent<EnemyController>());
                        break;
                }
                
            }
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
                Debug.Log("[CanMove] Check 1");
                if (fromPos.row == 0) return false;
                nextTile = _grid[fromPos.row - 1, fromPos.col];
            }
            else if(direction.y < 0)
            {
                Debug.Log("[CanMove] Check 2");
                if (fromPos.row == _grid.GetLength(0) - 1) return false;
                nextTile = _grid[fromPos.row + 1, fromPos.col];
            }
            else if(direction.x > 0)
            {
                Debug.Log($"[CanMove] Check 3 {fromPos.col} = {_grid.GetLength(1) - 1}");
                if (fromPos.col == _grid.GetLength(1) - 1) return false;
                nextTile = _grid[fromPos.row, fromPos.col + 1];
            }
            else if(direction.x < 0)
            {
                Debug.Log("[CanMove] Check 4");
                if (fromPos.col == 0) return false;
                nextTile = _grid[fromPos.row, fromPos.col - 1];
            }

            Debug.Log("Next Tile Condition: "+nextTile);
            bool isMovable = nextTile.Type == TileType.Empty && 
                !nextTile.HasSubstate(TileSubState.OnPlayer) && 
                !nextTile.HasSubstate(TileSubState.OnBomb) &&
                !nextTile.HasSubstate(TileSubState.OnExplosion) &&
                !nextTile.HasSubstate(TileSubState.OnEnemy);

            return isMovable;
        }
        private bool CanPlaceBomb(GridPos tilePos) 
            => !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnBomb) &&
            !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnExplosion);
        private bool IsDeadly(GridPos tilePos) => _grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnExplosion);

        private void PlaceBomb(IBombermanCharacter entity, GridPos tilePos)
        {
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

                // Propagate explosion tile based on bomb's explosion radius
                for (int i = 1; i <= explosionRadius; i++)
                {
                    if(tilePos.row + i <= _grid.GetLength(0) - 1)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.row + i, tilePos.col));
                        explosionGridPos.Add(new GridPos(tilePos.row + i, tilePos.col));
                    }
                    if(tilePos.col + i <= _grid.GetLength(1) - 1)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.row, tilePos.col + i));
                        explosionGridPos.Add(new GridPos(tilePos.row, tilePos.col + i));
                    }
                    if(tilePos.row - i >= 0)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.row - i, tilePos.col));
                        explosionGridPos.Add(new GridPos(tilePos.row - i, tilePos.col));
                    }
                    if(tilePos.col - i >= 0)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.row, tilePos.col - i));
                        explosionGridPos.Add(new GridPos(tilePos.row, tilePos.col - i));
                    }
                }
                bomb.OnBombExplode.AddListener(() => OnBombExplode(explosionGridPos));
                bomb.OnExplosionFinish.AddListener(() => OnExplosionFinish(entity, explosionGridPos));
                bomb.Initalize(explosionWorldPos);
                entity.BombCount++;
            }
        }

        private void OnBombExplode(List<GridPos> explosionGridPos)
        {
            // Change substate OnBomb and OnExplosion to exploding tiles
            foreach (GridPos item in explosionGridPos)
            {
                Debug.Log($"[Bomb Explode] Tile [{item.row}-{item.col}]" + _grid[item.row, item.col]);
                if(_grid[item.row, item.col].HasSubstate(TileSubState.OnBomb)) _grid[item.row, item.col].RemoveSubstate(TileSubState.OnBomb);
                _grid[item.row, item.col].AddSubstate(TileSubState.OnExplosion);
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

            entity.BombCount--;
        }

        /// <summary>
        /// Event listener on any crate destroyed inside grid
        /// Will change grid type and substate
        /// </summary>
        /// <param name="cratePos">Crate Explosion Coordinate</param>
        private void OnCrateDestroyed(GridPos cratePos)
        {
            _grid[cratePos.row, cratePos.col].Type = TileType.Empty;
            _grid[cratePos.row, cratePos.col].AddSubstate(TileSubState.OnDestroyedCrate);
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
            if (Math.Abs(moveDirection.x) <= 0.1f && Math.Abs(moveDirection.y) <= 0.1f) return;
            bool canMove = CanMove(fromPos, moveDirection);

            moveDirection.x = Math.Sign(moveDirection.x);
            moveDirection.y = Math.Sign(moveDirection.y * -1);

            GridPos targetGridPos = new GridPos((int)(fromPos.row + moveDirection.y), (int)(fromPos.col + moveDirection.x));
            Vector3 targetWorldPos = GridToWorld(targetGridPos);
            targetWorldPos += entity.OffsetMovement;
            Debug.Log($"Move Entity {fromPos} to {targetGridPos} | CanMove {canMove}");

            if (canMove) _grid[targetGridPos.row, targetGridPos.col].AddSubstate(TileSubState.OnPlayer);
            entity.Move(targetWorldPos, canMove, () =>
            {
                _grid[fromPos.row, fromPos.col].RemoveSubstate(TileSubState.OnPlayer);
                _playerGridPos = targetGridPos;
            });
        }

        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.col * _tileSize.x, _tileSize.y * 1.5f, tilePos.row * _tileSize.z * -1);
        private Vector3 GridToWorld(int x, int y) => new Vector3(y * _tileSize.x, _tileSize.y * 1.5f, x * _tileSize.z * -1);
    }

}
