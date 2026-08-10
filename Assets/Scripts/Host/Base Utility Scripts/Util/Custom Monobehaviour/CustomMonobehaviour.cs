using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Custom Monobehaviour script to hold functionality I want all script to have
    /// </summary>
    public class CustomMonoBehaviour : MonoBehaviour
    {
        protected EventManager _eventManager;
        protected GameManager _gameManager;

        [Header("Debug Settings")]
        [SerializeField] protected bool _debug;

        protected virtual void Awake()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null) Debug.LogError($"GameManager is null!");

            _eventManager = EventManager.Instance;
            if (_eventManager == null) Debug.LogError($"EventManager is null!");

        }
    }
}