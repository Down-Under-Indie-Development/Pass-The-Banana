using System;
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

    [SerializeField] private Dictionary<ulong, ScoreData> _playerScores = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        foreach (ulong client in NetworkManager.ConnectedClientsIds)
        {
            _playerScores[client] = ScoreData.Empty();

        }

    }

    #region Points
    public ScoreData GetPlayerScore(ulong clientId)
    {
        if (!_playerScores.TryGetValue(clientId, out ScoreData scoreData))
        {
            Debug.LogWarning($"No score found for clientId {clientId}. Available clients: {string.Join(", ", _playerScores.Keys)}");
            return ScoreData.Empty();
        }

        Debug.Log($"Getting score for {clientId}: {scoreData}");
        return scoreData;

    }

    public void AwardPoints(ulong clientId, int points = 1)
    {
        if (!EnsurePlayerScoreExists(clientId))
            return;

        _playerScores[clientId].points += points;
        Debug.Log($"<color={LogColours.Unity}>[SCORE MANAGER]</color> Player {clientId} now has {_playerScores[clientId].points} point(s)");
    }

    #endregion

    #region Pass
    public void RecordPass(ulong clientId)
    {
        if (!EnsurePlayerScoreExists(clientId))
            return;

        _playerScores[clientId].passes++;
    }
    #endregion

    #region Fails
    public void AwardFail(ulong clientId, int amount = 1)
    {
        if (EnsurePlayerScoreExists(clientId))
            _playerScores[clientId].fails += amount;

    }
    #endregion

    public Dictionary<ulong, ScoreData> GetAllPlayerScores() => _playerScores;
    public void ResetAllScores() => _playerScores.Clear();

    private bool EnsurePlayerScoreExists(ulong clientId)
    {
        if (_playerScores.ContainsKey(clientId))
            return true;

        _playerScores[clientId] = ScoreData.Empty();
        return true;

    }

    public int GetPlayerPlace(ulong clientId, Dictionary<ulong, ScoreData> dataSet)
    {
        // Guard: Return a fallback value if the player isn't in the dataset
        if (!dataSet.ContainsKey(clientId))
            return -69;

        ScoreData targetPlayer = dataSet[clientId];

        // INFO: Count how many players performed better than player
        int playersAhead = dataSet.Values.Count(other =>
            // INFO: Condition 1: They have more points
            other.points > targetPlayer.points ||

            // INFO: Condition 2: They have the same points, but fewer fails (better performance)
            (other.points == targetPlayer.points && other.fails < targetPlayer.fails)
        );

        // INFo: Placement is 1 + the number of players ahead of them
        return playersAhead + 1;
    }


}

[Serializable]
public class ScoreData
{
    public int points;
    public int passes;
    public int fails;

    public static ScoreData Empty() => new();

    public override string ToString() => $"Points: {points} | Passes: {passes} | Fails: {fails}";
}