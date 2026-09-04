using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using PTB.Managers;
namespace PTB.Client.Player
{
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetworkedController : NetworkBehaviour
    {
        [Space()]
        [Header("Menus")]
        [SerializeField] public GameObject _pauseMenuGO;
        public bool hostForcedPause { get; private set; } = false;

        [SerializeField] private int _correctAnswers;
        [field: SerializeField] public GameObject podium { get; private set; }

        private void Start()
        {
            if (!IsOwner) { GetComponent<PlayerNetworkedController>().enabled = false; return; }
            _pauseMenuGO?.SetActive(false);

        }

        private void Update()
        {

            // INFO: Pause Logic
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (!IsServer && !hostForcedPause)
            {
                TogglePauseMenu(); return;
            }

            if (IsServer) AskTogglePauseRPC();


        }

        #region Pause Handling
        private void TogglePauseMenu()
        {
            _pauseMenuGO.SetActive(!_pauseMenuGO.activeSelf);
        }

        [Rpc(SendTo.Server)]
        public void AskTogglePauseRPC()
        {
            // Tell the server to broadcast pause state to all clients (including itself)
            GameNetworkManager.Instance.TellPauseStateRPC();

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