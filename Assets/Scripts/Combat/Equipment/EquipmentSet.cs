using UnityEngine;

namespace FairyGate.Combat
{
    [CreateAssetMenu(fileName = "New Equipment Set", menuName = "Combat/Equipment Set")]
    public class EquipmentSet : ScriptableObject
    {
        public string setName;
        public EquipmentData armor;
        public WeaponData weapon;
        public EquipmentData accessory;

        [TextArea(2, 3)]
        public string description;
    }
}
