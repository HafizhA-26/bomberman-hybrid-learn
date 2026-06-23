using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class StarterUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _invalidEnemyText;
        [SerializeField] private Toggle _ruleBasedToggle;
        [SerializeField] private Toggle _mlAgentToggle;
        [SerializeField] private TextMeshProUGUI _changeNameTitle;
        [SerializeField] private TextMeshProUGUI _invalidNameText;
        [SerializeField] private InputNameValidator _inputName;
        [SerializeField] private Button _startButton;

        private PlayMode _chosenEnemyType = PlayMode.None;

        public Action<string, PlayMode> OnStartTriggered;

        private void Awake()
        {
            _startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnEnable()
        {
            _startButton.interactable = true;
            _invalidEnemyText.gameObject.SetActive(false);
            _invalidNameText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        public void Initialize(string savedUsername)
        {
            if(!string.IsNullOrEmpty(savedUsername))
            {
                _changeNameTitle.text = "Change Username";
                _inputName.Input.text = savedUsername;
            }
        }

        private void OnStartClicked()
        {
            // Start panel input validator
            if (_ruleBasedToggle.isOn) _chosenEnemyType = PlayMode.ManualRuleBased;
            else if (_mlAgentToggle.isOn) _chosenEnemyType = PlayMode.ManualMLAgent;

            bool isEnemyValid = _chosenEnemyType != PlayMode.None;
            bool isNameValid = _inputName.Result == InputNameValidator.ValidationResult.Ok;
            _invalidEnemyText.gameObject.SetActive(!isEnemyValid);
            _invalidNameText.gameObject.SetActive(!isNameValid);

            // Start game only if input valid
            if(isEnemyValid && isNameValid)
            {
                _startButton.interactable = false;
                OnStartTriggered?.Invoke(_inputName.Input.text, _chosenEnemyType);
            }
            else
            {
                GameInstance.Instance.AudioHandler.PlaySFX("SFX_Invalid", true);
            }
        }

        public void TakenUsername() => _inputName.SetUsernameTakenState();
    }

}
