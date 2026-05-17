using BombermanRL.Character;
using BombermanRL.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Grid
{
    public class GridStateManager : MonoBehaviour,  IGameplayStateProvider
    {
        [Header("Data Paramater")]
        [SerializeField] private int _validRespawnDistance = 3;

        private readonly Dictionary<GridPos, BombHandler> _placedBomb = new Dictionary<GridPos, BombHandler>();
        private readonly Dictionary<BombermanEntity, GridPos> _entityPositions = new();
        private readonly Dictionary<GridPos, IDestroyableProps> _destroyableProps = new Dictionary<GridPos, IDestroyableProps>();
        private TileState[,] _defaultGrid;
        private List<GridPos> _validRespawnPos = new List<GridPos>();
        private GameObject[,] _tileObjects;
        private TileState[,] _grid;
        private Vector3 _parentPos;
        private Vector3 _tileSize;

        public void PreSetup(Vector3 parentPos, Vector3 tileSize)
        {
            _parentPos = parentPos;
            _tileSize = tileSize;
        }

        public void Initialize(GameObject[,] tiles, TileState[,] tileStates)
        {
            if(tiles.GetLength(0) != tileStates.GetLength(0) || tiles.GetLength(1) != tileStates.GetLength(1))
            {
                Debug.LogWarning("Invalid tile objects and tile states to initialize");
                return;
            }

            _defaultGrid = new TileState[tiles.GetLength(0), tiles.GetLength(1)];
            _tileObjects = tiles;
            _grid = tileStates;

            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    GameObject tile = tiles[i, j];
                    TileState state = tileStates[i, j];
                    GridPos pos = new(i, j);

                    if (tile == null) continue;

                    switch(state.Type)
                    {
                        case TileType.Crate:
                            _destroyableProps[pos] = tile.GetComponent<CrateHandler>();
                            break;
                        case TileType.PlayerSpawn:
                        case TileType.EnemySpawn:
                            _entityPositions[tile.GetComponent<BombermanEntity>()] = pos;
                            tileStates[i, j] = new TileState(TileType.Empty, TileSubState.OnCharacter);
                            break;
                    }

                    _defaultGrid[i, j] = new(state.Type, state.SubState);
                    if (state.Type == TileType.Empty) _validRespawnPos.Add(pos);
                }
            }
        }
        public GridPos? GetCharacterGridPos(BombermanEntity entity) => _entityPositions.ContainsKey(entity) ? _entityPositions[entity] : null;
        public Vector3 GetCharacterWoldPos(BombermanEntity entity) => GridToWorld(_entityPositions[entity]);
        public BombermanEntity GetCharacterAt(GridPos tilePos)
        {
            foreach (KeyValuePair<BombermanEntity, GridPos> charaPos in _entityPositions)
            {
                if (tilePos.Equals(charaPos.Value)) return charaPos.Key;
            }
            return null;
        }
        public GridPos GetRandomValidRespawn(out Vector3 worldPos)
        {
            GridPos pos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)]; ;
            worldPos = GridToWorld(pos);
            return pos;
        }
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
                pos = _validRespawnPos[UnityEngine.Random.Range(0, _validRespawnPos.Count)]; ;
                worldPos = GridToWorld(pos.Value);
            }

            return pos;
        }

        public IDestroyableProps GetPropAt(GridPos tilePos) => _destroyableProps.ContainsKey(tilePos) ? _destroyableProps[tilePos] : null;
        public List<(IDestroyableProps prop, Vector3 originPos)> GetPropsOriginPos()
        {
            List<(IDestroyableProps prop, Vector3 originPos)> originPos = new();
            foreach (KeyValuePair<GridPos, IDestroyableProps> item in _destroyableProps)
            {
                originPos.Add((item.Value, GridToWorld(item.Key)));
            }
            return originPos;
        }

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
        /// <param name="fromPos">Origin position, X for grid row, Y for grid column</param>
        /// <param name="direction">X for move horizontal (>0 right, <0 left), Y for vertical (>0 top, <0 bottom)</param>
        /// <return>Is target tile to move available</return>
        public bool CanMove(BombermanEntity entity, Vector2 direction)
        {
            GridPos originalPos = _entityPositions[entity];
            TileState nextTile = new TileState(TileType.Empty);
            if (direction.y > 0)
            {
                if (originalPos.row == 0) return false;
                nextTile = _grid[originalPos.row - 1, originalPos.col];
            }
            else if (direction.y < 0)
            {
                if (originalPos.row == _grid.GetLength(0) - 1) return false;
                nextTile = _grid[originalPos.row + 1, originalPos.col];
            }
            else if (direction.x > 0)
            {
                if (originalPos.col == _grid.GetLength(1) - 1) return false;
                nextTile = _grid[originalPos.row, originalPos.col + 1];
            }
            else if (direction.x < 0)
            {
                if (originalPos.col == 0) return false;
                nextTile = _grid[originalPos.row, originalPos.col - 1];
            }

            bool isMovable = nextTile.Type == TileType.Empty &&
                !nextTile.HasSubstate(TileSubState.OnCharacter) &&
                !nextTile.HasSubstate(TileSubState.OnBomb);

            return isMovable;
        }

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
            GridPos enemyPos = _entityPositions.FirstOrDefault(item => item.Key.CharacterType != item.Key.CharacterType).Value;

            return new
                (
                    _entityPositions[entity],
                    nearbyCondition,
                    enemyPos,
                    bombTimerNorm,
                    NearbyRadius
                );
        }

        public void RegisterActiveBomb(BombHandler bomb, GridPos bombPos) => _placedBomb[bombPos] = bomb;

        public bool CanPlaceBomb(BombermanEntity entity)
        {
            GridPos tilePos = _entityPositions[entity];
            return !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnBomb) &&
            !_grid[tilePos.row, tilePos.col].HasSubstate(TileSubState.OnExplosion);
        }

        private bool CanExplosionSpawn(GridPos tilePos) =>
           tilePos.row < _grid.GetLength(0) &&
           tilePos.row >= 0 &&
           tilePos.col < _grid.GetLength(1) &&
           tilePos.col >= 0 &&
           _grid[tilePos.row, tilePos.col].Type != TileType.Wall;

        private bool IsExplosionBlocked(GridPos tilePos) => _grid[tilePos.row, tilePos.col].Type == TileType.Crate;

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

        public void OnBombExplode(List<GridPos> explosionPos)
        {
            foreach (GridPos item in explosionPos)
            {
                if (_grid[item.row, item.col].HasSubstate(TileSubState.OnBomb)) _grid[item.row, item.col].RemoveSubstate(TileSubState.OnBomb);
                _grid[item.row, item.col].AddSubstate(TileSubState.OnExplosion);
            }
        }

        public void OnPropDestroyedAt(GridPos propPos)
        {
            if (_grid[propPos.row, propPos.col].Type == TileType.Crate)
            {
                _grid[propPos.row, propPos.col].Type = TileType.Empty;
                _grid[propPos.row, propPos.col].AddSubstate(TileSubState.OnDestroyedCrate);
            }
        }

        public void OnExplosionFinish(List<GridPos> explosionPos)
        {
            foreach (GridPos item in explosionPos)
                _grid[item.row, item.col].RemoveSubstate(TileSubState.OnExplosion);

            if (_placedBomb.ContainsKey(explosionPos[0])) _placedBomb.Remove(explosionPos[0]);
            else Debug.LogWarning("Can't find spawned bomb at " + explosionPos[0]);
        }

        private Vector3 GridToWorld(GridPos tilePos) => new Vector3(tilePos.col * _tileSize.x + _parentPos.x, _tileSize.y * 1.5f + _parentPos.y, tilePos.row * _tileSize.z * -1 + _parentPos.z);
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
                }
            }
        }
    }
}