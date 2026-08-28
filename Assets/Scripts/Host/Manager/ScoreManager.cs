using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Utility;

/// <summary>
/// Holds and manages all logic regarding score tracking
/// </summary>
public class ScoreManager : NetworkedSingleton<ScoreManager>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    private Dictionary<ulong, ScoreData> playerScores = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        foreach (ulong client in NetworkManager.ConnectedClientsIds)
        {
            playerScores[client] = ScoreData.Empty();

        }

    }

    #region Points
    public ScoreData GetPlayerScore(ulong clientId)
    {
        if (!playerScores.TryGetValue(clientId, out ScoreData scoreData))
        {
            Debug.LogWarning($"No score found for clientId {clientId}. Available clients: {string.Join(", ", playerScores.Keys)}");
            return ScoreData.Empty();
        }

        Debug.Log($"Getting score for {clientId}: {scoreData}");
        return scoreData;

    }

    public void AwardPoints(ulong clientId, int points = 1)
    {
        if (!EnsurePlayerScoreExists(clientId))
            return;

        playerScores[clientId].points += points;
        Debug.Log($"<color={LogColours.Unity}>[SCORE MANAGER]</color> Player {clientId} now has {playerScores[clientId].points} point(s)");
    }

    #endregion

    #region Pass
    public void RecordPass(ulong clientId)
    {
        if (!EnsurePlayerScoreExists(clientId))
            return;

        playerScores[clientId].passes++;
    }
    #endregion

    #region Fails
    public void AwardFail(ulong clientId, int amount = 1)
    {
        EnsurePlayerScoreExists(clientId);
        playerScores[clientId].fails += amount;

    }
    #endregion

    public Dictionary<ulong, ScoreData> GetAllPlayerScores() => playerScores;
    public void ResetAllScores() => playerScores.Clear();

    private bool EnsurePlayerScoreExists(ulong clientId)
    {
        if (playerScores.ContainsKey(clientId))
            return true;

        playerScores[clientId] = new ScoreData();
        return true;
    }

    public int GetPlayerPlace(ulong clientId, Dictionary<ulong, ScoreData> dataSet)
    {
        if (!dataSet.ContainsKey(clientId))
            return -69;

        return dataSet.Values.Count(s => s.points > dataSet[clientId].points) + 1;
    }

}

public class ScoreData
{
    public int points;
    public int passes;
    public int fails;

    public static ScoreData Empty() => new();

    public override string ToString() => $"Points: {points} | Passes: {passes} | Fails: {fails}";
}