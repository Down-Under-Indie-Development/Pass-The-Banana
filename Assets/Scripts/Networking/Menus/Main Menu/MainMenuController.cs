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
        [Header("Host Game Menu")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private Slider _hostGamePlayerCountSlider; // TODO: Find a better way to do this

        [Header("Lobby Menu")]
        [SerializeField] private GameObject _lobbyScreen;

        [Header("Join Game Menu")]
        [SerializeField] private GameObject _joinGameMenu;

        [Header("Options Menu")]
        [SerializeField] private GameObject _optionsMenu;

        // INFO: Steam lobby settings
        public int minimumPlayers => SteamManager.Instance.minimumPlayers;

        public GameState currentGameState = GameState.MainMenu;

        #region Events
        private void OnEnable()
        {
            _eventManager.OnStartUnityClient += OnSteamClientConnect;
        }

        private void OnDisable()
        {
            _eventManager.OnStartUnityClient -= OnSteamClientConnect;


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
            currentGameState = GameState.HostGame;

        }

        public void CreateLobby()
        {
            if (_hostGamePlayerCountSlider == null) { Debug.LogError($"Slider is null!"); return; }
            // currentGameState = GameState.Lobby;
            _eventManager.OnCreateLobbyRequest?.Invoke((int)_hostGamePlayerCountSlider.value);

        }

        public void JoinGame()
        {
            if (_joinGameMenu == null) { Debug.LogWarning($"Join game menu is null!"); return; }
            currentGameState = GameState.JoinGame;
            _joinGameMenu.SetActive(true);
            _eventManager.OnJoinGame?.Invoke();


        }

        public void Options()
        {
            currentGameState = GameState.Options;
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
            if (menuStateToSwitchTo == currentGameState) { Debug.LogWarning($"Already on this state, enabling object!"); }
            if (menuToDisable == null) { Debug.LogWarning($"The provided menu is null"); return; }
            menuToDisable.SetActive(false);
            currentGameState = menuStateToSwitchTo;

        }

        public void BackButton()
        {
            switch (currentGameState)
            {
                case GameState.HostGame:
                    HandleMenuSwitching(_hostGameMenu, GameState.MainMenu);
                    break;
                case GameState.Lobby:
                    _eventManager.OnSteamClientDisconnect?.Invoke();
                    break;
                case GameState.JoinGame:
                    HandleMenuSwitching(_joinGameMenu, GameState.MainMenu);
                    break;
                default:
                    Debug.LogWarning($"Don't have logic for Game State: {_gameManager.currentGameState}");
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