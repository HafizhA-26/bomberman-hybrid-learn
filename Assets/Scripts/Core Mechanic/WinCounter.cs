using BombermanRL.Character;
using BombermanRL.Grid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

namespace BombermanRL
{
    public class WinCounter : MonoBehaviour
    {
        [Serializable]
        private struct CharacterCountText
        {
            public string CharacterName;
            public CharacterType CharacterType;
            public TextMeshProUGUI Text;
        }

        [Header("UI Components References")]
        [SerializeField] private TextMeshProUGUI _roundCountText;
        [SerializeField] private List<CharacterCountText> _charactersWinText;

        [Header("Object References")]
        [SerializeField] private List<MatchDirector> _matchDirector;

        [Header("Logging Settings")]
        [SerializeField] private int _logInterval = 100;
        [SerializeField] private bool _logToCsv = true;
        [SerializeField] private string _csvAppendixName = "OnlineOnly";

        private Dictionary<CharacterType, CharacterCountText> _characterTextDict = new();
        private Dictionary<CharacterType, int> _characterWinCount = new();
        private Dictionary<CharacterType, int> _characterBatchWin = new();

        private int _roundCount;
        private int _batchRoundCount;

        private string _csvFilePath;

        private void Awake()
        {
            foreach (MatchDirector item in _matchDirector)
            {
                item.OnCharacterWin += OnCharacterWin;
            }

            string csvColumns = "";
            _characterTextDict = _charactersWinText.ToDictionary(item => item.CharacterType);
            foreach(CharacterCountText item in  _charactersWinText)
            {
                _characterWinCount[item.CharacterType] = 0;
                _characterBatchWin[item.CharacterType] = 0;
                csvColumns += $"{item.CharacterType},";
            }

            if(_logToCsv)
            {
                _csvFilePath = Path.Combine(Application.dataPath, $"Training_WinRateLog_{_csvAppendixName}.csv");
                if(!File.Exists(_csvFilePath))
                {
                    File.WriteAllText(_csvFilePath, $"TotalEpisodes,{csvColumns}\n");
                }
            }
        }

        private void OnDestroy()
        {
            foreach (MatchDirector item in _matchDirector)
            {
                item.OnCharacterWin -= OnCharacterWin;
            }
        }

        public void OnCharacterWin(CharacterType type)
        {
            if (!_characterTextDict.ContainsKey(type)) return;

            _characterWinCount[type]++;
            _characterBatchWin[type]++;
            _roundCount++;
            _batchRoundCount++;

            _characterTextDict[type].Text.text = $"{_characterWinCount[type]}";
            _roundCountText.text = $"{_roundCount}";
            CheckAndLog();
        }

        private void CheckAndLog()
        {
            if(_batchRoundCount >= _logInterval)
            {
                string logWinRate = "";
                string statsWinrate = "";

                foreach (KeyValuePair<CharacterType, int> item in _characterBatchWin)
                {
                    float charaWinrate = (float)item.Value / _logInterval;
                    if (Academy.Instance.IsCommunicatorOn)
                        Academy.Instance.StatsRecorder.Add($"Stats/{item.Key}_WinRate", charaWinrate);
                    logWinRate += $"{item.Key} : {charaWinrate:P1} |";
                    statsWinrate += $"{charaWinrate},";
                }

                Debug.Log($"[Batch {_roundCount}] Win Rate -> {logWinRate}");
                if (_logToCsv)
                {
                    string logLine = $"{_roundCount},{statsWinrate}\n";
                    File.AppendAllText(_csvFilePath, logLine);
                }
            }
        }

    }

}
