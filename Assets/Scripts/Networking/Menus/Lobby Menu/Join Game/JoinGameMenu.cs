using PTB.Menus;
using PTB.Networking;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class JoinGameMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private TextMeshProUGUI _errorTXT;

    private void Start()
    {
        _errorTXT?.gameObject.SetActive(false);

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
        if (lobby != null) MainMenuController.Instance.currentGameState = GameState.Lobby;


    }
}