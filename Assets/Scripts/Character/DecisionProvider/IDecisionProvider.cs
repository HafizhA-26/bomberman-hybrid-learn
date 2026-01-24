namespace BombermanRL.Character
{
    public interface IDecisionProvider
    {
        ActionType Decide(GameplayState state);

    }

}
