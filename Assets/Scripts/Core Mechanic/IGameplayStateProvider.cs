using BombermanRL.Character;

namespace BombermanRL
{
    public interface IGameplayStateProvider
    {
        public GameplayState GetNearbyState(BombermanEntity entity, int NearbyRadius);
    }
}
