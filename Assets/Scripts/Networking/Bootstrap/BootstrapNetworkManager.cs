using Utility;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using PTB.Client.Player;
using PTB.Networking;

public class BootstrapNetworkManager : NetworkedSingleton<BootstrapNetworkManager>
{

    public LobbyData lobbyData { get; private set; }

    #region Change Scene

    #region SERVER
    public SceneEventProgressStatus ChangeNetworkScene(string sceneToLoad, string sceneToClose)
    {
        List<string> sceneList = new List<string> { sceneToClose };
        return ChangeNetworkScene(sceneToLoad, sceneList);

    }

    public SceneEventProgressStatus ChangeNetworkScene(string sceneToLoad, List<string> scenesToClose)
    {
        if (!NetworkManager.IsServer) return SceneEventProgressStatus.None; // INFO: Ensure server is running this
        if (sceneToLoad == null) { Debug.LogError($"Scene to load is null!"); return SceneEventProgressStatus.None; }

        foreach (string sceneName in scenesToClose)
        {
            if (string.IsNullOrEmpty(sceneName)) continue;
            Instance.CloseSceneObserverRPC(sceneName);

        }

        SceneEventProgressStatus sceneLoadStatus = NetworkManager.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
        if (sceneLoadStatus == SceneEventProgressStatus.Started) Debug.Log($"<color={LogColours.Unity}>[NETWORK]</color> Scene transition complete: {sceneToLoad}");
        return sceneLoadStatus;

    }
    #endregion

    #region CLIENT 

    [Rpc(SendTo.Everyone)]
    private void CloseSceneObserverRPC(string scenesToClose)
    {
        SceneManager.UnloadSceneAsync(scenesToClose);

    }
    #endregion

    #endregion

    #region Return To Lobby
    public void ReturnToLobby()
    {
        if (!NetworkManager.IsServer) return;

        SceneEventProgressStatus status = ChangeNetworkScene(BootstrapManager.Instance.lobbyScene, BootstrapManager.Instance.gameplayScenes);
        if (status == SceneEventProgressStatus.Started) Invoke(nameof(OpenLobbyMenuClientRPC), 0.1f);

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OpenLobbyMenuClientRPC()
    {
        _eventManager.OnStartUnityClient?.Invoke();

    }

    #endregion

    public void ForEachPlayer(Action<PlayerNetworkedController> action, bool includeHost = true)
    {
        foreach (NetworkClient netObj in NetworkManager.ConnectedClients.Values)
        {
            PlayerNetworkedController playerController = netObj.PlayerObject.GetComponent<PlayerNetworkedController>();
            if (playerController == null) continue;

            bool isHost = netObj.ClientId == NetworkManager.Singleton.LocalClientId;

            // GUARD: Skip host if not included
            if (isHost && !includeHost) continue;

            action?.Invoke(playerController);
        }
    }

    public void SetLobbyData(LobbyData newLobbyData) => lobbyData = newLobbyData;



}