using System;
using System.Collections.Generic;
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
        [SerializeField] private Image _arenaPreviewImg;
        [SerializeField] private TextMeshProUGUI _arenaTitleText;
        [SerializeField] private Button _prevArenaBtn;
        [SerializeField] private Button _nextArenaBtn;
        [SerializeField] private Button _startButton;
        [Header("Data")]
        [SerializeField] private List<Sprite> _arenaPreviews;

        private int _selectedArena = 0;
        private PlayMode _chosenEnemyType = PlayMode.None;

        public Action<string, PlayMode, int> OnStartTriggered;

        private void Awake()
        {
            _prevArenaBtn.onClick.AddListener(OnPrevArena);
            _nextArenaBtn.onClick.AddListener(OnNextArena);
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
            _prevArenaBtn.onClick.RemoveListener(OnPrevArena);
            _nextArenaBtn.onClick.RemoveListener(OnNextArena);
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
                OnStartTriggered?.Invoke(_inputName.Input.text, _chosenEnemyType, _selectedArena);
            }
            else
            {
                GameInstance.Instance.AudioHandler.PlaySFX("SFX_Invalid", true);
            }
        }

        public void TakenUsername()
        {
            _inputName.SetUsernameTakenState();
            GameInstance.Instance.AudioHandler.PlaySFX("SFX_Invalid", true);
        }

        private void OnNextArena()
        {
            _selectedArena = (_selectedArena + _arenaPreviews.Count + 1) % _arenaPreviews.Count;
            _arenaTitleText.text = "ARENA " + (_selectedArena + 1);
            _arenaPreviewImg.sprite = _arenaPreviews[_selectedArena];
        }

        private void OnPrevArena()
        {
            _selectedArena = (_selectedArena + _arenaPreviews.Count - 1) % _arenaPreviews.Count;
            _arenaTitleText.text = "ARENA " + (_selectedArena + 1);
            _arenaPreviewImg.sprite = _arenaPreviews[_selectedArena];
        }

        public void SetStartBtnInteractable(bool enable) => _startButton.interactable = enable;
    }

}
