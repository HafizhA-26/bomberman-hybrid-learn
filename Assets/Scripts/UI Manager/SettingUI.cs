using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class SettingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _bgmMuteButton;
        [SerializeField] private Button _sfxMuteButton;
        [Header("Sprite References")]
        [SerializeField] private Sprite _bgmOn;
        [SerializeField] private Sprite _bgmOff;
        [SerializeField] private Sprite _sfxOn;
        [SerializeField] private Sprite _sfxOff;

        private void Awake()
        {
            _bgmMuteButton.onClick.AddListener(ToggleBGM);
            _sfxMuteButton.onClick.AddListener(ToggleSFX);
        }

        private void Start()
        {
            if (GameInstance.Instance.AudioHandler.IsMuteBGM) _bgmMuteButton.image.sprite = _bgmOff;
            else _bgmMuteButton.image.sprite = _bgmOn;

            if (GameInstance.Instance.AudioHandler.IsMuteSFX) _sfxMuteButton.image.sprite = _sfxOff;
            else _sfxMuteButton.image.sprite = _sfxOn;
        }

        private void OnDestroy()
        {
            _bgmMuteButton.onClick.RemoveListener(ToggleBGM);
            _sfxMuteButton.onClick.RemoveListener(ToggleSFX);
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
    }
}