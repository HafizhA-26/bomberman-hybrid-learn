using BombermanRL.Props;
using DG.Tweening;
using System;
using UnityEngine;

namespace BombermanRL.Character
{
    /// <summary>
    /// Base AI-controlled entity. Runs a periodic decision loop (a looped, delayed call
    /// spaced by <see cref="AgentParameter.ActionCooldown"/>) that, whenever idle, asks a
    /// pluggable <see cref="IDecisionProvider"/> (rule-based by default; see
    /// <see cref="EnemyMLController"/> for the ML-Agents variant) for an action, then
    /// executes it through the same <see cref="BombermanEntity.RequestMove"/>/
    /// <see cref="BombermanEntity.RequestPlaceBomb"/> events <see cref="PlayerController"/>
    /// uses — this is what lets <see cref="Grid.MatchDirector"/> treat player and AI
    /// entities uniformly. Also relays every relevant lifecycle event (move result, bomb
    /// placed, invalid action, kill, death, win, reset) down to the decision provider so it
    /// can react (e.g. reward shaping for the RL provider).
    /// </summary>
    public class EnemyController : BombermanEntity
    {
        protected AIType _AIType = AIType.RuleBased;
        protected IDecisionProvider _decisionProvider;
        // The looped, delayed call driving the periodic decision cadence; killed/restarted around pauses and resets.
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

        /// <summary>
        /// Constructs this entity's <see cref="IDecisionProvider"/>. Base implementation
        /// wires up the scripted <see cref="RuleBasedDecision"/>; <see cref="EnemyMLController"/>
        /// overrides this to wire up <see cref="RLDecisionProvider"/> instead. Also
        /// instantiates <see cref="BombermanEntity._agentParameter"/> so this entity gets
        /// its own randomizable copy rather than sharing the serialized asset with other entities.
        /// </summary>
        protected virtual void InitializeAI()
        {
            _agentParameter = Instantiate(_agentParameter);
            _decisionProvider = new RuleBasedDecision(_agentParameter, OnDecisionDecided);
        }

        /// <summary>
        /// Starts the periodic decision loop, ticking every <see cref="AgentParameter.ActionCooldown"/> seconds for as long as this entity lives (looped indefinitely).
        /// </summary>
        public virtual void StartAI()
        {
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }

        /// <summary>
        /// Decision-loop tick: if idle, pulls the current local observation from the grid and asks <see cref="_decisionProvider"/> to decide an action. Skipped entirely while walking/dead/resetting.
        /// </summary>
        protected virtual void DecisionCallback()
        {
            if (_currentState == EntityState.Idle)
            {
                GameplayState currState = _stateProvider.GetNearbyState(this, NearbyObservationRadius);
                _decisionProvider.Decide(currState);
            }
        }

        /// <summary>
        /// Translates a decided <see cref="ActionType"/> into the matching move/bomb request. Idle actions are simply skipped (no request fired).
        /// </summary>
        /// <param name="actionToTake">The action chosen by <see cref="_decisionProvider"/>.</param>
        protected virtual void OnDecisionDecided(ActionType actionToTake)
        {
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

        // Requests a bomb placement, or notifies the decision provider of the rejection up-front if out of bombs (rather than letting the grid reject it).
        private void PlaceBomb()
        {
            if (BombCount <= 0)
            {
                _decisionProvider?.OnInvalidAction(ActionType.PlaceBomb);
                return;
            }

            OnRequestPlaceBomb();
        }

        /// <summary>
        /// Resolves the move (base behavior), then reports whether it actually succeeded to the decision provider (e.g. for bumped-move penalties).
        /// </summary>
        /// <param name="targetPos">World position of the destination tile.</param>
        /// <param name="canMove">Whether the grid approved the move.</param>
        /// <param name="onTileChanged">Callback to commit the entity's new logical grid position.</param>
        public override void Move(Vector3 targetPos, bool canMove, Action onTileChanged)
        {
            base.Move(targetPos, canMove, onTileChanged);
            _decisionProvider?.OnMove(canMove);
        }

        /// <summary>
        /// Consumes a bomb charge (base behavior), then notifies the decision provider a bomb was placed.
        /// </summary>
        public override void OnAblePlaceBomb()
        {
            base.OnAblePlaceBomb();
            _decisionProvider?.OnPlaceBomb();
        }

        /// <summary>
        /// Forwards a rejected action to the decision provider.
        /// </summary>
        /// <param name="action">The action type that was rejected.</param>
        public override void OnInvalidAction(ActionType action)
        {
            base.OnInvalidAction(action);
            _decisionProvider?.OnInvalidAction(action);
        }

        /// <summary>
        /// Forwards a kill this entity scored to the decision provider.
        /// </summary>
        /// <param name="killType">Classification of the kill.</param>
        public override void Kill(KillType killType)
        {
            base.Kill(killType);
            _decisionProvider?.OnKillSomeone(killType);
        }

        /// <summary>
        /// Forwards a prop this entity destroyed to the decision provider.
        /// </summary>
        /// <param name="prop">The prop that was destroyed.</param>
        public override void DestroyProps(IDestroyableProps prop)
        {
            base.DestroyProps(prop);
            _decisionProvider?.OnDestroyProps(prop);
        }

        /// <summary>
        /// Stops the decision loop before handing off to the base reset sequence, so the entity doesn't keep trying to act while resetting.
        /// </summary>
        /// <param name="resetWorldPos">World position to teleport to once the delay elapses.</param>
        /// <param name="resetDelay">Seconds to wait before actually teleporting.</param>
        public override void StartReset(Vector3 resetWorldPos, float resetDelay)
        {
            _decisionTween?.Kill();
            base.StartReset(resetWorldPos, resetDelay);
        }

        /// <summary>
        /// Completes the reset (base behavior), notifies the decision provider (which re-randomizes its parameters / flushes episode stats), and restarts the decision loop for the new episode.
        /// </summary>
        /// <param name="resetWorldPos">World position of the new spawn tile.</param>
        protected override void ResetEntity(Vector3 resetWorldPos)
        {
            base.ResetEntity(resetWorldPos);
            _decisionProvider.OnReset();
            Debug.Log($"{name} after randomized. Cooldown: {_agentParameter.ActionCooldown} | Move Duration {_agentParameter.MoveDuration} | Offensive Dis {_agentParameter.OffensiveDistance} | Danger Thres {_agentParameter.DangerBombThreshold}");
            _decisionTween = DOVirtual.DelayedCall(_agentParameter.ActionCooldown, DecisionCallback).SetLoops(-1);
        }

        /// <summary>
        /// Notifies the decision provider that this entity's side won.
        /// </summary>
        public override void Win()
        {
            _decisionProvider?.OnWin();
        }

        /// <summary>
        /// Notifies the decision provider of this entity's death, stops the decision loop, then plays the death animation (base behavior).</summary>
        /// <param name="isSuicide">Whether this was a self-inflicted death.</param>
        public override void Dead(bool isSuicide)
        {
            _decisionProvider?.OnDead(isSuicide);
            _decisionTween?.Kill();
            base.Dead(isSuicide);
        }

        /// <summary>
        /// Pauses/resumes this entity, additionally pausing/resuming the decision loop's tween while paused.
        /// </summary>
        /// <param name="pause">True to pause, false to resume.</param>
        public override void PauseCharacter(bool pause)
        {
            base.PauseCharacter(pause);
            if (pause) _decisionTween?.Pause();
            else _decisionTween?.Play();
        }
    }
}
