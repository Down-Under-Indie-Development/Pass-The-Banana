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
using UnityEngine.UI;

public class LobbyUIManager : CustomMonoBehaviour
{

    [Header("Player Panel")]
    [SerializeField] private GameObject playerPanelContent;
    [SerializeField] private GameObject playerInfoPanel;

    [Header("Buttons")]
    [SerializeField] private Button _startGameBTN;


    private Queue<SteamId> _connectedMembers = new();

    #region Events
    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;

    }
    #endregion

    private void Awake()
    {
        if (!NetworkManager.Singleton.IsHost && _startGameBTN != null) _startGameBTN.interactable = false;

    }

    private void LateUpdate()
    {
        RefreshUI(SteamLobbyManager.Instance.currentLobby);
    }


    #region Steamworks
    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        AddNewPlayer(friend);
    }

    #endregion

    private void RefreshUI(Lobby? lobby)
    {

        if (playerPanelContent == null) { Debug.LogError($"Player panel content is null!"); return; }
        if (playerInfoPanel == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }
        if (lobby == null) return;

        for (int i = 0; i < playerPanelContent.transform.childCount; i++)
        {
            Destroy(playerPanelContent.transform.GetChild(i).gameObject);

        }

        foreach (Friend member in lobby.Value.Members)
        {
            GameObject playerInfoGO = Instantiate(playerInfoPanel);
            playerInfoGO.transform.SetParent(playerPanelContent.transform, false);

            // INFO: Set Display
            PlayerUIInfo playerInfo = playerInfoGO.GetComponent<PlayerUIInfo>();
            bool isHost = lobby.Value.Owner.Id == member.Id;
            playerInfo.playerName = $"{member.Name} {(isHost ? "[HOST]" : "")}";
            playerInfo.playerPing = $"{GetEstimatedPing(lobby, member.Id)}";


        }
    }

    private void AddNewPlayer(Friend member)
    {
        if (!_connectedMembers.Contains(member.Id)) _connectedMembers.Enqueue(member.Id);

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

    #region Buttons
    public void StartGame()
    {
        MainMenuController _mainMenuController = MainMenuController.Instance;
        if (_connectedMembers.Count < _mainMenuController.minimumPlayers && !_debug) { Debug.LogWarning($"Need {_mainMenuController.minimumPlayers} players to start"); return; }
        NetworkManager.Singleton.SceneManager.LoadScene("Test Scene", LoadSceneMode.Single);

    }
    #endregion

}