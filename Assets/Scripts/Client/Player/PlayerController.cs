using Utility;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

namespace PTB.Client.Player
{
    public class PlayerNetworkedController : NetworkBehaviour
    {
        [Space()]
        [Header("Menus")]
        [SerializeField] public GameObject _pauseMenuGO;

        [SerializeField] private int _correctAnswers;

        private void Start()
        {
            _pauseMenuGO?.SetActive(false);

        }

        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) TogglePauseMenu(); // INFO: Check for pause menu
        }

        #region Menus
        private void TogglePauseMenu()
        {
            if (_pauseMenuGO == null) { Debug.LogWarning($"Pause menu is null!"); return; }
            _pauseMenuGO?.SetActive(!_pauseMenuGO.activeSelf);

        }
        #endregion


    }
}