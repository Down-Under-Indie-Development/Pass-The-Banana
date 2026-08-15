using Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using UnityEngine.UI;
using PTB.Networking;
using Unity.Netcode;
// using Unity.VisualScripting;

namespace PTB.Menus
{
    public class MainMenuController : Singleton<MainMenuController>
    {
        [Header("Host Game")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private Slider _hostGamePlayerCountSlider; // TODO: Find a better way to do this
        [SerializeField] private GameObject _lobbyScreen;

        [Header("Join Game")]
        [SerializeField] private GameObject _joinGameMenu;

        [Header("Options")]
        [SerializeField] private GameObject _optionsMenu;

        [Header("Game Settings")]
        [field: SerializeField] public int minimumPlayers { get; private set; } = 2;

        private GameState _currentGameState = GameState.MainMenu;

        #region Events
        private void OnEnable()
        {
            _eventManager.OnSteamClientConnect += OnSteamClientConnect;
        }

        private void OnDisable()
        {
            _eventManager.OnSteamClientConnect -= OnSteamClientConnect;

        }
        #endregion

        private void Start()
        {
            if (_hostGameMenu != null) _hostGameMenu.SetActive(false);
            if (_lobbyScreen != null) _lobbyScreen.SetActive(false);
            if (_joinGameMenu != null) _joinGameMenu.SetActive(false);
            if (_optionsMenu != null) _optionsMenu.SetActive(false);
            SceneManager.LoadScene("Networking Scene", LoadSceneMode.Additive);

        }

        #region Menus
        public void HostGame()
        {
            if (_hostGameMenu == null) { Debug.LogWarning($"Host game menu is null!"); return; }
            _hostGameMenu?.SetActive(true);
            _eventManager.OnHostGame?.Invoke();
            _currentGameState = GameState.HostGame;

        }

        public void CreateLobby()
        {
            if (_hostGamePlayerCountSlider == null) { Debug.LogError($"Slider is null!"); return; }
            _eventManager.OnCreateLobbyRequest?.Invoke((int)_hostGamePlayerCountSlider.value);
            _currentGameState = GameState.Lobby;

        }

        public void JoinGame()
        {
            if (_joinGameMenu == null) { Debug.LogWarning($"Join game menu is null!"); return; }
            _joinGameMenu.SetActive(true);
            _eventManager.OnJoinGame?.Invoke();
            _currentGameState = GameState.JoinGame;

        }

        public void Options()
        {
            _currentGameState = GameState.Options;
            Debug.LogWarning($"Not implemented!");

        }
        #endregion

        private void OnSteamClientConnect()
        {
            _lobbyScreen?.SetActive(true);
            _joinGameMenu?.SetActive(false);

        }

        #region Utility
        // INFO: Prevent switching to null UI
        private void HandleMenuSwitching(GameObject menuToDisable, GameState menuStateToSwitchTo)
        {
            if (menuStateToSwitchTo == _currentGameState) { Debug.LogWarning($"Already on this state, enabling object!"); }
            if (menuToDisable == null) { Debug.LogWarning($"The provided menu is null"); return; }
            menuToDisable.SetActive(false);
            _currentGameState = menuStateToSwitchTo;

        }

        public void BackButton()
        {
            switch (_currentGameState)
            {
                case GameState.HostGame:
                    HandleMenuSwitching(_hostGameMenu, GameState.MainMenu);
                    break;
                case GameState.Lobby:
                    SteamLobbyManager.Instance.DisconnectPlayer();
                    break;
                case GameState.JoinGame:
                    HandleMenuSwitching(_joinGameMenu, GameState.MainMenu);
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



}