using BombermanRL.Props;

namespace BombermanRL.Character
{
    public interface IDecisionProvider
    {
        ActionType Decide(GameplayState state);
        void OnPlaceBomb();
        void OnMove(bool canMove);
        void OnDestroyProps(IDestroyableProps prop);
        void OnKillSomeone(KillType character);
        void OnDead(bool isSuicide);
        void OnWin();
        void OnDestroy();
        void OnReset();
    }

}
