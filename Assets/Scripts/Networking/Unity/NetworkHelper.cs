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

    public NetworkManager networkManager { get; private set; }

    #region Events
    private void OnEnable()
    {
        _eventManager.OnStartHost += OnStartHost;
        _eventManager.OnStartClient += OnStartClient;
        _eventManager.OnClientDisconnect += Disconnect;
        _eventManager.OnHostDisconnect += OnHostDisconnect;

    }

    private void OnDisable()
    {
        _eventManager.OnClientDisconnect -= Disconnect;
        _eventManager.OnStartClient -= OnStartClient;
        _eventManager.OnHostDisconnect -= OnHostDisconnect;

        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;

    }
    #endregion

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager == null) { Debug.LogError($"Network manager not found, disabling {name}"); gameObject.SetActive(false); return; }

    }

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
    protected virtual void Disconnect()
    {
        if (networkManager == null) return;
        if (!networkManager.IsHost)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

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
        Debug.Log($"Host has started");

    }

    protected virtual void OnHostDisconnect()
    {
        Debug.LogWarning($"The host has disconnected closing server");
        SceneManager.LoadScene(0);
        Disconnect();
    }
    #endregion



}