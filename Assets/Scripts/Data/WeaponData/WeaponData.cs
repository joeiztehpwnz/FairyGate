using UnityEngine;

namespace FairyGate.Combat
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Stats")]
        public string weaponName;
        public WeaponType weaponType;

        [Header("Range Values")]
        [Tooltip("Range for melee skills (Attack, Smash, Windmill, Counter, Defense)")]
        public float meleeRange;
        [Tooltip("Range for ranged skills (RangedAttack) - 0 if weapon cannot use RangedAttack")]
        public float rangedRange;

        [Header("Damage Multipliers")]
        [Tooltip("Damage multiplier for melee skills (Attack, Smash, Windmill, Counter) - 1.0 = full damage")]
        [Range(0.1f, 2.0f)]
        public float meleeDamageMultiplier = 1.0f;
        [Tooltip("Damage multiplier for ranged skills (RangedAttack) - 1.0 = full damage")]
        [Range(0.1f, 2.0f)]
        public float rangedDamageMultiplier = 1.0f;

        [Header("Other Stats")]
        public int baseDamage;
        public float speed;
        public float stunDuration;

        [Header("N+1 Combo System")]
        [Tooltip("Number of basic attacks in this weapon's combo chain (N+1 window appears after last hit)")]
        [Range(1, 5)]
        public int comboLength = 3;
        [Tooltip("Weapon-specific knockdown rate modifier (higher = more knockdown buildup)")]
        [Range(0.1f, 2.0f)]
        public float knockdownRate = 1.0f;
        [Tooltip("Fast weapon flag for N+2 combo system (future feature)")]
        public bool isFastWeapon = false;

        [Header("Deprecated - Use meleeRange/rangedRange instead")]
        [Tooltip("Legacy range value - will be removed in future update")]
        public float range;

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

        [Header("Melee Trail Properties")]
        [Tooltip("Trail color for melee attacks (Attack, Smash, Windmill, Lunge)")]
        public Color meleeTrailColor = Color.white;
        [Tooltip("Trail width for melee attacks")]
        [Range(0.05f, 0.5f)]
        public float meleeTrailWidth = 0.15f;
        [Tooltip("Trail lifetime in seconds")]
        [Range(0.1f, 1.0f)]
        public float meleeTrailTime = 0.3f;

        [Header("Melee Trail Advanced")]
        [Tooltip("Start width multiplier (1.0 = full width at start)")]
        [Range(0.1f, 2.0f)]
        public float meleeTrailStartWidthMultiplier = 1.0f;
        [Tooltip("End width multiplier (1.0 = full width at end, 0.0 = tapers to point)")]
        [Range(0.0f, 2.0f)]
        public float meleeTrailEndWidthMultiplier = 1.0f;
        [Tooltip("Trail end color (creates gradient if different from start)")]
        public Color meleeTrailEndColor = Color.white;
        [Tooltip("Number of corner vertices (higher = smoother corners)")]
        [Range(0, 10)]
        public int meleeTrailCornerVertices = 3;
        [Tooltip("Number of end cap vertices (higher = rounder line ends)")]
        [Range(0, 10)]
        public int meleeTrailEndCapVertices = 3;

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
            sword.meleeRange = 1.5f;     // Standard sword melee
            sword.rangedRange = 1.0f;    // Can throw sword in desperation (short range)
            sword.meleeDamageMultiplier = 1.0f;   // Full melee damage (10)
            sword.rangedDamageMultiplier = 0.5f;  // Weak throw (5)
            sword.range = 1.5f;          // Legacy
            sword.baseDamage = 10;
            sword.speed = 1.0f;
            sword.stunDuration = 1.0f;
            sword.comboLength = 3;            // Standard 3-hit sword combo
            sword.knockdownRate = 1.0f;       // Normal weapon - baseline
            sword.isFastWeapon = false;
            sword.executionSpeedModifier = 0f;
            sword.speedResolutionModifier = 0f;
            sword.meleeTrailColor = Color.white;
            sword.meleeTrailWidth = 0.35f;
            sword.meleeTrailTime = 0.3f;
            sword.meleeTrailStartWidthMultiplier = 1.0f;
            sword.meleeTrailEndWidthMultiplier = 1.0f; // Uniform width
            sword.meleeTrailEndColor = Color.white;
            sword.meleeTrailCornerVertices = 3;
            sword.meleeTrailEndCapVertices = 3;
            sword.description = "Balanced baseline for all stats - can throw in desperation";
            return sword;
        }

        public static WeaponData CreateSpearData()
        {
            var spear = CreateInstance<WeaponData>();
            spear.weaponName = "Spear";
            spear.weaponType = WeaponType.Spear;
            spear.meleeRange = 1.5f;     // Uniform melee range (classic Mabinogi)
            spear.rangedRange = 1.5f;    // Can throw javelin-style (awkward)
            spear.meleeDamageMultiplier = 1.0f;   // Full melee damage (8)
            spear.rangedDamageMultiplier = 0.6f;  // Okay javelin throw (~5)
            spear.range = 1.5f;          // Legacy
            spear.baseDamage = 8;
            spear.speed = 0.8f;
            spear.stunDuration = 0.8f;        // Medium stun
            spear.comboLength = 3;            // Standard 3-hit spear combo
            spear.knockdownRate = 1.1f;       // Slightly higher knockdown
            spear.isFastWeapon = false;
            spear.executionSpeedModifier = 0.1f; // +10%
            spear.speedResolutionModifier = -0.1f; // -10%
            spear.meleeTrailColor = new Color(0.7f, 0.9f, 1.0f); // Light blue
            spear.meleeTrailWidth = 0.25f;
            spear.meleeTrailTime = 0.25f;
            spear.meleeTrailStartWidthMultiplier = 1.2f;
            spear.meleeTrailEndWidthMultiplier = 0.5f; // Tapers for thrust effect
            spear.meleeTrailEndColor = new Color(0.5f, 0.7f, 0.9f); // Darker blue at tip
            spear.meleeTrailCornerVertices = 2;
            spear.meleeTrailEndCapVertices = 5; // Sharp point
            spear.description = "Balanced stats with moderate damage/speed - can throw (classic: no range advantage)";
            return spear;
        }

        public static WeaponData CreateDaggerData()
        {
            var dagger = CreateInstance<WeaponData>();
            dagger.weaponName = "Dagger";
            dagger.weaponType = WeaponType.Dagger;
            dagger.meleeRange = 1.5f;    // Uniform melee range (classic Mabinogi)
            dagger.rangedRange = 2.5f;   // Can throw dagger effectively
            dagger.meleeDamageMultiplier = 1.0f;   // Full melee damage (6)
            dagger.rangedDamageMultiplier = 0.8f;  // Decent throwing knife (~5)
            dagger.range = 1.5f;         // Legacy
            dagger.baseDamage = 6;
            dagger.speed = 1.5f;
            dagger.stunDuration = 0.4f;       // Fast weapon - short stun (adjusted from 0.5s)
            dagger.comboLength = 5;           // Fast weapon - 5-hit combo chain
            dagger.knockdownRate = 0.8f;      // Lower knockdown buildup
            dagger.isFastWeapon = true;       // Enables N+2 system (future)
            dagger.executionSpeedModifier = -0.2f; // -20%
            dagger.speedResolutionModifier = 0.2f; // +20%
            dagger.meleeTrailColor = Color.yellow;
            dagger.meleeTrailWidth = 0.2f;
            dagger.meleeTrailTime = 0.2f;
            dagger.meleeTrailStartWidthMultiplier = 1.0f;
            dagger.meleeTrailEndWidthMultiplier = 0.3f; // Sharp taper for speed
            dagger.meleeTrailEndColor = new Color(1.0f, 0.8f, 0.0f); // Orange-yellow gradient
            dagger.meleeTrailCornerVertices = 5; // Smooth for fast slashes
            dagger.meleeTrailEndCapVertices = 5;
            dagger.description = "Speed advantage with damage trade-offs - good thrown weapon (classic: no range disadvantage)";
            return dagger;
        }

        public static WeaponData CreateMaceData()
        {
            var mace = CreateInstance<WeaponData>();
            mace.weaponName = "Mace";
            mace.weaponType = WeaponType.Mace;
            mace.meleeRange = 1.5f;      // Uniform melee range (classic Mabinogi)
            mace.rangedRange = 0.5f;     // Throwing mace is terrible
            mace.meleeDamageMultiplier = 1.0f;   // Full melee damage (15)
            mace.rangedDamageMultiplier = 0.3f;  // Terrible throw (~5)
            mace.range = 1.5f;           // Legacy
            mace.baseDamage = 15;
            mace.speed = 0.6f;
            mace.stunDuration = 1.7f;         // Slow weapon - long stun (adjusted from 1.5s)
            mace.comboLength = 1;             // Heavy weapon - single powerful hit
            mace.knockdownRate = 1.3f;        // Higher knockdown buildup
            mace.isFastWeapon = false;
            mace.executionSpeedModifier = 0.3f; // +30%
            mace.speedResolutionModifier = -0.3f; // -30%
            mace.meleeTrailColor = Color.red;
            mace.meleeTrailWidth = 0.5f;
            mace.meleeTrailTime = 0.4f;
            mace.meleeTrailStartWidthMultiplier = 1.5f; // Thick start for heavy impact
            mace.meleeTrailEndWidthMultiplier = 0.8f;
            mace.meleeTrailEndColor = new Color(0.8f, 0.0f, 0.0f); // Dark red at impact
            mace.meleeTrailCornerVertices = 2; // Less smooth for brutal weapon
            mace.meleeTrailEndCapVertices = 3;
            mace.description = "Damage/stun advantage with speed trade-offs - poor thrown weapon (classic: no range disadvantage)";
            return mace;
        }

        // RANGED WEAPONS

        public static WeaponData CreateBowData()
        {
            var bow = CreateInstance<WeaponData>();
            bow.weaponName = "Bow";
            bow.weaponType = WeaponType.Bow;
            bow.meleeRange = 1.0f;       // Can club enemies with bow (emergency)
            bow.rangedRange = 6.0f;      // Primary archery range
            bow.meleeDamageMultiplier = 0.5f;    // Weak club (5 damage)
            bow.rangedDamageMultiplier = 1.0f;   // Full archery damage (10)
            bow.range = 6.0f;            // Legacy
            bow.baseDamage = 10;
            bow.speed = 1.0f;
            bow.stunDuration = 0.3f;          // Ranged weapon - very short stun
            bow.comboLength = 1;              // Ranged weapon - no combo chain
            bow.knockdownRate = 0.7f;         // Low knockdown (ranged weapon)
            bow.isFastWeapon = false;
            bow.executionSpeedModifier = 0f;
            bow.speedResolutionModifier = 0f;
            bow.meleeTrailColor = new Color(0.7f, 0.7f, 0.7f); // Light gray (emergency melee)
            bow.meleeTrailWidth = 0.25f;
            bow.meleeTrailTime = 0.25f;
            bow.meleeTrailStartWidthMultiplier = 1.0f;
            bow.meleeTrailEndWidthMultiplier = 1.0f; // Uniform (weak melee)
            bow.meleeTrailEndColor = new Color(0.5f, 0.5f, 0.5f); // Darker gray
            bow.meleeTrailCornerVertices = 2;
            bow.meleeTrailEndCapVertices = 2;
            bow.isRangedWeapon = true;
            bow.projectileType = "Arrow";
            bow.trailColorStart = Color.yellow;
            bow.trailColorEnd = Color.red;
            bow.trailWidth = 0.08f;
            bow.description = "Standard ranged weapon with good range and accuracy - weak melee";
            return bow;
        }

        public static WeaponData CreateJavelinData()
        {
            var javelin = CreateInstance<WeaponData>();
            javelin.weaponName = "Javelin";
            javelin.weaponType = WeaponType.Javelin;
            javelin.meleeRange = 2.0f;   // Can stab with javelin in close combat
            javelin.rangedRange = 4.5f;  // Designed for throwing
            javelin.meleeDamageMultiplier = 0.8f;    // Okay melee stab (~11 damage)
            javelin.rangedDamageMultiplier = 1.0f;   // Full throwing damage (14)
            javelin.range = 4.5f;        // Legacy
            javelin.baseDamage = 14;
            javelin.speed = 0.8f; // Slower weapon = longer recovery
            javelin.stunDuration = 1.2f;      // Medium-long stun
            javelin.comboLength = 1;          // Ranged weapon - no combo chain
            javelin.knockdownRate = 1.2f;     // Good knockdown for heavy thrown
            javelin.isFastWeapon = false;
            javelin.executionSpeedModifier = 0.1f;
            javelin.speedResolutionModifier = -0.15f;
            javelin.meleeTrailColor = new Color(0.55f, 0.65f, 0.75f); // Steel gray-blue (decent melee)
            javelin.meleeTrailWidth = 0.28f;
            javelin.meleeTrailTime = 0.3f;
            javelin.meleeTrailStartWidthMultiplier = 1.2f;
            javelin.meleeTrailEndWidthMultiplier = 0.6f; // Taper for javelin stab
            javelin.meleeTrailEndColor = new Color(0.4f, 0.5f, 0.6f); // Darker steel
            javelin.meleeTrailCornerVertices = 2;
            javelin.meleeTrailEndCapVertices = 5;
            javelin.isRangedWeapon = true;
            javelin.projectileType = "Javelin";
            javelin.trailColorStart = new Color(0.6f, 0.6f, 0.6f); // Gray
            javelin.trailColorEnd = Color.white;
            javelin.trailWidth = 0.12f; // Thicker trail
            javelin.description = "Heavy thrown weapon - high damage, shorter range, slower - can stab in melee";
            return javelin;
        }

        public static WeaponData CreateThrowingKnifeData()
        {
            var knife = CreateInstance<WeaponData>();
            knife.weaponName = "Throwing Knife";
            knife.weaponType = WeaponType.ThrowingKnife;
            knife.meleeRange = 1.2f;     // Can use as knife in melee (weak)
            knife.rangedRange = 3.5f;    // Designed for throwing
            knife.meleeDamageMultiplier = 0.7f;    // Weak melee (~5 damage)
            knife.rangedDamageMultiplier = 1.0f;   // Full throwing damage (7)
            knife.range = 3.5f;          // Legacy
            knife.baseDamage = 7;
            knife.speed = 1.3f; // Fast weapon = quick recovery
            knife.stunDuration = 0.25f;       // Very fast weapon - very short stun
            knife.comboLength = 1;            // Ranged weapon - no combo chain
            knife.knockdownRate = 0.6f;       // Very low knockdown
            knife.isFastWeapon = true;        // Enables N+2 system (future)
            knife.executionSpeedModifier = -0.15f;
            knife.speedResolutionModifier = 0.15f;
            knife.meleeTrailColor = Color.cyan;
            knife.meleeTrailWidth = 0.2f;
            knife.meleeTrailTime = 0.2f;
            knife.meleeTrailStartWidthMultiplier = 1.0f;
            knife.meleeTrailEndWidthMultiplier = 0.4f; // Sharp taper
            knife.meleeTrailEndColor = new Color(0.0f, 0.8f, 1.0f); // Bright cyan
            knife.meleeTrailCornerVertices = 5;
            knife.meleeTrailEndCapVertices = 5;
            knife.isRangedWeapon = true;
            knife.projectileType = "Knife";
            knife.trailColorStart = Color.cyan;
            knife.trailColorEnd = Color.blue;
            knife.trailWidth = 0.05f; // Thin trail
            knife.description = "Quick ranged attack - low damage, fast speed, short range - weak melee";
            return knife;
        }

        public static WeaponData CreateSlingData()
        {
            var sling = CreateInstance<WeaponData>();
            sling.weaponName = "Sling";
            sling.weaponType = WeaponType.Sling;
            sling.meleeRange = 0.8f;     // Terrible for melee (it's a sling!)
            sling.rangedRange = 5.0f;    // Good range for slinging stones
            sling.meleeDamageMultiplier = 0.4f;    // Awful melee (~2 damage)
            sling.rangedDamageMultiplier = 1.0f;   // Full slinging damage (6)
            sling.range = 5.0f;          // Legacy
            sling.baseDamage = 6;
            sling.speed = 1.1f;
            sling.stunDuration = 0.8f; // Blunt impact
            sling.comboLength = 1;            // Ranged weapon - no combo chain
            sling.knockdownRate = 1.0f;       // Blunt weapons have good knockdown
            sling.isFastWeapon = false;
            sling.executionSpeedModifier = 0f;
            sling.speedResolutionModifier = 0f;
            sling.meleeTrailColor = new Color(0.5f, 0.4f, 0.3f); // Brown/tan (terrible melee)
            sling.meleeTrailWidth = 0.25f;
            sling.meleeTrailTime = 0.25f;
            sling.meleeTrailStartWidthMultiplier = 1.0f;
            sling.meleeTrailEndWidthMultiplier = 1.0f; // Uniform (terrible melee)
            sling.meleeTrailEndColor = new Color(0.4f, 0.3f, 0.2f); // Darker brown
            sling.meleeTrailCornerVertices = 2;
            sling.meleeTrailEndCapVertices = 2;
            sling.isRangedWeapon = true;
            sling.projectileType = "Stone";
            sling.trailColorStart = new Color(0.5f, 0.4f, 0.3f); // Brown/tan
            sling.trailColorEnd = Color.gray;
            sling.trailWidth = 0.06f;
            sling.description = "Simple ranged weapon using stones as projectiles - awful melee";
            return sling;
        }

        public static WeaponData CreateThrowingAxeData()
        {
            var axe = CreateInstance<WeaponData>();
            axe.weaponName = "Throwing Axe";
            axe.weaponType = WeaponType.ThrowingAxe;
            axe.meleeRange = 2.0f;       // Can swing axe in melee combat
            axe.rangedRange = 3.0f;      // Short-mid range when thrown
            axe.meleeDamageMultiplier = 0.9f;    // Decent melee chop (~11 damage)
            axe.rangedDamageMultiplier = 1.0f;   // Full throwing damage (12)
            axe.range = 3.0f;            // Legacy
            axe.baseDamage = 12;
            axe.speed = 0.9f;
            axe.stunDuration = 1.0f;          // Normal stun
            axe.comboLength = 1;              // Ranged weapon - no combo chain
            axe.knockdownRate = 1.1f;         // Decent knockdown for heavy weapon
            axe.isFastWeapon = false;
            axe.executionSpeedModifier = 0.05f;
            axe.speedResolutionModifier = -0.1f;
            axe.meleeTrailColor = new Color(0.6f, 0.1f, 0.1f); // Dark red (decent melee)
            axe.meleeTrailWidth = 0.4f;
            axe.meleeTrailTime = 0.35f;
            axe.meleeTrailStartWidthMultiplier = 1.3f;
            axe.meleeTrailEndWidthMultiplier = 0.7f; // Chop effect
            axe.meleeTrailEndColor = new Color(0.8f, 0.2f, 0.0f); // Bright red-orange at impact
            axe.meleeTrailCornerVertices = 2;
            axe.meleeTrailEndCapVertices = 3;
            axe.isRangedWeapon = true;
            axe.projectileType = "Axe";
            axe.trailColorStart = Color.red;
            axe.trailColorEnd = new Color(0.5f, 0f, 0f); // Dark red
            axe.trailWidth = 0.10f;
            axe.description = "Versatile weapon - decent melee attacks AND ranged throw";
            return axe;
        }
    }
}