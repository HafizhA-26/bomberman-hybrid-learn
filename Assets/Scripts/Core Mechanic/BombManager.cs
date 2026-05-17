using BombermanRL.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.Grid
{
    public class BombManager : MonoBehaviour
    {
        private Transform _tileParent;

        // Object pooling
        private readonly List<GameObject> _explosionPool = new();
        private readonly List<BombHandler> _bombPool= new();

        // Bomb and explosion prefab
        private GameObject _bombPrefab;
        private GameObject _explosionPrefab;


        public Action<BombermanEntity, List<GridPos>> OnBombExplode;
        public Action<BombermanEntity, List<GridPos>> OnTickExplosion;
        public Action<BombermanEntity, List<GridPos>> OnExplosionFinish;

        public void Initialize(GameObject bombPrefab, GameObject explosionPrefab, Transform tileParent)
        {
            _bombPrefab = bombPrefab;
            _explosionPrefab = explosionPrefab;
            _tileParent = tileParent;
        }

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
            if (bombObject.TryGetComponent(out BombHandler bomb))
            {
                bomb.OnBombExplode += () => OnBombExplode?.Invoke(placer, bombingGridPos);
                bomb.OnTickExplosion += () => OnTickExplosion?.Invoke(placer, bombingGridPos);
                bomb.OnExplosionFinish += () => OnExplosionFinish?.Invoke(placer, bombingGridPos);
                bomb.Initalize(bombingWorldPos, explosions);
            }

            return bomb;
        }

        private BombHandler CheckAvailBomb()
        {
            BombHandler bomb = null;
            bomb = _bombPool.FirstOrDefault(item => !item.gameObject.activeInHierarchy);
            if(bomb == null)
            {
                GameObject bombObject = Instantiate(_bombPrefab, _tileParent);
                _bombPool.Add(bomb.GetComponent<BombHandler>());
            }

            return bomb;
        }

        private List<GameObject> CheckAvailExplosion(int explosionCount)
        {
            List<GameObject> explosions;
            explosions = _explosionPool.Where(item => !item.activeInHierarchy).Take(explosionCount).ToList();
            for (int i = 0; i < explosionCount - explosions.Count; i++)
            {
                GameObject newExplosion = Instantiate(_explosionPrefab, _tileParent);
                explosions.Add(newExplosion);
                _explosionPool.Add(newExplosion);
            }

            return explosions;
        }

        public void PauseExplosions()
        {
            _bombPool.ForEach(item =>
            {
                if(item.gameObject.activeInHierarchy) item.PauseExplosion();
            });
        }

        public void ResetAllBombs()
        {
            _bombPool.ForEach(item => item.gameObject.SetActive(false));
            _explosionPool.ForEach(item => item.gameObject.SetActive(false));
        }
    }
}