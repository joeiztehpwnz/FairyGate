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
        public int physicalDefense = 10;
        public int magicalDefense = 10;         // Reserved for future use
        public int vitality = 10;

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
            stats.physicalDefense = 10;
            stats.magicalDefense = 10;
            stats.vitality = 10;
            return stats;
        }

        public static CharacterStats CreateTestPlayerStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 12;
            stats.dexterity = 14;
            stats.intelligence = 8;
            stats.focus = 10;
            stats.physicalDefense = 8;
            stats.magicalDefense = 8;
            stats.vitality = 12;
            return stats;
        }

        public static CharacterStats CreateTestEnemyStats()
        {
            var stats = CreateInstance<CharacterStats>();
            stats.strength = 10;
            stats.dexterity = 8;
            stats.intelligence = 10;
            stats.focus = 12;
            stats.physicalDefense = 12;
            stats.magicalDefense = 10;
            stats.vitality = 10;
            return stats;
        }
    }
}