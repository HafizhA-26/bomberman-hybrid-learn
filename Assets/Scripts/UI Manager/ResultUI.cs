using BombermanRL.UI.Leaderboard;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        [SerializeField] private GameObject _currentRankCard;
        [SerializeField] private GameObject _normalRankCard;
        [SerializeField] private GameObject _ellipsisRankCard;

        private LeaderboardCard _playerCard;
        private readonly List<LeaderboardCard> _instantiatedCards = new List<LeaderboardCard>();

        public event Action OnRetryTriggered;

        private void Awake()
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }
        private void OnDestroy()
        {
            _retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        public void SetupRankCards(List<LeaderboardModel> data)
        {
            _instantiatedCards.Clear();

            string currentUsername = GameInstance.Instance.PlayerName;
            LeaderboardCard leaderboardCard = null;

            // Populate leaderboard card
            foreach (LeaderboardModel model in data)
            {
                GameObject card = null;
                bool isCurrentPlayer = false;
                if (model.Username.Equals(currentUsername))
                {
                    isCurrentPlayer = true;
                    card = Instantiate(_currentRankCard, _rankGroupTransform);
                    _playerCard = card.GetComponent<LeaderboardCard>();
                }
                else
                {
                    card = Instantiate(_normalRankCard, _rankGroupTransform);
                }
                leaderboardCard = card.GetComponent<LeaderboardCard>();
                leaderboardCard.SetCard(model, isCurrentPlayer);
                _instantiatedCards.Add(leaderboardCard);
            }

            // Add ellipsis card at last if current player rank > 10 and not in last rank
            if(data.Count > 10 && !data[data.Count - 1].IsLastRank)
            {
                GameObject ellipsisCard = Instantiate(_ellipsisRankCard, _rankGroupTransform);
                leaderboardCard = ellipsisCard.GetComponent<LeaderboardCard>();
                leaderboardCard.SetCard(null, false);
                _instantiatedCards.Add(leaderboardCard);
            }

            Canvas.ForceUpdateCanvases();
        }

        public void ShowResultPanel()
        {
            float elapsedTime = 0;
            int actionCount = 0;

            // Setup before transition sequence 
            _resultPanel.alpha = 0f;
            _rankText.color = new Color32(255, 255, 255, 0);
            _retryButton.image.color = new Color32(255, 255, 255, 0);
            _rankGroupTransform.anchoredPosition = new Vector2(_rankGroupTransform.anchoredPosition.x, -1000);
            float targetScrollY = -_playerCard.transform.localPosition.y;

            _chosenEnemyText.text = Util.GetEnemyStaticName(GameInstance.Instance.OverrideGameConfig.GamePlayMode);
            _usernameText.text = GameInstance.Instance.PlayerName;
            _rankText.text = $"#{_playerCard.RankData.Rank}";

            _resultPanel.gameObject.SetActive(true);

            // Show result panel transition sequence
            Sequence showSeq = DOTween.Sequence();
            showSeq.Append(_resultPanel.DOFade(1f, 0.3f));
            showSeq.Append(_rankGroupTransform.DOAnchorPosY(targetScrollY, 1.5f).SetEase(Ease.OutBack));
            showSeq.Join(_rankText.DOFade(1f, 1f).SetDelay(0.5f));
            showSeq.Append(DOTween.To(() => elapsedTime, 
                (t) =>
                {
                    elapsedTime = t;
                    _timeText.text = Util.GetTimeFormatResult(elapsedTime);
                }, _playerCard.RankData.PlayTime, 0.5f));
            showSeq.Join(DOTween.To(() => actionCount, 
                (a) =>
                {
                    actionCount = a;
                    _actionCountText.text = actionCount.ToString();
                }, _playerCard.RankData.ActionCount, 0.5f));
            showSeq.Append(_retryButton.image.DOFade(1f, 0.3f));
        }

        private void OnRetryClicked()
        {
            _resultPanel.gameObject.SetActive(false);
            OnRetryTriggered?.Invoke();
        }
    }
}