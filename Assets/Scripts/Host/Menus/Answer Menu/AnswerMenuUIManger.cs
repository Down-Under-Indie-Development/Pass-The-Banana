using PTB.Managers;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utility;

[RequireComponent(typeof(NetworkObject))]
public class AnswerMenuUIManger : NetworkedSingleton<AnswerMenuUIManger>
{
    [SerializeField] private TextMeshProUGUI _questionTXT;
    [SerializeField] private GameObject _answerTilePrefab;
    [SerializeField] private GameObject _answerGridGO;

    [Rpc(SendTo.Server)]
    public void AddAnswerRPC(string answerValue)
    {
        NetworkObject answerNetworkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(
        _answerTilePrefab.GetComponent<NetworkObject>(),
        NetworkManager.Singleton.LocalClientId
        );

        //  Tell clients to parent this
        ParentTileClientRPC(answerNetworkObject.NetworkObjectId, answerValue);

    }

    [Rpc(SendTo.Server)]
    public void ClearPreviousAnswersRPC()
    {
        for (int i = _answerGridGO.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(_answerGridGO.transform.GetChild(i).gameObject);

        }

        SetQuestionTextRPC("");

    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetQuestionTextRPC(string question)
    {
        _questionTXT.text = question;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ParentTileClientRPC(ulong tileNetworkObjectId, string answer)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tileNetworkObjectId, out NetworkObject tileNetObj))
        {
            Debug.LogWarning($"Could not find tile with ID {tileNetworkObjectId}");
            return;
        }

        // Move to same scene as grid
        if (!NetworkManager.Singleton.IsServer) SceneManager.MoveGameObjectToScene(tileNetObj.gameObject, _answerGridGO.scene);
        tileNetObj.name = $"{answer}";
        tileNetObj.GetComponent<AnswerTile>().SetAnswer(answer);
        tileNetObj.transform.SetParent(_answerGridGO.transform);

        tileNetObj.transform.localPosition = Vector3.zero;
        tileNetObj.transform.localScale = Vector3.one;

        if (NetworkManager.Singleton.LocalClientId == GameNetworkManager.Instance.bombManager.playerWithBanana.Value) return;
        Image tileImage = tileNetObj.GetComponentInChildren<Image>();
        Color color = tileImage.color;
        color.a = 0.2f;
        tileImage.color = color;


    }
}
