using Unity.Netcode;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

/// <summary>
/// Handles Unity Netcode side for connecting and disconnecting clients
/// </summary>
public class NetworkHelper : PersistentSingleton<NetworkHelper>
{

    public NetworkManager networkManager => NetworkManager.Singleton;

    #region Events
    private void OnEnable()
    {
        _eventManager.OnStartHost += OnStartHost;
        _eventManager.OnStartClient += OnStartClient;
        _eventManager.OnClientDisconnect += StopUnityClient;
        _eventManager.OnHostDisconnect += OnHostDisconnectRPC;

    }

    private void OnDisable()
    {
        _eventManager.OnClientDisconnect -= StopUnityClient;
        _eventManager.OnStartClient -= OnStartClient;
        _eventManager.OnHostDisconnect -= OnHostDisconnectRPC;

        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;

    }
    #endregion

    #region Client
    // INFO: Start client connection 
    protected virtual void OnStartClient()
    {

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        if (!networkManager.StartClient()) { Debug.LogError($"Client failed to start!"); return; }

        Debug.Log($"Client has started");

    }

    protected virtual void OnClientConnected(ulong obj) { Debug.Log($"{obj} has connected!"); }
    protected virtual void OnClientDisconnect(ulong obj) { Debug.Log($"{obj} has disconnected!"); }
    #endregion

    #region Shared
    protected virtual void StopUnityClient()
    {
        if (networkManager == null) return;
        if (!networkManager.IsHost)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        networkManager.SceneManager.LoadScene("Main Menu Scene", LoadSceneMode.Single);
        networkManager.Shutdown();

    }

    #endregion

    #region Host
    // INFO: Start host connection
    protected virtual void OnStartHost()
    {
        // GUARD: Ensure host isn't already running
        if (networkManager.IsHost) return;

        // GUARD: Ensure server started
        if (!networkManager.StartHost()) return;
        Debug.Log($"{CheckPrivilege(networkManager.IsHost)} has started");

    }

    [Rpc(SendTo.ClientsAndHost)]
    protected virtual void OnHostDisconnectRPC()
    {
        networkManager.SceneManager.LoadScene("Main Menu Scene", LoadSceneMode.Single);
        Debug.Log($"The host has disconnected closing server");
        StopUnityClient();


    }

    #endregion

    public string CheckPrivilege(bool obj)
    {
        switch (obj)
        {
            case true:
                return "[HOST]";
            case false:
                return "[CLIENT]";

        }

    }



}