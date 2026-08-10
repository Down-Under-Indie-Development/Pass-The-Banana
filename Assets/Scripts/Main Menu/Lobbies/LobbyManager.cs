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
    public class LobbyManager : PersistentSingleton<LobbyManager>
    {
        private Lobby currentLobby;
        private SteamId lobbyID => currentLobby.Id;

        [SerializeField] private List<string> _devNames = new();

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
            SteamMatchmaking.OnLobbyMemberJoined += OnPlayerJoined;

        }

        private void OnDisable()
        {
            // INFO: Host
            SteamMatchmaking.OnLobbyMemberJoined -= OnPlayerJoined;
        }

        #endregion

        #region Host
        public void CreateLobby(Lobby lobby)
        {
            currentLobby = lobby;
            if (_debug) Test();

        }

        #region Join Lobby
        private void OnPlayerJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} is joining!");

        }
        #endregion
        #endregion

        #region Testing
        private void Test()
        {
            foreach (Friend friend in SteamFriends.GetFriends())
            {
                if (!_devNames.Contains(friend.Name)) continue;
                friend.InviteToGame(lobbyID.ToString());
                friend.SendMessage("This should work, accept it plz");
                // friend.SendMessage($"Hey I created another lobby, {lobbyID} (Don't use this code i'm still testing).");
                // Debug.Log($"{friend.Name}");

            }

        }
        #endregion

    }
}