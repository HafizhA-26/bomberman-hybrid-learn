using BombermanRL.API;
using BombermanRL.Grid;
using BombermanRL.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BombermanRL
{
    public class SessionController : MonoBehaviour
    {
        [SerializeField] private LevelBuilder _levelBuilder;
        [SerializeField] private UIManager _uiManager;
        [Header("Data")]
        [SerializeField] private GameModeConfig _ruleBasedGameConfig;
        [SerializeField] private GameModeConfig _mlAgentGameConfig;
        [SerializeField] private LevelTilemapData[] _arenaData;

        private string _deviceId;
        private int _selectedArena;

        private void Start()
        {
            _uiManager.OnStartTriggered += SaveEnterData;
            _uiManager.OnPlayerWin += UpdateLeaderboard;
            _uiManager.gameObject.SetActive(false);

            if (!PlayerPrefs.HasKey("DeviceID"))
            {
                PlayerPrefs.SetString("DeviceID", System.Guid.NewGuid().ToString());
                PlayerPrefs.Save();
            }

            _deviceId = PlayerPrefs.GetString("DeviceID");
            GetSavedUsername(_deviceId);
        }

        private void OnDestroy()
        {
            _uiManager.OnStartTriggered -= SaveEnterData;
            _uiManager.OnPlayerWin -= UpdateLeaderboard;
        }

        private void GetSavedUsername(string deviceId)
        {
            _ = APIManager.GetPlayerData(deviceId, (response) =>
            {
                GameInstance.Instance.PlayerData = response.Data;
                _uiManager.gameObject.SetActive(true);
                _uiManager.Initialize(response.Data);
            });
        }

        private void SaveEnterData(string playerName, PlayMode playMode, int selectedArena)
        {
            Debug.Log("Save Enter Data");
            if(_arenaData.Length <= selectedArena)
            {
                Debug.Log("Show Error Popup");
                GameInstance.Instance.AlertHandler.ShowErrorPopup("Arena data not found", () => SaveEnterData(playerName, playMode, selectedArena));
                return;
            }

            Dictionary<string, string> data = new()
            {
                ["username"] = playerName,
                ["deviceId"] = _deviceId
            };

            Debug.Log($"Player Name: {playerName} | Chosen Game Mode: {playMode}");

            if (playMode == PlayMode.ManualRuleBased) GameInstance.Instance.OverrideGameConfig = _ruleBasedGameConfig;
            else if (playMode == PlayMode.ManualMLAgent) GameInstance.Instance.OverrideGameConfig = _mlAgentGameConfig;

            _ = APIManager.UpdatePlayerInfo(data, (response) =>
            {
                if (response.Data != null)
                {
                    GameInstance.Instance.PlayerData = response.Data;
                    _selectedArena = selectedArena;
                    _levelBuilder.SetLevelData(_arenaData[selectedArena]);
                    _uiManager.StartMatch();
                }
                else if (response.Message.Contains("Username"))
                    _uiManager.OnTakenUsername();
                else
                    GameInstance.Instance.AlertHandler.ShowErrorPopup("Somehting wrong when update your name", () => SaveEnterData(playerName, playMode, selectedArena));
            });
        }

        public void UpdateLeaderboard(int actionCount, bool isWin, Action<LeaderboardResult, int> onSuccess)
        {
            Dictionary<string, string> data = new()
            {
                ["deviceId"] = _deviceId,
                ["actionCount"] = actionCount.ToString(),
                ["playTime"] = _uiManager.GetPlaytime().ToString(),
                ["enemyType"] = GameInstance.Instance.OverrideGameConfig.GamePlayMode == PlayMode.ManualRuleBased ? "0" : "1",
                ["arena"] = _selectedArena.ToString(),
                ["isWin"] = isWin.ToString()
            };

            _ = APIManager.PostLeaderboard(data, (response) =>
            {
                if(response.Data != null)
                {
                    onSuccess?.Invoke(response.Data, _selectedArena);
                }
                else
                {
                    GameInstance.Instance.AlertHandler.ShowErrorPopup("Somehting wrong when update leaderboard", () => UpdateLeaderboard(actionCount, isWin, onSuccess));
                }
            });
        }
    }
}