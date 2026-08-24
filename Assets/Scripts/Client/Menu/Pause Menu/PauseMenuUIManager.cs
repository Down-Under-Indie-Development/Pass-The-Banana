using Utility;
using UnityEngine;
using Unity.VisualScripting;
using PTB.Networking;
using UnityEngine.SceneManagement;
using PTB.Client.Player;
using Unity.Netcode;

namespace PTB.Client.Menus
{
    public class PauseMenuUIManager : CustomMonoBehaviour
    {
        [SerializeField] private GameObject _optionsMenu;

        public void ResumeGame()
        {
            if (NetworkManager.Singleton.IsServer) gameObject.transform.root.GetComponent<PlayerNetworkedController>().RequestTogglePauseRPC(); // TODO: Make this an event?
            if (gameObject.transform.root.GetComponent<PlayerNetworkedController>().hostForcedPause) return;
            gameObject.SetActive(false);

        }

        public void Options()
        {
            // GUARD: Prevent nulls
            if (_optionsMenu == null) { Debug.LogWarning($"Options menu is null"); return; }
            _optionsMenu?.SetActive(true);

        }

        public void ReturnToMainMenu()
        {
            _eventManager.OnSteamClientDisconnect?.Invoke();

        }

        public void QuitGame()
        {
            _eventManager.OnQuitGame?.Invoke();

#if UNITY_EDITOR
            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> Sike this is the editor!</color>");
            ReturnToMainMenu();

#endif
            Application.Quit();

        }
    }
}