using Netcode.Transports.Facepunch;
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
        ulong playerCount = (ulong)NetworkManager.ConnectedClients.Count;
        playerWithBanana.Value = (playerWithBanana.Value + 1) % playerCount;

        StartCoroutine(_gameManager.DelayCoroutine(.1f, _gameManager.questionManager.ProcessNextQuestion));
        Debug.Log($"<color={LogColours.Unity}>[BOMB MANAGER]</color> Bomb passed to player {playerWithBanana.Value}");

    }

    private void HandleChangePodiumColor(ulong client, Color colour)
    {
        NetworkManager.ConnectedClients[client].PlayerObject.GetComponent<PlayerNetworkedController>().podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = colour;
    }

    private ulong _previousPlayer = 420;
    [Rpc(SendTo.ClientsAndHost)]
    public void NotifyChangePodiumRPC()
    {
        HandleChangePodiumColor(playerWithBanana.Value, Color.red);
        if (playerWithBanana.Value != _previousPlayer && _previousPlayer != 420) HandleChangePodiumColor(_previousPlayer, Color.white);
        _previousPlayer = playerWithBanana.Value;

    }

    public bool IsPlayerWithBomb(ulong clientId) => playerWithBanana.Value == clientId;

}