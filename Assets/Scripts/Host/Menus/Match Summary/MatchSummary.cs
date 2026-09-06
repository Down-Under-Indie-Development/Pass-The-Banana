using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using PTB.Enums;
using PTB.Managers;
using Steamworks;
using Doc.Networking.Unity;
using Doc.Networking.Steam;
using System.Runtime.ExceptionServices;
using Doc.Networking;
using System.Linq;

namespace PTB.Menus
{
    public class MatchSummary : NetworkBehaviour
    {
        private GameNetworkManager _gameManager => GameNetworkManager.Instance;
        private ScoreManager _scoreManager => ScoreManager.Instance;

        [Header("Round Info")]
        [SerializeField] private TextMeshProUGUI _roundStatusTxt;
        [SerializeField] private TextMeshProUGUI _nextRoundTxt;


        [Header("Player Panel")]
        [SerializeField] private GameObject _playerContentGO;
        [SerializeField] private GameObject _playerCardPrefab;

        public IEnumerator MatchSummaryCoroutine(Dictionary<ulong, ScoreData> dataSet, float perPlayerDelay, float endPause, Action onCompleted = null)
        {
            if (_playerCardPrefab == null) { Debug.LogError($"Player card prefab is null!"); yield break; }
            if (_playerContentGO == null) { Debug.LogError($"Player content object is null!"); yield break; }

            int nextRound = _gameManager.roundManager.currentRound + 1;
            if (_roundStatusTxt != null) _roundStatusTxt.text = $"ROUND {_gameManager.roundManager.currentRound} DONE";
            if (_nextRoundTxt != null) _nextRoundTxt.text = nextRound <= _gameManager.currentGameLobbyData.numberOfRounds ? $"ROUND {nextRound}" : "LOBBY";


            List<ulong> connectedClientIds = new(NetworkManager.Singleton.ConnectedClientsIds);
            connectedClientIds.Sort((a, b) =>
                _gameManager.scoreManager.GetPlayerPlace(a, dataSet)
                    .CompareTo(_gameManager.scoreManager.GetPlayerPlace(b, dataSet))
            );

            // INFO: Show all player cards
            foreach (ulong clientId in connectedClientIds)
            {
                string steamName = BootstrapNetworkManager.GetPlayerSteamName(clientId);
                yield return ShowPlayerStats(clientId, dataSet, perPlayerDelay, steamName); // or use the value

            }

            // INFO: Last player delay
            yield return new WaitForSeconds(endPause);

            onCompleted?.Invoke();
            if (IsServer || (IsServer && onCompleted == null)) DeleteChildObject();

        }

        // INFO: Animation per player card
        private IEnumerator ShowPlayerStats(ulong clientId, Dictionary<ulong, ScoreData> dataSet, float perPlayerDelay, string steamName = null)
        {
            NetworkObject playerCardNetObj = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
                _playerCardPrefab.GetComponent<NetworkObject>(),
                NetworkManager.Singleton.LocalClientId
            );

            ScoreData playerStats = dataSet[clientId];
            int playerPlace = _gameManager.scoreManager.GetPlayerPlace(clientId, dataSet);
            int playerTotalScore = _gameManager.scoreManager.GetPlayerTotalScore(clientId);

            // INFO: Tell all clients to parent it
            ParentPlayerCardRPC(playerCardNetObj.NetworkObjectId, clientId.ToString(), playerTotalScore, playerStats.incorrectGuesses, playerStats.passes, playerPlace, steamName);

            // INFO: Delay for each player
            yield return new WaitForSeconds(perPlayerDelay);

        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ParentPlayerCardRPC(ulong cardNetworkObjectId, string clientId, int points, int fails, int passes, int place, string steamName = null)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkObjectId, out NetworkObject cardNetObj))
            {
                Debug.LogWarning($"Could not find card with ID {cardNetworkObjectId}");
                return;
            }

            PlayerCard playerCard = cardNetObj.GetComponent<PlayerCard>();
            ScoreData scoreData = new ScoreData
            {
                correctGuesses = points,
                incorrectGuesses = fails,
                passes = passes
            };

            string name = SteamManager.ConnectedToSteam ? steamName : clientId;

            playerCard.SetPlayerCardStats(name, scoreData, place);
            cardNetObj.transform.SetParent(_playerContentGO.transform);

            cardNetObj.transform.localPosition = Vector3.zero;
            cardNetObj.transform.localScale = Vector3.one;
        }

        #region Utility
        private void DeleteChildObject()
        {
            // Despawn all player card children first
            foreach (Transform child in _playerContentGO.transform)
            {
                NetworkObject childNetworkObject = child.GetComponent<NetworkObject>();
                if (childNetworkObject != null)
                {
                    childNetworkObject.Despawn(true);
                }
            }

            // Then despawn the MatchSummary itself
            NetworkObject.Despawn(true);
        }
        #endregion

    }
}