using BombermanRL.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Grid
{
    /// <summary>
    /// Owns the pool of <see cref="BombHandler"/> bombs and explosion visual GameObjects,
    /// spawns bombs on request, and relays each bomb's lifecycle events (explode, tick,
    /// finish) up to <see cref="MatchDirector"/> — tagged with the placer entity and the
    /// blast's grid positions, since <see cref="BombHandler"/> itself only knows about
    /// world positions and visuals, not game logic.
    /// </summary>
    public class BombManager : MonoBehaviour
    {
        [SerializeField]
        private Transform _tileParent;

        // Object pooling
        private readonly List<GameObject> _explosionPool = new();
        private readonly List<BombHandler> _bombPool= new();
        private readonly Dictionary<GameObject, Material> _explosionMat = new();

        // Bomb and explosion prefab
        private GameObject _bombPrefab;
        private GameObject _explosionPrefab;

        /// <summary>
        /// Relayed from the exploding <see cref="BombHandler"/>: (placer, blast tiles) the instant a bomb detonates.
        /// </summary>
        public Action<BombermanEntity, List<GridPos>> OnBombExplode;

        /// <summary>
        /// Relayed every FixedUpdate tick while a bomb's explosion is active: (placer, blast tiles) — used to check for victims.
        /// </summary>
        public Action<BombermanEntity, List<GridPos>> OnTickExplosion;

        /// <summary>
        /// Relayed once a bomb's explosion visuals finish fading out: (placer, blast tiles).
        /// </summary>
        public Action<BombermanEntity, List<GridPos>> OnExplosionFinish;

        /// <summary>
        /// Supplies the prefabs and parent transform used for pooling. Must be called once before any bombs are spawned.
        /// </summary>
        /// <param name="bombPrefab">Prefab instantiated (and pooled) for bombs.</param>
        /// <param name="explosionPrefab">Prefab instantiated (and pooled) for explosion blast-tile visuals.</param>
        public void Initialize(GameObject bombPrefab, GameObject explosionPrefab)
        {
            _bombPrefab = bombPrefab;
            _explosionPrefab = explosionPrefab;
        }

        /// <summary>
        /// Pulls a bomb and enough explosion visuals from the pools, positions/names them,
        /// wires this placement's lifecycle callbacks (capturing <paramref name="placer"/>
        /// and <paramref name="bombingGridPos"/> for this specific bomb), and starts the
        /// bomb's countdown via <see cref="BombHandler.Initalize"/>.
        /// </summary>
        /// <param name="placer">The entity that placed this bomb (attributed on explode/tick/finish events).</param>
        /// <param name="bombingGridPos">Grid positions of every tile this bomb's blast will cover, origin first.</param>
        /// <param name="bombingWorldPos">World positions matching <paramref name="bombingGridPos"/> one-to-one.</param>
        /// <param name="tileParent">Tile container parent of the arena to contain the explosions</param>
        /// <returns>The spawned/reused <see cref="BombHandler"/>, or null if either position list is empty.</returns>
        public BombHandler SpawnBomb(BombermanEntity placer, List<GridPos> bombingGridPos, List<Vector3> bombingWorldPos)
        {
            if (bombingWorldPos.Count == 0 || bombingGridPos.Count == 0) return null;

            // Get bomb object from pool
            BombHandler bombObject = CheckAvailBomb();
            bombObject.transform.position = bombingWorldPos[0];
            bombObject.name = $"Bomb[{bombingGridPos[0].row}-{bombingGridPos[0].col}]";
            bombObject.gameObject.SetActive(true);

            // Get explosion objects from pool
            List<GameObject> explosions = CheckAvailExplosion(bombingGridPos.Count);

            // Setup bomb & start countdown
            bombObject.OnBombExplode += () => ExplodeBomb(placer, bombingGridPos);
            bombObject.OnTickExplosion += () => TickExplosion(placer, bombingGridPos);
            bombObject.OnExplosionFinish += () => FinishExplosion(bombObject, placer, bombingGridPos);
            bombObject.Initalize(bombingWorldPos, explosions);

            return bombObject;
        }

        /// <summary>
        /// Returns the first inactive pooled bomb, or instantiates (and pools) a new one if none are free.
        /// </summary>
        /// <returns>Returns the available bomb</returns>
        private BombHandler CheckAvailBomb()
        {
            BombHandler bomb = null;
            // Check available bomb
            bomb = _bombPool.FirstOrDefault(item => !item.gameObject.activeInHierarchy);
            
            // Instantiate new bomb if there are no avail bomb
            if(bomb == null)
            {
                GameObject bombObject = Instantiate(_bombPrefab, _tileParent);
                bomb = bombObject.GetComponent<BombHandler>();
                _bombPool.Add(bomb);
            }

            return bomb;
        }

        /// <summary>
        /// Returns up to <paramref name="explosionCount"/> inactive pooled explosion objects, instantiating (and pooling) as many additional ones as needed to make up the count.
        /// </summary>
        /// <param name="explosionCount">Number of explosion visual objects needed (one per blast tile).</param>
        /// <returns>Return the available explosions</returns>
        private List<GameObject> CheckAvailExplosion(int explosionCount)
        {
            List<GameObject> explosions;
            explosions = _explosionPool.Where(item => !item.activeInHierarchy).Take(explosionCount).ToList();
            int availExplosionCount = explosions.Count;
            
            // Populate new bomb if there are no bomb available
            for (int i = 0; i < explosionCount - availExplosionCount; i++)
            {
                GameObject newExplosion = Instantiate(_explosionPrefab, _tileParent);
                explosions.Add(newExplosion);
                _explosionPool.Add(newExplosion);
                _explosionMat[newExplosion] = newExplosion.GetComponent<MeshRenderer>().material;
            }

            return explosions;
        }

        // Per-bomb closures below (captured over placer + bombingGridPos in SpawnBomb) just relay BombHandler's generic events into the game-logic-flavored, entity/position-tagged events this manager exposes.
        private void ExplodeBomb(BombermanEntity placer, List<GridPos> bombingGridPos) => OnBombExplode?.Invoke(placer, bombingGridPos);
        private void TickExplosion(BombermanEntity placer, List<GridPos> bombingGridPos) => OnTickExplosion?.Invoke(placer, bombingGridPos);

        /// <summary>Relays the finish event, then returns the bomb to the pool (deactivates it).</summary>
        private void FinishExplosion(BombHandler bomb, BombermanEntity placer, List<GridPos> bombingGridPos)
        {
            OnExplosionFinish?.Invoke(placer, bombingGridPos);
            bomb.gameObject.SetActive(false);
        }

        /// <summary>
        /// Pauses every currently active bomb's sequence in place (used when the whole match/episode is resetting).
        /// </summary>
        public void PauseExplosions()
        {
            _bombPool.ForEach(item =>
            {
                if(item.gameObject.activeInHierarchy) item.PauseExplosion();
            });
        }

        /// <summary>
        /// Hard-resets every bomb and explosion visual back to an inactive, fully-transparent, pool-parented state — used for a full episode reset rather than letting each bomb's own fade-out sequence finish naturally.
        /// </summary>
        public void ResetAllBombs()
        {
            _bombPool.ForEach(item => item.gameObject.SetActive(false));
            _explosionPool.ForEach(item =>
            {
                item.name = "UnusedExplosion";
                item.transform.SetParent(_tileParent);
                Color baseColor = _explosionMat[item].GetColor("_BaseColor");
                baseColor.a = 0;
                _explosionMat[item].SetColor("_BaseColor", baseColor);
                item.gameObject.SetActive(false);
            });
        }
    }
}