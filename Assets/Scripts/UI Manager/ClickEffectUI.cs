using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class ClickEffectUI : MonoBehaviour
    {
        private Button _button;
        private Toggle _toggle;

        private void Awake()
        {
            if (TryGetComponent<Button>(out _button))
                _button.onClick.AddListener(ClickSFX);

            if (TryGetComponent<Toggle>(out _toggle))
                _toggle.onValueChanged.AddListener(ToggleSFX);
        }

        private void OnDestroy()
        {
            if(_button != null) _button.onClick.RemoveListener(ClickSFX);
            if(_toggle != null) _toggle.onValueChanged.RemoveListener(ToggleSFX);
        }

        private void ClickSFX()
        {
            GameInstance.Instance.AudioHandler.PlaySFX("SFX_ButtonClick", true);
        }

        private void ToggleSFX(bool isOn)
        {
            GameInstance.Instance.AudioHandler.PlaySFX("SFX_ButtonClick", true);
        }
    }

}
