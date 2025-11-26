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
        public const float COUNTER_KNOCKBACK_DISTANCE = 3.0f;
        public const float SMASH_KNOCKBACK_DISTANCE = 3.0f;
        public const float WINDMILL_KNOCKBACK_DISTANCE = 3.0f;
        public const float KNOCKBACK_DISPLACEMENT_DISTANCE = 1.5f;
        public const float METER_KNOCKBACK_DISTANCE = 3.0f;

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

        // Stamina Costs (Punishing - Doubled from Original Values)
        public const int ATTACK_STAMINA_COST = 4;       // was 2
        public const int SMASH_STAMINA_COST = 10;       // was 5
        public const int WINDMILL_STAMINA_COST = 8;     // was 4
        public const int DEFENSE_STAMINA_COST = 4;      // was 2
        public const int COUNTER_STAMINA_COST = 6;      // was 3
        public const int LUNGE_STAMINA_COST = 8;        // was 4

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

        // Stamina Drain Rates (Per Second - Punishing Values)
        public const float DEFENSE_STAMINA_DRAIN = 5.0f;   // was 1.0f (5x higher)
        public const float COUNTER_STAMINA_DRAIN = 5.0f;   // was 1.0f (5x higher)

        // Charging Stamina Drain Rates (Per Second)
        public const float ATTACK_CHARGING_DRAIN = 2.0f;
        public const float SMASH_CHARGING_DRAIN = 4.0f;
        public const float COUNTER_CHARGING_DRAIN = 2.0f;
        public const float WINDMILL_CHARGING_DRAIN = 3.0f;
        public const float RANGED_AIMING_DRAIN = 1.5f;

        // Stamina Regeneration System
        public const float PASSIVE_STAMINA_REGEN = 0.4f;           // Per second while standing (24/min)
        public const float REST_STAMINA_REGENERATION_RATE = 25f;   // Per second during active rest
        public const float COMBAT_STAMINA_REGEN_MULTIPLIER = 0.0f; // No regen while in combat (0 = disabled)

        // Auto-Cancel Grace Period
        public const float AUTO_CANCEL_GRACE_PERIOD = 0.1f;

        // Classic Mabinogi: Movement Speed Modifiers During Charging
        public const float OFFENSIVE_CHARGE_MOVE_SPEED = 0.5f;      // 50% speed while charging offensive skills (Smash, Lunge)
        public const float DEFENSIVE_CHARGE_MOVE_SPEED = 0.3f;      // 30% movement speed for Defense/Counter during charged states
        public const float WINDMILL_CHARGE_MOVE_SPEED = 1.0f;       // No penalty (Windmill keeps mobile)
        public const float DEFENSE_MOVEMENT_SPEED_MODIFIER = 0.0f;  // Rooted in place (changed from 0.7f)
        public const float COUNTER_MOVEMENT_SPEED_MODIFIER = 0.0f;  // Rooted in place (changed from 0.7f)
        public const float WINDMILL_MOVEMENT_SPEED_MODIFIER = 1.0f; // No penalty (changed from 0.7f)

        // RangedAttack Skill Constants
        public const int RANGED_ATTACK_STAMINA_COST = 6;  // was 3
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

        // N+1 Combo System
        public const float CRITICAL_STUN_MULTIPLIER = 1.3f;   // Critical hits extend stun by 30%
        public const float N_PLUS_ONE_WINDOW_START = 0.7f;    // N+1 window opens at 70% of stun duration
        public const float N_PLUS_ONE_WINDOW_END = 0.95f;     // N+1 window closes at 95% of stun duration

        // N+2 System (Advanced - for fast weapons)
        public const float N_PLUS_TWO_FIRST_WINDOW = 0.5f;    // First window at 50% of stun
        public const float N_PLUS_TWO_SECOND_WINDOW = 0.9f;   // Second window at 90% of stun
        public const float N_PLUS_TWO_TOLERANCE = 0.02f;      // ±2% timing tolerance for N+2

        // AI Pattern Behavior
        public const int DEFENSIVE_INTERRUPT_PRIORITY_THRESHOLD = 15; // Priority >= 15 can interrupt skill execution
        public const float AI_PLAYER_SEARCH_COOLDOWN = 1.0f;          // Seconds between player searches

        // Combat Interaction Timing
        public const float DEFENSIVE_SKILL_TIMEOUT_SECONDS = 5.0f;    // Max time defensive skills wait for targets

        // Telegraph System Durations
        public const float TELEGRAPH_STANCE_DURATION = 0.5f;
        public const float TELEGRAPH_WEAPON_RAISE_DURATION = 0.8f;
        public const float TELEGRAPH_SHIELD_RAISE_DURATION = 0.6f;
        public const float TELEGRAPH_GROUND_INDICATOR_DURATION = 1.0f;
        public const float TELEGRAPH_CROUCH_DURATION = 0.4f;
        public const float TELEGRAPH_BACKSTEP_DURATION = 0.3f;
    }
}
