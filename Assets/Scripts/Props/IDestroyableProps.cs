using BombermanRL.Character;
using UnityEngine;

namespace BombermanRL.Props
{
    public interface IDestroyableProps
    {
        string Name { get; }
        TileType PropType { get; }
        bool IsDestroyed { get; }
        bool CanBeDestroyedBy(CharacterType characterType);
        void DestroyProps();
        void ResetProp(Vector3 resetWorldPos);
    }
}
