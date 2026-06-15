using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombermanRL
{
    public static class MainSceneInitiator
    {
        private const string ENTER_SCENE_NAME = "EnterScene";
        public static string FirstLoadedScenePath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoadRuntimeMethod()
        {
            if (SceneManager.GetActiveScene().name == ENTER_SCENE_NAME) return;
            FirstLoadedScenePath = SceneManager.GetActiveScene().path;
            SceneManager.LoadScene(ENTER_SCENE_NAME, LoadSceneMode.Single);
        }
    }
}