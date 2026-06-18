using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class MobileUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private OnScreenStick _virtualJoystick;
        [SerializeField] private Button _bombButton;
        [SerializeField] private Image _bombIcon;

        public void SetJoystickInteractable(bool interactable)
        {
            _virtualJoystick.enabled = interactable;
        }

        public void SetBombButtonInteractable(bool interactable)
        {
            _bombButton.interactable = interactable;
            _bombIcon.color = interactable ? Color.white : _bombButton.colors.disabledColor;
        }
        
    }
}