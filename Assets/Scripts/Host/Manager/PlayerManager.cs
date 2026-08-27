using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using UnityEngine;
using Unity.Netcode;
using Utility;
using PTB.Client.Player;
using HealthSystem;


/// <summary>
/// Holds and handles all logic regarding players
/// </summary>
public class PlayerManager : NetworkedSingleton<PlayerManager>
{

    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    [field: Header("Player Tracking")]
    [field: SerializeField, ReadOnly] public int PlayersRemaining { get; private set; }


    [Space()]
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();

    #region Spawn Players
    // INFO: Called on the host
    public void SpawnPlayers()
    {
        if (!IsServer) return;
        if (_playerPrefab == null) { Debug.LogError($"Player prefab is null, cannot spawn!"); return; }

        Debug.Log($"[SERVER] Spawning {_gameManager.bootstrapNetworkManager.connectedClientIds.Count} players");

        for (int i = 0; i < _gameManager.bootstrapNetworkManager.connectedClientIds.Count; i++)
        {
            ulong currentClient = _gameManager.bootstrapNetworkManager.connectedClientIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;

            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(currentClient, true);

        }

        InitializePlayers(NetworkManager.ConnectedClients.Count);

    }
    #endregion

    public void EliminatePlayer(ulong clientId)
    {
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject
            .GetComponent<PlayerNetworkedController>()
            .GetComponent<IDamageable>()
            .Die();

        PlayersRemaining--;

    }

    public void InitializePlayers(int playerCount)
    {
        PlayersRemaining = playerCount;
        Debug.Log($"Game initialized with {PlayersRemaining} players");
    }

    public void UpdatePlayerRemaining(int newValue)
    {
        if (PlayersRemaining - newValue <= 0) { PlayersRemaining = 0; return; }
        PlayersRemaining += newValue;

    }


}