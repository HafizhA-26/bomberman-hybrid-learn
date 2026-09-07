using BombermanRL.Props;
using DG.Tweening;
using System;
using UnityEngine;

namespace BombermanRL.Character
{
    public class EnemyController : BombermanEntity
    {
        protected AIType _AIType = AIType.RuleBased;
        protected IDecisionProvider _decisionProvider;
        protected Tween _decisionTween;

        protected new void Awake()
        {
            base.Awake();
            InitializeAI();
        }

        protected new void OnDestroy()
        {
            base.OnDestroy();
            _decisionProvider?.OnDestroy();
            _decisionTween?.Kill();
        }

        protected virtual void InitializeAI()
        {
            _agentParameter = Instantiate(_agentParameter);
            _decisionProvider = new RuleBasedDecision(_agentParameter, OnDecisionDecided);
        }

        public virtual void StartAI()
        {
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }
        
        protected virtual void DecisionCallback()
        {
            if (_currentState == EntityState.Idle)
            {
                GameplayState currState = _stateProvider.GetNearbyState(this, NearbyObservationRadius);
                _decisionProvider.Decide(currState);
            }
        }

        protected virtual void OnDecisionDecided(ActionType actionToTake)
        {
            //if (_AIType == AIType.MLAgent)
            //    Debug.Log("[AI][Decide] Action To Take: " + actionToTake.ToString());
            //else if(_AIType == AIType.RuleBased)
            //    Debug.Log("[Rule Based][Decided] Action To Take: " + actionToTake.ToString());

            //Debug.Log("Current Nearby State: " + currState.ToString());
            //Debug.Log("Action Take: "+actionToTake);

            switch (actionToTake)
            {
                case ActionType.Idle:
                    break;
                case ActionType.MoveUp:
                    OnRequestMove(Vector2.up);
                    break;
                case ActionType.MoveDown:
                    OnRequestMove(Vector2.down);
                    break;
                case ActionType.MoveLeft:
                    OnRequestMove(Vector2.left);
                    break;
                case ActionType.MoveRight:
                    OnRequestMove(Vector2.right);
                    break;
                case ActionType.PlaceBomb:
                    PlaceBomb();
                    break;
            }
        }

        private void PlaceBomb()
        {
            if (BombCount <= 0)
            {
                _decisionProvider?.OnInvalidAction(ActionType.PlaceBomb);
                return;
            }

            OnRequestPlaceBomb();
        }

        public override void Move(Vector3 targetPos, bool canMove, Action onTileChanged)
        {
            base.Move(targetPos, canMove, onTileChanged);
            _decisionProvider?.OnMove(canMove);
        }
        public override void OnAblePlaceBomb()
        {
            base.OnAblePlaceBomb();
            _decisionProvider?.OnPlaceBomb();
        }

        public override void OnInvalidAction(ActionType action)
        {
            base.OnInvalidAction(action);
            _decisionProvider?.OnInvalidAction(action);
        }

        public override void Kill(KillType killType)
        {
            base.Kill(killType);
            _decisionProvider?.OnKillSomeone(killType);
        }

        public override void DestroyProps(IDestroyableProps prop)
        {
            base.DestroyProps(prop);
            _decisionProvider?.OnDestroyProps(prop);
        }

        public override void StartReset(Vector3 resetWorldPos, float resetDelay)
        {
            _decisionTween?.Kill();
            base.StartReset(resetWorldPos, resetDelay);
        }

        protected override void ResetEntity(Vector3 resetWorldPos)
        {
            base.ResetEntity(resetWorldPos);
            _decisionProvider.OnReset();
            Debug.Log($"{name} after randomized. Cooldown: {_agentParameter.ActionCooldown} | Move Duration {_agentParameter.MoveDuration} | Offensive Dis {_agentParameter.OffensiveDistance} | Danger Thres {_agentParameter.DangerBombThreshold}");
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }

        public override void Win()
        {
            _decisionProvider?.OnWin();
        }

        public override void Dead(bool isSuicide)
        {
            _decisionProvider?.OnDead(isSuicide);
            _decisionTween?.Kill();
            base.Dead(isSuicide);
        }

        public override void PauseCharacter(bool pause)
        {
            base.PauseCharacter(pause);
            if (pause) _decisionTween.Pause();
            else _decisionTween?.Play();
        }
    }
}
