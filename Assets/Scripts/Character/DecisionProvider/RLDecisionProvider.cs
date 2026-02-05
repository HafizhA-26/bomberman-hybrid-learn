using BombermanRL.Props;
using System.Collections.Generic;
using Unity.MLAgents.Policies;

namespace BombermanRL.Character
{
    public class RLDecisionProvider : IDecisionProvider
    {
        private readonly float _dangerBombThreshold = 0.4f;
        private readonly int _offensiveDistance = 2;

        private AgentBomber _agent;
        private ActionType _lastAction = ActionType.Idle;
        private GameplayState _currentState;
        private int _bumpedMoveCount = 0;
        private int _prevDistance = -1;
        private bool _wasInDangerLastStep = false;
        private bool _dodgeRewardGiven = false;

        //private int _minimumStepToEnd = 30;
        //private int _currentStep = 0;

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
            _currentState = state;
            _agent.AddReward(0.001f); // For keeping agent alive
            _agent.SetGameplayState(state);

            // Add reward if entity get closer to the enemy
            int enemyDistance = state.EntityPos.Distance(state.PlayerPos);
            if (_prevDistance >= 0 && enemyDistance < _prevDistance)
                _agent.AddReward(0.004f);
            else if(_prevDistance >= 0 && enemyDistance > _prevDistance)
                _agent.AddReward(-0.002f);

            _prevDistance = enemyDistance;

            // Add dodging bomb reward
            bool isInDangerNow = IsInDanger(state);
            if (_wasInDangerLastStep && !isInDangerNow && !_dodgeRewardGiven)
            {
                _agent.AddReward(0.025f); 
                _dodgeRewardGiven = true;
            }
            if (isInDangerNow)
                _dodgeRewardGiven = false;
            _wasInDangerLastStep = isInDangerNow;

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
                _agent.AddReward(0.1f);
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
            _prevDistance = -1;
            _wasInDangerLastStep = false;
            _dodgeRewardGiven = false;
            _agent.AddReward(-1f);
        }

        public void OnPlaceBomb()
        {
            Dictionary<GridPos, TileState> nearby = _currentState.NearbyCondition;
            bool isPlayerNearby = false;
            bool isSafeTileExists = false;
            foreach (KeyValuePair<GridPos, TileState> item in nearby)
            {
                if (item.Key.Equals(_currentState.PlayerPos) && _currentState.EntityPos.Distance(item.Key) <= _offensiveDistance)
                    isPlayerNearby = true;
                if (item.Value.Type == TileType.Empty && _currentState.EntityPos.Distance(item.Key) == 1)
                    isSafeTileExists = true;
            }
            if (isPlayerNearby && isSafeTileExists)
            {
                _agent.AddReward(0.2f);
            }
        }

        public void OnMove(bool canMove)
        {
            // Check rewarding bumped move
            if (!canMove)
                _bumpedMoveCount++;
            else
                _bumpedMoveCount = 0;

            if (_bumpedMoveCount >= 2)
                _agent.AddReward(-0.001f);

        }

        private bool IsInDanger(GameplayState state)
        {
            foreach (KeyValuePair<GridPos, float> kvp in state.BombTimerNorm)
            {
                if(state.EntityPos.Distance(kvp.Key) <= 1 && kvp.Value > _dangerBombThreshold)
                    return true;
            }
            return false;
        }

        public void OnReset()
        {
            _agent.EndEpisode();
        }
    }
}
