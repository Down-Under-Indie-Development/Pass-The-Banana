using Utility;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private List<LevelDataSO> levelData;

    [Header("TV Screen")]
    [SerializeField] private TextMeshProUGUI tvScreen;
    [SerializeField] private GameObject answerPrefab;
    [SerializeField] private Transform answerParent;



    #region Events
    private void OnEnable()
    {
        _eventManager.OnQuestionFinished += DisplayAnswers;

    }

    private void OnDisable()
    {
        _eventManager.OnQuestionFinished -= DisplayAnswers;

    }
    #endregion

    private void Start()
    {
        if (levelData.Count <= 0) { Debug.LogError($"No levels provided"); return; }
        TypeWriter.Instance.WriteText(levelData[0].questions[0].question, tvScreen);

    }

    #region  Answers
    private void DisplayAnswers()
    {
        StartCoroutine(ShowAnswers());
    }

    private IEnumerator ShowAnswers()
    {
        // GUARD: Prevent Nulls
        if (answerParent == null) { Debug.LogError($"Answer parent is null"); yield break; }
        if (answerPrefab == null) { Debug.LogError($"Answer prefab is null"); yield break; }

        for (int i = 0; i < levelData[0].questions[0].answers.Count; i++)
        {
            GameObject answerGO = Instantiate(answerPrefab, answerParent);
            answerGO.GetComponentInChildren<TextMeshProUGUI>().text = levelData[0].questions[0].answers[i];
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
    #endregion

}
