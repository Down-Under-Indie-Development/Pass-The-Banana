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
    // INFO: Singletons
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;
    private PlayerAnimationHandler _playerAnimationHandler => PlayerAnimationHandler.Instance;

    [field: Header("Player Tracking")]
    [field: SerializeField, DictionaryDisplay(keyLabel = "ID", valueLabel = "Eliminated")] public Dictionary<ulong, bool> activePlayers { get; private set; } = new();
    public int playersRemaining { get; private set; }
    private Dictionary<ulong, Vector3> _originalPodiumPositions = new();
    // private List<ulong> _eliminatedPlayers = new();

    [Space()]
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();
    [SerializeField] private Transform _hotSeat;

    [Header("Audio Clips")]
    [SerializeField] private Dictionary<PlayerState, AudioClip> _playerSFX;


    #region Events
    private void OnEnable()
    {
        NetworkManager.Singleton.OnConnectionEvent += OnUnityClientDisconnect;

    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnConnectionEvent -= OnUnityClientDisconnect;

    }
    #endregion

    #region Spawn Players
    public void HandleSpawnPlayers()
    {
        if (!IsServer) return;
        if (_playerPrefab == null) { Debug.LogError($"Player prefab is null, cannot spawn!"); return; }

        Debug.Log($"[SERVER] Spawning {NetworkManager.Singleton.ConnectedClientsIds.Count} players");

        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++)
        {
            ulong currentClient = NetworkManager.Singleton.ConnectedClientsIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;

            // Store the original podium position
            _originalPodiumPositions[currentClient] = _spawnPositions[i].position;

            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(currentClient, true);
        }

        InitializePlayers();
    }

    public void InitializePlayers()
    {
        activePlayers = NetworkManager.Singleton.ConnectedClientsIds
          .ToDictionary(clientId => clientId, clientId => false);

        playersRemaining = activePlayers.Count;
        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> Game initialized with {playersRemaining} players");

    }

    #endregion

    #region Eliminate Player
    [Rpc(SendTo.Server)]
    public void EliminatePlayerRPC(ulong clientId)
    {
        _gameManager.scoreManager.AwardFail(clientId);
        AudioManager.Instance.PlayerAudio(_playerSFX[PlayerState.Eliminated]);

        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject
            .GetComponent<IDamageable>()
            .Die();

        activePlayers[clientId] = true;
        playersRemaining--;


    }
    #endregion

    #region Move To Hot Seat
    public Coroutine MoveToHotSeat(ulong clientId, bool reverse = false)
    {
        if (_hotSeat == null) { Debug.LogWarning($"Hot seat transform is null, cannot move player!"); return null; }
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
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

        if (!networkManager.IsServer) return;
        Debug.Log($"<color={LogColours.Unity}>[PLAYER MANAGER]</color> {connectionEventData.ClientId} has left!");

    }
    #endregion

    #region Utility
    [Rpc(SendTo.ClientsAndHost)]
    public void HandleChangePodiumColorRPC(ulong clientId, Color colour)
    {
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetworkedController>().podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = colour;
    }

    public bool IsPlayerActive(ulong clientId)
    {
        return NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId) && activePlayers[clientId] == false;
    }

    public void UpdatePlayerRemaining(int newValue)
    {
        if (playersRemaining - newValue <= 0) { playersRemaining = 0; return; }
        playersRemaining += newValue;
    }
    #endregion

    #region Enum
    private enum PlayerState
    {
        Eliminated,
        CorrectGuess,
        IncorrectGuess,

    }
    #endregion

}