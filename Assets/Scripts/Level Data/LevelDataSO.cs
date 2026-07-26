using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Level Data", fileName = "New Level Data")]
public class LevelDataSO : ScriptableObject
{
    [field: SerializeField] public int levelNumber { get; private set; }
    [field: SerializeField] public List<QuestionDataSO> questions { get; private set; }

}