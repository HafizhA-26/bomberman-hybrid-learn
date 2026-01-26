using BombermanRL.Props;

namespace BombermanRL.Character
{
    public interface IDecisionProvider
    {
        ActionType Decide(GameplayState state);
        void OnDestroyProps(IDestroyableProps prop);
        void OnKillSomeone(IBombermanCharacter character);
        void OnDead();
        void OnDestroy();
    }

}
