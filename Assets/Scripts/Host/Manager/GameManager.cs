using Utility;
using UnityEngine;
using System.Collections.Generic;
using PTB.Client.Player;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using PTB.Networking;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Services.Lobbies.Models;

public class GameManager : NetworkedSingleton<GameManager>
{


    [Space()]
    [Header("Game Settings")]
    [SerializeField] private List<CategorySO> _categories;


    [Space()]
    [Header("Player Tracking")]
    [SerializeField] private PlayerNetworkedController _playerWithBanana;
    private IReadOnlyDictionary<ulong, NetworkClient> _connectedClients => NetworkManager.Singleton.ConnectedClients;
    private IReadOnlyList<ulong> _connectedClientIds => NetworkManager.Singleton.ConnectedClientsIds;

    [Space()]
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();



    #region Networking Components
    // INFO: Network Components
    private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
    private SteamManager _steamManager => SteamManager.Instance;
    public GameState currentGameState => _networkHelper.sessionStateManager.currentSessionState.Value;
    #endregion

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

    #region Networking
    public override void OnNetworkSpawn()
    {
        RequestStartGameRPC();

    }
    #endregion

    #region Spawn Players
    // INFO: Spawn all the players on the host and client
    [Rpc(SendTo.Server)]
    private void RequestStartGameRPC()
    {
        SpawnPlayers();

    }

    // INFO: Called on the host
    private void SpawnPlayers()
    {
        if (!IsServer) return;
        if (_playerPrefab == null) { Debug.LogError($"Player prefab is null, cannot spawn!"); return; }


        for (int i = 0; i < _connectedClientIds.Count; i++)
        {
            ulong currentClient = _connectedClientIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;
            instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(currentClient, true);


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
        if (_networkHelper.sessionStateManager.currentSessionState.Value != GameState.Playing) return;
        Debug.Log($"{_networkHelper.CheckPrivilege()} All players spawned ready to start!");

    }
    #endregion

    #region Host Pausing
    [Rpc(SendTo.NotServer)] // INFO: Notify the clients the host has paused!
    public void NotifyServerOfHostPauseRPC()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"{_networkHelper.CheckPrivilege()} OMG the host has paused!");

        foreach (NetworkClient netObj in _connectedClients.Values)
        {
            PlayerNetworkedController playerController = netObj.PlayerObject.GetComponent<PlayerNetworkedController>();
            if (playerController == null) continue;
            playerController.PausePlayer();

        }
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