namespace FairyGate.Combat
{
    /// <summary>
    /// Interface for all skill execution states in the state pattern.
    /// Each state manages its own lifecycle and transitions.
    /// </summary>
    public interface ISkillState
    {
        /// <summary>
        /// Called once when entering this state.
        /// Use this for initialization and setup.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called every frame from SkillSystem.Update().
        /// Returns true if the state should auto-transition to the next state.
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        /// <returns>True if ready to transition, false otherwise</returns>
        bool Update(float deltaTime);

        /// <summary>
        /// Called once when exiting this state.
        /// CRITICAL: This is where cleanup happens! Guaranteed to run on every transition.
        /// Use this to remove from waiting lists, reset flags, notify managers, etc.
        /// </summary>
        void OnExit();

        /// <summary>
        /// Returns the SkillExecutionState enum for this state.
        /// Used for backward compatibility with existing code that checks currentState enum.
        /// </summary>
        /// <returns>The enum representation of this state</returns>
        SkillExecutionState GetStateType();

        /// <summary>
        /// Returns the next state to transition to when Update() returns true.
        /// Return null if no auto-transition should occur (e.g., waiting for manual input).
        /// </summary>
        /// <returns>Next state instance, or null for no auto-transition</returns>
        ISkillState GetNextState();

        /// <summary>
        /// Returns the skill type associated with this state execution.
        /// </summary>
        /// <returns>Current skill type</returns>
        SkillType GetSkillType();
    }
}
