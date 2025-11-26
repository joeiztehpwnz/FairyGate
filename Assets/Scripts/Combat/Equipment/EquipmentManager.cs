using System;
using UnityEngine;

namespace FairyGate.Combat
{
    public class EquipmentManager : MonoBehaviour
    {
        [Header("Current Equipment")]
        [SerializeField] private EquipmentData currentArmor;
        [SerializeField] private EquipmentData currentAccessory;
        // Note: Weapon handled by existing WeaponController

        [Header("Equipment Sets (Optional)")]
        [SerializeField] private EquipmentSet[] availableSets;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // C# Events (replaces UnityEvents for performance)
        public event Action<EquipmentData, EquipmentSlot> OnEquipmentChanged;
        public event Action OnEquipmentRefreshed;

        private CharacterStats baseStats;
        private CharacterStats modifiedStats;
        private CombatController combatController;

        public EquipmentData CurrentArmor => currentArmor;
        public EquipmentData CurrentAccessory => currentAccessory;
        public CharacterStats ModifiedStats => modifiedStats;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();

            // Get base stats from CombatController (use BaseStats to avoid circular dependency)
            baseStats = combatController.BaseStats;

            // Create modified stats as a copy of base stats
            modifiedStats = ScriptableObject.CreateInstance<CharacterStats>();

            if (baseStats != null)
            {
                CopyStats(baseStats, modifiedStats);
            }
            else
            {
                CombatLogger.LogCombat($"[EquipmentManager] {gameObject.name} - BaseStats is null! Cannot initialize equipment system.", CombatLogger.LogLevel.Error);
            }
        }

        private void Start()
        {
            // Apply initial equipment bonuses
            RefreshEquipmentBonuses();
        }

        public bool EquipItem(EquipmentData equipment)
        {
            if (equipment == null) return false;

            // Can only equip outside combat
            if (combatController.IsInCombat)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{gameObject.name} cannot change equipment during combat", CombatLogger.LogLevel.Warning);
                }
                return false;
            }

            // Equip based on slot
            switch (equipment.slot)
            {
                case EquipmentSlot.Armor:
                    currentArmor = equipment;
                    break;
                case EquipmentSlot.Accessory:
                    currentAccessory = equipment;
                    break;
                case EquipmentSlot.Weapon:
                    // Handled by WeaponController
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat("Use WeaponController to change weapons", CombatLogger.LogLevel.Warning);
                    }
                    return false;
            }

            RefreshEquipmentBonuses();
            OnEquipmentChanged?.Invoke(equipment, equipment.slot);

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{gameObject.name} equipped {equipment.equipmentName} in {equipment.slot} slot");
            }

            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (combatController.IsInCombat)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{gameObject.name} cannot change equipment during combat", CombatLogger.LogLevel.Warning);
                }
                return false;
            }

            switch (slot)
            {
                case EquipmentSlot.Armor:
                    currentArmor = null;
                    break;
                case EquipmentSlot.Accessory:
                    currentAccessory = null;
                    break;
            }

            RefreshEquipmentBonuses();

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{gameObject.name} unequipped {slot}");
            }

            return true;
        }

        public void RefreshEquipmentBonuses()
        {
            // Reset to base stats
            CopyStats(baseStats, modifiedStats);

            // Apply armor bonuses
            if (currentArmor != null)
            {
                ApplyEquipmentBonuses(currentArmor);
            }

            // Apply accessory bonuses
            if (currentAccessory != null)
            {
                ApplyEquipmentBonuses(currentAccessory);
            }

            OnEquipmentRefreshed?.Invoke();

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{gameObject.name} equipment bonuses refreshed - Str:{modifiedStats.strength} Dex:{modifiedStats.dexterity} Def:{modifiedStats.physicalDefense} Focus:{modifiedStats.focus}");
            }
        }

        private void ApplyEquipmentBonuses(EquipmentData equipment)
        {
            modifiedStats.strength += equipment.strengthBonus;
            modifiedStats.dexterity += equipment.dexterityBonus;
            modifiedStats.physicalDefense += equipment.physicalDefenseBonus;
            modifiedStats.focus += equipment.focusBonus;

            // Note: MaxHealth and MaxStamina bonuses are handled separately
            // in HealthSystem and StaminaSystem to avoid circular dependencies
        }

        private void CopyStats(CharacterStats source, CharacterStats destination)
        {
            if (source == null || destination == null)
            {
                CombatLogger.LogCombat($"[EquipmentManager] {gameObject.name} - Cannot copy stats: source or destination is null!", CombatLogger.LogLevel.Error);
                return;
            }

            destination.strength = source.strength;
            destination.dexterity = source.dexterity;
            destination.physicalDefense = source.physicalDefense;
            destination.focus = source.focus;
            destination.intelligence = source.intelligence;
            destination.magicalDefense = source.magicalDefense;
            destination.vitality = source.vitality;
        }

        // Quick equipment swapping for testing
        public void EquipSet(EquipmentSet set)
        {
            if (set == null) return;

            if (set.armor != null) EquipItem(set.armor);
            if (set.accessory != null) EquipItem(set.accessory);
            // Note: Weapon is handled by WeaponController

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{gameObject.name} equipped set: {set.setName}");
            }
        }
    }
}
