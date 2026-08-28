using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using UnityEngine;
using Unity.Netcode;
using Utility;
using PTB.Client.Player;
using HealthSystem;
using Unity.VisualScripting;
using System.Collections;


/// <summary>
/// Holds and handles all logic regarding players
/// </summary>
public class PlayerManager : NetworkedSingleton<PlayerManager>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;
    private Dictionary<ulong, Vector3> _originalPodiumPositions = new Dictionary<ulong, Vector3>();

    [field: Header("Player Tracking")]
    [field: SerializeField, ReadOnly] public int PlayersRemaining { get; private set; }

    [Space()]
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();
    [SerializeField] private Transform _hotSeat;

    #region Spawn Players
    public void SpawnPlayers()
    {
        if (!IsServer) return;
        if (_playerPrefab == null) { Debug.LogError($"Player prefab is null, cannot spawn!"); return; }

        Debug.Log($"[SERVER] Spawning {NetworkManager.ConnectedClientsIds.Count} players");

        for (int i = 0; i < NetworkManager.ConnectedClientsIds.Count; i++)
        {
            ulong currentClient = NetworkManager.ConnectedClientsIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;

            // Store the original podium position
            _originalPodiumPositions[currentClient] = _spawnPositions[i].position;

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

    public Coroutine MoveToHotSeat(ulong clientId, bool reverse = false)
    {
        if (_hotSeat == null) { Debug.LogWarning($"Hot seat transform is null, cannot move player!"); return null; }
        PlayerNetworkedController player = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetworkedController>();
        return StartCoroutine(MoveToHotSeatCoroutine(player.transform, .5f, reverse, clientId));

    }

    private IEnumerator MoveToHotSeatCoroutine(Transform playerTransform, float duration, bool reverse, ulong clientId)
    {
        Vector3 playerStart = playerTransform.position;
        Vector3 playerTargetPosition;

        if (reverse)
        {
            // Move back to original podium position
            playerTargetPosition = _originalPodiumPositions[clientId];
        }
        else
        {
            // Move to hot seat
            playerTargetPosition = _hotSeat.transform.position;

        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            playerTransform.position = Vector3.Lerp(playerStart, playerTargetPosition, t);

            yield return null;
        }

        playerTransform.position = playerTargetPosition;

    }

    [Rpc(SendTo.ClientsAndHost)]
    public void HandleChangePodiumColorRPC(ulong client, Color colour)
    {
        NetworkManager.ConnectedClients[client].PlayerObject.GetComponent<PlayerNetworkedController>().podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = colour;
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