using TMPro;
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
        [SerializeField] private GridManager _gridManager;

        private int _roundCount;
        private int _playerWinCount;
        private int _enemyWinCount;

        private void Awake()
        {
            _gridManager.OnPlayerWin += OnPlayerWin;
            _gridManager.OnEnemyWin += OnEnemyWin;
        }

        private void OnDestroy()
        {
            _gridManager.OnPlayerWin -= OnPlayerWin;
            _gridManager.OnEnemyWin -= OnEnemyWin;
        }

        private void OnPlayerWin()
        {
            _playerWinCount++;
            _roundCount++;
            UpdateCounterText();
        }

        private void OnEnemyWin()
        {
            _enemyWinCount++;
            _roundCount++;
            UpdateCounterText();
        }

        private void UpdateCounterText()
        {
            _playerWinCountText.text = $"{_playerWinCount}";
            _enemyWinCountText.text = $"{_enemyWinCount}";
            _roundCountText.text = $"{_roundCount}";
        }
    }

}
