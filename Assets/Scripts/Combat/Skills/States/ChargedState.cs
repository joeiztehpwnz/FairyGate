using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Charged state - skill is fully charged and ready to execute.
    ///
    /// Lifecycle:
    /// - Entry: Apply movement restrictions for charged state
    /// - Update: Wait for manual execution (or auto-execute for defensive skills)
    /// - Exit: Minimal cleanup
    ///
    /// Transitions:
    /// - Offensive skills: Manual transition via ExecuteSkill() → StartupState
    /// - Defensive skills: Immediate auto-transition → StartupState
    ///
    /// Special Behavior:
    /// - Defensive skills (Defense, Counter) auto-execute immediately
    /// - Offensive skills wait for player input
    /// </summary>
    public class ChargedState : SkillStateBase
    {
        public ChargedState(SkillSystem system, SkillType type) : base(system, type)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (skillSystem.EnableDebugLogs)
            {
                if (IsDefensiveSkill())
                {
                    Debug.Log($"{skillSystem.gameObject.name} {skillType} charged - auto-executing");
                }
                else
                {
                    Debug.Log($"{skillSystem.gameObject.name} {skillType} charged - waiting for execute input");
                }
            }
        }

        public override bool Update(float deltaTime)
        {
            // Defensive skills auto-execute immediately
            if (IsDefensiveSkill())
            {
                // Transition to Startup on next frame
                return true;
            }

            // Offensive skills wait for manual ExecuteSkill() call
            // No auto-transition
            return false;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Charged;
        }

        public override ISkillState GetNextState()
        {
            // Both offensive and defensive skills go to Startup
            // (RangedAttack is handled differently - see ExecuteSkill)
            return new StartupState(skillSystem, skillType);
        }
    }
}
