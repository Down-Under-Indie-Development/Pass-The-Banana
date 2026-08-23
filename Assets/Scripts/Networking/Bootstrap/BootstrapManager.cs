using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
        private SteamManager _steamManager => SteamManager.Instance;
        private NetworkManager _networkManager => NetworkManager.Singleton;
        private UnityNetworkHelper _unityNetworkHelper => UnityNetworkHelper.Instance;

        [Header("Transports")]
        private FacepunchTransport _facepunchTransport;
        private UnityTransport _unityTransport;

        private void Start()
        {
            _facepunchTransport = _networkManager.GetComponent<FacepunchTransport>();

            if (_debug) { EnableUnityTransport(); return; }

        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(defaultSceneToOpen, LoadSceneMode.Additive);

        }

        #region Debugging
        // DEBUG: Use Unity Transport (For Testing)
        private void EnableUnityTransport()
        {
            if (!_debug) return;
            _steamManager.gameObject.SetActive(false);

            #region Create Unity Transport Object
            GameObject transportObject = new GameObject();
            transportObject.AddComponent<UnityTransport>();
            transportObject.name = "Unity Transport [DEBUG]";
            _unityTransport = transportObject.GetComponent<UnityTransport>();
            #endregion

            // INFO: Swap transports
            _facepunchTransport.enabled = false;

            // INFO: Update the network manager
            _networkManager.NetworkConfig.NetworkTransport = _unityTransport;

            Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Using Unity Transport (Disable debug to use facepunch!)");

            GoToMenu();

        }
        #endregion

    }
}