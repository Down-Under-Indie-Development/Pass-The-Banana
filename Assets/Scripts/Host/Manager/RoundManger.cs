using System;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using UnityEngine;
using Utility;

#region Our Little Secret
[assembly: DictionaryDisplayForType(
    typeof(Dictionary<ulong, ScoreData>),
    keyLabel = "ID",
    valueLabel = "Stats"

)]
#endregion

public class RoundManger : NetworkedSingleton<RoundManger>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    // INFO: Current round tracking
    public int currentRound { get; private set; } = 1;

    [Header("Category Selection")]
    [SerializeField] private GameObject _categorySelectionGO;

    [Header("Round End Screens")]
    [SerializeField] private GameObject _summaryGO;

    [Header("Match Summary Settings")]
    [SerializeField] private float _perPlayerTimer = 0.25f;
    [SerializeField] private float _mathSummaryEndPause = 2f;

    [Header("Timer Settings")]
    [SerializeField] private bool showCountdown;

    [field: Header("Round Score Tracking")]
    [field: SerializeField, DictionaryDisplay(keyLabel = "Round #", valueLabel = "Player Stats")] public Dictionary<int, Dictionary<ulong, ScoreData>> currentRoundData { get; private set; } = new();

    public void StartRound()
    {
        if (currentRound == 1) InitialiseRoundScoreTracker();

        Debug.Log($"Starting Round {currentRound}/{_gameManager.currentGameLobbyData.numberOfRounds}");
        _gameManager.bombManager.SelectStartingPlayer();
        Invoke(nameof(ShowCategorySelection), .1f); // INFO: Allow time for syncing

    }

    #region Round Score Tracking
    // INFO: Create the round tracking dictionary
    private void InitialiseRoundScoreTracker()
    {
        for (int i = 1; i < _gameManager.bootstrapNetworkManager.lobbyData.numberOfRounds + 1; i++)
        {
            if (!currentRoundData.ContainsKey(i)) currentRoundData[i] = new();
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                currentRoundData[i][clientId] = ScoreData.Empty();
            }

        }

    }
    #endregion

    #region Category Selection
    private void ShowCategorySelection()
    {
        NetworkObject selectionScreen = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
           _categorySelectionGO.GetComponent<NetworkObject>(),
           NetworkManager.Singleton.LocalClientId);
    }

    public void ProcessChosenCategory(string selectedCategory)
    {
        _gameManager.questionManager.SelectCategory(selectedCategory);

        // INFO: Spawn the answers;
        _eventManager.OnCountdownStarted?.Invoke(_gameManager.questionManager.GetCurrentCategory().GetTimeLimit(), showCountdown);

    }
    #endregion

    #region Answer Selection
    public void ProcessAnswerChosen(string answer, ulong clientId)
    {
        // GUARD: Ensure the correct player guesses
        if (!_gameManager.bombManager.IsPlayerWithBomb(clientId)) return;

        Debug.Log($"<color={LogColours.Host}>[HOST]</color> {_gameManager.bombManager.playerWithBanana.Value} selected: {answer}");

        bool correct = _gameManager.questionManager.ValidateAnswer(answer);

        if (correct)
        {
            ProcessCorrectGuess(clientId);
        }
        else
        {
            ProcessIncorrectGuess(clientId);
        }

    }


    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyAnswerResultRpc(bool wasCorrect)
    {
        string result = wasCorrect ? "got it!" : "was wrong!";
        Debug.Log($"<color={LogColours.Client}>[BROADCAST]</color> {_gameManager.bombManager.playerWithBanana.Value} {result}");

    }

    public void ProcessCorrectGuess(ulong clientId)
    {
        NotifyAnswerResultRpc(true);
        _gameManager.scoreManager.AwardPoints(clientId); // INFO: Score
        currentRoundData[currentRound][clientId].points++;

        _gameManager.bombManager.ProcessPassTheBomb();

    }

    public void ProcessIncorrectGuess(ulong clientId)
    {
        NotifyAnswerResultRpc(false);
        currentRoundData[currentRound][clientId].fails++;
        _gameManager.bombManager.ProcessExplode();

    }
    #endregion

    #region End Of Round
    [ContextMenu("Start Next Round")]
    public void ProcessNextRound()
    {
        HandleServerEndOfRound();
        if (IsGameOver()) { HandleGameOver(); return; }
        SpawnMatchSummary(_gameManager.roundManager.currentRoundData[currentRound], ProgressToNextRound);

    }

    private void HandleServerEndOfRound()
    {
        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> Round {currentRound} done, processing next round");
        Timer.Instance.StopCountdown();

        ulong previousPlayer = _gameManager.bombManager.previousPlayerWithBanana;
        _gameManager.playerManager.MoveToHotSeat(previousPlayer, true);
        _gameManager.playerManager.HandleChangePodiumColorRPC(previousPlayer, Color.white);

        _gameManager.questionManager.ClearAnswers();

    }

    private NetworkObject SpawnMatchSummary(Dictionary<ulong, ScoreData> dataSet, Action onCompleted = null)
    {
        NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(_summaryGO.GetComponent<NetworkObject>(), NetworkManager.Singleton.LocalClientId);
        MatchSummary matchSummary = networkObject.GetComponent<MatchSummary>();
        matchSummary.StartCoroutine(matchSummary.MatchSummaryCoroutine(dataSet, _perPlayerTimer, _mathSummaryEndPause, onCompleted));

        return networkObject;

    }

    public void ProgressToNextRound()
    {
        currentRound++;
        StartRound();

    }
    #endregion

    #region Game Over
    public void HandleGameOver()
    {
        _gameManager.questionManager.ClearAnswers();

        if (_gameManager.playerManager.activePlayers.Count <= 0)
        {
            Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> ALL PLAYERS ELIMINATED ({_gameManager.playerManager.activePlayers.Count})");

        }
        else
        {
            Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> All rounds finished!");

        }

        // INFO: Show all players score
        SpawnMatchSummary(_gameManager.scoreManager.GetAllPlayerScores(), ReturnToLobby);

        // INFO: Destroy player game objects
        _gameManager.bootstrapNetworkManager.ForEachPlayer(p => Destroy(p.gameObject));

    }
    #endregion

    #region Utility
    private void ReturnToLobby() => _gameManager.bootstrapNetworkManager.ReturnToLobby();
    private bool IsGameOver() => currentRound >= _gameManager.currentGameLobbyData.numberOfRounds;

    #endregion

}