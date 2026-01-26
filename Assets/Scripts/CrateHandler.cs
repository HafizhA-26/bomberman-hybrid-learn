using BombermanRL.Character;
using BombermanRL.Props;
using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL
{
    public class CrateHandler : MonoBehaviour, IDestroyableProps
    {
        [SerializeField] private TileType _tileType;
        [SerializeField] private CharacterType _allowedDestroyer;

        private Transform _crateTransform;
        private bool _isDestroyed;

        public string Name => gameObject.name;
        public bool IsDestroyed => _isDestroyed;
        public TileType PropType => _tileType;


        private void Awake()
        {
            _crateTransform = GetComponent<Transform>();
        }

        public void DestroyProps()
        {
            if (_isDestroyed) return;

            _isDestroyed = true;
            _crateTransform.position = new Vector3(_crateTransform.position.x, 0.35f, _crateTransform.position.z);
        }

        public bool CanBeDestroyedBy(CharacterType characterType)
        {
            return (_allowedDestroyer & characterType) != 0;
        }
    }
}
