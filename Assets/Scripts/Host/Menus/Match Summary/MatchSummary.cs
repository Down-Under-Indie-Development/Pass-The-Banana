using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(NetworkObject))]
public class MatchSummary : NetworkBehaviour
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;
    [SerializeField] private GameObject _playerContentGO;
    [SerializeField] private GameObject _playerCardPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) StartCoroutine(MatchSummaryCoroutine(_gameManager.roundManager.ProgressToNextRound));

    }

    private IEnumerator MatchSummaryCoroutine(Action onCompleted = null)
    {
        if (_playerCardPrefab == null) { Debug.LogError($"Player card prefab is null!"); yield break; }
        if (_playerContentGO == null) { Debug.LogError($"Player content object is null!"); yield break; }

        Debug.Log($"We made it!");

        Dictionary<ulong, ScoreData> dataSet = _gameManager.roundManager.currentRoundData;

        System.Collections.Generic.List<ulong> clientIds = new(NetworkManager.ConnectedClientsIds);
        clientIds.Sort((a, b) =>
            _gameManager.scoreManager.GetPlayerPlace(a, dataSet)
                .CompareTo(_gameManager.scoreManager.GetPlayerPlace(b, dataSet))
        );

        foreach (ulong clientId in clientIds)
        {
            yield return ShowPlayerStats(clientId, dataSet);

        }

        yield return new WaitForSeconds(2f);

        onCompleted?.Invoke();
        if (IsServer)
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

    }

    // INFO: Animation per player card
    private IEnumerator ShowPlayerStats(ulong clientId, Dictionary<ulong, ScoreData> dataSet)
    {
        NetworkObject playerCardNetObj = NetworkManager.SpawnManager.InstantiateAndSpawn(
            _playerCardPrefab.GetComponent<NetworkObject>(),
            NetworkManager.LocalClientId
        );

        ScoreData playerScore = _gameManager.roundManager.currentRoundData[clientId];
        int playerPlace = _gameManager.scoreManager.GetPlayerPlace(clientId, _gameManager.roundManager.currentRoundData);

        // Tell all clients to parent it
        ParentPlayerCardRPC(playerCardNetObj.NetworkObjectId, playerScore.points, playerScore.fails, playerScore.passes, clientId, playerPlace);
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

}