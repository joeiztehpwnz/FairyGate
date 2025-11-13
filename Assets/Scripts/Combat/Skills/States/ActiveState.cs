using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Active state - skill is being executed (uncancellable phase).
    ///
    /// Lifecycle:
    /// - Entry: Process skill execution (damage, interaction)
    /// - Update: Count down active time (offensive) or immediate transition (defensive)
    /// - Exit: Minimal cleanup
    ///
    /// Transitions:
    /// - Defensive skills: Immediate → WaitingState
    /// - Offensive skills: elapsed >= activeTime → RecoveryState
    ///
    /// Special Behavior:
    /// - This phase is uncancellable
    /// - Calls ProcessSkillExecution() which triggers damage/interactions
    /// - Defensive skills skip the timer and go straight to Waiting
    /// </summary>
    public class ActiveState : SkillStateBase
    {
        private float activeTime;
        private bool skillSuccessful;

        public ActiveState(SkillSystem system, SkillType type) : base(system, type)
        {
            // RangedAttack has brief active time for interaction processing
            if (type == SkillType.RangedAttack)
            {
                activeTime = 0.1f; // Brief time as per coroutine implementation
            }
            else
            {
                // Calculate active time for other offensive skills
                activeTime = SpeedResolver.CalculateExecutionTime(
                    type,
                    system.WeaponController.WeaponData,
                    SkillExecutionState.Active
                );
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Process skill effect during active phase
            skillSuccessful = ProcessSkillExecution();

            if (skillSystem.EnableDebugLogs)
            {
                if (IsDefensiveSkill())
                {
                    Debug.Log($"{skillSystem.gameObject.name} {skillType} activated - entering Waiting state");
                }
                else
                {
                    Debug.Log($"{skillSystem.gameObject.name} {skillType} active (duration: {activeTime:F2}s, success: {skillSuccessful})");
                }
            }
        }

        public override bool Update(float deltaTime)
        {
            // Defensive skills immediately transition to Waiting state
            if (IsDefensiveSkill())
            {
                return true; // Trigger immediate transition
            }

            // Offensive skills have fixed active time
            elapsedTime += deltaTime;

            if (elapsedTime >= activeTime)
            {
                return true; // Trigger transition to Recovery
            }

            return false; // Continue active phase
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override SkillExecutionState GetStateType()
        {
            return SkillExecutionState.Active;
        }

        public override ISkillState GetNextState()
        {
            // Branch based on skill type
            if (IsDefensiveSkill())
            {
                // Defensive skills go to Waiting state (Phase 3: Implemented!)
                return new WaitingState(skillSystem, skillType);
            }
            else
            {
                // Offensive skills go to Recovery
                return new RecoveryState(skillSystem, skillType);
            }
        }

        /// <summary>
        /// Processes skill execution logic (damage, range checks, interactions).
        /// Matches original ProcessSkillExecution() from SkillSystem.
        /// </summary>
        private bool ProcessSkillExecution()
        {
            // RANGED ATTACK: Roll hit chance, draw visual trail
            if (skillType == SkillType.RangedAttack)
            {
                // Roll hit chance based on accuracy (BEFORE stopping aim)
                bool isHit = skillSystem.AccuracySystem != null ? skillSystem.AccuracySystem.RollHitChance() : false;
                skillSystem.LastRangedAttackHit = isHit;

                if (skillSystem.EnableDebugLogs)
                {
                    float accuracy = skillSystem.AccuracySystem != null ? skillSystem.AccuracySystem.CurrentAccuracy : 0f;
                    Debug.Log($"{skillSystem.gameObject.name} fired RangedAttack at {accuracy:F1}% accuracy → {(isHit ? "HIT" : "MISS")}");
                }

                // Stop accuracy tracking AFTER hit roll (fixes state machine timing bug)
                if (skillSystem.AccuracySystem != null)
                {
                    skillSystem.AccuracySystem.StopAiming();
                }

                // Process interaction (even on miss, for defensive skill responses)
                CombatInteractionManager.Instance?.ProcessSkillExecution(skillSystem, skillType);

                // Draw visual trail
                if (skillSystem.CombatController.CurrentTarget != null)
                {
                    if (isHit)
                    {
                        // HIT: Show hit trail
                        Vector3 hitPosition = skillSystem.CombatController.CurrentTarget.position + Vector3.up * 1f;
                        skillSystem.DrawRangedAttackTrail(skillSystem.transform.position, hitPosition, true);
                    }
                    else
                    {
                        // MISS: Show miss trail
                        Vector3 missPosition = skillSystem.AccuracySystem != null
                            ? skillSystem.AccuracySystem.CalculateMissPosition()
                            : skillSystem.CombatController.CurrentTarget.position;

                        skillSystem.DrawRangedAttackTrail(skillSystem.transform.position, missPosition, false);
                    }
                }

                return true;
            }

            // LUNGE: Perform dash movement toward target
            if (skillType == SkillType.Lunge)
            {
                if (skillSystem.CombatController.CurrentTarget != null)
                {
                    Vector3 dashDirection = (skillSystem.CombatController.CurrentTarget.position - skillSystem.transform.position).normalized;
                    dashDirection.y = 0f; // Keep horizontal

                    var characterController = skillSystem.GetComponent<CharacterController>();
                    if (characterController != null)
                    {
                        Vector3 dashMovement = dashDirection * CombatConstants.LUNGE_DASH_DISTANCE;
                        characterController.Move(dashMovement);

                        if (skillSystem.EnableDebugLogs)
                        {
                            Debug.Log($"{skillSystem.gameObject.name} lunged forward {CombatConstants.LUNGE_DASH_DISTANCE} units toward {skillSystem.CombatController.CurrentTarget.name}");
                        }
                    }
                }

                // Trigger combat interaction
                CombatInteractionManager.Instance?.ProcessSkillExecution(skillSystem, skillType);
                return true;
            }

            // Check range for other offensive skills
            if (IsOffensiveSkill())
            {
                if (skillSystem.CombatController.CurrentTarget == null)
                {
                    if (skillSystem.EnableDebugLogs)
                    {
                        Debug.Log($"{skillSystem.gameObject.name} {skillType} failed: no target");
                    }
                    return false;
                }

                if (!skillSystem.WeaponController.CheckRangeForSkill(skillSystem.CombatController.CurrentTarget, skillType))
                {
                    if (skillSystem.EnableDebugLogs)
                    {
                        Debug.Log($"{skillSystem.gameObject.name} {skillType} failed: target out of range");
                    }
                    return false;
                }
            }

            // Trigger combat interaction system
            CombatInteractionManager.Instance?.ProcessSkillExecution(skillSystem, skillType);

            return true;
        }
    }
}
