using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using PTB.Menus;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class SteamManager : PersistentSingleton<SteamManager>
    {

        [Header("Steam Settings")]
        [field: SerializeField] public uint appID { get; protected set; } = 480;
        public bool connectedToSteam => SteamClient.IsValid;

        [Header("Lobby Settings")]
        [field: SerializeField] public int minimumPlayers { get; protected set; } = 2;



        #region Networking
        // public Lobby? myLobby { get; protected set; }
        // private EventManager _eventManager => EventManager.Instance;
        protected FacepunchTransport _networkTransport;
        #endregion

        protected override void Awake()
        {
            _networkTransport = GetComponent<FacepunchTransport>();



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
            #endregion
            #endregion

            #region Client
            // INFO: Client
            _eventManager.OnSteamClientDisconnect += OnSteamClientLeave;

            #region Steamworks
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
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
            #endregion
            #endregion

            #region Client
            // INFO: Client
            _eventManager.OnSteamClientDisconnect -= OnSteamClientLeave;

            #region Steamworks
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;
            #endregion
            #endregion

        }
        #endregion

        private void Start()
        {
            EstablishSteamConnection();
            CheckSteamConnection();
        }


        #region Steam Connection
        #region Establish Connection
        // INFO: Establish connection to steam servers
        protected virtual bool EstablishSteamConnection()
        {
            if (connectedToSteam) return true; // INFO: Prevent calling more than once

            try
            {
                SteamClient.Init(appID);
                Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Successfully Connected to steam! | {SteamClient.Name} ({SteamClient.AppId})</color>");
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
        protected virtual void TerminateSteamConnection()
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
        protected void OnApplicationQuit()
        {
            TerminateSteamConnection();
            OnSteamClientLeave();
        }

        #endregion

        protected void CheckSteamConnection()
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
            EstablishSteamConnection();
            if (playerCount <= 0) playerCount = minimumPlayers;
            Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Lobby request received creating lobby!");
            // if (myLobby != null) { Debug.LogWarning($"Lobby already exists!"); return; }
            await SteamMatchmaking.CreateLobbyAsync(playerCount);

        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            try
            {
                // if (myLobby.Value.Id.IsValid) { Debug.LogError($"Failed to create lobby!"); return; }
                lobby.SetGameServer(lobby.Owner.Id);
                lobby.SetPrivate();
                lobby.SetJoinable(true);

                Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Lobby created! | {lobby.Owner.Name} ({lobby.Id}) | {lobby.MemberCount}/{lobby.MaxMembers}");
                GUIUtility.systemCopyBuffer = lobby.Id.ToString(); // INFO: Copies lobby code to peoples keyboard

            }
            catch (System.Exception e)
            {
                Debug.LogError($"{e.Message}");
            }
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
            // myLobby = joinedLobby;

        }
        #endregion

        #region Player Joining/Joined
        private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} is joining!");

        }


        private void OnLobbyEntered(Lobby lobby)
        {
            if (SteamClient.SteamId != lobby.Owner.Id) Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Client}>[CLIENT]</color> You entered {lobby.Owner.Name}'s lobby!");
            OnSteamClientEntered(lobby);


        }
        #endregion

        #region Player Left/Disconnected
        private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} left!");

        }

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} disconnected!");

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
            Debug.Log($"{CheckPrivilege()} Goodbye mister Host!");
            _eventManager.OnStopUnityHost?.Invoke();

        }
        #endregion

        #region Client

        protected virtual void OnSteamClientEntered(Lobby lobby)
        {
            // INFO: Client
            SteamFriends.SetRichPresence("connect", lobby.Id.ToString());

            if (SteamClient.SteamId == lobby.Owner.Id) { OnSteamHostEntered(); return; }
            _networkTransport.targetSteamId = lobby.Owner.Id;
            _eventManager.OnSteamClientConnect?.Invoke();

        }

        protected virtual void OnSteamClientLeave()
        {
            if (!connectedToSteam) return;
            // if (myLobby == null) { Debug.LogError($"Current lobby was null when leaving!"); return; }
            _networkTransport.targetSteamId = 0;

            // INFO: Leave the lobby
            SteamFriends.SetRichPresence("connect", null);

            if (NetworkManager.Singleton.IsHost)
            {
                OnSteamHostLeave();
            }
            else
            {
                _networkTransport.DisconnectRemoteClient(SteamClient.SteamId);
                _eventManager.OnStopUnityClient?.Invoke();

            }

            _networkTransport.DisconnectLocalClient();
            _networkTransport.DisconnectRemoteClient(SteamClient.SteamId);
            // await System.Threading.Tasks.Task.Delay(500);

        }


        #endregion

        #region Utility
        protected string CheckPrivilege()
        {
            if (!connectedToSteam) return $"<color={LogColours.Steamworks}>[STEAM]</color>";

            switch (NetworkManager.Singleton.IsHost)
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