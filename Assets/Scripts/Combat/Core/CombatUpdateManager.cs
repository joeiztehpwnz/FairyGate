using System.Collections.Generic;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Centralized update manager for combat systems.
    /// Consolidates multiple Update() calls into a single managed loop for performance.
    /// </summary>
    public class CombatUpdateManager : MonoBehaviour
    {
        private static CombatUpdateManager instance;
        public static CombatUpdateManager Instance => instance;

        private List<ICombatUpdatable> updatables = new List<ICombatUpdatable>(CombatConstants.COMBAT_UPDATABLES_INITIAL_CAPACITY);
        private List<ICombatFixedUpdatable> fixedUpdatables = new List<ICombatFixedUpdatable>(16);

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showUpdateStats = false;

        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log("[CombatUpdateManager] Initialized");
            }
        }

        public static void Register(ICombatUpdatable updatable)
        {
            if (instance != null && !instance.updatables.Contains(updatable))
            {
                instance.updatables.Add(updatable);

                if (instance.enableDebugLogs)
                {
                    Debug.Log($"[CombatUpdateManager] Registered {updatable.GetType().Name} ({instance.updatables.Count} total updatables)");
                }
            }
        }

        public static void Unregister(ICombatUpdatable updatable)
        {
            if (instance != null)
            {
                instance.updatables.Remove(updatable);

                if (instance.enableDebugLogs)
                {
                    Debug.Log($"[CombatUpdateManager] Unregistered {updatable.GetType().Name} ({instance.updatables.Count} remaining)");
                }
            }
        }

        public static void RegisterFixed(ICombatFixedUpdatable updatable)
        {
            if (instance != null && !instance.fixedUpdatables.Contains(updatable))
            {
                instance.fixedUpdatables.Add(updatable);

                if (instance.enableDebugLogs)
                {
                    Debug.Log($"[CombatUpdateManager] Registered fixed updatable {updatable.GetType().Name}");
                }
            }
        }

        public static void UnregisterFixed(ICombatFixedUpdatable updatable)
        {
            if (instance != null)
            {
                instance.fixedUpdatables.Remove(updatable);

                if (instance.enableDebugLogs)
                {
                    Debug.Log($"[CombatUpdateManager] Unregistered fixed updatable {updatable.GetType().Name}");
                }
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // Single consolidated update loop for all combat systems
            for (int i = 0; i < updatables.Count; i++)
            {
                updatables[i].CombatUpdate(deltaTime);
            }
        }

        private void FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;

            for (int i = 0; i < fixedUpdatables.Count; i++)
            {
                fixedUpdatables[i].CombatFixedUpdate(fixedDeltaTime);
            }
        }

        private void OnGUI()
        {
            if (showUpdateStats && Application.isPlaying)
            {
                GUI.Label(new Rect(10, 50, 300, 60),
                    $"Combat Update Manager\n" +
                    $"Updatables: {updatables.Count}\n" +
                    $"Fixed Updatables: {fixedUpdatables.Count}");
            }
        }
    }

    /// <summary>
    /// Interface for systems that need to update every frame.
    /// </summary>
    public interface ICombatUpdatable
    {
        void CombatUpdate(float deltaTime);
    }

    /// <summary>
    /// Interface for systems that need fixed timestep updates (physics-related).
    /// </summary>
    public interface ICombatFixedUpdatable
    {
        void CombatFixedUpdate(float fixedDeltaTime);
    }
}
