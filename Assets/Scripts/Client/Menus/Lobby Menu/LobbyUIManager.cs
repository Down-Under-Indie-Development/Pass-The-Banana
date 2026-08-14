using Utility;
using UnityEngine;
using Steamworks.Data;
using Steamworks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using PTB.Menus;
using System.Threading.Tasks;

public class LobbyUIManager : CustomMonoBehaviour
{

    [SerializeField] private GameObject playerPanelContent;
    [SerializeField] private GameObject playerInfoPanel;

    private Queue<Friend> _connectedMembers;

    #region Events
    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;

    }
    #endregion

    private void Awake()
    {

        _connectedMembers = new();

    }

    #region Steamworks
    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK) RefreshUI(lobby);

    }

    private void OnLobbyEntered(Lobby lobby)
    {
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

    public void FuckingWork()
    {
        _ = StartGame();
    }

    public async Task StartGame()
    {
        MainMenuController _mainMenuController = MainMenuController.Instance;
        if (_connectedMembers.Count < _mainMenuController.minimumPlayers && !_debug) { Debug.LogWarning($"Need {_mainMenuController.minimumPlayers} players to start"); return; }
        await SceneManager.LoadSceneAsync(1);

    }

}