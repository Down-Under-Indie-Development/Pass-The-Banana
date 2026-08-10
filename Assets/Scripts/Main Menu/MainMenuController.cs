using Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using UnityEngine.UI;
// using Unity.VisualScripting;

namespace PTB.Menus
{
    public class MainMenuController : Singleton<MainMenuController>
    {
        [Header("Host Game")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private Slider _hostGamePlayerCountSlider; // TODO: Find a better way to do thiss

        [Header("Join Game")]
        [SerializeField] private GameObject _joinGameMenu;

        [Header("Options")]
        [SerializeField] private GameObject _optionsMenu;

        private MenuState currentMenuState = MenuState.MainMenu;

        private void Start()
        {
            if (_hostGameMenu != null) _hostGameMenu.SetActive(false);
            if (_joinGameMenu != null) _joinGameMenu.SetActive(false);
            if (_optionsMenu != null) _optionsMenu.SetActive(false);

        }

        #region Menus
        private void HostGame()
        {
            if (_debug) SceneManager.LoadScene("Test Scene");
            _eventManager.OnHostGame?.Invoke();
            _hostGameMenu?.SetActive(true);
            currentMenuState = MenuState.HostGame;

        }

        public void CreateLobby()
        {
            if (_hostGamePlayerCountSlider == null) { Debug.LogError($"Slider is null!"); return; }
            _eventManager.OnCreateLobby?.Invoke((int)_hostGamePlayerCountSlider.value, true);

        }

        private void JoinGame()
        {
            _eventManager.OnJoinGame();
            currentMenuState = MenuState.JoinGame;
            Debug.LogWarning($"Not implemented!");

        }

        private void Options()
        {
            currentMenuState = MenuState.Options;
            Debug.LogWarning($"Not implemented!");

        }
        #endregion

        #region Utility
        // INFO: Prevent switching to null UI
        private void HandleMenuSwitching(GameObject menuToSwitchTo, MenuState menuStateToSwitchTo)
        {
            if (menuToSwitchTo == null) { Debug.LogWarning($"The provided menu is null"); return; }
            menuToSwitchTo.SetActive(true);
            currentMenuState = menuStateToSwitchTo;

        }

        public void BackButton()
        {
            switch (currentMenuState)
            {
                case MenuState.HostGame:
                    HandleMenuSwitching(_hostGameMenu, MenuState.HostGame);
                    break;
            }

        }
        #endregion

        #region Rage Quitting
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
        #endregion
    }

    // INFO: Menu state
    public enum MenuState
    {
        MainMenu,
        HostGame,
        JoinGame,
        Options,
    }

}