using BombermanRL.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombermanRL.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private bool _trainingMode = false;

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

        public event Action<string, PlayMode, int> OnStartTriggered; // username, selected playmode
        public event Action OnStartMatch;
        public event Action<int, bool, Action<LeaderboardResult, int>> OnPlayerWin;

        private void Awake()
        {
            if(!_trainingMode)
            {
                _starterUI.OnStartTriggered += OnGameStartTriggered;

#if !UNITY_EDITOR && UNITY_WEBGL
                _deviceType = Util.DetectPlatform();
#else
                _deviceType = 0;
#endif
            }
        }

        private void Start()
        {
            _winCounter.gameObject.SetActive(false);

            // Directly start game if on training mode
            if (_trainingMode)
            {
                GameInstance.Instance.ShowLoading(false);
                _winCounter.gameObject.SetActive(true);
                OnStartMatch?.Invoke();
            }
            else
            {
                _mobileUI.gameObject.SetActive(false);
                _desktopUI.gameObject.SetActive(false);
                _starterUI.gameObject.SetActive(true);
            }
        }
        private void OnDestroy()
        {
            if(!_trainingMode)
            {
                _starterUI.OnStartTriggered -= OnGameStartTriggered;
                if (_player) _player.OnBombCountChanged -= OnBombCountUpdated;
            }
        }

        public void Initialize(PlayerModel playerData)
        {
            if (!_trainingMode) _starterUI.Initialize(playerData?.Username);
        }

        private void OnGameStartTriggered(string playerName, PlayMode chosenEnemyType, int selectedArena)
        {
            _playerName = playerName;
            _enemyType = chosenEnemyType;
            OnStartTriggered?.Invoke(_playerName, _enemyType, selectedArena);
        }

        public void OnTakenUsername()
        {
            GameInstance.Instance.AudioHandler.PlaySFX("SFX_Invalid", true);
            _starterUI.TakenUsername();
            _starterUI.SetStartBtnInteractable(true);
        }

        public void StartMatch()
        {
            if (_deviceType == 0) _desktopUI.gameObject.SetActive(true);
            else _mobileUI.gameObject.SetActive(true);
            _starterUI.gameObject.SetActive(false);
            _winCounter.gameObject.SetActive(true);
            _winCounter.CheckMatchTimer();
            OnStartMatch?.Invoke();
        }

        public void SetupPlayerListener(PlayerController player)
        {
            if(!_trainingMode)
            {
                _player = player;
                _player.OnBombCountChanged += OnBombCountUpdated;
                _player.SetEntityName(_playerName);

                // Setup round win counter
                int enemyType = _enemyType == PlayMode.ManualMLAgent ? 1 : 0;
                Debug.Log("PlayerData NULL? " + (GameInstance.Instance.PlayerData == null));
                Debug.Log("WinRecords NULL? " + (GameInstance.Instance.PlayerData.WinRecords == null));
                Debug.Log("WinRecords Length " + (GameInstance.Instance.PlayerData.WinRecords.Length));
                WinRecordModel winRecord = GameInstance.Instance.PlayerData.WinRecords.FirstOrDefault(x => x.EnemyType == enemyType);
                _winCounter.SetCustomEntity(_player.CharacterType, _player.Name, winRecord?.WinCount ?? 0);
                _winCounter.SetCustomEntity(CharacterType.Bandit, Util.GetEnemyStaticName(_enemyType), winRecord?.LoseCount ?? 0);
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

        public float GetPlaytime() => _winCounter.TimeElapsed;

        public async void OnCharacterWin(CharacterType type)
        {
            bool isWin = type == _player.CharacterType;

            // Increase win counter
            _winCounter.OnCharacterWin(type);

            if(!_trainingMode)
            {
                // Play win/lose sfx
                if (isWin)
                    GameInstance.Instance.AudioHandler.PlaySFX("SFX_Win");
                else
                    GameInstance.Instance.AudioHandler.PlaySFX("SFX_Lose");

                _winCounter.EndMatchTimer();

                // Integrate data on player win
                OnPlayerWin?.Invoke(_player.ExecutedActionCount, isWin, (data, arena) =>
                {
                    _resultUI.SetupRankCards(data, arena);
                    _ = _resultUI.ShowResultPanel(isWin);
                });
            }

        }
        
    }
}