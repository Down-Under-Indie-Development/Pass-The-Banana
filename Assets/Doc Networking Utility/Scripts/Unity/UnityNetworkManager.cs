using Doc.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Doc.Networking.Events;

/// <summary>
/// Handles Unity Netcode side for connecting and disconnecting clients
/// </summary>
namespace Doc.Networking.Unity
{
    public class UnityNetworkManager : MonoBehaviour
    {

        #region Events
        protected virtual void OnEnable()
        {
            // INFO: Host
            NetworkUtilEventManager.OnSteamHostConnect += OnStartUnityHost;
            NetworkUtilEventManager.OnStopUnityHost += OnStopUnityHost;

            // INFO: Client
            NetworkUtilEventManager.OnSteamClientConnect += OnStartUnityClient;
            NetworkUtilEventManager.OnStopUnityClient += StopUnityClient;

        }

        protected virtual void OnDisable()
        {
            // INFO: Host
            NetworkUtilEventManager.OnSteamHostConnect -= OnStartUnityHost;
            NetworkUtilEventManager.OnStopUnityHost -= OnStopUnityHost;

            // INFO: Client
            NetworkUtilEventManager.OnSteamClientConnect -= OnStartUnityClient;
            NetworkUtilEventManager.OnStopUnityClient -= StopUnityClient;


        }
        #endregion

        #region Unity Client
        // INFO: Start Client Connection 
        protected virtual async void OnStartUnityClient()
        {

            try
            {
                NetworkManager.Singleton.StartClient();
                await SceneManager.UnloadSceneAsync(BootstrapManager.Instance.mainMenuScene);

                Debug.Log($"{CheckPrivilege()} Client has started");

                NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;

                NetworkUtilEventManager.OnStartUnityClient?.Invoke(); // INFO: Client started let other scripts know

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex.Message}");

            }

        }

        // INFO: Stop Client Connection
        protected virtual void StopUnityClient()
        {
            try
            {
                string privilege = NetworkManager.Singleton.IsServer ? "host" : "client";
                string color = NetworkManager.Singleton.IsServer ? LogColours.Host : LogColours.Client;

                Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> <color={color}>[{privilege.ToUpper()}]</color> Shutting down {privilege}...");

                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Shutdown error: {ex.Message}");
            }

        }

        #region Connection Events
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            switch (connectionEventData.EventType)
            {
                case ConnectionEvent.ClientDisconnected:
                    OnClientDisconnect(networkManager, connectionEventData);
                    break;
            }
        }

        protected virtual void OnClientDisconnect(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.ClientId != networkManager.LocalClientId) return;
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
            SceneManager.LoadScene(BootstrapManager.Instance.gameObject.scene.name);

            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> You left the lobby!");


        }
        #endregion

        #endregion

        #region Unity Host
        // INFO: Start host connection
        protected virtual void OnStartUnityHost()
        {
            try
            {
                NetworkManager.Singleton.StartHost();

                NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;

                // INFO: Configure Network Manager
                NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
                NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = true;
                NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);

                Debug.Log($"{CheckPrivilege()} Unity Server has started");
                NetworkUtilEventManager.OnStartUnityHost?.Invoke(); // INFO: Host started let other scripts know

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex.Message}");

            }

        }

        protected virtual void OnStopUnityHost()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            StopUnityClient(); // INFO: Stop host client

        }

        #endregion

        #region Utility
        public static string CheckPrivilege()
        {
            if (!NetworkManager.Singleton.IsListening) return $"<color={LogColours.Unity}>[UNITY]</color>";

            switch (NetworkManager.Singleton.IsHost)
            {
                case true:
                    return $"<color={LogColours.Unity}>[UNITY]</color> <color={LogColours.Host}>[HOST]</color>";
                case false:
                    return $"<color={LogColours.Unity}>[UNITY]</color> <color={LogColours.Client}>[CLIENT]</color>";

            }
        }
        #endregion

    }

}
