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

    private AnswerMenuUIManger _answerUIManager => AnswerMenuUIManger.Instance;

    // INFO: Stuff
    [HideInInspector] private CategorySO _currentCategory;
    [HideInInspector] private QuestionData _currentQuestion;
    [HideInInspector] private int _currentQuestionIndex = 0;


    [Space()]
    [Header("Player Tracking")]
    public HotPotatoManager hotPotatoManager;

    [SerializeField, ReadOnly] public int playersRemaining;
    private Dictionary<ulong, int> _playerScores = new();

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

    }

    private void OnDisable()
    {
        _eventManager.OnCountdownFinished -= hotPotatoManager.ProcessExplode;


    }
    #endregion

    #region Networking
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(DelayCoroutine(.1f, SpawnPlayers));

    }
    #endregion

    #region Spawn Players
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
            netObj.SpawnAsPlayerObject(currentClient, true);

        }

        HandleStartGameRPC();
    }
    #endregion

    #region Gameplay Loop
    // INFO: All players spawn now do shit!
    [Rpc(SendTo.Server)]
    private void HandleStartGameRPC()
    {
        BootstrapManager.Instance.sessionStateManager.UpdateSessionState(GameState.Playing);
        if (BootstrapManager.Instance.sessionStateManager.currentSessionState.Value != GameState.Playing) return;
        Debug.Log($"{_networkHelper.CheckPrivilege()} All players spawned ready to start!");

        GameStarted();

    }

    private void GameStarted()
    {
        playersRemaining = _networkHelper.networkManager.ConnectedClients.Count;
        HandleChooseCategory();

    }

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

    #region Choose Category

    #region Server
    // INFO: Select the starting category
    private void HandleChooseCategory()
    {
        NetworkObject selectionScreen = _networkHelper.networkManager.SpawnManager.InstantiateAndSpawn(
            _categorySelectionGO.GetComponent<NetworkObject>(),
            _networkHelper.networkManager.LocalClientId);

    }

    public void ProcessCategorySelectionServer(string selectedCategory)
    {
        // INFO: Store selected Category and current question
        _currentCategory = categoryContainer.categories.FirstOrDefault(category => category.categoryName == selectedCategory);
        _currentQuestion = _currentCategory.questions[_currentQuestionIndex];

        // INFO: Intialise the timer
        _timeRemaining = _currentCategory.GetTimeLimit(); // TODO: Add Timer
        _eventManager.OnCountdownFinished -= hotPotatoManager.ProcessExplode;
        _eventManager.OnCountdownFinished += hotPotatoManager.ProcessExplode;

        // INFO: Spawn the answers
        StartCoroutine(DelayCoroutine(.5f, HandleSpawnAnswers));
        _eventManager.OnCountdownStarted?.Invoke(_timeRemaining);

    }
    #endregion
    #endregion

    #region Chose Question 

    #region Server
    public void ProcessNextQuestion()
    {
        _currentQuestionIndex += 1;
        if (_currentQuestionIndex > _currentCategory.questions.Count - 1) { Debug.LogError($"<color={LogColours.Host}>[HOST]</color> This category doesn't have enough questions"); return; }
        _currentQuestion = _currentCategory.questions[_currentQuestionIndex];
        HandleSpawnAnswers();

    }
    #endregion

    #endregion

    #region Answers
    #region Spawn Answers
    private void HandleSpawnAnswers()
    {
        _answerUIManager.ClearPreviousAnswersRPC();
        _answerUIManager.SetQuestionTextRPC(_currentQuestion.question);

        // INFO: RPC no like complex data structures 🥹
        foreach (AnswerData answerData in _currentQuestion.answers)
        {
            AnswerMenuUIManger.Instance.AddAnswerRPC(answerData.answer);
        }


        NotifyChangePodiumRPC();

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyChangePodiumRPC()
    {
        // Debug.Log($"{_playerWithBanana.name}");
        NetworkManager.Singleton.ConnectedClients[hotPotatoManager._playerWithBanana.Value].PlayerObject.GetComponent<PlayerNetworkedController>().podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = Color.red;

    }

    #endregion

    #region Choose Answer

    #region Server
    public void ProcessAnswerChosen(string answer, ulong clientId)
    {
        // GUARD: Ensure the correct player guesses
        if (hotPotatoManager._playerWithBanana == null) return;
        if (clientId != hotPotatoManager._playerWithBanana.Value) return;
        _playerScores[clientId] = _playerScores.GetValueOrDefault(clientId) + 1;

        Debug.Log($"<color={LogColours.Host}>[HOST]</color> {hotPotatoManager._playerWithBanana.Value} selected: {answer}");

        bool correct = _currentQuestion.answers.Any(a => a.correctAnswer && a.answer == answer);

        if (correct)
        {
            ProcessCorrectGuess();
        }
        else
        {
            ProcessIncorrectGuess();
        }

    }

    private void ProcessCorrectGuess()
    {
        if (playersRemaining <= 1) { GameWin(); return; }
        NotifyAnswerResultRpc(true);
        hotPotatoManager.ProcessPassTheBomb();


    }

    private void ProcessIncorrectGuess()
    {
        NotifyAnswerResultRpc(false);
        hotPotatoManager.ProcessExplode();

    }

    #region Client
    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyAnswerResultRpc(bool wasCorrect)
    {
        string result = wasCorrect ? "got it!" : "was wrong!";
        Debug.Log($"<color={LogColours.Client}>[CLIENT]</color> {hotPotatoManager._playerWithBanana.Value} {result}");

    }

    #endregion

    #endregion



    public IEnumerator DelayCoroutine(float seconds, Action onCompleted = null)
    {
        yield return new WaitForSeconds(seconds);
        onCompleted?.Invoke();

    }
    #endregion

    #endregion
    #endregion

    private void GameWin()
    {
        Debug.Log($"YOu win!");
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