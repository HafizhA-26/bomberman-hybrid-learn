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
        [Tooltip("[ML Agent Parameter] How far the agent will observe its surrounding. Can't be randomized")]
        [SerializeField] private int _nearbyObservationRadius = 2;

        [Header("Randomized Parameter")]
        [SerializeField] private bool _isRandomized = false;
        [SerializeField] private float _minActionCooldwon = 0.5f;
        [SerializeField] private float _minMoveDuration = 0.5f;
        [SerializeField] private int _minOffensiveDistance = 1;
        [SerializeField] private float _minDangerBombThreshold = 0.2f;
        [SerializeField] private int _minBombLimit = 1;

        public float ActionCooldown { 
            get { 
                if(!_isRandomized) return _actionCooldown;
                else return Random.Range(_minActionCooldwon, _actionCooldown);
            } 
        }
        public float MoveDuration {
            get
            {
                if (!_isRandomized) return _moveDuration;
                else return Random.Range(_minMoveDuration, _actionCooldown - 0.05f);
            }
        }
        public int OffensiveDistance { 
            get {
                if (!_isRandomized || _minOffensiveDistance == _offensiveDistance) return _offensiveDistance;
                else return Random.Range(_minOffensiveDistance, _offensiveDistance);
            }
        }
        public float DangerBombThreshold { 
            get {
                if (!_isRandomized) return _dangerBombThreshold;
                else return Random.Range(_minDangerBombThreshold, _dangerBombThreshold);
            }
        }
        public int BombLimit { 
            get {
                if (!_isRandomized || _bombLimit == _minBombLimit) return _bombLimit;
                else return Random.Range(_minBombLimit, _bombLimit);
            }
        }

        public int NearbyObservationRadius { get => _nearbyObservationRadius; }
    }
}
