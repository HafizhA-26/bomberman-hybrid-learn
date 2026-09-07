using BombermanRL.Props;
using System;

namespace BombermanRL.Character
{
    public interface IDecisionProvider
    {
        Action<ActionType> OnDecidedAction { get; set; }
        void Decide(GameplayState state);
        void OnInvalidAction(ActionType actionType);
        void OnPlaceBomb();
        void OnMove(bool canMove);
        void OnDestroyProps(IDestroyableProps prop);
        void OnKillSomeone(KillType killType);
        void OnDead(bool isSuicide);
        void OnWin();
        void OnDestroy();
        void OnReset();
    }

}
