using BombermanRL.Props;
using DG.Tweening;
using UnityEngine;

namespace BombermanRL.Character
{
    public class EnemyController : BombermanEntity
    {
        protected AIType _AIType = AIType.RuleBased;
        protected IDecisionProvider _decisionProvider;
        protected Tween _decisionTween;

        private void Awake()
        {
            _decisionProvider = new RuleBasedDecision();
        }

        protected void Start()
        {
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }

        protected void OnDestroy()
        {
            _decisionProvider?.OnDestroy();
            _decisionTween?.Kill();
        }

        private void DecisionCallback()
        {
            if (_currentState == EntityState.Idle)
            {
                GameplayState currState = OnRequestGameplayState();
                ActionType actionToTake = _decisionProvider.Decide(currState);

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
        }

        private void PlaceBomb()
        {
            if (BombCount >= BombLimit) return;

            OnRequestPlaceBomb();
            _decisionProvider.OnPlaceBomb();
        }

        public override void Kill(KillType killType)
        {
            base.Kill(killType);
            _decisionProvider.OnKillSomeone(killType);
        }

        public override void DestroyProps(IDestroyableProps prop)
        {
            base.DestroyProps(prop);
            _decisionProvider?.OnDestroyProps(prop);
        }

        public override void StartReset(Vector3 resetWorldPos, float resetDelay)
        {
            _decisionTween?.Kill();
            _decisionProvider.OnReset();
            base.StartReset(resetWorldPos, resetDelay);
        }

        protected override void ResetEntity(Vector3 resetWorldPos)
        {
            base.ResetEntity(resetWorldPos);
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }

        public override void Win()
        {
            _decisionProvider?.OnWin();
        }
    }
}
