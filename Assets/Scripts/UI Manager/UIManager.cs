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

        private void Awake()
        {
            // TODO: Check device via .jslib
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
            Debug.Log("Bomb Count: "+bombCount);
            _desktopUI.SetBombCount(bombCount);
            _mobileUI.SetBombButtonInteractable(bombCount > 0);
        }
        
    }
}