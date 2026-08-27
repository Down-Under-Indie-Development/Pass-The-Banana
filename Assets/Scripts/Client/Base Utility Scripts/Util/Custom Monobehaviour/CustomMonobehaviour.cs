using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Custom Monobehaviour script to hold functionality I want all script to have
    /// </summary>
    public class CustomMonoBehaviour : MonoBehaviour
    {
        protected EventManager _eventManager => EventManager.Instance;
        protected GameNetworkManager _gameManager => GameNetworkManager.Instance;

        [Header("Debug Settings")]
        [SerializeField] protected bool _debug;

    }
}