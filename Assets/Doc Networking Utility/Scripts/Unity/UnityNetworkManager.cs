using Doc.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Doc.Networking.Events;
using System.Threading.Tasks;
using Steamworks;

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

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex.Message}");

            }

            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            Debug.Log($"{CheckPrivilege()} Client has started");
            await SceneManager.UnloadSceneAsync(BootstrapManager.Instance.mainMenuScene);

            NetworkUtilEventManager.OnStartUnityClient?.Invoke(); // INFO: Client started let other scripts know

        }

        // INFO: Stop Client Connection
        protected virtual async void StopUnityClient()
        {
            string privilege = NetworkManager.Singleton.IsServer ? "host" : "client";
            string color = NetworkManager.Singleton.IsServer ? LogColours.Host : LogColours.Client;

            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> <color={color}>[{privilege.ToUpper()}]</color> Shutting down {privilege}...");

            try
            {
                NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
                NetworkManager.Singleton.Shutdown();

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Shutdown error: {ex.Message}");

            }
        }

        private void OnClientStopped(bool isHost)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> You left the lobby!");

        }

        #region Connection Events
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            Debug.Log($"{connectionEventData.EventType}");
            switch (connectionEventData.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    OnClientConnected(networkManager, connectionEventData);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    OnClientKicked(networkManager, connectionEventData);
                    break;
            }
        }

        protected virtual void OnClientConnected(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientConnected) return;
            if (!networkManager.IsServer) return;

            BootstrapNetworkManager.AddPlayer(connectionEventData.ClientId, BootstrapNetworkManager.GetPlayerSteamClient(connectionEventData.ClientId).Id);
            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> {connectionEventData.ClientId} has joined!");

        }

        protected virtual void OnClientKicked(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (networkManager.IsServer && connectionEventData.ClientId != 0) BootstrapNetworkManager.RemovePlayer(connectionEventData.ClientId);
            if (connectionEventData.ClientId != networkManager.LocalClientId) return;

            StopUnityClient();
            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> You've been kicked from the lobby!");

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


            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex.Message}");

            }

            BootstrapNetworkManager.AddPlayer(NetworkManager.Singleton.LocalClientId, SteamClient.SteamId);

            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;

            // INFO: Configure Network Manager
            NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
            NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = true;
            NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);

            Debug.Log($"{CheckPrivilege()} Unity Server has started");
            NetworkUtilEventManager.OnStartUnityHost?.Invoke(); // INFO: Host started let other scripts know

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
