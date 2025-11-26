using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Waiting state - defensive skills wait for incoming attacks.
    ///
    /// Lifecycle:
    /// - Entry: Notify CombatInteractionManager to add to waiting list
    /// - Update: Drain stamina per frame, check for depletion, no auto-transition
    /// - Exit: CRITICAL - Remove from CombatInteractionManager's waiting list
    ///
    /// Transitions:
    /// - External call to ForceTransitionToRecovery() → RecoveryState
    /// - Stamina depleted → RecoveryState (auto-cancel)
    /// - Manual cancel → UnchargedState
    ///
    /// Special Behavior:
    /// - Drains stamina continuously (Defense/Counter have different drain rates)
    /// - OnExit() guaranteed to clean up waitingDefensiveSkills list (fixes memory leaks!)
    /// - This state is externally controlled - CombatInteractionManager decides when to end it
    /// </summary>
    public class WaitingState : SkillStateBase
    {
        public WaitingState(SkillSystem system, SkillType type) : base(system, type)
        {
            // Validate this is actually a defensive skill
            if (!IsDefensiveSkill())
            {
                CombatLogger.LogSkill($"WaitingState created for non-defensive skill {type}!", CombatLogger.LogLevel.Error);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            CombatLogger.LogSkill($"<color=cyan>[STATE PATTERN] {skillSystem.gameObject.name} entered WaitingState for {skillType}</color>");

            // NOTE: CombatInteractionManager.ProcessSkillExecution() was already called in ActiveState
            // The SkillExecution is already in the waitingDefensiveSkills list
            // We don't need to add it again here
        }

        public override bool Update(float deltaTime)
        {
            // Drain stamina continuously while waiting
            float drainRate = skillType == SkillType.Defense
                ? CombatConstants.DEFENSE_STAMINA_DRAIN
                : CombatConstants.COUNTER_STAMINA_DRAIN;

            int staminaBefore = skillSystem.StaminaSystem.CurrentStamina;
            skillSystem.StaminaSystem.DrainStamina(drainRate, deltaTime);
            int staminaAfter = skillSystem.StaminaSystem.CurrentStamina;

            // Debug: Log stamina drain every second
            elapsedTime += deltaTime;
            if (Mathf.FloorToInt(elapsedTime) != Mathf.FloorToInt(elapsedTime - deltaTime))
            {
                CombatLogger.LogSkill($"[WaitingState] {skillSystem.gameObject.name} stamina: {staminaAfter} (drained {staminaBefore - staminaAfter} this frame, rate: {drainRate}/s)");
            }

            // CRITICAL FIX: Auto-cancel if stamina depleted (fixes memory leak bug #3)
            if (skillSystem.StaminaSystem.CurrentStamina <= 0)
            {
                CombatLogger.LogSkill($"<color=red>[STATE PATTERN] {skillSystem.gameObject.name} {skillType} auto-cancelled due to stamina depletion</color>");

                // Transition to Recovery (OnExit will clean up)
                skillSystem.StateMachine.TransitionTo(new RecoveryState(skillSystem, skillType));
                return false; // Don't use auto-transition, we manually triggered it
            }

            // No auto-transition - defensive skills wait for external trigger
            // CombatInteractionManager will call ForceTransitionToRecovery() when consumed
            return false;
        }

        public override void OnExit()
        {
            base.OnExit();

            CombatLogger.LogSkill($"<color=yellow>[STATE PATTERN] {skillSystem.gameObject.name} exiting WaitingState for {skillType} - CLEANUP STARTING</color>");

            // CRITICAL FIX: Reset movement modifier before exiting
            // Waiting state applies 0% movement speed (frozen), must reset to 1f when exiting
            // This prevents AI from being stuck slow after knockback interrupts defensive skills
            skillSystem.MovementController.SetMovementModifier(1f);

            // CRITICAL FIX: Remove from CombatInteractionManager's waiting list
            // This fixes ALL memory leak bugs:
            // - Manual cancellation (OnExit always runs)
            // - Stamina depletion (we just handled above)
            // - Timeout cleanup (handled by CombatInteractionManager)
            // - Attack hits (CombatInteractionManager calls ForceTransitionToRecovery)

            if (CombatInteractionManager.Instance != null)
            {
                // New method we'll add to CombatInteractionManager
                CombatInteractionManager.Instance.RemoveWaitingDefensiveSkill(skillSystem);
            }

            CombatLogger.LogSkill($"<color=green>[STATE PATTERN] {skillSystem.gameObject.name} WaitingState cleanup COMPLETE</color>");
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Waiting;
        }

        public override ISkillState GetNextState()
        {
            // No auto-transition from Waiting state
            // External systems call ForceTransitionToRecovery() or CancelSkill()
            return null;
        }
    }
}
