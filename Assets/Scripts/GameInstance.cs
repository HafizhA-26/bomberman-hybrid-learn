using BombermanRL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombermanRL
{
    public class GameInstance : MonoBehaviour
    {
        [SerializeField] private string _baseURL = "http://127.0.0.1:3000";
        [SerializeField] private string _playableSceneName = "Playable";
        [Space(15)]
        [SerializeField] private AudioHandler _audioHandler;
        [SerializeField] private LoadingHandler _loadingHandler;
        [SerializeField] private AlertUI _alertHandler;
        [Space(15)]
        [SerializeField] private TextMeshProUGUI _versionText;

        private static bool _onShuttingDown = false;
        private static GameInstance _instance;

        public static GameInstance Instance
        {
            get
            {
                if(_onShuttingDown)
                {
                    Debug.LogWarning("Instance is already destroyed");
                    return null;
                }

                if(_instance == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("GameInstance");
                    if(prefab != null)
                    {
                        GameObject newInstance = Instantiate(prefab);
                        _instance = newInstance.GetComponent<GameInstance>();
                        _instance.name = "GameInstance [Singleton]";
                        DontDestroyOnLoad(newInstance);
                    }
                    else
                    {
                        Debug.Log("Failed to create game instance");
                    }
                }
                return _instance;
            }
        }

        public AudioHandler AudioHandler { get => _audioHandler; }
        public AlertUI AlertHandler { get => _alertHandler; }
        public GameModeConfig OverrideGameConfig { get; set; }
        public PlayerModel PlayerData { get; set; }
        public string BASE_URL { get => _baseURL; }

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
                if(transform.parent == null) DontDestroyOnLoad(gameObject);
            }else if(_instance != this) 
                Destroy(gameObject);
        }

        private void Start()
        {
            _audioHandler.PlayBGM("BGM_Main");

            _versionText.text = "version " + Application.version;

            string firstScene = MainSceneInitiator.FirstLoadedScenePath;
            if (string.IsNullOrEmpty(firstScene) || firstScene.Equals(MainSceneInitiator.ENTER_SCENE_NAME))
                firstScene = _playableSceneName;

            _ = SceneManager.LoadSceneAsync(firstScene);
            ShowLoading(true, 0.3f, true);
        }

        private static void OnApplicationQuit()
        {
            _onShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (_instance == this) _onShuttingDown = true;
        }

        /// <summary>
        /// Show loading panel over all other objects
        /// </summary>
        /// <param name="show">True to show, False to close</param>
        /// <param name="minLoadingTime">Minimum loading time (seconds) to prevent blinking loading experience</param>
        /// <param name="isOpaque">Use fully opaque loading panel?</param>
        public void ShowLoading(bool show, float minLoadingTime = 0, bool isOpaque = false)
        {
            if (show) _loadingHandler.ShowLoadingPanel(minLoadingTime, isOpaque);
            else _loadingHandler.HideLoadingPanel();
        }
    }

}
