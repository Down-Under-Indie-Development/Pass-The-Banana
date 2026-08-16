using System;
using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using PTB.Menus;
using Steamworks;
using Steamworks.Data;
using UnityEditor;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class SteamLobbyManager : PersistentSingleton<SteamLobbyManager>
    {
        [Header("Steam Settings")]
        [SerializeField] private uint _appId = 480;

        [Header("Lobby Settings")]
        [field: SerializeField] public int minimumPlayers { get; private set; } = 2;

        public Lobby? currentLobby { get; private set; } = null;
        private FacepunchTransport _networkTransport;
        private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
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

            Debug.Log($"{_networkHelper.CheckPrivilege()} Lobby request received creating lobby!");
            _eventManager.OnStartUnityHost?.Invoke();
            await SteamMatchmaking.CreateLobbyAsync(playerCount);

        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK) { Debug.LogWarning($"Error creating lobby!"); return; }

            lobby.SetJoinable(true);
            lobby.SetPrivate();
            lobby.SetGameServer(lobby.Owner.Id);
            Debug.Log($"{_networkHelper.CheckPrivilege()} Lobby created! | {lobby.Owner.Name} ({lobby.Id}) | {lobby.MemberCount}/{lobby.MaxMembers}");
            GUIUtility.systemCopyBuffer = lobby.Id.ToString(); // INFO: Copies lobby code to peoples keyboard

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

        }


        private void OnLobbyEntered(Lobby lobby)
        {
            currentLobby = lobby;

            // GUARD: self-check whitelist before doing anything else
            if (!_debug && !_devSteamId.Contains(SteamClient.SteamId.ToString()) && SteamClient.SteamId != lobby.Owner.Id)
            {
                Debug.LogWarning($"You're not whitelisted, leaving lobby.");
                DisconnectPlayer();
                return;
            }

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
            if (friend.Id == lobby.Owner.Id) { OnSteamHostLobbyLeave(); return; }
            _eventManager.OnUnityClientDisconnect?.Invoke();

        }

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} disconnected!");
            currentLobby = null;
            if (friend.Id == lobby.Owner.Id) { OnSteamHostLobbyLeave(); return; }
            _eventManager.OnUnityClientDisconnect?.Invoke();

        }
        #endregion
        #endregion

        #region Host
        protected virtual void OnSteamHostLobbyEnter()
        {
            StartUnityClient();
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
            _eventManager.OnSteamClientConnect?.Invoke();
            StartUnityClient();
            Debug.Log($"You entered {lobby.Owner.Name}'s lobby");

        }
        #endregion

        public virtual void DisconnectPlayer()
        {
            if (!SteamManager.Instance.connectedToSteam) return;
            if (currentLobby == null) return;

            SteamFriends.SetRichPresence("connect", null);
            currentLobby?.Leave();
            OnSteamHostLobbyLeave();

        }

    }
}