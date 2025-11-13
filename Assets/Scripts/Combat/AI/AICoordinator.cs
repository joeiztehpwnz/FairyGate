using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Coordinates attack timing between multiple AI enemies to prevent overwhelming the player
    /// while maintaining consistent pressure. Enemies request permission before attacking.
    /// </summary>
    public class AICoordinator : MonoBehaviour, IAICombatCoordinator
    {
        private static AICoordinator instance;
        private static bool isQuitting = false;

        public static AICoordinator Instance
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
                    // Fix #1: Check for scene-placed instance before auto-creating
                    instance = FindFirstObjectByType<AICoordinator>();

                    if (instance == null)
                    {
                        // No instance exists, create one
                        var go = new GameObject("AICoordinator (Auto-Created)");
                        instance = go.AddComponent<AICoordinator>();
                        // Note: DontDestroyOnLoad removed - let scene transitions clean up auto-created instances
                        // Scene-placed instances can still use DontDestroyOnLoad in Awake if needed
                    }
                }
                return instance;
            }
        }

        [Header("Attack Timing Configuration")]
        [SerializeField] private int maxSimultaneousAttackers = 1;
        [SerializeField] private float minTimeBetweenAttacks = 0.8f;
        [SerializeField] private float attackDuration = 3.0f; // How long to reserve an attack slot (conservative: accounts for charge + execution + recovery)
                                                               // Based on slowest skill (Smash: 2.0s charge + ~1s execution/recovery)

        [Header("Priority Weights (Higher = More Aggressive)")]
        [SerializeField] private float berserkerPriority = 1.5f;
        [SerializeField] private float soldierPriority = 1.0f;
        [SerializeField] private float assassinPriority = 1.2f;
        [SerializeField] private float guardianPriority = 0.7f;
        [SerializeField] private float archerPriority = 0.9f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDebugGUI = true;

        [Header("Formation System")]
        [SerializeField] private bool useFormationSystem = true;
        [SerializeField] private bool showFormationGizmos = true;

        private class AttackSlot
        {
            public SimpleTestAI attacker;
            public float endTime;
        }

        private class FormationSlot
        {
            public int slotIndex;
            public Vector3 baseDirection; // Direction from player (0-360 degrees)
            public SimpleTestAI occupant; // Current AI occupying this slot
            public float lastAssignTime; // For preventing slot thrashing
        }

        private List<SimpleTestAI> registeredEnemies = new List<SimpleTestAI>();
        private List<AttackSlot> activeAttackers = new List<AttackSlot>();
        private float lastAttackGrantedTime = -999f;

        // Formation system
        private List<FormationSlot> formationSlots = new List<FormationSlot>();
        private Transform playerTransform; // Cached player reference for slot positioning

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                // NOTE: Removed DontDestroyOnLoad to allow proper scene cleanup
                // Auto-created instances will be destroyed with the scene
                // This prevents "objects not cleaned up" warning
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            // Initialize formation slots
            InitializeFormationSlots();
        }

        private void InitializeFormationSlots()
        {
            formationSlots.Clear();

            // Create 8 slots in a circle (45 degrees apart)
            float angleStep = 360f / CombatConstants.FORMATION_SLOT_COUNT;

            for (int i = 0; i < CombatConstants.FORMATION_SLOT_COUNT; i++)
            {
                float angle = i * angleStep;
                float radians = angle * Mathf.Deg2Rad;

                formationSlots.Add(new FormationSlot
                {
                    slotIndex = i,
                    baseDirection = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)),
                    occupant = null,
                    lastAssignTime = -999f
                });
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[AICoordinator] Initialized {formationSlots.Count} formation slots");
            }
        }

        private void OnDestroy()
        {
            // Clear singleton reference when destroyed
            if (instance == this)
            {
                instance = null;
            }

            // Clear all lists to prevent stale references
            if (registeredEnemies != null)
            {
                registeredEnemies.Clear();
            }
            if (activeAttackers != null)
            {
                activeAttackers.Clear();
            }
            if (formationSlots != null)
            {
                formationSlots.Clear();
            }

            if (enableDebugLogs)
            {
                Debug.Log("[AICoordinator] OnDestroy - Cleared all enemy lists and formation slots");
            }
        }

        private void OnApplicationQuit()
        {
            // Prevent auto-creation during application shutdown
            isQuitting = true;
        }

        private void Update()
        {
            // Find player if not cached
            if (playerTransform == null)
            {
                FindPlayer();
            }
        }

        private void FindPlayer()
        {
            // Find the player by looking for CombatController with "Player" in name
            var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var combatant in combatants)
            {
                if (combatant.name.Contains("Player"))
                {
                    playerTransform = combatant.transform;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[AICoordinator] Found player: {playerTransform.name}");
                    }
                    break;
                }
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

            // Release formation slot if occupied
            ReleaseFormationSlot(enemy);

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

            // Check if requester is actually ready to attack (Fix #15)
            if (requester == null || !requester.IsReadyToAttack)
            {
                if (enableDebugLogs && requester != null)
                {
                    Debug.Log($"AICoordinator: Denied {requester.name} - requester not ready to attack");
                }
                return false;
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
                    // Check if this enemy is also ready to attack (Fix #3: Null-safe property access)
                    try
                    {
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
                    catch (System.NullReferenceException)
                    {
                        // Enemy components were destroyed, skip this enemy
                        // Will be cleaned up on next registration/unregistration cycle
                        continue;
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

        /// <summary>
        /// Request a formation slot position. Returns null if no slots available or formation system disabled.
        /// </summary>
        public Vector3? RequestFormationSlot(SimpleTestAI requester, float desiredDistance)
        {
            if (!useFormationSystem || playerTransform == null)
                return null;

            // Check if requester already has a slot
            FormationSlot currentSlot = formationSlots.Find(slot => slot.occupant == requester);
            if (currentSlot != null)
            {
                // Already has a slot, return updated position
                return CalculateSlotPosition(currentSlot, desiredDistance);
            }

            // Find nearest available slot
            FormationSlot bestSlot = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < formationSlots.Count; i++)
            {
                FormationSlot slot = formationSlots[i];

                // Skip occupied slots
                if (slot.occupant != null && slot.occupant != requester)
                    continue;

                // Skip slots on cooldown (prevent thrashing)
                if (Time.time - slot.lastAssignTime < CombatConstants.FORMATION_SLOT_REASSIGN_COOLDOWN)
                    continue;

                // Calculate score (prefer slots closer to requester's current position)
                Vector3 slotPosition = CalculateSlotPosition(slot, desiredDistance);
                float distance = Vector3.Distance(requester.transform.position, slotPosition);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestSlot = slot;
                }
            }

            if (bestSlot != null)
            {
                // Assign slot
                bestSlot.occupant = requester;
                bestSlot.lastAssignTime = Time.time;

                if (enableDebugLogs)
                {
                    Debug.Log($"[AICoordinator] Assigned formation slot {bestSlot.slotIndex} to {requester.name}");
                }

                return CalculateSlotPosition(bestSlot, desiredDistance);
            }

            // No slots available
            return null;
        }

        /// <summary>
        /// Release formation slot occupied by this enemy
        /// </summary>
        public void ReleaseFormationSlot(SimpleTestAI enemy)
        {
            for (int i = 0; i < formationSlots.Count; i++)
            {
                if (formationSlots[i].occupant == enemy)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[AICoordinator] Released formation slot {formationSlots[i].slotIndex} from {enemy.name}");
                    }

                    formationSlots[i].occupant = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Calculate world position for a formation slot
        /// </summary>
        private Vector3 CalculateSlotPosition(FormationSlot slot, float distance)
        {
            if (playerTransform == null)
                return Vector3.zero;

            // Base position = player + (direction * distance)
            Vector3 basePosition = playerTransform.position + (slot.baseDirection * distance);

            // Add small random offset for variation (per slot, consistent)
            Vector3 offset = new Vector3(
                Mathf.Sin(slot.slotIndex * 1.23f) * CombatConstants.FORMATION_SLOT_OFFSET,
                0f,
                Mathf.Cos(slot.slotIndex * 2.34f) * CombatConstants.FORMATION_SLOT_OFFSET
            );

            return basePosition + offset;
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

            GUILayout.BeginArea(new Rect(10, 200, 300, 250));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>AI Coordinator</b>");
            GUILayout.Label($"Registered Enemies: {registeredEnemies.Count}");
            GUILayout.Label($"Active Attackers: {activeAttackers.Count}/{maxSimultaneousAttackers}");

            float timeSinceLast = Time.time - lastAttackGrantedTime;
            GUILayout.Label($"Time Since Last: {timeSinceLast:F1}s");

            // Formation System Info
            if (useFormationSystem)
            {
                int occupiedSlots = formationSlots.Count(s => s.occupant != null);
                GUILayout.Space(5);
                GUILayout.Label($"<b>Formation Slots: {occupiedSlots}/{formationSlots.Count}</b>");
            }

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

        private void OnDrawGizmos()
        {
            if (!showFormationGizmos || !useFormationSystem || playerTransform == null)
                return;

            // Draw formation slots
            for (int i = 0; i < formationSlots.Count; i++)
            {
                FormationSlot slot = formationSlots[i];

                // Use average distance for visualization (2.0 units)
                Vector3 slotPosition = CalculateSlotPosition(slot, 2.0f);

                // Color based on occupancy
                Gizmos.color = slot.occupant != null ? Color.red : Color.green;

                // Draw sphere at slot position
                Gizmos.DrawWireSphere(slotPosition, 0.3f);

                // Draw line from player to slot
                Gizmos.color = slot.occupant != null ? new Color(1f, 0.5f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f, 0.3f);
                Gizmos.DrawLine(playerTransform.position, slotPosition);

                #if UNITY_EDITOR
                // Label slot number
                UnityEditor.Handles.Label(
                    slotPosition + Vector3.up * 0.5f,
                    $"Slot {i}\n{(slot.occupant != null ? slot.occupant.name : "Empty")}"
                );
                #endif
            }
        }
    }
}
