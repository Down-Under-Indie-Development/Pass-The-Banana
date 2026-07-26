using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Question Data", fileName = "New Question")]
public class QuestionDataSO : ScriptableObject
{
    [field: SerializeField] public string question { get; private set; }
    [field: SerializeField] public List<string> answers { get; private set; }

}