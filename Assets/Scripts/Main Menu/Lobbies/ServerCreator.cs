using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class ServerCreator : Singleton<ServerCreator>
    {
        private Lobby currentLobby;
        private SteamId lobbyID => currentLobby.Id;

        private void Start()
        {
            if (!SteamManager.connectedToSteam)
            {
                Debug.LogWarning($"Not connected to steam, disabling {name}");
                gameObject.SetActive(false);
                return;

            }

        }

        #region Events
        private void OnEnable()
        {
            // INFO: Host
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            _eventManager.OnCreateLobby += CreateLobby;

            // INFO: Client
            SteamFriends.OnGameLobbyJoinRequested += OnGameInviteAccepted;
            SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;

        }

        private void OnDisable()
        {
            // INFO: Host
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            _eventManager.OnCreateLobby -= CreateLobby;
        }

        #endregion

        #region Host
        #region Create Lobby
        public async void CreateLobby(int playerCount, bool friendsOnly)
        {
            Debug.Log($"[HOST] Lobby request received creating lobby!");
            Debug.Log($"Creating lobby for {playerCount} player(s)");
            await SteamMatchmaking.CreateLobbyAsync(playerCount);
            currentLobby.SetJoinable(true);
            if (!friendsOnly) currentLobby.SetPublic();

        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            try
            {
                // Guard: Lobby already exists
                if (currentLobby.Id.IsValid) return;

                currentLobby = lobby; // INFO: Create lobby
                if (_debug) Debug.Log($"Lobby Created ({currentLobby.Id})");

            }
            catch (System.Exception e)
            {
                Debug.LogError($"Lobby creation failed");
                Debug.LogError($"{e.Message}");
            }


            Debug.Log($"[HOST] Lobby created!");
            LobbyManager.instance.CreateLobby(currentLobby);

        }
        #endregion

        #endregion

        #region Client
        private async void OnGameInviteAccepted(Lobby lobby, SteamId steamId)
        {
            await lobby.Join();

        }

        private async void OnGameRichPresenceJoinRequested(Friend friend, string s)
        {
            if (!ulong.TryParse(s, out ulong seshID)) return;
            await SteamMatchmaking.JoinLobbyAsync(seshID);

        }

        #endregion

    }
}