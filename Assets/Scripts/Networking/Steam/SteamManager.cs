using UnityEngine;
using Utility;
using Steamworks;

namespace PTB.Networking
{
    public class SteamManager : PersistentSingleton<SteamManager>
    {
        [Header("Steam Settings")]
        [field: SerializeField] public uint appID { get; private set; } = 480;
        public bool connectedToSteam => SteamClient.IsValid;
        private UnityNetworkHelper _networkHelper => UnityNetworkHelper.Instance;

        // INFO: Connect to steam
        protected override void Awake()
        {
            base.Awake();
            EstablishClientSteamConnection();

        }

        #region Steam Connection
        #region Establish Connection
        // INFO: Establish connection to steam servers
        public bool EstablishClientSteamConnection()
        {
            if (connectedToSteam) return true; // INFO: Prevent calling more than once

            try
            {
                SteamClient.Init(appID);
                Debug.Log($"<color=orange>{_networkHelper.CheckPrivilege()}</color> Successfully Connected to steam! | {SteamClient.Name} ({SteamClient.AppId})");
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
                if (!connectedToSteam) Debug.Log($"<color=orange>[CLIENT]</color> Connection terminated successfully!");

            }
            catch (System.Exception e)
            {
                if (connectedToSteam) Debug.LogError($"{e.Message}");

            }
        }
        #endregion

        // INFO: Ensure correct termination
        private void OnDestroy() => TerminateSteamConnection();
        private void OnApplicationQuit() => TerminateSteamConnection();
        #endregion

    }
}