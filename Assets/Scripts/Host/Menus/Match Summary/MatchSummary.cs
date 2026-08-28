using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(NetworkObject))]
public class MatchSummary : NetworkBehaviour
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    [Header("Round Info")]
    [SerializeField] private TextMeshProUGUI _roundStatusTxt;
    [SerializeField] private TextMeshProUGUI _nextRoundTxt;


    [Header("Player Panel")]
    [SerializeField] private GameObject _playerContentGO;
    [SerializeField] private GameObject _playerCardPrefab;

    public IEnumerator MatchSummaryCoroutine(Dictionary<ulong, ScoreData> dataSet, Action onCompleted = null)
    {
        if (_playerCardPrefab == null) { Debug.LogError($"Player card prefab is null!"); yield break; }
        if (_playerContentGO == null) { Debug.LogError($"Player content object is null!"); yield break; }

        int nextRound = _gameManager.roundManager.currentRound + 1;
        if (_roundStatusTxt != null) _roundStatusTxt.text = $"ROUND {_gameManager.roundManager.currentRound} DONE";
        if (_nextRoundTxt != null) _nextRoundTxt.text = nextRound < _gameManager.currentGameLobbyData.numberOfRounds ? $"ROUND {nextRound}" : _nextRoundTxt.text = "LOBBY";


        System.Collections.Generic.List<ulong> clientIds = new(NetworkManager.ConnectedClientsIds);
        clientIds.Sort((a, b) =>
            _gameManager.scoreManager.GetPlayerPlace(a, dataSet)
                .CompareTo(_gameManager.scoreManager.GetPlayerPlace(b, dataSet))
        );

        // INFO: Show all player cards
        foreach (ulong clientId in clientIds)
            yield return ShowPlayerStats(clientId, dataSet);

        // INFO: Last player delay
        yield return new WaitForSeconds(2f);

        onCompleted?.Invoke();
        if (IsServer || (IsServer && onCompleted == null)) DeleteChildObject();

    }

    // INFO: Animation per player card
    private IEnumerator ShowPlayerStats(ulong clientId, Dictionary<ulong, ScoreData> dataSet)
    {
        NetworkObject playerCardNetObj = NetworkManager.SpawnManager.InstantiateAndSpawn(
            _playerCardPrefab.GetComponent<NetworkObject>(),
            NetworkManager.LocalClientId
        );

        ScoreData playerScore = dataSet[clientId];
        int playerPlace = _gameManager.scoreManager.GetPlayerPlace(clientId, dataSet);

        // Tell all clients to parent it
        ParentPlayerCardRPC(playerCardNetObj.NetworkObjectId, playerScore.points, playerScore.fails, playerScore.passes, clientId, playerPlace);

        // INFO: Delay for each player
        yield return new WaitForSeconds(.5f);

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ParentPlayerCardRPC(ulong cardNetworkObjectId, int score, int fails, int passes, ulong clientId, int place)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkObjectId, out NetworkObject cardNetObj))
        {
            Debug.LogWarning($"Could not find card with ID {cardNetworkObjectId}");
            return;
        }

        PlayerCard playerCard = cardNetObj.GetComponent<PlayerCard>();
        ScoreData scoreData = new ScoreData();
        scoreData.points = score;
        scoreData.fails = fails;
        scoreData.passes = passes;

        playerCard.SetPlayerCardStats(scoreData, clientId, place);
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