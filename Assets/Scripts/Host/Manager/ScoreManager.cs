using System.Collections.Generic;
using UnityEngine;
using Utility;

/// <summary>
/// Holds and manages all logic regarding to score tracking
/// </summary>
public class ScoreManager : NetworkedSingleton<ScoreManager>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    // INFO: Player Scores
    private Dictionary<ulong, int> playerScores = new();

    public void AwardPoints(ulong clientId, int points = 1)
    {
        playerScores[clientId] = playerScores.GetValueOrDefault(clientId) + points;
        Debug.Log($"<color={LogColours.Unity}>[SCORE MANAGER]</color> Player {clientId} now has {playerScores[clientId]} point(s)");

    }

    public int GetPlayerScore(ulong clientId) => playerScores.GetValueOrDefault(clientId, 0);

    public Dictionary<ulong, int> GetAllScores() => playerScores;


    public void ResetScores() => playerScores.Clear();


}