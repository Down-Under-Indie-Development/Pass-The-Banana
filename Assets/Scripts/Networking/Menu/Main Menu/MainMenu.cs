using Utility;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using PTB.Networking.Menus.Interfaces;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Steamworks.Data;

namespace PTB.Networking.Menus
{
    public class MainMenu : Singleton<MainMenu>, IMenu
    {

        // INFO: Networking Components
        // private UnityNetworkHelper _unityNetworkHelper => UnityNetworkHelper.Instance;
        private BootstrapManager _bootstrapManager => BootstrapManager.Instance;

        [Header("Sub Menus")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private GameObject _joinGameMenu;
        [SerializeField] private GameObject _optionsMenu;
        [SerializeField] public GameObject _lobbyScreen;

        private GameState _localCurrentGameSate = GameState.MainMenu;

        [Header("Debugging")]
        [SerializeField] private TextMeshProUGUI _debugTXT;

        #region Events
        private void OnEnable()
        {
            _eventManager.OnStartUnityHost += LobbyCreated;

        }

        private void OnDisable()
        {
            _eventManager.OnStartUnityHost -= LobbyCreated;
            CloseMenu();


        }
        #endregion

        private void Start()
        {
            if (_hostGameMenu != null) _hostGameMenu.SetActive(false);
            if (_lobbyScreen != null) _lobbyScreen.SetActive(false);
            if (_joinGameMenu != null) _joinGameMenu.SetActive(false);
            if (_optionsMenu != null) _optionsMenu.SetActive(false);

        }

        private void Update()
        {
            if (_debugTXT != null && _bootstrapManager.sessionStateManager)
                _debugTXT.text = $"{_bootstrapManager.sessionStateManager.currentSessionState.Value}";

        }

        #region Sub Menus
        public void HostGame()
        {
            if (_hostGameMenu == null) { Debug.LogWarning($"Host game menu is null!"); return; }
            HandleMenuSwitching(null, GameState.HostGame);
            _hostGameMenu.SetActive(true);

        }

        public void JoinGame()
        {
            if (_joinGameMenu == null) { Debug.LogWarning($"Join game menu is null!"); return; }
            _joinGameMenu.SetActive(true);
            _localCurrentGameSate = GameState.JoinGame;

        }

        public void Options()
        {
            Debug.LogWarning($"Not implemented!");

        }
        #endregion

        // INFO: Lobby Created, lets go!
        private void LobbyCreated()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            _bootstrapManager.sessionStateManager.UpdateSessionState(GameState.Lobby);
            BootstrapNetworkManager.Instance.ChangeNetworkScene(_bootstrapManager.lobbyScene, _bootstrapManager.mainMenuScene);

        }

        public void OpenMenu() { ResetMenu(); }
        public void CloseMenu()
        {
            ResetMenu();
        }

        public void ResetMenu()
        {
            _lobbyScreen?.SetActive(false);
            _joinGameMenu?.SetActive(false);
            _hostGameMenu?.SetActive(false);
        }

        public void Refresh() => ResetMenu();

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
                    HandleMenuSwitching(_lobbyScreen, GameState.MainMenu);
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