using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using Utility;

public class RoundManger : NetworkedSingleton<RoundManger>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;
    private float _fuseTime;

    [Header("Category Selection")]
    [SerializeField] private GameObject _categorySelectionGO;

    [Header("Round End Screens")]
    [SerializeField] private GameObject _winScreenGO;
    [SerializeField] private GameObject _endOfRoundSummaryGO;

    public int currentRound { get; private set; } = 1;
    public Dictionary<ulong, ScoreData> currentRoundData { get; private set; } = new();

    public void StartRound()
    {
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            currentRoundData[clientId] = ScoreData.Empty();
        }

        Debug.Log($"Starting Round {currentRound}/{_gameManager.currentGameLobbyData.numberOfRounds}");
        _gameManager.bombManager.SelectStartingPlayer();
        StartCoroutine(_gameManager.DelayCoroutine(.1f, ShowCategorySelection)); // INFO: Allow time for syncing

    }

    #region Category Selection
    private void ShowCategorySelection()
    {
        NetworkObject selectionScreen = NetworkManager.SpawnManager.InstantiateAndSpawn(
           _categorySelectionGO.GetComponent<NetworkObject>(),
           NetworkManager.LocalClientId);
    }

    public void ProcessChosenCategory(string selectedCategory)
    {
        _gameManager.questionManager.SelectCategory(selectedCategory);

        // INFO: Intialise the timer
        _fuseTime = _gameManager.questionManager.GetCurrentCategory().GetTimeLimit();
        _eventManager.OnCountdownFinished -= _gameManager.bombManager.ProcessExplode;
        _eventManager.OnCountdownFinished += _gameManager.bombManager.ProcessExplode;

        // INFO: Spawn the answers;
        _eventManager.OnCountdownStarted?.Invoke(_fuseTime);

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
        // if (_gameManager.playerManager.PlayersRemaining <= 1) { HandleGameOver(); return; }
        NotifyAnswerResultRpc(true);
        _gameManager.scoreManager.AwardPoints(clientId); // INFO: Score
        currentRoundData[clientId].points += 1;
        _gameManager.bombManager.ProcessPassTheBomb();

    }

    public void ProcessIncorrectGuess(ulong clientId)
    {
        NotifyAnswerResultRpc(false);
        _gameManager.scoreManager.AwardFail(clientId);
        currentRoundData[clientId].fails += 1;
        _gameManager.bombManager.ProcessExplode();

    }
    #endregion


    #region End Of Round
    private NetworkObject SpawnMatchSummary(Dictionary<ulong, ScoreData> dataSet, Action onCompleted = null)
    {
        NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(_endOfRoundSummaryGO.GetComponent<NetworkObject>(), NetworkManager.Singleton.LocalClientId);
        MatchSummary matchSummary = networkObject.GetComponent<MatchSummary>();
        matchSummary.StartCoroutine(matchSummary.MatchSummaryCoroutine(dataSet, onCompleted));

        return networkObject;

    }

    [ContextMenu("Start Next Round")]
    public void ProcessNextRound()
    {
        HandleServerEndOfRound();
        if (IsGameOver()) { HandleGameOver(); return; }
        NetworkObject networkObject = SpawnMatchSummary(_gameManager.roundManager.currentRoundData, ProgressToNextRound);

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

    public void ProgressToNextRound()
    {
        currentRound++;
        currentRoundData.Clear();
        StartRound();

    }

    private bool IsGameOver() => currentRound >= _gameManager.currentGameLobbyData.numberOfRounds;

    private void HandleGameOver()
    {
        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> All rounds finished!");
        SpawnMatchSummary(_gameManager.scoreManager.GetAllPlayerScores(), ReturnToLobby);
        _gameManager.bootstrapNetworkManager.ForEachPlayer(p => NetworkObject.Destroy(p.gameObject));

    }


    private void ReturnToLobby()
    {

        _gameManager.bootstrapNetworkManager.ChangeNetworkScene("MainMenuScene", "TestScene");
    }

    #endregion

}