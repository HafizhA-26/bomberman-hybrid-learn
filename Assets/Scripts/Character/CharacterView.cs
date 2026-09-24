using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL.Character
{
    /// <summary>
    /// Presentation layer for a character: drives the Animator (walk/idle/death) and plays
    /// the red damage flash effect on death. Holds no gameplay state — every method is a
    /// simple, stateless trigger called by <see cref="BombermanEntity"/> and its subclasses.
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private Animator _characterAnimator;

        // Animation clip name -> length in seconds, so callers can know how long a triggered animation (e.g. a death) will play for.
        private Dictionary<string, float> _animationDurations = new Dictionary<string, float>();
        public Dictionary<string, float> AnimationDurations { get => _animationDurations; }

        // Caches every clip's duration from the Animator Controller once, up front, to avoid repeated lookups at runtime.
        private void Awake()
        {
            foreach (AnimationClip item in _characterAnimator.runtimeAnimatorController.animationClips)
            {
                _animationDurations[item.name] = item.length;
            }
        }

        /// <summary>
        /// Trigger walk loop animation
        /// </summary>
        public void SetWalk() => _characterAnimator.SetTrigger("Walk");

        /// <summary>
        /// Trigger idle loop animation
        /// </summary>
        public void SetIdle() => _characterAnimator.SetTrigger("Idle");

        /// <summary>
        /// Trigger death animation for player or ally
        /// </summary>
        /// <returns>Death animation duration</returns>
        public float SetGoodDeath()
        {
            PlayDamagedEffect();
            _characterAnimator.SetTrigger("Death1");
            return _animationDurations["Die_1"];
        }

        /// <summary>
        /// Trigger death animation for enemy
        /// </summary>
        /// <returns>Death animation duration</returns>
        public float SetBadDeath()
        {
            PlayDamagedEffect();
            _characterAnimator.SetTrigger("Death2");
            return _animationDurations["Die_2"];
        }

        /// <summary>
        /// Flashes every visible skinned-mesh material on this character red then back to normal, as a damage/death cue. Runs in parallel with (not blocking) the death animation trigger.
        /// </summary>
        private void PlayDamagedEffect()
        {
            List<Material> materialToAnimate = new List<Material>();
            foreach (Transform child in transform)
            {
                if(child.gameObject.activeInHierarchy &&  child.TryGetComponent(out SkinnedMeshRenderer renderer))
                {
                    materialToAnimate.Add(renderer.material);
                }
            }

            Sequence damagedSeq = DOTween.Sequence();
            for (int i = 0; i < materialToAnimate.Count; i++)
            {
                if (i == 0) damagedSeq.Append(FadeRedDamage(materialToAnimate[i], true));
                else damagedSeq.Join(FadeRedDamage(materialToAnimate[i], true));
            }

            for (int i = 0; i < materialToAnimate.Count; i++)
            {
                if (i == 0) damagedSeq.Append(FadeRedDamage(materialToAnimate[i], false));
                else damagedSeq.Join(FadeRedDamage(materialToAnimate[i], false));
            }
        }

        /// <summary>
        /// Tweens a material toward/away from red by fading its green and blue channels
        /// out (fade in = red flash appears) or back in (fade out = returns to normal),
        /// since removing G/B while keeping R leaves only red.
        /// </summary>
        /// <param name="material">Material to animate (mutated directly, so should be an instance, not a shared asset).</param>
        /// <param name="isFadeIn">True to fade toward red (G/B -> 0.2), false to fade back to normal (G/B -> 1).</param>
        /// <returns>The running tween, so it can be sequenced/joined by the caller.</returns>
        private Tween FadeRedDamage(Material material, bool isFadeIn)
        {
            Tween fadeTween = DOTween.To(() => material.GetColor("_BaseColor").g, x =>
            {
                Color temp = material.GetColor("_BaseColor");
                temp.g = x;
                temp.b = x;
                material.SetColor("_BaseColor", temp);
            }, isFadeIn ? 0.2f : 1, 0.5f);

            return fadeTween;
        }

        /// <summary>
        /// Freezes/resumes the Animator entirely, by zeroing or restoring its playback speed.
        /// </summary>
        /// <param name="pause">True to freeze all animation, false to resume normal playback.</param>
        public void PauseAnimation(bool pause)
        {
            if (pause) _characterAnimator.speed = 0f;
            else _characterAnimator.speed = 1f;
        }
    }

}
