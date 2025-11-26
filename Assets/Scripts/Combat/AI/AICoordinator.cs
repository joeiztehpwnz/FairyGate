using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Coordinates attack timing between multiple AI enemies to prevent overwhelming the player
    /// while maintaining consistent pressure. Enemies request permission before attacking.
    /// Uses FormationManager and AttackCoordinator for modular coordination logic.
    /// </summary>
    public class AICoordinator : AutoCreateSingleton<AICoordinator>, IAICombatCoordinator
    {

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

        // Coordination Components
        private FormationManager formationManager;
        private AttackCoordinator attackCoordinator;

        private List<IAIAgent> registeredEnemies = new List<IAIAgent>();
        private Transform playerTransform; // Cached player reference for slot positioning

        protected override void OnSingletonAwake()
        {
            // Initialize coordination components
            InitializeComponents();
        }

        /// <summary>
        /// Initializes the formation and attack coordination components.
        /// </summary>
        private void InitializeComponents()
        {
            // Initialize Formation Manager
            formationManager = new FormationManager(useFormationSystem, enableDebugLogs);

            // Initialize Attack Coordinator
            attackCoordinator = new AttackCoordinator(
                maxSimultaneousAttackers,
                minTimeBetweenAttacks,
                attackDuration,
                berserkerPriority,
                soldierPriority,
                assassinPriority,
                guardianPriority,
                archerPriority,
                enableDebugLogs
            );
        }

        protected override void OnSingletonDestroy()
        {

            // Clear all lists to prevent stale references
            if (registeredEnemies != null)
            {
                registeredEnemies.Clear();
            }
            if (attackCoordinator != null)
            {
                attackCoordinator.ClearAllSlots();
            }
            if (formationManager != null)
            {
                formationManager.ClearAllSlots();
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogAI("[AICoordinator] OnDestroy - Cleared all enemy lists and coordination components");
            }
        }

        private void Update()
        {
            // Find player if not cached and update formation manager
            if (playerTransform == null)
            {
                FindPlayer();
            }

            // Update formation manager's player reference
            if (formationManager != null && formationManager.PlayerTransform != playerTransform)
            {
                formationManager.PlayerTransform = playerTransform;
            }
        }

        /// <summary>
        /// Finds and caches the player transform reference using faction system.
        /// </summary>
        private void FindPlayer()
        {
            // Find the player by looking for CombatController with Player faction
            var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var combatant in combatants)
            {
                if (combatant.Faction == Faction.Player)
                {
                    playerTransform = combatant.transform;
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogAI($"[AICoordinator] Found player: {playerTransform.name}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Register an enemy AI when it becomes active or enters combat
        /// </summary>
        public void RegisterEnemy(IAIAgent enemy)
        {
            if (!registeredEnemies.Contains(enemy))
            {
                registeredEnemies.Add(enemy);
                if (enableDebugLogs)
                {
                    CombatLogger.LogAI($"AICoordinator: Registered {enemy.name} (Total: {registeredEnemies.Count})");
                }
            }
        }

        /// <summary>
        /// Unregister an enemy AI when it dies or exits combat
        /// </summary>
        public void UnregisterEnemy(IAIAgent enemy)
        {
            registeredEnemies.Remove(enemy);

            // Remove from attack coordinator
            if (attackCoordinator != null)
            {
                attackCoordinator.RemoveAttacker(enemy);
            }

            // Release formation slot if occupied
            if (formationManager != null)
            {
                formationManager.ReleaseFormationSlot(enemy);
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogAI($"AICoordinator: Unregistered {enemy.name} (Remaining: {registeredEnemies.Count})");
            }
        }

        /// <summary>
        /// Request permission to perform an attack. Returns true if attack is allowed.
        /// </summary>
        public bool RequestAttackPermission(IAIAgent requester)
        {
            if (!registeredEnemies.Contains(requester))
            {
                // Auto-register if not already registered
                RegisterEnemy(requester);
            }

            // Delegate to attack coordinator
            return attackCoordinator != null && attackCoordinator.RequestAttackPermission(requester, registeredEnemies);
        }

        /// <summary>
        /// Notify coordinator that an attack has completed early (e.g., interrupted)
        /// </summary>
        public void ReleaseAttackSlot(IAIAgent attacker)
        {
            if (attackCoordinator != null)
            {
                attackCoordinator.ReleaseAttackSlot(attacker);
            }
        }

        /// <summary>
        /// Request a formation slot position. Returns null if no slots available or formation system disabled.
        /// </summary>
        public Vector3? RequestFormationSlot(IAIAgent requester, float desiredDistance)
        {
            return formationManager?.RequestFormationSlot(requester, desiredDistance);
        }

        /// <summary>
        /// Release formation slot occupied by this enemy
        /// </summary>
        public void ReleaseFormationSlot(IAIAgent enemy)
        {
            formationManager?.ReleaseFormationSlot(enemy);
        }

        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUILayout.BeginArea(new Rect(10, 200, 300, 250));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>AI Coordinator</b>");
            GUILayout.Label($"Registered Enemies: {registeredEnemies.Count}");

            if (attackCoordinator != null)
            {
                GUILayout.Label($"Active Attackers: {attackCoordinator.ActiveAttackerCount}/{maxSimultaneousAttackers}");
                GUILayout.Label($"Time Since Last: {attackCoordinator.TimeSinceLastAttack:F1}s");
            }

            // Formation System Info
            if (formationManager != null && formationManager.IsEnabled)
            {
                GUILayout.Space(5);
                GUILayout.Label($"<b>Formation Slots: {formationManager.OccupiedSlotCount}/{formationManager.TotalSlotCount}</b>");
            }

            // Display active attackers
            if (attackCoordinator != null)
            {
                var activeAttackers = attackCoordinator.GetActiveAttackers();
                if (activeAttackers.Count > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("<b>Current Attackers:</b>");
                    foreach (var (attacker, remainingTime) in activeAttackers)
                    {
                        if (attacker != null)
                        {
                            GUILayout.Label($"  • {attacker.name} ({remainingTime:F1}s left)");
                        }
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            // Delegate to formation manager
            formationManager?.DrawDebugGizmos(showFormationGizmos);
        }
    }
}
