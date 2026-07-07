using DG.Tweening;
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

        public void PauseAnimation(bool pause)
        {
            if (pause) _characterAnimator.speed = 0f;
            else _characterAnimator.speed = 1f;
        }
    }

}
