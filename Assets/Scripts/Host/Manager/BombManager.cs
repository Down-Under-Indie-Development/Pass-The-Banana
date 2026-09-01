using System.Collections.Generic;
using System.Linq;
using Netcode.Transports.Facepunch;
using NUnit.Framework;
using PTB.Client.Player;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Utility;

/// <summary>
/// Holds and manages all logic regarding the exploding banana
/// </summary>
public class BombManager : NetworkedSingleton<BombManager>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    [field: Header("Player Tracking")]
    public NetworkVariable<ulong> playerWithBanana = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public ulong SelectStartingPlayer()
    {
        return playerWithBanana.Value = (ulong)Random.Range(0, NetworkManager.ConnectedClientsIds.Count - 1);

    }

    public void ProcessExplode()
    {
        if (playerWithBanana == null) return;
        _gameManager.playerManager.EliminatePlayer(playerWithBanana.Value);
        ProcessPassTheBomb();

    }

    public void ProcessPassTheBomb()
    {
        if (_gameManager.activePlayers.Count == 1)
        {
            Debug.Log($"<color={LogColours.Unity}>[BOMB MANAGER]</color> We have a winner!");
            // TODO: Add logic to give points to the active player
            _gameManager.questionManager.ProcessNextQuestion(); return;

        }

        if (_gameManager.activePlayers.Count <= 0) { _gameManager.roundManager.HandleGameOver(); return; }

        int currentIndex = _gameManager.activePlayers.IndexOf(playerWithBanana.Value);
        int nextIndex = (currentIndex + 1) % _gameManager.activePlayers.Count;
        playerWithBanana.Value = _gameManager.activePlayers[nextIndex];

        Debug.Log($"<color={LogColours.Unity}>[BOMB MANAGER]</color> Bomb passed to player {playerWithBanana.Value}");
        StartCoroutine(_gameManager.DelayCoroutine(.1f, _gameManager.questionManager.ProcessNextQuestion));

    }



    public ulong previousPlayerWithBanana { get; private set; } = 420;
    [Rpc(SendTo.ClientsAndHost)]
    public void NotifyChangePodiumRPC()
    {
        _gameManager.playerManager.HandleChangePodiumColorRPC(playerWithBanana.Value, Color.red);
        if (playerWithBanana.Value != previousPlayerWithBanana && previousPlayerWithBanana != 420)
        {
            _gameManager.playerManager.HandleChangePodiumColorRPC(previousPlayerWithBanana, Color.white);
            if (IsServer) _gameManager.playerManager.MoveToHotSeat(previousPlayerWithBanana, true);

        }

        if (!IsServer) return;

        // INFO: Move the player to the hot seat
        _gameManager.playerManager.MoveToHotSeat(playerWithBanana.Value);
        previousPlayerWithBanana = playerWithBanana.Value;

    }

    public bool IsPlayerWithBomb(ulong clientId) => playerWithBanana.Value == clientId;

}