using UnityEngine;
using Utility;

namespace Steamworks
{
    public class SteamManager : PersistentSingleton<SteamManager>
    {
        [Header("Steam Settings")]
        [SerializeField] private uint _appID = 480;
        public static bool connectedToSteam => SteamClient.IsValid;

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
        // INFO: Force the script to spawn
        public bool EstablishSteamConnection()
        {
            if (connectedToSteam) return true; // INFO: Prevent calling more than once

            try
            {
                SteamClient.Init(_appID);
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

    }
}