using BombermanRL.Character;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private bool _playableMode = false;

        [Header("References")]
        [SerializeField] private HUDCounter _winCounter;
        [SerializeField] private MobileUIManager _mobileUI;
        [SerializeField] private DesktopUIManager _desktopUI;
        [SerializeField] private StarterUI _starterUI;
        [SerializeField] private ResultUI _resultUI;

        private PlayerController _player;
        private PlayMode _enemyType;
        private string _playerName;
        private int _deviceType; // 0: Desktop, 1: Android, 2: iOS

        public event Action<string, PlayMode> OnStartTriggered; // username, selected playmode
        public event Action OnStartMatch;

        private void Awake()
        {
            if(_playableMode)
            {
                _starterUI.OnStartTriggered += OnGameStartTriggered;

                GameInstance.Instance.ShowLoading(false);
#if !UNITY_EDITOR && UNITY_WEBGL
                _deviceType = Util.DetectPlatform();
#else
                _deviceType = 0;
#endif
                if(_deviceType == 0 ) _desktopUI.gameObject.SetActive(true);
                else _mobileUI.gameObject.SetActive(true);
            }
        }

        private void Start()
        {
            // Directly start game if on training mode
            if (!_playableMode) OnStartMatch?.Invoke();
            else _starterUI.gameObject.SetActive(true);
        }
        private void OnDestroy()
        {
            if(_playableMode)
            {
                _starterUI.OnStartTriggered -= OnGameStartTriggered;
                if (_player) _player.OnBombCountChanged -= OnBombCountUpdated;
            }
        }

        public void Initialize(string savedUsername)
        {
            if (_playableMode) _starterUI.Initialize(savedUsername);
        }

        private void OnGameStartTriggered(string playerName, PlayMode chosenEnemyType)
        {
            _playerName = playerName;
            _enemyType = chosenEnemyType;
            OnStartTriggered?.Invoke(_playerName, _enemyType);
        }

        public void OnTakenUsername()
        {
            GameInstance.Instance.AudioHandler.PlaySFX("SFX_Invalid", true);
            _starterUI.TakenUsername();
        }

        public void StartMatch()
        {
            _starterUI.gameObject.SetActive(false);
            _winCounter.CheckMatchTimer();
            OnStartMatch?.Invoke();
        }

        public void SetupPlayerListener(PlayerController player)
        {
            if(_playableMode)
            {
                _player = player;
                _player.OnBombCountChanged += OnBombCountUpdated;
                _player.SetEntityName(_playerName);
                _winCounter.SetCustomEntity(_player.CharacterType, _player.Name, 0);
                _winCounter.SetCustomEntity(CharacterType.Bandit, Util.GetEnemyStaticName(_enemyType), 0);
            }
        }

        /// <summary>
        /// Update current player's bomb count text
        /// </summary>
        /// <param name="bombCount">Current bomb count</param>
        private void OnBombCountUpdated(int bombCount)
        {
            switch(_deviceType)
            {
                case 0:
                    _desktopUI.SetBombCount(bombCount);
                    break;
                case 1:
                case 2:
                    _mobileUI.SetBombButtonInteractable(bombCount > 0);
                    break;
                default:
                    break;
            }
        }

        public void OnCharacterWin(CharacterType type)
        {
            float matchTime = _winCounter.OnCharacterWin(type);

            // TODO: integration data
            List<LeaderboardModel> dummyData = new List<LeaderboardModel>
            {
                new(1, "Player1", 10, 300.055f, false, DateTime.Now, DateTime.Now),
                new(2, _playerName, _player.ExecutedActionCount, matchTime, false, DateTime.Now, DateTime.Now),
                new(3, "Player1", 10, 400.055f, false, DateTime.Now, DateTime.Now),
                new(4, "Player1", 10, 500.055f, false, DateTime.Now, DateTime.Now),
                new(5, "Player1", 10, 600.055f, false, DateTime.Now, DateTime.Now),
            };
            _resultUI.SetupRankCards(dummyData);
            _resultUI.ShowResultPanel();
        }
        
    }
}