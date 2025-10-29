using UnityEngine;

namespace FairyGate.Combat
{
    public static class SpeedResolver
    {
        public static float CalculateSpeed(SkillType skillType, CharacterStats stats, WeaponData weapon)
        {
            float baseSpeed = weapon.speed + (stats.dexterity / CombatConstants.DEXTERITY_SPEED_DIVISOR);
            float modifiedSpeed = baseSpeed * (1 + weapon.speedResolutionModifier);
            return modifiedSpeed;
        }

        public static SpeedResolutionResult ResolveSpeedConflict(
            Transform combatant1Transform, Transform combatant2Transform,
            SkillType skill1, SkillType skill2)
        {
            var weapon1 = combatant1Transform.GetComponent<WeaponController>()?.WeaponData;
            var weapon2 = combatant2Transform.GetComponent<WeaponController>()?.WeaponData;
            var stats1 = combatant1Transform.GetComponent<CombatController>()?.Stats;
            var stats2 = combatant2Transform.GetComponent<CombatController>()?.Stats;

            float speed1 = (weapon1 != null && stats1 != null) ? CalculateSpeed(skill1, stats1, weapon1) : 0f;
            float speed2 = (weapon2 != null && stats2 != null) ? CalculateSpeed(skill2, stats2, weapon2) : 0f;

            var result = new SpeedResolutionResult
            {
                combatant1Speed = speed1,
                combatant2Speed = speed2,
                skill1 = skill1,
                skill2 = skill2
            };

            if (Mathf.Approximately(speed1, speed2))
            {
                result.resolution = SpeedResolution.Tie;
            }
            else if (speed1 > speed2)
            {
                result.resolution = SpeedResolution.Player1Wins;
                result.winner = combatant1Transform;
                result.loser = combatant2Transform;
                result.winningSkill = skill1;
                result.losingSkill = skill2;
            }
            else
            {
                result.resolution = SpeedResolution.Player2Wins;
                result.winner = combatant2Transform;
                result.loser = combatant1Transform;
                result.winningSkill = skill2;
                result.losingSkill = skill1;
            }

            return result;
        }

        public static bool RequiresSpeedResolution(SkillType skill1, SkillType skill2)
        {
            // Speed resolution only applies to offensive vs offensive skills
            return IsOffensiveSkill(skill1) && IsOffensiveSkill(skill2);
        }

        public static bool IsOffensiveSkill(SkillType skill)
        {
            return skill == SkillType.Attack || skill == SkillType.Smash || skill == SkillType.Windmill || skill == SkillType.RangedAttack || skill == SkillType.Lunge;
        }

        public static bool IsDefensiveSkill(SkillType skill)
        {
            return skill == SkillType.Defense || skill == SkillType.Counter;
        }

        public static bool CanInteract(SkillType skill1, SkillType skill2)
        {
            // Non-interactions (both defensive skills)
            if (skill1 == SkillType.Counter && skill2 == SkillType.Counter) return false;
            if (skill1 == SkillType.Defense && skill2 == SkillType.Defense) return false;
            if ((skill1 == SkillType.Defense && skill2 == SkillType.Counter) ||
                (skill1 == SkillType.Counter && skill2 == SkillType.Defense)) return false;

            return true;
        }

        public static float CalculateExecutionTime(SkillType skillType, WeaponData weapon, SkillExecutionState phase)
        {
            float baseTime = GetBaseExecutionTime(skillType, phase);
            return baseTime * (1 + weapon.executionSpeedModifier);
        }

        private static float GetBaseExecutionTime(SkillType skillType, SkillExecutionState phase)
        {
            switch (skillType)
            {
                case SkillType.Attack:
                    return phase switch
                    {
                        SkillExecutionState.Startup => 0.2f,
                        SkillExecutionState.Active => 0.2f,
                        SkillExecutionState.Recovery => 0.3f,
                        _ => 0f
                    };

                case SkillType.Smash:
                    return phase switch
                    {
                        SkillExecutionState.Startup => 0.5f,
                        SkillExecutionState.Active => 0.3f,
                        SkillExecutionState.Recovery => 0.8f,
                        _ => 0f
                    };

                case SkillType.Windmill:
                    return phase switch
                    {
                        SkillExecutionState.Startup => 0.3f,
                        SkillExecutionState.Active => 0.4f,
                        SkillExecutionState.Recovery => 0.5f,
                        _ => 0f
                    };

                case SkillType.Defense:
                    return phase switch
                    {
                        SkillExecutionState.Startup => 0.1f,
                        SkillExecutionState.Recovery => 0.2f,
                        _ => 0f
                    };

                case SkillType.Counter:
                    return phase switch
                    {
                        SkillExecutionState.Startup => 0.1f,
                        SkillExecutionState.Recovery => 0.4f,
                        _ => 0f
                    };

                case SkillType.Lunge:
                    return phase switch
                    {
                        SkillExecutionState.Startup => 0.1f,
                        SkillExecutionState.Active => 0.15f,
                        SkillExecutionState.Recovery => 0.2f,
                        _ => 0f
                    };

                default:
                    return 0f;
            }
        }
    }

    [System.Serializable]
    public class SpeedResolutionResult
    {
        public SpeedResolution resolution;
        public Transform winner;
        public Transform loser;
        public SkillType skill1;
        public SkillType skill2;
        public SkillType winningSkill;
        public SkillType losingSkill;
        public float combatant1Speed;
        public float combatant2Speed;
    }
}