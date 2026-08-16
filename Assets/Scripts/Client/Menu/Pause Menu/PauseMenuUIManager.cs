using Utility;
using UnityEngine;
using Unity.VisualScripting;
using PTB.Networking;

namespace PTB.Client.Menus
{
    public class PauseMenuUIManager : CustomMonoBehaviour
    {
        [SerializeField] private GameObject _optionsMenu;

        public void Options()
        {
            // GUARD: Prevent nulls
            if (_optionsMenu == null) { Debug.LogWarning($"Options menu is null"); return; }
            _optionsMenu?.SetActive(true);

        }

        public void ReturnToMainMenu()
        {
            if (SteamLobbyManager.hasInstance) SteamLobbyManager.Instance.DisconnectPlayer(); // TODO: Make an event for this

        }

        public void QuitGame()
        {
            _eventManager.OnQuitGame?.Invoke();
#if UNITY_EDITOR
            Debug.Log($"<color=orange>Sike this is the editor!</color>");
#endif
            Application.Quit();

        }
    }
}