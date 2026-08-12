using UnityEngine;
using Utility;

namespace Steamworks
{
    public class SteamManager : PersistentSingleton<SteamManager>
    {
        [Header("Steam Settings")]
        [field: SerializeField] public uint appID { get; private set; } = 480;
        public bool connectedToSteam => SteamClient.IsValid;

        // INFO: Connect to steam
        protected override void Awake()
        {
            base.Awake();
            EstablishSteamConnection();

        }

        private void Update()
        {
            if (connectedToSteam) SteamClient.RunCallbacks();
        }

        #region Steam Connection
        #region Establish Connection
        // INFO: Establish connection to steam servers
        public bool EstablishSteamConnection()
        {
            if (connectedToSteam) return true; // INFO: Prevent calling more than once

            try
            {
                SteamClient.Init(appID);
                string connectionStatus = connectedToSteam ? $"Connected to steam! | {SteamClient.Name} ({SteamClient.AppId})" : "Connection failed";
                Debug.Log($"{connectionStatus}");

                _eventManager.OnConnectedToSteam?.Invoke();


            }
            catch (System.Exception e)
            {
                Debug.LogError($"{e.Message}");
                return false;
            }

            return true;

        }
        #endregion

        #region Terminate Connection
        // INFO: Disconnect from steam
        public void TerminateSteamConnection()
        {
            if (!connectedToSteam) return;

            try
            {
                SteamClient.Shutdown();
                if (!connectedToSteam) Debug.Log($"Connection terminated successfully!");
            }
            catch (System.Exception e)
            {
                if (connectedToSteam) Debug.LogError($"{e.Message}");

            }
        }
        #endregion

        // INFO: Ensure correct termination
        private void OnDestroy() => TerminateSteamConnection();
        private void OnDisable() => TerminateSteamConnection();
        private void OnApplicationQuit() => TerminateSteamConnection();
        #endregion

    }
}