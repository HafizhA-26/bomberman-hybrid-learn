using Unity.MLAgents.Policies;

namespace BombermanRL.Character
{
    public class RLDecisionProvider : IDecisionProvider
    {
        private AgentBomber _agent;
        private ActionType _lastAction = ActionType.Idle;

        public RLDecisionProvider(AgentBomber agent, LearningType learningType) 
        {
            _agent = agent;
            _agent.OnActionDecided += OnRequestDeciced;
            BehaviorParameters behaviorParam = _agent.GetComponent<BehaviorParameters>();
            switch (learningType)
            {
                case LearningType.OfflineLearning:
                    behaviorParam.BehaviorType = BehaviorType.InferenceOnly;
                    break;
                case LearningType.OnlineLearning:
                    behaviorParam.Model = null;
                    behaviorParam.BehaviorType = BehaviorType.Default;
                    break;
                case LearningType.HybridLearning:
                    behaviorParam.BehaviorType = BehaviorType.Default;
                    break;
            }
        }

        public ActionType Decide(GameplayState state)
        {
            _agent.SetGameplayState(state);
            _agent.RequestDecision();
            return _lastAction;
        }

        public void OnRequestDeciced(ActionType action)
        {
            _lastAction = action;
        }
    }
}
