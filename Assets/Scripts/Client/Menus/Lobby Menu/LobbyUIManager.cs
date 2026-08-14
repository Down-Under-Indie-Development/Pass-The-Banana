using Utility;
using UnityEngine;
using Steamworks.Data;
using Steamworks;
using PTB.Networking;
using System;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine.UIElements;

public class LobbyUIManager : CustomMonoBehaviour
{

    [SerializeField] private GameObject playerPanelContent;
    [SerializeField] private GameObject playerInfoPanel;

    private Queue<Friend> _connectedMembers;

    #region Events
    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

    }
    #endregion

    private void Start()
    {
        _connectedMembers = new();
        _eventManager.OnCreateLobbyRequest?.Invoke(1);
    }

    #region Steamworks
    private void OnLobbyEntered(Lobby lobby)
    {
        StartCoroutine(PublishPingLocationRoutine(lobby));
    }

    private IEnumerator PublishPingLocationRoutine(Lobby lobby)
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();

        float timeout = 10f;
        float elapsed = 0f;
        NetPingLocation? pingLocation = null;

        while (elapsed < timeout)
        {
            pingLocation = SteamNetworkingUtils.LocalPingLocation;
            if (pingLocation.HasValue) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (pingLocation.HasValue)
        {
            lobby.SetMemberData("ping_location", pingLocation.Value.ToString());
            Debug.Log("Published ping location");
        }
        else
        {
            Debug.LogWarning("Ping location unavailable after 10s timeout.");
        }



        RefreshUI(lobby);
    }
    #endregion


    private void RefreshUI(Lobby lobby)
    {
        if (playerPanelContent == null) { Debug.LogError($"Player panel content is null!"); return; }
        if (playerInfoPanel == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }

        foreach (Friend member in lobby.Members)
        {
            if (_connectedMembers.Contains(member)) continue; // INFO: Already showing (Hopefully)
            _connectedMembers.Enqueue(member);
            GameObject playerInfoGO = Instantiate(playerInfoPanel);
            playerInfoGO.transform.SetParent(playerPanelContent.transform, false);
            // INFO: Set Display
            PlayerUIInfo playerInfo = playerInfoGO.GetComponent<PlayerUIInfo>();
            bool isHost = lobby.Owner.Id == member.Id;
            playerInfo.playerName = $"{member.Name} {(isHost ? "[HOST]" : "")}";
            playerInfo.playerPing = $"{GetEstimatedPing(lobby, member.Id)}";

        }
    }

    [ContextMenu("Testing")]
    public void Test()
    {


    }

    int GetEstimatedPing(Lobby? lobby, SteamId memberId)
    {
        if (lobby == null) return -1;
        Debug.Log($"Test");

        Friend member = new Friend(memberId);
        string encoded = lobby.Value.GetMemberData(member, "ping_location");
        if (string.IsNullOrEmpty(encoded)) return -1;

        NetPingLocation? theirLocation = NetPingLocation.TryParseFromString(encoded);
        if (!theirLocation.HasValue) return -1;

        int estimatedPing = SteamNetworkingUtils.EstimatePingTo(theirLocation.Value);
        return estimatedPing;
    }

}