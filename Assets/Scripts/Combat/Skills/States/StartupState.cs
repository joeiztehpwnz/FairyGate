using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Startup state - brief windup before skill activates.
    ///
    /// Lifecycle:
    /// - Entry: Apply movement restriction, start timer
    /// - Update: Count down startup time
    /// - Exit: Minimal cleanup
    ///
    /// Transitions:
    /// - elapsed >= startupTime → ActiveState (auto-transition)
    ///
    /// Special Behavior:
    /// - Character is immobilized during startup (movement = 0)
    /// - Duration varies by skill and weapon
    /// </summary>
    public class StartupState : SkillStateBase
    {
        private float startupTime;

        public StartupState(SkillSystem system, SkillType type) : base(system, type)
        {
            // Calculate startup time based on skill and weapon
            startupTime = SpeedResolver.CalculateExecutionTime(
                type,
                system.WeaponController.WeaponData,
                SkillExecutionState.Startup
            );
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (skillSystem.EnableDebugLogs)
            {
                CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} startup (duration: {startupTime:F2}s)");
            }
        }

        public override bool Update(float deltaTime)
        {
            elapsedTime += deltaTime;

            // Check if startup complete
            if (elapsedTime >= startupTime)
            {
                return true; // Trigger transition to Active
            }

            return false; // Continue startup
        }

        public override void OnExit()
        {
            // Reset movement modifier to prevent stuck states
            skillSystem.MovementController.SetMovementModifier(1.0f);

            base.OnExit();
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Startup;
        }

        public override ISkillState GetNextState()
        {
            // Transition to Active state when startup completes
            return new ActiveState(skillSystem, skillType);
        }
    }
}
