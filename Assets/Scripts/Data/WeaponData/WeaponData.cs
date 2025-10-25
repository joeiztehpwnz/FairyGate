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

        [Header("Ranged Attack Properties (Optional)")]
        public bool isRangedWeapon = false;
        [Tooltip("Projectile type name for debug display")]
        public string projectileType = "Arrow";
        [Tooltip("Trail start color")]
        public Color trailColorStart = Color.yellow;
        [Tooltip("Trail end color (on hit)")]
        public Color trailColorEnd = Color.red;
        [Tooltip("Trail width")]
        public float trailWidth = 0.08f;
        [Tooltip("Sound effect when firing")]
        public AudioClip fireSound;

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

        // RANGED WEAPONS

        public static WeaponData CreateBowData()
        {
            var bow = CreateInstance<WeaponData>();
            bow.weaponName = "Bow";
            bow.weaponType = WeaponType.Bow;
            bow.range = 6.0f;
            bow.baseDamage = 10;
            bow.speed = 1.0f;
            bow.stunDuration = 0.3f;
            bow.executionSpeedModifier = 0f;
            bow.speedResolutionModifier = 0f;
            bow.isRangedWeapon = true;
            bow.projectileType = "Arrow";
            bow.trailColorStart = Color.yellow;
            bow.trailColorEnd = Color.red;
            bow.trailWidth = 0.08f;
            bow.description = "Standard ranged weapon with good range and accuracy";
            return bow;
        }

        public static WeaponData CreateJavelinData()
        {
            var javelin = CreateInstance<WeaponData>();
            javelin.weaponName = "Javelin";
            javelin.weaponType = WeaponType.Javelin;
            javelin.range = 4.5f;
            javelin.baseDamage = 14;
            javelin.speed = 0.8f; // Slower weapon = longer recovery
            javelin.stunDuration = 1.2f;
            javelin.executionSpeedModifier = 0.1f;
            javelin.speedResolutionModifier = -0.15f;
            javelin.isRangedWeapon = true;
            javelin.projectileType = "Javelin";
            javelin.trailColorStart = new Color(0.6f, 0.6f, 0.6f); // Gray
            javelin.trailColorEnd = Color.white;
            javelin.trailWidth = 0.12f; // Thicker trail
            javelin.description = "Heavy thrown weapon - high damage, shorter range, slower";
            return javelin;
        }

        public static WeaponData CreateThrowingKnifeData()
        {
            var knife = CreateInstance<WeaponData>();
            knife.weaponName = "Throwing Knife";
            knife.weaponType = WeaponType.ThrowingKnife;
            knife.range = 3.5f;
            knife.baseDamage = 7;
            knife.speed = 1.3f; // Fast weapon = quick recovery
            knife.stunDuration = 0.2f;
            knife.executionSpeedModifier = -0.15f;
            knife.speedResolutionModifier = 0.15f;
            knife.isRangedWeapon = true;
            knife.projectileType = "Knife";
            knife.trailColorStart = Color.cyan;
            knife.trailColorEnd = Color.blue;
            knife.trailWidth = 0.05f; // Thin trail
            knife.description = "Quick ranged attack - low damage, fast speed, short range";
            return knife;
        }

        public static WeaponData CreateSlingData()
        {
            var sling = CreateInstance<WeaponData>();
            sling.weaponName = "Sling";
            sling.weaponType = WeaponType.Sling;
            sling.range = 5.0f;
            sling.baseDamage = 6;
            sling.speed = 1.1f;
            sling.stunDuration = 0.8f; // Blunt impact
            sling.executionSpeedModifier = 0f;
            sling.speedResolutionModifier = 0f;
            sling.isRangedWeapon = true;
            sling.projectileType = "Stone";
            sling.trailColorStart = new Color(0.5f, 0.4f, 0.3f); // Brown/tan
            sling.trailColorEnd = Color.gray;
            sling.trailWidth = 0.06f;
            sling.description = "Simple ranged weapon using stones as projectiles";
            return sling;
        }

        public static WeaponData CreateThrowingAxeData()
        {
            var axe = CreateInstance<WeaponData>();
            axe.weaponName = "Throwing Axe";
            axe.weaponType = WeaponType.ThrowingAxe;
            axe.range = 3.0f; // Short range when thrown
            axe.baseDamage = 12;
            axe.speed = 0.9f;
            axe.stunDuration = 1.0f;
            axe.executionSpeedModifier = 0.05f;
            axe.speedResolutionModifier = -0.1f;
            axe.isRangedWeapon = true;
            axe.projectileType = "Axe";
            axe.trailColorStart = Color.red;
            axe.trailColorEnd = new Color(0.5f, 0f, 0f); // Dark red
            axe.trailWidth = 0.10f;
            axe.description = "Versatile weapon - can use melee attacks AND ranged throw";
            return axe;
        }
    }
}