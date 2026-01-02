using System.Collections.Generic;
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
        private Vector3 _tileSize;

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
                if (type == TileType.Empty) continue; // Skip instantiation if empty;
                if (type == TileType.PlayerSpawn || type == TileType.EnemySpawn)
                {
                    _grid[row, col] = new(TileType.Empty);
                    if (type == TileType.PlayerSpawn) _grid[row, col].AddSubstate(TileSubState.OnPlayer);
                    else if(type == TileType.EnemySpawn)  _grid[row, col].AddSubstate(TileSubState.OnEnemy);
                }

                GameObject tile = Instantiate(tilePrefabDict[type].PrefabObject, _objectsTileParent, true);
                Vector3 newPos = new Vector3(_tileSize.x * row, _tileSize.y * 1.5f, _tileSize.z * col * -1);
                newPos += tilePrefabDict[type].OffsetSpawn;
                tile.transform.position = newPos;
                tile.name = $"{type.ToString()}[{row}-{col}]";

                _tiles[row, col] = tile;
            }
        }
    }

}
