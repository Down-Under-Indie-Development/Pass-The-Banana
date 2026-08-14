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
    #endregion

    private EventManager _eventManager => EventManager.Instance;

    [Header("Game Settings")]
    [SerializeField] private List<CategorySO> _categories;

    [Header("Player Tracking")]
    [SerializeField] private PlayerNetworkedController _playerWithBanana;
    [SerializeField] private GameObject _playerPrefab;
    [field: SerializeField] public GameState currentGameState { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
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
        _eventManager.OnGameStart -= RequestStartGameRPC;

    }
    #endregion

    private bool ran;
    private void Update()
    {
        if (!ran)
            if (NetworkManager.Singleton.IsServer && SceneManager.GetActiveScene().name == "Test Scene") { RequestStartGameRPC(); ran = true; }

    }

    [Rpc(SendTo.Server)]
    private void RequestStartGameRPC()
    {
        currentGameState = GameState.Playing;
        if (!IsHost) return;
        SpawnPlayers();

    }

    private void SpawnPlayers()
    {
        if (!IsHost) return;
        if (_playerPrefab == null) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject instance = Instantiate(_playerPrefab);
            instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        }
    }
}


public enum GameState
{
    MainMenu,
    Lobby,
    Playing,


}