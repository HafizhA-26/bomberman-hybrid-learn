using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class AlertUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _alertCG;
        [SerializeField] private TextMeshProUGUI _errorText;
        [SerializeField] private Button _retryButton;

        private void Awake()
        {
            _alertCG.alpha = 0f;
            _retryButton.onClick.RemoveAllListeners();
            gameObject.SetActive(false);
        }

        public void ShowErrorPopup<T>(BaseResponse<T> response, UnityAction retryCallback)
        {
            gameObject.SetActive(true);

            _retryButton.interactable = true;
            _retryButton.onClick.RemoveAllListeners();
            _retryButton.onClick.AddListener(() =>
            {
                _retryButton.interactable = false;
                _alertCG.DOFade(0f, 0.3f).OnComplete(() =>
                {
                    retryCallback?.Invoke();
                    gameObject.SetActive(false);
                });
            });

            _alertCG.DOFade(1f, 0.3f);
            _errorText.text = response.Error ?? response.Message;
        }

        public void ShowErrorPopup(string error, UnityAction retryCallback)
        {
            gameObject.SetActive(true);

            _retryButton.interactable = true;
            _retryButton.onClick.RemoveAllListeners();
            _retryButton.onClick.AddListener(() =>
            {
                _retryButton.interactable = false;
                _alertCG.DOFade(0f, 0.3f).OnComplete(() =>
                {
                    retryCallback?.Invoke();
                    gameObject.SetActive(false);
                });
            });

            _alertCG.DOFade(1f, 0.3f);
            _errorText.text = error;
        }
    }
}