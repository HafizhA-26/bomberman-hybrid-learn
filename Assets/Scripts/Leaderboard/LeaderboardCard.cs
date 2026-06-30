using TMPro;
using UnityEngine;

namespace BombermanRL.UI.Leaderboard
{
    public class LeaderboardCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _timeMoveText;
        [SerializeField] private TMP_Text _etcText;

        private LeaderboardModel _rankData;

        public LeaderboardModel RankData { get => _rankData; }

        public void SetCard(LeaderboardModel data, bool isCurrentPlayer)
        {
            // Set ellipsis card if data null
            if(data == null)
            {
                _rankText.gameObject.SetActive(false);
                _usernameText.gameObject.SetActive(false);
                _timeMoveText.gameObject.SetActive(false);
                _etcText.gameObject.SetActive(true);
            }
            else
            {
                _rankData = data;
                _rankText.gameObject.SetActive(true);
                _usernameText.gameObject.SetActive(true);
                _timeMoveText.gameObject.SetActive(!isCurrentPlayer);
                _etcText.gameObject.SetActive(false);
            }

            // Setup card UI text
            _rankText.text = $"#{data.Rank}";
            _usernameText.text = $"{data.Username}";
            string resultTime = Util.GetTimeFormatResult(data.PlayTime);
            int actionCount = data.ActionCount;
            _timeMoveText.text = $"{resultTime} | {actionCount}";
        }
    }


}
