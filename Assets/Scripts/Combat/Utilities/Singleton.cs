using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// Provides consistent singleton pattern with configurable persistence and auto-creation.
    /// </summary>
    /// <typeparam name="T">The MonoBehaviour type inheriting from this class</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static bool isQuitting = false;

        /// <summary>
        /// Override to true if this singleton should persist across scene loads (uses DontDestroyOnLoad)
        /// </summary>
        protected virtual bool PersistAcrossScenes => false;

        /// <summary>
        /// Override to true if this singleton should auto-create when accessed and no instance exists
        /// </summary>
        protected virtual bool AutoCreateInstance => false;

        /// <summary>
        /// Gets the singleton instance. Returns null if quitting or instance doesn't exist (and AutoCreate is false)
        /// </summary>
        public static T Instance
        {
            get
            {
                // Don't auto-create during shutdown or scene cleanup
                if (isQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    // Try to find existing instance in scene
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        // Check if we should auto-create
                        // Note: We need a temporary instance to check the property
                        // This is a limitation - auto-create must be decided statically or via attribute
                        // For now, derived classes that need auto-create should override the static getter
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Called during Awake. Override to add initialization logic.
        /// Always call base.Awake() first.
        /// </summary>
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;

                if (PersistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }

                OnSingletonAwake();
            }
            else if (instance != this)
            {
                CombatLogger.LogSystem(
                    $"[{typeof(T).Name}] Duplicate instance detected on {gameObject.name} - destroying component only. " +
                    $"Keeping instance on {instance.gameObject.name}",
                    CombatLogger.LogLevel.Warning);
                Destroy(this); // Only destroy the component, not the entire GameObject
            }
        }

        /// <summary>
        /// Override to perform initialization after singleton is established.
        /// Called only on the primary instance.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Called during OnDestroy. Override to add cleanup logic.
        /// Always call base.OnDestroy() to ensure singleton reference is cleared.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            OnSingletonDestroy();
        }

        /// <summary>
        /// Override to perform cleanup before singleton is destroyed.
        /// </summary>
        protected virtual void OnSingletonDestroy() { }

        /// <summary>
        /// Called when the application is quitting.
        /// Prevents null reference errors during shutdown.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Resets the quitting flag. Useful for editor play mode transitions.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            isQuitting = false;
            instance = null;
        }
    }

    /// <summary>
    /// Singleton that auto-creates when accessed if no instance exists.
    /// Use this base class for managers that must always be available.
    /// </summary>
    /// <typeparam name="T">The MonoBehaviour type inheriting from this class</typeparam>
    public abstract class AutoCreateSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        /// <summary>
        /// Gets the singleton instance. Auto-creates if none exists and not quitting.
        /// </summary>
        public new static T Instance
        {
            get
            {
                // Access base to check quitting state
                var baseInstance = Singleton<T>.Instance;
                if (baseInstance != null)
                {
                    return baseInstance;
                }

                // Auto-create if base returned null and we're not quitting
                if (FindFirstObjectByType<T>() == null)
                {
                    var go = new GameObject($"{typeof(T).Name} (Auto-Created)");
                    return go.AddComponent<T>();
                }

                return FindFirstObjectByType<T>();
            }
        }

        protected override bool AutoCreateInstance => true;
    }
}
