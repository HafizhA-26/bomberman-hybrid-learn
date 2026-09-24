using BombermanRL.Character;
using BombermanRL.Props;
using UnityEngine;

namespace BombermanRL
{
    /// <summary>
    /// Destructible crate prop. "Destruction" is purely a visual sink below the floor
    /// (rather than deactivating the object), which keeps <see cref="ResetProp"/> simple —
    /// resetting just means moving it back up, no re-enabling of renderers/colliders needed.
    /// </summary>
    public class CrateHandler : MonoBehaviour, IDestroyableProps
    {
        [SerializeField] private TileType _tileType;
        // Bitmask of which character types are allowed to destroy this crate with their bombs (assumes CharacterType is a [Flags] enum).
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

        /// <summary>
        /// Sinks the crate below the floor to visually represent destruction. No-ops if already destroyed.
        /// </summary>
        public void DestroyProps()
        {
            if (_isDestroyed) return;

            _isDestroyed = true;
            _crateTransform.position = new Vector3(_crateTransform.position.x, 0.35f, _crateTransform.position.z);
        }

        /// <summary>
        /// Bitmask check — true if <paramref name="characterType"/> is one of the flags set on <see cref="_allowedDestroyer"/>.
        /// </summary>
        /// <param name="characterType">The character type of the entity that placed the exploding bomb.</param>
        /// <returns>True if can be destroyed by <paramref name="characterType"/></returns>
        public bool CanBeDestroyedBy(CharacterType characterType)
        {
            return (_allowedDestroyer & characterType) != 0;
        }

        /// <summary>
        /// Raises the crate back to <paramref name="resetWorldPos"/> and clears its destroyed flag.
        /// </summary>
        /// <param name="resetWorldPos">World position to restore the crate to.</param>
        public void ResetProp(Vector3 resetWorldPos)
        {
            _crateTransform.position = resetWorldPos;
            _isDestroyed = false;
        }
    }
}
