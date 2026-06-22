using BombermanRL.UI;
using System.Collections;
using UnityEngine;

namespace BombermanRL
{
    public class SessionController : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;

        private string _deviceId;
        private string _savedUsername;

        private void Start()
        {
            if(!PlayerPrefs.HasKey("DeviceID"))
            {
                PlayerPrefs.SetString("DeviceID", System.Guid.NewGuid().ToString());
                PlayerPrefs.Save();
            }

            _deviceId = PlayerPrefs.GetString("DeviceID");
            _savedUsername = PlayerPrefs.GetString("Username", "");

            _uiManager.Initialize(_savedUsername);
        }

        private void OnDestroy()
        {
            
        }
    }
}