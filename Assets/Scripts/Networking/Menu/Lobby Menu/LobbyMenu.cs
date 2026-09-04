using Utility;
using UnityEngine;
using Steamworks.Data;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using PTB.Networking.Menus.Interfaces;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace PTB.Networking.Menus
{
    public class LobbyUIManager : NetworkBehaviour, IMenu
    {
        private EventManager _eventManager => EventManager.Instance;


        [Header("Player Panel")]
        [SerializeField] private GameObject _playerPanelContentGO;
        [SerializeField] private GameObject _playerInfoPanelPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _startGameBTN;

        [Header("Lobby Info Text")]
        [SerializeField] private TextMeshProUGUI _roundsTxt;
        [SerializeField] private TextMeshProUGUI _lobbyCodeTxt;

        private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
        private SteamManager _steamManager => SteamManager.Instance;

        #region Events
        private void OnEnable()
        {
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            NetworkManager.Singleton.OnConnectionEvent += OnUnityClientDisconnect;

        }

        private void OnDisable()
        {
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnConnectionEvent -= OnUnityClientDisconnect;

        }
        #endregion

        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer && _startGameBTN != null) _startGameBTN.interactable = false;
            Refresh();

        }


        #region IMenu Components
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
            RefreshUI();
        }
        #endregion

        #region Steamworks

        private void OnLobbyEntered(Lobby lobby)
        {
            RefreshUI();

        }
        #endregion

        #region Networking
        #region Get Lobby Info
        [Rpc(SendTo.Server)]
        private void AskForLobbyInfoRPC()
        {
            string code = "Code: ";
            string lobby = _steamManager.myLobby.HasValue ? _steamManager.myLobby.Value.Id.ToString() : "UNITY (NO CODE)";
            string lobbyCode = code + lobby;
            string numberOfRounds = $"ROUND 1 OF {BootstrapNetworkManager.Instance.lobbyData.numberOfRounds}";

            TellLobbyInfoRPC(lobbyCode, numberOfRounds);

        }

        [Rpc(SendTo.ClientsAndHost)]
        // INFO: Tell the client the lobby info
        private void TellLobbyInfoRPC(string lobbyCode, string numberOfRounds)
        {
            // GUARD: Prevent unnecessary refresh
            if (_lobbyCodeTxt.text == lobbyCode || _roundsTxt.text == numberOfRounds) return;

            if (_lobbyCodeTxt != null) _lobbyCodeTxt.text = lobbyCode;
            if (_roundsTxt != null) _roundsTxt.text = numberOfRounds;

        }

        #endregion

        #region Get Player List
        [Rpc(SendTo.Server)]
        private void AskForPlayerListRPC()
        {
            ClearPlayerPanel();

            switch (BootstrapManager.Instance.selectedTransport)
            {
                case BootstrapManager.Transport.Facepunch:
                    TellSteamPlayerListRPC();
                    break;

                case BootstrapManager.Transport.Unity:
                    TellUnityPlayerListRPC();
                    break;
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TellSteamPlayerListRPC()
        {
            if (!_steamManager.myLobby.HasValue) { Debug.LogError($"Steam lobby doesn't have a value, can't show player list!"); return; }

            Lobby lobby = _steamManager.myLobby.Value;
            foreach (Friend member in lobby.Members)
            {
                // INFO: Set Display
                bool isHost = lobby.Owner.Id == member.Id;
                CreatePlayerCard($"{member.Name}", isHost, $"{-1}ms");

            }
        }

        #region Debugging
        [Rpc(SendTo.ClientsAndHost)]
        private void TellUnityPlayerListRPC()
        {

            // Display all connected members
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                bool isHost = clientId == 0;

                // INFO: Set Display
                CreatePlayerCard($"{clientId}", isHost, $"{0}ms");
            }
        }
        #endregion

        private void OnUnityClientDisconnect(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected) return;

            if (connectionEventData.ClientId == networkManager.LocalClientId)
                return;

            if (!IsServer) return;
            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> {connectionEventData.ClientId} has left!");
            Refresh();

        }


        #endregion

        #endregion

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

        #region Buttons
        public void StartGame()
        {
            BootstrapManager bootstrapManager = BootstrapManager.Instance;
            if (NetworkManager.Singleton.ConnectedClients.Count < bootstrapManager.minimumPlayers && bootstrapManager.selectedTransport == BootstrapManager.Transport.Facepunch) { Debug.LogWarning($"Need {bootstrapManager.minimumPlayers} players to start"); return; }
            Debug.Log($"{_networkHelper.CheckPrivilege()} Started the game!");
            BootstrapNetworkManager.Instance.ChangeNetworkScene(bootstrapManager.gameplayScenes[0], bootstrapManager.lobbyScene);
        }

        public void LeaveGame()
        {
            if (_steamManager.connectedToSteam) { _eventManager.OnSteamClientDisconnect?.Invoke(); return; }

            // !! Unity handling
            _eventManager.OnStopUnityClient?.Invoke();

        }

        #endregion

        #region Utility
        private void ClearPlayerPanel()
        {
            foreach (Transform child in _playerPanelContentGO.transform)
                Destroy(child.gameObject);

        }

        private void RefreshUI()
        {
            // GUARD: Prevent Nulls
            if (_playerPanelContentGO == null) { Debug.LogError($"Player panel content is null!"); return; }
            if (_playerInfoPanelPrefab == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }

            AskForLobbyInfoRPC();
            AskForPlayerListRPC();

        }
        #endregion

    }
}