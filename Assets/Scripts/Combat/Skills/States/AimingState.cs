using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Aiming state - ranged attack accuracy building phase.
    ///
    /// Lifecycle:
    /// - Entry: Start accuracy tracking, apply movement restriction
    /// - Update: No auto-transition (player controls when to fire)
    /// - Exit: Stop accuracy tracking
    ///
    /// Transitions:
    /// - ExecuteSkill() called → StartupState (then Active for firing)
    /// - Manual cancel → UnchargedState
    ///
    /// Special Behavior:
    /// - Player-controlled timing (no auto-transition)
    /// - Accuracy builds over time via AccuracySystem
    /// - Can be cancelled at any time (refunds stamina if not fired)
    /// </summary>
    public class AimingState : SkillStateBase
    {
        public AimingState(SkillSystem system, SkillType type) : base(system, type)
        {
            // Validate this is actually a ranged skill
            if (type != SkillType.RangedAttack)
            {
                CombatLogger.LogSkill($"AimingState created for non-ranged skill {type}!", CombatLogger.LogLevel.Error);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            CombatLogger.LogSkill($"<color=cyan>[STATE PATTERN] {skillSystem.gameObject.name} entered AimingState for {skillType}</color>");

            // Start accuracy tracking
            if (skillSystem.AccuracySystem != null)
            {
                skillSystem.AccuracySystem.StartAiming(skillSystem.CombatController.CurrentTarget);
            }

            if (skillSystem.EnableDebugLogs)
            {
                CombatLogger.LogSkill($"{skillSystem.gameObject.name} started aiming {skillType}");
            }
        }

        public override bool Update(float deltaTime)
        {
            // No auto-transition - player controls when to fire via ExecuteSkill()
            // AI controls this via SimpleTestAI.AutoFireWhenReady() coroutine

            // Check for target loss (auto-cancel)
            if (skillSystem.CombatController.CurrentTarget == null)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} aiming cancelled: target lost");
                }
                skillSystem.StateMachine.TransitionTo(new UnchargedState(skillSystem, SkillType.Attack));
                return false;
            }

            // Check for range loss (auto-cancel)
            float weaponRange = skillSystem.WeaponController != null
                ? skillSystem.WeaponController.GetRangedRange()
                : CombatConstants.RANGED_ATTACK_BASE_RANGE;

            float distanceToTarget = Vector3.Distance(
                skillSystem.transform.position,
                skillSystem.CombatController.CurrentTarget.position
            );

            if (distanceToTarget > weaponRange)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} aiming cancelled: target out of range ({distanceToTarget:F1} > {weaponRange})");
                }
                skillSystem.StateMachine.TransitionTo(new UnchargedState(skillSystem, SkillType.Attack));
                return false;
            }

            // Drain stamina while aiming (punishing stamina management)
            if (skillSystem.StaminaSystem != null)
            {
                skillSystem.StaminaSystem.DrainStamina(CombatConstants.RANGED_AIMING_DRAIN, deltaTime);

                // Auto-cancel if stamina depleted
                if (skillSystem.StaminaSystem.CurrentStamina <= 0)
                {
                    if (skillSystem.EnableDebugLogs)
                    {
                        CombatLogger.LogSkill($"{skillSystem.gameObject.name} aiming cancelled - stamina depleted!");
                    }
                    skillSystem.CancelSkill();
                    return false;
                }
            }

            return false; // No auto-transition
        }

        public override void OnExit()
        {
            // Reset movement modifier to prevent stuck states
            skillSystem.MovementController.SetMovementModifier(1.0f);

            base.OnExit();

            CombatLogger.LogSkill($"<color=yellow>[STATE PATTERN] {skillSystem.gameObject.name} exiting AimingState</color>");

            // NOTE: Don't call StopAiming() here - accuracy needs to persist for hit roll
            // ActiveState will call StopAiming() AFTER rolling hit chance
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Aiming;
        }

        public override ISkillState GetNextState()
        {
            // No auto-transition - external call to ExecuteSkill() handles transition
            return null;
        }
    }
}
