using BombermanRL.UI;
using System.Collections;
using UnityEngine;

namespace BombermanRL
{
    public class SessionController : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [Header("Data")]
        [SerializeField] private GameModeConfig _ruleBasedGameConfig;
        [SerializeField] private GameModeConfig _mlAgentGameConfig;

        private string _deviceId;
        private string _savedUsername;

        private void Start()
        {
            _uiManager.OnStartTriggered += SaveEnterData;

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
            _uiManager.OnStartTriggered -= SaveEnterData;
        }

        private void SaveEnterData(string playerName, PlayMode playMode)
        {
            _savedUsername = playerName;
            PlayerPrefs.SetString("Username", _savedUsername);

            Debug.Log($"Player Name: {playerName} | Chosen Game Mode: {playMode}");

            if (playMode == PlayMode.ManualRuleBased) GameInstance.Instance.OverrideGameConfig = _ruleBasedGameConfig;
            else if (playMode == PlayMode.ManualMLAgent) GameInstance.Instance.OverrideGameConfig = _mlAgentGameConfig;

            // TODO: Save entry play data & check availability of username

            _uiManager.StartMatch();
        }
    }
}