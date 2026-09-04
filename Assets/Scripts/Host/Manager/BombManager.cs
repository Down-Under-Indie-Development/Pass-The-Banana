using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Utility;


namespace PTB.Managers
{
    /// <summary>
    /// Holds and manages all logic regarding the exploding banana
    /// </summary>
    public class BombManager : NetworkedSingleton<BombManager>
    {
        private GameNetworkManager _gameManager => GameNetworkManager.Instance;

        [field: Header("Player Tracking")]
        public NetworkVariable<ulong> playerWithBanana = new NetworkVariable<ulong>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        #region Events
        private void OnEnable()
        {
            _eventManager.OnCountdownFinished += ProcessExplode;

        }

        private void OnDisable()
        {
            _eventManager.OnCountdownFinished -= ProcessExplode;
        }
        #endregion

        public void ProcessExplode()
        {
            if (playerWithBanana == null) return;
            _gameManager.playerManager.EliminatePlayerRPC(playerWithBanana.Value);
            Invoke(nameof(ProcessPassTheBomb), .2f); // INFO: Add delay for explosion animation

        }

        public void ProcessPassTheBomb()
        {
            var alivePlayerIds = _gameManager.playerManager.playersRemaining;

            // INFO: Someone is still alive!
            if (alivePlayerIds.Count >= 2)
            {
                int currentIndex = alivePlayerIds.IndexOf(playerWithBanana.Value);
                int nextIndex = (currentIndex + 1) % alivePlayerIds.Count;
                playerWithBanana.Value = alivePlayerIds[nextIndex];
                Debug.Log($"<color={LogColours.Unity}>[BOMB MANAGER]</color> Bomb passed to player {playerWithBanana.Value}");

            }

            _gameManager.questionManager.ProcessNextQuestion();

        }

        public ulong previousPlayerWithBanana { get; private set; } = 420;
        [Rpc(SendTo.ClientsAndHost)]
        public void TellChangePodiumRPC()
        {
            _gameManager.playerManager.HandleChangePodiumColorRPC(playerWithBanana.Value, Color.red);
            if (playerWithBanana.Value != previousPlayerWithBanana && previousPlayerWithBanana != 420)
            {
                _gameManager.playerManager.HandleChangePodiumColorRPC(previousPlayerWithBanana, Color.white);
                if (IsServer) _gameManager.playerManager.MoveToHotSeat(previousPlayerWithBanana, true);

            }

            if (!IsServer) return;

            // INFO: Move the player to the hot seat
            _gameManager.playerManager.MoveToHotSeat(playerWithBanana.Value);
            previousPlayerWithBanana = playerWithBanana.Value;

        }

        #region Utility
        public bool IsPlayerWithBomb(ulong clientId) => playerWithBanana.Value == clientId;

        public ulong SelectStartingPlayer() => playerWithBanana.Value = (ulong)Random.Range(0, NetworkManager.Singleton.ConnectedClientsIds.Count - 1);

        #endregion

    }
}