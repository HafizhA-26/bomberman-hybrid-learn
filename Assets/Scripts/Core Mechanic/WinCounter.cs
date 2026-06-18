using BombermanRL.Character;
using BombermanRL.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

namespace BombermanRL.UI
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
        [SerializeField] private TextMeshProUGUI _playTimeText;

        [Header("Object References")]
        [SerializeField] private List<MatchDirector> _matchDirector;

        [Header("Timer Settings")]
        [SerializeField] private int _maxPlayTimeSeconds = 6039; // same as 99:99

        [Header("Logging Settings")]
        [SerializeField] private int _logInterval = 100;
        [SerializeField] private bool _logToCsv = true;
        [SerializeField] private string _csvAppendixName = "OnlineOnly";

        private Dictionary<CharacterType, CharacterCountText> _characterTextDict = new();
        private Dictionary<CharacterType, int> _characterWinCount = new();
        private Dictionary<CharacterType, int> _characterBatchWin = new();

        private Coroutine _timeCounter;
        private int _timeElapsed = 0;
        private int _roundCount;
        private int _batchRoundCount;
        private bool _isMatchEnded = true;

        private string _csvFilePath;

        private void Awake()
        {
            foreach (MatchDirector item in _matchDirector)
            {
                item.OnMatchStart += CheckMatchTimer;
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
                item.OnMatchStart -= CheckMatchTimer;
                item.OnCharacterWin -= OnCharacterWin;
            }
        }

        public void OnCharacterWin(CharacterType type)
        {
            if (!_characterTextDict.ContainsKey(type)) return;
            _isMatchEnded = true;

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

        private void CheckMatchTimer()
        {
            if (_timeCounter == null)
            {
                _isMatchEnded = false;
                _timeElapsed = 5990;
                _timeCounter = StartCoroutine(StartMatchTimer());
            }
        }

        private IEnumerator StartMatchTimer()
        {
            while(!_isMatchEnded || _timeElapsed <= _maxPlayTimeSeconds)
            {
                yield return new WaitForSeconds(1f);
                _timeElapsed++;
                int minutes = _timeElapsed / 60;
                int seconds = _timeElapsed % 60;
                string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);
                _playTimeText.text = formattedTime;
                if(_timeElapsed >= _maxPlayTimeSeconds) _playTimeText.text = "NO:OB";
            }
        }
    }

}
