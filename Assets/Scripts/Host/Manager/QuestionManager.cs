using System.Collections;
using System.Linq;
using System.Transactions;
using PTB.Client.Player;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Utility;

public class QuestionManager : NetworkedSingleton<QuestionManager>
{
    private GameNetworkManager _gameManager => GameNetworkManager.Instance;

    // Fields
    private AnswerMenuUIManger _answerUIManager => AnswerMenuUIManger.Instance;
    private CategorySO _currentCategory;
    private QuestionData _currentQuestion;
    private int _currentQuestionIndex = 0;
    [field: SerializeField] public CategoriesContainerSO categoryContainer { get; private set; }

    // Functions
    public void SelectCategory(string selectedCategory)
    {
        _currentCategory = categoryContainer.categories.FirstOrDefault(category => category.categoryName == selectedCategory);
        _currentQuestionIndex = 0;
        _currentQuestion = _currentCategory.questions[_currentQuestionIndex];
        StartCoroutine(HandleSpawnAnswers());

    }

    public void ProcessNextQuestion()
    {
        _currentQuestionIndex += 1;

        if (_currentQuestionIndex > _currentCategory.questions.Count - 1 || _gameManager.playerManager.PlayersRemaining <= 1)
        {
            _gameManager.roundManager.ProcessNextRound();
            return;
        }
        _currentQuestion = _currentCategory.questions[_currentQuestionIndex];

        StartCoroutine(HandleSpawnAnswers());

    }

    public void ClearAnswers()
    {
        _answerUIManager.ClearPreviousAnswersRPC();
    }

    public IEnumerator HandleSpawnAnswers()
    {
        ClearAnswers();
        _gameManager.bombManager.NotifyChangePodiumRPC();
        yield return new WaitForSeconds(.55f);

        _answerUIManager.SetQuestionTextRPC(GetCurrentQuestion().question);

        // INFO: RPC no like complex data structures 🥹
        foreach (AnswerData answerData in GetCurrentQuestion().answers)
        {
            AnswerMenuUIManger.Instance.AddAnswerRPC(answerData.answer);
        }


    }


    public bool ValidateAnswer(string answer)
    {
        return _currentQuestion.answers.Any(a => a.correctAnswer && a.answer == answer);
    }

    public QuestionData GetCurrentQuestion() => _currentQuestion;
    public CategorySO GetCurrentCategory() => _currentCategory;
    public float GetTimeLimit() => _currentCategory.GetTimeLimit();


}