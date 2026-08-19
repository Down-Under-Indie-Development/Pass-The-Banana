using PTB.Networking;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapManager : MonoBehaviour
{
    [SerializeField] private string menuName = "MainMenuScene";

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuName, LoadSceneMode.Additive);

    }

}