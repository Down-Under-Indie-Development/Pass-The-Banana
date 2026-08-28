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
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Linq;
using Netcode.Transports.Facepunch;
using HealthSystem;

public class GameNetworkManager : NetworkedSingleton<GameNetworkManager>
{

    [field: Header("Game Settings")]
    [SerializeField, ReadOnly] private float _answerTime;

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

    #region Events
    private void OnEnable()
    {

    }

    private void OnDisable()
    {
        _eventManager.OnCountdownFinished -= bombManager.ProcessExplode;


    }
    #endregion

    #region Networking
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) StartCoroutine(DelayCoroutine(.1f, HandleStartGameRPC));

    }
    #endregion

    // INFO: All players spawn now do shit!
    [Rpc(SendTo.Server)]
    public void HandleStartGameRPC()
    {
        currentGameLobbyData = bootstrapNetworkManager.lobbyData;
        BootstrapManager.Instance.sessionStateManager.UpdateSessionState(GameState.Playing);
        if (BootstrapManager.Instance.sessionStateManager.currentSessionState.Value != GameState.Playing) return;

        playerManager.SpawnPlayers();
        roundManager.StartRound();

    }

    #region Host Pausing
    private bool _gamePaused = false;

    [Rpc(SendTo.ClientsAndHost)]  // INFO: Send to all clients AND the host
    public void BroadcastPauseStateRPC()
    {
        // INFO: Toggle the pause state
        _gamePaused = !_gamePaused;

        Debug.Log($"{(_gamePaused ? "Host paused the game!" : "Host has unpaused the game!")}");

        // INFO: Apply pause state to all connected players (including host)
        // bootstrapNetworkManager.ForEachPlayer(player => player._pauseMenuGO.SetActive(false), false);
        bootstrapNetworkManager.ForEachPlayer(player => player.ApplyPauseState(_gamePaused), false);

    }
    #endregion

    public IEnumerator DelayCoroutine(float seconds, Action onCompleted = null)
    {
        yield return new WaitForSeconds(seconds);
        onCompleted?.Invoke();

    }

    public void EndGame()
    {
        Debug.Log($"YOu win!");
    }

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