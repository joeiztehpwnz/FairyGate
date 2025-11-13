using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Base class for all skill states, providing common functionality.
    /// Inherit from this to create specific state implementations.
    /// </summary>
    public abstract class SkillStateBase : ISkillState
    {
        protected SkillSystem skillSystem;
        protected SkillType skillType;
        protected float elapsedTime;

        public SkillStateBase(SkillSystem system, SkillType type)
        {
            skillSystem = system;
            skillType = type;
            elapsedTime = 0f;
        }

        /// <summary>
        /// Default OnEnter behavior: Reset timer, update enum state, apply movement restrictions.
        /// Override to add state-specific initialization.
        /// </summary>
        public virtual void OnEnter()
        {
            elapsedTime = 0f;

            // Update the enum-based currentState for backward compatibility
            skillSystem.SetState(GetStateType());

            // Apply movement restrictions based on state
            skillSystem.MovementController.ApplySkillMovementRestriction(skillType, GetStateType());

            if (skillSystem.EnableDebugLogs)
            {
                Debug.Log($"{skillSystem.gameObject.name} entered {GetType().Name} for {skillType}");
            }
        }

        /// <summary>
        /// Abstract Update method - each state must implement its own logic.
        /// Returns true when ready to transition to next state.
        /// </summary>
        public abstract bool Update(float deltaTime);

        /// <summary>
        /// Default OnExit behavior: Log transition.
        /// Override to add state-specific cleanup (CRITICAL for defensive skills!).
        /// </summary>
        public virtual void OnExit()
        {
            if (skillSystem.EnableDebugLogs)
            {
                Debug.Log($"{skillSystem.gameObject.name} exiting {GetType().Name}");
            }
        }

        /// <summary>
        /// Returns the SkillExecutionState enum for backward compatibility.
        /// </summary>
        public abstract SkillExecutionState GetStateType();

        /// <summary>
        /// Returns the next state to transition to.
        /// Return null for no auto-transition (e.g., waiting for input).
        /// </summary>
        public abstract ISkillState GetNextState();

        /// <summary>
        /// Returns the current skill type.
        /// </summary>
        public SkillType GetSkillType()
        {
            return skillType;
        }

        /// <summary>
        /// Helper method to check if a skill is defensive.
        /// </summary>
        protected bool IsDefensiveSkill()
        {
            return SpeedResolver.IsDefensiveSkill(skillType);
        }

        /// <summary>
        /// Helper method to check if a skill is offensive.
        /// </summary>
        protected bool IsOffensiveSkill()
        {
            return SpeedResolver.IsOffensiveSkill(skillType);
        }
    }
}
