using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Netcode.Transports.Facepunch;
using PTB.Menus;
using Steamworks;
using Steamworks.Data;
using UnityEditor;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class SteamManager : PersistentSingleton<SteamManager>
    {

        [Header("Steam Settings")]
        [field: SerializeField] public uint appID { get; protected set; } = 480;
        public bool connectedToSteam { get; protected set; }

        [Header("Lobby Settings")]
        [field: SerializeField] public int minimumPlayers { get; protected set; } = 2;

        public Lobby? currentLobby { get; protected set; } = null;


        #region Steamworks
        protected FacepunchTransport _networkTransport;
        protected bool IsHost => SteamClient.SteamId == currentLobby.Value.Owner.Id;
        #endregion

        protected override void Awake()
        {
            base.Awake();
            _networkTransport = GetComponent<FacepunchTransport>();
            EstablishSteamConnection();


        }

        #region Events
        private void OnEnable()
        {
            SubscribeToEvents();

        }

        private void OnDisable()
        {
            UnSubscribeToEvents();

        }


        private void SubscribeToEvents()
        {
            #region Host
            // INFO: Host
            _eventManager.OnCreateLobbyRequest += StartSteamServer;

            #region Steamworks
            // INFO: Steamworks
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
            #endregion
            #endregion

            #region Client
            // INFO: Client
            _eventManager.OnSteamClientDisconnect += DisconnectPlayer;

            #region Steamworks
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;
            #endregion
            #endregion

        }

        private void UnSubscribeToEvents()
        {
            #region Host
            // INFO: Host
            _eventManager.OnCreateLobbyRequest -= StartSteamServer;

            #region Steamworks
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            #endregion
            #endregion

            #region Client
            // INFO: Client
            _eventManager.OnSteamClientDisconnect -= DisconnectPlayer;
            #region Steamworks
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;
            #endregion
            #endregion

        }
        #endregion

        private void Start()
        {
            CheckSteamConnection();
        }


        #region Steam Connection
        #region Establish Connection
        // INFO: Establish connection to steam servers
        protected bool EstablishSteamConnection()
        {
            if (connectedToSteam) return true; // INFO: Prevent calling more than once

            try
            {
                SteamClient.Init(appID);
                connectedToSteam = true;
                Debug.Log($"{CheckPrivilege()} Successfully Connected to steam! | {SteamClient.Name} ({SteamClient.AppId})</color>");
                _networkTransport.steamAppId = appID;
                _eventManager.OnConnectedToSteam?.Invoke();

            }
            catch (System.Exception e)
            {
                Debug.LogError($"{e.Message}");
                return false;
            }

            return true;

        }
        #endregion

        #region Terminate Connection
        // INFO: Disconnect from steam
        protected void TerminateSteamConnection()
        {
            if (!connectedToSteam) return;

            try
            {
                SteamClient.Shutdown();
                if (!connectedToSteam) Debug.Log($"<color=orange>[CLIENT]</color> Connection terminated successfully!");

            }
            catch (System.Exception e)
            {
                if (connectedToSteam) Debug.LogError($"{e.Message}");

            }
        }

        // INFO: Ensure correct termination
        private void OnDestroy() => TerminateSteamConnection();
        protected void OnApplicationQuit() => TerminateSteamConnection();
        #endregion

        private void CheckSteamConnection()
        {
            if (connectedToSteam) return;

            // INFO: Not Connected
            Debug.LogWarning($"Not connected to steam, disabling {name}");
            gameObject.SetActive(false);

        }
        #endregion

        // INFO: DO NOT EDIT!
        #region Steamworks
        #region Create Server
        private async void StartSteamServer(int playerCount)
        {
            if (currentLobby != null) { Debug.LogWarning($"Lobby is already created!"); return; }

            Debug.Log($"{CheckPrivilege()} Lobby request received creating lobby!");
            await SteamMatchmaking.CreateLobbyAsync(playerCount);

        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK) { Debug.LogWarning($"Error creating lobby!"); return; }

            lobby.SetJoinable(true);
            lobby.SetPrivate();
            lobby.SetGameServer(lobby.Owner.Id);
            Debug.Log($"{CheckPrivilege()} Lobby created! | {lobby.Owner.Name} ({lobby.Id}) | {lobby.MemberCount}/{lobby.MaxMembers}");
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
            Debug.Log($"{CheckPrivilege()} {friend.Name} is joining!");

        }


        private void OnLobbyEntered(Lobby lobby)
        {
            Debug.Log($"{CheckPrivilege()} You entered {lobby.Owner.Name}'s lobby!");
            OnSteamClientEntered(lobby);


        }
        #endregion

        #region Player Left/Disconnected
        private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} left!");
            OnSteamClientLeave();

        }

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} disconnected!");
            OnSteamClientLeave();

        }
        #endregion
        #endregion

        #region Host
        protected virtual void OnSteamHostEntered()
        {
            _eventManager.OnSteamHostConnect?.Invoke();
            Debug.Log($"{CheckPrivilege()} Oh herro mister Host!");

        }

        protected virtual void OnSteamHostLeave()
        {
            Debug.Log($"Hi?");
            _eventManager.OnStopUnityHost?.Invoke();
        }
        #endregion

        #region Client

        protected virtual void OnSteamClientEntered(Lobby lobby)
        {
            // INFO: Client
            currentLobby = lobby;
            _networkTransport.targetSteamId = currentLobby.Value.Owner.Id;
            SteamFriends.SetRichPresence("connect", currentLobby.Value.Id.ToString());

            if (IsHost) { OnSteamHostEntered(); return; }
            _eventManager.OnSteamClientConnect?.Invoke();

        }

        protected virtual void OnSteamClientLeave()
        {
            currentLobby = null;
            if (IsHost) { OnSteamHostLeave(); return; }
            _eventManager.OnStopUnityClient?.Invoke();

        }

        protected virtual void DisconnectPlayer()
        {
            if (!connectedToSteam) return;
            if (currentLobby == null) return;

            SteamFriends.SetRichPresence("connect", null);
            currentLobby?.Leave();

        }
        #endregion

        #region Utility
        protected string CheckPrivilege()
        {
            if (!connectedToSteam || currentLobby == null) return $"<color={LogColours.Steamworks}>[STEAM]</color>";

            switch (SteamClient.SteamId == currentLobby.Value.Owner.Id)
            {
                case true:
                    return $"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Host}>[HOST]</color>";
                case false:
                    return $"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Client}>[CLIENT]</color>";
            }
        }
        #endregion


    }
}