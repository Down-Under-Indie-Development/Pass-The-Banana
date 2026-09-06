using Utility;
using UnityEngine;
using Unity.Netcode;
using Doc.Networking;
using Doc.Networking.Unity;
using Doc.Networking.Session;
using Doc.Networking.Enums;
using Doc.Networking.Data;
using PTB.Client.Player;

namespace PTB.Managers
{
    public class GameNetworkManager : NetworkedSingleton<GameNetworkManager>
    {
        public BombManager bombManager => BombManager.Instance;
        public PlayerManager playerManager => PlayerManager.Instance;
        public QuestionManager questionManager => QuestionManager.Instance;
        public ScoreManager scoreManager => ScoreManager.Instance;
        public RoundManger roundManager => RoundManger.Instance;

        #region Networking Components
        // INFO: Network Components
        public SessionStateManager sessionStateManager => SessionStateManager.Instance;
        public BootstrapNetworkManager bootstrapNetworkManager => BootstrapNetworkManager.Instance;
        #endregion

        public LobbyInfo currentGameLobbyData { get; private set; }

        #region Networking
        public override void OnNetworkSpawn()
        {
            if (!IsServer) { enabled = false; return; }
            Invoke(nameof(HandleStartGame), 0.1f);

        }

        #endregion

        // INFO: All players spawn now do shit!
        private void HandleStartGame()
        {
            if (!IsServer) return;

            currentGameLobbyData = bootstrapNetworkManager.lobbyData;

            sessionStateManager.UpdateSessionState(GameState.Playing);
            if (sessionStateManager.currentSessionState.Value != GameState.Playing)
            {
                Debug.LogError($"Game state isn't set to playing {sessionStateManager.currentSessionState.Value}");
                return;

            }

            playerManager.HandleSpawnPlayers();
            roundManager.StartRound();

        }

        #region Host Pausing
        private bool _gamePaused = false;

        [Rpc(SendTo.ClientsAndHost)]  // INFO: Send to all clients AND the host
        public void TellPauseStateRPC()
        {
            // INFO: Toggle the pause state
            _gamePaused = !_gamePaused;

            Debug.Log($"{(_gamePaused ? "Host paused the game!" : "Host has unpaused the game!")}");

            // INFO: Apply pause state to all connected players (including host)
            // bootstrapNetworkManager.ForEachPlayer(player => player._pauseMenuGO.SetActive(false), false);
            bootstrapNetworkManager.ForEachPlayer(player => player.GetComponent<PlayerNetworkedController>().ApplyPauseState(_gamePaused));

        }
        #endregion

        // public IEnumerator DelayCoroutine(float seconds, Action onCompleted = null)
        // {
        //     yield return new WaitForSeconds(seconds);
        //     Debug.Log("Wait complete, about to invoke callback");
        //     onCompleted?.Invoke();
        //     Debug.Log("Callback invoked");

        // }

    }
}

