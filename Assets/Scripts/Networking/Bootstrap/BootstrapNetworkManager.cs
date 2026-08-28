using Utility;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using PTB.Client.Player;

public class BootstrapNetworkManager : NetworkedSingleton<BootstrapNetworkManager>
{

    public LobbyData lobbyData { get; private set; }

    #region Change Scene
    public void ChangeNetworkScene(string sceneToLoad, string sceneToClose)
    {
        List<string> sceneList = new List<string> { sceneToClose };
        ChangeNetworkScene(sceneToLoad, sceneList.ToList<string>());

    }

    public void ChangeNetworkScene(string sceneToLoad, List<string> scenesToClose)
    {
        // if (!NetworkManager.Singleton.IsServer) return;
        if (scenesToClose.Count == 0) return;

        foreach (string sceneName in scenesToClose)
        {
            if (string.IsNullOrEmpty(sceneToLoad)) continue;
            Instance.CloseSceneObserverRPC(sceneToLoad, sceneName);

        }

        NetworkManager.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
        Debug.Log($"Scene transition complete: {sceneToLoad}");

    }

    // [Rpc(SendTo.Authority)]
    // private void ClosesScenesRPC(string scenesToClose)
    // {
    //     CloseSceneObserverRPC(scenesToClose);
    // }

    [Rpc(SendTo.Everyone)]
    private void CloseSceneObserverRPC(string sceneToLoad, string scenesToClose)
    {
        SceneManager.UnloadSceneAsync(scenesToClose);

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

    public void SetLobbyData(LobbyData newLobbyData)
    {
        lobbyData = newLobbyData;

    }

}