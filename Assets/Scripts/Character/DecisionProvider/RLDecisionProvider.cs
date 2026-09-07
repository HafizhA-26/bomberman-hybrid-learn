using BombermanRL.Props;
using System;
using Unity.MLAgents;
using UnityEngine;

namespace BombermanRL.Character
{
    public class RLDecisionProvider : IDecisionProvider
    {
        private readonly AgentBomber _agent;
        private readonly AgentParameter _agentParameter;
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
        private Action<ActionType> _onDecidedAction;

        public Action<ActionType> OnDecidedAction { get => _onDecidedAction; set => _onDecidedAction = value; }

        public RLDecisionProvider(AgentBomber agent, AgentParameter agentParameter, Action<ActionType> onDecidedAction) 
        {
            _agent = agent;
            _agent.OnActionDecided += OnRequestDecided;
            _agentParameter = agentParameter;
            _onDecidedAction = onDecidedAction;
        }

        public void Decide(GameplayState state)
        {
            _currentState = state;
            _stepsAlive++;
            _agent.AddReward(-0.001f); // Step penalty
            _agent.SetGameplayState(state);

            _agent.RequestDecision();
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

        public void OnKillSomeone(KillType killType)
        {
            switch (killType)
            {
                case KillType.NormalKill:
                    _killCount++;
                    _agent.AddReward(4f);
                    break;
                case KillType.FriendlyFire:
                    _agent.AddReward(-0.5f);
                    break;
                case KillType.Suicide:
                    break;
            }
        }

        public void OnRequestDecided(ActionType action)
        {
            //Debug.Log("[AI][OnRequestDecided] Action To Take: " + action.ToString());
            _onDecidedAction?.Invoke(action);
        }

        public void OnDead(bool isSuicide)
        {
            if (isSuicide)
            {
                _agent.AddReward(-5f);
                _suicideCount++;
            }
            else _agent.AddReward(-1f);
            _deadCount++;
        }

        public void OnPlaceBomb()
        {
            _agent.AddReward(0.02f);
            if (_currentState.EntityPos.Distance(_currentState.EntityPos) <= _agentParameter.OffensiveDistance)
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
        public void OnInvalidAction(ActionType actionType)
        {
            //Debug.Log($"[{_agent.name}] Invalid Action: " + actionType.ToString());
            switch(actionType)
            {
                case ActionType.PlaceBomb:
                    _agent.AddReward(-0.01f);
                    break;
            }
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
            _agentParameter.RandomizeParameter();
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
