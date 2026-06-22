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
        [SerializeField] private InputNameValidator _inputName;
        [SerializeField] private Button _startButton;

        private EnemyType _chosenEnemyType = EnemyType.None;

        public Action<string, EnemyType> OnStartTriggered;

        private void Awake()
        {
            _startButton.onClick.AddListener(OnStartClicked);
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
            if (_ruleBasedToggle.isOn) _chosenEnemyType = EnemyType.RuleBasedAgent;
            else if (_mlAgentToggle.isOn) _chosenEnemyType = EnemyType.MlAgent;

            bool isEnemyValid = _chosenEnemyType != EnemyType.None;
            bool isNameValid = _inputName.Result == InputNameValidator.ValidationResult.Ok;
            _invalidEnemyText.gameObject.SetActive(!isEnemyValid);

            // Start game only if input valid
            if(isEnemyValid && isNameValid)
            {
                OnStartTriggered?.Invoke(_inputName.Input.text, _chosenEnemyType);
            }
        }

        public void TakenUsername() => _inputName.SetUsernameTakenState();
    }

}
