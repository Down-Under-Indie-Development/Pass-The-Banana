using Utility;
using UnityEngine;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using PTB.Networking.Menus.Interfaces;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PTB.Networking.Menus
{
    public class LobbyMenu : CustomMonoBehaviour, IMenu
    {
        [SerializeField] private TextMeshProUGUI _roundsText;

        [Header("Player Panel")]
        [SerializeField] private GameObject _playerPanelContentGO;
        [SerializeField] private GameObject _playerInfoPanelPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _startGameBTN;

        [SerializeField] private TextMeshProUGUI _lobbyCodeTxt;

        private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
        private SteamManager _steamManager => SteamManager.Instance;


        private Queue<SteamId> _connectedMembers = new();

        #region Events
        private void OnEnable()
        {
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            _eventManager.OnUnityClientDisconnected += ResetMenu;

            if (_networkHelper.networkManager != null && !_networkHelper.networkManager.IsHost && _startGameBTN != null) _startGameBTN.interactable = false;
            UpdateLobbyScreenText();

        }

        private void OnDisable()
        {
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            _eventManager.OnUnityClientDisconnected -= ResetMenu;
            CloseMenu();


        }
        #endregion

        private void LateUpdate()
        {
            Refresh();
        }


        #region Steamworks

        private void OnLobbyEntered(Lobby lobby)
        {
            UpdateLobbyScreenText();
            RefreshUI(lobby);

        }

        private void UpdateLobbyScreenText()
        {
            if (_lobbyCodeTxt != null && _steamManager.myLobby.HasValue) _lobbyCodeTxt.text = $"Code: {_steamManager.myLobby.Value.Id}";
            if (_roundsText != null && BootstrapNetworkManager.Instance.lobbyData != null) _roundsText.text = $"ROUND 1 OF {BootstrapNetworkManager.Instance.lobbyData.numberOfRounds}";

        }

        #endregion

        public void OpenMenu() { ResetMenu(); }
        public void CloseMenu()
        {
            ResetMenu();
        }

        public void ResetMenu()
        {
            _lobbyCodeTxt.text = "";
            ClearPlayerPanel();

        }

        public void Refresh()
        {
            if (_steamManager.myLobby.HasValue && BootstrapManager.Instance.selectedTransport == BootstrapManager.Transport.Facepunch) { RefreshUI(_steamManager.myLobby.Value); return; }
            RefreshUI();

        }

        private void ClearPlayerPanel()
        {
            for (int i = 0; i < _playerPanelContentGO.transform.childCount; i++)
                Destroy(_playerPanelContentGO.transform.GetChild(i).gameObject);

        }

        private void RefreshUI(Lobby? lobby = null)
        {
            // GUARD: Prevent Nulls
            if (_playerPanelContentGO == null) { Debug.LogError($"Player panel content is null!"); return; }
            if (_playerInfoPanelPrefab == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }

            ClearPlayerPanel();
            if (_lobbyCodeTxt != null) UpdateLobbyScreenText();

            // DEBUG: Check for Unity Transport
            if (BootstrapManager.Instance.selectedTransport == BootstrapManager.Transport.Unity) { GetUnityPlayerList(); return; }

            if (!lobby.HasValue) return;

            foreach (Friend member in lobby.Value.Members)
            {
                // INFO: Set Display
                bool isHost = lobby.Value.Owner.Id == member.Id;
                CreatePlayerCard($"{member.Name}", isHost, $"{-1}ms");

            }
        }

        // INFO: Client
        private PlayerUIInfo CreatePlayerCard(string playerName, bool host, string playerPing)
        {
            GameObject playerInfoGO = Instantiate(_playerInfoPanelPrefab);
            playerInfoGO.transform.SetParent(_playerPanelContentGO.transform, false);

            // INFO: Set Display
            PlayerUIInfo playerInfo = playerInfoGO.GetComponent<PlayerUIInfo>();
            playerInfo.playerName = playerInfo.playerName = $"{playerName} {(host ? "[HOST]" : "")}";
            playerInfo.playerPing = playerPing;

            return playerInfo;
        }

        #region Debugging
        // INFO: Client Side
        private void GetUnityPlayerList()
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                bool isHost = client.ClientId == 0;

                // INFO: Set Display
                CreatePlayerCard($"{client.ClientId}", isHost, $"{0}ms");

            }
        }
        #endregion

        #region Buttons
        public void StartGame()
        {
            BootstrapManager bootstrapManager = BootstrapManager.Instance;
            // MainMenuController _mainMenuController = MainMenuController.Instance;
            if (_connectedMembers.Count < SteamManager.Instance.minimumPlayers && !_debug) { Debug.LogWarning($"Need {SteamManager.Instance.minimumPlayers} players to start"); return; }
            Debug.Log($"{_networkHelper.CheckPrivilege()} Started the game!");
            BootstrapNetworkManager.Instance.ChangeNetworkScene(bootstrapManager.gameplayScenes[0], bootstrapManager.mainMenuScene);

        }
        #endregion

    }
}