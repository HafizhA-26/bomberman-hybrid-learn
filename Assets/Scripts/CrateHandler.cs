using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL
{
    public class CrateHandler : MonoBehaviour
    {
        private Transform _crateTransform;
        private bool _isDestroyed;

        public readonly UnityEvent OnCrateDestroyed = new UnityEvent();

        private void Awake()
        {
            _crateTransform = GetComponent<Transform>();
        }

        private void OnDestroy()
        {
            OnCrateDestroyed.RemoveAllListeners();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDestroyed) return;

            if (other.CompareTag("Explosion"))
            {
                _isDestroyed = true;
                _crateTransform.position = new Vector3(_crateTransform.position.x, 0.35f, _crateTransform.position.z);
                OnCrateDestroyed?.Invoke();
            }
        }
    }
}
