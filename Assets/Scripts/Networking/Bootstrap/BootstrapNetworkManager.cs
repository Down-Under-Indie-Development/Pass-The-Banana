using Utility;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BootstrapNetworkManager : NetworkedSingleton<BootstrapNetworkManager>
{
    #region Change Scene
    public static void ChangeNetworkScene(string sceneName, string sceneToClose)
    {
        if (string.IsNullOrEmpty(sceneToClose)) return;
        List<string> sceneList = new List<string> { sceneToClose };
        ChangeNetworkScene(sceneName, sceneList);

    }

    public static void ChangeNetworkScene(string sceneName, List<string> scenesToClose)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (string.IsNullOrEmpty(sceneName)) return;
        if (scenesToClose == null || scenesToClose.Count == 0) return;
        foreach (string scene in scenesToClose)
        {
            Instance.ClosesScenesRPC(scene);
        }

        Debug.Log($"Scene transition complete: {sceneName}");
    }

    [Rpc(SendTo.Authority)]
    private void ClosesScenesRPC(string scenesToClose)
    {
        CloseSceneObserverRPC(scenesToClose);
    }

    [Rpc(SendTo.NotServer)]
    private void CloseSceneObserverRPC(string scenesToClose)
    {
        if (string.IsNullOrEmpty(name)) return;
        SceneManager.UnloadSceneAsync(name);

    }
    #endregion

}