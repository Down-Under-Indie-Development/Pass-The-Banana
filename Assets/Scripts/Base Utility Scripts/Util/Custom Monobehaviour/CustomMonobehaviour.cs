using Managers;
using Events;
using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Custom Monobehaviour script to hold functionality I want all script to have
    /// </summary>
    public class CustomMonoBehaviour : MonoBehaviour
    {
        protected BaseEventManager _eventManager;
        protected BaseGameManager _gameManager;

        [Header("Debug Settings")]
        [SerializeField] private bool _debug;

        protected virtual void Awake()
        {
            _gameManager = BaseGameManager.Instance;
            if (_gameManager == null) Debug.LogError($"GameManager is null!");

            _eventManager = BaseEventManager.Instance;
            if (_eventManager == null) Debug.LogError($"EventManager is null!");

        }
    }
}