using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Charging state - builds up charge meter from 0% to 100%.
    ///
    /// Lifecycle:
    /// - Entry: Reset elapsed time, start charging
    /// - Update: Increment time, update progress, handle knockdown pauses
    /// - Exit: Fire OnSkillCharged event
    ///
    /// Transitions:
    /// - chargeProgress >= 1.0 → ChargedState (auto-transition)
    ///
    /// Special Behavior:
    /// - Pauses (doesn't reset!) during knockdown
    /// - Charge time varies by skill and character stats
    /// </summary>
    public class ChargingState : SkillStateBase
    {
        private float chargeTime;

        public ChargingState(SkillSystem system, SkillType type) : base(system, type)
        {
            // Calculate charge time based on skill and stats
            chargeTime = CalculateChargeTime();
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Reset charge progress (Note: elapsedTime already reset by base class)
            skillSystem.ChargeProgress = 0f;

            if (skillSystem.EnableDebugLogs)
            {
                CombatLogger.LogSkill($"{skillSystem.gameObject.name} started charging {skillType} (charge time: {chargeTime:F2}s)");
            }
        }

        public override bool Update(float deltaTime)
        {
            // Classic Mabinogi: Knockdown CANCELS charging (lose progress)
            // Stun pauses but doesn't cancel (handled by StatusEffectManager.CanAct)
            if (skillSystem.StatusEffectManager.IsKnockedDown)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"[Classic Mabinogi] {skillSystem.gameObject.name} charging cancelled by knockdown - progress lost!");
                }

                // Force transition back to Uncharged state (lose all progress)
                skillSystem.StateMachine.TransitionTo(new UnchargedState(skillSystem, SkillType.Attack));
                return false;
            }

            // Stun pauses charging but doesn't cancel (progress preserved)
            if (skillSystem.StatusEffectManager.IsStunned)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} charging paused by stun (progress preserved)");
                }
                return false; // Don't increment time, don't transition
            }

            // Drain stamina while charging (punishing stamina management)
            float drainRate = GetChargingDrainRate();
            if (drainRate > 0 && skillSystem.StaminaSystem != null)
            {
                skillSystem.StaminaSystem.DrainStamina(drainRate, deltaTime);

                // Auto-cancel if stamina depleted
                if (skillSystem.StaminaSystem.CurrentStamina <= 0)
                {
                    if (skillSystem.EnableDebugLogs)
                    {
                        CombatLogger.LogSkill($"{skillSystem.gameObject.name} charging cancelled - stamina depleted!");
                    }
                    skillSystem.CancelSkill();
                    return false;
                }
            }

            // Increment elapsed time
            elapsedTime += deltaTime;

            // Update charge progress (0.0 → 1.0)
            skillSystem.ChargeProgress = Mathf.Clamp01(elapsedTime / chargeTime);

            // Check if fully charged
            if (skillSystem.ChargeProgress >= 1.0f)
            {
                skillSystem.ChargeProgress = 1.0f;

                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} fully charged and ready to execute");
                }

                return true; // Trigger transition to Charged
            }

            return false; // Continue charging
        }

        public override void OnExit()
        {
            // Reset movement modifier to prevent stuck states
            skillSystem.MovementController.SetMovementModifier(1.0f);

            base.OnExit();

            // Fire event for UI/feedback systems
            skillSystem.TriggerSkillCharged(skillType);
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Charging;
        }

        public override ISkillState GetNextState()
        {
            // Transition to Charged state when charging completes
            return new ChargedState(skillSystem, skillType);
        }

        /// <summary>
        /// Calculates charge time based on skill type and character stats.
        /// Classic Mabinogi system: Variable load times per skill (Windmill 0.8s, Smash 2.0s).
        /// </summary>
        private float CalculateChargeTime()
        {
            // Get base charge time per skill (Classic Mabinogi variable timing)
            float baseChargeTime = skillType switch
            {
                SkillType.Attack => CombatConstants.ATTACK_CHARGE_TIME,         // 0.0s (instant)
                SkillType.Windmill => CombatConstants.WINDMILL_CHARGE_TIME,     // 0.8s (fast)
                SkillType.Defense => CombatConstants.DEFENSE_CHARGE_TIME,       // 1.0s (quick)
                SkillType.Counter => CombatConstants.COUNTER_CHARGE_TIME,       // 1.0s (quick)
                SkillType.Lunge => CombatConstants.LUNGE_CHARGE_TIME,           // 1.5s (medium)
                SkillType.Smash => CombatConstants.SMASH_CHARGE_TIME,           // 2.0s (slow)
                SkillType.RangedAttack => CombatConstants.RANGED_ATTACK_CHARGE_TIME, // 0.0s (uses aiming)
                _ => CombatConstants.SMASH_CHARGE_TIME // Default fallback
            };

            // Apply dexterity modifier (faster charging with higher dex)
            float modifiedTime = baseChargeTime / (1 + skillSystem.Stats.dexterity / 10f);
            return modifiedTime;
        }

        /// <summary>
        /// Gets the stamina drain rate for the current skill while charging.
        /// </summary>
        private float GetChargingDrainRate()
        {
            return skillType switch
            {
                SkillType.Attack => CombatConstants.ATTACK_CHARGING_DRAIN,
                SkillType.Smash => CombatConstants.SMASH_CHARGING_DRAIN,
                SkillType.Counter => CombatConstants.COUNTER_CHARGING_DRAIN,
                SkillType.Windmill => CombatConstants.WINDMILL_CHARGING_DRAIN,
                SkillType.Defense => CombatConstants.COUNTER_CHARGING_DRAIN, // Same as Counter
                SkillType.Lunge => CombatConstants.SMASH_CHARGING_DRAIN,     // Same as Smash
                _ => 0f
            };
        }
    }
}
