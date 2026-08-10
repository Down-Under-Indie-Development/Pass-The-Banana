using Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;

namespace PTB.Menus
{
    public class MainMenuController : Singleton<MainMenuController>
    {
        // INFO: Start Steam connection
        private void Start()
        {
            SteamManager.Instance.StartSteamGame();
        }

        public void HostGame()
        {
            SceneManager.LoadScene("Test Scene");
            _eventManager.OnHostGame?.Invoke();
            Debug.LogWarning($"Not Fully implemented!");

        }

        public void JoinGame()
        {
            _eventManager.OnJoinGame();
            Debug.LogWarning($"Not implemented!");

        }

        public void Options() { Debug.LogWarning($"Not implemented!"); }

        // INFO: Quit Game
        public void QuitGame()
        {
            _eventManager.OnQuitGame?.Invoke(); // INFO: Allow for saving in the future
            Application.Quit();

#if UNITY_EDITOR
            Debug.LogWarning($"Doesn't work in the editor!");
#endif

        }

        // INFO: If the game was closed
        private void OnApplicationQuit()
        {
            _eventManager.OnQuitGame?.Invoke();

        }
    }
}