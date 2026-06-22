using BombermanRL.Character;
using System;
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

        private PlayerController _player;
        private EnemyType _enemyType;
        private string _playerName;
        private int _deviceType; // 0: Desktop, 1: Android, 2: iOS

        public event Action OnStartGame;

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
            }
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
            // Directly start game if on training mode
            if(!_playableMode) OnStartGame?.Invoke();
            _starterUI.Initialize(savedUsername);
        }

        private void OnGameStartTriggered(string playerName, EnemyType chosenEnemyType)
        {
            //TODO: Add integration to save data before playing
            _playerName = playerName;
            _enemyType = chosenEnemyType;
            _winCounter.CheckMatchTimer();
            _starterUI.gameObject.SetActive(false);
            OnStartGame?.Invoke();
        }

        public void SetupPlayerListener(PlayerController player)
        {
            _player = player;
            _player.OnBombCountChanged += OnBombCountUpdated;
            _player.SetEntityName(_playerName);
            _winCounter.SetCustomEntityName(_player.CharacterType, _player.Name);
            _winCounter.SetCustomEntityName(CharacterType.Bandit, Util.GetEnemyStaticName(_enemyType));
        }


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
            _winCounter.OnCharacterWin(type);
        }
        
    }
}