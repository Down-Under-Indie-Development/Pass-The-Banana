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
using TMPro;

public class LobbyUIManager : CustomMonoBehaviour
{

    [Header("Player Panel")]
    [SerializeField] private GameObject playerPanelContent;
    [SerializeField] private GameObject playerInfoPanel;

    [Header("Buttons")]
    [SerializeField] private Button _startGameBTN;

    [SerializeField] private TextMeshProUGUI _lobbyCodeTxt;

    private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
    private SteamManager _steamManager => SteamManager.Instance;


    private Queue<SteamId> _connectedMembers = new();

    #region Events
    private void OnEnable()
    {
        // _networkHelper.networkManager.OnClientConnectedCallback += UpdateLobbyCodeText;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        _eventManager.OnUnityClientDisconnected += ResetMenu;
    }

    private void OnDisable()
    {
        // _networkHelper.networkManager.OnClientConnectedCallback += UpdateLobbyCodeText;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        _eventManager.OnUnityClientDisconnected -= ResetMenu;


    }
    #endregion

    private void Start()
    {
        if (_networkHelper.networkManager != null && !_networkHelper.networkManager.IsHost && _startGameBTN != null) _startGameBTN.interactable = false;
        UpdateLobbyCodeText();


    }

    private void ResetMenu()
    {
        _lobbyCodeTxt.text = "";
        ClearPlayerPanel();

    }

    private void FixedUpdate()
    {
        if (_steamManager.myLobby != null) RefreshUI(_steamManager.myLobby);
    }


    #region Steamworks

    private void OnLobbyEntered(Lobby lobby)
    {
        UpdateLobbyCodeText();
        RefreshUI(lobby);

    }

    private void UpdateLobbyCodeText()
    {
        if (_lobbyCodeTxt != null) _lobbyCodeTxt.text = $"Code: {SteamManager.Instance.myLobby.Value.Id}";

    }

    #endregion

    private void ClearPlayerPanel()
    {
        for (int i = 0; i < playerPanelContent.transform.childCount; i++)
        {
            Destroy(playerPanelContent.transform.GetChild(i).gameObject);

        }

    }

    private void RefreshUI(Lobby? lobby)
    {
        if (playerPanelContent == null) { Debug.LogError($"Player panel content is null!"); return; }
        if (playerInfoPanel == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }
        if (lobby == null) return;
        ClearPlayerPanel();
        if (_lobbyCodeTxt != null && _lobbyCodeTxt.text == "") UpdateLobbyCodeText();

        foreach (Friend member in lobby.Value.Members)
        {
            GameObject playerInfoGO = Instantiate(playerInfoPanel);
            playerInfoGO.transform.SetParent(playerPanelContent.transform, false);

            // INFO: Set Display
            PlayerUIInfo playerInfo = playerInfoGO.GetComponent<PlayerUIInfo>();
            bool isHost = lobby.Value.Owner.Id == member.Id;
            playerInfo.playerName = $"{member.Name} {(isHost ? "[HOST]" : "     ")}";
            playerInfo.playerPing = $"{-1}ms";


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

    #region Buttons
    public void StartGame()
    {
        MainMenuController _mainMenuController = MainMenuController.Instance;
        if (_connectedMembers.Count < SteamManager.Instance.minimumPlayers && !_debug) { Debug.LogWarning($"Need {SteamManager.Instance.minimumPlayers} players to start"); return; }
        Debug.Log($"{_networkHelper.CheckPrivilege()} Started the game!");
        _networkHelper.networkManager.SceneManager.LoadScene("Test Scene", LoadSceneMode.Single);

    }
    #endregion

}