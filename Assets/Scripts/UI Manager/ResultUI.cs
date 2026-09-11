using BombermanRL.UI.Leaderboard;
using DG.Tweening;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
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
        [SerializeField] private TMP_Text _leaderboardTitle;
        [SerializeField] private TMP_Text _winLoseText;
        [SerializeField] private TMP_Text _chosenEnemyText;
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _bestRankText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _actionCountText;
        [SerializeField] private RectTransform _rankGroupTransform;
        [SerializeField] private Button _retryButton;
        [Header("Data")]
        [SerializeField] private GameObject _leaderboardCardPrefab;
        [SerializeField] private Color32 _winFontColor;
        [SerializeField] private Color32 _loseFontColor;

        private readonly List<LeaderboardCard> _instantiatedCards = new List<LeaderboardCard>();
        private LeaderboardCard _playerCard;
        private PlayerLeaderboard _playerRankData;
        private GameObject _ellipsisCard;

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
            _retryButton.transform.localScale = Vector2.zero;
        }

        public void SetupRankCards(LeaderboardResult data, int selectedArena)
        {
            string currentUsername = GameInstance.Instance.PlayerData.Username;
            string currentEnemy = Util.GetEnemyStaticName(GameInstance.Instance.OverrideGameConfig.GamePlayMode);
            _playerRankData = data.MyRank;
            LeaderboardCard leaderboardCard = null;
            _leaderboardTitle.text = $"ARENA {selectedArena + 1} - {currentEnemy.ToUpper()}";
            // Populate leaderboard card
            for (int i = 0; i < data.TopRanks.Length; i++)
            {
                GameObject card = null;
                bool isCurrentPlayer = false;
                LeaderboardModel model = data.TopRanks[i];

                if (model.Username.Equals(currentUsername))
                {
                    isCurrentPlayer = true;
                    model = new PlayerLeaderboard(_playerRankData.Rank, model.Username, model.ActionCount, model.PlayTime, _playerRankData.BestRank);
                }

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

            // Add ellipsis card at last
            if (_ellipsisCard == null)
            {
                _ellipsisCard = Instantiate(_leaderboardCardPrefab, _rankGroupTransform);
                leaderboardCard = _ellipsisCard.GetComponent<LeaderboardCard>();
                leaderboardCard.SetEllipsisCard();
            }

            ResetAnimComponents();
            _resultPanel.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        public async Awaitable ShowResultPanel(bool isPlayerWin)
        {
            await Awaitable.EndOfFrameAsync();

            float elapsedTime = 0;
            int actionCount = 0;
            float winElapsedTime = 0;
            int winActionCount = 0;
            bool isPlayerOnLeaderboard = (_playerCard != null);

            // Setup before transition sequence 
            float targetScrollY = 0;
            _chosenEnemyText.text = Util.GetEnemyStaticName(GameInstance.Instance.OverrideGameConfig.GamePlayMode);
            _usernameText.text = GameInstance.Instance.PlayerData.Username;

            // Setup result panel based on player win/lose
            if (isPlayerWin)
            {
                _winLoseText.text = "YOU WIN!!!";
                _winLoseText.color = _winFontColor;
                winElapsedTime = _playerRankData.PlayTime;
                winActionCount = _playerRankData.ActionCount;
                _rankText.text = $"{((_playerRankData.Rank == _playerRankData.BestRank)? _playerRankData.Rank : "-")}";
                _bestRankText.text = $"#{_playerRankData.BestRank}";
                _rankText.transform.parent.gameObject.SetActive(true);
                _timeText.gameObject.SetActive(true);
                _actionCountText.gameObject.SetActive(true);
            }
            else
            {
                _winLoseText.text = "YOU LOSE!!!";
                _winLoseText.color = _loseFontColor;
                _rankText.transform.parent.gameObject.SetActive(false);
                _timeText.gameObject.SetActive(true);
                _actionCountText.gameObject.SetActive(false);
            }
            if(_playerCard != null)
                targetScrollY = -_playerCard.transform.localPosition.y;
            else
                targetScrollY = -_ellipsisCard.transform.localPosition.y;
            _rankGroupTransform.anchoredPosition = new Vector2(_rankGroupTransform.anchoredPosition.x, -1000);

            // Show result panel transition sequence
            Sequence showSeq = DOTween.Sequence();
            showSeq.Append(_resultPanel.DOFade(1f, 0.3f));
            showSeq.Append(_rankGroupTransform.DOAnchorPosY(targetScrollY, 1.5f).SetEase(Ease.OutBack));
            if(isPlayerOnLeaderboard)
            {
                showSeq.Join(_rankText.DOFade(1f, 1f).SetDelay(0.5f));
                showSeq.Join(_bestRankText.DOFade(1f, 1f).SetDelay(0.5f));
                showSeq.Append(_playerCard.transform.DOScale(1.4f, 0.75f));
                showSeq.Join(DOTween.To(() => elapsedTime, 
                    (t) =>
                    {
                        elapsedTime = t;
                        _timeText.text = Util.GetTimeFormatResult(elapsedTime);
                    }, winElapsedTime, 0.75f));
                showSeq.Join(DOTween.To(() => actionCount, 
                    (a) =>
                    {
                        actionCount = a;
                        _actionCountText.text = actionCount.ToString();
                    }, winActionCount, 0.75f));
            }
            else
            {
                showSeq.Append(_ellipsisCard.transform.DOScale(1.4f, 0.75f));
            }
            showSeq.Append(_retryButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        }

        private void OnRetryClicked()
        {
            GameInstance.Instance.ShowLoading(true, 0.3f, true);
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }
    }
}