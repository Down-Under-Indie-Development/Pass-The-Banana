using HealthSystem;
using Netcode.Transports.Facepunch;
using PTB.Client.Player;
using Unity.Netcode;
using UnityEngine;
using Utility;

/// <summary>
/// Responsible for tracking the player with the banana and logic to pass the banana
/// to another player
/// </summary>
public class HotPotatoManager : NetworkedSingleton<HotPotatoManager>
{

    [field: Header("Player Tracking")]
    [ReadOnly]
    public NetworkVariable<ulong> _playerWithBanana = new NetworkVariable<ulong>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        GameManager.Instance.hotPotatoManager = this;

    }

    public void ProcessExplode()
    {
        if (_playerWithBanana == null) return;
        NetworkManager.Singleton.ConnectedClients[_playerWithBanana.Value].PlayerObject.GetComponent<PlayerNetworkedController>().GetComponent<IDamageable>().Die();
        GameManager.Instance.playersRemaining -= 1;
        ProcessPassTheBomb();

    }

    public void ProcessPassTheBomb()
    {
        ulong playerCount = (ulong)NetworkManager.ConnectedClients.Count;
        _playerWithBanana.Value = (_playerWithBanana.Value + 1) % playerCount;

        StartCoroutine(GameManager.Instance.DelayCoroutine(0.1f, GameManager.Instance.ProcessNextQuestion));
        Debug.Log($"<color={LogColours.Host}>[HOST]</color> Bomb passed to player {_playerWithBanana.Value}");


    }

}