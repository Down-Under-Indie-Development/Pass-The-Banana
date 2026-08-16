using Unity.Netcode;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

/// <summary>
/// Handles Unity Netcode side for connecting and disconnecting clients
/// </summary>
public class UnityNetworkHelper : NetworkBehaviour
{
    public static UnityNetworkHelper Instance;
    private EventManager _eventManager => EventManager.Instance;
    public NetworkManager networkManager => NetworkManager.Singleton;

    private void Awake()
    {
        // GUARD: Destroy duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // note: DontDestroyOnLoad(this) also works for a component but gameObject is the clearer form

    }

    #region Events
    private void OnEnable()
    {
        _eventManager.OnStartUnityHost += OnStartUnityHost;
        _eventManager.OnStartUnityClient += OnStartUnityClient;
        // _eventManager.OnUnityClientDisconnect += StopUnityClient;
        _eventManager.OnUnityHostDisconnect += OnUnityHostDisconnectRPC;

    }

    private void OnDisable()
    {
        // _eventManager.OnUnityClientDisconnect -= StopUnityClient;
        _eventManager.OnStartUnityClient -= OnStartUnityClient;
        _eventManager.OnUnityHostDisconnect -= OnUnityHostDisconnectRPC;

        if (networkManager == null) return;
        networkManager.OnClientConnectedCallback -= OnUnityClientConnected;
        networkManager.OnClientDisconnectCallback -= OnUnityClientDisconnect;

    }
    #endregion

    #region Client
    // INFO: Start client connection 
    protected virtual void OnStartUnityClient()
    {

        networkManager.OnClientConnectedCallback += OnUnityClientConnected;
        networkManager.OnClientDisconnectCallback += OnUnityClientDisconnect;
        if (!networkManager.StartClient()) { Debug.LogError($"{CheckPrivilege()} Client failed to start!"); return; }

        Debug.Log($"{CheckPrivilege()} Client has started");

    }

    protected virtual void OnUnityClientConnected(ulong obj) { Debug.Log($"{obj} has connected!"); }
    protected virtual void OnUnityClientDisconnect(ulong obj)
    {
        StopUnityClient();
    }

    protected virtual void StopUnityClient()
    {
        if (networkManager == null) return;
        if (!networkManager.IsHost)
        {
            networkManager.OnClientConnectedCallback -= OnUnityClientConnected;
            networkManager.OnClientDisconnectCallback -= OnUnityClientDisconnect;
        }

        networkManager.SceneManager.LoadScene("Main Menu Scene", LoadSceneMode.Single);
        networkManager.Shutdown();
    }

    #endregion

    #region Host
    // INFO: Start host connection
    protected virtual void OnStartUnityHost()
    {
        // GUARD: Ensure host isn't already running
        if (networkManager.IsHost) return;

        // GUARD: Ensure server started
        if (!networkManager.StartHost()) return;
        Debug.Log($"{CheckPrivilege()} Unity Server has started");

    }

    [Rpc(SendTo.ClientsAndHost)]
    protected virtual void OnUnityHostDisconnectRPC()
    {
        networkManager.SceneManager.LoadScene("Main Menu Scene", LoadSceneMode.Single);
        if (!networkManager.IsHost) Debug.Log($"{CheckPrivilege()} Host has disconnected closing server");
        StopUnityClient();


    }

    #endregion

    public string CheckPrivilege()
    {
        if (networkManager == null || networkManager.IsHost == default) return "<color=orange>[CLIENT]</color>";

        switch (networkManager.IsHost)
        {
            case true:
                return "<color=orange>[HOST]</color>";
            case false:
                return "<color=orange>[CLIENT]</color>";

        }

    }



}