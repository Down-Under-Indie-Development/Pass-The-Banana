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
        public bool hostForcedPause { get; private set; } = false;

        [SerializeField] private int _correctAnswers;

        private void Start()
        {
            if (!IsOwner) { GetComponent<PlayerNetworkedController>().enabled = false; return; }
            _pauseMenuGO?.SetActive(false);

        }

        private void Update()
        {
            // if (!IsOwner) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {

                if (!IsServer && !hostForcedPause)
                {
                    Debug.Log($"Test");
                    TogglePauseMenu(); return;
                }

                RequestTogglePauseRPC();

            }
        }

        #region Pause Handling
        private void TogglePauseMenu()
        {
            _pauseMenuGO.SetActive(!_pauseMenuGO.activeSelf);
        }

        [Rpc(SendTo.Server)]
        public void RequestTogglePauseRPC()
        {
            // Tell the server to broadcast pause state to all clients (including itself)
            GameManager.Instance.BroadcastPauseStateRPC();
        }

        // Called from GameManager via RPC - applies pause state to this player
        public void ApplyPauseState(bool isPaused)
        {
            _pauseMenuGO.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
            if (!IsServer) hostForcedPause = isPaused;

        }
        #endregion
    }
}