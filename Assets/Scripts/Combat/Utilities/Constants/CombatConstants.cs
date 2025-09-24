using UnityEngine;

namespace FairyGate.Combat
{
    public static class CombatConstants
    {
        // Base Values
        public const int BASE_HEALTH = 100;
        public const int BASE_STAMINA = 100;
        public const float BASE_MOVEMENT_SPEED = 5.0f;
        public const float BASE_SKILL_CHARGE_TIME = 2.0f;

        // Knockdown System
        public const int ATTACK_KNOCKDOWN_BUILDUP = 15;
        public const float KNOCKDOWN_METER_DECAY_RATE = -5f; // per second
        public const float KNOCKDOWN_METER_THRESHOLD = 100f;
        public const float KNOCKDOWN_DURATION = 2.0f;

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

        // Stamina Costs
        public const int ATTACK_STAMINA_COST = 2;
        public const int SMASH_STAMINA_COST = 4;
        public const int WINDMILL_STAMINA_COST = 3;
        public const int DEFENSE_STAMINA_COST = 3;
        public const int COUNTER_STAMINA_COST = 5;

        // Stamina Drain Rates (per second)
        public const float DEFENSE_STAMINA_DRAIN = 3f;
        public const float COUNTER_STAMINA_DRAIN = 5f;

        // Rest System
        public const float REST_STAMINA_REGENERATION_RATE = 25f;

        // Auto-Cancel Grace Period
        public const float AUTO_CANCEL_GRACE_PERIOD = 0.1f;

        // Movement Speed Modifiers
        public const float DEFENSE_MOVEMENT_SPEED_MODIFIER = 0.7f; // 30% reduction
        public const float COUNTER_MOVEMENT_SPEED_MODIFIER = 0.7f; // 30% reduction
        public const float WINDMILL_MOVEMENT_SPEED_MODIFIER = 0.7f; // 30% reduction
    }
}