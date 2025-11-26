using UnityEngine;

namespace FairyGate.Combat
{
    [CreateAssetMenu(fileName = "New Character Stats", menuName = "Combat/Character Stats")]
    public class CharacterStats : ScriptableObject
    {
        [Header("Combat Stats")]
        public int strength = 10;
        public int dexterity = 10;
        public int intelligence = 10;           // Reserved for future use
        public int focus = 10;
        public int will = 10;                   // Knockdown resistance
        public int physicalDefense = 10;
        public int magicalDefense = 10;         // Reserved for future use
        public int vitality = 10;

        [Header("Combat Modifiers")]
        [Range(0f, 100f)]
        public float criticalChance = 5f;       // Critical hit chance percentage (0-100%)

        [Header("Derived Values (Read-Only)")]
        [SerializeField] private int maxHealth = 150;
        [SerializeField] private int maxStamina = 130;
        [SerializeField] private float movementSpeed = 7f;
        [SerializeField] private float staminaEfficiency = 0.67f;

        public int MaxHealth => CombatConstants.BASE_HEALTH + (vitality * (int)CombatConstants.VITALITY_HEALTH_MULTIPLIER);
        public int MaxStamina => CombatConstants.BASE_STAMINA + (focus * (int)CombatConstants.FOCUS_STAMINA_MULTIPLIER);
        public float MovementSpeed => CombatConstants.BASE_MOVEMENT_SPEED + (dexterity * CombatConstants.DEXTERITY_MOVEMENT_MULTIPLIER);
        public float StaminaEfficiency => focus / CombatConstants.FOCUS_STAMINA_EFFICIENCY_DIVISOR;

        private void OnValidate()
        {
            // Update derived values in inspector for reference
            maxHealth = MaxHealth;
            maxStamina = MaxStamina;
            movementSpeed = MovementSpeed;
            staminaEfficiency = StaminaEfficiency;
        }

        public static CharacterStats CreateDefaultStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 10;
            stats.dexterity = 10;
            stats.intelligence = 10;
            stats.focus = 10;
            stats.will = 10;
            stats.physicalDefense = 10;
            stats.magicalDefense = 10;
            stats.vitality = 10;
            stats.criticalChance = 5f;
            return stats;
        }

        public static CharacterStats CreateTestPlayerStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 12;
            stats.dexterity = 14;
            stats.intelligence = 8;
            stats.focus = 10;
            stats.will = 12;               // Higher knockdown resistance for player
            stats.physicalDefense = 8;
            stats.magicalDefense = 8;
            stats.vitality = 12;
            stats.criticalChance = 8f;     // Slightly higher crit for player
            return stats;
        }

        public static CharacterStats CreateTestEnemyStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 10;
            stats.dexterity = 8;
            stats.intelligence = 10;
            stats.focus = 12;
            stats.will = 8;                // Lower knockdown resistance
            stats.physicalDefense = 12;
            stats.magicalDefense = 10;
            stats.vitality = 10;
            stats.criticalChance = 3f;
            return stats;
        }

        // Enemy Archetype Presets - Experimental Content
        public static CharacterStats CreateBerserkerStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 15;          // High damage
            stats.dexterity = 10;         // Medium speed
            stats.intelligence = 8;
            stats.focus = 8;              // Low stamina
            stats.will = 5;               // Very low knockdown resistance (glass cannon)
            stats.physicalDefense = 5;    // Glass cannon
            stats.magicalDefense = 5;
            stats.vitality = 6;           // Low HP
            stats.criticalChance = 10f;   // High crit chance (aggressive)
            return stats;
        }

        public static CharacterStats CreateGuardianStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 7;           // Low damage
            stats.dexterity = 6;          // Slow
            stats.intelligence = 10;
            stats.focus = 12;             // Good stamina for defense
            stats.will = 18;              // Very high knockdown resistance (tank)
            stats.physicalDefense = 15;   // High defense
            stats.magicalDefense = 12;
            stats.vitality = 15;          // High HP
            stats.criticalChance = 2f;    // Low crit (defensive build)
            return stats;
        }

        public static CharacterStats CreateAssassinStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 10;          // Medium damage
            stats.dexterity = 16;         // Very fast
            stats.intelligence = 10;
            stats.focus = 14;             // High stamina for mobility
            stats.will = 7;               // Low knockdown resistance (fragile)
            stats.physicalDefense = 7;    // Low defense
            stats.magicalDefense = 8;
            stats.vitality = 8;           // Low-medium HP
            stats.criticalChance = 15f;   // Very high crit (precision strikes)
            return stats;
        }

        public static CharacterStats CreateSoldierStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 10;          // Balanced
            stats.dexterity = 10;
            stats.intelligence = 10;
            stats.focus = 10;
            stats.will = 10;              // Balanced
            stats.physicalDefense = 10;
            stats.magicalDefense = 10;
            stats.vitality = 10;
            stats.criticalChance = 5f;    // Balanced
            return stats;
        }

        public static CharacterStats CreateArcherStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 8;           // Low melee damage
            stats.dexterity = 14;         // Fast for kiting
            stats.intelligence = 10;
            stats.focus = 15;             // High accuracy/stamina
            stats.will = 8;               // Low knockdown resistance (ranged unit)
            stats.physicalDefense = 6;    // Fragile
            stats.magicalDefense = 8;
            stats.vitality = 7;           // Low HP
            stats.criticalChance = 12f;   // High crit (precision)
            return stats;
        }
    }
}