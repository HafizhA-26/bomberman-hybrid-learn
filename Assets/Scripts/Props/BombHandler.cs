using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BombermanRL
{
    /// <summary>
    /// Drives a single pooled bomb instance through its full lifecycle: countdown display,
    /// detonation, holding the explosion visuals for a duration, then fading them out and
    /// reporting back to <see cref="Grid.BombManager"/>. Reused from a pool — every field
    /// is reset in <see cref="OnEnable"/>/<see cref="OnDisable"/> rather than on
    /// destroy/create, since the GameObject itself is never destroyed once pooled.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BombHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _bomb;
        [SerializeField] private TextMeshPro _countdownText;

        [Header("Bomb Parameter")]
        [SerializeField] private float _explodeCountdown = 3f;
        [SerializeField] private float _explosionTime = 2f;
        [SerializeField] private float _explodeTransition = 0.2f;

        [Header("SFX Data")]
        [SerializeField] private AudioClip _dropBombSFX;
        [SerializeField] private AudioClip _explosionSFX;

        // Explosion visual objects assigned to this bomb's blast tiles for the current placement (borrowed from BombManager's pool).
        private List<GameObject> _explosions = new List<GameObject>();
        private Dictionary<GameObject, Material> _explosionMat = new Dictionary<GameObject, Material>();
        private List<Vector3> _explodePos = new List<Vector3>();
        private Sequence _explosionSeq;
        private AudioSource _bombAudioSource;
        private float _currentTimer; // Seconds remaining until detonation; read by GetCurrentTimerNorm for AI observation.
        private bool _isExploded;

        /// <summary>Fired the instant the bomb detonates (countdown reaches zero).</summary>
        public Action OnBombExplode;
        /// <summary>Fired every FixedUpdate tick while the explosion is active (post-detonation, before it fades out) — used to check for victims standing in the blast.</summary>
        public Action OnTickExplosion;
        /// <summary>Fired once the explosion visuals have fully faded out and the bomb is done.</summary>
        public Action OnExplosionFinish;

        private void Awake()
        {
            _bombAudioSource = GetComponent<AudioSource>();
            GameInstance.Instance.AudioHandler.OnSFXMute += OnSFXMute;

            _bombAudioSource.mute = GameInstance.Instance.AudioHandler.IsMuteSFX;
        }

        private void OnDestroy()
        {
            if (GameInstance.Instance) GameInstance.Instance.AudioHandler.OnSFXMute -= OnSFXMute;
        }

        // Pooled objects are deactivated rather than destroyed, so this is effectively the "return to pool" cleanup: stop any running sequence and clear all subscribers so the next placer's subscriptions don't stack on top of a previous one's.
        private void OnDisable()
        {
            _explosionMat.Clear();
            _isExploded = false;
            _explosionSeq?.Kill();

            OnBombExplode = null;
            OnTickExplosion = null;
            OnExplosionFinish = null;
        }

        // Pooled objects are reactivated rather than instantiated fresh, so this is effectively the "fetch from pool" setup: reset the countdown display and timer for this new placement.
        private void OnEnable()
        {
            _currentTimer = _explodeCountdown;
            _isExploded = false;
            _bomb.SetActive(true);
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = _explodeCountdown.ToString();
        }

        private void FixedUpdate()
        {
            if (_isExploded) OnTickExplosion?.Invoke();
        }

        /// <summary>
        /// Starts this bomb's full timeline as a single DOTween sequence: countdown display
        /// (with a short initial delay), detonation callback, fading in every blast tile's
        /// explosion visual in parallel, holding for <see cref="_explosionTime"/>, fading
        /// them all back out in parallel, then resetting the explosion objects (fully
        /// transparent, reparented, deactivated) and firing <see cref="OnExplosionFinish"/>.
        /// </summary>
        /// <param name="explodePos">World position for each tile this bomb's blast covers.</param>
        /// <param name="explosionObjects">One pooled explosion visual GameObject per entry in <paramref name="explodePos"/> (must be the same length).</param>
        public void Initalize(List<Vector3> explodePos, List<GameObject> explosionObjects)
        {
            if (explodePos.Count != explosionObjects.Count)
            {
                Debug.LogWarning("Explode positions and explosion counts doesn't sync");
                return;
            }

            _bombAudioSource.PlayOneShot(_dropBombSFX);
            _explosionSeq = DOTween.Sequence();
            _explodePos = explodePos;
            _explosions = explosionObjects;
            _explosions.ForEach(explosion =>
            {
                _explosionMat[explosion] = explosion.GetComponent<MeshRenderer>().material;
                explosion.gameObject.SetActive(true);
            });

            // Start countdown explosion
            float countdown = _explodeCountdown;
            int lastDiplayedValue = -1;
            Tween countdownTween = DOTween.To(
                () => countdown,
                time =>
                {
                    _currentTimer = time;
                    int displayValue = Mathf.CeilToInt(time);
                    if(lastDiplayedValue != displayValue)
                    {
                        lastDiplayedValue = displayValue;
                        _countdownText.text = displayValue.ToString();
                    }
                }, 0, _explodeCountdown).SetDelay(0.3f);
            _explosionSeq.Append(countdownTween);

            // On Explode
            _explosionSeq.AppendCallback(() =>
            {
                _bomb.SetActive(false);
                _countdownText.gameObject.SetActive(false);
                _isExploded = true;
                _bombAudioSource.PlayOneShot(_explosionSFX);
                OnBombExplode?.Invoke();
            });

            // Show explosion fire
            for (int i = 0; i < _explodePos.Count; i++)
            {
                // Setup explosion fire cube
                Vector3 explosionPos = _explodePos[i];
                GameObject explosionObject = _explosions[i];
                explosionObject.transform.position = explosionPos;
                explosionObject.name = "Explosion-" + i;
                explosionObject.transform.parent = transform;
                explosionObject.gameObject.SetActive(true);

                // Animate fade in explosion
                if (i == 0) _explosionSeq.Append(FadeExplosionTransition(explosionObject, true));
                else _explosionSeq.Join(FadeExplosionTransition(explosionObject, true));
            }

            // Hold explosion
            _explosionSeq.AppendInterval(_explosionTime);

            // Finish explosion
            for (int i = 0; i < _explosions.Count; i++)
            {
                GameObject explosionObject = _explosions[i];

                // Animate fade out explosion
                if (i == 0) _explosionSeq.Append(FadeExplosionTransition(explosionObject, false));
                else _explosionSeq.Join(FadeExplosionTransition(explosionObject, false));
            }

            // Invoke finishing event
            _explosionSeq.OnComplete(() =>
            {
                // Reset explosion on complete
                _explosions.ForEach(item =>
                {
                    item.name = "UnusedExplosion";
                    item.transform.SetParent(transform.parent);
                    Color baseColor = _explosionMat[item].GetColor("_BaseColor");
                    baseColor.a = 0;
                    _explosionMat[item].SetColor("_BaseColor", baseColor);
                    item.gameObject.SetActive(false);
                });
                OnExplosionFinish?.Invoke();
            });
        }

        /// <summary>
        /// Normalized (0-1) countdown progress — 0 at placement, 1 at detonation. Consumed by <see cref="Grid.GridStateManager.GetNearbyState"/> as the AI's bomb-timer observation.
        /// </summary>
        public float GetCurrentTimerNorm() => Mathf.InverseLerp(_explodeCountdown, 0, _currentTimer);


        /// <summary>
        /// Fades a single explosion tile's material alpha in (toward 0.6, appearing) or out (toward 0, disappearing).
        /// </summary>
        /// <param name="explosion">The explosion visual GameObject to fade.</param>
        /// <param name="isExplode">True to fade in (appear), false to fade out (disappear).</param>
        public Tween FadeExplosionTransition(GameObject explosion, bool isExplode)
        {
            Material material = _explosionMat[explosion];
            Tween fadeTween = DOTween.To(() => material.GetColor("_BaseColor").a, x =>
            {
                Color temp = material.GetColor("_BaseColor");
                temp.a = x;
                material.SetColor("_BaseColor", temp);
            }, isExplode ? 0.6f : 0, _explodeTransition);

            return fadeTween;
        }

        /// <summary>
        /// Freezes this bomb mid-sequence (used when the whole match/episode is being reset) without resetting its visuals — <see cref="Grid.BombManager.ResetAllBombs"/> handles the actual cleanup afterward.
        /// </summary>
        public void PauseExplosion()
        {
            _isExploded = false;
            _explosionSeq?.Pause();
        }

        /// <summary>
        /// Mutes/unmutes this bomb's audio source in response to the global SFX mute toggle.
        /// </summary>
        /// <param name="mute">True to mute.</param>
        private void OnSFXMute(bool mute) => _bombAudioSource.mute = mute;
    }

}
