using BombermanRL.Character;
using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL.Grid
{
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Level Generator Parameter")]
        [SerializeField] private LevelTilemapData _levelData;
        [SerializeField] private Transform _envParent;
        [SerializeField] private Transform _floorsParent;
        [SerializeField] private Transform _objectsTileParent;
        [Space(10)]
        [SerializeField] private TilePrefabsData _tilePrefabsData;

        private List<Material> _floorMaterials;
        public List<Material> FloorMaterials { get => _floorMaterials; }
        public GameObject BombPrefab { get => _tilePrefabsData.BombPrefab; }
        public GameObject ExplosionPrefab { get => _tilePrefabsData.ExplosionPrefab; }
        public Transform ObjectsTileParent { get => _objectsTileParent; }
        public Vector3 TileSize { get => _tilePrefabsData.FloorPrefab.transform.lossyScale; }
        public Vector3 ParentPos { get => _envParent.position; }

        private void Awake()
        {
            _floorMaterials = new List<Material>()
                {
                    _tilePrefabsData.FloorPrefab.GetComponent<MeshRenderer>().sharedMaterial,
                    _tilePrefabsData.AgentSuccessFloorMat,
                    _tilePrefabsData.AgentNeutralFloorMat,
                    _tilePrefabsData.AgentFailedFloorMat
                };

            Debug.Log("TileSize: "+TileSize);
        }

        private void OnDrawGizmosSelected()
        {
            if(_levelData == null 
                || _tilePrefabsData == null 
                || _floorsParent == null) return;

            // Start Draw Floors Preview
            Gizmos.color = new Color32(28, 44, 80, 255);
            //Gizmos.matrix = _floorsParent.localToWorldMatrix;
            for (int i = 0; i < _levelData.GridWidth; i++)
            {
                for (int j = 0; j < _levelData.GridHeight; j++)
                {
                    Gizmos.DrawWireCube(FloorGridToWorld(new GridPos(i, j)), TileSize);
                }
            }

            // Start Draw Tiles
            for (int i = 0; i < _levelData.LevelTiles.Count; i++)
            {
                int row = i / _levelData.GridWidth;
                int col = i % _levelData.GridHeight;

                TileType type = _levelData.LevelTiles[i];
                GridPos tileGridPos = new GridPos(row, col);

                switch (type)
                {
                    case TileType.Wall:
                        Gizmos.color = Color.white;
                        Gizmos.DrawCube(GridToWorld(tileGridPos), TileSize);
                        break;
                    case TileType.Crate:
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawCube(GridToWorld(tileGridPos), TileSize);
                        break;
                    case TileType.PlayerSpawn:
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(GridToWorld(tileGridPos), TileSize.x);
                        break;
                    case TileType.EnemySpawn:
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(GridToWorld(tileGridPos), TileSize.x);
                        break;
                }


            }
        }

        public GameObject[,] CreateFloor()
        {
            if (_floorsParent == null || _tilePrefabsData == null)
                return null;

            GameObject[,] floors = new GameObject[_levelData.GridWidth, _levelData.GridHeight];

            // Populate all floor gameobjects
            for (int i = 0; i < floors.GetLength(0); i++)
            {
                for (int j = 0; j < floors.GetLength(1); j++)
                {
                    GameObject floor = Instantiate(_tilePrefabsData.FloorPrefab, _floorsParent, true);
                    Vector3 newPos = new Vector3(TileSize.x * j + transform.position.x, TileSize.y * 0.5f, TileSize.z * i * -1 + transform.position.z);
                    floor.transform.position = newPos;
                    floor.name = $"Floor[{i}-{j}]";
                    floors[i, j] = floor;
                }
            }

            return floors;
        }

        public (GameObject[,], TileState[,]) LoadLevelTile()
        {
            GameObject[,] gridObjects = new GameObject[_levelData.GridWidth, _levelData.GridHeight];
            TileState[,] gridState = new TileState[_levelData.GridWidth, _levelData.GridHeight];
            Dictionary<TileType, TilePrefabsData.TilePrefab> tilePrefabDict = _tilePrefabsData.TilePrefabDict;
            int enemyCount = 0;

            for (int i = 0; i < _levelData.LevelTiles.Count; i++)
            {
                int row = i / _levelData.GridWidth;
                int col = i % _levelData.GridHeight;

                TileType type = _levelData.LevelTiles[i];
                GridPos tileGridPos = new GridPos(row, col);
                gridState[row, col] = new(type);

                if (type == TileType.Empty) continue;

                // Spawn tile based on prefab
                GameObject tile = Instantiate(tilePrefabDict[type].PrefabObject, _objectsTileParent, true);
                gridObjects[row, col] = tile;

                // Re-position tile based offset spawn
                Vector3 newPos = GridToWorld(tileGridPos);
                newPos += tilePrefabDict[type].OffsetSpawn;
                tile.transform.position = newPos;

                // Change game object name after spawn
                switch (type)
                {
                    case TileType.Wall:
                    case TileType.Crate:
                        tile.name = $"{type.ToString()}[{row}-{col}]";
                        break;
                    case TileType.PlayerSpawn:
                        tile.name = "Player";
                        tile.GetComponent<BombermanEntity>().OffsetMovement = tilePrefabDict[type].OffsetSpawn;
                        break;
                    case TileType.EnemySpawn:
                        tile.name = $"Enemy-{enemyCount}";
                        tile.GetComponent<BombermanEntity>().OffsetMovement = tilePrefabDict[type].OffsetSpawn;
                        enemyCount++;
                        break;
                }
            }

            return (gridObjects, gridState);
        }
        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.col * TileSize.x + ParentPos.x, TileSize.y * 1.5f + ParentPos.y, tilePos.row * TileSize.z * -1 + ParentPos.z);
        private Vector3 FloorGridToWorld(GridPos tilePos) => new Vector3(tilePos.col * TileSize.x + ParentPos.x, TileSize.y * 0.5f + ParentPos.y, tilePos.row * TileSize.z * -1 + ParentPos.z);
    }
}