using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

namespace BombermanRL
{
    public class WinCounter : MonoBehaviour
    {
        [Header("UI Components References")]
        [SerializeField] private TextMeshProUGUI _playerWinCountText;
        [SerializeField] private TextMeshProUGUI _enemyWinCountText;
        [SerializeField] private TextMeshProUGUI _roundCountText;

        [Header("Object References")]
        [SerializeField] private List<GridManager> _gridManagers;

        [Header("Logging Settings")]
        [SerializeField] private int _logInterval = 100;
        [SerializeField] private bool _logToCsv = true;
        [SerializeField] private string _csvAppendixName = "OnlineOnly";

        private int _roundCount;
        private int _playerWinCount;
        private int _enemyWinCount;

        private int _batchRoundCount;
        private int _batchPlayerWinCount;
        private int _batchEnemyWinCount;

        private string _csvFilePath;

        private void Awake()
        {
            foreach (GridManager item in _gridManagers)
            {
                item.OnPlayerWin += OnPlayerWin;
                item.OnEnemyWin += OnEnemyWin;
            }

            if(_logToCsv)
            {
                _csvFilePath = Path.Combine(Application.dataPath, $"OnlineTraining_WinRateLog_{_csvAppendixName}.csv");
                if(!File.Exists(_csvFilePath))
                {
                    File.WriteAllText(_csvFilePath, "TotalEpisodes,Agent Winrate,Player Winrate\n");
                }
            }
        }

        private void OnDestroy()
        {
            foreach (GridManager item in _gridManagers)
            {
                item.OnPlayerWin -= OnPlayerWin;
                item.OnEnemyWin -= OnEnemyWin;
            }
        }

        private void OnPlayerWin()
        {
            _playerWinCount++;
            _roundCount++;
            _batchPlayerWinCount++;
            _batchRoundCount++;

            UpdateCounterText();
            CheckAndLog();
        }

        private void OnEnemyWin()
        {
            _enemyWinCount++;
            _roundCount++;
            _batchEnemyWinCount++;
            _batchRoundCount++;

            UpdateCounterText();
            CheckAndLog();
        }

        private void UpdateCounterText()
        {
            _playerWinCountText.text = $"{_playerWinCount}";
            _enemyWinCountText.text = $"{_enemyWinCount}";
            _roundCountText.text = $"{_roundCount}";
        }

        private void CheckAndLog()
        {
            if(_batchRoundCount >= _logInterval)
            {
                float agentWinRate = (float)_batchEnemyWinCount / _logInterval;
                float playerWinRate = (float)_batchPlayerWinCount / _logInterval;

                if(Academy.Instance.IsCommunicatorOn)
                {
                    Academy.Instance.StatsRecorder.Add("OnlineStats/Agent_WinRate", agentWinRate);
                    Academy.Instance.StatsRecorder.Add("OnlineStats/RuleBased_WinRate", playerWinRate);
                }

                Debug.Log($"[Batch {_roundCount}] Win Rate -> Agent: {agentWinRate:P1} | Player: {playerWinRate:P1}");

                if(_logToCsv)
                {
                    string logLine = $"{_roundCount},{agentWinRate},{playerWinRate}\n";
                    File.AppendAllText(_csvFilePath, logLine);
                }

                _batchRoundCount = 0;
                _batchEnemyWinCount = 0;
                _batchPlayerWinCount = 0;
            }
        }

    }

}
