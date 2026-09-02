using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Creates a singleton in the scene when called to allow for public static access to 
/// the attached functions
/// </summary>
namespace Utility
{
    #region Singleton
    public abstract class Singleton<T> : CustomMonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        public static bool hasInstance => instance != null;

        // INFO: Set the instance
        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = CreateSingletonInstance();

                return instance;

            }
        }

        // GUARD: override to opt out of being parented under "Singletons" (e.g. PersistentSingleton)
        protected virtual bool _parentUnderSingletonsContainer => true;

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;
            OnInstanceCreated();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        protected static T CreateSingletonInstance()
        {
            if (!Application.isPlaying) return null;

            instance = FindAnyObjectByType<T>();

            if (instance != null) return instance;

            GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
            instance = singletonObject.AddComponent<T>();

            // GUARD: skip reparenting if the instance opted out
            if (!(instance as Singleton<T>)._parentUnderSingletonsContainer) return instance;

            GameObject singletonObjectParent = GameObject.Find("Singletons") ?? new GameObject("Singletons");
            singletonObject.transform.SetParent(singletonObjectParent.transform);

            return instance;
        }
        protected virtual void OnInstanceCreated() { }

    }

    #region Persistent Singleton
    /// <summary>
    /// Creates a singleton in the scene when called but is set to not destroy on load allowing it to stay in the scene 
    /// when transitioning between scenes
    /// </summary>
    public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        // GUARD: never let CreateSingletonInstance reparent this under "Singletons"
        protected override bool _parentUnderSingletonsContainer => false;

        protected override void OnInstanceCreated()
        {
            gameObject.transform.SetParent(null); // INFO: Ensure parent is null
            DontDestroyOnLoad(gameObject);

        }
    }
    #endregion
    #endregion

    #region Network Singleton
    #region Networked Singleton
    public abstract class NetworkedSingleton<T> : NetworkBehaviour where T : MonoBehaviour
    {
        protected virtual EventManager _eventManager => EventManager.Instance;
        private static T instance = null;
        public static bool hasInstance => instance != null;

        public static T Instance => instance;
        protected virtual void Awake()
        {
            SetSingleton();
            OnInstanceCreated();

        }

        private void SetSingleton()
        {
            instance = this as T;

        }

        private void ResetSingleton()
        {
            instance = null;
        }

        public override void OnDestroy()
        {
            // base.OnDestroy();
            if (instance == this) ResetSingleton();

        }

        protected virtual void OnInstanceCreated() { }

    }
    #endregion

    #region Persistent Networked Singleton
    public class PersistentNetworkSingleton<T> : NetworkedSingleton<T> where T : NetworkBehaviour
    {
        protected override void OnInstanceCreated()
        {
            gameObject.transform.SetParent(null);
            // DontDestroyOnLoad(gameObject);
        }
    }

    #endregion

    #endregion

}