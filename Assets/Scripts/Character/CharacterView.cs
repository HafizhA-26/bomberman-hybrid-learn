using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL.Character
{
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private Animator _characterAnimator;

        private Dictionary<string, float> _animationDurations = new Dictionary<string, float>();
        public Dictionary<string, float> AnimationDurations { get => _animationDurations; }

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
            _characterAnimator.SetTrigger("Death1");
            return _animationDurations["Die_1"];
        }
        /// <summary>
        /// Trigger death animation for enemy
        /// </summary>
        /// <returns>Death animation duration</returns>
        public float SetBadDeath()
        {
            _characterAnimator.SetTrigger("Death2");
            return _animationDurations["Die_2"];
        }
    }

}
