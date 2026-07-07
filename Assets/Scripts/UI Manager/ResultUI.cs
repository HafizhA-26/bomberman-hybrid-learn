using BombermanRL.UI.Leaderboard;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BombermanRL.UI
{
    public class ResultUI: MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _resultPanel;
        [SerializeField] private TMP_Text _chosenEnemyText;
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _actionCountText;
        [SerializeField] private RectTransform _rankGroupTransform;
        [SerializeField] private Button _retryButton;
        [Header("Data")]
        [SerializeField] private GameObject _leaderboardCardPrefab;

        private LeaderboardCard _playerCard;
        private GameObject _ellipsisCard;
        private readonly List<LeaderboardCard> _instantiatedCards = new List<LeaderboardCard>();

        private void Awake()
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }
        private void OnDestroy()
        {
            _retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        private void ResetAnimComponents()
        {
            _resultPanel.alpha = 0f;
            _rankText.color = new Color32(255, 255, 255, 0);
            _retryButton.image.color = new Color32(255, 255, 255, 0);
        }

        public void SetupRankCards(List<LeaderboardModel> data)
        {
            string currentUsername = GameInstance.Instance.PlayerName;
            LeaderboardCard leaderboardCard = null;

            // Populate leaderboard card
            for (int i = 0; i < data.Count; i++)
            {
                GameObject card = null;
                bool isCurrentPlayer = false;
                LeaderboardModel model = data[i];

                if (model.Username.Equals(currentUsername))
                    isCurrentPlayer = true;

                // Populate missing cards
                if (_instantiatedCards.Count <= i)
                {
                    card = Instantiate(_leaderboardCardPrefab, _rankGroupTransform);
                    _instantiatedCards.Add(card.GetComponent<LeaderboardCard>());
                }

                leaderboardCard = card.GetComponent<LeaderboardCard>();
                leaderboardCard.SetCard(model, isCurrentPlayer);
                if (isCurrentPlayer) _playerCard = leaderboardCard;
            }

            // Add ellipsis card at last if current player rank > 10 and not in last rank
            if (data.Count > 10 && !data[^1].IsLastRank && _ellipsisCard == null)
            {
                _ellipsisCard = Instantiate(_leaderboardCardPrefab, _rankGroupTransform);
                leaderboardCard = _ellipsisCard.GetComponent<LeaderboardCard>();
                leaderboardCard.SetEllipsisCard();
            }

            ResetAnimComponents();
            _resultPanel.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        public async Awaitable ShowResultPanel()
        {
            if(_playerCard == null)
            {
                Debug.LogWarning("Failed to find player's rank");
                return;
            }

            await Awaitable.EndOfFrameAsync();

            float elapsedTime = 0;
            int actionCount = 0;
            
            // Setup before transition sequence 
            float targetScrollY = -_playerCard.transform.localPosition.y;
            _rankGroupTransform.anchoredPosition = new Vector2(_rankGroupTransform.anchoredPosition.x, -1000);

            _chosenEnemyText.text = Util.GetEnemyStaticName(GameInstance.Instance.OverrideGameConfig.GamePlayMode);
            _usernameText.text = GameInstance.Instance.PlayerName;
            _rankText.text = $"#{_playerCard.RankData.Rank}";

            // Show result panel transition sequence
            Sequence showSeq = DOTween.Sequence();
            showSeq.Append(_resultPanel.DOFade(1f, 0.3f));
            showSeq.Append(_rankGroupTransform.DOAnchorPosY(targetScrollY, 1.5f).SetEase(Ease.OutBack));
            showSeq.Join(_rankText.DOFade(1f, 1f).SetDelay(0.5f));
            showSeq.Append(_playerCard.transform.DOScale(1.4f, 0.75f));
            showSeq.Join(DOTween.To(() => elapsedTime, 
                (t) =>
                {
                    elapsedTime = t;
                    _timeText.text = Util.GetTimeFormatResult(elapsedTime);
                }, _playerCard.RankData.PlayTime, 0.75f));
            showSeq.Join(DOTween.To(() => actionCount, 
                (a) =>
                {
                    actionCount = a;
                    _actionCountText.text = actionCount.ToString();
                }, _playerCard.RankData.ActionCount, 0.75f));
            showSeq.Append(_retryButton.image.DOFade(1f, 0.3f));
        }

        private void OnRetryClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}