using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Defines condition types and evaluation logic for AI pattern system.
    /// Conditions determine when pattern nodes can execute and when transitions occur.
    /// </summary>
    [System.Serializable]
    public class PatternCondition
    {
        [Header("Condition Type")]
        public ConditionType type;

        [Header("Comparison Values")]
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public SkillType skillValue;
        public CombatState combatStateValue;

        /// <summary>
        /// Evaluates this condition against the current game state.
        /// </summary>
        public bool Evaluate(PatternEvaluationContext context)
        {
            switch (type)
            {
                // Health-based conditions
                case ConditionType.HealthAbove:
                    return context.selfHealthPercentage > floatValue;

                case ConditionType.HealthBelow:
                    return context.selfHealthPercentage < floatValue;

                // Hit tracking
                case ConditionType.HitsTakenCount:
                    return context.hitsTaken >= intValue;

                case ConditionType.HitsDealtCount:
                    return context.hitsDealt >= intValue;

                // Player state
                case ConditionType.PlayerCharging:
                    return context.isPlayerCharging == boolValue;

                case ConditionType.PlayerSkillType:
                    return context.playerSkill == skillValue;

                case ConditionType.PlayerCombatState:
                    return context.playerCombatState == combatStateValue;

                case ConditionType.PlayerInRange:
                    return context.distanceToPlayer <= floatValue;

                case ConditionType.WeaponInRange:
                    // Check if player is within weapon's effective range
                    if (context.weaponController != null && context.playerTransform != null)
                    {
                        // For now, check melee range (can be extended to check skill-specific range)
                        float weaponRange = context.weaponController.GetMeleeRange();
                        return context.distanceToPlayer <= weaponRange;
                    }
                    return false;

                // Self state
                case ConditionType.StaminaAbove:
                    return context.selfStamina > floatValue;

                case ConditionType.StaminaBelow:
                    return context.selfStamina < floatValue;

                case ConditionType.SkillReady:
                    return context.skillSystem.CanChargeSkill(skillValue);

                case ConditionType.SelfCombatState:
                    return context.selfCombatState == combatStateValue;

                // Timing
                case ConditionType.TimeElapsed:
                    return context.timeInCurrentNode >= floatValue;

                case ConditionType.CooldownExpired:
                    // intValue represents cooldown ID
                    return context.IsCooldownExpired(intValue);

                // Random
                case ConditionType.RandomChance:
                    // Use stored random value (rolled once per node entry)
                    // This prevents random flipping every frame
                    return context.randomValue < floatValue;

                default:
                    Debug.LogWarning($"[PatternCondition] Unknown condition type: {type}");
                    return false;
            }
        }
    }

    /// <summary>
    /// Types of conditions that can be evaluated for pattern logic.
    /// </summary>
    public enum ConditionType
    {
        // Health-based
        HealthAbove,        // HP > X%
        HealthBelow,        // HP < X%

        // Hit tracking
        HitsTakenCount,     // Taken N hits since last reset
        HitsDealtCount,     // Dealt N hits since last reset

        // Player state
        PlayerCharging,     // Player is charging a skill
        PlayerSkillType,    // Player charging specific skill
        PlayerCombatState,  // Player in specific state (knockdown, stun, etc)
        PlayerInRange,      // Player within distance (uses floatValue for custom range)
        WeaponInRange,      // Player within weapon's effective range (melee/ranged based on weapon)

        // Self state
        StaminaAbove,       // Stamina > X%
        StaminaBelow,       // Stamina < X%
        SkillReady,         // Specific skill is charged/ready

        // Timing
        TimeElapsed,        // X seconds since pattern start/node entry
        CooldownExpired,    // Specific cooldown timer expired

        // Random
        RandomChance,       // X% probability

        // Self combat state (added at end to avoid breaking existing patterns)
        SelfCombatState     // Self in specific state (knockback, knockdown, stun, etc)
    }

    /// <summary>
    /// Context data passed to condition evaluation.
    /// Contains all relevant game state information for decision-making.
    /// </summary>
    public class PatternEvaluationContext
    {
        // Self state
        public float selfHealthPercentage;
        public float selfStamina;
        public int hitsTaken;
        public int hitsDealt;
        public float timeInCurrentNode;
        public float randomValue; // Random value rolled once per node entry (0-1)
        public CombatState selfCombatState;

        // Player state
        public bool isPlayerCharging;
        public SkillType playerSkill;
        public CombatState playerCombatState;
        public float distanceToPlayer;

        // References
        public SkillSystem skillSystem;
        public WeaponController weaponController;
        public Transform playerTransform;

        // Cooldown tracking (simple dictionary-based system)
        private System.Collections.Generic.Dictionary<int, float> cooldowns =
            new System.Collections.Generic.Dictionary<int, float>();

        public void StartCooldown(int cooldownID, float duration)
        {
            cooldowns[cooldownID] = Time.time + duration;
        }

        public bool IsCooldownExpired(int cooldownID)
        {
            if (!cooldowns.ContainsKey(cooldownID))
                return true; // Never started = expired

            return Time.time >= cooldowns[cooldownID];
        }

        public void ResetCooldown(int cooldownID)
        {
            cooldowns.Remove(cooldownID);
        }
    }
}
