using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using Doc.Networking.Events;
using Doc.Networking.Data;

namespace Doc.Networking.Menus.Game
{
    public class HostGame : MonoBehaviour
    {
        private BootstrapNetworkManager _bootstrapNetworkManager => BootstrapNetworkManager.Instance;

        [Header("Menu Components")]
        [SerializeField] private Slider _maxPlayerSlider;
        [SerializeField] private Slider _numberOfRoundsSlide;
        [SerializeField] private Button _friendsOnly;

        public void CreateLobby()
        {
            if (_maxPlayerSlider == null) { Debug.LogError($"Player count slider is null!"); return; }
            if (_numberOfRoundsSlide == null) { Debug.LogError($"Number of rounds slider is null!"); return; }

            LobbyInfo lobbyData = new LobbyInfo();
            lobbyData.maxPlayers = (int)_maxPlayerSlider.value;
            lobbyData.numberOfRounds = (int)_numberOfRoundsSlide.value;
            // lobbyData.friendsOnly = _friendsOnly.

            Debug.Log(lobbyData);

            _bootstrapNetworkManager.SetLobbyData(lobbyData);
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport)
            {
                Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Bypassing Facepunch transport, starting host!");
                NetworkUtilEventManager.OnSteamHostConnect?.Invoke();
                return;

            }

            NetworkUtilEventManager.OnCreateLobbyRequest?.Invoke(lobbyData);

        }

    }
}