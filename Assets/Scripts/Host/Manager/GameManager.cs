using Utility;
using UnityEngine;
using System.Collections.Generic;
using PTB.Client.Player;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{

    #region Singleton
    public static GameManager Instance;
    private EventManager _eventManager => EventManager.Instance;
    #endregion


    [Header("Game Settings")]
    [SerializeField] private List<CategorySO> _categories;

    [Header("Player Tracking")]
    [SerializeField] private PlayerNetworkedController _playerWithBanana;
    [SerializeField] private GameObject _playerPrefab;

    [Header("Game State")]
    [field: SerializeField] public GameState currentGameState { get; set; }

    [Header("Player Settings")]
    [SerializeField] private List<Transform> _spawnPositions = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(this);
        }
        else
        {
            // Destroy(gameObject);
            NetworkObject.Despawn(true);
        }

    }

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

    public override void OnNetworkSpawn()
    {

        if (!IsServer) return;
        RequestStartGameRPC();

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RequestStartGameRPC()
    {
        currentGameState = GameState.Playing;
        SpawnPlayers();

    }

    private void SpawnPlayers()
    {
        if (!IsServer) return;
        if (_playerPrefab == null) return;
        Debug.Log($"Connected clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}");


        List<ulong> clientIds = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong currentClient = clientIds[i];
            GameObject instance = Instantiate(_playerPrefab);
            instance.transform.position = _spawnPositions[i].position;
            instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(currentClient);


        }
    }
}


public enum GameState
{
    MainMenu,
    Lobby,
    Playing,


}