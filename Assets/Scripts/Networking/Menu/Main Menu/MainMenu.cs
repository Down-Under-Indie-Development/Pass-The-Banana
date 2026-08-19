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
        private UnityNetworkHelper _unityNetworkHelper => UnityNetworkHelper.Instance;

        [Header("Sub Menus")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private GameObject _joinGameMenu;
        [SerializeField] private GameObject _optionsMenu;
        [SerializeField] private GameObject _lobbyScreen;

        private GameState _localCurrentGameSate = GameState.MainMenu;

        [Header("Debugging")]
        [SerializeField] private TextMeshProUGUI _debugTXT;

        #region Events
        private void OnEnable()
        {
            _eventManager.OnStartUnityClient += ClientConnected;
            _unityNetworkHelper.sessionStateManager.currentSessionState.OnValueChanged += HandleSessionStateChange;

        }

        private void OnDisable()
        {
            _eventManager.OnStartUnityClient -= ClientConnected;
            _unityNetworkHelper.sessionStateManager.currentSessionState.OnValueChanged -= HandleSessionStateChange;
            CloseMenu();


        }

        private void HandleSessionStateChange(GameState previousValue, GameState newValue)
        {
            _localCurrentGameSate = newValue;
            // Debug.Log($"{_localCurrentGameSate}");

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
            if (_debugTXT != null) _debugTXT.text = $"{_unityNetworkHelper.sessionStateManager.currentSessionState.Value}";

        }

        #region Sub Menus
        public void HostGame()
        {
            if (_hostGameMenu == null) { Debug.LogWarning($"Host game menu is null!"); return; }
            HandleMenuSwitching(null, GameState.HostGame);
            _hostGameMenu.SetActive(true);

        }


        public void CreateLobby()
        {
            Slider playerCountSlider = _hostGameMenu.gameObject.GetComponentInChildren<Slider>();
            if (playerCountSlider == null) { Debug.LogError($"Host game menu needs a slider for player count!"); return; }
            if (_unityNetworkHelper.networkManager.NetworkConfig.NetworkTransport is UnityTransport)
            {
                Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> Bypassing Facepunch transport, starting host!");
                _eventManager.OnSteamHostConnect?.Invoke();
                return;

            }

            _eventManager.OnCreateLobbyRequest?.Invoke((int)playerCountSlider.value);

        }

        public void JoinGame()
        {
            if (_joinGameMenu == null) { Debug.LogWarning($"Join game menu is null!"); return; }
            _joinGameMenu.SetActive(true);

        }

        public void Options()
        {
            Debug.LogWarning($"Not implemented!");

        }
        #endregion

        private void ClientConnected()
        {
            ResetMenu();
            _lobbyScreen?.SetActive(true);
            _unityNetworkHelper.sessionStateManager.UpdateSessionState(GameState.Lobby);


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