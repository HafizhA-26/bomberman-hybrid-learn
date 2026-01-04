using BombermanRL.Character;
using DG.Tweening;
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

        private Vector3 _tileSize;

        public struct GridPos
        {
            public GridPos(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
            public int x, y;

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
                    Vector3 newPos = new Vector3(_tileSize.x * i, _tileSize.y * 0.5f, _tileSize.z * j * -1);
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
                    case TileType.Crate:
                        tile.name = $"{type.ToString()}[{row}-{col}]";
                        _tiles[row, col] = tile; // Save reference to inmovable object
                        break;
                    case TileType.PlayerSpawn:
                        tile.name = "Player";
                        _grid[row, col] = new(TileType.Empty);
                        _grid[row, col].AddSubstate(TileSubState.OnPlayer);
                        _player = tile.GetComponent<PlayerController>();
                        break;
                    case TileType.EnemySpawn:
                        tile.name = $"Enemy{_enemies.Count}";
                        _grid[row, col] = new(TileType.Empty);
                        _grid[row, col].AddSubstate(TileSubState.OnEnemy);
                        _enemies.Add(tile.GetComponent<EnemyController>());
                        break;
                }
                
            }
            DOVirtual.DelayedCall(3f, () => PlaceBomb(new GridPos() { x = 0, y = 0 }));
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
            if(direction.x > 0)
            {
                if (fromPos.x == _grid.GetLength(0) - 1) return false;
                nextTile = _grid[fromPos.x + 1, fromPos.y];
            }
            else if(direction.x < 0)
            {
                if (fromPos.x == 0) return false;
                nextTile = _grid[fromPos.x - 1, fromPos.y];
            }
            else if(direction.y > 0)
            {
                if (fromPos.y == _grid.GetLength(1) - 1) return false;
                nextTile = _grid[fromPos.x, fromPos.y + 1];
            }
            else if(direction.y < 0)
            {
                if (fromPos.y == 0) return false;
                nextTile = _grid[fromPos.x, fromPos.y - 1];
            }

            bool isMovable = nextTile.Type == TileType.Empty && 
                !nextTile.HasSubstate(TileSubState.OnPlayer) && 
                !nextTile.HasSubstate(TileSubState.OnBomb) &&
                !nextTile.HasSubstate(TileSubState.OnEnemy);

            return isMovable;
        }
        private bool CanPlaceBomb(GridPos tilePos) 
            => !_grid[tilePos.x, tilePos.y].HasSubstate(TileSubState.OnBomb) &&
            !_grid[tilePos.x, tilePos.y].HasSubstate(TileSubState.OnExplosion);
        private bool IsDeadly(GridPos tilePos) => _grid[tilePos.x, tilePos.y].HasSubstate(TileSubState.OnExplosion);

        private void PlaceBomb(GridPos tilePos)
        {
            if(!CanPlaceBomb(tilePos)) return;

            List<GridPos> explosionGridPos = new List<GridPos>();
            GameObject bombObject = Instantiate(_tilePrefabsData.BombPrefab, _objectsTileParent, true);
            bombObject.transform.position = GridToWorld(tilePos);
            bombObject.name = $"Bomb[{tilePos.x}-{tilePos.y}]";
            _grid[tilePos.x, tilePos.y].AddSubstate(TileSubState.OnBomb);
            
            if(bombObject.TryGetComponent<BombHandler>(out BombHandler bomb))
            {
                List<Vector3> explosionWorldPos = new List<Vector3>();
                int explosionRadius = bomb.ExplosionRadius;

                explosionWorldPos.Add(GridToWorld(tilePos));
                for (int i = 1; i <= explosionRadius; i++)
                {
                    if(tilePos.x + i <= _grid.GetLength(0) - 1)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.x + i, tilePos.y));
                        explosionGridPos.Add(new GridPos(tilePos.x + i, tilePos.y));
                    }
                    if(tilePos.y + i <= _grid.GetLength(1) - 1)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.x, tilePos.y + i));
                        explosionGridPos.Add(new GridPos(tilePos.x, tilePos.y + i));
                    }
                    if(tilePos.x - i >= 0)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.x - i, tilePos.y));
                        explosionGridPos.Add(new GridPos(tilePos.x - i, tilePos.y));
                    }
                    if(tilePos.y - i >= 0)
                    {
                        explosionWorldPos.Add(GridToWorld(tilePos.x, tilePos.y - i));
                        explosionGridPos.Add(new GridPos(tilePos.x, tilePos.y - i));
                    }
                }
                bomb.Initalize(explosionWorldPos);
                bomb.OnBombExplode.AddListener(() => OnBombExplode(explosionGridPos));
                bomb.OnExplosionFinish.AddListener(() => OnExplosionFinish(explosionGridPos));
            }
        }

        private void OnBombExplode(List<GridPos> explosionGridPos)
        {
            foreach (GridPos item in explosionGridPos)
            {
                if(_grid[item.x, item.y].HasSubstate(TileSubState.OnBomb)) _grid[item.x, item.y].RemoveSubstate(TileSubState.OnBomb);
                _grid[item.x, item.y].AddSubstate(TileSubState.OnExplosion);
            }
        }

        private void OnExplosionFinish(List<GridPos> explosionGridPos)
        {
            foreach (GridPos item in explosionGridPos)
                _grid[item.x, item.y].RemoveSubstate(TileSubState.OnExplosion);
        }

        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.x * _tileSize.x, _tileSize.y * 1.5f, tilePos.y * _tileSize.z * -1);
        private Vector3 GridToWorld(int x, int y) => new Vector3(x * _tileSize.x, _tileSize.y * 1.5f, y * _tileSize.z * -1);
    }

}
