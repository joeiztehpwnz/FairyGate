using System.Collections.Generic;
using UnityEngine;

namespace FairyGate.Combat
{
    public class CombatInteractionManager : Singleton<CombatInteractionManager>
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogs = true;

        // Component dependencies
        private CombatObjectPoolManager poolManager;
        private SkillInteractionResolver interactionResolver;
        private SpeedConflictResolver speedResolver;

        private Queue<SkillExecution> pendingExecutions = new Queue<SkillExecution>();
        private List<SkillExecution> waitingDefensiveSkills = new List<SkillExecution>();

        protected override void OnSingletonAwake()
        {
            // Initialize components
            poolManager = new CombatObjectPoolManager();
            interactionResolver = new SkillInteractionResolver(enableDebugLogs);
            speedResolver = new SpeedConflictResolver(poolManager);
        }

        protected override void OnSingletonDestroy()
        {
            // Clear all lists and pools to prevent stale references
            if (waitingDefensiveSkills != null)
            {
                waitingDefensiveSkills.Clear();
            }
            if (pendingExecutions != null)
            {
                pendingExecutions.Clear();
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat("[CombatInteractionManager] OnDestroy - Cleared all lists and pools");
            }
        }

        private void Update()
        {
            ProcessPendingExecutions();
            CleanupStaleDefensiveSkills();
        }

        /// <summary>
        /// CRITICAL FIX #2/#3: Periodically clean up defensive skills that have been waiting too long.
        /// Prevents memory leak from defensive skills that never get consumed.
        /// </summary>
        private void CleanupStaleDefensiveSkills()
        {
            // Use centralized constant for defensive skill timeout

            for (int i = waitingDefensiveSkills.Count - 1; i >= 0; i--)
            {
                var defensiveSkill = waitingDefensiveSkills[i];

                // NULL SAFETY: Check if references are destroyed (scene reload, object destruction)
                if (defensiveSkill == null || defensiveSkill.skillSystem == null ||
                    defensiveSkill.combatant == null || !defensiveSkill.skillSystem)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat($"[Scene Cleanup] Removing defensive skill with destroyed references at index {i}", CombatLogger.LogLevel.Warning);
                    }
                    waitingDefensiveSkills.RemoveAt(i);
                    if (defensiveSkill != null)
                    {
                        poolManager.ReturnSkillExecution(defensiveSkill);
                    }
                    continue;
                }

                // Check if defensive skill has been waiting too long
                if (Time.time - defensiveSkill.timestamp > CombatConstants.DEFENSIVE_SKILL_TIMEOUT_SECONDS)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat($"[CRITICAL FIX #2/#3] Cleaning up stale {defensiveSkill.combatant.name} {defensiveSkill.skillType} (waited {Time.time - defensiveSkill.timestamp:F1}s)");
                    }

                    // Force complete the defensive skill on the SkillSystem
                    CompleteDefensiveSkillExecution(defensiveSkill);

                    // SAFETY FALLBACK: If the skill is still in the list after cleanup attempt,
                    // manually remove it. This handles cases where ForceTransitionToRecovery()
                    // didn't trigger state transition (skill was already in different state).
                    if (i < waitingDefensiveSkills.Count && waitingDefensiveSkills[i] == defensiveSkill)
                    {
                        if (enableDebugLogs)
                        {
                            CombatLogger.LogCombat($"[SAFETY] Manual removal of {defensiveSkill.combatant.name} {defensiveSkill.skillType} (state transition didn't trigger cleanup)", CombatLogger.LogLevel.Warning);
                        }
                        waitingDefensiveSkills.RemoveAt(i);
                        poolManager.ReturnSkillExecution(defensiveSkill);
                    }
                }
            }
        }

        public void ProcessSkillExecution(SkillSystem skillSystem, SkillType skillType)
        {
            var execution = poolManager.GetSkillExecution();
            execution.skillSystem = skillSystem;
            execution.skillType = skillType;
            execution.combatant = skillSystem.GetComponent<CombatController>();
            execution.timestamp = Time.time;

            if (SpeedResolver.IsOffensiveSkill(skillType))
            {
                pendingExecutions.Enqueue(execution);

                // Track execution to prevent premature slot release
                SkillExecutionTracker.Instance.OnSkillQueued(execution.combatant, skillType);
            }
            else if (SpeedResolver.IsDefensiveSkill(skillType))
            {
                waitingDefensiveSkills.Add(execution);
            }
        }

        /// <summary>
        /// Phase 3: Removes a defensive skill from waiting list when SkillSystem exits Waiting state.
        /// Called by WaitingState.OnExit() to prevent memory leak.
        /// This fixes ALL memory leak bugs by guaranteeing cleanup on every transition.
        /// </summary>
        public void RemoveWaitingDefensiveSkill(SkillSystem skillSystem)
        {
            // NULL SAFETY: Check if skillSystem is destroyed before accessing properties
            if (skillSystem == null || !skillSystem)
            {
                CombatLogger.LogCombat($"<color=red>[Scene Cleanup] RemoveWaitingDefensiveSkill called with null/destroyed SkillSystem</color>", CombatLogger.LogLevel.Warning);
                return;
            }

            CombatLogger.LogCombat($"<color=orange>[CombatInteractionManager] RemoveWaitingDefensiveSkill called for {skillSystem.gameObject.name}, waiting list has {waitingDefensiveSkills.Count} entries</color>");

            // Find and remove the SkillExecution associated with this SkillSystem
            for (int i = waitingDefensiveSkills.Count - 1; i >= 0; i--)
            {
                var defensiveSkill = waitingDefensiveSkills[i];

                // NULL SAFETY: Skip destroyed entries
                if (defensiveSkill == null || defensiveSkill.skillSystem == null || !defensiveSkill.skillSystem)
                {
                    CombatLogger.LogCombat($"<color=yellow>[Scene Cleanup] Skipping destroyed entry at index {i}</color>", CombatLogger.LogLevel.Warning);
                    waitingDefensiveSkills.RemoveAt(i);
                    if (defensiveSkill != null)
                    {
                        poolManager.ReturnSkillExecution(defensiveSkill);
                    }
                    continue;
                }

                if (defensiveSkill.skillSystem == skillSystem)
                {
                    CombatLogger.LogCombat($"<color=lime>[State Pattern Cleanup] Found and removing {defensiveSkill.combatant.name} {defensiveSkill.skillType} from waiting list (index {i})</color>");

                    waitingDefensiveSkills.RemoveAt(i);
                    poolManager.ReturnSkillExecution(defensiveSkill);

                    CombatLogger.LogCombat($"<color=lime>[State Pattern Cleanup] Removal complete, waiting list now has {waitingDefensiveSkills.Count} entries</color>");
                    return; // Found and removed
                }
            }

            CombatLogger.LogCombat($"<color=red>[State Pattern Cleanup] Could not find {skillSystem.gameObject.name} in waiting list!</color>", CombatLogger.LogLevel.Warning);
        }

        private void ProcessPendingExecutions()
        {
            if (pendingExecutions.Count == 0) return;

            var offensiveSkills = poolManager.GetSkillExecutionList();

            // Collect all simultaneous offensive executions
            while (pendingExecutions.Count > 0)
            {
                var execution = pendingExecutions.Dequeue();
                if (Time.time - execution.timestamp < CombatConstants.SIMULTANEOUS_EXECUTION_WINDOW)
                {
                    offensiveSkills.Add(execution);
                }
                else
                {
                    // Execution too old, return to pool
                    poolManager.ReturnSkillExecution(execution);
                }
            }

            if (offensiveSkills.Count == 1)
            {
                // Single offensive skill
                ProcessSingleOffensiveSkill(offensiveSkills[0]);
                poolManager.ReturnSkillExecution(offensiveSkills[0]);
            }
            else if (offensiveSkills.Count > 1)
            {
                // Multiple offensive skills - resolve speed conflicts
                ProcessMultipleOffensiveSkills(offensiveSkills);
                // Return all to pool after processing
                foreach (var skill in offensiveSkills)
                {
                    poolManager.ReturnSkillExecution(skill);
                }
            }

            // Return list to pool
            poolManager.ReturnSkillExecutionList(offensiveSkills);
        }

        private void ProcessSingleOffensiveSkill(SkillExecution offensiveSkill)
        {
            // Notify tracker that processing has started
            SkillExecutionTracker.Instance.OnSkillProcessingStarted(offensiveSkill.combatant);

            // Check for defensive responses
            var validDefenses = GetValidDefensiveResponses(offensiveSkill);

            if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
            {
                CombatLogger.LogCombat($"[RangedAttack Debug] {offensiveSkill.combatant.name} RangedAttack found {validDefenses.Count} defensive responses");
            }

            if (validDefenses.Count == 0)
            {
                // No defensive response - execute normally
                ExecuteOffensiveSkillDirectly(offensiveSkill);
            }
            else
            {
                // Process defensive interactions
                foreach (var defense in validDefenses)
                {
                    ProcessSkillInteraction(offensiveSkill, defense);
                    // CRITICAL FIX: Don't return to pool here - WaitingState.OnExit() handles cleanup
                    // This prevents double-free bug that was causing Defense/Counter to get stuck
                    // The state machine transition during ProcessSkillInteraction triggers:
                    // CompleteDefensiveSkillExecution -> ForceTransitionToRecovery -> WaitingState.OnExit()
                    // -> RemoveWaitingDefensiveSkill() which removes from list AND returns to pool
                }
            }

            // Return list to pool
            poolManager.ReturnSkillExecutionList(validDefenses);

            // Notify tracker that processing is complete - slot can now be released
            SkillExecutionTracker.Instance.OnSkillProcessingCompleted(offensiveSkill.combatant);
        }

        private void ProcessMultipleOffensiveSkills(List<SkillExecution> offensiveSkills)
        {
            // Notify tracker that all skills are starting processing
            foreach (var skill in offensiveSkills)
            {
                SkillExecutionTracker.Instance.OnSkillProcessingStarted(skill.combatant);
            }

            // Resolve speed conflicts between offensive skills
            var speedResults = speedResolver.ResolveSpeedConflicts(offensiveSkills);

            foreach (var result in speedResults)
            {
                if (result.resolution == SpeedResolution.Tie)
                {
                    // Simultaneous execution (CRITICAL FIX #1: Check if combatants are still alive)
                    foreach (var execution in result.tiedExecutions)
                    {
                        // Check if combatant is still alive before executing
                        var healthSystem = execution.combatant.GetComponent<HealthSystem>();
                        if (healthSystem != null && healthSystem.IsAlive)
                        {
                            ExecuteOffensiveSkillDirectly(execution);
                        }
                        else if (enableDebugLogs)
                        {
                            CombatLogger.LogCombat($"[CRITICAL FIX #1] {execution.combatant.name} died mid-execution, skipping {execution.skillType}");
                        }
                    }
                }
                else
                {
                    // Winner executes, loser is cancelled
                    ExecuteOffensiveSkillDirectly(result.winner);
                    CancelSkillExecution(result.loser, "Lost speed resolution");
                }
            }

            // Return results list to pool
            poolManager.ReturnResultsList(speedResults);

            // Notify tracker that all skills have completed processing - slots can now be released
            foreach (var skill in offensiveSkills)
            {
                SkillExecutionTracker.Instance.OnSkillProcessingCompleted(skill.combatant);
            }
        }

        private List<SkillExecution> GetValidDefensiveResponses(SkillExecution offensiveSkill)
        {
            var validResponses = poolManager.GetSkillExecutionList();

            if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
            {
                CombatLogger.LogCombat($"[RangedAttack Debug] Checking {waitingDefensiveSkills.Count} waiting defensive skills");
                foreach (var defensiveSkill in waitingDefensiveSkills)
                {
                    CombatLogger.LogCombat($"[RangedAttack Debug] Found waiting {defensiveSkill.skillType} from {defensiveSkill.combatant.name}");
                }
            }

            // Create a copy to avoid modification during iteration
            var defensiveSkillsCopy = new List<SkillExecution>(waitingDefensiveSkills);
            foreach (var defensiveSkill in defensiveSkillsCopy)
            {
                // Check if defensive skill can respond to this offensive skill
                bool canRespond = CanDefensiveSkillRespond(defensiveSkill, offensiveSkill);

                if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
                {
                    CombatLogger.LogCombat($"[RangedAttack Debug] {defensiveSkill.combatant.name} {defensiveSkill.skillType} can respond: {canRespond}");
                }

                if (canRespond)
                {
                    validResponses.Add(defensiveSkill);
                    // Phase 3: DON'T remove here - let WaitingState.OnExit() handle cleanup
                    // This ensures cleanup happens exactly once via state machine lifecycle
                    // waitingDefensiveSkills.Remove(defensiveSkill); // OLD CODE - causes double removal
                }
            }

            return validResponses;
        }

        private bool CanDefensiveSkillRespond(SkillExecution defensiveSkill, SkillExecution offensiveSkill)
        {
            // Check range requirements
            var defenderWeapon = defensiveSkill.combatant.GetComponent<WeaponController>();
            var attackerTransform = offensiveSkill.combatant.transform;

            // SPECIAL CASE: For ranged attacks, defender doesn't need weapon range to block/counter
            // They're defending against a projectile, not attacking back
            bool isRangedAttack = offensiveSkill.skillType == SkillType.RangedAttack;

            // Initial range check when entering waiting state (skip for ranged attacks)
            if (!isRangedAttack && !defenderWeapon.IsInRange(attackerTransform))
            {
                return false;
            }

            // Check if skills can interact
            if (!SpeedResolver.CanInteract(offensiveSkill.skillType, defensiveSkill.skillType))
            {
                if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
                {
                    CombatLogger.LogCombat($"[RangedAttack Debug] SpeedResolver.CanInteract returned FALSE for {offensiveSkill.skillType} vs {defensiveSkill.skillType}");
                }
                return false;
            }

            // Secondary range check - attacker must be in range to hit the defender
            var attackerWeapon = offensiveSkill.combatant.GetComponent<WeaponController>();
            if (!attackerWeapon.IsInRange(defensiveSkill.combatant.transform))
            {
                if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
                {
                    float distance = Vector3.Distance(offensiveSkill.combatant.transform.position, defensiveSkill.combatant.transform.position);
                    float attackerRange = attackerWeapon?.WeaponData?.range ?? 0f;
                    CombatLogger.LogCombat($"[RangedAttack Debug] Attacker {offensiveSkill.combatant.name} OUT OF RANGE: distance={distance:F1}, attacker weapon range={attackerRange:F1}");
                }
                return false;
            }

            if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
            {
                CombatLogger.LogCombat($"[RangedAttack Debug] All checks PASSED - {defensiveSkill.combatant.name} {defensiveSkill.skillType} can respond!");
            }

            return true;
        }

        private void ProcessSkillInteraction(SkillExecution offensiveSkill, SkillExecution defensiveSkill)
        {
            var interaction = interactionResolver.DetermineInteraction(offensiveSkill.skillType, defensiveSkill.skillType);

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"Processing interaction: {offensiveSkill.combatant.name} {offensiveSkill.skillType} vs " +
                         $"{defensiveSkill.combatant.name} {defensiveSkill.skillType} = {interaction}");
            }

            // Get character stats and weapon data
            var attackerStats = offensiveSkill.combatant.Stats;
            var defenderStats = defensiveSkill.combatant.Stats;
            var attackerWeapon = offensiveSkill.combatant.GetComponent<WeaponController>()?.WeaponData;
            var defenderWeapon = defensiveSkill.combatant.GetComponent<WeaponController>()?.WeaponData;

            if (attackerStats == null || defenderStats == null || attackerWeapon == null || defenderWeapon == null)
            {
                CombatLogger.LogCombat("Missing required components for skill interaction", CombatLogger.LogLevel.Error);
                return;
            }

            // Process damage and effects based on interaction
            // Note: ProcessInteractionEffects will handle completing defensive skills conditionally
            interactionResolver.ProcessInteractionEffects(
                interaction,
                offensiveSkill,
                defensiveSkill,
                attackerStats,
                defenderStats,
                attackerWeapon,
                defenderWeapon,
                CompleteDefensiveSkillExecution);
        }


        private void ExecuteOffensiveSkillDirectly(SkillExecution execution)
        {
            if (!ValidateOffensiveExecution(execution))
                return;

            if (HandleSpecialSkillExecution(execution))
                return;

            var targetTransform = execution.combatant.CurrentTarget;
            if (targetTransform == null) return;

            var target = targetTransform.GetComponent<CombatController>();
            if (target == null) return;

            // Final faction check - don't attack non-hostiles
            if (!execution.combatant.IsHostileTo(target))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{execution.combatant.name} cancelled attack on non-hostile {target.name}");
                }
                return;
            }

            var targetComponents = GetTargetComponents(target);
            if (targetComponents == null) return;

            var attackerStats = execution.combatant.Stats;
            var attackerWeapon = execution.combatant.GetComponent<WeaponController>()?.WeaponData;
            if (attackerStats == null || attackerWeapon == null) return;

            if (HandleRangedAttackMiss(execution, target))
                return;

            int damage = ApplySkillDamage(execution, target, targetComponents, attackerStats, attackerWeapon, out bool wasCritical);
            ApplySkillEffects(execution, target, targetComponents, attackerStats, attackerWeapon, damage, wasCritical);

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{execution.combatant.name} {execution.skillType} hit {target.name} for {damage} damage");
            }
        }

        private bool ValidateOffensiveExecution(SkillExecution execution)
        {
            // CRITICAL FIX #1: Safety check - don't execute if attacker is dead
            var attackerHealth = execution.combatant.GetComponent<HealthSystem>();
            if (attackerHealth == null || !attackerHealth.IsAlive)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"[CRITICAL FIX #1] {execution.combatant.name} is dead, cannot execute {execution.skillType}");
                }
                return false;
            }
            return true;
        }

        private bool HandleSpecialSkillExecution(SkillExecution execution)
        {
            // WINDMILL: AoE skill that hits all enemies in range
            if (execution.skillType == SkillType.Windmill)
            {
                ExecuteWindmillAoE(execution);
                return true;
            }
            return false;
        }

        private class TargetComponents
        {
            public HealthSystem Health;
            public KnockdownMeterTracker KnockdownMeter;
            public StatusEffectManager StatusEffects;
            public CharacterStats Stats;
        }

        private TargetComponents GetTargetComponents(CombatController target)
        {
            var targetHealth = target.GetComponent<HealthSystem>();
            var targetKnockdownMeter = target.GetComponent<KnockdownMeterTracker>();
            var targetStatusEffects = target.GetComponent<StatusEffectManager>();

            // Null safety checks
            Debug.Assert(targetHealth != null, $"HealthSystem is null on {target.gameObject.name}");
            Debug.Assert(targetKnockdownMeter != null, $"KnockdownMeterTracker is null on {target.gameObject.name}");
            Debug.Assert(targetStatusEffects != null, $"StatusEffectManager is null on {target.gameObject.name}");
            if (targetHealth == null) return null; // Fallback for production builds

            var targetStats = targetHealth.GetComponent<CombatController>()?.Stats;
            Debug.Assert(targetStats != null, $"CharacterStats is null on {target.gameObject.name}");
            if (targetStats == null) return null;

            return new TargetComponents
            {
                Health = targetHealth,
                KnockdownMeter = targetKnockdownMeter,
                StatusEffects = targetStatusEffects,
                Stats = targetStats
            };
        }

        private bool HandleRangedAttackMiss(SkillExecution execution, CombatController target)
        {
            // SPECIAL HANDLING FOR RANGED ATTACK: Check if it hit
            if (execution.skillType == SkillType.RangedAttack)
            {
                bool rangedAttackHit = execution.skillSystem.LastRangedAttackHit;

                if (!rangedAttackHit)
                {
                    // MISS: No damage, no effects
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat($"{execution.combatant.name} RangedAttack missed {target.name}");
                    }
                    return true; // Indicates miss occurred
                }
            }
            return false; // No miss occurred
        }

        private int ApplySkillDamage(SkillExecution execution, CombatController target, TargetComponents targetComponents,
            CharacterStats attackerStats, WeaponData attackerWeapon, out bool wasCritical)
        {
            // N+1 System: Roll for critical hit
            wasCritical = DamageCalculator.RollCriticalHit(attackerStats);

            // Calculate and apply damage (with skill-specific damage multiplier)
            int damage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, targetComponents.Stats, execution.skillType);

            // Apply critical damage multiplier if critical hit
            if (wasCritical)
            {
                damage = DamageCalculator.CalculateCriticalDamage(damage);
            }

            targetComponents.Health.TakeDamage(damage, execution.combatant.transform);

            // Register hit dealt for attacker's pattern tracking
            WeaponController executionWeaponController = execution.combatant.GetComponent<WeaponController>();
            executionWeaponController?.RegisterHitDealt(target.transform);

            // N+1 System: Only basic Attack creates timing windows (weapon combo extension)
            // Skills like Smash/Windmill are finishers, not starters
            if (executionWeaponController != null && execution.skillType == SkillType.Attack)
            {
                executionWeaponController.OnHitLanded(target.transform, wasCritical);
            }

            return damage;
        }

        private void ApplySkillEffects(SkillExecution execution, CombatController target, TargetComponents targetComponents,
            CharacterStats attackerStats, WeaponData attackerWeapon, int damage, bool wasCritical)
        {
            // UNIVERSAL: All hits apply stun (Mabinogi three-tier CC system)
            // N+1 System: Calculate stun with Focus resistance and critical multiplier
            float calculatedStun = DamageCalculator.CalculateStunDuration(
                attackerWeapon.stunDuration,
                targetComponents.Stats,
                wasCritical
            );
            // Use ApplyCalculatedStun to prevent double Focus application
            targetComponents.StatusEffects.ApplyCalculatedStun(calculatedStun);

            // Apply skill-specific effects
            switch (execution.skillType)
            {
                case SkillType.Attack:
                case SkillType.RangedAttack:
                case SkillType.Lunge:
                    // Build knockdown meter (stun already applied above)
                    // N+1 System: Pass weapon data for knockdown rate modifier
                    targetComponents.KnockdownMeter.AddMeterBuildup(damage, attackerStats, attackerWeapon, execution.combatant.transform);
                    break;

                case SkillType.Smash:
                case SkillType.Windmill:
                    // Immediate knockdown with displacement (bypasses meter system)
                    // Stun is overridden by knockdown effect
                    Vector3 directHitDirection = (target.transform.position - execution.combatant.transform.position).normalized;
                    Vector3 directHitDisplacement = directHitDirection * (execution.skillType == SkillType.Smash
                        ? CombatConstants.SMASH_KNOCKBACK_DISTANCE
                        : CombatConstants.WINDMILL_KNOCKBACK_DISTANCE);
                    targetComponents.KnockdownMeter.TriggerImmediateKnockdown(directHitDisplacement);
                    break;
            }
        }

        private void ExecuteWindmillAoE(SkillExecution execution)
        {
            var attackerStats = execution.combatant.Stats;
            var attackerWeapon = execution.combatant.GetComponent<WeaponController>();

            if (attackerStats == null || attackerWeapon?.WeaponData == null)
            {
                CombatLogger.LogCombat($"Windmill execution failed: missing stats or weapon on {execution.combatant.name}", CombatLogger.LogLevel.Error);
                return;
            }

            var targets = GetWindmillTargets(execution);

            foreach (var target in targets)
            {
                ExecuteWindmillOnTarget(execution, target, attackerStats, attackerWeapon.WeaponData);
            }

            NotifyTrackerWindmillExecuted(execution, targets.Count);
        }

        /// <summary>
        /// Finds all valid targets for Windmill AoE attack.
        /// Filters out self, dead targets, allies, and invalid combatants.
        /// </summary>
        private List<CombatController> GetWindmillTargets(SkillExecution execution)
        {
            var targets = new List<CombatController>();
            float windmillRange = CombatConstants.WINDMILL_RADIUS;

            Collider[] hitColliders = Physics.OverlapSphere(execution.combatant.transform.position, windmillRange);

            foreach (var hitCollider in hitColliders)
            {
                // Skip self
                if (hitCollider.transform == execution.combatant.transform)
                    continue;

                // Check if valid target
                var targetCombatController = hitCollider.GetComponent<CombatController>();
                if (targetCombatController == null)
                    continue;

                // Skip allies (same combatant reference)
                if (targetCombatController == execution.combatant)
                    continue;

                // Skip dead targets
                var targetHealth = targetCombatController.GetComponent<HealthSystem>();
                if (targetHealth == null || !targetHealth.IsAlive)
                    continue;

                // Skip non-hostile targets (faction check)
                if (!execution.combatant.IsHostileTo(targetCombatController))
                    continue;

                targets.Add(targetCombatController);
            }

            return targets;
        }

        /// <summary>
        /// Executes Windmill attack on a single target.
        /// Reuses existing helper methods from Tier 1 refactoring for consistency.
        /// </summary>
        private void ExecuteWindmillOnTarget(SkillExecution execution, CombatController target,
            CharacterStats attackerStats, WeaponData attackerWeapon)
        {
            var targetComponents = GetTargetComponents(target);
            if (targetComponents == null)
                return;

            // Apply damage using shared helper
            int damage = ApplySkillDamage(execution, target, targetComponents, attackerStats, attackerWeapon, out bool wasCritical);

            // Apply effects using shared helper
            ApplySkillEffects(execution, target, targetComponents, attackerStats, attackerWeapon, damage, wasCritical);

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{execution.combatant.name} Windmill hit {target.name} for {damage} damage");
            }
        }

        /// <summary>
        /// Notifies execution tracker that Windmill AoE was executed.
        /// </summary>
        private void NotifyTrackerWindmillExecuted(SkillExecution execution, int hitCount)
        {
            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{execution.combatant.name} Windmill AoE complete - hit {hitCount} targets");
            }
        }

        private void CompleteDefensiveSkillExecution(SkillExecution defensiveSkill)
        {
            var skillSystem = defensiveSkill.skillSystem;
            if (skillSystem != null)
            {
                // Transition defensive skill out of waiting state to recovery
                // This triggers WaitingState.OnExit() which handles all cleanup:
                // - RemoveWaitingDefensiveSkill() removes from list
                // - Returns SkillExecution to pool
                // Single source of truth for cleanup prevents double-free bugs
                skillSystem.ForceTransitionToRecovery();
            }
        }

        private void CancelSkillExecution(SkillExecution execution, string reason)
        {
            execution.skillSystem.CancelSkill();

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{execution.combatant.name} {execution.skillType} cancelled: {reason}");
            }
        }
    }
}