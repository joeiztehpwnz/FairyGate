using UnityEngine;

namespace FairyGate.Combat
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Stats")]
        public string weaponName;
        public WeaponType weaponType;
        public float range;
        public int baseDamage;
        public float speed;
        public float stunDuration;

        [Header("Speed Modifiers")]
        [Range(-0.5f, 0.5f)]
        public float executionSpeedModifier;     // -20% to +30%
        [Range(-0.5f, 0.5f)]
        public float speedResolutionModifier;    // -0.30 to +0.20

        [Header("Visual References")]
        public GameObject weaponPrefab;
        public Sprite weaponIcon;
        public AudioClip[] hitSounds;

        [Header("Debug Info")]
        [TextArea(2, 4)]
        public string description;

        public static WeaponData CreateSwordData()
        {
            var sword = CreateInstance<WeaponData>();
            sword.weaponName = "Sword";
            sword.weaponType = WeaponType.Sword;
            sword.range = 1.5f;
            sword.baseDamage = 10;
            sword.speed = 1.0f;
            sword.stunDuration = 1.0f;
            sword.executionSpeedModifier = 0f;
            sword.speedResolutionModifier = 0f;
            sword.description = "Balanced baseline for all stats";
            return sword;
        }

        public static WeaponData CreateSpearData()
        {
            var spear = CreateInstance<WeaponData>();
            spear.weaponName = "Spear";
            spear.weaponType = WeaponType.Spear;
            spear.range = 2.5f;
            spear.baseDamage = 8;
            spear.speed = 0.8f;
            spear.stunDuration = 0.8f;
            spear.executionSpeedModifier = 0.1f; // +10%
            spear.speedResolutionModifier = -0.1f; // -10%
            spear.description = "Range advantage with moderate damage/speed trade-offs";
            return spear;
        }

        public static WeaponData CreateDaggerData()
        {
            var dagger = CreateInstance<WeaponData>();
            dagger.weaponName = "Dagger";
            dagger.weaponType = WeaponType.Dagger;
            dagger.range = 1.0f;
            dagger.baseDamage = 6;
            dagger.speed = 1.5f;
            dagger.stunDuration = 0.5f;
            dagger.executionSpeedModifier = -0.2f; // -20%
            dagger.speedResolutionModifier = 0.2f; // +20%
            dagger.description = "Speed advantage with range/damage trade-offs";
            return dagger;
        }

        public static WeaponData CreateMaceData()
        {
            var mace = CreateInstance<WeaponData>();
            mace.weaponName = "Mace";
            mace.weaponType = WeaponType.Mace;
            mace.range = 1.2f;
            mace.baseDamage = 15;
            mace.speed = 0.6f;
            mace.stunDuration = 1.5f;
            mace.executionSpeedModifier = 0.3f; // +30%
            mace.speedResolutionModifier = -0.3f; // -30%
            mace.description = "Damage/stun advantage with speed/range trade-offs";
            return mace;
        }
    }
}