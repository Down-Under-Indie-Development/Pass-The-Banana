using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Utility;
using PTB.Client.Player;
using HealthSystem;
using System.Collections;
using System.Linq;
using PTB.Enums;

namespace PTB.Managers
{
    /// <summary>
    /// Holds and handles all logic regarding players
    /// </summary>
    public class PlayerManager : NetworkedSingleton<PlayerManager>
    {
        // INFO: Singletons
        private GameNetworkManager _gameManager => GameNetworkManager.Instance;
        private PlayerAnimationHandler _playerAnimationHandler => PlayerAnimationHandler.Instance;

        [field: Header("Player Tracking")]
        [field: SerializeField, DictionaryDisplay(keyLabel = "ID", valueLabel = "Eliminated")] public Dictionary<ulong, bool> activePlayers { get; private set; } = new();

        public List<ulong> playersRemaining => activePlayers
            .Where(kvp => !kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        private Dictionary<ulong, Vector3> _originalPodiumPositions = new();

        [Header("Player Settings")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private List<Transform> _spawnPositions = new();
        [SerializeField] private Transform _hotSeatTransform;

        [Header("Audio Clips")]
        [SerializeField] private Dictionary<PlayerSFXType, List<AudioClip>> _playerSFX;

        #region Events
        private void OnEnable()
        {
            if (NetworkManager == null) { Debug.LogError($"Are you sure you're in the right scene?"); return; }

            NetworkManager.OnConnectionEvent += OnUnityClientDisconnect;

        }

        private void OnDisable()
        {
            if (NetworkManager == null) return;
            NetworkManager.OnConnectionEvent -= OnUnityClientDisconnect;

        }
        #endregion

        public override void OnNetworkSpawn()
        {
            if (!IsServer) { enabled = false; return; }
        }

        #region Spawn Players
        public void HandleSpawnPlayers()
        {
            if (!IsServer) return;
            if (_playerPrefab == null) { Debug.LogError($"Player prefab is null, cannot spawn!"); return; }

            Debug.Log($"[SERVER] Spawning {NetworkManager.ConnectedClientsIds.Count} players");

            for (int i = 0; i < NetworkManager.ConnectedClientsIds.Count; i++)
            {
                ulong currentClient = NetworkManager.ConnectedClientsIds[i];
                GameObject instance = Instantiate(_playerPrefab);
                instance.transform.position = _spawnPositions[i].position;

                // Store the original podium position
                _originalPodiumPositions[currentClient] = _spawnPositions[i].position;

                NetworkObject netObj = instance.GetComponent<NetworkObject>();
                netObj.SpawnAsPlayerObject(currentClient, true);
            }

            InitializePlayers();
        }

        public void InitializePlayers()
        {
            activePlayers = NetworkManager.ConnectedClientsIds
              .ToDictionary(clientId => clientId, clientId => false);

            Debug.Log($"<color={LogColours.Unity}>[ROUND MANAGER]</color> Game initialized with {playersRemaining} players");

        }

        #endregion

        #region Player Functions

        #region Eliminate Player
        [Rpc(SendTo.Server)]
        public void AskEliminatePlayerRPC(ulong clientId)
        {
            _gameManager.scoreManager.AwardFail(clientId);
            TellEliminatePlayerRPC();
            NetworkManager.ConnectedClients[clientId].PlayerObject
                .GetComponent<IDamageable>()
                .Die();

            activePlayers[clientId] = true;

        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TellEliminatePlayerRPC()
        {
            AudioManager.Instance.PlayerRandomClip(GetAudioClip(PlayerSFXType.Eliminated));

        }
        #endregion

        #region Move To Hot Seat
        public Coroutine MoveToHotSeat(ulong clientId, bool reverse = false)
        {
            if (_hotSeatTransform == null) { Debug.LogWarning($"Hot seat transform is null, cannot move player!"); return null; }
            NetworkObject playerObj = NetworkManager.ConnectedClients[clientId].PlayerObject;
            _playerAnimationHandler.SetAnimator(playerObj.GetComponent<Animator>(), "inHotSeat", !reverse);
            return StartCoroutine(MoveToHotSeatCoroutine(playerObj.transform, .5f, reverse, clientId));

        }

        private IEnumerator MoveToHotSeatCoroutine(Transform playerTransform, float duration, bool reverse, ulong clientId)
        {
            Vector3 playerStart = playerTransform.position;
            Vector3 playerTargetPosition = reverse ? _originalPodiumPositions[clientId] : _hotSeatTransform.transform.position;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (playerTransform == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                playerTransform.position = Vector3.Lerp(playerStart, playerTargetPosition, t);

                yield return null;
            }

            playerTransform.position = playerTargetPosition;

        }
        #endregion

        #region Player Leave 
        private void OnUnityClientDisconnect(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected) return;

            if (!networkManager.IsServer) return;
            Debug.Log($"<color={LogColours.Unity}>[PLAYER MANAGER]</color> {connectionEventData.ClientId} has left!");

        }

        #endregion

        #endregion

        #region Utility
        [Rpc(SendTo.ClientsAndHost)]
        public void HandleChangePodiumColorRPC(ulong clientId, Color colour)
        {
            GameObject podium = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetworkedController>().podium;
            if (podium == null) { Debug.LogError($"Player's Podium is null!"); return; }

            podium.transform.GetChild(1).GetComponent<MeshRenderer>().material.color = colour;

        }

        public bool IsPlayerActive(ulong clientId)
        {
            return NetworkManager.ConnectedClients.ContainsKey(clientId) && activePlayers[clientId] == false;
        }

        private List<AudioClip> GetAudioClip(PlayerSFXType sfxType)
        {
            if (!_playerSFX.ContainsKey(sfxType)) { Debug.LogWarning($"No key found for '{sfxType}'!"); return null; }
            return _playerSFX[sfxType];

        }

        #endregion

    }
}