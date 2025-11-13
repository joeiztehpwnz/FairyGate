using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Executes AI behavior patterns at runtime. Manages pattern state machine,
    /// evaluates conditions, handles transitions, and coordinates with other AI systems.
    ///
    /// Classic Mabinogi Design: AI follows consistent, learnable patterns that reward
    /// player observation and prediction.
    /// </summary>
    [RequireComponent(typeof(SkillSystem))]
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(StaminaSystem))]
    [RequireComponent(typeof(MovementController))]
    public class PatternExecutor : MonoBehaviour
    {
        [Header("Pattern Configuration")]
        [SerializeField] private PatternDefinition patternDefinition;

        [Header("Target Tracking")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private float playerSearchCooldown = 1.0f;

        [Header("Skill Timing")]
        [SerializeField] private float skillCooldown = 3.0f;
        [SerializeField] private float randomVariance = 2.0f;

        [Header("Combat Configuration")]
        [SerializeField] private float engageDistance = 3.0f;
        [SerializeField] private bool useCoordination = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDebugGUI = false;

        // Pattern state
        private PatternNode currentNode;
        private float timeInCurrentNode = 0f;
        private int hitsTaken = 0;
        private int hitsDealt = 0;
        private CombatState lastCombatState = CombatState.Idle;

        // Skill timing
        private float nextSkillTime;

        // Attack coordination
        private bool hasAttackSlot = false;

        // Skill execution tracking
        private Coroutine currentSkillCoroutine;

        // Weapon capability
        private bool hasRangedWeapon = false;

        // Context for pattern evaluation
        private PatternEvaluationContext context;

        // Component references
        private SkillSystem skillSystem;
        private HealthSystem healthSystem;
        private StaminaSystem staminaSystem;
        private CombatController combatController;
        private WeaponController weaponController;
        private MovementController movementController;
        private float lastPlayerSearchTime = -999f;

        // Movement state
        private AICoordinator coordinator;

        // Public accessors for debugging
        public PatternNode CurrentNode => currentNode;
        public float TimeInCurrentNode => timeInCurrentNode;
        public int HitsTaken => hitsTaken;
        public int HitsDealt => hitsDealt;

        private void Awake()
        {
            // Cache component references
            skillSystem = GetComponent<SkillSystem>();
            healthSystem = GetComponent<HealthSystem>();
            staminaSystem = GetComponent<StaminaSystem>();
            combatController = GetComponent<CombatController>();
            weaponController = GetComponent<WeaponController>();
            movementController = GetComponent<MovementController>();

            // Get coordinator if available
            coordinator = AICoordinator.Instance;

            // Initialize evaluation context
            context = new PatternEvaluationContext
            {
                skillSystem = skillSystem,
                weaponController = weaponController
            };
        }

        private void Start()
        {
            // Initialize pattern
            if (patternDefinition != null)
            {
                currentNode = patternDefinition.GetStartingNode();

                // Roll initial random value
                context.randomValue = Random.value;

                if (currentNode != null && enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} initialized with pattern '{patternDefinition.patternName}' starting at node '{currentNode.nodeName}'");
                }
            }
            else
            {
                Debug.LogWarning($"[PatternExecutor] {gameObject.name} has no pattern assigned! AI will not function.");
            }

            // Find player
            FindPlayer();

            // Initialize skill cooldown timer
            nextSkillTime = Time.time + skillCooldown + Random.Range(-randomVariance, randomVariance);

            // Check weapon capabilities
            UpdateWeaponCapabilities();

            // Subscribe to combat events for hit tracking
            if (healthSystem != null)
            {
                healthSystem.OnDamageReceived += OnDamageReceived;
            }

            // Subscribe to weapon hit events for hits dealt tracking
            if (weaponController != null)
            {
                weaponController.OnHitDealt += OnHitDealt;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (healthSystem != null)
            {
                healthSystem.OnDamageReceived -= OnDamageReceived;
            }

            if (weaponController != null)
            {
                weaponController.OnHitDealt -= OnHitDealt;
            }

            // Cleanup
            Cleanup();
        }

        private void OnDisable()
        {
            // Cleanup on disable as well
            Cleanup();
        }

        private void Update()
        {
            if (patternDefinition == null || currentNode == null)
                return;

            // Periodically search for player if lost
            if (targetPlayer == null && Time.time - lastPlayerSearchTime >= playerSearchCooldown)
            {
                FindPlayer();
                lastPlayerSearchTime = Time.time;
            }

            // Update time in current node
            timeInCurrentNode += Time.deltaTime;

            // Update evaluation context
            UpdateEvaluationContext();

            // Handle combat engagement/disengagement
            UpdateCombatEngagement();

            // Check for transitions
            // Normal transitions only when skill system is idle
            // High-priority defensive interrupts (priority >= 15) can override during skill execution
            bool canCheckTransitions = skillSystem.CurrentState == SkillExecutionState.Uncharged;
            bool canCheckDefensiveInterrupts = (context.selfCombatState == CombatState.Knockback ||
                                                 context.selfCombatState == CombatState.Stunned) &&
                                                skillSystem.CurrentState != SkillExecutionState.Uncharged;

            if (canCheckTransitions || canCheckDefensiveInterrupts)
            {
                PatternTransition validTransition = null;

                if (canCheckDefensiveInterrupts)
                {
                    // Only check high-priority transitions (>= 15) that can interrupt skills
                    validTransition = GetHighPriorityDefensiveTransition(context);

                    if (validTransition != null)
                    {
                        Debug.LogWarning($"[PatternExecutor] {gameObject.name} INTERRUPTING skill for defensive transition: {validTransition.targetNodeName}");
                        // Cancel current skill
                        skillSystem.CancelSkill();
                    }
                }
                else
                {
                    // Normal transition check
                    validTransition = currentNode.GetValidTransition(context);
                }

                if (validTransition != null)
                {
                    TransitionToNode(validTransition);
                }
                else if (canCheckTransitions)
                {
                    // No valid transition - check for timeout fallback (only during normal checks)
                    if (!string.IsNullOrEmpty(currentNode.fallbackNodeName) &&
                        currentNode.fallbackTimeout > 0f &&
                        timeInCurrentNode >= currentNode.fallbackTimeout)
                    {
                        // Timeout exceeded, transition to fallback node
                        if (enableDebugLogs)
                        {
                            Debug.LogWarning($"[PatternExecutor] {gameObject.name} node '{currentNode.nodeName}' timeout ({currentNode.fallbackTimeout}s) - transitioning to fallback '{currentNode.fallbackNodeName}'");
                        }

                        ForceTransitionToNode(currentNode.fallbackNodeName);
                    }
                }
            }

            // Try to use skills if in combat and cooldown is ready
            if (combatController.IsInCombat && Time.time >= nextSkillTime && skillSystem.CurrentState == SkillExecutionState.Uncharged)
            {
                TryUseSkill();
            }

            // Execute movement behavior for current node (but not during Charged state)
            // Freeze movement when charged to prevent range drift
            if (skillSystem.CurrentState != SkillExecutionState.Charged)
            {
                ExecuteMovementBehavior();
            }
        }

        /// <summary>
        /// Checks if the current pattern node is ready to execute.
        /// This checks node execution conditions (stamina, range, etc.)
        /// AI should only attempt skills when this returns true.
        /// </summary>
        public bool IsNodeReady()
        {
            if (currentNode == null)
                return false;

            // Check all node execution conditions
            return currentNode.CanExecute(context);
        }

        /// <summary>
        /// Gets the skill that the current pattern node wants to use.
        /// Called by SimpleTestAI or other AI controllers.
        /// NOTE: Caller should check IsNodeReady() before attempting to use this skill.
        /// </summary>
        public SkillType GetCurrentSkill()
        {
            if (currentNode == null)
            {
                // Fallback to safe default
                return SkillType.Attack;
            }

            // Simply return the node's skill
            // Readiness checks happen in IsNodeReady()
            return currentNode.skillToUse;
        }

        /// <summary>
        /// Gets telegraph data for the current node (if any).
        /// Returns null if no telegraph is defined.
        /// </summary>
        public TelegraphData GetCurrentTelegraph()
        {
            return currentNode?.telegraph;
        }

        /// <summary>
        /// Resets hit counters (usually called after certain transitions).
        /// </summary>
        public void ResetHitCounters()
        {
            hitsTaken = 0;
            hitsDealt = 0;

            if (enableDebugLogs)
            {
                Debug.Log($"[PatternExecutor] {gameObject.name} hit counters reset");
            }
        }

        /// <summary>
        /// Forces transition to a specific node by name.
        /// Useful for external triggers or special events.
        /// </summary>
        public void ForceTransitionToNode(string nodeName)
        {
            PatternNode targetNode = patternDefinition.GetNodeByName(nodeName);

            if (targetNode != null)
            {
                currentNode = targetNode;
                timeInCurrentNode = 0f;

                // Roll new random value for this node
                context.randomValue = Random.value;

                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} forced transition to node '{nodeName}'");
                }
            }
            else
            {
                Debug.LogWarning($"[PatternExecutor] {gameObject.name} failed to force transition - node '{nodeName}' not found");
            }
        }

        /// <summary>
        /// Gets the highest-priority defensive transition (priority >= 15) that can interrupt skill execution.
        /// Used for reactive defensive behaviors like knockback recovery.
        /// </summary>
        private PatternTransition GetHighPriorityDefensiveTransition(PatternEvaluationContext context)
        {
            if (currentNode == null || currentNode.transitions == null || currentNode.transitions.Count == 0)
                return null;

            const int DEFENSIVE_INTERRUPT_PRIORITY_THRESHOLD = 15;

            // Sort by priority (highest first)
            var sortedTransitions = new List<PatternTransition>(currentNode.transitions);
            sortedTransitions.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Return first high-priority transition whose conditions are met
            foreach (var transition in sortedTransitions)
            {
                // Only consider high-priority transitions
                if (transition.priority < DEFENSIVE_INTERRUPT_PRIORITY_THRESHOLD)
                    break; // Since sorted, no point checking lower priority

                if (transition.EvaluateConditions(context))
                    return transition;
            }

            return null;
        }

        private void TransitionToNode(PatternTransition transition)
        {
            PatternNode targetNode = patternDefinition.GetNodeByName(transition.targetNodeName);

            if (targetNode == null)
            {
                Debug.LogWarning($"[PatternExecutor] {gameObject.name} transition failed - target node '{transition.targetNodeName}' not found");
                return;
            }

            string previousNodeName = currentNode.nodeName;

            // Execute transition
            currentNode = targetNode;
            timeInCurrentNode = 0f;

            // Roll new random value for this node (for RandomChance conditions)
            context.randomValue = Random.value;

            // Reset hit counters if transition requests it
            if (transition.resetHitCounters)
            {
                ResetHitCounters();
            }

            // Start cooldown if transition requests it
            if (transition.startCooldownID > 0 && transition.cooldownDuration > 0f)
            {
                context.StartCooldown(transition.startCooldownID, transition.cooldownDuration);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[PatternExecutor] {gameObject.name} transitioned from '{previousNodeName}' to '{currentNode.nodeName}' (priority {transition.priority})");
            }
        }

        private void UpdateEvaluationContext()
        {
            // Update self state
            context.selfHealthPercentage = healthSystem != null
                ? (healthSystem.CurrentHealth / (float)healthSystem.MaxHealth) * 100f
                : 100f;

            context.selfStamina = staminaSystem != null
                ? staminaSystem.CurrentStamina
                : 0f;

            context.hitsTaken = hitsTaken;
            context.hitsDealt = hitsDealt;
            context.timeInCurrentNode = timeInCurrentNode;

            context.selfCombatState = combatController != null
                ? combatController.CurrentCombatState
                : CombatState.Idle;

            // Re-roll random value when entering defensive states for reactive decisions
            // This allows RandomChance conditions to work for interrupt-based transitions
            if (context.selfCombatState == CombatState.Knockback || context.selfCombatState == CombatState.Stunned)
            {
                // Re-roll if this is a NEW defensive state (wasn't in this state last frame)
                if (lastCombatState != context.selfCombatState)
                {
                    context.randomValue = Random.value;
                }
            }

            // Track state for next frame
            lastCombatState = context.selfCombatState;

            // Update player state
            if (targetPlayer != null)
            {
                context.playerTransform = targetPlayer;
                context.distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

                CombatController playerCombat = targetPlayer.GetComponent<CombatController>();
                if (playerCombat != null)
                {
                    context.playerSkill = playerCombat.CurrentSkill;
                    context.playerCombatState = playerCombat.CurrentCombatState;
                    context.isPlayerCharging = playerCombat.CurrentState == SkillExecutionState.Charging;
                }
                else
                {
                    context.isPlayerCharging = false;
                    context.playerSkill = SkillType.Attack;
                    context.playerCombatState = CombatState.Idle;
                }
            }
            else
            {
                context.distanceToPlayer = float.MaxValue;
                context.isPlayerCharging = false;
                context.playerSkill = SkillType.Attack;
                context.playerCombatState = CombatState.Idle;
            }
        }

        private void FindPlayer()
        {
            // Find player by looking for CombatController that isn't this AI
            var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var combatant in combatants)
            {
                if (combatant != combatController && combatant.name.Contains("Player"))
                {
                    targetPlayer = combatant.transform;

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PatternExecutor] {gameObject.name} found target: {targetPlayer.name}");
                    }
                    return;
                }
            }

            // Fallback: Find closest combatant
            if (targetPlayer == null && combatants.Length > 0)
            {
                float closestSqrDistance = float.MaxValue;
                foreach (var combatant in combatants)
                {
                    if (combatant != combatController)
                    {
                        float sqrDist = (transform.position - combatant.transform.position).sqrMagnitude;
                        if (sqrDist < closestSqrDistance)
                        {
                            closestSqrDistance = sqrDist;
                            targetPlayer = combatant.transform;
                        }
                    }
                }
            }
        }

        private void OnDamageReceived(int damage, Transform attacker)
        {
            hitsTaken++;

            if (enableDebugLogs)
            {
                Debug.Log($"[PatternExecutor] {gameObject.name} took hit (total: {hitsTaken})");
            }
        }

        private void OnHitDealt(Transform target)
        {
            hitsDealt++;

            if (enableDebugLogs)
            {
                Debug.Log($"[PatternExecutor] {gameObject.name} dealt hit to {target.name} (total: {hitsDealt})");
            }
        }

        /// <summary>
        /// Executes the movement behavior defined by the current pattern node.
        /// This is the core of pattern-driven movement.
        /// </summary>
        private void ExecuteMovementBehavior()
        {
            if (currentNode == null || movementController == null || targetPlayer == null)
            {
                movementController?.SetMovementInput(Vector3.zero);
                return;
            }

            // Check if movement is frozen for this node
            if (currentNode.freezeMovement || !movementController.CanMove)
            {
                movementController.SetMovementInput(Vector3.zero);
                return;
            }

            // Execute movement based on behavior type
            switch (currentNode.movementBehavior)
            {
                case MovementBehaviorType.MaintainCustomRange:
                    MaintainCustomRange(currentNode.customTargetRange, currentNode.rangeTolerance);
                    break;

                case MovementBehaviorType.ApproachTarget:
                    ApproachTarget();
                    break;

                case MovementBehaviorType.RetreatFromTarget:
                    RetreatFromTarget();
                    break;

                case MovementBehaviorType.CircleStrafeLeft:
                    CircleStrafe(false);
                    break;

                case MovementBehaviorType.CircleStrafeRight:
                    CircleStrafe(true);
                    break;

                case MovementBehaviorType.HoldPosition:
                    HoldPosition();
                    break;

                case MovementBehaviorType.UseFormationSlot:
                    UseFormationSlot();
                    break;

                case MovementBehaviorType.FollowAtDistance:
                    FollowAtDistance();
                    break;

                default:
                    movementController.SetMovementInput(Vector3.zero);
                    break;
            }
        }

        /// <summary>
        /// Maintains a specific range from target.
        /// </summary>
        private void MaintainCustomRange(float targetRange, float tolerance)
        {
            float distance = context.distanceToPlayer;
            Vector3 toTarget = (targetPlayer.position - transform.position).normalized;

            if (distance < targetRange - tolerance)
            {
                // Too close - move away
                movementController.SetMovementInput(-toTarget * currentNode.movementSpeedMultiplier);
            }
            else if (distance > targetRange + tolerance)
            {
                // Too far - move closer
                movementController.SetMovementInput(toTarget * currentNode.movementSpeedMultiplier);
            }
            else
            {
                // Within acceptable range - stop
                movementController.SetMovementInput(Vector3.zero);
            }
        }

        /// <summary>
        /// Moves directly toward target, stopping at weapon range.
        /// </summary>
        private void ApproachTarget()
        {
            if (weaponController == null || targetPlayer == null)
            {
                movementController?.SetMovementInput(Vector3.zero);
                return;
            }

            // Stop at weapon range (1.5m for melee, varies for ranged)
            float weaponRange = weaponController.WeaponData?.isRangedWeapon ?? false
                ? weaponController.GetRangedRange()
                : weaponController.GetMeleeRange();

            float distance = context.distanceToPlayer;

            // Approach if beyond weapon range, otherwise stop
            if (distance > weaponRange)
            {
                Vector3 toTarget = (targetPlayer.position - transform.position).normalized;
                movementController.SetMovementInput(toTarget * currentNode.movementSpeedMultiplier);
            }
            else
            {
                // At weapon range - stop moving
                movementController.SetMovementInput(Vector3.zero);
            }
        }

        /// <summary>
        /// Moves directly away from target.
        /// </summary>
        private void RetreatFromTarget()
        {
            Vector3 awayFromTarget = (transform.position - targetPlayer.position).normalized;
            movementController.SetMovementInput(awayFromTarget * currentNode.movementSpeedMultiplier);
        }

        /// <summary>
        /// Strafes in a circle around the target.
        /// </summary>
        private void CircleStrafe(bool clockwise)
        {
            Vector3 toTarget = (targetPlayer.position - transform.position).normalized;
            Vector3 strafeDirection = clockwise
                ? new Vector3(toTarget.z, 0, -toTarget.x)
                : new Vector3(-toTarget.z, 0, toTarget.x);

            movementController.SetMovementInput(strafeDirection.normalized * currentNode.movementSpeedMultiplier);
        }

        /// <summary>
        /// Stops all movement.
        /// </summary>
        private void HoldPosition()
        {
            movementController.SetMovementInput(Vector3.zero);
        }

        /// <summary>
        /// Requests formation slot from AICoordinator and moves to it.
        /// Falls back to weapon range if no coordinator available.
        /// NOTE: Formation system requires SimpleTestAI component, so this is simplified.
        /// </summary>
        private void UseFormationSlot()
        {
            // Formation system currently requires SimpleTestAI component for the coordinator API
            // For now, fall back to maintaining weapon range
            // TODO: Refactor AICoordinator to work with Transform or PatternExecutor directly
            if (weaponController != null)
            {
                float weaponRange = weaponController.WeaponData?.isRangedWeapon ?? false
                    ? weaponController.GetRangedRange()
                    : weaponController.GetMeleeRange();
                MaintainCustomRange(weaponRange, currentNode.rangeTolerance);
            }
            else
            {
                movementController?.SetMovementInput(Vector3.zero);
            }
        }

        /// <summary>
        /// Follows target at a safe distance (good for ranged characters).
        /// Classic Mabinogi: All melee weapons use uniform range.
        /// </summary>
        private void FollowAtDistance()
        {
            // Use a safe follow distance (longer than optimal range)
            float followDistance = 6.0f; // Default safe distance

            if (weaponController != null)
            {
                // Note: GetMeleeRange() returns uniform range for all melee weapons (classic Mabinogi design)
                bool isRanged = weaponController.WeaponData?.isRangedWeapon ?? false;
                float baseRange = isRanged
                    ? weaponController.GetRangedRange()  // Varies by ranged weapon type
                    : weaponController.GetMeleeRange();  // Uniform for all melee weapons
                followDistance = baseRange * 1.5f; // 150% of weapon range
            }

            MaintainCustomRange(followDistance, currentNode.rangeTolerance);
        }

        /// <summary>
        /// Updates combat engagement/disengagement based on distance to target.
        /// </summary>
        private void UpdateCombatEngagement()
        {
            if (targetPlayer == null || combatController == null)
                return;

            float sqrDistance = context.distanceToPlayer * context.distanceToPlayer;
            float sqrEngageDistance = engageDistance * engageDistance;
            float sqrDisengageDistance = (engageDistance * 1.5f) * (engageDistance * 1.5f);

            // Enter combat if player within engage distance
            if (!combatController.IsInCombat && sqrDistance <= sqrEngageDistance)
            {
                combatController.EnterCombat(targetPlayer);

                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} engaged {targetPlayer.name} in combat");
                }
            }

            // Exit combat if player too far
            if (combatController.IsInCombat && sqrDistance > sqrDisengageDistance)
            {
                combatController.ExitCombat();

                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} disengaged from combat (too far)");
                }
            }
        }

        /// <summary>
        /// Attempts to use a skill based on the current pattern node.
        /// </summary>
        private void TryUseSkill()
        {
            if (enableDebugLogs && context.selfCombatState == CombatState.Knockback)
            {
                Debug.Log($"[PatternExecutor] {gameObject.name} TryUseSkill() called while in Knockback! Current node: {currentNode?.nodeName}");
            }

            // Check if pattern node is ready
            if (!IsNodeReady())
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} pattern node '{currentNode.nodeName}' not ready - skipping skill");
                }
                return;
            }

            // Request attack permission if coordination enabled
            if (useCoordination && coordinator != null && !hasAttackSlot)
            {
                // Note: AICoordinator API requires SimpleTestAI, so we can't request slots directly yet
                // TODO: Refactor AICoordinator API to work with Transform or PatternExecutor
                // For now, skip coordination or require SimpleTestAI component
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[PatternExecutor] {gameObject.name} coordination enabled but API requires SimpleTestAI component");
                }
            }

            // Get skill from current node
            SkillType selectedSkill = GetCurrentSkill();

            // Check if can charge skill
            if (!skillSystem.CanChargeSkill(selectedSkill))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} cannot charge {selectedSkill} (insufficient stamina or wrong state)");
                }
                return;
            }

            // Check if in range before charging (prevents slow shuffle while charging)
            if (weaponController != null && targetPlayer != null)
            {
                float weaponRange = weaponController.WeaponData?.isRangedWeapon ?? false
                    ? weaponController.GetRangedRange()
                    : weaponController.GetMeleeRange();

                float distance = context.distanceToPlayer;

                // Don't start charging if still out of range - keep approaching at full speed
                if (distance > weaponRange)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PatternExecutor] {gameObject.name} waiting to reach range ({distance:F1} > {weaponRange:F1}) before charging {selectedSkill}");
                    }
                    return;
                }
            }

            // Start charging the skill
            if (selectedSkill == SkillType.Attack)
            {
                // Attack doesn't require charging - execute immediately
                skillSystem.ExecuteSkill(SkillType.Attack);

                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} executed {selectedSkill}");
                }
            }
            else
            {
                // Other skills require charging
                skillSystem.StartCharging(selectedSkill);

                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} charging {selectedSkill}");
                }

                // Start coroutine to execute when charged
                if (currentSkillCoroutine != null)
                {
                    StopCoroutine(currentSkillCoroutine);
                }
                currentSkillCoroutine = StartCoroutine(ExecuteSkillWhenCharged(selectedSkill));
            }

            // Set next skill time
            nextSkillTime = Time.time + skillCooldown + Random.Range(-randomVariance, randomVariance);
        }

        /// <summary>
        /// Coroutine that waits for skill to complete execution cycle.
        /// </summary>
        private System.Collections.IEnumerator ExecuteSkillWhenCharged(SkillType skillType)
        {
            float maxWaitTime = 5.0f;
            float startTime = Time.time;
            bool executionTriggered = false;

            // Wait for complete execution cycle
            while (skillSystem.CurrentState != SkillExecutionState.Uncharged &&
                   Time.time - startTime < maxWaitTime)
            {
                // Trigger execution when charged
                if (!executionTriggered &&
                    skillSystem.CurrentState == SkillExecutionState.Charged &&
                    skillSystem.CurrentSkill == skillType)
                {
                    skillSystem.ExecuteSkill(skillType);
                    executionTriggered = true;

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[PatternExecutor] {gameObject.name} executed {skillType}");
                    }
                }

                yield return null;
            }

            // Check if completed or timed out
            if (skillSystem.CurrentState != SkillExecutionState.Uncharged)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[PatternExecutor] {gameObject.name} skill {skillType} timed out in state {skillSystem.CurrentState} - cancelling");
                }
                skillSystem.CancelSkill();
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[PatternExecutor] {gameObject.name} skill {skillType} completed successfully");
            }

            currentSkillCoroutine = null;
        }

        /// <summary>
        /// Updates weapon capability flags based on current weapon.
        /// </summary>
        private void UpdateWeaponCapabilities()
        {
            if (weaponController != null && weaponController.WeaponData != null)
            {
                hasRangedWeapon = weaponController.WeaponData.isRangedWeapon;

                if (enableDebugLogs)
                {
                    Debug.Log($"[PatternExecutor] {gameObject.name} weapon capabilities: ranged={hasRangedWeapon}");
                }
            }
        }

        /// <summary>
        /// Cleanup coroutines and state.
        /// </summary>
        private void Cleanup()
        {
            if (currentSkillCoroutine != null)
            {
                StopCoroutine(currentSkillCoroutine);
                currentSkillCoroutine = null;
            }

            ReleaseAttackSlot();
        }

        /// <summary>
        /// Releases attack slot if held.
        /// </summary>
        private void ReleaseAttackSlot()
        {
            if (hasAttackSlot && coordinator != null)
            {
                // Note: AICoordinator API requires SimpleTestAI
                // TODO: Refactor to work with PatternExecutor directly
                hasAttackSlot = false;
            }
        }

        private void OnGUI()
        {
            if (!showDebugGUI || patternDefinition == null) return;

            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>Pattern Debug: {gameObject.name}</b>");
            GUILayout.Label($"Pattern: {patternDefinition.patternName}");
            GUILayout.Label($"Current Node: {currentNode?.nodeName ?? "None"}");
            GUILayout.Label($"Next Skill: {currentNode?.skillToUse}");
            GUILayout.Label($"Time in Node: {timeInCurrentNode:F1}s");

            GUILayout.Space(5);
            GUILayout.Label("<b>Hit Tracking:</b>");
            GUILayout.Label($"  Hits Taken: {hitsTaken}");
            GUILayout.Label($"  Hits Dealt: {hitsDealt}");

            GUILayout.Space(5);
            GUILayout.Label("<b>Context:</b>");
            GUILayout.Label($"  Health: {context.selfHealthPercentage:F0}%");
            GUILayout.Label($"  Stamina: {context.selfStamina:F0}");
            GUILayout.Label($"  Distance: {context.distanceToPlayer:F1}");
            GUILayout.Label($"  Player Charging: {context.isPlayerCharging}");

            if (currentNode != null && currentNode.transitions != null && currentNode.transitions.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("<b>Possible Transitions:</b>");
                foreach (var transition in currentNode.transitions)
                {
                    bool valid = transition.EvaluateConditions(context);
                    string status = valid ? "✓ READY" : "✗ WAITING";
                    GUILayout.Label($"  {status} → {transition.targetNodeName}");
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Visualizes current pattern state in scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (currentNode == null || !Application.isPlaying)
                return;

            // Draw current pattern state
            Vector3 labelPos = transform.position + Vector3.up * 3f;
            UnityEditor.Handles.Label(labelPos,
                $"Pattern: {patternDefinition?.patternName}\n" +
                $"Node: {currentNode.nodeName}\n" +
                $"Skill: {currentNode.skillToUse}\n" +
                $"Time: {timeInCurrentNode:F1}s",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.cyan },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });

            // Draw line to target player
            if (targetPlayer != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up, targetPlayer.position + Vector3.up);
            }
        }
        #endif
    }
}
