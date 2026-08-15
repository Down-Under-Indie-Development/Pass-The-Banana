using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class SteamLobbyManager : PersistentSingleton<SteamLobbyManager>
    {
        [Header("Steam Settings")]
        [SerializeField] private uint _appId = 480;

        [Header("UI")]
        [SerializeField] private GameObject lobbyScreen;

        public Lobby? currentLobby { get; private set; }
        private FacepunchTransport _networkTransport;
        private NetworkHelper _networkHelper => NetworkHelper.Instance;
        private SteamManager _steamManager => SteamManager.Instance;

        [Header("Testing")]
        [SerializeField] private List<string> _devSteamId = new();

        protected override void Awake()
        {
            base.Awake();
            _networkTransport = GetComponent<FacepunchTransport>();
            EstablishUnitySteamConnection();

        }

        #region Events
        private void OnEnable()
        {
            // INFO: Host
            _eventManager.OnCreateLobbyRequest += StartSteamServer;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;

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
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;

            // INFO: Client
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;

        }

        #endregion

        private void EstablishUnitySteamConnection()
        {
            _steamManager.EstablishClientSteamConnection();
            _networkTransport.steamAppId = _appId;
            if (_steamManager.connectedToSteam) return;

            // INFO: Not Connected
            Debug.LogWarning($"Not connected to steam, disabling {name}");
            gameObject.SetActive(false);

        }

        // INFO: DO NOT EDIT!
        #region Steamworks
        #region Create Server
        private async void StartSteamServer(int playerCount)
        {
            if (currentLobby != null) { Debug.LogWarning($"Lobby is already created!"); return; }

            _eventManager.OnStartUnityHost?.Invoke();
            Debug.Log($"{_networkHelper.CheckPrivilege()} Lobby request received creating lobby!");
            await SteamMatchmaking.CreateLobbyAsync(playerCount);

        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK) { Debug.LogWarning($"Error creating lobby!"); return; }

            lobby.SetJoinable(true);
            lobby.SetPrivate();
            lobby.SetGameServer(lobby.Owner.Id);
            Debug.Log($"{_networkHelper.CheckPrivilege()} Lobby created! | {lobby.Owner.Name} ({lobby.Id}) | {lobby.MemberCount}/{lobby.MaxMembers}");

        }

        #endregion

        #region Invite Player
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

        #region Player Joining/Joined
        private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} is joining!");

            if (!_debug && !_devSteamId.Contains(friend.Id.ToString()))
            {
                Debug.Log($"{friend.Name} was kicked as they're not on the list!");
                DisconnectPlayer();

            }


            currentLobby = lobby;

        }

        private void OnLobbyEntered(Lobby lobby)
        {
            currentLobby = lobby;
            _networkTransport.targetSteamId = lobby.Owner.Id;
            SteamFriends.SetRichPresence("connect", currentLobby.Value.Id.ToString());

            if (_networkHelper.networkManager.IsHost) { OnSteamHostLobbyEnter(); return; }
            OnClientEnterLobby(lobby);




        }
        #endregion

        #region Player Left/Disconnected
        private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} left!");
            currentLobby = null;
            if (friend.Id == lobby.Owner.Id || _networkHelper.networkManager.IsHost) OnSteamHostLobbyLeave();
            // DisconnectPlayer();

        }

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} disconnected!");
            currentLobby = null;
            if (friend.Id == lobby.Owner.Id || _networkHelper.networkManager.IsHost) OnSteamHostLobbyLeave();
            // DisconnectPlayer();


        }
        #endregion
        #endregion

        #region Host
        protected virtual void OnSteamHostLobbyEnter()
        {

        }

        protected virtual void OnSteamHostLobbyLeave()
        {
            _eventManager.OnUnityHostDisconnect?.Invoke();
        }
        #endregion

        #region Client
        private void StartUnityClient()
        {
            _eventManager.OnStartUnityClient?.Invoke();

        }

        protected virtual void OnClientEnterLobby(Lobby lobby)
        {
            // INFO: Client
            Debug.Log($"You entered {lobby.Owner.Name}'s lobby");
            StartUnityClient();
            lobbyScreen?.SetActive(true);

        }
        #endregion

        public virtual void DisconnectPlayer()
        {
            if (!SteamManager.Instance.connectedToSteam) return;

            // bool isSelf = friend.Id == 0 || friend.Id == SteamClient.SteamId;
            bool leaverWasHost = _networkHelper.networkManager.IsHost;

            SteamFriends.SetRichPresence("connect", null);
            currentLobby?.Leave();
            currentLobby = null;

            _eventManager.OnUnityClientDisconnect?.Invoke();

            if (leaverWasHost)
                _eventManager.OnUnityHostDisconnect?.Invoke();
        }

    }
}