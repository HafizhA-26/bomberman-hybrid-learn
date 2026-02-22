using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL
{
    public class BombHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _bomb;
        [SerializeField] private GameObject _explosionCubePrefab;
        [SerializeField] private TextMeshPro _countdownText;

        [Header("Bomb Parameter")]
        [SerializeField] private int _explosionRadius = 1;
        [SerializeField] private float _explodeCountdown = 3f;
        [SerializeField] private float _explodeTime = 2f;
        [SerializeField] private float _explodeTransition = 0.3f;

        private List<GameObject> _explosions = new List<GameObject>();
        private List<Vector3> _explodePos = new List<Vector3>();
        private List<Tween> _explosionTween = new List<Tween>();
        private List<Tween> _finishExplosionTween = new List<Tween>();
        private Tween _countdownTween;
        private float _currentTimer;

        public readonly UnityEvent OnBombExplode = new UnityEvent();
        public readonly UnityEvent OnTickExplosion = new UnityEvent();
        public readonly UnityEvent OnExplosionFinish = new UnityEvent();

        public int ExplosionRadius { get => _explosionRadius; }

        private void OnDestroy()
        {
            StopExplosion();

            foreach (GameObject item in _explosions)
            {
                if (item) Destroy(item);
            }

            OnBombExplode.RemoveAllListeners();
            OnExplosionFinish.RemoveAllListeners();
        }

        public void Initalize(List<Vector3> explodePos)
        {
            _explodePos = explodePos;

            float countdown = _explodeCountdown;
            _countdownTween = DOTween.To(
                () => countdown,
                time =>
                {
                    _currentTimer = time;
                    _countdownText.text = Mathf.CeilToInt(time).ToString();
                }, 0, _explodeCountdown).SetDelay(0.5f);

            _countdownTween.OnComplete(SpawnExplosion);
        }


        private void SpawnExplosion()
        {
            _bomb.SetActive(false);
            _countdownText.gameObject.SetActive(false);
            OnBombExplode?.Invoke();
            OnTickExplosion.Invoke();
            DOVirtual.DelayedCall(_explodeTime / 4, () => OnTickExplosion?.Invoke()).SetLoops(3);

            foreach (Vector3 item in _explodePos)
            {
                GameObject explosionObject = Instantiate(_explosionCubePrefab, transform, true);
                explosionObject.transform.position = item;
                _explosions.Add(explosionObject);

                // Animate fade start explosion
                Material material = explosionObject.GetComponent<MeshRenderer>().material;
                Tween explode = DOTween.To(() => material.GetColor("_BaseColor").a, x =>
                {
                    Color temp = material.GetColor("_BaseColor");
                    temp.a = x;
                    material.SetColor("_BaseColor", temp);
                }, 0.6f, _explodeTransition);

                _explosionTween.Add(explode);
            }

            // Delayed finish explosion
            Tween holdExplosionTween = DOVirtual.DelayedCall(_explodeTime, () =>
            {
                foreach (GameObject item in _explosions)
                {
                    // Animate fade finish explosion
                    Material material = item.GetComponent<MeshRenderer>().material;
                    Tween finishExplode = DOTween.To(() => material.GetColor("_BaseColor").a, x =>
                    {
                        Color temp = material.GetColor("_BaseColor");
                        temp.a = x;
                        material.SetColor("_BaseColor", temp);
                    }, 0f, _explodeTransition);

                    _finishExplosionTween.Add(finishExplode);
                }
                Tween finishingTween = DOVirtual.DelayedCall(_explodeTransition, () =>
                {
                    OnExplosionFinish?.Invoke();
                    Destroy(gameObject);
                });

                _finishExplosionTween.Add(finishingTween);
            });

            _finishExplosionTween.Add(holdExplosionTween);
        }

        public void StopExplosion()
        {
            _countdownTween?.Kill();
        }

        public float GetCurrentTimerNorm() => Mathf.InverseLerp(_explodeCountdown, 0, _currentTimer);
    }

}
