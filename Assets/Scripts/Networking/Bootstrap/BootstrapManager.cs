using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

namespace PTB.Networking
{
    public class BootstrapManager : CustomMonoBehaviour
    {
        [Header("Default Menu")]
        [SerializeField] private string defaultSceneToOpen = "MainMenuScene";


        private void Start()
        {
            EnableUnityTransport();

        }

        private void EnableUnityTransport()
        {
            if (!_debug) return;
            Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Using Unity Transport (Disable debug to use facepunch!)");
            GetComponent<SteamManager>().enabled = false;

            // INFO: Facepunch Transport
            GetComponent<FacepunchTransport>().enabled = false;

            // INFO: Unity Transport
            UnityTransport _unityTransport = GetComponent<UnityTransport>();
            _unityTransport.enabled = true;

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _unityTransport;

            GoToMenu();

        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(defaultSceneToOpen, LoadSceneMode.Additive);

        }

    }
}