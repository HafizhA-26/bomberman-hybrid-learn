using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class SettingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _bgmMuteButton;
        [SerializeField] private Button _sfxMuteButton;
        [SerializeField] private Button _fullscreenButton;
        [Header("Sprite References")]
        [SerializeField] private Sprite _bgmOn;
        [SerializeField] private Sprite _bgmOff;
        [SerializeField] private Sprite _sfxOn;
        [SerializeField] private Sprite _sfxOff;
        [SerializeField] private Sprite _fullScreen;
        [SerializeField] private Sprite _shrinkScreen;

        private bool _lastFullscrenState;
        private void Awake()
        {
            _bgmMuteButton.onClick.AddListener(ToggleBGM);
            _sfxMuteButton.onClick.AddListener(ToggleSFX);
            _fullscreenButton.onClick.AddListener(ToggleFullscreen);
        }

        private void Start()
        {
            if (GameInstance.Instance.AudioHandler.IsMuteBGM) _bgmMuteButton.image.sprite = _bgmOff;
            else _bgmMuteButton.image.sprite = _bgmOn;

            if (GameInstance.Instance.AudioHandler.IsMuteSFX) _sfxMuteButton.image.sprite = _sfxOff;
            else _sfxMuteButton.image.sprite = _sfxOn;
        }

        private void Update()
        {
            if(_lastFullscrenState != Screen.fullScreen)
            {
                _lastFullscrenState = Screen.fullScreen;
                _fullscreenButton.image.sprite = _lastFullscrenState ? _shrinkScreen : _fullScreen;
            }
        }

        private void OnDestroy()
        {
            _bgmMuteButton.onClick.RemoveListener(ToggleBGM);
            _sfxMuteButton.onClick.RemoveListener(ToggleSFX);
            _fullscreenButton.onClick.RemoveListener(ToggleFullscreen);
        }

        private void ToggleBGM()
        {
            if (GameInstance.Instance.AudioHandler.ToggleMuteBGM()) _bgmMuteButton.image.sprite = _bgmOff;
            else _bgmMuteButton.image.sprite = _bgmOn;
        }

        private void ToggleSFX()
        {
            if (GameInstance.Instance.AudioHandler.ToggleMuteSFX()) _sfxMuteButton.image.sprite = _sfxOff;
            else _sfxMuteButton.image.sprite = _sfxOn;
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        private void OnBGMMute(bool mute) => _bgmMuteButton.image.sprite = mute ? _bgmOff : _bgmOn;
        private void OnSFXMute(bool mute) => _sfxMuteButton.image.sprite = mute ? _sfxOff : _sfxOn;

    }
}