using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles determination of skill interactions and processing of their effects.
    /// Extracted from CombatInteractionManager to reduce complexity.
    /// </summary>
    public class SkillInteractionResolver
    {
        private readonly bool enableDebugLogs;

        public SkillInteractionResolver(bool enableDebugLogs = true)
        {
            this.enableDebugLogs = enableDebugLogs;
        }

        /// <summary>
        /// Determines the interaction result between an offensive and defensive skill.
        /// </summary>
        public InteractionResult DetermineInteraction(SkillType offensive, SkillType defensive)
        {
            // All interactions from the matrix
            switch (offensive)
            {
                case SkillType.Attack:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.AttackerStunned; // Attack vs Defense → Attacker stunned, defender blocks
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Attack vs Counter → Counter reflection
                    }
                    break;

                case SkillType.Smash:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderKnockedDown; // Smash vs Defense → Defender knocked down, takes 75% reduced damage
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Smash vs Counter → Counter reflection
                    }
                    break;

                case SkillType.Windmill:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderBlocks; // Windmill vs Defense → No status effects, defender blocks
                        case SkillType.Counter: return InteractionResult.WindmillBreaksCounter; // Windmill vs Counter → Windmill breaks counter, knocks down defender
                    }
                    break;

                case SkillType.RangedAttack:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderBlocks; // RangedAttack vs Defense → Defender blocks 100% damage
                        case SkillType.Counter: return InteractionResult.CounterIneffective; // RangedAttack vs Counter → Counter is ineffective
                    }
                    break;

                case SkillType.Lunge:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.AttackerStunned; // Lunge vs Defense → Attacker stunned, defender blocks
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Lunge vs Counter → Counter reflection
                    }
                    break;
            }

            return InteractionResult.NoInteraction;
        }

        /// <summary>
        /// Processes the effects of a skill interaction, applying damage and status effects.
        /// </summary>
        public void ProcessInteractionEffects(
            InteractionResult interaction,
            SkillExecution attacker,
            SkillExecution defender,
            CharacterStats attackerStats,
            CharacterStats defenderStats,
            WeaponData attackerWeapon,
            WeaponData defenderWeapon,
            System.Action<SkillExecution> completeDefensiveSkillCallback)
        {
            // NULL SAFETY: Check if combatants are destroyed (scene reload, death during processing)
            if (attacker == null || attacker.combatant == null || !attacker.combatant ||
                defender == null || defender.combatant == null || !defender.combatant)
            {
                CombatLogger.LogCombat($"[Scene Cleanup] ProcessInteractionEffects aborted - attacker or defender destroyed", CombatLogger.LogLevel.Warning);
                return;
            }

            var attackerHealth = attacker.combatant.GetComponent<HealthSystem>();
            var defenderHealth = defender.combatant.GetComponent<HealthSystem>();
            var attackerStatusEffects = attacker.combatant.GetComponent<StatusEffectManager>();
            var defenderStatusEffects = defender.combatant.GetComponent<StatusEffectManager>();
            var defenderKnockdownMeter = defender.combatant.GetComponent<KnockdownMeterTracker>();

            // Null safety checks
            if (attackerHealth == null || defenderHealth == null ||
                attackerStatusEffects == null || defenderStatusEffects == null ||
                defenderKnockdownMeter == null)
            {
                CombatLogger.LogCombat($"[Scene Cleanup] ProcessInteractionEffects aborted - missing components", CombatLogger.LogLevel.Warning);
                return;
            }

            switch (interaction)
            {
                case InteractionResult.AttackerStunned: // Attack vs Defense
                    ProcessAttackerStunned(attacker, defender, attackerStats, defenderStats, attackerWeapon, attackerStatusEffects, defenderStatusEffects);
                    completeDefensiveSkillCallback(defender);
                    break;

                case InteractionResult.CounterReflection: // Any skill vs Counter
                    ProcessCounterReflection(attacker, defender, attackerStats, attackerWeapon, attackerHealth, attackerStatusEffects);
                    completeDefensiveSkillCallback(defender);
                    break;

                case InteractionResult.CounterIneffective: // RangedAttack vs Counter
                    ProcessCounterIneffective(attacker, defender, attackerStats, attackerWeapon, defenderStats, defenderHealth, defenderStatusEffects, defenderKnockdownMeter);
                    completeDefensiveSkillCallback(defender);
                    break;

                case InteractionResult.DefenderKnockedDown: // Smash vs Defense
                    ProcessDefenderKnockedDown(attacker, defender, attackerStats, attackerWeapon, defenderStats, defenderHealth, defenderStatusEffects);
                    completeDefensiveSkillCallback(defender);
                    break;

                case InteractionResult.DefenderBlocks: // Windmill vs Defense OR RangedAttack vs Defense
                    ProcessDefenderBlocks(attacker, defender);
                    completeDefensiveSkillCallback(defender);
                    break;

                case InteractionResult.WindmillBreaksCounter: // Windmill vs Counter
                    ProcessWindmillBreaksCounter(attacker, defender, attackerStats, attackerWeapon, defenderStats, defenderHealth, defenderStatusEffects);
                    completeDefensiveSkillCallback(defender);
                    break;
            }
        }

        private void ProcessAttackerStunned(SkillExecution attacker, SkillExecution defender, CharacterStats attackerStats, CharacterStats defenderStats, WeaponData attackerWeapon, StatusEffectManager attackerStatusEffects, StatusEffectManager defenderStatusEffects)
        {
            // Attacker stunned, defender blocks (0 damage)
            // N+1 System: Calculate stun with Focus resistance for cohesive timing
            float attackerStun = DamageCalculator.CalculateStunDuration(attackerWeapon.stunDuration, attackerStats);
            attackerStatusEffects.ApplyCalculatedStun(attackerStun);

            // Defender receives half calculated stun with Focus resistance
            float defenderStun = DamageCalculator.CalculateStunDuration(attackerWeapon.stunDuration, defenderStats) * 0.5f;
            defenderStatusEffects.ApplyCalculatedStun(defenderStun);

            // ONE-HIT BLOCK: Defense breaks after blocking first hit
            defender.skillSystem.MarkDefenseBlocked();

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{GetSafeCombatantName(attacker)} attack blocked by {GetSafeCombatantName(defender)} defense (Defense broken after block)");
            }
        }

        private void ProcessCounterReflection(SkillExecution attacker, SkillExecution defender, CharacterStats attackerStats, WeaponData attackerWeapon, HealthSystem attackerHealth, StatusEffectManager attackerStatusEffects)
        {
            // Attacker knocked down, defender takes 0 damage, reflects calculated damage back
            Vector3 counterKnockbackDirection = (attacker.combatant.transform.position - defender.combatant.transform.position).normalized;
            Vector3 counterDisplacement = counterKnockbackDirection * CombatConstants.COUNTER_KNOCKBACK_DISTANCE;
            attackerStatusEffects.ApplyInteractionKnockdown(counterDisplacement);

            // N+1 System: Roll for critical on counter reflection
            CharacterStats defenderStats = defender.combatant.Stats;
            bool wasCritical = defenderStats != null && DamageCalculator.RollCriticalHit(defenderStats);

            int reflectedDamage = DamageCalculator.CalculateCounterReflection(attackerStats, attackerWeapon);
            if (wasCritical)
            {
                reflectedDamage = DamageCalculator.CalculateCriticalDamage(reflectedDamage);
            }

            attackerHealth.TakeDamage(reflectedDamage, defender.combatant.transform);

            // Register hit dealt for defender's pattern tracking (counter reflected damage)
            WeaponController defenderWeaponController = defender.combatant.GetComponent<WeaponController>();
            defenderWeaponController?.RegisterHitDealt(attacker.combatant.transform);

            // Note: Counter reflections do NOT create N+1 windows (only basic Attack does)
            // Note: Counter knockdown overrides stun, but stun should technically be applied first
            // The knockdown from counter is stronger, so the stun effect is immediately overridden
            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{GetSafeCombatantName(defender)} counter reflected {reflectedDamage} damage to {GetSafeCombatantName(attacker)}");
            }
        }

        private void ProcessCounterIneffective(SkillExecution attacker, SkillExecution defender, CharacterStats attackerStats, WeaponData attackerWeapon, CharacterStats defenderStats, HealthSystem defenderHealth, StatusEffectManager defenderStatusEffects, KnockdownMeterTracker defenderKnockdownMeter)
        {
            // Counter is ineffective against ranged attacks
            // Check if ranged attack hit
            bool counterRangedAttackHit = attacker.skillSystem.LastRangedAttackHit;

            if (counterRangedAttackHit)
            {
                // HIT: Defender takes full damage, no reflection
                // N+1 System: Roll for critical hit
                bool wasCritical = DamageCalculator.RollCriticalHit(attackerStats);

                int rangedDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats, SkillType.RangedAttack);
                if (wasCritical)
                {
                    rangedDamage = DamageCalculator.CalculateCriticalDamage(rangedDamage);
                }

                defenderHealth.TakeDamage(rangedDamage, attacker.combatant.transform);

                // Register hit dealt for attacker's pattern tracking
                WeaponController rangedAttackerWeaponController = attacker.combatant.GetComponent<WeaponController>();
                rangedAttackerWeaponController?.RegisterHitDealt(defender.combatant.transform);

                // Note: RangedAttack skill does NOT create N+1 windows (only basic Attack does)
                // Apply universal hit stun with Focus resistance and critical multiplier
                float calculatedStun = DamageCalculator.CalculateStunDuration(attackerWeapon.stunDuration, defenderStats, wasCritical);
                defenderStatusEffects.ApplyCalculatedStun(calculatedStun);

                // Build knockdown meter
                // N+1 System: Pass weapon data for knockdown rate modifier
                defenderKnockdownMeter.AddMeterBuildup(rangedDamage, attackerStats, attackerWeapon, attacker.combatant.transform);

                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{GetSafeCombatantName(defender)} Counter ineffective against {GetSafeCombatantName(attacker)} RangedAttack - took {rangedDamage} damage");
                }
            }
            else
            {
                // MISS: Counter takes 0 damage but still completes (wasted counter)
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{GetSafeCombatantName(attacker)} RangedAttack missed - {GetSafeCombatantName(defender)} Counter wasted");
                }
            }
        }

        private void ProcessDefenderKnockedDown(SkillExecution attacker, SkillExecution defender, CharacterStats attackerStats, WeaponData attackerWeapon, CharacterStats defenderStats, HealthSystem defenderHealth, StatusEffectManager defenderStatusEffects)
        {
            // Defender knocked down, takes 75% reduced damage
            Vector3 smashKnockbackDirection = (defender.combatant.transform.position - attacker.combatant.transform.position).normalized;
            Vector3 smashDisplacement = smashKnockbackDirection * CombatConstants.SMASH_KNOCKBACK_DISTANCE;
            defenderStatusEffects.ApplyInteractionKnockdown(smashDisplacement);

            // N+1 System: Roll for critical hit
            bool wasCritical = DamageCalculator.RollCriticalHit(attackerStats);

            int baseDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats, attacker.skillType);
            if (wasCritical)
            {
                baseDamage = DamageCalculator.CalculateCriticalDamage(baseDamage);
            }

            int reducedDamage = DamageCalculator.ApplyDamageReduction(baseDamage, CombatConstants.SMASH_VS_DEFENSE_DAMAGE_REDUCTION, defenderStats);
            defenderHealth.TakeDamage(reducedDamage, attacker.combatant.transform);

            // Register hit dealt for attacker's pattern tracking
            WeaponController attackerWeaponController = attacker.combatant.GetComponent<WeaponController>();
            attackerWeaponController?.RegisterHitDealt(defender.combatant.transform);

            // Note: Smash skill does NOT create N+1 windows (only basic Attack does)

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{GetSafeCombatantName(attacker)} smash broke through {GetSafeCombatantName(defender)} defense for {reducedDamage} damage");
            }
        }

        private void ProcessDefenderBlocks(SkillExecution attacker, SkillExecution defender)
        {
            if (attacker.skillType == SkillType.RangedAttack)
            {
                // Check if the ranged attack hit or missed
                bool rangedAttackHit = attacker.skillSystem.LastRangedAttackHit;

                if (rangedAttackHit)
                {
                    // HIT: Defense blocks 100% of ranged attack damage (complete block)
                    // ONE-HIT BLOCK: Defense breaks after blocking first hit
                    defender.skillSystem.MarkDefenseBlocked();

                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat($"{GetSafeCombatantName(defender)} completely blocked {GetSafeCombatantName(attacker)} RangedAttack (Defense broken after block)");
                    }
                }
                else
                {
                    // MISS: Defense takes 0 damage, but is consumed (one block per activation)
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat($"{GetSafeCombatantName(attacker)} RangedAttack missed - {GetSafeCombatantName(defender)} Defense consumed (no block)");
                    }
                }
            }
            else
            {
                // Windmill vs Defense: Blocked cleanly (0 damage)
                // ONE-HIT BLOCK: Defense breaks after blocking first hit
                defender.skillSystem.MarkDefenseBlocked();

                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{GetSafeCombatantName(defender)} blocked {GetSafeCombatantName(attacker)} windmill (Defense broken after block)");
                }
            }
        }

        private void ProcessWindmillBreaksCounter(SkillExecution attacker, SkillExecution defender, CharacterStats attackerStats, WeaponData attackerWeapon, CharacterStats defenderStats, HealthSystem defenderHealth, StatusEffectManager defenderStatusEffects)
        {
            // Windmill breaks through counter, knocks down defender, deals normal damage
            Vector3 windmillKnockbackDirection = (defender.combatant.transform.position - attacker.combatant.transform.position).normalized;
            Vector3 windmillDisplacement = windmillKnockbackDirection * CombatConstants.WINDMILL_KNOCKBACK_DISTANCE;
            defenderStatusEffects.ApplyInteractionKnockdown(windmillDisplacement);

            // N+1 System: Roll for critical hit
            bool wasCritical = DamageCalculator.RollCriticalHit(attackerStats);

            int windmillDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats, attacker.skillType);
            if (wasCritical)
            {
                windmillDamage = DamageCalculator.CalculateCriticalDamage(windmillDamage);
            }

            defenderHealth.TakeDamage(windmillDamage, attacker.combatant.transform);

            // Register hit dealt for attacker's pattern tracking
            WeaponController windmillAttackerWeaponController = attacker.combatant.GetComponent<WeaponController>();
            windmillAttackerWeaponController?.RegisterHitDealt(defender.combatant.transform);

            // Note: Windmill skill does NOT create N+1 windows (only basic Attack does)

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{GetSafeCombatantName(attacker)} windmill broke through {GetSafeCombatantName(defender)} counter for {windmillDamage} damage and knockdown");
            }
        }

        /// <summary>
        /// Helper method to safely get combatant name (handles destroyed objects)
        /// </summary>
        private string GetSafeCombatantName(SkillExecution execution)
        {
            if (execution == null || execution.combatant == null || !execution.combatant)
            {
                return "[Destroyed]";
            }
            return execution.combatant.name;
        }
    }
}
