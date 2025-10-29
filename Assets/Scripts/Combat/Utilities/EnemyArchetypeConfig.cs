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
            public float skillCooldown;
            public bool enableMovement;     // For TestRepeaterAI
            public SkillType repeaterSkill; // For TestRepeaterAI
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
                        skillCooldown = 1.5f   // Fast attacks
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
                        skillCooldown = 2.5f   // Slower, methodical
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
                        skillCooldown = 1.8f   // Quick, responsive
                    };

                case EnemyArchetype.Archer:
                    return new ArchetypeData
                    {
                        stats = CharacterStats.CreateArcherStats(),
                        aiType = "SimpleTestAI",   // Reactive AI (special fields unused but harmless)
                        attackWeight = 10f,
                        defenseWeight = 15f,
                        counterWeight = 15f,
                        smashWeight = 5f,
                        windmillWeight = 5f,
                        skillCooldown = 2.0f,
                        enableMovement = true,     // Unused by SimpleTestAI
                        repeaterSkill = SkillType.RangedAttack  // Unused by SimpleTestAI
                    };

                case EnemyArchetype.Soldier:
                default:
                    return new ArchetypeData
                    {
                        stats = CharacterStats.CreateSoldierStats(),
                        aiType = "SimpleTestAI",
                        attackWeight = 30f,    // Balanced
                        defenseWeight = 20f,
                        counterWeight = 20f,
                        smashWeight = 15f,
                        windmillWeight = 15f,
                        skillCooldown = 2.0f
                    };
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
    }
}
