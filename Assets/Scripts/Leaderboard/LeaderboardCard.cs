using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BombermanRL.UI.Leaderboard
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(CanvasGroup))]
    public class LeaderboardCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _timeMoveText;
        [SerializeField] private TMP_Text _etcText;
        [Header("Data")]
        [SerializeField] private Sprite _normalCardSprite;
        [SerializeField] private Sprite _playerCardSprite;

        private LeaderboardModel _rankData;

        public LeaderboardModel RankData { get => _rankData; }

        public void SetCard(LeaderboardModel data, bool isCurrentPlayer)
        {
            _rankData = data;
            Image img = GetComponent<Image>();
            CanvasGroup cg = GetComponent<CanvasGroup>();
            string resultTime = Util.GetTimeFormatResult(data.PlayTime);
            int actionCount = data.ActionCount;

            _timeMoveText.gameObject.SetActive(true);
            _usernameText.gameObject.SetActive(true);

            // Setup player card or normal card
            if (isCurrentPlayer)
            {
                cg.alpha = 1f;
                PlayerLeaderboard playerData = (PlayerLeaderboard) data;
                // Change style card if new rank 
                if(playerData.IsNewRecord)
                {
                    img.sprite = _playerCardSprite;
                    _usernameText.color = new Color32(41, 41, 41, 255);
                    _timeMoveText.color = new Color32(41, 41, 41, 255);
                    _rankText.color = new Color32(252, 163, 17, 255);
                    _rankText.text = $"#{playerData.BestRank}";
                    _timeMoveText.text = $"{resultTime} | {actionCount}  <color=#FCA311><b><i><sub>Best Record</sub></i></b></color>";
                }
                else
                {
                    _timeMoveText.text = $"{resultTime} | {actionCount}  <b><i><sub>Best Record</sub></i></b>";
                }
            }
            else
            {
                cg.alpha = 0.3f;
                img.sprite = _normalCardSprite;
                _usernameText.color = Color.white;
                _timeMoveText.color = Color.white;
                _rankText.color = Color.white;
                _rankText.text = $"#{data.Rank}";
                _timeMoveText.text = $"{resultTime} | {actionCount}";
            }

            _usernameText.text = $"{data.Username}";
        }

        public void SetEllipsisCard()
        {
            CanvasGroup cg = GetComponent<CanvasGroup>();
            cg.alpha = 0.1f;
            _rankText.gameObject.SetActive(false);
            _usernameText.gameObject.SetActive(false);
            _timeMoveText.gameObject.SetActive(false);
            _etcText.gameObject.SetActive(true);
        }
    }


}
