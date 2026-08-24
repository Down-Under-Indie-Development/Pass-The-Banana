using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

namespace PTB.Networking
{

    public class BootstrapManager : Singleton<BootstrapManager>
    {
        [Header("Default Menu")]
        [SerializeField] private string defaultSceneToOpen = "MainMenuScene";

        [Header("Network Components")]
        public SteamManager steamManager => SteamManager.Instance;
        public NetworkManager networkManager => NetworkManager.Singleton;
        public UnityNetworkHelper unityNetworkHelper => UnityNetworkHelper.Instance;
        public SessionStateManager sessionStateManager => SessionStateManager.Instance;

        [Header("Transports")]
        [field: SerializeField] public Transport selectedTransport { get; private set; } = Transport.Facepunch;

        // INFO: Debugging
        private UnityTransport _unityTransport = null;

        private void Start()
        {

            if (selectedTransport == Transport.Unity) { EnableUnityTransport(); return; }
            SteamManager.Instance.EstablishSteamConnection();

        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(defaultSceneToOpen, LoadSceneMode.Additive);

        }

        #region Debugging
        // DEBUG: Use Unity Transport (For Testing)
        private void EnableUnityTransport()
        {
            if (selectedTransport != Transport.Unity) return;
            Destroy(steamManager.gameObject);

            #region Create Unity Transport Object
            GameObject transportObject = new GameObject();
            transportObject.AddComponent<UnityTransport>();
            transportObject.name = "Unity Transport [DEBUG]";
            _unityTransport = transportObject.GetComponent<UnityTransport>();
            #endregion

            // INFO: Update the network manager
            networkManager.NetworkConfig.NetworkTransport = _unityTransport;
            Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Using Unity Transport (Switch transport to use facepunch!)");

            GoToMenu();

        }

        #endregion

        public enum Transport
        {
            Facepunch,
            Unity

        }

    }
}