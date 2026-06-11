using UnityEngine;

namespace BombermanRL
{
    public class GameInstance : MonoBehaviour
    {
        [SerializeField] private AudioHandler _audioHandler;

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
                    _instance = FindAnyObjectByType<GameInstance>();
                    if(_instance == null)
                    {
                        GameObject newInstance = new GameObject();
                        _instance = newInstance.AddComponent<GameInstance>();
                        _instance.name = "GameInstance [Singleton]";
                        DontDestroyOnLoad(newInstance);
                    }
                }
                return _instance;
            }
        }

        public AudioHandler AudioHandler { get => _audioHandler; }

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
                if(transform.parent == null) DontDestroyOnLoad(gameObject);
            }else if(_instance != this) 
                Destroy(gameObject);
        }

        private void OnApplicationQuit()
        {
            _onShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (_instance == this) _onShuttingDown = true;
        }
    }

}
