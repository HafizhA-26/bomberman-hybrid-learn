using BombermanRL.Character;
using BombermanRL.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Integrations.Match3;
using UnityEngine;

namespace BombermanRL.Grid
{
    /// <summary>
    /// Single source of truth for the arena's logical grid state.
    /// Tracks tile types/substates, entity grid positions, active bombs, and destructible
    /// props, and exposes the queries/mutations that <see cref="MatchDirector"/> and AI
    /// agents need for movement validation, bomb placement, explosion propagation,
    /// respawn selection, and episode resets. Coordinates are purely logical (row/col);
    /// world-space conversion happens here via <see cref="GridToWorld"/>.
    /// </summary>
    public class GridStateManager : MonoBehaviour,  IGameplayStateProvider
    {
        [Header("Data Paramater")]
        // Minimum grid distance a respawn point must keep from other newly-placed entities.
        [SerializeField] private int _validRespawnDistance = 3;

        // Grid position -> bomb currently ticking on that tile.
        private readonly Dictionary<GridPos, BombHandler> _placedBomb = new Dictionary<GridPos, BombHandler>();
        // Live/current grid position per tracked entity (player + enemies).
        private readonly Dictionary<BombermanEntity, GridPos> _entityPositions = new();
        // Spawn-time grid position per entity, used to restore/report origin on reset.
        private readonly Dictionary<BombermanEntity, GridPos> _entityOriginalPos = new();
        // Grid position -> destructible prop (crate) occupying that tile.
        private readonly Dictionary<GridPos, IDestroyableProps> _destroyableProps = new Dictionary<GridPos, IDestroyableProps>();
        // All empty tiles eligible as a respawn point.
        private readonly List<GridPos> _validRespawnPos = new List<GridPos>();
        // Pristine copy of the grid as first loaded, used to restore state on ResetGridState.
        private TileState[,] _defaultGrid;
        private GameObject[,] _tileObjects;
        private TileState[,] _grid;
        private Vector3 _parentPos;
        private Vector3 _tileSize;

        /// <summary>
        /// Caches the world-space anchor and tile size needed to convert grid coordinates
        /// to world positions. Must be called before any grid&lt;-&gt;world conversion
        /// (typically right after <see cref="LevelBuilder"/> builds the level, before
        /// <see cref="Initialize"/>).
        /// <param name="parentPos">Env Parent Position</param>
        /// <param name="tileSize">Base floor tile size</param>
        /// </summary>
        public void PreSetup(Vector3 parentPos, Vector3 tileSize)
        {
            _parentPos = parentPos;
            _tileSize = tileSize;
        }

        /// <summary>
        /// Builds the internal lookup tables (entity positions, destructible props,
        /// valid respawn tiles) from the level's instantiated objects and tile states,
        /// and snapshots the pristine grid into <see cref="_defaultGrid"/> so
        /// <see cref="ResetGridState"/> can restore it later. Spawn tiles are converted
        /// to <see cref="TileType.Empty"/> with an <see cref="TileSubState.OnCharacter"/>
        /// substate once their occupant is registered.
        /// <param name="tiles">Every instantiated tiles from <see cref="LevelBuilder"/></param>
        /// <param name="tileStates">Every tile states from <see cref="LevelBuilder"/></param>
        /// </summary>
        public void Initialize(GameObject[,] tiles, TileState[,] tileStates)
        {
            if(tiles.GetLength(0) != tileStates.GetLength(0) || tiles.GetLength(1) != tileStates.GetLength(1))
            {
                Debug.LogWarning("Invalid tile objects and tile states to initialize");
                return;
            }

            // Safe default grid state reference for reset purpose
            _defaultGrid = new TileState[tiles.GetLength(0), tiles.GetLength(1)];
            _tileObjects = tiles;
            _grid = tileStates;

            // Register specific tile states
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    GameObject tile = tiles[i, j];
                    TileState state = tileStates[i, j];
                    GridPos pos = new(i, j);

                    switch(state.Type)
                    {
                        case TileType.Empty:
                            _validRespawnPos.Add(pos);
                            break;
                        case TileType.Crate:
                            _destroyableProps[pos] = tile.GetComponent<CrateHandler>();
                            break;
                        case TileType.PlayerSpawn:
                        case TileType.EnemySpawn:
                            _entityPositions[tile.GetComponent<BombermanEntity>()] = pos;
                            _entityOriginalPos[tile.GetComponent<BombermanEntity>()] = pos;
                            tileStates[i, j] = new TileState(TileType.Empty, TileSubState.OnCharacter);
                            break;
                    }

                    _defaultGrid[i, j] = new(state.Type, state.SubState);
                }
            }
        }

        /// <summary>
        /// Returns the entity's spawn-time grid and world position, or (null, null) if it isn't tracked.
        /// <param name="entity">Target character to search</param>
        /// </summary>
        public (GridPos?, Vector3?) GetCharacterOriginPos(BombermanEntity entity) 
            => _entityOriginalPos.ContainsKey(entity) ? (_entityOriginalPos[entity], GridToWorld(_entityOriginalPos[entity])) : (null, null);

        /// <summary>
        /// Returns the entity's current grid position, or null if it isn't tracked.
        /// </summary>
        /// <param name="entity">Target character to search</param>
        /// <returns>Character <see cref="GridPos"/> or null if entity not registered</returns>
        public GridPos? GetCharacterGridPos(BombermanEntity entity) => _entityPositions.ContainsKey(entity) ? _entityPositions[entity] : null;

        /// <summary>
        /// Returns the entity's current world position (grid position converted via <see cref="GridToWorld"/>).
        /// </summary>
        /// <param name="entity">Target character to search</param>
        /// <returns>Character world pos</returns>
        public Vector3 GetCharacterWorldPos(BombermanEntity entity) => GridToWorld(_entityPositions[entity]);

        /// <summary>
        /// Linear scan for whichever tracked entity currently occupies <paramref name="tilePos"/>; returns null if the tile is unoccupied.
        /// </summary>
        /// <param name="tilePos">Target tile to search an entity</param>
        /// <returns>Entity at <paramref name="tilePos"/></returns>
        public BombermanEntity GetCharacterAt(GridPos tilePos)
        {
            foreach (KeyValuePair<BombermanEntity, GridPos> charaPos in _entityPositions)
            {
                if (tilePos.Equals(charaPos.Value)) return charaPos.Key;
            }
            return null;
        }

        /// <summary>
        /// Debug helper: logs every tracked entity's current grid position.
        /// </summary>
        public void LogAllEntityPos()
        {
            foreach (KeyValuePair<BombermanEntity, GridPos> charaPos in _entityPositions)
            {
                Debug.Log($"{charaPos.Key.name} at {charaPos.Value.ToString()}");
            }
        }

        /// <summary>
        /// Picks a fully random valid (empty) tile with no avoidance check. Prefer the overload below during active matches.
        /// <paramref name="worldPos">Spawn world position</paramref>
        /// </summary>
        public GridPos GetRandomValidRespawn(out Vector3 worldPos)
        {
            GridPos pos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)]; ;
            worldPos = GridToWorld(pos);
            return pos;
        }
        /// <summary>
        /// Picks a random valid tile that is at least <see cref="_validRespawnDistance"/> tiles
        /// away from every position in <paramref name="avoidPositions"/> (used so respawning
        /// entities don't land next to each other or an active bomb site). Falls back to any
        /// valid tile if no candidate satisfies the distance constraint and nothing needs
        /// avoiding yet; returns null if no valid tile exists at all.
        /// <param name="avoidPositions">Positions to avoid spwan nearly at</param>
        /// <param name="worldPos">Spawn world position</param>
        /// </summary>
        public GridPos? GetRandomValidRespawn(List<GridPos> avoidPositions, out Vector3 worldPos)
        {
            List<GridPos> actualValidPos = new();
            GridPos? pos = null;
            worldPos = Vector3.zero;

            // Check actual valid position list based on position to avoid
            foreach (GridPos validPos in _validRespawnPos)
            {
                bool isActualValid = true;
                foreach (GridPos avoidPos in avoidPositions)
                {
                    if(validPos.Distance(avoidPos) <= _validRespawnDistance)
                    {
                        isActualValid = false;
                        break;
                    }
                }
                if(isActualValid) actualValidPos.Add(validPos);
            }

            // Get new random valid pos
            if (actualValidPos.Count > 0)
            {
                pos = actualValidPos[UnityEngine.Random.Range(0, actualValidPos.Count)];
                worldPos = GridToWorld(pos.Value);
            }else if(avoidPositions.Count == 0)
            {
                pos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)];
                worldPos = GridToWorld(pos.Value);
            }

            return pos;
        }

        /// <summary>
        /// Returns the destructible prop at <paramref name="tilePos"/>, or null if the tile has none.
        /// </summary>
        /// <param name="tilePos">Target tile position to search a prop</param>
        /// <returns>Prop at <paramref name="tilePos"/></returns>
        public IDestroyableProps GetPropAt(GridPos tilePos) => _destroyableProps.ContainsKey(tilePos) ? _destroyableProps[tilePos] : null;

        /// <summary>
        /// Returns every tracked destructible prop paired with the world position of its origin tile, for restoring props on episode reset.
        /// </summary>
        /// <returns>List of prop and its orginal spawn position</returns>
        public List<(IDestroyableProps prop, Vector3 originPos)> GetPropsOriginPos()
        {
            List<(IDestroyableProps prop, Vector3 originPos)> originPos = new();
            foreach (KeyValuePair<GridPos, IDestroyableProps> item in _destroyableProps)
            {
                originPos.Add((item.Value, GridToWorld(item.Key)));
            }
            return originPos;
        }

        /// <summary>
        /// Restores every destroyed crate's grid tile type back to its original prop type (logical state only; does not respawn/re-enable the crate GameObject — see <see cref="ResetProp"/> callers in MatchDirector).
        /// </summary>
        public void ResetGridPropStates()
        {
            foreach (KeyValuePair<GridPos, IDestroyableProps> item in _destroyableProps)
            {
                _grid[item.Key.row, item.Key.col].RemoveSubstate(TileSubState.OnDestroyedCrate);
                _grid[item.Key.row, item.Key.col].Type = item.Value.PropType;
            }
        }

        /// <summary>
        /// Directly adjust grid state after entity teleported
        /// </summary>
        /// <param name="entity">Target bomberman entity</param>
        /// <param name="targetPos">Teleported grud position</param>
        public void OnEntityTeleported(BombermanEntity entity, GridPos targetPos)
        {
            Debug.Log($"{entity.name} is teleported {_entityPositions[entity]} to {targetPos}");
            GridPos originalPos = _entityPositions[entity];
            _grid[targetPos.row, targetPos.col].AddSubstate(TileSubState.OnCharacter);
            _grid[originalPos.row, originalPos.col].RemoveSubstate(TileSubState.OnCharacter);
            _entityPositions[entity] = targetPos;
        }

        /// <summary>
        /// Reserves <paramref name="targetPos"/> for <paramref name="entity"/> immediately
        /// (marks it <see cref="TileSubState.OnCharacter"/>) so a second entity can't be
        /// routed onto the same tile while this move is still animating. Returns a callback
        /// that must be invoked once the move visually completes to release the origin tile
        /// and commit the entity's new logical position.
        /// </summary>
        /// <param name="entity">The moving entity</param>
        /// <param name="targetPos">Move goal grid position</param>
        /// <returns>Action on character reached different tile</returns>
        public Action OnEntityMove(BombermanEntity entity, GridPos targetPos)
        {
            GridPos originalPos = _entityPositions[entity];
            _grid[targetPos.row, targetPos.col].AddSubstate(TileSubState.OnCharacter);
            return () =>
            {
                _grid[originalPos.row, originalPos.col].RemoveSubstate(TileSubState.OnCharacter);
                _entityPositions[entity] = targetPos;
            };
        }

        /// <summary>
        /// Converts an input direction into the target grid cell and world position one
        /// step away from the entity's current tile. This only computes the destination —
        /// it does not check whether the move is legal (see <see cref="CanMove"/>).
        /// </summary>
        /// <param name="entity">The moving entity</param>
        /// <param name="moveDirection">Direction to move. Only accept value 0, -1 or 1 for the x and y axis</param>
        /// <returns>Grid tile position to move and its world position</returns>
        public (GridPos, Vector3) GetNextMovePosition(BombermanEntity entity, Vector2 moveDirection)
        {
            GridPos originalPos = _entityPositions[entity];
            moveDirection.x = Math.Sign(moveDirection.x);
            moveDirection.y = Math.Sign(moveDirection.y * -1);

            GridPos targetGridPos = new ((int)(originalPos.row + moveDirection.y), (int)(originalPos.col + moveDirection.x));
            Vector3 targetWorldPos = GridToWorld(targetGridPos);
            targetWorldPos += entity.OffsetMovement;
            //Debug.Log($"Move Entity {fromPos} to {targetGridPos} | CanMove {canMove}");

            return (targetGridPos, targetWorldPos);
        }

        /// <summary>
        /// Check if character can move to target grid pos
        /// </summary>
        /// <param name="entity">Entity requesting the move; its current grid position is the origin.</param>
        /// <param name="direction">X for move horizontal (>0 right, <0 left), Y for vertical (>0 top, <0 bottom)</param>0 bottom)</param>
        /// <return>Is target tile to move available</return>
        public bool CanMove(BombermanEntity entity, Vector2 direction)
        {
            GridPos originalPos = _entityPositions[entity];
            TileState nextTile = new TileState(TileType.Empty);

            // Get next tile based on direction move
            if (direction.y > 0) // Move Up
            {
                if (originalPos.row == 0) return false;
                nextTile = _grid[originalPos.row - 1, originalPos.col];
            }
            else if (direction.y < 0) // Move down
            {
                if (originalPos.row == _grid.GetLength(0) - 1) return false;
                nextTile = _grid[originalPos.row + 1, originalPos.col];
            }
            else if (direction.x > 0) // Move right
            {
                if (originalPos.col == _grid.GetLength(1) - 1) return false;
                nextTile = _grid[originalPos.row, originalPos.col + 1];
            }
            else if (direction.x < 0) // Move left
            {
                if (originalPos.col == 0) return false;
                nextTile = _grid[originalPos.row, originalPos.col - 1];
            }

            // Check blocking condition 
            bool isMovable = nextTile.Type == TileType.Empty &&
                !nextTile.HasSubstate(TileSubState.OnCharacter) &&
                !nextTile.HasSubstate(TileSubState.OnBomb);

            return isMovable;
        }

        /// <summary>
        /// Builds a localized snapshot of the arena around <paramref name="entity"/> —
        /// tile states within <paramref name="NearbyRadius"/> tiles plus normalized bomb
        /// timers for any tile with an active bomb — used as the observation fed to the
        /// rule-based/ML agent's decision logic.
        /// </summary>
        /// <param name="entity">The entity to be checked its surround condition</param>
        /// <param name="NearbyRadius">Radius to get its condition</param>
        /// <returns>Surround <see cref="GameplayState"/></returns>
        public GameplayState GetNearbyState(BombermanEntity entity, int NearbyRadius)
        {
            Dictionary<GridPos, TileState> nearbyCondition = new Dictionary<GridPos, TileState>();
            Dictionary<GridPos, float> bombTimerNorm = new Dictionary<GridPos, float>();

            // Get nearby tiles condition based on radius
            GridPos entityPos = _entityPositions[entity];
            int startRow = Mathf.Clamp(entityPos.row - NearbyRadius, 0, _grid.GetLength(0) - 1);
            int endRow = Mathf.Clamp(entityPos.row + NearbyRadius, 0, _grid.GetLength(0) - 1);
            int startCol = Mathf.Clamp(entityPos.col - NearbyRadius, 0, _grid.GetLength(1) - 1);
            int endCol = Mathf.Clamp(entityPos.col + NearbyRadius, 0, _grid.GetLength(1) - 1);

            // Register all nearby state condition including existing bombs
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

            // Get current character's enemy position
            GridPos enemyPos = _entityPositions.FirstOrDefault(item => item.Key.CharacterType != entity.CharacterType).Value;

            return new
                (
                    _entityPositions[entity],
                    nearbyCondition,
                    enemyPos,
                    bombTimerNorm,
                    NearbyRadius
                );
        }

        /// <summary>
        /// Marks <paramref name="bombPos"/> as holding <paramref name="bomb"/> so its timer/effects can be looked up later (e.g. by <see cref="GetNearbyState"/> or <see cref="ResetGridBombStates"/>).
        /// </summary>
        /// <param name="bomb">Bomb to register</param>
        /// <param name="bombPos">Bomb grid pos</param>
        public void RegisterActiveBomb(BombHandler bomb, GridPos bombPos) => _placedBomb[bombPos] = bomb;

        /// <summary>
        /// Check if an entity can place bomb on the tile they stand on it.
        /// </summary>
        /// <param name="entity">Target entity that will place the bomb</param>
        /// <returns>True if can place the bomb</returns>
        public bool CanPlaceBomb(BombermanEntity entity)
        {
            GridPos tilePos = _entityPositions[entity];
            return !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnBomb) &&
            !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnExplosion);
        }

        /// <summary>
        /// Can explosition spawn on the <paramref name="tilePos"/>
        /// </summary>
        /// <param name="tilePos">Target tile grid position to check</param>
        /// <returns>True if can spawn explosion</returns>
        private bool CanExplosionSpawn(GridPos tilePos) =>
           tilePos.row < _grid.GetLength(0) &&
           tilePos.row >= 0 &&
           tilePos.col < _grid.GetLength(1) &&
           tilePos.col >= 0 &&
           _grid[tilePos.row, tilePos.col].Type != TileType.Wall;

        /// <summary>
        /// A crate stops the blast from propagating further in that direction (see <see cref="OnPlaceBomb(BombermanEntity)"/>)
        /// </summary>
        /// <param name="tilePos">Target tile grid position to check</param>
        /// <returns></returns>
        private bool IsExplosionBlocked(GridPos tilePos) => 
            _grid[tilePos.row, tilePos.col].Type == TileType.Crate;

        /// <summary>
        /// Clears the <see cref="TileSubState.OnBomb"/> flag from every tile with a currently registered bomb and empties the bomb registry. 
        /// Used when force-clearing bombs on episode reset.
        /// </summary>
        public void ResetGridBombStates()
        {
            foreach (KeyValuePair<GridPos, BombHandler> item in _placedBomb)
            {
                _grid[item.Key.row, item.Key.col].RemoveSubstate(TileSubState.OnBomb);
            }
            _placedBomb.Clear();
        }

        /// <summary>
        /// Computes the tiles a bomb placed by <paramref name="entity"/> will hit: the origin
        /// tile plus, in each of the 4 cardinal directions, up to <see cref="BombermanEntity.BombExplodeRadius"/>
        /// tiles — stopping early if the direction runs off the grid/hits a wall, and stopping
        /// (but still including) the first crate it reaches, since crates block further
        /// propagation. Also flags the origin tile as <see cref="TileSubState.OnBomb"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public (List<GridPos>, List<Vector3>) OnPlaceBomb(BombermanEntity entity)
        {
            GridPos tilePos = _entityPositions[entity];
            Vector3 entityWorldPos = GridToWorld(tilePos);
            List<Vector3> explosionWorldPos = new List<Vector3>() { entityWorldPos };
            List<GridPos> explosionGridPos = new List<GridPos>() { tilePos };

            // Propagate explosion tile with + shape based on bomb's explosion radius
            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= entity.BombExplodeRadius; j++)
                {
                    GridPos explosionPos = new();
                    // Get next grid pos to check based on direction to check (0 : top. 1 : right, 2 : bottom, 3 : left)
                    if (i == 0) explosionPos = new GridPos(tilePos.row + j, tilePos.col);
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
            _grid[tilePos.row, tilePos.col].AddSubstate(TileSubState.OnBomb);

            return (explosionGridPos, explosionWorldPos);
        }

        /// <summary>
        /// Flips every blast tile from "bomb pending" to "exploding" substate (called the instant a bomb detonates, before victims/props are resolved).
        /// </summary>
        /// <param name="explosionPos">Bomb explosion positions</param>
        public void OnBombExplode(List<GridPos> explosionPos)
        {
            foreach (GridPos item in explosionPos)
            {
                if (_grid[item.row, item.col].HasSubstate(TileSubState.OnBomb)) _grid[item.row, item.col].RemoveSubstate(TileSubState.OnBomb);
                _grid[item.row, item.col].AddSubstate(TileSubState.OnExplosion);
            }
        }

        /// <summary>
        /// Converts a crate tile to an empty, walkable tile (flagged as a destroyed crate) once its prop has been destroyed.
        /// </summary>
        /// <param name="propPos">Prop tile pos</param>
        public void OnPropDestroyedAt(GridPos propPos)
        {
            if (_grid[propPos.row, propPos.col].Type == TileType.Crate)
            {
                _grid[propPos.row, propPos.col].Type = TileType.Empty;
                _grid[propPos.row, propPos.col].AddSubstate(TileSubState.OnDestroyedCrate);
            }
        }

        /// <summary>
        /// Clears the <see cref="TileSubState.OnExplosion"/> hazard flag from the blast tiles and removes the now-spent bomb from the active registry (keyed by the blast's origin tile).
        /// </summary>
        /// <param name="explosionPos">Bomb explosion positions</param>
        public void OnExplosionFinish(List<GridPos> explosionPos)
        {
            foreach (GridPos item in explosionPos)
                _grid[item.row, item.col].RemoveSubstate(TileSubState.OnExplosion);

            if (_placedBomb.ContainsKey(explosionPos[0])) _placedBomb.Remove(explosionPos[0]);
            else Debug.LogWarning("Can't find spawned bomb at " + explosionPos[0]);
        }

        // Row -> world Z (negated, so row increases "into" the screen), column -> world X.
        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.col * _tileSize.x + _parentPos.x, _tileSize.y * 1.5f + _parentPos.y, tilePos.row * _tileSize.z * -1 + _parentPos.z);

        /// <summary>
        /// Deep-resets the grid back to its pristine <see cref="_defaultGrid"/> snapshot,
        /// clears the bomb registry, and converts any leftover spawn-type tiles to
        /// <see cref="TileType.Empty"/> (since entities themselves, not the tile, own
        /// occupancy after <see cref="Initialize"/> has run once).
        /// </summary>
        public void ResetGridState()
        {
            _placedBomb.Clear();

            // Deep reset grid states to default states
            for (int i = 0; i < _defaultGrid.GetLength(0); i++)
            {
                for (int j = 0; j < _defaultGrid.GetLength(1); j++)
                {
                    TileState state = _defaultGrid[i, j];
                    _grid[i,j] = new(state.Type, state.SubState);
                    if (_grid[i,j].Type == TileType.PlayerSpawn || _grid[i,j].Type == TileType.EnemySpawn)
                        _grid[i,j].Type = TileType.Empty;
                }
            }
        }
    }
}