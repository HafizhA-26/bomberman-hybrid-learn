using BombermanRL.Character;
using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL.Grid
{
    /// <summary>
    /// Procedurally builds the arena at match start from a <see cref="LevelTilemapData"/>
    /// asset: spawns floor tiles, walls, crates, the player, and enemies at their grid
    /// positions, converting grid coordinates to world space along the way. Consumed once
    /// by <see cref="MatchDirector.StartMatch"/>; the resulting object grids are then handed
    /// to <see cref="GridStateManager"/> to track logical state.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Level Generator Parameter")]
        [SerializeField] private LevelTilemapData _levelData;
        [SerializeField] private Transform _envParent;
        [SerializeField] private Transform _floorsParent;
        [SerializeField] private Transform _objectsTileParent;
        [Space(10)]
        [SerializeField] private TilePrefabsData _tilePrefabsData;
        // Fallback game mode (player/enemy prefabs + spawn offsets) used when no override is supplied via GameInstance.
        [SerializeField] private GameModeConfig _defaultGameMode;

        // Floor materials indexed as [0]=neutral, [1]=agent success, [2]=agent neutral, [3]=agent failed; used by MatchDirector to color-code episode outcomes.
        private List<Material> _floorMaterials;
        private Dictionary<PlayMode, GameObject> _enemyPool;
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
                    _tilePrefabsData.FloorPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial,
                    _tilePrefabsData.AgentSuccessFloorMat,
                    _tilePrefabsData.AgentNeutralFloorMat,
                    _tilePrefabsData.AgentFailedFloorMat
                };

        }

        // Editor-only preview: draws the floor grid and placed walls/crates/spawns as gizmos in the Scene view for level design, without needing Play mode.
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

        /// <summary>
        /// Swaps the level layout asset used by subsequent <see cref="CreateFloor"/>/<see cref="LoadLevelTile"/> calls (e.g. for set custom arena or procedural level rotation between episodes).
        /// </summary>
        /// <param name="data">Custom level arena data</param>
        public void SetLevelData(LevelTilemapData data) => _levelData = data;

        /// <summary>
        /// Instantiates one floor tile per grid cell (width x height), spaced by
        /// <see cref="TileSize"/> and anchored at this transform's position. Yields every
        /// 20 tiles via <see cref="Awaitable.NextFrameAsync"/> to spread the instantiation
        /// cost across frames and avoid a load-time hitch on larger grids.
        /// </summary>
        /// <returns>A [width, height] grid of the instantiated floor GameObjects.</returns>
        public async Awaitable<GameObject[,]> CreateFloor()
        {
            if (_floorsParent == null || _tilePrefabsData == null)
                return null;

            int count = 0;
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

                    count++;

                    if(count % 20 == 0) await Awaitable.NextFrameAsync();
                }
            }

            return floors;
        }

        /// <summary>
        /// Reads <see cref="_levelData"/>'s flat tile list and instantiates the gameplay
        /// object for each non-empty tile (wall, crate, player, enemy) at its converted
        /// world position. Player/enemy prefabs come from <see cref="_defaultGameMode"/>,
        /// unless <see cref="GameInstance.OverrideGameConfig"/> is set (playable scenes
        /// override the default training config). Yields every 3 tiles to spread
        /// instantiation cost across frames.
        /// </summary>
        /// <returns>
        /// A tuple of the [width, height] instantiated GameObject grid (null where empty)
        /// and the matching logical <see cref="TileState"/> grid, both consumed by
        /// <see cref="GridStateManager.Initialize"/>.
        /// </returns>
        public async Awaitable<(GameObject[,], TileState[,])> LoadLevelTile()
        {
            // Check for override game mode from game instance [Override if in playable scene]
            if (GameInstance.Instance.OverrideGameConfig != null)
                _defaultGameMode = GameInstance.Instance.OverrideGameConfig;

            GameObject[,] gridObjects = new GameObject[_levelData.GridWidth, _levelData.GridHeight];
            TileState[,] gridState = new TileState[_levelData.GridWidth, _levelData.GridHeight];
            Dictionary<TileType, TilePrefabsData.TilePrefab> tilePrefabDict = _tilePrefabsData.TilePrefabDict;
            int enemyCount = 0;
            int count = 0;

            for (int i = 0; i < _levelData.LevelTiles.Count; i++)
            {
                int row = i / _levelData.GridWidth;
                int col = i % _levelData.GridHeight;

                TileType type = _levelData.LevelTiles[i];
                GridPos tileGridPos = new GridPos(row, col);
                gridState[row, col] = new(type);

                if (type == TileType.Empty) continue;

                GameObject tile = null;
                Vector3 newPos = GridToWorld(tileGridPos);

                // Set offset and instantiate gameplay objects
                switch (type)
                {
                    case TileType.Wall:
                    case TileType.Crate:
                        tile = Instantiate(tilePrefabDict[type].PrefabObject, _objectsTileParent, true);
                        newPos += tilePrefabDict[type].OffsetSpawn;
                        tile.name = $"{type.ToString()}[{row}-{col}]";
                        break;
                    case TileType.PlayerSpawn:
                        tile = Instantiate(_defaultGameMode.PlayerPrefab, _objectsTileParent, true);
                        tile.name = "Player";
                        newPos += _defaultGameMode.PlayerOffset;
                        tile.GetComponent<BombermanEntity>().OffsetMovement = _defaultGameMode.PlayerOffset;
                        break;
                    case TileType.EnemySpawn:
                        tile = Instantiate(_defaultGameMode.EnemyPrefab, _objectsTileParent, true);
                        tile.name = $"Enemy-{enemyCount}";
                        newPos += _defaultGameMode.EnemyOffset;
                        tile.GetComponent<BombermanEntity>().OffsetMovement = _defaultGameMode.EnemyOffset;
                        enemyCount++;
                        break;
                }

                // Set final objects and position
                gridObjects[row, col] = tile;
                tile.transform.position = newPos;

                count++;

                if(count % 3 == 0) await Awaitable.NextFrameAsync();
            }

            return (gridObjects, gridState);
        }
        // Converts a grid cell to the world position for gameplay objects (raised to TileSize.y * 1.5 so props/characters sit above the floor mesh).
        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.col * TileSize.x + ParentPos.x, TileSize.y * 1.5f + ParentPos.y, tilePos.row * TileSize.z * -1 + ParentPos.z);

        // Same conversion as GridToWorld but at floor height (TileSize.y * 0.5), used for placing/previewing floor tiles.
        private Vector3 FloorGridToWorld(GridPos tilePos) => new Vector3(tilePos.col * TileSize.x + ParentPos.x, TileSize.y * 0.5f + ParentPos.y, tilePos.row * TileSize.z * -1 + ParentPos.z);
    }
}