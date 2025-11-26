using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Manages attack timing and permission for AI enemies.
    /// Prevents overwhelming the player by limiting simultaneous attackers
    /// and enforcing time intervals between attacks.
    /// </summary>
    public class AttackCoordinator
    {
        /// <summary>
        /// Represents an active attack slot with timing information.
        /// </summary>
        private class AttackSlot
        {
            public IAIAgent attacker;
            public float endTime;
        }

        private readonly List<AttackSlot> activeAttackers = new List<AttackSlot>();
        private float lastAttackGrantedTime = -999f;

        private readonly int maxSimultaneousAttackers;
        private readonly float minTimeBetweenAttacks;
        private readonly float attackDuration;
        private readonly bool enableDebugLogs;

        // Priority weights for different enemy types
        private readonly float berserkerPriority;
        private readonly float soldierPriority;
        private readonly float assassinPriority;
        private readonly float guardianPriority;
        private readonly float archerPriority;

        /// <summary>
        /// Gets the count of currently active attackers.
        /// </summary>
        public int ActiveAttackerCount => activeAttackers.Count;

        /// <summary>
        /// Gets the time since the last attack was granted.
        /// </summary>
        public float TimeSinceLastAttack => Time.time - lastAttackGrantedTime;

        /// <summary>
        /// Initializes a new instance of the AttackCoordinator.
        /// </summary>
        /// <param name="maxSimultaneousAttackers">Maximum number of simultaneous attackers allowed</param>
        /// <param name="minTimeBetweenAttacks">Minimum time between granting attack permissions</param>
        /// <param name="attackDuration">How long to reserve an attack slot</param>
        /// <param name="berserkerPriority">Priority weight for berserker archetype</param>
        /// <param name="soldierPriority">Priority weight for soldier archetype</param>
        /// <param name="assassinPriority">Priority weight for assassin archetype</param>
        /// <param name="guardianPriority">Priority weight for guardian archetype</param>
        /// <param name="archerPriority">Priority weight for archer archetype</param>
        /// <param name="enableDebugLogs">Whether to log debug messages</param>
        public AttackCoordinator(
            int maxSimultaneousAttackers,
            float minTimeBetweenAttacks,
            float attackDuration,
            float berserkerPriority,
            float soldierPriority,
            float assassinPriority,
            float guardianPriority,
            float archerPriority,
            bool enableDebugLogs)
        {
            this.maxSimultaneousAttackers = maxSimultaneousAttackers;
            this.minTimeBetweenAttacks = minTimeBetweenAttacks;
            this.attackDuration = attackDuration;
            this.berserkerPriority = berserkerPriority;
            this.soldierPriority = soldierPriority;
            this.assassinPriority = assassinPriority;
            this.guardianPriority = guardianPriority;
            this.archerPriority = archerPriority;
            this.enableDebugLogs = enableDebugLogs;
        }

        /// <summary>
        /// Request permission to perform an attack.
        /// Evaluates capacity, timing, and priority to determine if attack should be allowed.
        /// </summary>
        /// <param name="requester">The AI agent requesting attack permission</param>
        /// <param name="registeredEnemies">List of all registered enemies for priority comparison</param>
        /// <returns>True if attack permission is granted, false otherwise</returns>
        public bool RequestAttackPermission(IAIAgent requester, List<IAIAgent> registeredEnemies)
        {
            // Check if requester is actually ready to attack (Fix #15)
            if (requester == null || !requester.IsReadyToAttack)
            {
                if (enableDebugLogs && requester != null)
                {
                    CombatLogger.LogAttack($"[AttackCoordinator] Denied {requester.name} - requester not ready to attack");
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
                    CombatLogger.LogAttack($"[AttackCoordinator] Denied {requester.name} - at max capacity ({activeAttackers.Count}/{maxSimultaneousAttackers})");
                }
                return false;
            }

            // Check if minimum time has passed since last attack
            float timeSinceLastAttack = Time.time - lastAttackGrantedTime;
            if (timeSinceLastAttack < minTimeBetweenAttacks)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogAttack($"[AttackCoordinator] Denied {requester.name} - too soon ({timeSinceLastAttack:F2}s < {minTimeBetweenAttacks:F2}s)");
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
                                CombatLogger.LogAttack($"[AttackCoordinator] Denied {requester.name} - higher priority enemy {enemy.name} waiting");
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
        /// Releases an attack slot, making it available for other attackers.
        /// Call this when an attack completes early (e.g., interrupted).
        /// </summary>
        /// <param name="attacker">The AI agent releasing its attack slot</param>
        public void ReleaseAttackSlot(IAIAgent attacker)
        {
            int beforeCount = activeAttackers.Count;
            activeAttackers.RemoveAll(slot => slot.attacker == attacker);

            if (enableDebugLogs && activeAttackers.Count < beforeCount)
            {
                CombatLogger.LogAttack($"[AttackCoordinator] Released attack slot for {attacker.name}");
            }
        }

        /// <summary>
        /// Removes an attacker from active attack slots (used when enemy dies or unregisters).
        /// </summary>
        /// <param name="enemy">The AI agent to remove from attack slots</param>
        public void RemoveAttacker(IAIAgent enemy)
        {
            activeAttackers.RemoveAll(slot => slot.attacker == enemy);
        }

        /// <summary>
        /// Clears all active attack slots (used during cleanup).
        /// </summary>
        public void ClearAllSlots()
        {
            activeAttackers.Clear();

            if (enableDebugLogs)
            {
                CombatLogger.LogAttack("[AttackCoordinator] Cleared all attack slots");
            }
        }

        /// <summary>
        /// Gets a copy of the active attackers list for debugging/UI purposes.
        /// </summary>
        public List<(IAIAgent attacker, float remainingTime)> GetActiveAttackers()
        {
            var result = new List<(IAIAgent, float)>();
            foreach (var slot in activeAttackers)
            {
                if (slot.attacker != null)
                {
                    float remaining = slot.endTime - Time.time;
                    result.Add((slot.attacker, remaining));
                }
            }
            return result;
        }

        /// <summary>
        /// Grants an attack slot to the specified attacker.
        /// </summary>
        /// <param name="attacker">The AI agent being granted attack permission</param>
        private void GrantAttackSlot(IAIAgent attacker)
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
                CombatLogger.LogAttack($"[AttackCoordinator] Granted attack slot to {attacker.name} (Active: {activeAttackers.Count}/{maxSimultaneousAttackers})");
            }
        }

        /// <summary>
        /// Removes expired attack slots from the active list.
        /// </summary>
        private void CleanupExpiredSlots()
        {
            int beforeCount = activeAttackers.Count;
            activeAttackers.RemoveAll(slot => Time.time >= slot.endTime || slot.attacker == null);

            if (enableDebugLogs && activeAttackers.Count < beforeCount)
            {
                CombatLogger.LogAttack($"[AttackCoordinator] Cleaned up {beforeCount - activeAttackers.Count} expired slots");
            }
        }

        /// <summary>
        /// Calculates the attack priority for an enemy based on its archetype.
        /// Higher priority enemies get preference when requesting attack permission.
        /// </summary>
        /// <param name="enemy">The AI agent to calculate priority for</param>
        /// <returns>Priority weight (higher = more aggressive)</returns>
        private float GetEnemyPriority(IAIAgent enemy)
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
    }
}
