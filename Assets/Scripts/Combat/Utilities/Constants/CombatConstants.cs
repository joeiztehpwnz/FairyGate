using UnityEngine;

namespace FairyGate.Combat
{
    public static class CombatConstants
    {
        // Base Values
        public const int BASE_HEALTH = 100;
        public const int BASE_STAMINA = 100;
        public const float BASE_MOVEMENT_SPEED = 1.5f;

        // Weapon Range Constants (Classic Mabinogi Authenticity)
        // In Classic Mabinogi, all melee weapons attacked from the same distance
        // Weapon differentiation was through damage, speed, stun duration, and combo length - NOT range
        public const float STANDARD_MELEE_RANGE = 1.5f;  // All melee weapons use this value directly

        // Classic Mabinogi: Variable Skill Charge Times (Pre-2012 System)
        public const float ATTACK_CHARGE_TIME = 0.0f;      // Instant execution
        public const float WINDMILL_CHARGE_TIME = 0.8f;    // Fast AoE (dominated pre-Genesis)
        public const float DEFENSE_CHARGE_TIME = 1.0f;     // Quick defensive option
        public const float COUNTER_CHARGE_TIME = 1.0f;     // Quick defensive option
        public const float SMASH_CHARGE_TIME = 2.0f;       // Matches classic exactly
        public const float RANGED_ATTACK_CHARGE_TIME = 0.0f; // Uses aiming system instead
        // Note: LUNGE_CHARGE_TIME = 1.5f is defined below in Lunge Skill Constants section

        // Knockdown System
        public const int ATTACK_KNOCKDOWN_BUILDUP = 15;
        public const float KNOCKDOWN_METER_DECAY_RATE = -5f; // per second
        public const float KNOCKBACK_METER_THRESHOLD = 50f;
        public const float KNOCKDOWN_METER_THRESHOLD = 100f;
        public const float KNOCKBACK_DURATION = 0.8f;
        public const float KNOCKDOWN_DURATION = 2.0f;

        // Knockback Distances
        public const float COUNTER_KNOCKBACK_DISTANCE = 1.5f;
        public const float SMASH_KNOCKBACK_DISTANCE = 2.0f;
        public const float WINDMILL_KNOCKBACK_DISTANCE = 1.8f;
        public const float KNOCKBACK_DISPLACEMENT_DISTANCE = 0.8f;
        public const float METER_KNOCKBACK_DISTANCE = 1.2f;

        // Windmill AoE
        public const float WINDMILL_RADIUS = 2.5f;  // AoE radius for Windmill skill

        // Damage Reduction
        public const float SMASH_VS_DEFENSE_DAMAGE_REDUCTION = 0.75f;
        public const float MAX_DAMAGE_REDUCTION = 0.90f;
        public const int MINIMUM_DAMAGE = 1;

        // Status Effect Calculations
        public const float FOCUS_STUN_RESISTANCE_DIVISOR = 30f;
        public const float FOCUS_STATUS_RECOVERY_DIVISOR = 30f;
        public const float FOCUS_STAMINA_EFFICIENCY_DIVISOR = 15f;

        // Speed Resolution
        public const float DEXTERITY_SPEED_DIVISOR = 5f;

        // Stat Scaling
        public const float VITALITY_HEALTH_MULTIPLIER = 5f;
        public const float FOCUS_STAMINA_MULTIPLIER = 3f;
        public const float DEXTERITY_MOVEMENT_MULTIPLIER = 0.2f;
        public const float STRENGTH_KNOCKDOWN_DIVISOR = 10f;
        public const float PHYSICAL_DEFENSE_REDUCTION_DIVISOR = 20f;

        // Classic Mabinogi: Stamina Costs (Adjusted to Pre-2012 Values)
        public const int ATTACK_STAMINA_COST = 2;
        public const int SMASH_STAMINA_COST = 5;        // Increased from 4
        public const int WINDMILL_STAMINA_COST = 4;     // Increased from 3
        public const int DEFENSE_STAMINA_COST = 2;      // Reduced from 3 (initial cost)
        public const int COUNTER_STAMINA_COST = 3;      // Reduced from 5 (initial cost)
        public const int LUNGE_STAMINA_COST = 4;

        // Defense Block System
        public const float DEFENSE_BLOCK_RECOVERY_TIME = 0.5f;

        // Lunge Skill Constants
        public const float LUNGE_CHARGE_TIME = 1.5f;
        public const float LUNGE_DASH_DISTANCE = 2.0f;
        public const float LUNGE_MIN_RANGE = 2.0f;      // Can't lunge if closer
        public const float LUNGE_MAX_RANGE = 4.0f;      // Can't lunge if farther
        public const float LUNGE_STARTUP_TIME = 0.1f;
        public const float LUNGE_ACTIVE_TIME = 0.15f;   // Fast dash (vulnerable but quick)
        public const float LUNGE_RECOVERY_TIME = 0.2f;

        // Classic Mabinogi: Stamina Drain Rates (Per Second, Reduced from Modern Values)
        public const float DEFENSE_STAMINA_DRAIN = 1.0f;   // Reduced from 3.0f
        public const float COUNTER_STAMINA_DRAIN = 1.0f;   // Reduced from 5.0f

        // Classic Mabinogi: Stamina Regeneration System
        public const float PASSIVE_STAMINA_REGEN = 0.4f;           // Per second while standing (24/min)
        public const float REST_STAMINA_REGENERATION_RATE = 25f;   // Per second during active rest

        // Auto-Cancel Grace Period
        public const float AUTO_CANCEL_GRACE_PERIOD = 0.1f;

        // Classic Mabinogi: Movement Speed Modifiers During Charging
        public const float OFFENSIVE_CHARGE_MOVE_SPEED = 0.5f;      // 50% speed while charging offensive skills (Smash, Lunge)
        public const float DEFENSIVE_CHARGE_MOVE_SPEED = 0.0f;      // Rooted in place for Defense/Counter
        public const float WINDMILL_CHARGE_MOVE_SPEED = 1.0f;       // No penalty (Windmill keeps mobile)
        public const float DEFENSE_MOVEMENT_SPEED_MODIFIER = 0.0f;  // Rooted in place (changed from 0.7f)
        public const float COUNTER_MOVEMENT_SPEED_MODIFIER = 0.0f;  // Rooted in place (changed from 0.7f)
        public const float WINDMILL_MOVEMENT_SPEED_MODIFIER = 1.0f; // No penalty (changed from 0.7f)

        // RangedAttack Skill Constants
        public const int RANGED_ATTACK_STAMINA_COST = 3;
        public const float RANGED_ATTACK_BASE_RANGE = 6.0f;    // Default if weapon doesn't override
        public const int RANGED_ATTACK_BASE_DAMAGE = 10;       // Default if weapon doesn't override

        // Accuracy System Constants
        public const float ACCURACY_BUILD_STATIONARY = 40f;     // % per second
        public const float ACCURACY_BUILD_MOVING = 20f;         // % per second
        public const float ACCURACY_DECAY_WHILE_MOVING = 10f;   // % per second
        public const float FOCUS_ACCURACY_DIVISOR = 20f;
        public const float MAX_MISS_ANGLE = 45f;                // degrees

        // RangedAttack Movement & Timing
        public const float RANGED_ATTACK_AIMING_MOVEMENT_MODIFIER = 0.5f; // 50% speed
        public const float RANGED_ATTACK_RECOVERY_TIME = 0.3f;
        public const float RANGED_ATTACK_TRAIL_DURATION = 0.5f;

        // Skill Execution & Processing
        public const float SIMULTANEOUS_EXECUTION_WINDOW = 0.1f; // 100ms window for simultaneous skill execution
        public const int SKILL_EXECUTION_POOL_INITIAL_CAPACITY = 16;
        public const int COMBAT_UPDATABLES_INITIAL_CAPACITY = 32;

        // AI Movement Constants
        public const float AI_OPTIMAL_RANGE_BUFFER_NEAR = 0.5f;
        public const float AI_OPTIMAL_RANGE_BUFFER_FAR = 1.0f;

        // AI Formation System
        public const int FORMATION_SLOT_COUNT = 8;            // Number of circular positions around player
        public const float FORMATION_SLOT_OFFSET = 0.3f;       // Random offset per enemy for variation
        public const float FORMATION_SLOT_REASSIGN_COOLDOWN = 2.0f; // Prevent slot thrashing
    }
}
