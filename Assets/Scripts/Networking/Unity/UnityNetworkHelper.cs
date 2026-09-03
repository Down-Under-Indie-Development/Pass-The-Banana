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
    // INFO: Start Client Connection 
    protected virtual async void OnStartUnityClient()
    {
        if (networkManager.IsHost) return;
        try
        {
            networkManager.StartClient();
            await SceneManager.UnloadSceneAsync(BootstrapManager.Instance.mainMenuScene);

            Debug.Log($"{CheckPrivilege()} Client has started");
            _eventManager.OnStartUnityClient?.Invoke(); // INFO: Client started let other scripts know

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{ex.Message}");

        }

    }

    // INFO: Stop Client Connection
    protected virtual async void StopUnityClient()
    {
        try
        {
            string privilege = networkManager.IsServer ? "host" : "client";
            string color = networkManager.IsServer ? LogColours.Host : LogColours.Client;

            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> <color={color}>[{privilege.ToUpper()}]</color> Shutting down {privilege}...");
            networkManager.Shutdown();
            // Destroy(networkManager.gameObject);

            _eventManager.OnUnityClientDisconnected?.Invoke(); // INFO: Client stopped let other scripts know

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Shutdown error: {ex.Message}");
        }

    }

    #endregion

    #region Unity Host
    // INFO: Start host connection
    protected virtual void OnStartUnityHost()
    {
        try
        {
            networkManager.StartHost();

            // INFO: Configure Network Manager
            networkManager.SceneManager.ActiveSceneSynchronizationEnabled = true;
            networkManager.SceneManager.PostSynchronizationSceneUnloading = true;
            networkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);

            Debug.Log($"{CheckPrivilege()} Unity Server has started");
            _eventManager.OnStartUnityHost?.Invoke(); // INFO: Host started let other scripts know

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{ex.Message}");

        }

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