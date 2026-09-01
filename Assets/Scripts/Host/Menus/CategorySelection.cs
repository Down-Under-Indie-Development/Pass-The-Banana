using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkObject))]
public class CategorySelection : NetworkBehaviour
{

    [Space()]
    [Header("Selector")]
    [SerializeField] private GameObject _selectorMenu;
    private TMP_Dropdown _categoryDropdown;

    [Space()]
    [Header("Client")]
    [SerializeField] private GameObject _clientMenu;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        #region Client Menu
        if (!GameNetworkManager.Instance.bombManager.IsPlayerWithBomb(NetworkManager.LocalClientId)) return;

        #endregion

        _clientMenu?.SetActive(false);
        _selectorMenu?.SetActive(true);


        // INFO: Server Setup the dropdown
        _categoryDropdown = GetComponentInChildren<TMP_Dropdown>();
        _categoryDropdown.ClearOptions();

        InitializeCategoryDropdown();

    }

    public void AddItemToDropDown(string value)
    {
        _categoryDropdown.options.Add(new TMP_Dropdown.OptionData(value));
        _categoryDropdown.RefreshShownValue();

    }


    // INFO: Client
    #region CATEGORY SELECTION - CLIENT

    public void HandleCategorySelected()
    {
        string selectedCategory = _categoryDropdown.options[_categoryDropdown.value].text;
        RequestCategoryConfirmationRpc(selectedCategory);
    }

    // INFO: Load dropdown options client
    public void InitializeCategoryDropdown()
    {
        foreach (CategorySO category in GameNetworkManager.Instance.questionManager.categoryContainer.categories)
        {
            AddItemToDropDown(category.categoryName);

        }


        // !! INFO: Dev testing only
        if (!Debug.isDebugBuild) return;
        foreach (CategorySO category in GameNetworkManager.Instance.questionManager.testingCategoryContainer.categories)
        {
            AddItemToDropDown(category.categoryName + " (TESTING)");

        }

    }
    #endregion

    #region CATEGORY SELECTION - SERVER
    [Rpc(SendTo.Server)]
    public void RequestCategoryConfirmationRpc(string selectedCategory)
    {
        Debug.Log($"<{LogColours.Unity}>[CATEGORY]</color> Selected: {selectedCategory}");

        // INFO: Tell GameManager to process this
        GameNetworkManager.Instance.roundManager.ProcessChosenCategory(selectedCategory);

        // INFO: Despawn this UI
        NetworkObject.Despawn(true);

    }
    #endregion

}