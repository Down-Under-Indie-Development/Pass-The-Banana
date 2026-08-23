using Utility;
using UnityEngine;
using System.Collections.Generic;
using PTB.Client.Player;
using Unity.Netcode;
using PTB.Networking;
using System.Collections;
using System;
using System.Data;

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
    public GameState currentGameState => SessionStateManager.Instance.currentSessionState.Value;
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
        if (!IsServer) return;
        StartCoroutine(SpawnPlayersDelayed());

    }

    private IEnumerator SpawnPlayersDelayed()
    {
        yield return new WaitForSeconds(0.5f);
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

        Debug.Log($"[SERVER] Spawning {_connectedClientIds.Count} players");

        for (int i = 0; i < _connectedClientIds.Count; i++)
        {
            ulong currentClient = _connectedClientIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;

            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            Debug.Log($"[SERVER] Spawning player for client {currentClient}, IsOwner will be: {currentClient == NetworkManager.Singleton.LocalClientId}");

            netObj.SpawnAsPlayerObject(currentClient, true);

            Debug.Log($"[SERVER] Player spawned for {currentClient}");
        }

        StartGameRPC();
    }
    #endregion

    #region Game Sequence
    // INFO: All players spawn now do shit!
    [Rpc(SendTo.Server)]
    private void StartGameRPC()
    {
        BootstrapManager.Instance.sessionStateManager.UpdateSessionState(GameState.Playing);
        if (BootstrapManager.Instance.sessionStateManager.currentSessionState.Value != GameState.Playing) return;
        Debug.Log($"{_networkHelper.CheckPrivilege()} All players spawned ready to start!");

    }
    #endregion

    #region Host Pausing
    private bool _gamePaused = false;

    [Rpc(SendTo.ClientsAndHost)]  // INFO: Send to all clients AND the host
    public void BroadcastPauseStateRPC()
    {
        // INFO: Toggle the pause state
        _gamePaused = !_gamePaused;

        Debug.Log($"{_networkHelper.CheckPrivilege()} Game paused: {_gamePaused}");

        // INFO: Apply pause state to all connected players (including host)
        ForEachPlayer(player => player.ApplyPauseState(_gamePaused));

    }
    #endregion

    public void ForEachPlayer(Action<PlayerNetworkedController> action)
    {
        foreach (NetworkClient netObj in _connectedClients.Values)
        {
            PlayerNetworkedController playerController = netObj.PlayerObject.GetComponent<PlayerNetworkedController>();
            if (playerController == null) continue;
            action?.Invoke(playerController);
        }
    }

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