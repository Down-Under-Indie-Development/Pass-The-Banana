using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Category", fileName = "New Category")]
public class CategorySO : ScriptableObject
{
    [field: Header("Category Info")]
    [field: SerializeField] public string categoryName { get; private set; }
    [field: SerializeField] public Difficulty categoryDifficulty { get; private set; } = Difficulty.Easy;

    [field: Space()]
    [field: Header("Questions")]
    [field: SerializeField] public List<QuestionData> questions { get; private set; }
    public int GetTimeLimit() => (int)categoryDifficulty;

    public enum Difficulty
    {
        Easy = 120,
        Medium = 60,
        Hard = 30,
        Test = 10,

    }

}

[Serializable]
public class QuestionData
{
    public string question;
    public List<AnswerData> answers;

}

[Serializable]
public class AnswerData
{
    public string answer;
    public bool correctAnswer;
}