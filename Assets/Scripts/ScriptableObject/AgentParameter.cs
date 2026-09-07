using UnityEngine;

namespace BombermanRL
{
    [CreateAssetMenu(fileName = "AgentParameter", menuName = "Bomberman/Agent Parameter")]
    public class AgentParameter : ScriptableObject
    {
        [Header("Action Parameter")]
        [SerializeField] private float _actionCooldown = 1f;
        [SerializeField] private float _moveDuration = 0.9f;
        [SerializeField] private int _offensiveDistance = 3;
        [SerializeField] private float _dangerBombThreshold = 0.5f;
        [SerializeField] private int _bombLimit = 1;
        [SerializeField] private int _bombExplosionRadius = 1;
        [Tooltip("[ML Agent Parameter] How far the agent will observe its surrounding. Can't be randomized")]
        [SerializeField] private int _nearbyObservationRadius = 2;

        [Header("Randomized Parameter")]
        [SerializeField] private bool _isRandomized = false;
        [SerializeField] private float _minActionCooldwon = 0.5f;
        [SerializeField] private float _maxActionCooldown = 1f;
        [SerializeField] private int _minOffensiveDistance = 1;
        [SerializeField] private int _maxOffensiveDistance = 3;
        [SerializeField] private float _minDangerBombThreshold = 0.2f;
        [SerializeField] private float _maxDangerBombThreshold = 0.5f;

        public float ActionCooldown { get => _actionCooldown; }
        public float MoveDuration { get => _moveDuration;  }
        public int OffensiveDistance { get => _offensiveDistance; }
        public float DangerBombThreshold { get => _dangerBombThreshold; }
        public int BombLimit { get => _bombLimit; }

        public int NearbyObservationRadius { get => _nearbyObservationRadius; }
        public int BombExplosionRadius { get => _bombExplosionRadius; }

        public bool IsRandomizedParameter { get => _isRandomized; }

        public void RandomizeParameter()
        {
            if (!_isRandomized) return;

            _actionCooldown = Random.Range(_minActionCooldwon, _maxActionCooldown);
            _moveDuration = _actionCooldown - 0.1f;
            _offensiveDistance = Random.Range(_minOffensiveDistance, _maxOffensiveDistance);
            _dangerBombThreshold = Random.Range(_minDangerBombThreshold, _maxDangerBombThreshold);
        }
    }
}
