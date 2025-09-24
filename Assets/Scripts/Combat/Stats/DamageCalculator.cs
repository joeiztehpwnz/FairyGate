using UnityEngine;

namespace FairyGate.Combat
{
    public static class DamageCalculator
    {
        public static int CalculateBaseDamage(CharacterStats attackerStats, WeaponData weapon, CharacterStats defenderStats)
        {
            int baseDamage = weapon.baseDamage + attackerStats.strength - defenderStats.physicalDefense;
            return Mathf.Max(baseDamage, CombatConstants.MINIMUM_DAMAGE);
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

        public static float CalculateStunDuration(float baseStunDuration, CharacterStats targetStats)
        {
            return baseStunDuration * (1 - targetStats.focus / CombatConstants.FOCUS_STUN_RESISTANCE_DIVISOR);
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

        public static float CalculateStaminaDrainRate(float baseDrainRate, CharacterStats userStats)
        {
            return baseDrainRate * (1 - userStats.StaminaEfficiency);
        }

        public static DamageResult ProcessDamage(
            CharacterStats attacker,
            WeaponData attackerWeapon,
            CharacterStats defender,
            InteractionResult interaction)
        {
            var result = new DamageResult();

            switch (interaction)
            {
                case InteractionResult.AttackerWins:
                    result.damage = CalculateBaseDamage(attacker, attackerWeapon, defender);
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
                        CalculateBaseDamage(attacker, attackerWeapon, defender),
                        CombatConstants.SMASH_VS_DEFENSE_DAMAGE_REDUCTION,
                        defender
                    );
                    result.damage = reducedDamage;
                    result.knockdownDuration = CalculateKnockdownDuration(defender);
                    break;

                case InteractionResult.SimultaneousExecution:
                    result.damage = CalculateBaseDamage(attacker, attackerWeapon, defender);
                    result.reflectedDamage = CalculateBaseDamage(defender, attackerWeapon, attacker); // Assuming same weapon for simplicity
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