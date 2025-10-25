using UnityEngine;

namespace FairyGate.Combat
{
    [CreateAssetMenu(fileName = "New Equipment", menuName = "Combat/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        [Header("Equipment Info")]
        public string equipmentName;
        public EquipmentSlot slot;
        public EquipmentTier tier;

        [Header("Stat Modifiers")]
        public int strengthBonus;
        public int dexterityBonus;
        public int physicalDefenseBonus;
        public int focusBonus;
        public int maxHealthBonus;
        public int maxStaminaBonus;
        public float movementSpeedBonus;

        [Header("Visual")]
        public Sprite icon;
        public GameObject equipmentPrefab; // Optional visual representation

        [Header("Description")]
        [TextArea(3, 5)]
        public string description;

        // Future: Special properties
        // public List<EquipmentProperty> specialProperties;
    }
}
