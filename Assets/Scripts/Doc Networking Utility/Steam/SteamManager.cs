using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace PTB.Networking
{

    public class SteamManager : Singleton<SteamManager>
    {
        private EventManager _eventManager => EventManager.Instance;

        [field: Header("Steam Settings")]
        [field: SerializeField] public uint appID { get; protected set; } = 480;
        public bool connectedToSteam => SteamClient.IsValid;

        public Lobby? myLobby { get; protected set; }


        [Header("Events")]
        public UnityEvent EvtSteamInitialised = new UnityEvent();
        public UnityEvent EvtSteamInitialisedError = new UnityEvent();


        #region Networking
        // protected NetworkManager _networkManager => NetworkManager.Singleton;
        protected FacepunchTransport _networkTransport => NetworkManager.Singleton.GetComponent<FacepunchTransport>();
        #endregion

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

        #region Steam Connection
        #region Establish Connection
        // INFO: Establish connection to steam servers
        public virtual bool EstablishSteamConnection()
        {
            if (connectedToSteam) return false;

            try
            {
                SteamClient.Init(appID);
                Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Successfully Connected to steam! | {SteamClient.Name} ({SteamClient.AppId})</color>");
                _networkTransport.steamAppId = appID;
                // _eventManager.OnConnectedToSteam?.Invoke();
                EvtSteamInitialised?.Invoke();

            }
            catch (System.Exception e)
            {
                Debug.LogError($"{e.Message}");
                EvtSteamInitialisedError?.Invoke();

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
                SteamFriends.SetRichPresence("connect", null);
                SteamClient.Shutdown();
                if (!connectedToSteam) Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Connection terminated successfully!");

            }
            catch (System.Exception e)
            {
                if (connectedToSteam) Debug.LogError($"{e.Message}");

            }
        }

        // INFO: Ensure correct termination
        protected void OnApplicationQuit()
        {
            OnSteamClientLeave();
            TerminateSteamConnection();
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

        #region Lobbies
        // INFO: DO NOT EDIT!
        #region Steamworks
        #region Create Server
        private async void StartSteamServer(LobbyData lobbyData)
        {
            if (!connectedToSteam) EstablishSteamConnection();
            Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Lobby request received creating lobby!");
            Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync(lobbyData.maxPlayers);
            lobby.Value.SetGameServer(lobby.Value.Owner.Id);
            lobby.Value.SetJoinable(true);

            if (lobbyData.friendsOnly)
            {
                lobby.Value.SetFriendsOnly();

            }
            else
            {
                lobby.Value.SetPrivate();
            }


        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            try
            {
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
            if (myLobby.HasValue) { Debug.LogWarning("Already in a lobby"); return; }

            RoomEnter joinedLobby = await lobby.Join();
            if (joinedLobby != RoomEnter.Success) { Debug.LogError($"Failed to join {lobby}"); return; }

        }

        private async void OnGameRichPresenceJoinRequested(Friend friend, string s)
        {
            if (myLobby.HasValue) { Debug.LogWarning("Already in a lobby"); return; }

            if (!ulong.TryParse(s, out ulong seshID)) return;
            Lobby? joinedLobby = await SteamMatchmaking.JoinLobbyAsync(seshID);
            if (joinedLobby == null) { Debug.LogError($"Failed to join lobby!"); return; }


        }
        #endregion

        #region Player Joining/Joined
        private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} is joining!");

        }


        private void OnLobbyEntered(Lobby lobby)
        {
            OnSteamClientEntered(lobby);
            if (SteamClient.SteamId != lobby.Owner.Id) Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Client}>[CLIENT]</color> You entered {lobby.Owner.Name}'s lobby!");


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
            myLobby = lobby;
            SteamFriends.SetRichPresence("connect", lobby.Id.ToString());

            if (SteamClient.SteamId == lobby.Owner.Id) { OnSteamHostEntered(); return; }
            _networkTransport.targetSteamId = lobby.Owner.Id;
            _eventManager.OnSteamClientConnect?.Invoke();

        }

        protected virtual void OnSteamClientLeave()
        {
            if (!connectedToSteam) { Debug.LogError($"Client is not connected, cannot disconnect!"); return; }
            _networkTransport.targetSteamId = 0;

            // INFO: Leave the lobby
            SteamFriends.SetRichPresence("connect", null);
            myLobby?.Leave();

            if (SteamClient.SteamId == myLobby.Value.Owner.Id)
            {
                OnSteamHostLeave();
            }
            else
            {
                _eventManager.OnStopUnityClient?.Invoke();

            }
            myLobby = null;

        }


        #endregion

        #region Utility
        protected virtual string CheckPrivilege()
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

        private bool AreAllPlayersReady()
        {
            // int lobbyMemberCount = SteamMatchmaking.;

            // for (int i = 0; i < lobbyMemberCount; i++)
            // {
            //     CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            //     string readyState = SteamMatchmaking.GetLobbyMemberData(lobbyID, memberID, "ready");

            //     if (readyState != "true")
            //         return false;
            // }

            return true;
        }
        #endregion
        #endregion


    }
}