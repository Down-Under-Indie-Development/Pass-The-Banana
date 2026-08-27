using System.Linq;
using Unity.Netcode;
using UnityEngine;
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

    public void StartRound()
    {
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
        if (_gameManager.playerManager.PlayersRemaining <= 1) { EndRound(true); return; }
        NotifyAnswerResultRpc(true);
        _gameManager.scoreManager.AwardPoints(clientId); // INFO: Score
        _gameManager.bombManager.ProcessPassTheBomb();

    }

    public void ProcessIncorrectGuess(ulong clientId)
    {
        NotifyAnswerResultRpc(false);
        _gameManager.bombManager.ProcessExplode();
    }
    #endregion


    #region End Of Round
    private void EndRound(bool won)
    {
        if (won)
            GameNetworkManager.Instance.EndGame();
    }

    [ContextMenu("Start Next Round")]
    public void ProcessNextRound()
    {
        Timer.Instance.StopCountdown();
        if (_endOfRoundSummaryGO != null) _endOfRoundSummaryGO.SetActive(true);

        _gameManager.playerManager.MoveToHotSeat(_gameManager.bombManager.previousPlayerWithBanana, true);
        _gameManager.playerManager.HandleChangePodiumColor(_gameManager.bombManager.previousPlayerWithBanana, Color.white);
        _gameManager.questionManager.ClearAnswers();

        StartRound();
        Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> Starting next round");

    }

    #endregion

}