using Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using UnityEngine.UI;
using PTB.Networking;
using Unity.Netcode;
using TMPro;
// using Unity.VisualScripting;

namespace PTB.Menus
{
    public class MainMenuController : Singleton<MainMenuController>
    {

        // INFO: Networking Components
        private UnityNetworkHelper _unityNetworkHelper => UnityNetworkHelper.Instance;

        [Header("Host Game Menu")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private Slider _hostGamePlayerCountSlider; // TODO: Find a better way to do this

        [Header("Lobby Menu")]
        [SerializeField] private GameObject _lobbyScreen;

        [Header("Join Game Menu")]
        [SerializeField] private GameObject _joinGameMenu;

        [Header("Options Menu")]
        [SerializeField] private GameObject _optionsMenu;

        private GameState _localCurrentGameSate = GameState.MainMenu;

        [Header("Debugging")]
        [SerializeField] private TextMeshProUGUI _debugTXT;

        #region Events
        private void OnEnable()
        {
            _eventManager.OnStartUnityClient += OnStartUnityClient;
            _unityNetworkHelper.sessionStateManager.currentSessionState.OnValueChanged += HandleSessionStateChange;
            _eventManager.OnUnityClientDisconnected += ResetMenu;

        }

        private void OnDisable()
        {
            _eventManager.OnStartUnityClient -= OnStartUnityClient;
            _unityNetworkHelper.sessionStateManager.currentSessionState.OnValueChanged -= HandleSessionStateChange;
            _eventManager.OnUnityClientDisconnected -= ResetMenu;

        }

        private void HandleSessionStateChange(GameState previousValue, GameState newValue)
        {
            _localCurrentGameSate = newValue;
            Debug.Log($"{_localCurrentGameSate}");

        }
        #endregion

        private void Start()
        {
            if (_hostGameMenu != null) _hostGameMenu.SetActive(false);
            if (_lobbyScreen != null) _lobbyScreen.SetActive(false);
            if (_joinGameMenu != null) _joinGameMenu.SetActive(false);
            if (_optionsMenu != null) _optionsMenu.SetActive(false);

        }

        private void ResetMenu()
        {
            Start();
            _localCurrentGameSate = GameState.MainMenu;


        }

        private void LateUpdate()
        {
            if (_debugTXT != null) _debugTXT.text = $"{_unityNetworkHelper.sessionStateManager.currentSessionState.Value}";

        }

        #region Menus
        public void HostGame()
        {
            if (_hostGameMenu == null) { Debug.LogWarning($"Host game menu is null!"); return; }
            HandleMenuSwitching(null, GameState.HostGame);
            _hostGameMenu.SetActive(true);

        }


        public void CreateLobby()
        {
            if (_hostGamePlayerCountSlider == null) { Debug.LogError($"Slider is null!"); return; }
            _eventManager.OnCreateLobbyRequest?.Invoke((int)_hostGamePlayerCountSlider.value);


        }

        public void JoinGame()
        {
            if (_joinGameMenu == null) { Debug.LogWarning($"Join game menu is null!"); return; }
            _joinGameMenu.SetActive(true);
            _localCurrentGameSate = GameState.JoinGame;


        }

        public void Options()
        {
            _localCurrentGameSate = GameState.Options;
            Debug.LogWarning($"Not implemented!");

        }
        #endregion

        private void OnStartUnityClient()
        {
            _joinGameMenu?.SetActive(false);
            _hostGameMenu?.SetActive(false);
            _lobbyScreen?.SetActive(true);
            if (NetworkManager.Singleton.IsServer) _unityNetworkHelper.sessionStateManager.UpdateSessionState(GameState.Lobby);
            _localCurrentGameSate = _unityNetworkHelper.sessionStateManager.currentSessionState.Value;


        }

        #region Utility
        // INFO: Prevent switching to null UI
        private void HandleMenuSwitching(GameObject menuToDisable, GameState menuStateToSwitchTo)
        {
            if (menuStateToSwitchTo == _localCurrentGameSate) { Debug.LogWarning($"Already on this state, enabling object!"); }
            if (menuToDisable != null) menuToDisable.SetActive(false);
            _localCurrentGameSate = menuStateToSwitchTo;

        }

        public void BackButton()
        {
            switch (_localCurrentGameSate)
            {
                case GameState.HostGame:
                    HandleMenuSwitching(_hostGameMenu, GameState.MainMenu);
                    HandleMenuSwitching(_lobbyScreen, GameState.MainMenu);
                    break;
                case GameState.Lobby:

                    _eventManager.OnSteamClientDisconnect?.Invoke();
                    _lobbyScreen.SetActive(false);
                    break;
                case GameState.JoinGame:
                    HandleMenuSwitching(_joinGameMenu, GameState.MainMenu);
                    break;
                default:
                    Debug.LogWarning($"Don't have logic for Game State: {_localCurrentGameSate}");
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