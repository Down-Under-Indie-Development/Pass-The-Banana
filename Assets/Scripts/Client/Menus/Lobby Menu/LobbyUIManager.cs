using Utility;
using UnityEngine;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using PTB.Menus;
using System.Threading.Tasks;
using Unity.Netcode;
using PTB.Networking;

public class LobbyUIManager : CustomMonoBehaviour
{

    [SerializeField] private GameObject playerPanelContent;
    [SerializeField] private GameObject playerInfoPanel;

    private Queue<SteamId> _connectedMembers = new();

    #region Events
    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyMemberJoined;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyMemberJoined;

    }
    #endregion

    private void Awake()
    {
        Debug.Log($"[LobbyUIManager] Awake on {gameObject.name}, active={gameObject.activeInHierarchy}");

    }

    #region Steamworks
    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK) RefreshUI(lobby);

    }

    private void OnLobbyMemberJoined(Lobby lobby)
    {
        RefreshUI(lobby);
    }

    #endregion

    private void RefreshUI(Lobby? lobby)
    {

        if (playerPanelContent == null) { Debug.LogError($"Player panel content is null!"); return; }
        if (playerInfoPanel == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }
        if (lobby == null) { Debug.LogError($"Lobby is null!"); return; }

        foreach (Friend member in lobby.Value.Members)
        {
            if (_connectedMembers.Contains(member.Id)) continue; // INFO: Already showing (Hopefully)
            _connectedMembers.Enqueue(member.Id);

            GameObject playerInfoGO = Instantiate(playerInfoPanel);
            playerInfoGO.transform.SetParent(playerPanelContent.transform, false);

            // INFO: Set Display
            PlayerUIInfo playerInfo = playerInfoGO.GetComponent<PlayerUIInfo>();
            bool isHost = lobby.Value.Owner.Id == member.Id;
            playerInfo.playerName = $"{member.Name} {(isHost ? "[HOST]" : "")}";
            playerInfo.playerPing = $"{GetEstimatedPing(lobby, member.Id)}";


        }
    }

    private void LateUpdate()
    {
        RefreshUI(SteamLobbyManager.Instance.currentLobby);
    }

    int GetEstimatedPing(Lobby? lobby, SteamId memberId)
    {
        if (lobby == null) return -1;

        Friend member = new Friend(memberId);
        string encoded = lobby.Value.GetMemberData(member, "ping_location");
        if (string.IsNullOrEmpty(encoded)) return -1;

        NetPingLocation? theirLocation = NetPingLocation.TryParseFromString(encoded);
        if (!theirLocation.HasValue) return -1;

        int estimatedPing = SteamNetworkingUtils.EstimatePingTo(theirLocation.Value);
        return estimatedPing;
    }

    public void StartGame()
    {
        MainMenuController _mainMenuController = MainMenuController.Instance;
        if (_connectedMembers.Count < _mainMenuController.minimumPlayers && !_debug) { Debug.LogWarning($"Need {_mainMenuController.minimumPlayers} players to start"); return; }
        NetworkManager.Singleton.SceneManager.LoadScene("Test Scene", LoadSceneMode.Single);

    }

}