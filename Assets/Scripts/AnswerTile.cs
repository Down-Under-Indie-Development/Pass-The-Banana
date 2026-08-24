using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NetworkObject))]
public class AnswerTile : NetworkBehaviour, IPointerClickHandler
{
    private EventManager _eventManager => EventManager.Instance;
    private TextMeshProUGUI txtAnswer;

    void Awake()
    {
        txtAnswer = GetComponentInChildren<TextMeshProUGUI>();
        if (txtAnswer == null) { Debug.LogError($"Can't find the text component!"); return; }

    }

    public void SetAnswer(string answer)
    {
        if (string.IsNullOrEmpty(answer)) { Debug.LogWarning($"Provided answer is null"); return; }
        txtAnswer.text = answer;

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectAnswerServerRpc(txtAnswer.text);

    }

    // INFO: Server Side
    [Rpc(SendTo.Server)]
    private void SelectAnswerServerRpc(string answer, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        GameManager.Instance.OnQuestionSelectedRPC(answer, clientId);

    }
}
