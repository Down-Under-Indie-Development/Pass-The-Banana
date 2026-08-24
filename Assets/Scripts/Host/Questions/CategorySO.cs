using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Category", fileName = "New Category")]
[Serializable]
public class CategorySO : ScriptableObject
{
    [field: SerializeField] public string categoryName { get; private set; }
    [field: SerializeField] public List<QuestionSO> questions { get; private set; }

}