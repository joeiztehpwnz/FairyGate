using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Uncharged state - the default idle state when no skill is being executed.
    ///
    /// Lifecycle:
    /// - Entry: Reset all skill-related flags and movement restrictions
    /// - Update: No timer, waits for manual transition (StartCharging, StartAiming)
    /// - Exit: Minimal cleanup
    ///
    /// Transitions:
    /// - StartCharging() → ChargingState
    /// - StartAiming() → AimingState (for RangedAttack)
    /// </summary>
    public class UnchargedState : SkillStateBase
    {
        public UnchargedState(SkillSystem system, SkillType type) : base(system, type)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Reset charge progress
            skillSystem.ChargeProgress = 0f;

            // Reset movement restrictions (allow full movement)
            skillSystem.MovementController.SetMovementModifier(1f);

            // Reset current skill to default (Attack)
            skillSystem.CurrentSkill = SkillType.Attack;
        }

        public override bool Update(float deltaTime)
        {
            // Uncharged state is idle - no auto-transition
            // Waits for player input (StartCharging or StartAiming)
            return false;
        }

        public override void OnExit()
        {
            // Reset movement modifier to prevent stuck states
            skillSystem.MovementController.SetMovementModifier(1.0f);

            base.OnExit();
            // Minimal cleanup needed - just transitioning to active state
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Uncharged;
        }

        public override ISkillState GetNextState()
        {
            // No auto-transition from Uncharged
            // Manual transitions via StartCharging/StartAiming
            return null;
        }
    }
}
