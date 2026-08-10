using Utility;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using HealthSystem;
using PTB.Client.Player;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private List<CategorySO> _categories;
    [SerializeField] private PlayerController _playerWithBanana;

    #region Events
    private void OnEnable()
    {
        // _eventManager.OnQuestionFinished += DisplayAnswers;
        _eventManager.OnCountdownFinished += Test;
    }

    private void OnDisable()
    {
        // _eventManager.OnQuestionFinished -= DisplayAnswers;
        _eventManager.OnCountdownFinished -= Test;

    }
    #endregion

    private void Start()
    {
        // if (_categories.Count <= 0) { Debug.LogError($"No categories provided"); return; }
        _eventManager.OnGameStart?.Invoke();
        _eventManager.OnCountdownStarted?.Invoke();


        // TypeWriter.Instance.WriteText(categoryData[0].questions[0].question, tvScreen);

    }

    private void Test()
    {
        _playerWithBanana?.GetComponent<Health>().Die();

    }

    #region  Answers
    // private void DisplayAnswers()
    // {
    //     StartCoroutine(ShowAnswers());
    // }

    // private IEnumerator ShowAnswers()
    // {
    //     // GUARD: Prevent Nulls
    //     if (answerParent == null) { Debug.LogError($"Answer parent is null"); yield break; }
    //     if (answerPrefab == null) { Debug.LogError($"Answer prefab is null"); yield break; }

    //     for (int i = 0; i < categoryData[0].questions[0].answers.Count; i++)
    //     {
    //         GameObject answerGO = Instantiate(answerPrefab, answerParent);
    //         answerGO.GetComponentInChildren<TextMeshProUGUI>().text = categoryData[0].questions[0].answers[i];
    //         yield return new WaitForSecondsRealtime(.5f);
    //     }
    // }
    #endregion

}
