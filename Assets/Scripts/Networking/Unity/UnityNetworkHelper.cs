using PTB.Networking;
using Unity.Netcode;
using UnityEngine;
using Utility;

/// <summary>
/// Handles Unity Netcode side for connecting and disconnecting clients
/// </summary>
public class UnityNetworkHelper : NetworkedSingleton<UnityNetworkHelper>
{
    // private EventManager _eventManager => EventManager.Instance;
    public virtual NetworkManager networkManager => NetworkManager.Singleton;
    public virtual SessionStateManager sessionStateManager => SessionStateManager.Instance;

    #region Events
    protected virtual void OnEnable()
    {
        // INFO: Host
        _eventManager.OnSteamHostConnect += OnStartUnityHost;
        _eventManager.OnStopUnityHost += OnStopUnityHost;

        // INFO: Client
        _eventManager.OnSteamClientConnect += OnStartUnityClient;
        _eventManager.OnStopUnityClient += StopUnityClient;

    }

    protected virtual void OnDisable()
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
        _eventManager.OnStartUnityClient?.Invoke();

    }

    protected virtual void StopUnityClient()
    {
        if (networkManager == null) return;

        string privilege = IsServer ? "host" : "client";
        string color = IsServer ? LogColours.Host : LogColours.Client;

        Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> <color={color}>[{privilege.ToUpper()}]</color> Shutting down {privilege}...");
        networkManager.Shutdown();
        _eventManager.OnUnityClientDisconnected?.Invoke();

    }

    #endregion

    #region Host
    // INFO: Start host connection
    protected virtual void OnStartUnityHost()
    {
        // GUARD: Ensure server started
        if (!networkManager.StartHost()) return;
        Debug.Log($"{CheckPrivilege()} Unity Server has started");
        _eventManager.OnStartUnityClient?.Invoke();

    }

    protected virtual void OnStopUnityHost()
    {
        if (!networkManager.IsHost) return;
        StopUnityClient();

    }

    #endregion

    #region Utility
    public virtual string CheckPrivilege()
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