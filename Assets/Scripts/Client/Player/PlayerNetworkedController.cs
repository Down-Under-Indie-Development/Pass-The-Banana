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
            if (!IsOwner) return;
            _pauseMenuGO?.SetActive(false);
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // Don't allow unpause if host forced it
                if (_hostForcedPause) return;
                RequestTogglePauseRPC();
            }
        }

        #region Pause Handling
        [Rpc(SendTo.Server)]
        private void RequestTogglePauseRPC()
        {
            // Tell the server to broadcast pause state to all clients (including itself)
            GameManager.Instance.BroadcastPauseStateRPC();
        }

        // Called from GameManager via RPC - applies pause state to this player
        public void ApplyPauseState(bool isPaused)
        {
            _pauseMenuGO?.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
            if (!IsServer) _hostForcedPause = isPaused;
        }
        #endregion
    }
}