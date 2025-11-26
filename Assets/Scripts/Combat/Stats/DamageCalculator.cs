using UnityEngine;

namespace FairyGate.Combat
{
    public static class DamageCalculator
    {
        public static int CalculateBaseDamage(CharacterStats attackerStats, WeaponData weapon, CharacterStats defenderStats, SkillType skillType)
        {
            // Apply skill-specific damage multiplier
            float damageMultiplier = skillType == SkillType.RangedAttack
                ? weapon.rangedDamageMultiplier
                : weapon.meleeDamageMultiplier;

            int weaponDamage = Mathf.RoundToInt(weapon.baseDamage * damageMultiplier);
            int totalDamage = weaponDamage + attackerStats.strength - defenderStats.physicalDefense;
            return Mathf.Max(totalDamage, CombatConstants.MINIMUM_DAMAGE);
        }

        public static int CalculateCounterReflection(CharacterStats attackerStats, WeaponData attackerWeapon)
        {
            int reflectedDamage = attackerWeapon.baseDamage + attackerStats.strength - attackerStats.physicalDefense;
            return Mathf.Max(reflectedDamage, CombatConstants.MINIMUM_DAMAGE);
        }

        public static int ApplyDamageReduction(int baseDamage, float reductionPercent, CharacterStats defenderStats)
        {
            // Formula: Min(0.90, Base Reduction × (1 + Physical Defense/20))
            float enhancedReduction = Mathf.Min(
                CombatConstants.MAX_DAMAGE_REDUCTION,
                reductionPercent * (1 + defenderStats.physicalDefense / CombatConstants.PHYSICAL_DEFENSE_REDUCTION_DIVISOR)
            );

            float finalDamage = baseDamage * (1 - enhancedReduction);
            return Mathf.Max(Mathf.RoundToInt(finalDamage), CombatConstants.MINIMUM_DAMAGE);
        }

        // Critical Hit System
        public static bool RollCriticalHit(CharacterStats attackerStats)
        {
            float roll = Random.Range(0f, 100f);
            return roll < attackerStats.criticalChance;
        }

        public static int CalculateCriticalDamage(int baseDamage, float criticalMultiplier = 1.5f)
        {
            return Mathf.RoundToInt(baseDamage * criticalMultiplier);
        }

        public static float CalculateCriticalStunMultiplier()
        {
            return CombatConstants.CRITICAL_STUN_MULTIPLIER;
        }

        public static float CalculateStunDuration(float baseStunDuration, CharacterStats targetStats)
        {
            return baseStunDuration * (1 - targetStats.focus / CombatConstants.FOCUS_STUN_RESISTANCE_DIVISOR);
        }

        public static float CalculateStunDuration(float baseStunDuration, CharacterStats targetStats, bool wasCriticalHit)
        {
            float stunDuration = baseStunDuration * (1 - targetStats.focus / CombatConstants.FOCUS_STUN_RESISTANCE_DIVISOR);

            // Apply critical hit stun multiplier
            if (wasCriticalHit)
            {
                stunDuration *= CombatConstants.CRITICAL_STUN_MULTIPLIER;
            }

            return stunDuration;
        }

        public static float CalculateKnockdownDuration(CharacterStats targetStats)
        {
            return CombatConstants.KNOCKDOWN_DURATION * (1 - targetStats.focus / CombatConstants.FOCUS_STATUS_RECOVERY_DIVISOR);
        }

        public static int CalculateKnockdownMeterBuildup(CharacterStats attackerStats, CharacterStats defenderStats)
        {
            float buildup = CombatConstants.ATTACK_KNOCKDOWN_BUILDUP + (attackerStats.strength / CombatConstants.STRENGTH_KNOCKDOWN_DIVISOR);
            buildup -= (defenderStats.focus / CombatConstants.FOCUS_STATUS_RECOVERY_DIVISOR);
            return Mathf.Max(Mathf.RoundToInt(buildup), 1);
        }

        public static int CalculateKnockdownMeterBuildup(CharacterStats attackerStats, CharacterStats defenderStats, float weaponKnockdownRate)
        {
            // Base knockdown buildup
            float buildup = CombatConstants.ATTACK_KNOCKDOWN_BUILDUP + (attackerStats.strength / CombatConstants.STRENGTH_KNOCKDOWN_DIVISOR);

            // Apply weapon knockdown rate modifier
            buildup *= weaponKnockdownRate;

            // Apply Will resistance (defender stat)
            float willResistance = defenderStats.will / 30f;  // 0-1 range
            buildup *= (1f - willResistance);

            // Subtract Focus resistance (legacy)
            buildup -= (defenderStats.focus / CombatConstants.FOCUS_STATUS_RECOVERY_DIVISOR);

            return Mathf.Max(Mathf.RoundToInt(buildup), 1);
        }

        public static float CalculateStaminaDrainRate(float baseDrainRate, CharacterStats userStats)
        {
            return baseDrainRate * (1 - userStats.StaminaEfficiency);
        }

        // NOTE: This method is currently unused - damage is calculated directly in CombatInteractionManager
        public static DamageResult ProcessDamage(
            CharacterStats attacker,
            WeaponData attackerWeapon,
            CharacterStats defender,
            InteractionResult interaction,
            SkillType skillType = SkillType.Attack)
        {
            var result = new DamageResult();

            switch (interaction)
            {
                case InteractionResult.AttackerWins:
                    result.damage = CalculateBaseDamage(attacker, attackerWeapon, defender, skillType);
                    result.stunDuration = CalculateStunDuration(attackerWeapon.stunDuration, defender);
                    break;

                case InteractionResult.DefenderBlocks:
                    result.damage = 0;
                    result.attackerStunDuration = CalculateStunDuration(attackerWeapon.stunDuration, attacker);
                    result.defenderStunDuration = CalculateStunDuration(attackerWeapon.stunDuration * 0.5f, defender);
                    break;

                case InteractionResult.CounterReflection:
                    result.damage = 0; // Defender takes no damage
                    result.reflectedDamage = CalculateCounterReflection(attacker, attackerWeapon);
                    result.knockdownDuration = CalculateKnockdownDuration(attacker);
                    break;

                case InteractionResult.DefenderKnockedDown:
                    int reducedDamage = ApplyDamageReduction(
                        CalculateBaseDamage(attacker, attackerWeapon, defender, skillType),
                        CombatConstants.SMASH_VS_DEFENSE_DAMAGE_REDUCTION,
                        defender
                    );
                    result.damage = reducedDamage;
                    result.knockdownDuration = CalculateKnockdownDuration(defender);
                    break;

                case InteractionResult.SimultaneousExecution:
                    result.damage = CalculateBaseDamage(attacker, attackerWeapon, defender, skillType);
                    result.reflectedDamage = CalculateBaseDamage(defender, attackerWeapon, attacker, skillType); // Assuming same weapon for simplicity
                    break;
            }

            return result;
        }
    }

    [System.Serializable]
    public class DamageResult
    {
        public int damage;
        public int reflectedDamage;
        public float stunDuration;
        public float attackerStunDuration;
        public float defenderStunDuration;
        public float knockdownDuration;
        public int knockdownMeterBuildup;
    }
}