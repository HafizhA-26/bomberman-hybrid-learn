using BombermanRL.Character;
using UnityEngine;

namespace BombermanRL.Props
{
    /// <summary>
    /// Contract for any grid prop that can be destroyed by an explosion (currently just
    /// <see cref="CrateHandler"/>). Implemented so <see cref="Grid.GridStateManager"/> and
    /// <see cref="Grid.MatchDirector"/> can handle prop destruction generically without
    /// knowing the concrete prop type.
    /// </summary>
    public interface IDestroyableProps
    {
        /// <summary>Display name, used in logs (e.g. "Player Destroy Crate[2-3]").</summary>
        string Name { get; }

        /// <summary>The grid tile type this prop occupies (e.g. <see cref="TileType.Crate"/>) — used to restore the correct tile type on reset/destruction.</summary>
        TileType PropType { get; }

        /// <summary>
        /// Whether an explosion placed by <paramref name="characterType"/> is allowed to destroy this prop.
        /// </summary>
        bool IsDestroyed { get; }

        /// <summary>
        /// Whether an explosion placed by <paramref name="characterType"/> is allowed to destroy this prop.
        /// </summary>
        /// <param name="characterType">The character type of the entity that placed the exploding bomb.</param>
        bool CanBeDestroyedBy(CharacterType characterType);

        /// <summary>
        /// Destroys this prop (visually and logically). Should be idempotent — safe to call on an already-destroyed prop.
        /// </summary>
        void DestroyProps();

        /// <summary>
        /// Restores this prop to its original, undestroyed state at <paramref name="resetWorldPos"/>, for episode resets.
        /// </summary>
        void ResetProp(Vector3 resetWorldPos);
    }
}
