using BombermanRL.Props;
using System.Diagnostics;
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
            _agent.OnActionDecided += OnRequestDecided;
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
            _agent.AddReward(-0.001f);
            _agent.SetGameplayState(state);
            _agent.RequestDecision();
            return _lastAction;
        }

        public void OnDestroy()
        {
            _agent.OnActionDecided -= OnRequestDecided;

        }

        public void OnDestroyProps(IDestroyableProps prop)
        {
            if(prop.PropType == TileType.Crate)
                _agent.AddReward(0.2f);
        }

        public void OnKillSomeone(IBombermanCharacter character)
        {
            switch (character.Type)
            {
                case CharacterType.None:
                    break;
                case CharacterType.Player:
                    _agent.AddReward(1f);
                    break;
                case CharacterType.Bandit:
                    _agent.AddReward(-0.1f);
                    break;
            }
        }

        public void OnRequestDecided(ActionType action)
        {
            _lastAction = action;
        }

        public void OnDead()
        {
            _agent.AddReward(-1f);
            _agent.EndEpisode();
        }
    }
}
