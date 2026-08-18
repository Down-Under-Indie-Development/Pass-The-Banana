using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Utility;

namespace PTB.Networking
{
    public class SessionStateManager : NetworkedSingleton<SessionStateManager>
    {
        public NetworkVariable<GameState> currentSessionState { get; private set; } = new NetworkVariable<GameState>
        (
            GameState.MainMenu,

NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server
        );


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
            if (IsServer) Debug.Log($"{UnityNetworkHelper.Instance.CheckPrivilege()} Game state has been changed to {newValue}");
            if (IsClient) Debug.Log($"{UnityNetworkHelper.Instance.CheckPrivilege()} Syncing game state from host ({newValue})");

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