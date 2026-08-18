using Utility;
using UnityEngine;
using System.Collections.Generic;
using PTB.Client.Player;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using PTB.Networking;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class GameManager : NetworkedSingleton<GameManager>
{


    [Header("Game Settings")]
    [SerializeField] private List<CategorySO> _categories;


    [Header("Player Tracking")]
    [SerializeField] private PlayerNetworkedController _playerWithBanana;

    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();
    [SerializeField] private GameObject _pauseMenuGO;

    #region Networking Components
    // INFO: Network Components
    private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
    private SteamManager _steamManager => SteamManager.Instance;
    #endregion

    // INFO: Sync game state
    public GameState currentGameState => _networkHelper.sessionStateManager.currentSessionState.Value;

    #region Events
    private void OnEnable()
    {
        // _eventManager.OnQuestionFinished += DisplayAnswers;
        _eventManager.OnGameStart += RequestStartGameRPC;
    }

    private void OnDisable()
    {
        if (_eventManager == null) return;
        _eventManager.OnGameStart -= RequestStartGameRPC;

    }
    #endregion

    private void Start()
    {
        _pauseMenuGO?.SetActive(false);

    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) TogglePauseMenu(); // INFO: Check for pause menu
    }

    #region Menus
    private void TogglePauseMenu()
    {
        if (_pauseMenuGO == null) { Debug.LogWarning($"Pause menu is null!"); return; }
        _pauseMenuGO?.SetActive(!_pauseMenuGO.activeSelf);

    }
    #endregion


    #region Networking
    // INFO: Players spawned in (Get the host to start it)
    public override void OnNetworkSpawn()
    {

        if (!IsServer) return;
        RequestStartGameRPC();

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RequestStartGameRPC()
    {
        SpawnPlayers();


    }

    // INFO: Called on the host
    private void SpawnPlayers()
    {
        if (!IsServer) return;
        if (_playerPrefab == null) return;

        List<ulong> clientIds = new List<ulong>(_networkHelper.networkManager.ConnectedClientsIds);
        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong currentClient = clientIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;
            instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(currentClient);


        }

        StartGameRPC();
    }
    #endregion

    #region Game Sequence
    // INFO: All players spawn now do shit!
    [Rpc(SendTo.Server)]
    private void StartGameRPC()
    {
        _networkHelper.sessionStateManager.UpdateSessionState(GameState.Playing);
        Debug.Log($"{_networkHelper.CheckPrivilege()} All players spawned ready to start!");

    }
    #endregion


}


// INFO: Game state
public enum GameState
{
    MainMenu,
    HostGame,
    Lobby,
    JoinGame,
    Options,
    Playing,
}