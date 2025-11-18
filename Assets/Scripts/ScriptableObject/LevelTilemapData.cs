using System.Collections.Generic;
using UnityEngine;
namespace BombermanRL
{

    [CreateAssetMenu(fileName = "LevelTilemapData", menuName = "Bomberman/LevelTilemapData")]
    public class LevelTilemapData : ScriptableObject
    {
        [Tooltip("Width of Level Grid")]
        [SerializeField] private int _gridWidth = 4;
        [Tooltip("Height of Level Grid")]
        [SerializeField] private int _gridHeight = 4;

        [Tooltip("Base Level Tile Format")]
        [SerializeField]
        private List<TileType> _levelTiles = new List<TileType> {
            TileType.Empty, TileType.Empty, TileType.Empty, TileType.Empty,
            TileType.Empty, TileType.Empty, TileType.Empty, TileType.Empty,
            TileType.Empty, TileType.Empty, TileType.Empty, TileType.Empty,
            TileType.Empty, TileType.Empty, TileType.Empty, TileType.Empty
        };

        public int GridWidth { get => _gridWidth; }
        public int GridHeight { get => _gridHeight; }
        public List<TileType> LevelTiles { get => _levelTiles; }

        private void OnValidate()
        {
            if (_gridWidth <= 0 || _gridHeight <= 0) return;

            if(_levelTiles.Count < _gridHeight * _gridWidth)
            {
                int missingTileCount = (_gridWidth * _gridHeight) - _levelTiles.Count;
                for (int i = 0; i < missingTileCount; i++)
                {
                    _levelTiles.Add(TileType.Empty);
                }
                Debug.LogWarning($"Missing {missingTileCount} tiles. Adding {missingTileCount} empty tiles");
            }
            else if(_levelTiles.Count > _gridHeight * _gridWidth)
            {
                int excessiveTileCount = _levelTiles.Count - (_gridHeight * _gridWidth);
                _levelTiles.RemoveRange(_gridWidth * _gridHeight, excessiveTileCount);
                Debug.LogWarning($"Tile Size Exceed Grid Width & Height Limit! Removing {excessiveTileCount} tiles");
            }
        }
    }
}

