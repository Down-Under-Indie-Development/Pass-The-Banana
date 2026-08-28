using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    private int _currentRound = 1;

    public void StartRound()
    {
        Debug.Log($"Starting Round {_currentRound}/{_gameManager.currentGameLobbyData.numberOfRounds}");
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
        _gameManager.bombManager.ProcessPassTheBomb();

    }

    public void ProcessIncorrectGuess(ulong clientId)
    {
        NotifyAnswerResultRpc(false);
        _gameManager.scoreManager.AwardFail(clientId);
        _gameManager.bombManager.ProcessExplode();

    }
    #endregion


    #region End Of Round

    [ContextMenu("Start Next Round")]
    public void ProcessNextRound()
    {

        NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(_endOfRoundSummaryGO.GetComponent<NetworkObject>(), NetworkManager.Singleton.LocalClientId);
        _endOfRoundSummaryGO.SetActive(true);
        HandleServerEndOfRound();
        ClientSideRPC(networkObject.NetworkObjectId);
        if (IsServer) ProgressToNextRound();

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClientSideRPC(ulong tileNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tileNetworkObjectId, out NetworkObject tileNetObj))
        {
            Debug.LogWarning($"Could not find tile with ID {tileNetworkObjectId}");
            return;
        }


        if (!NetworkManager.Singleton.IsServer) SceneManager.MoveGameObjectToScene(tileNetObj.gameObject, gameObject.scene);

    }

    private void HandleServerEndOfRound()
    {

        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> Round {_currentRound} done, processing next round");
        Timer.Instance.StopCountdown();

        ulong previousPlayer = _gameManager.bombManager.previousPlayerWithBanana;
        _gameManager.playerManager.MoveToHotSeat(previousPlayer, true);
        _gameManager.playerManager.HandleChangePodiumColorRPC(previousPlayer, Color.white);

        _gameManager.questionManager.ClearAnswers();

    }

    private void ProgressToNextRound()
    {
        _currentRound++;

        if (IsGameOver())
        {
            HandleGameOver();
            return;
        }

        StartRound();
    }

    private bool IsGameOver() => _currentRound >= _gameManager.currentGameLobbyData.numberOfRounds;

    private void HandleGameOver()
    {
        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> All rounds finished!");
        // TODO: Implement game over logic
    }

    #endregion

}