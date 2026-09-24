using BombermanRL.Character;
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
    /// <summary>
    /// Main handling for match scores and time elapsed.
    /// For monitoring training growth, <see cref="_logToCsv"/> can be used for tracking winrate every <see cref="_logInterval"/> rounds
    /// Orchestrated by <see cref="UIManager"/>
    /// </summary>
    public class HUDCounter : MonoBehaviour
    {
        /// <summary>
        /// Simple struct container for easier usage
        /// </summary>
        [Serializable]
        private struct CharacterCountText
        {
            public string CharacterName;
            public CharacterType CharacterType;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI ScoreText;
        }

        [Header("UI Components References")]
        [SerializeField] private TextMeshProUGUI _roundCountText;
        [SerializeField] private TextMeshProUGUI _playTimeText;
        [SerializeField] private RectTransform _timeContainer;
        [SerializeField] private List<CharacterCountText> _charactersWinText;

        [Header("Timer Settings")]
        [SerializeField] private int _maxPlayTimeSeconds = 5999; // same as 99:99

        [Header("Logging Settings")]
        [SerializeField] private int _logInterval = 100;
        [SerializeField] private bool _logToCsv = true;
        [SerializeField] private string _csvAppendixName = "OnlineOnly";

        private Dictionary<CharacterType, CharacterCountText> _characterTextDict = new();
        private Dictionary<CharacterType, int> _characterWinCount = new();
        private Dictionary<CharacterType, int> _characterBatchWin = new();

        private Coroutine _timeCounter;
        private float _timeElapsed = 0;
        private int _roundCount;
        private int _batchRoundCount;
        private bool _isMatchEnded = true;

        private string _csvFilePath;

        public float TimeElapsed { get => _timeElapsed; }

        private void Awake()
        {
            string csvColumns = "";

            // Setup UI and Dictionary
            _characterTextDict = _charactersWinText.ToDictionary(item => item.CharacterType);
            foreach(CharacterCountText item in  _charactersWinText)
            {
                _characterWinCount[item.CharacterType] = 0;
                _characterBatchWin[item.CharacterType] = 0;
                csvColumns += $"{item.CharacterType},";

                item.NameText.text = item.CharacterName;
            }

            // Setup CSV file for logging
            if (_logToCsv)
            {
                _csvFilePath = Path.Combine(Application.dataPath, $"Training_WinRateLog_{_csvAppendixName}.csv");
                if (!File.Exists(_csvFilePath))
                {
                    File.WriteAllText(_csvFilePath, $"TotalEpisodes,{csvColumns}\n");
                }
                else
                {
                    string[] lines = File.ReadAllLines(_csvFilePath);
                    string[] lastRow = lines[lines.Length - 1].Split(',');
                    if (int.TryParse(lastRow[0], out _roundCount))
                    {
                        Debug.Log("Continue log from batch round " + _roundCount);
                        _roundCountText.text = _roundCount.ToString();
                    }
                }
            }
        }

        private void Update()
        {
            _timeElapsed += Time.deltaTime;
        }

        /// <summary>
        /// Update UI and counter on character win 
        /// </summary>
        /// <param name="type">Last standing character type</param>
        public void OnCharacterWin(CharacterType type)
        {
            if (!_characterTextDict.ContainsKey(type)) return;
            _isMatchEnded = true;

            _characterWinCount[type]++;
            _characterBatchWin[type]++;
            _roundCount++;
            _batchRoundCount++;

            _characterTextDict[type].ScoreText.text = $"{_characterWinCount[type]}";
            _roundCountText.text = $"{_roundCount}";
            CheckAndLog();
        }

        /// <summary>
        /// Check per-<see cref="_logInterval"/> and start writing win rate log to csv file
        /// </summary>
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

                foreach (CharacterCountText item in _charactersWinText)
                {
                    _characterBatchWin[item.CharacterType] = 0;
                }
                _batchRoundCount = 0;
            }
        }

        /// <summary>
        /// Check and start match timer
        /// </summary>
        public void CheckMatchTimer()
        {
            if (_timeCounter == null)
            {
                _isMatchEnded = false;
                _timeCounter = StartCoroutine(StartMatchTimer());
            }
        }

        /// <summary>
        /// Stop timer and hide time cpimyer IO
        /// </summary>
        public void EndMatchTimer()
        {
            _isMatchEnded = true;
            StopCoroutine(_timeCounter);
            _timeCounter = null;
            _timeContainer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Setup score UI based on custom character data
        /// </summary>
        /// <param name="type">Custom character type</param>
        /// <param name="characterName">Character name</param>
        /// <param name="score">Base character</param>
        public void SetCustomEntity(CharacterType type, string characterName, int score)
        {
            if(!_characterTextDict.ContainsKey(type))
            {
                Debug.Log("Character data counter doesn't exists");
                return;
            }

            _characterTextDict[type].NameText.text = characterName;
            _characterTextDict[type].ScoreText.text = score.ToString();
        }

        /// <summary>
        /// Coroutine for match timer
        /// </summary>
        /// <returns>Coroutine to start</returns>
        private IEnumerator StartMatchTimer()
        {
            while(!_isMatchEnded && _timeElapsed <= _maxPlayTimeSeconds)
            {
                yield return new WaitForSeconds(1f);
                string formattedTime = Util.GetTimeFormatGameplay(_timeElapsed);
                _playTimeText.text = formattedTime;
                if(_timeElapsed >= _maxPlayTimeSeconds) _playTimeText.text = "NO:OB";
            }
        }
    }

}
