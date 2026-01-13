namespace BombermanRL.Character
{
    public class RuleBasedDecision : IDecisionProvider
    {
        public ActionType Decide(GameplayState state)
        {
            // Add Rule Based Decision
            return ActionType.Idle;
        }
    }
}
