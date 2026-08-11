using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class LobbyManager : Singleton<LobbyManager>
    {
        private Lobby? currentLobby;
        private FacepunchTransport _networkTransport = null;
        private NetworkManager _networkManager;

        [Header("Testing")]
        [SerializeField] private List<string> _devNames = new();

        #region Events
        private void OnEnable()
        {
            // INFO: Host
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

            // INFO: Client
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;

            _eventManager.OnCreateLobby += StartHost;


        }

        private void OnDisable()
        {
            // INFO: Host
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

            // INFO: Client
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;

            _eventManager.OnCreateLobby -= StartHost;

            if (_networkManager == null) return;
            _networkManager.OnServerStarted -= OnServerStarted;
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;

        }

        #endregion

        private void Start()
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogWarning($"Not connected to steam, disabling {name}");
                gameObject.SetActive(false);
                return;

            }

            _networkTransport.GetComponent<FacepunchTransport>();
            _networkManager = NetworkManager.Singleton;

        }

        #region Netcode
        #region Host
        private async void StartHost(int playerCount)
        {
            _networkManager.OnServerStarted += OnServerStarted;
            _networkManager.StartHost();

            Debug.Log($"[HOST] Lobby request received creating lobby!");
            Debug.Log($"Creating lobby for {playerCount} player(s)");
            currentLobby = await SteamMatchmaking.CreateLobbyAsync(playerCount);
            // if (!friendsOnly) currentLobby?.SetPublic();
            if (_debug) Test();

        }

        private void OnServerStarted()
        {
            Debug.Log($"Server Started!");
        }
        #endregion

        #region  Client
        public void StartClient(SteamId steamId)
        {
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
            _networkTransport.targetSteamId = steamId;
            if (_networkManager.StartClient()) { Debug.Log($"Client has started"); }

        }

        private void OnClientDisconnect(ulong obj)
        {
            throw new NotImplementedException();
        }

        private void OnClientConnected(ulong obj)
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion

        #region Steamworks
        #region Host

        #region Create Lobby
        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK) { Debug.LogWarning($"Error creating lobby!"); return; }

            lobby.SetJoinable(true);
            lobby.SetGameServer(lobby.Owner.Id);
            Debug.Log($"Lobby created! | {lobby.Owner.Name}");


        }
        #endregion

        #region Join Lobby
        private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} has joined!");

        }

        #endregion
        #endregion

        #region Shared
        public void Disconnected()
        {
            currentLobby?.Leave();
            if (_networkManager == null) return;
            if (_networkManager.IsHost) _networkManager.OnServerStarted -= OnServerStarted;
            if (!_networkManager.IsHost)
            {
                _networkManager.OnClientConnectedCallback -= OnClientConnected;
                _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
            }

            _networkManager.Shutdown();
            Debug.Log($"Disconnected");
        }
        #endregion

        #region Client

        #region Invited to lobby
        private void OnLobbyInvite(Friend friend, Lobby lobby)
        {
            Debug.Log($"{friend.Name} was invited to {lobby.Id}");

        }

        private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
        {
            RoomEnter joinedLobby = await lobby.Join();
            if (joinedLobby != RoomEnter.Success) { Debug.LogError($"Failed to join {lobby}"); return; }
            currentLobby = lobby;
            Debug.Log($"{steamId} joined {lobby}");

        }

        private async void OnGameRichPresenceJoinRequested(Friend friend, string s)
        {
            if (!ulong.TryParse(s, out ulong seshID)) return;
            Lobby? joinedLobby = await SteamMatchmaking.JoinLobbyAsync(seshID);
            if (joinedLobby == null) { Debug.LogError($"Failed to join lobby!"); return; }

        }
        #endregion

        #region Lobby Entered 
        private void OnLobbyEntered(Lobby lobby)
        {
            if (_networkManager.IsHost) return;
            Debug.Log($"You entered {lobby}");
            StartClient(currentLobby.Value.Owner.Id);

        }
        #endregion



        #endregion
        #endregion

        private void OnApplicationQuit()
        {
            Disconnected();
        }

        #region Testing
        private void Test()
        {
            foreach (Friend friend in SteamFriends.GetFriends())
            {
                if (!_devNames.Contains(friend.Name)) continue;
                friend.InviteToGame(currentLobby.Value.Id.ToString());
                friend.SendMessage("This should work, accept it plz");
                // friend.SendMessage($"Hey I created another lobby, {lobbyID} (Don't use this code i'm still testing).");
                // Debug.Log($"{friend.Name}");

            }

        }
        #endregion


    }
}