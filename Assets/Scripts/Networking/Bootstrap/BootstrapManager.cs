using PTB.Networking;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapManager : MonoBehaviour
{
    [SerializeField] private string menuName = "MainMenuScene";
    private NetworkManager _networkManager => NetworkManager.Singleton;
    private SteamManager _steamManager => SteamManager.Instance;

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuName, LoadSceneMode.Additive);

    }

}