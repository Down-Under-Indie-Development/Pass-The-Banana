using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkObject))]
public class CategorySelection : NetworkBehaviour
{

    [Space()]
    [Header("Host")]
    [SerializeField] private GameObject _hostMenu;
    private TMP_Dropdown _categoryDropdown;

    [Space()]
    [Header("Client")]
    [SerializeField] private GameObject _clientMenu;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            _hostMenu?.SetActive(false);
            _clientMenu?.SetActive(true);
            return;

        }

        _categoryDropdown = GetComponentInChildren<TMP_Dropdown>();
        _categoryDropdown.ClearOptions();

        AddOptionsRPC();

    }

    public void AddItemToDropDown(string value)
    {
        _categoryDropdown.options.Add(new TMP_Dropdown.OptionData(value));
        _categoryDropdown.RefreshShownValue();

    }


    public void ConfirmCategory()
    {
        if (!IsServer) return;
        ConfirmedCategoryServerRPC();

    }

    [Rpc(SendTo.Server)]
    private void ConfirmedCategoryServerRPC()
    {
        Debug.Log($"Selected: {_categoryDropdown.options[_categoryDropdown.value].text}");
        GameManager.Instance.OnCategorySelected(_categoryDropdown.options[_categoryDropdown.value].text);
        NetworkObject.Despawn(true);

    }

    [Rpc(SendTo.Server)]
    private void AddOptionsRPC()
    {
        foreach (CategorySO category in GameManager.Instance.categoryContainer.categories)
        {
            AddItemToDropDown(category.categoryName);

        }
    }
}