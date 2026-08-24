using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Scriptable Objects/Question Data", fileName = "New Question")]
public class QuestionSO : ScriptableObject
{
    [field: SerializeField] public string question { get; private set; }
    [field: SerializeField] public List<AnswerData> answers { get; private set; }

}

[Serializable]
public class AnswerData
{
    public string answer;
    public bool correctAnswer;
}