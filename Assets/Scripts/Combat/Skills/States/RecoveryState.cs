using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Recovery state - post-execution cooldown before returning to idle.
    ///
    /// Lifecycle:
    /// - Entry: Apply movement restriction, start timer
    /// - Update: Count down recovery time
    /// - Exit: Fire OnSkillExecuted event, reset to idle state
    ///
    /// Transitions:
    /// - elapsed >= recoveryTime → UnchargedState (auto-transition)
    ///
    /// Special Behavior:
    /// - Character is immobilized during recovery (movement = 0)
    /// - Duration varies by skill and weapon
    /// - OnExit fires completion event for UI/feedback
    /// </summary>
    public class RecoveryState : SkillStateBase
    {
        private float recoveryTime;

        public RecoveryState(SkillSystem system, SkillType type) : base(system, type)
        {
            // RangedAttack has custom recovery time calculation
            if (type == SkillType.RangedAttack)
            {
                // Use constant recovery time scaled by weapon speed
                float baseRecoveryTime = CombatConstants.RANGED_ATTACK_RECOVERY_TIME;

                if (system.WeaponController.WeaponData != null)
                {
                    recoveryTime = baseRecoveryTime / system.WeaponController.WeaponData.speed;
                }
                else
                {
                    recoveryTime = baseRecoveryTime;
                }
            }
            else
            {
                // Calculate recovery time for other skills
                recoveryTime = SpeedResolver.CalculateExecutionTime(
                    type,
                    system.WeaponController.WeaponData,
                    SkillExecutionState.Recovery
                );
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (skillSystem.EnableDebugLogs)
            {
                Debug.Log($"{skillSystem.gameObject.name} {skillType} recovery (duration: {recoveryTime:F2}s)");
            }
        }

        public override bool Update(float deltaTime)
        {
            elapsedTime += deltaTime;

            // Check if recovery complete
            if (elapsedTime >= recoveryTime)
            {
                return true; // Trigger transition to Uncharged
            }

            return false; // Continue recovery
        }

        public override void OnExit()
        {
            base.OnExit();

            // Fire completion event
            // For RangedAttack, use stored hit result; for others, assume success
            bool wasSuccessful = (skillType == SkillType.RangedAttack)
                ? skillSystem.LastRangedAttackHit
                : true;

            skillSystem.TriggerSkillExecuted(skillType, wasSuccessful);

            if (skillSystem.EnableDebugLogs)
            {
                Debug.Log($"{skillSystem.gameObject.name} {skillType} execution complete (success: {wasSuccessful})");
            }
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Recovery;
        }

        public override ISkillState GetNextState()
        {
            // Return to Uncharged (idle) state
            return new UnchargedState(skillSystem, SkillType.Attack);
        }
    }
}
