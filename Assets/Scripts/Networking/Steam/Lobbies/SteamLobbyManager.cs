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
    public class SteamLobbyManager : PersistentSingleton<SteamLobbyManager>
    {
        [Header("Steam Settings")]
        [SerializeField] private uint _appId = 480;

        private Lobby? currentLobby;
        private FacepunchTransport _networkTransport;
        private NetworkHelper _networkHelper;


        [Header("Testing")]
        [SerializeField] private List<string> _devSteamId = new();

        #region Events
        private void OnEnable()
        {
            // INFO: Host
            _eventManager.OnCreateLobbyRequest += StartSteamServer;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

            // INFO: Client
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;



        }

        private void OnDisable()
        {
            // INFO: Host
            _eventManager.OnCreateLobbyRequest -= StartSteamServer;
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

            // INFO: Client
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;

        }

        #endregion

        private void Start()
        {
            _networkTransport = GetComponent<FacepunchTransport>();
            _networkHelper = NetworkHelper.Instance;
            EstablishSteamConnection();

        }

        private void EstablishSteamConnection()
        {
            _networkTransport.steamAppId = _appId;

            SteamManager.Instance.EstablishSteamConnection();

            if (!SteamClient.IsValid)
            {
                Debug.LogWarning($"Not connected to steam, disabling {name}");
                gameObject.SetActive(false);
                return;

            }

        }

        #region Steamworks
        #region Host
        private async void StartSteamServer(int playerCount)
        {
            _eventManager.OnStartHost?.Invoke();

            Debug.Log($"[HOST] Lobby request received creating lobby!");
            Debug.Log($"Creating lobby for {playerCount} player(s)");
            currentLobby = await SteamMatchmaking.CreateLobbyAsync(playerCount);
            // if (!friendsOnly) currentLobby?.SetPublic();
            if (_debug) Test();

        }

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

        #region  Client
        public void StartUnityClient(SteamId steamId)
        {
            _networkTransport.targetSteamId = steamId;
            _eventManager.OnStartClient?.Invoke();
        }

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
            currentLobby = joinedLobby;

        }

        #endregion

        #region Lobby Entered 
        private void OnLobbyEntered(Lobby lobby)
        {
            if (_networkHelper.networkManager.IsHost) return;
            Debug.Log($"You entered {lobby.Owner.Name}'s lobby");
            StartUnityClient(lobby.Owner.Id);

        }
        #endregion

        #region Shared
        public void Disconnected()
        {
            currentLobby?.Leave();
            _eventManager.OnClientDisconnect?.Invoke();

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
            if (!_debug) return;

            foreach (Friend friend in SteamFriends.GetFriends())
            {
                if (!_devSteamId.Contains(friend.Id.ToString())) continue;
                friend.InviteToGame(currentLobby.Value.Id.ToString());
                friend.SendMessage("This should work, accept it plz");
                // friend.SendMessage($"Hey I created another lobby, {lobbyID} (Don't use this code i'm still testing).");
                // Debug.Log($"{friend.Name}");

            }

        }
        #endregion


    }
}