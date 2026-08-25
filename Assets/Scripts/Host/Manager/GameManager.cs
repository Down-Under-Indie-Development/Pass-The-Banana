using Utility;
using UnityEngine;
using System.Collections.Generic;
using PTB.Client.Player;
using Unity.Netcode;
using PTB.Networking;
using System.Collections;
using System;
using System.Data;
using Unity.VisualScripting;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Linq;
using Netcode.Transports.Facepunch;
using HealthSystem;

public class GameManager : NetworkedSingleton<GameManager>
{

    [field: Header("Game Settings")]
    [SerializeField, ReadOnly] private float _timeRemaining;
    [field: SerializeField] public CategoriesContainerSO categoryContainer { get; private set; }
    [SerializeField] private GameObject _categorySelectionGO;
    [SerializeField] private GameObject _uiCanvasGO;

    [Space()]
    [Header("Answers")]
    [SerializeField] private GameObject _answerGridGO;
    [SerializeField] private GameObject _answerTilePrefab;

    // INFO: Stuff
    [HideInInspector] private CategorySO _currentCategory;
    [HideInInspector] private QuestionData _currentQuestion;


    [Space()]
    [Header("Player Tracking")]
    [SerializeField, ReadOnly] private PlayerNetworkedController _playerWithBanana;

    [Space()]
    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> _spawnPositions = new();

    #region Networking Components
    // INFO: Network Components
    private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
    public GameState currentGameState => SessionStateManager.Instance.currentSessionState.Value;
    private BootstrapNetworkManager _bootstrapNetworkManager => BootstrapNetworkManager.Instance;
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
        yield return new WaitForSeconds(0.1f);
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

        Debug.Log($"[SERVER] Spawning {_bootstrapNetworkManager.connectedClientIds.Count} players");

        for (int i = 0; i < _bootstrapNetworkManager.connectedClientIds.Count; i++)
        {
            ulong currentClient = _bootstrapNetworkManager.connectedClientIds[i];
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
        GameStarted();

    }
    #endregion

    #region Host Pausing
    private bool _gamePaused = false;

    [Rpc(SendTo.ClientsAndHost)]  // INFO: Send to all clients AND the host
    public void BroadcastPauseStateRPC()
    {
        // INFO: Toggle the pause state
        _gamePaused = !_gamePaused;

        Debug.Log($"{(_gamePaused ? "Host paused the game!" : "Host has unpaused the game!")}");

        // INFO: Apply pause state to all connected players (including host)
        BootstrapNetworkManager.Instance.ForEachPlayer(player => player._pauseMenuGO.SetActive(false), false);
        BootstrapNetworkManager.Instance.ForEachPlayer(player => player.ApplyPauseState(_gamePaused));


    }
    #endregion

    private void GameStarted()
    {
        _playerWithBanana = BootstrapNetworkManager.Instance.connectedClients[0].PlayerObject.GetComponent<PlayerNetworkedController>();
        SelectCategory();

    }

    #region Select Category
    // INFO: Select the starting category
    private void SelectCategory()
    {
        NetworkObject selectionScreen = _networkHelper.networkManager.SpawnManager.InstantiateAndSpawn(
            _categorySelectionGO.GetComponent<NetworkObject>(),
            _networkHelper.networkManager.LocalClientId);

    }

    public void OnCategorySelected(string selectedCategory)
    {
        _currentCategory = categoryContainer.categories.FirstOrDefault(category => category.categoryName == selectedCategory);
        _currentQuestion = _currentCategory.questions[0];
        _timeRemaining = _currentCategory.GetTimeLimit(); // TODO: Add Timer
        SpawnAnswers();

    }
    #endregion


    #region Answers
    #region Spawn Answers
    private void SpawnAnswers()
    {
        if (_answerTilePrefab == null) { Debug.LogWarning($"Answer prefab is null"); return; }
        if (_answerGridGO == null) { Debug.LogWarning($"Answer grid game object is null"); return; }

        foreach (AnswerData answerData in _currentQuestion.answers)
        {
            NetworkObject answerNetworkObject = _networkHelper.networkManager.SpawnManager.InstantiateAndSpawn(
    _answerTilePrefab.GetComponent<NetworkObject>(),
    _networkHelper.networkManager.LocalClientId
);
            // Tell clients to parent this
            ParentTileClientRPC(answerNetworkObject.NetworkObjectId, answerData.answer);

        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ParentTileClientRPC(ulong tileNetworkObjectId, string answer)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tileNetworkObjectId, out NetworkObject tileNetObj))
        {
            Debug.LogWarning($"Could not find tile with ID {tileNetworkObjectId}");
            return;
        }

        // Move to same scene as grid
        if (!NetworkManager.Singleton.IsServer) SceneManager.MoveGameObjectToScene(tileNetObj.gameObject, _answerGridGO.scene);
        tileNetObj.name = $"{answer}";
        tileNetObj.GetComponent<AnswerTile>().SetAnswer(answer);
        tileNetObj.transform.SetParent(_answerGridGO.transform);

        tileNetObj.transform.localPosition = Vector3.zero;
        tileNetObj.transform.localScale = Vector3.one;

    }
    #endregion

    #region Select Answer
    public void OnAnswerSelectedRPC(string answer, ulong clientId)
    {
        // GUARD: Ensure the correct player guesses
        if (clientId != _playerWithBanana.OwnerClientId) return;

        Debug.Log($"{_playerWithBanana.name} selected: {answer}");
        HandleAnswerSelection(answer);

    }

    private void HandleAnswerSelection(string answer)
    {
        bool correct = _currentQuestion.answers.Any(a => a.correctAnswer && a.answer == answer);
        if (!correct) { Explode(); return; }
        PassTheBomb();

    }

    private void Explode()
    {
        Debug.Log($"You got it wrong, bitch!");
        _playerWithBanana.GetComponent<IDamageable>().Die();
        // TODO: Create a list of remaining players (Just an int)

    }

    private void PassTheBomb()
    {
        Debug.Log($"You got it, bitch!");
        _playerWithBanana = _networkHelper.networkManager.ConnectedClients[1].PlayerObject.GetComponent<PlayerNetworkedController>();

    }
    #endregion
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