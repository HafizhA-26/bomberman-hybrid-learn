using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL
{
    [CreateAssetMenu(fileName = "TilePrefabsData", menuName = "Bomberman/TilePrefabsData")]
    public class TilePrefabsData : ScriptableObject
    {
        [Tooltip("Tile Prefabs")]
        [SerializeField] private List<TilePrefab> _tilePrefabs = new List<TilePrefab>();

        [Tooltip("Prefab of the level floor")]
        [SerializeField] private GameObject _floorPrefab;

        [Tooltip("Prefab for placing bomb")]
        [SerializeField] private GameObject _bombPrefab;
        public Dictionary<TileType, TilePrefab> TilePrefabDict { get => _tilePrefabs.ToDictionary(item => item.TileType); }
        public GameObject FloorPrefab { get => _floorPrefab; }
        public GameObject BombPrefab { get => _bombPrefab; }

        [Serializable]
        public class TilePrefab
        {
            public TileType TileType;
            public GameObject PrefabObject;
            public Vector3 OffsetSpawn;
        }
    }
}
