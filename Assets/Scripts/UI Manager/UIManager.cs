using BombermanRL.Character;
using UnityEngine;

namespace BombermanRL.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MobileUIManager _mobileUI;
        [SerializeField] private DesktopUIManager _desktopUI;

        private PlayerController _player;
        private int _deviceType; // 0: Desktop, 1: Android, 2: iOS

        private void Awake()
        {
            GameInstance.Instance.ShowLoading(false);
#if !UNITY_EDITOR && UNITY_WEBGL
            _deviceType = Util.DetectPlatform();
#else
            _deviceType = 0;
#endif
        }

        public void Initialize(PlayerController player)
        {
            _player = player;
            _player.OnBombCountChanged += OnBombCountUpdated;
        }

        private void OnDestroy()
        {
            if(_player) _player.OnBombCountChanged += OnBombCountUpdated;
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
        
    }
}