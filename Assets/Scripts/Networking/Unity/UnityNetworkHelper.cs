using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PTB.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

/// <summary>
/// Handles Unity Netcode side for connecting and disconnecting clients
/// </summary>
public class UnityNetworkHelper : Singleton<UnityNetworkHelper>
{
    public virtual NetworkManager networkManager => NetworkManager.Singleton;

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
        _eventManager.OnStopUnityHost -= OnStopUnityHost;

        // INFO: Client
        _eventManager.OnSteamClientConnect -= OnStartUnityClient;
        _eventManager.OnStopUnityClient -= StopUnityClient;

    }
    #endregion

    #region Unity Client
    // INFO: Start client connection 
    protected virtual async void OnStartUnityClient()
    {
        if (networkManager.IsHost) return;
        if (!networkManager.StartClient()) { Debug.LogError($"{CheckPrivilege()} Client failed to start!"); return; }

        Debug.Log($"{CheckPrivilege()} Client has started");

        await SceneManager.UnloadSceneAsync(BootstrapManager.Instance.mainMenuScene);
        _eventManager.OnStartUnityClient?.Invoke(); // INFO: Client started let other scripts know

    }

    protected virtual async void StopUnityClient()
    {
        if (networkManager == null) return;

        string privilege = networkManager.IsServer ? "host" : "client";
        string color = networkManager.IsServer ? LogColours.Host : LogColours.Client;

        Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> <color={color}>[{privilege.ToUpper()}]</color> Shutting down {privilege}...");

        try
        {
            networkManager.Shutdown();
            Destroy(networkManager.gameObject);

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Shutdown error: {ex.Message}");
        }

        _eventManager.OnUnityClientDisconnected?.Invoke(); // INFO: Client stopped let other scripts know


    }

    #endregion

    #region Unity Host
    // INFO: Start host connection
    protected virtual void OnStartUnityHost()
    {

        // GUARD: Ensure server started
        // RegisterNetworkPrefabs();
        if (!networkManager.StartHost()) return;

        // INFO: Configure Network Manager
        networkManager.SceneManager.ActiveSceneSynchronizationEnabled = true;
        networkManager.SceneManager.PostSynchronizationSceneUnloading = true;
        // networkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);

        Debug.Log($"{CheckPrivilege()} Unity Server has started");
        _eventManager.OnStartUnityHost?.Invoke(); // INFO: Host started let other scripts know

    }

    protected virtual void OnStopUnityHost()
    {
        if (!networkManager.IsServer) return;
        StopUnityClient(); // INFO: Stop host client

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