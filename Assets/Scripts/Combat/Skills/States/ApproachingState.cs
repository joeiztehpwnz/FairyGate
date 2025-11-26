using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Approaching state - auto-moving toward target after skill is charged.
    ///
    /// Lifecycle:
    /// - Entry: Start moving toward target
    /// - Update: Continue approaching, check distance, execute when in range
    /// - Exit: Clear movement override
    ///
    /// Transitions:
    /// - In range → StartupState (execute skill)
    /// - Target lost/dead → UnchargedState (cancel)
    /// - Manual cancel (Space) → UnchargedState
    ///
    /// Special Behavior:
    /// - Player can manually adjust movement while approaching
    /// - Auto-executes when reaching weapon range
    /// - Only used for offensive skills executed while out of range
    /// </summary>
    public class ApproachingState : SkillStateBase
    {
        private Transform target;
        private float requiredRange;
        private bool shouldTransition = false;

        public ApproachingState(SkillSystem system, SkillType type) : base(system, type)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Get target and required range
            target = skillSystem.CombatController?.CurrentTarget;
            requiredRange = skillSystem.WeaponController.GetSkillRange(skillType);

            if (target == null)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} entered ApproachingState with no target - will cancel", CombatLogger.LogLevel.Warning);
                }
                shouldTransition = true;
                return;
            }

            if (skillSystem.EnableDebugLogs)
            {
                float currentDistance = Vector3.Distance(skillSystem.transform.position, target.position);
                CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} approaching target (current: {currentDistance:F1}m, required: {requiredRange:F1}m)");
            }
        }

        public override bool Update(float deltaTime)
        {
            elapsedTime += deltaTime;

            // Check if we should cancel (target lost)
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} approach cancelled - target lost");
                }
                shouldTransition = true;
                return true; // Transition to UnchargedState
            }

            // Check if target is dead
            var targetHealth = target.GetComponent<HealthSystem>();
            if (targetHealth != null && !targetHealth.IsAlive)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} approach cancelled - target dead");
                }
                shouldTransition = true;
                return true; // Transition to UnchargedState
            }

            // Calculate direction to target
            Vector3 toTarget = (target.position - skillSystem.transform.position);
            toTarget.y = 0f; // Keep horizontal
            float distance = toTarget.magnitude;

            // Check if in range
            if (distance <= requiredRange)
            {
                if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} reached target range ({distance:F1}m <= {requiredRange:F1}m) - executing");
                }
                shouldTransition = false; // We want to execute, not cancel
                return true; // Transition to StartupState
            }

            // Continue moving toward target
            Vector3 direction = toTarget.normalized;
            skillSystem.MovementController.SetMovementInput(direction);

            // No auto-transition yet
            return false;
        }

        public override void OnExit()
        {
            // Reset movement modifier to prevent stuck states
            skillSystem.MovementController.SetMovementModifier(1.0f);

            base.OnExit();

            // Clear movement override
            skillSystem.MovementController.SetMovementInput(Vector3.zero);

            if (skillSystem.EnableDebugLogs)
            {
                CombatLogger.LogSkill($"{skillSystem.gameObject.name} {skillType} exiting ApproachingState");
            }
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Approaching;
        }

        public override ISkillState GetNextState()
        {
            // If shouldTransition is true, we're cancelling (target lost)
            if (shouldTransition)
            {
                return new UnchargedState(skillSystem, skillType);
            }

            // Otherwise, we reached range and should execute
            // Handle RangedAttack separately - needs aiming state
            if (skillType == SkillType.RangedAttack)
            {
                return new AimingState(skillSystem, skillType);
            }

            // All other offensive skills go to startup
            return new StartupState(skillSystem, skillType);
        }
    }
}
