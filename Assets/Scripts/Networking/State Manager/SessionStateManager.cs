using Unity.Netcode;
using UnityEngine;

namespace PTB.Networking
{
    [RequireComponent(typeof(NetworkObject))]
    public class SessionStateManager : NetworkBehaviour
    {
        public static SessionStateManager Instance;


        public NetworkVariable<GameState> currentSessionState { get; private set; } = new NetworkVariable<GameState>
        (
            GameState.MainMenu,

NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this)
            {
                // Not the server-authorized instance — shouldn't normally happen if you only spawn one
                NetworkObject.Despawn(true);
                return;

            }

            Instance = this;
            // No DontDestroyOnLoad needed — dynamically spawned objects persist
            // across scene loads by default unless DestroyWithScene is set true.
            NetworkObject.DestroyWithScene = false;
        }

        public override void OnNetworkDespawn()
        {
            Instance = null;
        }

        #region Events
        private void OnEnable()
        {
            currentSessionState.OnValueChanged += SessionStateChanged;
        }

        private void OnDisable()
        {
            currentSessionState.OnValueChanged -= SessionStateChanged;

        }
        #endregion

        private void SessionStateChanged(GameState previousValue, GameState newValue)
        {
            Debug.Log($"Game state has been changed to {newValue}");

        }

        public void UpdateSessionState(GameState newValue)
        {
            if (!IsServer)
            {
                Debug.Log($"You aint the hsot!");
                return;
            }

            currentSessionState.Value = newValue;


        }

    }
}