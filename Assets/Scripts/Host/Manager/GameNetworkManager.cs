using Utility;
using UnityEngine;
using System.Collections.Generic;
using PTB.Client.Player;
using Unity.Netcode;
using PTB.Networking;
using System.Collections;
using System;
using System.Data;
using Unity.VisualScripting;
using UnityEngine.UI;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Linq;
using Netcode.Transports.Facepunch;
using HealthSystem;

public class GameNetworkManager : NetworkedSingleton<GameNetworkManager>
{
    public BombManager bombManager => BombManager.Instance;
    public PlayerManager playerManager => PlayerManager.Instance;
    public QuestionManager questionManager => QuestionManager.Instance;
    public ScoreManager scoreManager => ScoreManager.Instance;
    public RoundManger roundManager => RoundManger.Instance;

    #region Networking Components
    // INFO: Network Components
    public UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;
    public SessionStateManager sessionStateManager => SessionStateManager.Instance;
    public BootstrapNetworkManager bootstrapNetworkManager => BootstrapNetworkManager.Instance;
    #endregion

    public LobbyData currentGameLobbyData { get; private set; }

    #region Networking
    public override void OnNetworkSpawn()
    {
        if (IsServer) Invoke(nameof(HandleStartGame), 0.1f);

    }

    #endregion

    // INFO: All players spawn now do shit!
    private void HandleStartGame()
    {
        Debug.Log($"HandleStartGame called - IsServer: {IsServer}");
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
        bootstrapNetworkManager.ForEachPlayer(player => player.ApplyPauseState(_gamePaused));

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

// INFO: Game state
public enum GameState
{
    MainMenu,
    HostGame,
    Lobby,
    JoinGame,
    Options,
    Playing,
}