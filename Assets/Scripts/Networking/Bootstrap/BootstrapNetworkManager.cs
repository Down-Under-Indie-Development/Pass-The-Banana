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

    public static void ChangeNetworkScene(string sceneName, List<string> sceneToClose)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (string.IsNullOrEmpty(sceneName)) return;
        if (sceneToClose == null || sceneToClose.Count == 0) return;

        Instance.LoadAndCloseScene(sceneName, sceneToClose);
    }

    private async void LoadAndCloseScene(string sceneName, List<string> scenesToClose)
    {
        // INFO: Load new scene first (additively) so network objects can migrate
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        foreach (string name in scenesToClose)
        {
            if (string.IsNullOrEmpty(name)) continue;
            await SceneManager.UnloadSceneAsync(name);

        }


        Debug.Log($"Scene transition complete: {sceneName}");
    }
    #endregion

}