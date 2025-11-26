using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Orchestrates skill state transitions using the state pattern.
    /// Replaces coroutine-based skill execution with a deterministic state machine.
    ///
    /// Key Benefits:
    /// - Guaranteed cleanup (OnExit always runs)
    /// - Debuggable (inspect current state in inspector)
    /// - Testable (can mock states)
    /// - No synchronization bugs (single source of truth)
    /// </summary>
    public class SkillStateMachine
    {
        private ISkillState currentState;
        private readonly SkillSystem skillSystem;

        public ISkillState CurrentState => currentState;

        public SkillStateMachine(SkillSystem system)
        {
            skillSystem = system;
            // Start in Uncharged state
            currentState = new UnchargedState(system, SkillType.Attack);
        }

        /// <summary>
        /// Called every frame from SkillSystem.Update().
        /// Updates current state and handles auto-transitions.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (currentState == null) return;

            // Update current state
            bool shouldTransition = currentState.Update(deltaTime);

            // Handle auto-transition if requested
            if (shouldTransition)
            {
                ISkillState nextState = currentState.GetNextState();
                if (nextState != null)
                {
                    TransitionTo(nextState);
                }
                else if (skillSystem.EnableDebugLogs)
                {
                    CombatLogger.LogSkill($"{skillSystem.gameObject.name}: State {currentState.GetType().Name} " +
                                   $"requested transition but GetNextState() returned null", CombatLogger.LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Transitions to a new state, running OnExit/OnEnter lifecycle hooks.
        /// This is the ONLY way states should transition (guarantees cleanup).
        /// </summary>
        public void TransitionTo(ISkillState newState)
        {
            if (newState == null)
            {
                CombatLogger.LogSkill($"{skillSystem.gameObject.name}: Cannot transition to null state", CombatLogger.LogLevel.Error);
                return;
            }

            if (skillSystem.EnableDebugLogs)
            {
                string currentStateName = currentState != null ? currentState.GetType().Name : "null";
                CombatLogger.LogSkill($"{skillSystem.gameObject.name}: Transitioning from {currentStateName} to {newState.GetType().Name}");
            }

            // Exit current state (guaranteed cleanup!)
            currentState?.OnExit();

            // Enter new state
            currentState = newState;
            currentState.OnEnter();
        }

        /// <summary>
        /// Forces an immediate transition to a new state.
        /// Used by external systems (e.g., CombatInteractionManager calling ForceTransitionToRecovery).
        /// </summary>
        public void ForceTransitionTo(ISkillState newState)
        {
            TransitionTo(newState);
        }

        /// <summary>
        /// Returns the current state's enum type for backward compatibility.
        /// </summary>
        public SkillExecutionState GetCurrentStateType()
        {
            return currentState?.GetStateType() ?? SkillExecutionState.Uncharged;
        }

        /// <summary>
        /// Returns the current state's skill type.
        /// </summary>
        public SkillType GetCurrentSkillType()
        {
            return currentState?.GetSkillType() ?? SkillType.Attack;
        }

        /// <summary>
        /// Debug method to get current state name for inspector/GUI.
        /// </summary>
        public string GetCurrentStateName()
        {
            return currentState != null ? currentState.GetType().Name : "null";
        }
    }
}
