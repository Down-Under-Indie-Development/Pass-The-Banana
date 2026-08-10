using UnityEngine;
using Steamworks;
using Utility;
// using Unity.VisualScripting;

namespace Steamworks
{
    public class SteamManager : PersistentSingleton<SteamManager>
    {
        [SerializeField] private uint _appID = 480;
        private bool _connectedToSteam => SteamClient.IsValid;

        private void Update()
        {
            SteamClient.RunCallbacks();
        }

        // INFO: Force the script to spawn
        public void StartSteamGame()
        {
            if (_connectedToSteam) return; // INFO: Prevent calling more than once

            try
            {
                SteamClient.Init(_appID);
                _eventManager.OnConnectedToSteam?.Invoke();

            }
            catch (System.Exception e)
            {
                Debug.LogError($"{e.Message}");
            }

        }

        private void OnApplicationQuit()
        {
            if (_connectedToSteam) SteamClient.Shutdown();
        }

    }
}