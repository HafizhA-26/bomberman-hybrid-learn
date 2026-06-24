using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BombermanRL
{
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

        private List<GameObject> _explosions = new List<GameObject>();
        private List<Vector3> _explodePos = new List<Vector3>();
        private Sequence _explosionSeq;
        private AudioSource _bombAudioSource;
        private float _currentTimer;
        private bool _isExploded;

        public Action OnBombExplode;
        public Action OnTickExplosion;
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

        private void OnDisable()
        {
            _isExploded = false;
            _explosionSeq?.Kill();

            OnBombExplode = null;
            OnTickExplosion = null;
            OnExplosionFinish = null;
        }

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
            _explosions.ForEach(explosion => explosion.gameObject.SetActive(true));

            // Start countdown explosion
            float countdown = _explodeCountdown;
            Tween countdownTween = DOTween.To(
                () => countdown,
                time =>
                {
                    _currentTimer = time;
                    _countdownText.text = Mathf.CeilToInt(time).ToString();
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
                // Reset explosion on bomb disable
                _explosions.ForEach(item =>
                {
                    item.name = "UnusedExplosion";
                    item.transform.SetParent(transform.parent);
                    Material material = item.GetComponent<MeshRenderer>().material;
                    Color baseColor = material.GetColor("_BaseColor");
                    baseColor.a = 0;
                    material.SetColor("_BaseColor", baseColor);
                    item.gameObject.SetActive(false);
                });
                OnExplosionFinish?.Invoke();
            });
        }

        public float GetCurrentTimerNorm() => Mathf.InverseLerp(_explodeCountdown, 0, _currentTimer);
        
        public Tween FadeExplosionTransition(GameObject explosion, bool isExplode)
        {
            Material material = explosion.GetComponent<MeshRenderer>().material;
            Tween fadeTween = DOTween.To(() => material.GetColor("_BaseColor").a, x =>
            {
                Color temp = material.GetColor("_BaseColor");
                temp.a = x;
                material.SetColor("_BaseColor", temp);
            }, isExplode ? 0.6f : 0, _explodeTransition);

            return fadeTween;
        }
        public void PauseExplosion() => _explosionSeq?.Pause();

        private void OnSFXMute(bool mute) => _bombAudioSource.mute = mute;
    }

}
