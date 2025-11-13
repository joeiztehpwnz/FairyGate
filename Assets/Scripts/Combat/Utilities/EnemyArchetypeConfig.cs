using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Configuration helper for enemy archetypes.
    /// Provides stats and AI recommendations for each enemy type.
    /// </summary>
    public static class EnemyArchetypeConfig
    {
        public struct ArchetypeData
        {
            public CharacterStats stats;
            public string aiType;           // Recommended AI: "SimpleTestAI", "KnightAI", "PatternedAI", "TestRepeaterAI"
            public float attackWeight;
            public float defenseWeight;
            public float counterWeight;
            public float smashWeight;
            public float windmillWeight;
            public float lungeWeight;
            public float rangedAttackWeight;
            public float skillCooldown;
            public bool enableMovement;     // For TestRepeaterAI
            public SkillType repeaterSkill; // For TestRepeaterAI

            // Weapon Swapping Configuration
            public WeaponType primaryWeapon;            // Primary weapon slot
            public WeaponType secondaryWeapon;          // Secondary weapon slot
            public bool enableWeaponSwapping;           // Whether this archetype can swap weapons
            public PreferredRange preferPrimaryAtRange; // When to prefer primary weapon (Close/Far/Either)
            public PreferredRange preferSecondaryAtRange; // When to prefer secondary weapon
            public float swapDistanceThreshold;         // Distance in units to trigger swap consideration
            public float swapCooldown;                  // Minimum seconds between swaps
        }

        public static ArchetypeData GetArchetypeData(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Berserker:
                    return new ArchetypeData
                    {
                        stats = CharacterStats.CreateBerserkerStats(),
                        aiType = "SimpleTestAI",
                        attackWeight = 50f,    // Very aggressive
                        defenseWeight = 5f,    // Rarely defends
                        counterWeight = 10f,
                        smashWeight = 25f,     // Loves smash
                        windmillWeight = 10f,
                        lungeWeight = 15f,
                        rangedAttackWeight = 0f,
                        skillCooldown = 1.5f,  // Fast attacks
                        // Weapon Swapping: Aggressive close-range fighter
                        primaryWeapon = WeaponType.Mace,                // Heavy hitter for close combat
                        secondaryWeapon = WeaponType.Sword,             // Medium reach fallback
                        enableWeaponSwapping = true,
                        preferPrimaryAtRange = PreferredRange.Close,    // Mace when close
                        preferSecondaryAtRange = PreferredRange.Far,    // Sword when further
                        swapDistanceThreshold = 2.5f,                   // Aggressive close-range threshold
                        swapCooldown = 3.0f                             // Fast swaps for aggression
                    };

                case EnemyArchetype.Guardian:
                    return new ArchetypeData
                    {
                        stats = CharacterStats.CreateGuardianStats(),
                        aiType = "SimpleTestAI",   // Reactive AI with defensive personality
                        attackWeight = 15f,
                        defenseWeight = 45f,   // Very defensive
                        counterWeight = 25f,   // Counter-heavy
                        smashWeight = 10f,
                        windmillWeight = 5f,
                        lungeWeight = 5f,
                        rangedAttackWeight = 0f,
                        skillCooldown = 2.5f,  // Slower, methodical
                        // Weapon Swapping: Defensive spacing control
                        primaryWeapon = WeaponType.Mace,                // Heavy weapon for close defense
                        secondaryWeapon = WeaponType.Spear,             // Long reach to maintain distance
                        enableWeaponSwapping = true,
                        preferPrimaryAtRange = PreferredRange.Close,    // Mace when cornered
                        preferSecondaryAtRange = PreferredRange.Far,    // Spear to keep distance
                        swapDistanceThreshold = 4.0f,                   // Larger defensive bubble
                        swapCooldown = 7.0f                             // Methodical, deliberate swaps
                    };

                case EnemyArchetype.Assassin:
                    return new ArchetypeData
                    {
                        stats = CharacterStats.CreateAssassinStats(),
                        aiType = "SimpleTestAI",   // Reactive AI with counter-focused personality
                        attackWeight = 25f,
                        defenseWeight = 20f,
                        counterWeight = 35f,   // Counter master
                        smashWeight = 5f,
                        windmillWeight = 15f,
                        lungeWeight = 20f,     // Fast gap closer
                        rangedAttackWeight = 0f,
                        skillCooldown = 1.8f,  // Quick, responsive
                        // Weapon Swapping: Speed and consistency over versatility
                        primaryWeapon = WeaponType.Dagger,              // Fast primary
                        secondaryWeapon = WeaponType.Dagger,            // Same weapon (no swapping needed)
                        enableWeaponSwapping = false,                   // Focus on speed, not swapping
                        preferPrimaryAtRange = PreferredRange.Close,
                        preferSecondaryAtRange = PreferredRange.Close,
                        swapDistanceThreshold = 3.0f,
                        swapCooldown = 5.0f
                    };

                case EnemyArchetype.Archer:
                    return new ArchetypeData
                    {
                        stats = CharacterStats.CreateArcherStats(),
                        aiType = "SimpleTestAI",
                        attackWeight = 15f,    // Melee fallback when close
                        defenseWeight = 10f,   // Minimal defense (prefers staying at range)
                        counterWeight = 10f,   // Minimal counter
                        smashWeight = 5f,      // Rarely smash
                        windmillWeight = 5f,   // Rarely windmill
                        lungeWeight = 0f,      // Never close distance
                        rangedAttackWeight = 100f,  // Primary attack (boosted to ~200 at range)
                        skillCooldown = 2.0f,  // Faster attack cycle
                        enableMovement = true,
                        repeaterSkill = SkillType.RangedAttack,
                        // Weapon Swapping: Ranged specialist with melee fallback
                        primaryWeapon = WeaponType.Bow,                 // Ranged primary
                        secondaryWeapon = WeaponType.Dagger,            // Fast melee for close quarters
                        enableWeaponSwapping = true,
                        preferPrimaryAtRange = PreferredRange.Far,      // Bow when at range
                        preferSecondaryAtRange = PreferredRange.Close,  // Dagger when rushed
                        swapDistanceThreshold = 3.5f,                   // Tactical retreat threshold
                        swapCooldown = 5.0f                             // Balanced swap frequency
                    };

                case EnemyArchetype.Soldier:
                default:
                    var soldierData = new ArchetypeData
                    {
                        stats = CharacterStats.CreateSoldierStats(),
                        aiType = "SimpleTestAI",
                        attackWeight = 30f,    // Balanced
                        defenseWeight = 20f,
                        counterWeight = 20f,
                        smashWeight = 15f,
                        windmillWeight = 15f,
                        lungeWeight = 10f,
                        rangedAttackWeight = 0f,
                        skillCooldown = 2.0f
                    };
                    ApplyDefaultWeaponSwapProfile(ref soldierData); // Use default: Sword + Sword, swapping disabled
                    return soldierData;
            }
        }

        public static string GetArchetypeDescription(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Berserker:
                    return "Glass Cannon - High damage, low defense, aggressive";
                case EnemyArchetype.Guardian:
                    return "Tank - High defense, slow, defensive playstyle";
                case EnemyArchetype.Assassin:
                    return "Speedster - Fast, evasive, counter-focused";
                case EnemyArchetype.Archer:
                    return "Ranged Specialist - Keeps distance, ranged attacks";
                case EnemyArchetype.Soldier:
                default:
                    return "Balanced - Jack of all trades";
            }
        }

        /// <summary>
        /// Helper method to apply default weapon swap configuration values.
        /// Used as a base profile that archetypes can inherit or override.
        /// </summary>
        private static void ApplyDefaultWeaponSwapProfile(ref ArchetypeData data)
        {
            data.primaryWeapon = WeaponType.Sword;           // Default primary weapon
            data.secondaryWeapon = WeaponType.Sword;         // Default secondary (same as primary)
            data.enableWeaponSwapping = false;               // Swapping disabled by default
            data.preferPrimaryAtRange = PreferredRange.Either;
            data.preferSecondaryAtRange = PreferredRange.Either;
            data.swapDistanceThreshold = 3.0f;               // Standard engagement distance
            data.swapCooldown = 5.0f;                        // Moderate cooldown
        }
    }
}
