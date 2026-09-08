using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;
using PTB.Enums;
using DocNet.Utility;

namespace PTB.Managers
{
    /// <summary>
    /// Holds and manages all logic regarding score tracking
    /// </summary>
    public class ScoreManager : NetworkedSingleton<ScoreManager>
    {
        [field: Header("Scoring Settings")]
        [field: SerializeField] public int failPenalty { get; private set; } = 50;

        [SerializeField] private Dictionary<ulong, ScoreData> _playerScores = new();

        public override void OnNetworkSpawn()
        {
            foreach (ulong client in NetworkManager.ConnectedClientsIds)
            {
                _playerScores[client] = ScoreData.Empty();

            }

            if (!IsServer) { enabled = false; return; }
        }

        public ScoreData GetPlayerScoreData(ulong clientId)
        {
            if (!_playerScores.TryGetValue(clientId, out ScoreData scoreData))
            {
                Debug.LogWarning($"No score found for clientId {clientId}. Available clients: {string.Join(", ", _playerScores.Keys)}");
                return ScoreData.Empty();
            }

            Debug.Log($"Getting score for {clientId}: {scoreData}");
            return scoreData;

        }

        public int GetPlayerPlace(ulong clientId, Dictionary<ulong, ScoreData> dataSet)
        {
            if (!dataSet.ContainsKey(clientId))
                return -69;

            ScoreData targetPlayer = dataSet[clientId];
            int targetScore = targetPlayer.correctGuesses - (targetPlayer.incorrectGuesses * failPenalty);  // FIXED

            int playersAhead = dataSet.Values.Count(other =>
                other.correctGuesses - (other.incorrectGuesses * failPenalty) > targetScore  // FIXED
            );

            return playersAhead + 1;
        }

        public int GetPlayerTotalScore(ulong clientId)
        {
            if (!_playerScores.ContainsKey(clientId)) { Debug.LogError($"No key found for {clientId}"); return -1; }
            ScoreData playerScoreData = GetPlayerScoreData(clientId);
            return playerScoreData.correctGuesses - (playerScoreData.incorrectGuesses * failPenalty);

        }

        #region Points
        public void AwardPoints(ulong clientId)
        {
            if (!EnsurePlayerScoreExists(clientId))
                return;

            _playerScores[clientId].correctGuesses++;
            Debug.Log($"<color={LogColours.Unity}>[SCORE MANAGER]</color> Player {clientId} now has {_playerScores[clientId].correctGuesses} point(s)");
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
        public void AwardFail(ulong clientId)
        {
            if (EnsurePlayerScoreExists(clientId))
                _playerScores[clientId].incorrectGuesses++;

        }
        #endregion

        #region Utility
        private bool EnsurePlayerScoreExists(ulong clientId)
        {
            if (_playerScores.ContainsKey(clientId))
                return true;

            _playerScores[clientId] = ScoreData.Empty();
            return true;

        }

        public Dictionary<ulong, ScoreData> GetAllPlayerScores() => _playerScores;
        public void ResetAllScores() => _playerScores.Clear();

        #endregion


    }
}

