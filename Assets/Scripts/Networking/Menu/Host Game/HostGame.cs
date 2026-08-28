using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using Utility;

public class HostGame : CustomMonoBehaviour
{
    [Header("Menu Components")]
    [SerializeField] private Slider _maxPlayerSlider;
    [SerializeField] private Slider _numberOfRoundsSlide;
    [SerializeField] private Button _friendsOnly;

    public void CreateLobby()
    {
        if (_maxPlayerSlider == null) { Debug.LogError($"Player count slider is null!"); return; }
        if (_numberOfRoundsSlide == null) { Debug.LogError($"Number of rounds slider is null!"); return; }

        LobbyData lobbyData = new LobbyData();
        lobbyData.maxPlayers = (int)_maxPlayerSlider.value;
        lobbyData.numberOfRounds = (int)_numberOfRoundsSlide.value;
        // lobbyData.friendsOnly = _friendsOnly.

        Debug.Log(lobbyData);

        BootstrapNetworkManager.Instance.SetLobbyData(lobbyData);
        if (BootstrapNetworkManager.Instance.NetworkManager.NetworkConfig.NetworkTransport is UnityTransport)
        {
            Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Bypassing Facepunch transport, starting host!");
            _eventManager.OnSteamHostConnect?.Invoke();
            return;

        }

        _eventManager.OnCreateLobbyRequest?.Invoke(lobbyData);

    }

}