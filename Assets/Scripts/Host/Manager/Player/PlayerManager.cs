using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using UnityEngine;
using Unity.Netcode;
using Utility;
using PTB.Client.Player;
using HealthSystem;
using Unity.VisualScripting;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using PTB.Networking;


/// <summary>
/// Holds and handles all logic regarding players
/// </summary>
public class PlayerManager : NetworkedSingleton<PlayerManager>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;
    private Dictionary<ulong, Vector3> _originalPodiumPositions = new Dictionary<ulong, Vector3>();
    private HashSet<ulong> _eliminatedPlayers = new HashSet<ulong>(); // Track eliminated players

    [field: Header("Player Tracking")]
    [field: SerializeField, ReadOnly] public int PlayersRemaining { get; private set; }

    [Space()]
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();
    [SerializeField] private Transform _hotSeat;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip _bombExplodeSFX;

    private PlayerAnimationHandler _playerAnimationHandler => PlayerAnimationHandler.Instance;

    #region Events
    private void OnEnable()
    {
        NetworkManager.Singleton.OnConnectionEvent += OnUnityClientDisconnect;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnConnectionEvent -= OnUnityClientDisconnect;

    }
    #endregion

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

    [Rpc(SendTo.Server)]
    public void EliminatePlayerRPC(ulong clientId)
    {
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject
            .GetComponent<PlayerNetworkedController>()
            .GetComponent<IDamageable>()
            .Die();

        _eliminatedPlayers.Add(clientId); // Track as eliminated

        for (int i = 0; i < _gameManager.activePlayers.Count; i++)
        {
            if (_gameManager.activePlayers[i] != clientId) continue;
            _gameManager.activePlayers.Remove(clientId);
        }

    }

    #region Move To Hot Seat
    public Coroutine MoveToHotSeat(ulong clientId, bool reverse = false)
    {
        if (_hotSeat == null) { Debug.LogWarning($"Hot seat transform is null, cannot move player!"); return null; }
        NetworkObject playerObj = NetworkManager.ConnectedClients[clientId].PlayerObject;
        _playerAnimationHandler.SetAnimator(playerObj.GetComponent<Animator>(), "inHotSeat", !reverse);
        return StartCoroutine(MoveToHotSeatCoroutine(playerObj.transform, .5f, reverse, clientId));

    }

    private IEnumerator MoveToHotSeatCoroutine(Transform playerTransform, float duration, bool reverse, ulong clientId)
    {
        Vector3 playerStart = playerTransform.position;
        Vector3 playerTargetPosition = reverse ? _originalPodiumPositions[clientId] : _hotSeat.transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (playerTransform == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            playerTransform.position = Vector3.Lerp(playerStart, playerTargetPosition, t);

            yield return null;
        }

        playerTransform.position = playerTargetPosition;

    }
    #endregion

    #region Player Leave 
    private async void OnUnityClientDisconnect(NetworkManager networkManager, ConnectionEventData connectionEventData)
    {
        if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected) return;

        if (connectionEventData.ClientId == networkManager.LocalClientId)
        {
            string sceneName = BootstrapManager.Instance.gameObject.scene.name;

            // await SceneManager.UnloadSceneAsync(sceneName);
            // await Task.Delay(100);
            // await SceneManager.UnloadSceneAsync(gameObject.scene.name);
            // await Task.Delay(100);

            await SceneManager.LoadSceneAsync(sceneName);
            return;

        }

        if (!networkManager.IsServer) return;
        Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> {connectionEventData.ClientId} has left!");

    }
    #endregion


    #region Utility
    [Rpc(SendTo.ClientsAndHost)]
    public void HandleChangePodiumColorRPC(ulong client, Color colour)
    {
        NetworkManager.ConnectedClients[client].PlayerObject.GetComponent<PlayerNetworkedController>().podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = colour;
    }

    public bool IsPlayerActive(ulong clientId)
    {
        return NetworkManager.ConnectedClients.ContainsKey(clientId) && !_eliminatedPlayers.Contains(clientId);
    }

    public void InitializePlayers(int playerCount)
    {
        PlayersRemaining = playerCount;
        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> Game initialized with {PlayersRemaining} players");

    }

    public void UpdatePlayerRemaining(int newValue)
    {
        if (PlayersRemaining - newValue <= 0) { PlayersRemaining = 0; return; }
        PlayersRemaining += newValue;
    }
    #endregion

}