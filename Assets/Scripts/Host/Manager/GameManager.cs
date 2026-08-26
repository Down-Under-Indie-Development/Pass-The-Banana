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
    [SerializeField, ReadOnly]
    public NetworkVariable<ulong> _playerWithBanana = new NetworkVariable<ulong>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    [SerializeField, ReadOnly] private int _playersRemaining;
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
        // _eventManager.OnQuestionFinished += DisplayAnswers;
        _eventManager.OnGameStart += RequestStartGameRPC;
        _playerWithBanana.OnValueChanged += HandleValueChanged;

    }

    private void OnDisable()
    {
        if (_eventManager == null) return;
        _eventManager.OnGameStart -= RequestStartGameRPC;
        _playerWithBanana.OnValueChanged += HandleValueChanged;
        _eventManager.OnCountdownFinished -= Explode;


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
            netObj.SpawnAsPlayerObject(currentClient, true);

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
        _playersRemaining = _networkHelper.networkManager.ConnectedClients.Count;
        _playerWithBanana.Value = BootstrapNetworkManager.Instance.connectedClients[0].ClientId;
        ChooseCategory();

    }


    #region Choose Category
    // INFO: Select the starting category
    private void ChooseCategory()
    {
        NetworkObject selectionScreen = _networkHelper.networkManager.SpawnManager.InstantiateAndSpawn(
            _categorySelectionGO.GetComponent<NetworkObject>(),
            _networkHelper.networkManager.LocalClientId);

    }

    public void OnCategoryChosen(string selectedCategory)
    {
        if (!IsServer) return;

        _currentCategory = categoryContainer.categories.FirstOrDefault(category => category.categoryName == selectedCategory);
        _currentQuestion = _currentCategory.questions[_currentQuestionIndex];
        _timeRemaining = _currentCategory.GetTimeLimit(); // TODO: Add Timer
        _eventManager.OnCountdownFinished -= Explode;
        _eventManager.OnCountdownFinished += Explode;

        // INFO: Add a delay
        StartCoroutine(DelayCoroutine(.5f, SpawnAnswers));

    }
    #endregion

    #region Chose Question 
    private void ChoseQuestion()
    {
        _currentQuestionIndex += 1;
        if (_currentQuestionIndex > _currentCategory.questions.Count - 1) { Debug.LogError($"This category doesn't have enough questions"); return; }
        _currentQuestion = _currentCategory.questions[_currentQuestionIndex];
        SpawnAnswers();
        _eventManager.OnCountdownStarted?.Invoke(_timeRemaining);


    }
    #endregion

    #region Answers
    #region Spawn Answers
    private void SpawnAnswers()
    {
        _answerUIManager.ClearPreviousAnswersRPC();
        _answerUIManager.SetQuestionTextRPC(_currentQuestion.question);

        // INFO: RPC no like complex data structures 🥹
        foreach (AnswerData answerData in _currentQuestion.answers)
        {
            AnswerMenuUIManger.Instance.AddAnswerRPC(answerData.answer);
        }


        ChangePodiumRPC();

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangePodiumRPC()
    {
        // Debug.Log($"{_playerWithBanana.name}");
        NetworkManager.Singleton.ConnectedClients[_playerWithBanana.Value].PlayerObject.GetComponent<PlayerNetworkedController>().podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = Color.red;

    }

    #endregion

    #region Choose Answer
    public void OnAnswerChosen(string answer, ulong clientId)
    {
        // GUARD: Ensure the correct player guesses
        if (_playerWithBanana == null) return;
        if (clientId != _playerWithBanana.Value) return;
        _playerScores[clientId] = _playerScores.GetValueOrDefault(clientId) + 1;

        Debug.Log($"{_playerWithBanana.Value} selected: {answer}");
        HandleAnswerSelection(answer);

    }

    private void HandleAnswerSelection(string answer)
    {
        bool correct = _currentQuestion.answers.Any(a => a.correctAnswer && a.answer == answer);
        if (!correct) { InCorrectGuess(); return; }
        CorrectGuess();

    }

    private void CorrectGuess()
    {
        Debug.Log($"You got it, bitch!");
        if (_playersRemaining <= 1) { GameWin(); return; }
        PassTheBomb();

    }
    private void InCorrectGuess()
    {
        Debug.Log($"You got it wrong, bitch!");
        Explode();
    }

    #region Utility
    private void Explode()
    {
        if (_playerWithBanana == null) return;
        NetworkManager.Singleton.ConnectedClients[_playerWithBanana.Value].PlayerObject.GetComponent<PlayerNetworkedController>().GetComponent<IDamageable>().Die();
        _playersRemaining -= 1;
        PassTheBomb();

    }

    private void HandleValueChanged(ulong oldValue, ulong newValue)
    {
        if (IsServer) { Debug.Log($"{UnityNetworkHelper.Instance.CheckPrivilege()} Player with the banana has been changed to {newValue}"); return; }
        Debug.Log($"{UnityNetworkHelper.Instance.CheckPrivilege()} Syncing player with the banana from host ({newValue})");

    }

    private void PassTheBomb()
    {
        if (_networkHelper.networkManager.ConnectedClients.Count <= 1)
        {
            _playerWithBanana.Value = _networkHelper.networkManager.ConnectedClients[0].ClientId;
        }
        else
        {
            _playerWithBanana.Value = _networkHelper.networkManager.ConnectedClients[1].ClientId;
        }

        StartCoroutine(DelayCoroutine(0.1f, ChoseQuestion)); // INFO: Add delay for client update

    }

    private IEnumerator DelayCoroutine(float seconds, Action onCompleted = null)
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


    #region Utility

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