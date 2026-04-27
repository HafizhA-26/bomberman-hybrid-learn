using BombermanRL.Props;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

namespace BombermanRL.Character
{
    public class RLDecisionProvider : IDecisionProvider
    {
        private int _offensiveDistance = 3;

        private AgentBomber _agent;
        private ActionType _lastAction = ActionType.Idle;
        private GameplayState _currentState;
        private int _bumpedMoveCount = 0;

        // Stats variable
        private int _bombPlacedCount = 0;
        private int _bombPlacedNearPlayer = 0;
        private int _stepsAlive = 0;
        private int _destroyCrateCount = 0;
        private int _dodgeBombCount = 0;
        private int _bumpedMoveStatCount = 0;
        private int _killCount = 0;
        private int _deadCount = 0;
        private int _suicideCount = 0;
        private int _winCount = 0;

        public RLDecisionProvider(AgentBomber agent, AgentParameter agentParameter) 
        {
            _agent = agent;
            _agent.OnActionDecided += OnRequestDecided;
            _offensiveDistance = agentParameter.OffensiveDistance;
        }

        public ActionType Decide(GameplayState state)
        {
            _currentState = state;
            _stepsAlive++;
            _agent.AddReward(-0.001f); // Step penalty
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
            {
                _agent.AddReward(0.1f);
                _destroyCrateCount++;
            }
        }

        public void OnKillSomeone(IBombermanCharacter character)
        {
            switch (character.Type)
            {
                case CharacterType.None:
                    break;
                case CharacterType.Player:
                    _killCount++;
                    _agent.AddReward(4f);
                    
                    break;
                case CharacterType.Bandit:
                    _agent.AddReward(-0.5f); // On kill other bandit
                    break;
            }
        }

        public void OnRequestDecided(ActionType action)
        {
            _lastAction = action;
        }

        public void OnDead(bool isSuicide)
        {
            if (isSuicide)
            {
                _agent.AddReward(-2f);
                _suicideCount++;
            }
            else _agent.AddReward(-1f);
            _deadCount++;
        }

        public void OnPlaceBomb()
        {
            _agent.AddReward(0.02f);
            if (_currentState.EntityPos.Distance(_currentState.EntityPos) <= _offensiveDistance)
            {
                _agent.AddReward(0.1f);
                _bombPlacedNearPlayer++;
            }

            _bombPlacedCount++;
        }

        public void OnMove(bool canMove)
        {
            // Check rewarding bumped move
            if (!canMove)
            {
                _bumpedMoveCount++;
                _bumpedMoveStatCount++;
            }
            else
                _bumpedMoveCount = 0;

            if (_bumpedMoveCount >= 2)
                _agent.AddReward(-0.02f);

        }

        public void OnReset()
        {
            Academy.Instance.StatsRecorder.Add("Enemy/StepsAlive", _stepsAlive);
            Academy.Instance.StatsRecorder.Add("Enemy/BumpedMove", _bumpedMoveStatCount);
            Academy.Instance.StatsRecorder.Add("Enemy/BombPlaced", _bombPlacedCount);
            Academy.Instance.StatsRecorder.Add("Enemy/GoodBombPlaced", _bombPlacedNearPlayer);
            Academy.Instance.StatsRecorder.Add("Enemy/DodgedBomb", _dodgeBombCount);
            Academy.Instance.StatsRecorder.Add("Enemy/DestroyCrates", _destroyCrateCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Kills", _killCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Deaths", _deadCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Suicides", _suicideCount);
            Academy.Instance.StatsRecorder.Add("Enemy/Win", _winCount);

            _agent.EndEpisode();
            _bombPlacedCount = 0;
            _bombPlacedNearPlayer = 0;
            _stepsAlive = 0;
            _bumpedMoveStatCount = 0;
            _destroyCrateCount = 0;
            _dodgeBombCount = 0;
            _killCount = 0;
            _deadCount = 0;
            _suicideCount = 0;
            _winCount = 0;
        }

        public void OnWin()
        {
            _winCount++;
            _agent.AddReward(1.5f);
        }
    }
}
