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

public class GameManager : NetworkedSingleton<GameManager>
{


    [Space()]
    [Header("Game Settings")]
    [SerializeField] private List<CategorySO> _categories;
    [SerializeField] private GameObject _answerGridGO;
    [SerializeField] private GameObject _answerTilePrefab;
    [HideInInspector] private CategorySO _currentCategory;
    [HideInInspector] private QuestionSO _currentQuestions;


    [Space()]
    [Header("Player Tracking")]
    [SerializeField] private PlayerNetworkedController _playerWithBanana;

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
        SpawnAnswersGO();

    }

    #region Spawn Answers
    private void SpawnAnswersGO()
    {
        if (_answerTilePrefab == null) { Debug.LogWarning($"Answer prefab is null"); return; }
        if (_answerGridGO == null) { Debug.LogWarning($"Answer grid game object is null"); return; }

        _currentCategory = _categories[0];
        _currentQuestions = _currentCategory.questions[0];
        foreach (AnswerData answerData in _currentCategory.questions[0].answers)
        {
            NetworkObject answerNetworkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
    UnityNetworkHelper.Instance.networkPrefabsToSpawn[2].GetComponent<NetworkObject>(),
    NetworkManager.Singleton.LocalClientId
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

        // AnswerTile answerTile = answerNetworkObject.GetComponent<AnswerTile>();
        tileNetObj.GetComponent<AnswerTile>().SetAnswer(answer);

        // Now parent it
        tileNetObj.transform.SetParent(_answerGridGO.transform);
        tileNetObj.transform.localPosition = Vector3.zero;
        tileNetObj.transform.localScale = Vector3.one;

    }
    #endregion

    #region Selecte Question
    public void OnQuestionSelectedRPC(string answer, ulong clientId)
    {
        // GUARD: Ensure the correct player guesses
        if (clientId != _playerWithBanana.OwnerClientId) return;

        Debug.Log($"{_playerWithBanana.name} selected: {answer}");
        HandleAnswerSelection(answer);

    }

    private void HandleAnswerSelection(string answer)
    {
        bool correct = _currentQuestions.answers.Any(a => a.correctAnswer && a.answer == answer);

        if (correct)
            Debug.Log($"You got it, bitch!");
        else
            Debug.Log($"You got it wrong, bitch!");

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