using PTB.Networking;
using PTB.Networking.Menus.Interfaces;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

namespace PTB.Networking.Menus
{
    public class JoinGameMenu : MonoBehaviour, IMenu
    {
        private EventManager _eventManager => EventManager.Instance;

        [SerializeField] private TMP_InputField _joinCodeInputField;
        [SerializeField] private TextMeshProUGUI _errorTXT;

        #region Events
        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            CloseMenu();

        }
        #endregion

        private void Start()
        {
            _errorTXT?.gameObject.SetActive(false);

        }

        public void OpenMenu() { ResetMenu(); }
        public void CloseMenu()
        {
            ResetMenu();
        }

        public void ResetMenu()
        {
            _joinCodeInputField.text = "";
            _errorTXT?.gameObject.SetActive(false);

        }

        public void Refresh() { }

        public async void JoinGame()
        {
            if (_joinCodeInputField == null) { Debug.LogError($"Input field null!"); return; }

            // DEBUG: For Testing
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport)
            {
                Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Bypassing Facepunch transport, starting client!");
                string sceneName = SceneManager.GetActiveScene().name;
                _eventManager.OnSteamClientConnect?.Invoke();
                return;

            }

            if (!ulong.TryParse(_joinCodeInputField.text, out ulong lobbyId))
            {
                Debug.LogWarning($"{UnityNetworkHelper.Instance.CheckPrivilege()} Invalid join code: {_joinCodeInputField.text}");
                _joinCodeInputField.text = "";
                _errorTXT?.gameObject.SetActive(true);
                return;

            }

            _errorTXT?.gameObject.SetActive(false);

            Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

        }
    }
}