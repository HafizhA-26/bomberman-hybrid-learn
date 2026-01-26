using BombermanRL.Character;

namespace BombermanRL.Props
{
    public interface IDestroyableProps
    {
        string Name { get; }
        TileType PropType { get; }
        bool IsDestroyed { get; }
        bool CanBeDestroyedBy(CharacterType characterType);
        void DestroyProps();
    }
}
