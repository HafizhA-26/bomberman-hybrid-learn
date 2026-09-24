using BombermanRL.Props;
using System;

namespace BombermanRL.Character
{
    /// <summary>
    /// Common contract for a character's "brain" — implemented by
    /// <see cref="RuleBasedDecision"/> (scripted opponent) and
    /// <see cref="RLDecisionProvider"/> (ML-Agents-driven opponent) so
    /// <see cref="BombermanEntity"/>/<see cref="MatchDirector"/> can drive either without
    /// caring which one is behind it. <see cref="Decide"/> is the only method that chooses
    /// an action; every other method is a lifecycle/event hook a provider can use for
    /// reward shaping, stats, or simply ignore (as the rule-based provider does).
    /// </summary>
    public interface IDecisionProvider
    {
        /// <summary>
        /// Invoked by the provider once it has chosen an action for the current step (synchronously for rule-based, asynchronously for ML-Agents inference).
        /// </summary>
        Action<ActionType> OnDecidedAction { get; set; }

        /// <summary>
        /// Given the current observation, decide (and eventually report via <see cref="OnDecidedAction"/>) the next action to take
        /// </summary>
        /// <param name="state">Nearby observed tile condition. Provided by <see cref="IGameplayStateProvider"/></param>
        void Decide(GameplayState state);

        /// <summary>
        /// Called when an attempted action (e.g. moving into a wall, placing a bomb where one already exists) was rejected by the game.
        /// </summary>
        /// <param name="actionType">Attempted action</param>
        void OnInvalidAction(ActionType actionType);

        /// <summary>
        /// Called after this entity successfully places a bomb.
        /// </summary>
        void OnPlaceBomb();

        /// <summary>
        /// Called after a move attempt resolves, reporting whether it actually succeeded.
        /// </summary>
        /// <param name="canMove">Does entity can actually move or just facing the direction</param>
        void OnMove(bool canMove);

        /// <summary>
        /// Called when this entity destroys a prop (e.g. a crate) with an explosion.
        /// </summary>
        /// <param name="prop">Destroyed prop</param>
        void OnDestroyProps(IDestroyableProps prop);

        /// <summary>
        /// Called when this entity kills another character, with the kill's classification (normal/friendly-fire/suicide).
        /// </summary>
        /// <param name="killType">Kill type when killing someone</param>
        void OnKillSomeone(KillType killType);

        /// <summary>
        /// Called when this entity dies, indicating whether it was a self-inflicted (suicide) death.
        /// </summary>
        /// <param name="isSuicide">Is dead because suicide bomb?</param>
        void OnDead(bool isSuicide);

        /// <summary>
        /// Called when this entity's side wins the match/episode.
        /// </summary>
        void OnWin();

        /// <summary>
        /// Called when this provider is being torn down, to release any subscriptions/resources.
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// Called at the start of a new episode/match to reset per-episode state.
        /// </summary>
        void OnReset();
    }

}
