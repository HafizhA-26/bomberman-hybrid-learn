using DG.Tweening;
using UnityEngine;


namespace BombermanRL
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingHandler : MonoBehaviour
    {
        [SerializeField] private RectTransform _loadingThrobber;

        private CanvasGroup _loadingCanvasGroup;
        private Tween _spinLoadingTween;
        private float _minLoadingTime = 0;
        private bool _ableToClose = false; // True if has reached min loading time
        private bool _readyToClose = false; // True if HideLoading has been called even when min loading time has not been reached

        private void Awake()
        {
            _loadingCanvasGroup = GetComponent<CanvasGroup>();
            _loadingCanvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if(gameObject.activeInHierarchy)
            {
                if(_minLoadingTime > 0)
                {
                    _minLoadingTime -= Time.deltaTime;
                }
                else if(!_ableToClose)
                {
                    _ableToClose = true;
                    if(_readyToClose) HideLoadingPanel();
                }
            }
        }

        public void ShowLoadingPanel(float minLoadingTime = 0)
        {
            // Return if loading panel already showed up
            if (gameObject.activeInHierarchy) return;

            _minLoadingTime = minLoadingTime;
            _ableToClose = _minLoadingTime <= 0;
            _readyToClose = false;

            // Start throbber looping animation
            if(_spinLoadingTween == null)
            {
                _spinLoadingTween = _loadingThrobber.DOLocalRotate(Vector3.back * 360, 1.5f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
                _spinLoadingTween.Play();
            }

            gameObject.SetActive(true);
            _loadingCanvasGroup.DOFade(1f, 0.2f);
        }

        public void HideLoadingPanel()
        {
            _readyToClose = true;

            // Return if loading panel already hide
            if (!gameObject.activeInHierarchy || !_ableToClose) return;

            _loadingCanvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                _spinLoadingTween.Kill();
                _spinLoadingTween = null;
            });
        }
    }

}
