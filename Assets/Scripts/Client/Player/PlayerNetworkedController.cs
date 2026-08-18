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
        private bool _hostForcedPause = false;

        [SerializeField] private int _correctAnswers;

        private void Start()
        {
            // if (!IsOwner) return;
            _pauseMenuGO?.SetActive(false);

        }

        private void Update()
        {
            // if (Keyboard.current.escapeKey.wasPressedThisFrame && Time.timeScale != 0) TogglePauseMenu(); // INFO: Check for pause menu
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // Don't allow unpause if host forced it
                if (_hostForcedPause) return;
                TogglePauseMenu();
            }

        }

        #region Menus
        private void TogglePauseMenu()
        {
            if (_pauseMenuGO == null) { Debug.LogWarning($"Pause menu is null!"); return; }
            GameManager.Instance.NotifyServerOfHostPauseRPC();
            _pauseMenuGO?.SetActive(!_pauseMenuGO.activeSelf);

        }

        // INFO: Pause when host pauses
        public void PausePlayer()
        {
            Time.timeScale = _pauseMenuGO.activeSelf ? 0f : 1f;
            if (NetworkManager.Singleton.IsServer) return;
            _hostForcedPause = _pauseMenuGO.activeSelf;
            TogglePauseMenu();

        }
        #endregion


    }
}