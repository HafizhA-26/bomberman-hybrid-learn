using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BombermanRL.LevelTilemapData;

namespace BombermanRL
{
    [CreateAssetMenu(fileName = "TilePrefabsData", menuName = "Bomberman/TilePrefabsData")]
    public class TilePrefabsData : ScriptableObject
    {
        [Tooltip("Tile Prefabs")]
        [SerializeField] private List<TilePrefab> _tilePrefabs = new List<TilePrefab>();

        [Tooltip("Prefab of the level floor")]
        [SerializeField] private GameObject _floorPrefab;
        public Dictionary<TileType, TilePrefab> TilePrefabDict { get => _tilePrefabs.ToDictionary(item => item.TileType); }
        public GameObject FloorPrefab { get => _floorPrefab; }

        [Serializable]
        public class TilePrefab
        {
            public TileType TileType;
            public GameObject PrefabObject;
            public Vector3 OffsetSpawn;
        }
    }
}
