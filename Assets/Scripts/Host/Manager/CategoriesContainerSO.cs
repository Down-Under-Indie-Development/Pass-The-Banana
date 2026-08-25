using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Category Container", fileName = "New Category Container")]
public class CategoriesContainerSO : ScriptableObject
{
    [field: SerializeField] public List<CategorySO> categories { get; private set; } = new();

}