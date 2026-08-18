using PTB.Menus;
using PTB.Networking;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Utility;

public class JoinGameMenu : CustomMonoBehaviour
{
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private TextMeshProUGUI _errorTXT;

    #region Events
    private void OnEnable()
    {
        _eventManager.OnUnityClientDisconnected += ResetMenu;
    }

    private void OnDisable()
    {
        _eventManager.OnUnityClientDisconnected -= ResetMenu;

    }
    #endregion

    private void Start()
    {
        _errorTXT?.gameObject.SetActive(false);

    }

    private void ResetMenu()
    {
        _joinCodeInputField.text = "";

    }

    public async void JoinGame()
    {
        if (_joinCodeInputField == null) { Debug.LogError($"Input field null!"); return; }

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