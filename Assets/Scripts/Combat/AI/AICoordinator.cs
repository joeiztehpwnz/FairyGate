using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Coordinates attack timing between multiple AI enemies to prevent overwhelming the player
    /// while maintaining consistent pressure. Enemies request permission before attacking.
    /// </summary>
    public class AICoordinator : MonoBehaviour
    {
        private static AICoordinator instance;
        public static AICoordinator Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AICoordinator");
                    instance = go.AddComponent<AICoordinator>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Attack Timing Configuration")]
        [SerializeField] private int maxSimultaneousAttackers = 1;
        [SerializeField] private float minTimeBetweenAttacks = 0.8f;
        [SerializeField] private float attackDuration = 2.0f; // How long to reserve an attack slot

        [Header("Priority Weights (Higher = More Aggressive)")]
        [SerializeField] private float berserkerPriority = 1.5f;
        [SerializeField] private float soldierPriority = 1.0f;
        [SerializeField] private float assassinPriority = 1.2f;
        [SerializeField] private float guardianPriority = 0.7f;
        [SerializeField] private float archerPriority = 0.9f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDebugGUI = true;

        private class AttackSlot
        {
            public SimpleTestAI attacker;
            public float endTime;
        }

        private List<SimpleTestAI> registeredEnemies = new List<SimpleTestAI>();
        private List<AttackSlot> activeAttackers = new List<AttackSlot>();
        private float lastAttackGrantedTime = -999f;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Register an enemy AI when it becomes active or enters combat
        /// </summary>
        public void RegisterEnemy(SimpleTestAI enemy)
        {
            if (!registeredEnemies.Contains(enemy))
            {
                registeredEnemies.Add(enemy);
                if (enableDebugLogs)
                {
                    Debug.Log($"AICoordinator: Registered {enemy.name} (Total: {registeredEnemies.Count})");
                }
            }
        }

        /// <summary>
        /// Unregister an enemy AI when it dies or exits combat
        /// </summary>
        public void UnregisterEnemy(SimpleTestAI enemy)
        {
            registeredEnemies.Remove(enemy);

            // Remove from active attackers if present
            activeAttackers.RemoveAll(slot => slot.attacker == enemy);

            if (enableDebugLogs)
            {
                Debug.Log($"AICoordinator: Unregistered {enemy.name} (Remaining: {registeredEnemies.Count})");
            }
        }

        /// <summary>
        /// Request permission to perform an attack. Returns true if attack is allowed.
        /// </summary>
        public bool RequestAttackPermission(SimpleTestAI requester)
        {
            if (!registeredEnemies.Contains(requester))
            {
                // Auto-register if not already registered
                RegisterEnemy(requester);
            }

            // Clean up expired attack slots
            CleanupExpiredSlots();

            // Check if we're at max capacity
            if (activeAttackers.Count >= maxSimultaneousAttackers)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"AICoordinator: Denied {requester.name} - at max capacity ({activeAttackers.Count}/{maxSimultaneousAttackers})");
                }
                return false;
            }

            // Check if minimum time has passed since last attack
            float timeSinceLastAttack = Time.time - lastAttackGrantedTime;
            if (timeSinceLastAttack < minTimeBetweenAttacks)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"AICoordinator: Denied {requester.name} - too soon ({timeSinceLastAttack:F2}s < {minTimeBetweenAttacks:F2}s)");
                }
                return false;
            }

            // Check priority - if there are other enemies waiting with higher priority, deny
            float requesterPriority = GetEnemyPriority(requester);
            foreach (var enemy in registeredEnemies)
            {
                if (enemy != requester && enemy != null)
                {
                    // Check if this enemy is also ready to attack
                    if (enemy.IsReadyToAttack && GetEnemyPriority(enemy) > requesterPriority)
                    {
                        // Higher priority enemy is waiting, deny this request
                        if (enableDebugLogs)
                        {
                            Debug.Log($"AICoordinator: Denied {requester.name} - higher priority enemy {enemy.name} waiting");
                        }
                        return false;
                    }
                }
            }

            // Grant permission
            GrantAttackSlot(requester);
            return true;
        }

        /// <summary>
        /// Notify coordinator that an attack has completed early (e.g., interrupted)
        /// </summary>
        public void ReleaseAttackSlot(SimpleTestAI attacker)
        {
            activeAttackers.RemoveAll(slot => slot.attacker == attacker);
            if (enableDebugLogs)
            {
                Debug.Log($"AICoordinator: Released attack slot for {attacker.name}");
            }
        }

        private void GrantAttackSlot(SimpleTestAI attacker)
        {
            var slot = new AttackSlot
            {
                attacker = attacker,
                endTime = Time.time + attackDuration
            };

            activeAttackers.Add(slot);
            lastAttackGrantedTime = Time.time;

            if (enableDebugLogs)
            {
                Debug.Log($"AICoordinator: Granted attack slot to {attacker.name} (Active: {activeAttackers.Count}/{maxSimultaneousAttackers})");
            }
        }

        private void CleanupExpiredSlots()
        {
            int beforeCount = activeAttackers.Count;
            activeAttackers.RemoveAll(slot => Time.time >= slot.endTime || slot.attacker == null);

            if (enableDebugLogs && activeAttackers.Count < beforeCount)
            {
                Debug.Log($"AICoordinator: Cleaned up {beforeCount - activeAttackers.Count} expired slots");
            }
        }

        private float GetEnemyPriority(SimpleTestAI enemy)
        {
            // Try to determine archetype from name or stats
            string enemyName = enemy.name.ToLower();

            if (enemyName.Contains("berserker")) return berserkerPriority;
            if (enemyName.Contains("assassin")) return assassinPriority;
            if (enemyName.Contains("guardian")) return guardianPriority;
            if (enemyName.Contains("archer")) return archerPriority;
            if (enemyName.Contains("soldier")) return soldierPriority;

            // Default priority
            return soldierPriority;
        }

        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUILayout.BeginArea(new Rect(10, 200, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>AI Coordinator</b>");
            GUILayout.Label($"Registered Enemies: {registeredEnemies.Count}");
            GUILayout.Label($"Active Attackers: {activeAttackers.Count}/{maxSimultaneousAttackers}");

            float timeSinceLast = Time.time - lastAttackGrantedTime;
            GUILayout.Label($"Time Since Last: {timeSinceLast:F1}s");

            if (activeAttackers.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("<b>Current Attackers:</b>");
                foreach (var slot in activeAttackers)
                {
                    if (slot.attacker != null)
                    {
                        float remaining = slot.endTime - Time.time;
                        GUILayout.Label($"  • {slot.attacker.name} ({remaining:F1}s left)");
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
