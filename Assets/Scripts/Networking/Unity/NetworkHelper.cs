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
        DontDestroyOnLoad(gameObject);

    }

    #region Events
    private void OnEnable()
    {
        // INFO: Host
        _eventManager.OnSteamHostConnect += OnStartUnityHost;
        _eventManager.OnStopUnityHost += OnStopUnityHost;

        // INFO: Client
        _eventManager.OnSteamClientConnect += OnStartUnityClient;
        _eventManager.OnStopUnityClient += StopUnityClient;

    }

    private void OnDisable()
    {
        // INFO: Host
        _eventManager.OnSteamHostConnect -= OnStartUnityHost;
        _eventManager.OnSteamClientConnect -= OnStartUnityClient;

        // INFO: Client
        _eventManager.OnStopUnityClient -= StopUnityClient;
        _eventManager.OnStopUnityHost -= OnStopUnityHost;

    }
    #endregion

    #region Client
    // INFO: Start client connection 
    protected virtual void OnStartUnityClient()
    {
        if (networkManager.IsHost) return;
        if (!networkManager.StartClient()) { Debug.LogError($"{CheckPrivilege()} Client failed to start!"); return; }

        Debug.Log($"{CheckPrivilege()} Client has started");

    }

    protected virtual void StopUnityClient()
    {
        if (networkManager != null) networkManager.Shutdown();
        SceneManager.LoadScene(0);

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
        _eventManager.OnStartUnityClient?.Invoke();

    }

    // [Rpc(SendTo.ClientsAndHost)]
    protected virtual void OnStopUnityHost()
    {
        if (!networkManager.IsHost) return;
        if (!IsHost) Debug.Log($"{CheckPrivilege()} Host has disconnected closing server");
        StopUnityClient();

    }

    #endregion

    #region Utility
    public string CheckPrivilege()
    {
        if (networkManager == null || networkManager.IsHost == default) return $"<color={LogColours.Unity}>[UNITY]</color>";

        switch (networkManager.IsHost)
        {
            case true:
                return $"<color={LogColours.Unity}>[UNITY]</color> <color={LogColours.Host}>[HOST]</color>";
            case false:
                return $"<color={LogColours.Unity}>[UNITY]</color> <color={LogColours.Client}>[CLIENT]</color>";

        }
    }
    #endregion



}