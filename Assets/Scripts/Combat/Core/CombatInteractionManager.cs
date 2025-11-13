using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    public class CombatInteractionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogs = true;

        private static CombatInteractionManager instance;
        public static CombatInteractionManager Instance => instance;

        private SkillExecutionPool executionPool = new SkillExecutionPool();
        private Queue<SkillExecution> pendingExecutions = new Queue<SkillExecution>();
        private List<SkillExecution> waitingDefensiveSkills = new List<SkillExecution>();

        // List pool for Phase 3.3 optimization
        private static Stack<List<SkillExecution>> skillExecutionListPool = new Stack<List<SkillExecution>>();
        private static Stack<List<List<SkillExecution>>> nestedListPool = new Stack<List<List<SkillExecution>>>();
        private static Stack<List<SpeedResolutionGroupResult>> resultsListPool = new Stack<List<SpeedResolutionGroupResult>>();

        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            // Clear singleton reference when destroyed (scene unload)
            if (instance == this)
            {
                instance = null;
            }

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
                Debug.Log("[CombatInteractionManager] OnDestroy - Cleared all lists and pools");
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
            const float DEFENSIVE_SKILL_TIMEOUT = 5.0f; // 5 seconds max waiting time

            for (int i = waitingDefensiveSkills.Count - 1; i >= 0; i--)
            {
                var defensiveSkill = waitingDefensiveSkills[i];

                // NULL SAFETY: Check if references are destroyed (scene reload, object destruction)
                if (defensiveSkill == null || defensiveSkill.skillSystem == null ||
                    defensiveSkill.combatant == null || !defensiveSkill.skillSystem)
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"[Scene Cleanup] Removing defensive skill with destroyed references at index {i}");
                    }
                    waitingDefensiveSkills.RemoveAt(i);
                    if (defensiveSkill != null)
                    {
                        executionPool.Return(defensiveSkill);
                    }
                    continue;
                }

                // Check if defensive skill has been waiting too long
                if (Time.time - defensiveSkill.timestamp > DEFENSIVE_SKILL_TIMEOUT)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[CRITICAL FIX #2/#3] Cleaning up stale {defensiveSkill.combatant.name} {defensiveSkill.skillType} (waited {Time.time - defensiveSkill.timestamp:F1}s)");
                    }

                    // Force complete the defensive skill on the SkillSystem
                    CompleteDefensiveSkillExecution(defensiveSkill);

                    // NOTE: CompleteDefensiveSkillExecution triggers WaitingState.OnExit which calls
                    // RemoveWaitingDefensiveSkill, which already removes from list and returns to pool
                    // So we DON'T need to manually remove here (would cause double-removal)
                    // The list will be modified by the state transition callback
                }
            }
        }

        // List pool helper methods (Phase 3.3 optimization)
        private static List<SkillExecution> GetSkillExecutionList()
        {
            return skillExecutionListPool.Count > 0 ? skillExecutionListPool.Pop() : new List<SkillExecution>();
        }

        private static void ReturnSkillExecutionList(List<SkillExecution> list)
        {
            list.Clear();
            skillExecutionListPool.Push(list);
        }

        private static List<SpeedResolutionGroupResult> GetResultsList()
        {
            return resultsListPool.Count > 0 ? resultsListPool.Pop() : new List<SpeedResolutionGroupResult>();
        }

        private static void ReturnResultsList(List<SpeedResolutionGroupResult> list)
        {
            list.Clear();
            resultsListPool.Push(list);
        }

        private static List<List<SkillExecution>> GetNestedList()
        {
            return nestedListPool.Count > 0 ? nestedListPool.Pop() : new List<List<SkillExecution>>();
        }

        private static void ReturnNestedList(List<List<SkillExecution>> list)
        {
            // Return inner lists to pool first
            foreach (var innerList in list)
            {
                ReturnSkillExecutionList(innerList);
            }
            list.Clear();
            nestedListPool.Push(list);
        }

        public void ProcessSkillExecution(SkillSystem skillSystem, SkillType skillType)
        {
            var execution = executionPool.Get();
            execution.skillSystem = skillSystem;
            execution.skillType = skillType;
            execution.combatant = skillSystem.GetComponent<CombatController>();
            execution.timestamp = Time.time;

            if (SpeedResolver.IsOffensiveSkill(skillType))
            {
                pendingExecutions.Enqueue(execution);
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
                Debug.LogWarning($"<color=red>[Scene Cleanup] RemoveWaitingDefensiveSkill called with null/destroyed SkillSystem</color>");
                return;
            }

            Debug.Log($"<color=orange>[CombatInteractionManager] RemoveWaitingDefensiveSkill called for {skillSystem.gameObject.name}, waiting list has {waitingDefensiveSkills.Count} entries</color>");

            // Find and remove the SkillExecution associated with this SkillSystem
            for (int i = waitingDefensiveSkills.Count - 1; i >= 0; i--)
            {
                var defensiveSkill = waitingDefensiveSkills[i];

                // NULL SAFETY: Skip destroyed entries
                if (defensiveSkill == null || defensiveSkill.skillSystem == null || !defensiveSkill.skillSystem)
                {
                    Debug.LogWarning($"<color=yellow>[Scene Cleanup] Skipping destroyed entry at index {i}</color>");
                    waitingDefensiveSkills.RemoveAt(i);
                    if (defensiveSkill != null)
                    {
                        executionPool.Return(defensiveSkill);
                    }
                    continue;
                }

                if (defensiveSkill.skillSystem == skillSystem)
                {
                    Debug.Log($"<color=lime>[State Pattern Cleanup] Found and removing {defensiveSkill.combatant.name} {defensiveSkill.skillType} from waiting list (index {i})</color>");

                    waitingDefensiveSkills.RemoveAt(i);
                    executionPool.Return(defensiveSkill);

                    Debug.Log($"<color=lime>[State Pattern Cleanup] Removal complete, waiting list now has {waitingDefensiveSkills.Count} entries</color>");
                    return; // Found and removed
                }
            }

            Debug.LogWarning($"<color=red>[State Pattern Cleanup] Could not find {skillSystem.gameObject.name} in waiting list!</color>");
        }

        private void ProcessPendingExecutions()
        {
            if (pendingExecutions.Count == 0) return;

            var offensiveSkills = GetSkillExecutionList(); // Phase 3.3: Use pooled list

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
                    executionPool.Return(execution);
                }
            }

            if (offensiveSkills.Count == 1)
            {
                // Single offensive skill
                ProcessSingleOffensiveSkill(offensiveSkills[0]);
                executionPool.Return(offensiveSkills[0]);
            }
            else if (offensiveSkills.Count > 1)
            {
                // Multiple offensive skills - resolve speed conflicts
                ProcessMultipleOffensiveSkills(offensiveSkills);
                // Return all to pool after processing
                foreach (var skill in offensiveSkills)
                {
                    executionPool.Return(skill);
                }
            }

            // Return list to pool (Phase 3.3)
            ReturnSkillExecutionList(offensiveSkills);
        }

        private void ProcessSingleOffensiveSkill(SkillExecution offensiveSkill)
        {
            // Check for defensive responses
            var validDefenses = GetValidDefensiveResponses(offensiveSkill);

            if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
            {
                Debug.Log($"[RangedAttack Debug] {offensiveSkill.combatant.name} RangedAttack found {validDefenses.Count} defensive responses");
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

            // Return list to pool (Phase 3.3)
            ReturnSkillExecutionList(validDefenses);
        }

        private void ProcessMultipleOffensiveSkills(List<SkillExecution> offensiveSkills)
        {
            // Resolve speed conflicts between offensive skills
            var speedResults = ResolveSpeedConflicts(offensiveSkills);

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
                            Debug.Log($"[CRITICAL FIX #1] {execution.combatant.name} died mid-execution, skipping {execution.skillType}");
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

            // Return results list to pool (Phase 3.3)
            ReturnResultsList(speedResults);
        }

        private List<SkillExecution> GetValidDefensiveResponses(SkillExecution offensiveSkill)
        {
            var validResponses = GetSkillExecutionList(); // Phase 3.3: Use pooled list

            if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
            {
                Debug.Log($"[RangedAttack Debug] Checking {waitingDefensiveSkills.Count} waiting defensive skills");
                foreach (var defensiveSkill in waitingDefensiveSkills.ToList())
                {
                    Debug.Log($"[RangedAttack Debug] Found waiting {defensiveSkill.skillType} from {defensiveSkill.combatant.name}");
                }
            }

            foreach (var defensiveSkill in waitingDefensiveSkills.ToList())
            {
                // Check if defensive skill can respond to this offensive skill
                bool canRespond = CanDefensiveSkillRespond(defensiveSkill, offensiveSkill);

                if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
                {
                    Debug.Log($"[RangedAttack Debug] {defensiveSkill.combatant.name} {defensiveSkill.skillType} can respond: {canRespond}");
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
                    Debug.Log($"[RangedAttack Debug] SpeedResolver.CanInteract returned FALSE for {offensiveSkill.skillType} vs {defensiveSkill.skillType}");
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
                    Debug.Log($"[RangedAttack Debug] Attacker {offensiveSkill.combatant.name} OUT OF RANGE: distance={distance:F1}, attacker weapon range={attackerRange:F1}");
                }
                return false;
            }

            if (enableDebugLogs && offensiveSkill.skillType == SkillType.RangedAttack)
            {
                Debug.Log($"[RangedAttack Debug] All checks PASSED - {defensiveSkill.combatant.name} {defensiveSkill.skillType} can respond!");
            }

            return true;
        }

        private void ProcessSkillInteraction(SkillExecution offensiveSkill, SkillExecution defensiveSkill)
        {
            var interaction = DetermineInteraction(offensiveSkill.skillType, defensiveSkill.skillType);

            if (enableDebugLogs)
            {
                Debug.Log($"Processing interaction: {offensiveSkill.combatant.name} {offensiveSkill.skillType} vs " +
                         $"{defensiveSkill.combatant.name} {defensiveSkill.skillType} = {interaction}");
            }

            // Get character stats and weapon data
            var attackerStats = offensiveSkill.combatant.Stats;
            var defenderStats = defensiveSkill.combatant.Stats;
            var attackerWeapon = offensiveSkill.combatant.GetComponent<WeaponController>()?.WeaponData;
            var defenderWeapon = defensiveSkill.combatant.GetComponent<WeaponController>()?.WeaponData;

            if (attackerStats == null || defenderStats == null || attackerWeapon == null || defenderWeapon == null)
            {
                Debug.LogError("Missing required components for skill interaction");
                return;
            }

            // Process damage and effects based on interaction
            // Note: ProcessInteractionEffects will handle completing defensive skills conditionally
            ProcessInteractionEffects(interaction, offensiveSkill, defensiveSkill, attackerStats, defenderStats, attackerWeapon, defenderWeapon);
        }

        private InteractionResult DetermineInteraction(SkillType offensive, SkillType defensive)
        {
            // All interactions from the matrix
            switch (offensive)
            {
                case SkillType.Attack:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.AttackerStunned; // Attack vs Defense → Attacker stunned, defender blocks
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Attack vs Counter → Counter reflection
                    }
                    break;

                case SkillType.Smash:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderKnockedDown; // Smash vs Defense → Defender knocked down, takes 75% reduced damage
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Smash vs Counter → Counter reflection
                    }
                    break;

                case SkillType.Windmill:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderBlocks; // Windmill vs Defense → No status effects, defender blocks
                        case SkillType.Counter: return InteractionResult.WindmillBreaksCounter; // Windmill vs Counter → Windmill breaks counter, knocks down defender
                    }
                    break;

                case SkillType.RangedAttack:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderBlocks; // RangedAttack vs Defense → Defender blocks 100% damage
                        case SkillType.Counter: return InteractionResult.CounterIneffective; // RangedAttack vs Counter → Counter is ineffective
                    }
                    break;

                case SkillType.Lunge:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.AttackerStunned; // Lunge vs Defense → Attacker stunned, defender blocks
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Lunge vs Counter → Counter reflection
                    }
                    break;
            }

            return InteractionResult.NoInteraction;
        }

        /// <summary>
        /// Helper method to safely get combatant name (handles destroyed objects)
        /// </summary>
        private string GetSafeCombatantName(SkillExecution execution)
        {
            if (execution == null || execution.combatant == null || !execution.combatant)
            {
                return "[Destroyed]";
            }
            return execution.combatant.name;
        }

        private void ProcessInteractionEffects(
            InteractionResult interaction,
            SkillExecution attacker,
            SkillExecution defender,
            CharacterStats attackerStats,
            CharacterStats defenderStats,
            WeaponData attackerWeapon,
            WeaponData defenderWeapon)
        {
            // NULL SAFETY: Check if combatants are destroyed (scene reload, death during processing)
            if (attacker == null || attacker.combatant == null || !attacker.combatant ||
                defender == null || defender.combatant == null || !defender.combatant)
            {
                Debug.LogWarning($"[Scene Cleanup] ProcessInteractionEffects aborted - attacker or defender destroyed");
                return;
            }

            var attackerHealth = attacker.combatant.GetComponent<HealthSystem>();
            var defenderHealth = defender.combatant.GetComponent<HealthSystem>();
            var attackerStatusEffects = attacker.combatant.GetComponent<StatusEffectManager>();
            var defenderStatusEffects = defender.combatant.GetComponent<StatusEffectManager>();
            var defenderKnockdownMeter = defender.combatant.GetComponent<KnockdownMeterTracker>();

            // Null safety checks
            if (attackerHealth == null || defenderHealth == null ||
                attackerStatusEffects == null || defenderStatusEffects == null ||
                defenderKnockdownMeter == null)
            {
                Debug.LogWarning($"[Scene Cleanup] ProcessInteractionEffects aborted - missing components");
                return;
            }

            switch (interaction)
            {
                case InteractionResult.AttackerStunned: // Attack vs Defense
                    // Attacker stunned, defender blocks (0 damage)
                    attackerStatusEffects.ApplyStun(attackerWeapon.stunDuration);
                    defenderStatusEffects.ApplyStun(attackerWeapon.stunDuration * 0.5f); // Defender receives half stun

                    // ONE-HIT BLOCK: Defense breaks after blocking first hit
                    defender.skillSystem.MarkDefenseBlocked();

                    if (enableDebugLogs)
                    {
                        Debug.Log($"{GetSafeCombatantName(attacker)} attack blocked by {GetSafeCombatantName(defender)} defense (Defense broken after block)");
                    }

                    // Defense breaks immediately after blocking
                    CompleteDefensiveSkillExecution(defender);
                    break;

                case InteractionResult.CounterReflection: // Any skill vs Counter
                    // Attacker knocked down, defender takes 0 damage, reflects calculated damage back
                    Vector3 counterKnockbackDirection = (attacker.combatant.transform.position - defender.combatant.transform.position).normalized;
                    Vector3 counterDisplacement = counterKnockbackDirection * CombatConstants.COUNTER_KNOCKBACK_DISTANCE;
                    attackerStatusEffects.ApplyInteractionKnockdown(counterDisplacement);
                    int reflectedDamage = DamageCalculator.CalculateCounterReflection(attackerStats, attackerWeapon);
                    attackerHealth.TakeDamage(reflectedDamage, defender.combatant.transform);

                    // Register hit dealt for defender's pattern tracking (counter reflected damage)
                    WeaponController defenderWeaponController = defender.combatant.GetComponent<WeaponController>();
                    defenderWeaponController?.RegisterHitDealt(attacker.combatant.transform);

                    // Note: Counter knockdown overrides stun, but stun should technically be applied first
                    // The knockdown from counter is stronger, so the stun effect is immediately overridden
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{GetSafeCombatantName(defender)} counter reflected {reflectedDamage} damage to {GetSafeCombatantName(attacker)}");
                    }
                    // Complete defensive skill
                    CompleteDefensiveSkillExecution(defender);
                    break;

                case InteractionResult.CounterIneffective: // RangedAttack vs Counter
                    // Counter is ineffective against ranged attacks
                    // Check if ranged attack hit
                    bool counterRangedAttackHit = attacker.skillSystem.LastRangedAttackHit;

                    if (counterRangedAttackHit)
                    {
                        // HIT: Defender takes full damage, no reflection
                        int rangedDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats, SkillType.RangedAttack);
                        defenderHealth.TakeDamage(rangedDamage, attacker.combatant.transform);

                        // Register hit dealt for attacker's pattern tracking
                        WeaponController rangedAttackerWeaponController = attacker.combatant.GetComponent<WeaponController>();
                        rangedAttackerWeaponController?.RegisterHitDealt(defender.combatant.transform);

                        // Apply universal hit stun
                        defenderStatusEffects.ApplyStun(attackerWeapon.stunDuration);

                        // Build knockdown meter
                        defenderKnockdownMeter.AddMeterBuildup(rangedDamage, attackerStats, attacker.combatant.transform);

                        if (enableDebugLogs)
                        {
                            Debug.Log($"{GetSafeCombatantName(defender)} Counter ineffective against {GetSafeCombatantName(attacker)} RangedAttack - took {rangedDamage} damage");
                        }

                        // Complete Counter (WaitingState.OnExit handles cleanup)
                        CompleteDefensiveSkillExecution(defender);
                    }
                    else
                    {
                        // MISS: Counter takes 0 damage but still completes (wasted counter)
                        if (enableDebugLogs)
                        {
                            Debug.Log($"{GetSafeCombatantName(attacker)} RangedAttack missed - {GetSafeCombatantName(defender)} Counter wasted");
                        }

                        // Complete Counter (WaitingState.OnExit handles cleanup)
                        CompleteDefensiveSkillExecution(defender);
                    }
                    break;

                case InteractionResult.DefenderKnockedDown: // Smash vs Defense
                    // Defender knocked down, takes 75% reduced damage
                    Vector3 smashKnockbackDirection = (defender.combatant.transform.position - attacker.combatant.transform.position).normalized;
                    Vector3 smashDisplacement = smashKnockbackDirection * CombatConstants.SMASH_KNOCKBACK_DISTANCE;
                    defenderStatusEffects.ApplyInteractionKnockdown(smashDisplacement);
                    int baseDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats, attacker.skillType);
                    int reducedDamage = DamageCalculator.ApplyDamageReduction(baseDamage, CombatConstants.SMASH_VS_DEFENSE_DAMAGE_REDUCTION, defenderStats);
                    defenderHealth.TakeDamage(reducedDamage, attacker.combatant.transform);

                    // Register hit dealt for attacker's pattern tracking
                    WeaponController attackerWeaponController = attacker.combatant.GetComponent<WeaponController>();
                    attackerWeaponController?.RegisterHitDealt(defender.combatant.transform);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"{GetSafeCombatantName(attacker)} smash broke through {GetSafeCombatantName(defender)} defense for {reducedDamage} damage");
                    }
                    // Complete defensive skill
                    CompleteDefensiveSkillExecution(defender);
                    break;

                case InteractionResult.DefenderBlocks: // Windmill vs Defense OR RangedAttack vs Defense
                    if (attacker.skillType == SkillType.RangedAttack)
                    {
                        // Check if the ranged attack hit or missed
                        bool rangedAttackHit = attacker.skillSystem.LastRangedAttackHit;

                        if (rangedAttackHit)
                        {
                            // HIT: Defense blocks 100% of ranged attack damage (complete block)
                            // ONE-HIT BLOCK: Defense breaks after blocking first hit
                            defender.skillSystem.MarkDefenseBlocked();

                            if (enableDebugLogs)
                            {
                                Debug.Log($"{GetSafeCombatantName(defender)} completely blocked {GetSafeCombatantName(attacker)} RangedAttack (Defense broken after block)");
                            }

                            // Defense breaks immediately after blocking
                            CompleteDefensiveSkillExecution(defender);
                        }
                        else
                        {
                            // MISS: Defense takes 0 damage, but is consumed (one block per activation)
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{GetSafeCombatantName(attacker)} RangedAttack missed - {GetSafeCombatantName(defender)} Defense consumed (no block)");
                            }

                            // Complete Defense (WaitingState.OnExit handles cleanup)
                            CompleteDefensiveSkillExecution(defender);
                        }
                    }
                    else
                    {
                        // Windmill vs Defense: Blocked cleanly (0 damage)
                        // ONE-HIT BLOCK: Defense breaks after blocking first hit
                        defender.skillSystem.MarkDefenseBlocked();

                        if (enableDebugLogs)
                        {
                            Debug.Log($"{GetSafeCombatantName(defender)} blocked {GetSafeCombatantName(attacker)} windmill (Defense broken after block)");
                        }

                        // Defense breaks immediately after blocking
                        CompleteDefensiveSkillExecution(defender);
                    }
                    break;

                case InteractionResult.WindmillBreaksCounter: // Windmill vs Counter
                    // Windmill breaks through counter, knocks down defender, deals normal damage
                    Vector3 windmillKnockbackDirection = (defender.combatant.transform.position - attacker.combatant.transform.position).normalized;
                    Vector3 windmillDisplacement = windmillKnockbackDirection * CombatConstants.WINDMILL_KNOCKBACK_DISTANCE;
                    defenderStatusEffects.ApplyInteractionKnockdown(windmillDisplacement);
                    int windmillDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats, attacker.skillType);
                    defenderHealth.TakeDamage(windmillDamage, attacker.combatant.transform);

                    // Register hit dealt for attacker's pattern tracking
                    WeaponController windmillAttackerWeaponController = attacker.combatant.GetComponent<WeaponController>();
                    windmillAttackerWeaponController?.RegisterHitDealt(defender.combatant.transform);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"{GetSafeCombatantName(attacker)} windmill broke through {GetSafeCombatantName(defender)} counter for {windmillDamage} damage and knockdown");
                    }
                    // Complete defensive skill
                    CompleteDefensiveSkillExecution(defender);
                    break;
            }
        }

        private void ExecuteOffensiveSkillDirectly(SkillExecution execution)
        {
            // CRITICAL FIX #1: Safety check - don't execute if attacker is dead
            var attackerHealth = execution.combatant.GetComponent<HealthSystem>();
            if (attackerHealth == null || !attackerHealth.IsAlive)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[CRITICAL FIX #1] {execution.combatant.name} is dead, cannot execute {execution.skillType}");
                }
                return;
            }

            // WINDMILL: AoE skill that hits all enemies in range
            if (execution.skillType == SkillType.Windmill)
            {
                ExecuteWindmillAoE(execution);
                return;
            }

            // All other skills: single-target execution
            var target = execution.combatant.CurrentTarget;
            if (target == null) return;

            var targetHealth = target.GetComponent<HealthSystem>();
            var targetKnockdownMeter = target.GetComponent<KnockdownMeterTracker>();
            var targetStatusEffects = target.GetComponent<StatusEffectManager>();

            // Null safety checks
            Debug.Assert(targetHealth != null, $"HealthSystem is null on {target.gameObject.name}");
            Debug.Assert(targetKnockdownMeter != null, $"KnockdownMeterTracker is null on {target.gameObject.name}");
            Debug.Assert(targetStatusEffects != null, $"StatusEffectManager is null on {target.gameObject.name}");
            if (targetHealth == null) return; // Fallback for production builds

            var attackerStats = execution.combatant.Stats;
            var targetStats = targetHealth.GetComponent<CombatController>()?.Stats;
            var attackerWeapon = execution.combatant.GetComponent<WeaponController>()?.WeaponData;

            Debug.Assert(attackerStats != null, $"CharacterStats is null on {execution.combatant.gameObject.name}");
            Debug.Assert(targetStats != null, $"CharacterStats is null on {target.gameObject.name}");
            Debug.Assert(attackerWeapon != null, $"WeaponData is null on {execution.combatant.gameObject.name}");
            if (attackerStats == null || targetStats == null || attackerWeapon == null) return; // Fallback for production builds

            // SPECIAL HANDLING FOR RANGED ATTACK: Check if it hit
            if (execution.skillType == SkillType.RangedAttack)
            {
                bool rangedAttackHit = execution.skillSystem.LastRangedAttackHit;

                if (!rangedAttackHit)
                {
                    // MISS: No damage, no effects
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{execution.combatant.name} RangedAttack missed {target.name}");
                    }
                    return; // Skip all damage and effects
                }

                // If we reach here, the ranged attack hit - continue with normal damage application
            }

            // Calculate and apply damage (with skill-specific damage multiplier)
            int damage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, targetStats, execution.skillType);
            targetHealth.TakeDamage(damage, execution.combatant.transform);

            // Register hit dealt for attacker's pattern tracking
            WeaponController executionWeaponController = execution.combatant.GetComponent<WeaponController>();
            executionWeaponController?.RegisterHitDealt(target.transform);

            // UNIVERSAL: All hits apply stun (Mabinogi three-tier CC system)
            targetStatusEffects.ApplyStun(attackerWeapon.stunDuration);

            // Apply skill-specific effects
            switch (execution.skillType)
            {
                case SkillType.Attack:
                case SkillType.RangedAttack:
                case SkillType.Lunge:
                    // Build knockdown meter (stun already applied above)
                    targetKnockdownMeter.AddMeterBuildup(damage, attackerStats, execution.combatant.transform);
                    break;

                case SkillType.Smash:
                case SkillType.Windmill:
                    // Immediate knockdown with displacement (bypasses meter system)
                    // Stun is overridden by knockdown effect
                    Vector3 directHitDirection = (target.transform.position - execution.combatant.transform.position).normalized;
                    Vector3 directHitDisplacement = directHitDirection * (execution.skillType == SkillType.Smash
                        ? CombatConstants.SMASH_KNOCKBACK_DISTANCE
                        : CombatConstants.WINDMILL_KNOCKBACK_DISTANCE);
                    targetKnockdownMeter.TriggerImmediateKnockdown(directHitDisplacement);
                    break;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{execution.combatant.name} {execution.skillType} hit {target.name} for {damage} damage");
            }
        }

        private void ExecuteWindmillAoE(SkillExecution execution)
        {
            var attackerStats = execution.combatant.Stats;
            var attackerWeapon = execution.combatant.GetComponent<WeaponController>()?.WeaponData;

            if (attackerStats == null || attackerWeapon == null)
            {
                Debug.LogError($"Windmill execution failed: missing stats or weapon on {execution.combatant.name}");
                return;
            }

            // Get Windmill range
            float windmillRange = CombatConstants.WINDMILL_RADIUS;

            // Find all colliders in range
            Collider[] hitColliders = Physics.OverlapSphere(execution.combatant.transform.position, windmillRange);

            int hitCount = 0;
            foreach (var hitCollider in hitColliders)
            {
                // Skip self
                if (hitCollider.transform == execution.combatant.transform) continue;

                // Check if this is a valid target (has CombatController)
                var targetCombatController = hitCollider.GetComponent<CombatController>();
                if (targetCombatController == null) continue;

                // Skip if on same faction (don't hit allies)
                if (targetCombatController == execution.combatant) continue;

                // Get target components
                var targetHealth = hitCollider.GetComponent<HealthSystem>();
                var targetKnockdownMeter = hitCollider.GetComponent<KnockdownMeterTracker>();
                var targetStatusEffects = hitCollider.GetComponent<StatusEffectManager>();
                var targetStats = targetCombatController.Stats;

                if (targetHealth == null || targetKnockdownMeter == null || targetStatusEffects == null || targetStats == null)
                {
                    continue; // Skip invalid targets
                }

                // Skip dead targets
                if (!targetHealth.IsAlive) continue;

                // Calculate damage
                int damage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, targetStats, SkillType.Windmill);
                targetHealth.TakeDamage(damage, execution.combatant.transform);

                // Register hit dealt for attacker's pattern tracking
                WeaponController executionWeaponController = execution.combatant.GetComponent<WeaponController>();
                executionWeaponController?.RegisterHitDealt(hitCollider.transform);

                // Apply knockdown with displacement
                Vector3 knockbackDirection = (hitCollider.transform.position - execution.combatant.transform.position).normalized;
                Vector3 displacement = knockbackDirection * CombatConstants.WINDMILL_KNOCKBACK_DISTANCE;
                targetKnockdownMeter.TriggerImmediateKnockdown(displacement);

                hitCount++;

                if (enableDebugLogs)
                {
                    Debug.Log($"{execution.combatant.name} Windmill hit {hitCollider.name} for {damage} damage (AoE {hitCount})");
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{execution.combatant.name} Windmill AoE complete - hit {hitCount} targets");
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
                Debug.Log($"{execution.combatant.name} {execution.skillType} cancelled: {reason}");
            }
        }

        private List<SpeedResolutionGroupResult> ResolveSpeedConflicts(List<SkillExecution> offensiveSkills)
        {
            var results = GetResultsList(); // Phase 3.3: Use pooled list

            // Group skills by simultaneous execution timing
            var groups = GroupSimultaneousSkills(offensiveSkills);

            foreach (var group in groups)
            {
                if (group.Count == 1)
                {
                    results.Add(new SpeedResolutionGroupResult
                    {
                        resolution = SpeedResolution.Player1Wins,
                        winner = group[0],
                        loser = null
                    });
                }
                else
                {
                    // Resolve speed between group members
                    var speedResult = ResolveSpeedBetweenSkills(group);
                    results.Add(speedResult);
                }
            }

            // Return groups list to pool (Phase 3.3)
            ReturnNestedList(groups);

            return results;
        }

        private List<List<SkillExecution>> GroupSimultaneousSkills(List<SkillExecution> skills)
        {
            var groups = GetNestedList(); // Phase 3.3: Use pooled list

            foreach (var skill in skills.OrderBy(s => s.timestamp))
            {
                bool addedToGroup = false;

                foreach (var group in groups)
                {
                    if (Mathf.Abs(skill.timestamp - group[0].timestamp) <= CombatConstants.SIMULTANEOUS_EXECUTION_WINDOW)
                    {
                        group.Add(skill);
                        addedToGroup = true;
                        break;
                    }
                }

                if (!addedToGroup)
                {
                    var newGroup = GetSkillExecutionList(); // Phase 3.3: Use pooled list
                    newGroup.Add(skill);
                    groups.Add(newGroup);
                }
            }

            return groups;
        }

        private SpeedResolutionGroupResult ResolveSpeedBetweenSkills(List<SkillExecution> skills)
        {
            if (skills.Count < 2)
            {
                return new SpeedResolutionGroupResult
                {
                    resolution = SpeedResolution.Player1Wins,
                    winner = skills[0],
                    loser = null
                };
            }

            // Calculate speeds for all skills
            var skillSpeeds = skills.Select(skill =>
            {
                var combatant = skill.combatant;
                var stats = combatant.Stats;
                var weaponController = combatant.GetComponent<WeaponController>();
                var weapon = weaponController?.WeaponData;

                return new
                {
                    skill = skill,
                    speed = stats != null && weapon != null
                        ? SpeedResolver.CalculateSpeed(skill.skillType, stats, weapon)
                        : 0f
                };
            }).ToList();

            // Find highest speed
            float maxSpeed = skillSpeeds.Max(s => s.speed);
            var winners = skillSpeeds.Where(s => Mathf.Approximately(s.speed, maxSpeed)).ToList();

            if (winners.Count == 1)
            {
                // Single winner
                var winner = winners[0];
                var loser = skillSpeeds.FirstOrDefault(s => s != winner);

                return new SpeedResolutionGroupResult
                {
                    resolution = SpeedResolution.Player1Wins,
                    winner = winner.skill,
                    loser = loser?.skill
                };
            }
            else
            {
                // Tie - simultaneous execution
                return new SpeedResolutionGroupResult
                {
                    resolution = SpeedResolution.Tie,
                    tiedExecutions = winners.Select(w => w.skill).ToList()
                };
            }
        }

        private class SkillExecution
        {
            public SkillSystem skillSystem;
            public SkillType skillType;
            public CombatController combatant;
            public float timestamp;

            public void Reset()
            {
                skillSystem = null;
                skillType = SkillType.Attack;
                combatant = null;
                timestamp = 0f;
            }
        }

        private class SkillExecutionPool
        {
            private Stack<SkillExecution> pool = new Stack<SkillExecution>(CombatConstants.SKILL_EXECUTION_POOL_INITIAL_CAPACITY);

            public SkillExecution Get()
            {
                return pool.Count > 0 ? pool.Pop() : new SkillExecution();
            }

            public void Return(SkillExecution execution)
            {
                execution.Reset();
                pool.Push(execution);
            }
        }

        private class SpeedResolutionGroupResult
        {
            public SpeedResolution resolution;
            public SkillExecution winner;
            public SkillExecution loser;
            public List<SkillExecution> tiedExecutions;
        }
    }
}