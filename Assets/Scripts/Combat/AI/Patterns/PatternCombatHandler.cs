using UnityEngine;
using System.Collections;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles skill execution and combat interactions for pattern-based AI.
    /// Manages skill timing, coordination with AICoordinator, and combat engagement/disengagement.
    ///
    /// Classic Mabinogi Design: Combat is turn-based and deliberate, with clear skill execution
    /// timing and coordination between multiple enemies.
    /// </summary>
    public class PatternCombatHandler
    {
        private readonly MonoBehaviour owner;
        private readonly SkillSystem skillSystem;
        private readonly CombatController combatController;
        private readonly WeaponController weaponController;
        private readonly Transform transform;
        private readonly bool enableDebugLogs;
        private readonly TelegraphSystem telegraphSystem;

        // Combat configuration
        private readonly float engageDistance;
        private readonly bool useCoordination;

        // State
        private bool hasAttackSlot = false;
        private AICoordinator coordinator;

        public bool HasAttackSlot => hasAttackSlot;

        public PatternCombatHandler(
            MonoBehaviour owner,
            SkillSystem skillSystem,
            CombatController combatController,
            WeaponController weaponController,
            Transform transform,
            float engageDistance,
            bool useCoordination,
            bool enableDebugLogs = false)
        {
            this.owner = owner;
            this.skillSystem = skillSystem;
            this.combatController = combatController;
            this.weaponController = weaponController;
            this.transform = transform;
            this.engageDistance = engageDistance;
            this.useCoordination = useCoordination;
            this.enableDebugLogs = enableDebugLogs;

            // Get telegraph system component
            telegraphSystem = owner.GetComponent<TelegraphSystem>();

            // Initialize coordinator reference
            coordinator = AICoordinator.Instance;
        }

        /// <summary>
        /// Initializes the combat handler. Should be called during Start().
        /// </summary>
        public void Initialize()
        {
            // Subscribe to skill completion events to release attack slots promptly
            if (skillSystem != null)
            {
                skillSystem.OnSkillExecuted += OnSkillCompleted;
                skillSystem.OnSkillCancelled += OnSkillCancelled;
            }
        }

        /// <summary>
        /// Called when a skill completes execution - releases attack slot immediately.
        /// </summary>
        private void OnSkillCompleted(SkillType skillType, bool wasSuccessful)
        {
            if (hasAttackSlot && coordinator != null)
            {
                // Release attack slot immediately instead of waiting for timeout
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} skill {skillType} completed - releasing attack slot");
                }

                // Note: We can't release here directly because we don't have IAIAgent reference
                // PatternExecutor will call ReleaseAttackSlot from Cleanup
                // For now, just flag that we should release
                // This will be handled by checking skillSystem.CurrentState == Uncharged in Update
            }
        }

        /// <summary>
        /// Called when a skill is cancelled - releases attack slot immediately.
        /// </summary>
        private void OnSkillCancelled(SkillType skillType)
        {
            if (hasAttackSlot && coordinator != null)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} skill {skillType} cancelled - will release attack slot");
                }
            }
        }

        /// <summary>
        /// Registers this AI with the coordinator if coordination is enabled.
        /// </summary>
        public void RegisterWithCoordinator(IAIAgent agent)
        {
            if (useCoordination && coordinator != null)
            {
                coordinator.RegisterEnemy(agent);
                if (enableDebugLogs)
                {
                    CombatLogger.LogAI($"[PatternCombatHandler] {transform.name} registered with AICoordinator");
                }
            }
        }

        /// <summary>
        /// Starts charging a skill for pattern-controlled execution.
        /// Shows telegraph if provided, then begins charging.
        /// </summary>
        public void StartChargingSkill(SkillType skillType, IAIAgent agent, TelegraphData telegraph = null)
        {
            // Request attack permission if coordination enabled
            if (useCoordination && coordinator != null && !hasAttackSlot)
            {
                hasAttackSlot = coordinator.RequestAttackPermission(agent);
                if (enableDebugLogs && hasAttackSlot)
                {
                    CombatLogger.LogAttack($"[PatternCombatHandler] {transform.name} received attack permission from coordinator");
                }
            }

            // Check if can charge skill
            if (!skillSystem.CanChargeSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} cannot charge {skillType} (insufficient stamina or wrong state)");
                }
                return;
            }

            // Show telegraph if provided
            if (telegraph != null && telegraphSystem != null)
            {
                telegraphSystem.ShowTelegraph(telegraph, skillType);

                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"[PatternCombatHandler] {transform.name} showing telegraph for {skillType}");
                }

                // Telegraph system handles its own timing/display
                // We proceed immediately - the telegraph displays async
            }

            // Start charging/aiming based on skill type
            if (skillType == SkillType.RangedAttack)
            {
                skillSystem.StartAiming(skillType);
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} started aiming {skillType}");
                }
            }
            else
            {
                skillSystem.StartCharging(skillType);
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} started charging {skillType}");
                }
            }
        }

        /// <summary>
        /// Executes a charged skill if ready (pattern-controlled timing).
        /// </summary>
        public void ExecuteReadySkill(SkillType skillType)
        {
            // Verify skill is ready
            if (!IsSkillCharged(skillType))
            {
                return;
            }

            // Execute the skill
            skillSystem.ExecuteSkill(skillType);

            if (enableDebugLogs)
            {
                if (skillType == SkillType.RangedAttack)
                {
                    float accuracy = skillSystem.AccuracySystem?.CurrentAccuracy ?? 0f;
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} executed {skillType} at {accuracy:F1}% accuracy");
                }
                else
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} executed {skillType}");
                }
            }
        }

        /// <summary>
        /// Checks if a skill is charged and ready to execute.
        /// Uses pattern node configuration for ranged attack thresholds.
        /// </summary>
        public bool IsSkillCharged(SkillType skillType, float accuracyThreshold = 60f)
        {
            // For ranged attack, check if accuracy threshold reached
            if (skillType == SkillType.RangedAttack)
            {
                bool isAiming = skillSystem.CurrentState == SkillExecutionState.Aiming &&
                               skillSystem.CurrentSkill == skillType;
                bool hasAccuracy = skillSystem.AccuracySystem != null &&
                                  skillSystem.AccuracySystem.CurrentAccuracy >= accuracyThreshold;

                // Debug: Log why skill is not ready
                if (enableDebugLogs && !isAiming)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} RangedAttack not ready - wrong state: {skillSystem.CurrentState} (expected Aiming)");
                }
                else if (enableDebugLogs && !hasAccuracy)
                {
                    float currentAccuracy = skillSystem.AccuracySystem?.CurrentAccuracy ?? 0f;
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} RangedAttack not ready - accuracy {currentAccuracy:F1}% < threshold {accuracyThreshold:F1}%");
                }

                return isAiming && hasAccuracy;
            }

            // For other skills, check if charged
            bool isCharged = skillSystem.CurrentState == SkillExecutionState.Charged &&
                            skillSystem.CurrentSkill == skillType;

            if (enableDebugLogs && !isCharged)
            {
                CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} {skillType} not ready - state: {skillSystem.CurrentState} (expected Charged)");
            }

            return isCharged;
        }

        /// <summary>
        /// Updates combat engagement/disengagement based on distance to target.
        /// Returns true if combat was just entered this frame (useful for triggering initial charging).
        /// </summary>
        public bool UpdateCombatEngagement(Transform targetPlayer, float distanceToPlayer)
        {
            if (targetPlayer == null || combatController == null)
                return false;

            float sqrDistance = distanceToPlayer * distanceToPlayer;
            float sqrEngageDistance = engageDistance * engageDistance;
            float sqrDisengageDistance = (engageDistance * 1.5f) * (engageDistance * 1.5f);

            bool justEnteredCombat = false;

            // Enter combat if player within engage distance
            if (!combatController.IsInCombat && sqrDistance <= sqrEngageDistance)
            {
                combatController.EnterCombat(targetPlayer);
                justEnteredCombat = true;

                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"[PatternCombatHandler] {transform.name} engaged {targetPlayer.name} in combat");
                }
            }

            // Exit combat if player too far
            if (combatController.IsInCombat && sqrDistance > sqrDisengageDistance)
            {
                combatController.ExitCombat();

                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"[PatternCombatHandler] {transform.name} disengaged from combat (too far)");
                }
            }

            return justEnteredCombat;
        }

        /// <summary>
        /// Cleanup state. Should be called on disable/destroy.
        /// </summary>
        public void Cleanup(IAIAgent agent)
        {
            ReleaseAttackSlot(agent);

            // Clear any tracked executions
            var combatController = owner.GetComponent<CombatController>();
            if (combatController != null)
            {
                SkillExecutionTracker.Instance.ClearCombatant(combatController);
            }

            // Unregister from coordinator
            if (coordinator != null)
            {
                coordinator.UnregisterEnemy(agent);
                if (enableDebugLogs)
                {
                    CombatLogger.LogAI($"[PatternCombatHandler] {transform.name} unregistered from AICoordinator");
                }
            }
        }

        /// <summary>
        /// Checks if skill has completed and releases attack slot if held.
        /// Should be called every frame from PatternExecutor.Update().
        /// </summary>
        public void CheckAndReleaseSlotIfComplete(IAIAgent agent)
        {
            // Only release slot when skill returns to Uncharged AND execution is complete
            if (hasAttackSlot && skillSystem.CurrentState == SkillExecutionState.Uncharged)
            {
                // Check if there's still an execution in progress
                var combatController = owner.GetComponent<CombatController>();
                if (!SkillExecutionTracker.Instance.HasExecutionInProgress(combatController))
                {
                    ReleaseAttackSlot(agent);
                }
                else if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"[PatternCombatHandler] {transform.name} skill state is Uncharged but execution still in progress - keeping slot");
                }
            }
        }

        /// <summary>
        /// Releases attack slot if held.
        /// </summary>
        public void ReleaseAttackSlot(IAIAgent agent)
        {
            if (hasAttackSlot && coordinator != null)
            {
                // Release attack slot using new IAIAgent interface
                coordinator.ReleaseAttackSlot(agent);
                hasAttackSlot = false;

                if (enableDebugLogs)
                {
                    CombatLogger.LogAttack($"[PatternCombatHandler] {transform.name} released attack slot");
                }
            }
        }
    }
}
