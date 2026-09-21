using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class MobileUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _virtualDpad;
        [SerializeField] private OnScreenStick _virtualJoystick;
        [SerializeField] private Button _bombButton;
        [SerializeField] private Image _bombIcon;

        private void Awake()
        {
            Input.multiTouchEnabled = true;
        }

        /// <summary>
        /// Set interactability of mobile virtual d-pad buttons
        /// </summary>
        /// <param name="interactable">Interactable?</param>
        public void SetDpadInteractable(bool interactable)
        {
            foreach (Transform item in _virtualDpad.transform)
            {
                if(item.TryGetComponent(out OnScreenButton padBtn))
                    padBtn.enabled = interactable;
            }
        }

        /// <summary>
        /// Set interactability of mobile virtual joystick
        /// </summary>
        /// <param name="interactable">Interactable?</param>
        public void SetJoystickInteractable(bool interactable)
        {
            _virtualJoystick.enabled = interactable;
        }

        /// <summary>
        /// Set interactability of mobile bombing button
        /// </summary>
        /// <param name="interactable">Interactable?</param>
        public void SetBombButtonInteractable(bool interactable)
        {
            _bombButton.interactable = interactable;
            _bombIcon.color = interactable ? Color.white : _bombButton.colors.disabledColor;
        }
        
    }
}