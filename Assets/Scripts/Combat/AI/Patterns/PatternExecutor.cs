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
    public class PatternExecutor : MonoBehaviour, IAIAgent
    {
        [Header("Pattern Configuration")]
        [SerializeField] private PatternDefinition patternDefinition;

        [Header("Target Tracking")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private float playerSearchCooldown = 1.0f;

        [Header("Combat Configuration")]
        [SerializeField] private float engageDistance = 3.0f;
        [SerializeField] private bool useCoordination = true;

        [Header("Weapon Swapping")]
        [SerializeField] private bool enableWeaponSwapping = false;
        [SerializeField] private PreferredRange preferPrimaryAtRange = PreferredRange.Either;
        [SerializeField] private PreferredRange preferSecondaryAtRange = PreferredRange.Either;
        [SerializeField] private float swapDistanceThreshold = 3.0f;
        [SerializeField] private float swapCooldown = 5.0f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDebugGUI = false;

        [Header("N+1 Combo System")]
        [SerializeField] private float nPlusOneChance = 0.4f; // 40% chance to attempt N+1 (adjust per archetype)
        [SerializeField] private SkillType[] preferredComboFinishers = new SkillType[] { SkillType.Smash, SkillType.Windmill };

        // Pattern state
        private PatternNode currentNode;
        private float timeInCurrentNode = 0f;
        private int hitsTaken = 0;
        private int hitsDealt = 0;
        private CombatState lastCombatState = CombatState.Idle;

        // Context for pattern evaluation
        private PatternEvaluationContext context;

        // Component references
        private SkillSystem skillSystem;
        private HealthSystem healthSystem;
        private StaminaSystem staminaSystem;
        private CombatController combatController;
        private WeaponController weaponController;
        private MovementController movementController;
        private ICombatStateValidator stateValidator;
        private float lastPlayerSearchTime = -999f;

        // Delegated component handlers
        private PatternMovementController movementHandler;
        private PatternWeaponManager weaponManager;
        private PatternCombatHandler combatHandler;

        // Public accessors for debugging
        public PatternNode CurrentNode => currentNode;
        public float TimeInCurrentNode => timeInCurrentNode;
        public int HitsTaken => hitsTaken;
        public int HitsDealt => hitsDealt;

        // IAIAgent interface implementation
        // Note: Removed IsNodeReady() check - coordinator only manages timing/capacity, not node readiness
        public bool IsReadyToAttack
        {
            get
            {
                bool ready = !combatHandler.HasAttackSlot && combatController.IsInCombat;
                if (enableDebugLogs && !ready)
                {
                    CombatLogger.LogPattern($"{gameObject.name} IsReadyToAttack=false (hasSlot={combatHandler.HasAttackSlot}, inCombat={combatController.IsInCombat})", CombatLogger.LogLevel.Debug);
                }
                return ready;
            }
        }
        public bool IsInCombat => combatController.IsInCombat;

        private void Awake()
        {
            // Cache component references
            skillSystem = GetComponent<SkillSystem>();
            healthSystem = GetComponent<HealthSystem>();
            staminaSystem = GetComponent<StaminaSystem>();
            combatController = GetComponent<CombatController>();
            weaponController = GetComponent<WeaponController>();
            movementController = GetComponent<MovementController>();

            // Get or add state validator
            stateValidator = GetComponent<ICombatStateValidator>();
            if (stateValidator == null)
            {
                stateValidator = gameObject.AddComponent<CombatStateValidator>();
            }

            // Initialize evaluation context
            context = new PatternEvaluationContext
            {
                skillSystem = skillSystem,
                weaponController = weaponController
            };

            // Initialize delegated handlers
            movementHandler = new PatternMovementController(
                movementController,
                weaponController,
                transform,
                enableDebugLogs);

            weaponManager = new PatternWeaponManager(
                weaponController,
                transform,
                enableWeaponSwapping,
                preferPrimaryAtRange,
                preferSecondaryAtRange,
                swapDistanceThreshold,
                swapCooldown,
                enableDebugLogs);

            combatHandler = new PatternCombatHandler(
                this,
                skillSystem,
                combatController,
                weaponController,
                transform,
                engageDistance,
                useCoordination,
                enableDebugLogs);
        }

        private void Start()
        {
            // Subscribe to death event
            if (healthSystem != null)
            {
                healthSystem.OnDied += OnDied;
            }

            // Initialize pattern
            if (patternDefinition != null)
            {
                currentNode = patternDefinition.GetStartingNode();

                // Roll initial random value
                context.randomValue = Random.value;

                if (currentNode != null && enableDebugLogs)
                {
                    CombatLogger.LogPattern($"{gameObject.name} initialized with pattern '{patternDefinition.patternName}' starting at node '{currentNode.nodeName}'");
                }
            }
            else
            {
                CombatLogger.LogPattern($"{gameObject.name} has no pattern assigned! AI will not function.", CombatLogger.LogLevel.Warning);
            }

            // Find player
            FindPlayer();

            // Initialize combat handler
            combatHandler.Initialize();

            // Check weapon capabilities
            weaponManager.UpdateWeaponCapabilities();

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

            // Register with coordinator
            combatHandler.RegisterWithCoordinator(this);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (healthSystem != null)
            {
                healthSystem.OnDamageReceived -= OnDamageReceived;
                healthSystem.OnDied -= OnDied;
            }

            if (weaponController != null)
            {
                weaponController.OnHitDealt -= OnHitDealt;
            }

            // Cleanup
            Cleanup();
        }

        private void OnDied(Transform killer)
        {
            // Stop all pattern execution and movement
            if (movementHandler != null)
            {
                movementHandler.StopMovement();
            }

            // Cancel any active skills
            if (skillSystem != null)
            {
                skillSystem.CancelSkill();
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogPattern($"{gameObject.name} died - stopping all AI behavior");
            }
        }

        private void OnDisable()
        {
            // Cleanup on disable as well
            Cleanup();
        }

        private void Update()
        {
            // Stop all AI behavior if dead
            if (healthSystem != null && !healthSystem.IsAlive)
                return;

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

            // Handle weapon swapping if enabled
            if (enableWeaponSwapping && targetPlayer != null)
            {
                weaponManager.ConsiderWeaponSwap(targetPlayer, context.distanceToPlayer);
            }

            // Handle combat engagement/disengagement
            bool justEnteredCombat = combatHandler.UpdateCombatEngagement(targetPlayer, context.distanceToPlayer);

            // If we just entered combat and current node wants to start charging, do it now
            // This handles the case where AI starts out of combat range
            if (justEnteredCombat && currentNode.startChargingSkill)
            {
                combatHandler.StartChargingSkill(currentNode.skillToUse, this, currentNode.telegraph);

                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"{gameObject.name} entered combat - node '{currentNode.nodeName}' started charging {currentNode.skillToUse}");
                }
            }

            // Check and release attack slot if skill completed
            combatHandler.CheckAndReleaseSlotIfComplete(this);

            // Check for pattern node transitions
            HandlePatternTransitions();

            // Pattern skill control: Execute charged skill if node requests it
            if (currentNode.executeChargedSkill && combatController.IsInCombat)
            {
                // Check if skill is charged and ready (use node's custom accuracy threshold for ranged attacks)
                if (combatHandler.IsSkillCharged(currentNode.skillToUse, currentNode.rangedAccuracyThreshold))
                {
                    combatHandler.ExecuteReadySkill(currentNode.skillToUse);

                    if (enableDebugLogs)
                    {
                        CombatLogger.LogPattern($"{gameObject.name} node '{currentNode.nodeName}' executed charged {currentNode.skillToUse}");
                    }
                }
            }

            // Execute movement behavior for current node
            // Movement will be automatically blocked by MovementController when canMove is false
            // This ensures movement input is always updated when possible
            movementHandler.ExecuteMovementBehavior(currentNode, targetPlayer, context);
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
                CombatLogger.LogPattern($"{gameObject.name} hit counters reset", CombatLogger.LogLevel.Debug);
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
                // Cancel any active skill if the current node requests it (respects cancelSkillOnExit flag)
                // This allows multi-node patterns to maintain charged skills across transitions
                if (currentNode.cancelSkillOnExit && skillSystem.CurrentState != SkillExecutionState.Uncharged)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogPattern($"{gameObject.name} cancelling {skillSystem.CurrentSkill} before forced transition");
                    }
                    skillSystem.CancelSkill();
                }

                currentNode = targetNode;
                timeInCurrentNode = 0f;

                // Reset movement state tracking (for RetreatFixedDistance)
                movementHandler.ResetRetreatState();

                // Roll new random value for this node
                context.randomValue = Random.value;

                // Pattern skill control: Start charging if node requests it
                // Note: Removed IsInCombat check to allow charging at game start
                if (currentNode.startChargingSkill)
                {
                    combatHandler.StartChargingSkill(currentNode.skillToUse, this, currentNode.telegraph);

                    if (enableDebugLogs)
                    {
                        CombatLogger.LogPattern($"{gameObject.name} forced transition - node '{currentNode.nodeName}' started charging {currentNode.skillToUse}");
                    }
                }

                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"{gameObject.name} forced transition to node '{nodeName}'");
                }
            }
            else
            {
                CombatLogger.LogPattern($"{gameObject.name} failed to force transition - node '{nodeName}' not found", CombatLogger.LogLevel.Warning);
            }
        }

        /// <summary>
        /// Handles all pattern transition logic including defensive interrupts and timeout fallbacks.
        /// </summary>
        private void HandlePatternTransitions()
        {
            bool canCheckTransitions = stateValidator.CanTransitionNode();
            bool canCheckDefensiveInterrupts = CanCheckDefensiveInterrupts();

            LogTransitionBlockedIfNeeded(canCheckTransitions, canCheckDefensiveInterrupts);

            if (!canCheckTransitions && !canCheckDefensiveInterrupts)
                return;

            PatternTransition validTransition = FindValidTransition(canCheckDefensiveInterrupts);

            if (validTransition != null)
            {
                TransitionToNode(validTransition);
            }
            else if (canCheckTransitions)
            {
                CheckTimeoutFallback();
            }
        }

        private bool CanCheckDefensiveInterrupts()
        {
            return (context.selfCombatState == CombatState.Knockback || context.selfCombatState == CombatState.Stunned) &&
                   skillSystem.CurrentState != SkillExecutionState.Uncharged;
        }

        private void LogTransitionBlockedIfNeeded(bool canCheckTransitions, bool canCheckDefensiveInterrupts)
        {
            bool shouldLog = enableDebugLogs && !canCheckTransitions && !canCheckDefensiveInterrupts &&
                           timeInCurrentNode > 1f && timeInCurrentNode < 1.05f;

            if (shouldLog)
            {
                CombatLogger.LogPattern($"{gameObject.name} transitions blocked - {stateValidator.GetStateDebugInfo()}", CombatLogger.LogLevel.Debug);
            }
        }

        private PatternTransition FindValidTransition(bool checkDefensiveInterrupts)
        {
            if (checkDefensiveInterrupts)
            {
                PatternTransition defensiveTransition = GetHighPriorityDefensiveTransition(context);
                if (defensiveTransition != null)
                {
                    CombatLogger.LogPattern($"{gameObject.name} INTERRUPTING skill for defensive transition: {defensiveTransition.targetNodeName}", CombatLogger.LogLevel.Warning);
                    skillSystem.CancelSkill();
                }
                return defensiveTransition;
            }

            PatternTransition normalTransition = currentNode.GetValidTransition(context, enableDebugLogs);
            if (enableDebugLogs && normalTransition != null)
            {
                CombatLogger.LogPattern($"{gameObject.name} found valid transition to '{normalTransition.targetNodeName}' (time: {timeInCurrentNode:F3}s, state: {skillSystem.CurrentState})", CombatLogger.LogLevel.Debug);
            }
            return normalTransition;
        }

        private void CheckTimeoutFallback()
        {
            bool hasTimeout = !string.IsNullOrEmpty(currentNode.fallbackNodeName) &&
                            currentNode.fallbackTimeout > 0f &&
                            timeInCurrentNode >= currentNode.fallbackTimeout;

            if (hasTimeout)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"{gameObject.name} node '{currentNode.nodeName}' timeout ({currentNode.fallbackTimeout}s) - transitioning to fallback '{currentNode.fallbackNodeName}'", CombatLogger.LogLevel.Warning);
                }
                ForceTransitionToNode(currentNode.fallbackNodeName);
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

            // Sort by priority (highest first)
            var sortedTransitions = new List<PatternTransition>(currentNode.transitions);
            sortedTransitions.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Return first high-priority transition whose conditions are met
            foreach (var transition in sortedTransitions)
            {
                // Only consider high-priority transitions (can interrupt skill execution)
                if (transition.priority < CombatConstants.DEFENSIVE_INTERRUPT_PRIORITY_THRESHOLD)
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
                CombatLogger.LogPattern($"{gameObject.name} transition failed - target node '{transition.targetNodeName}' not found", CombatLogger.LogLevel.Warning);
                return;
            }

            string previousNodeName = currentNode.nodeName;

            // Cancel any active skill if the current node requests it (respects cancelSkillOnExit flag)
            // This allows multi-node patterns to maintain charged skills across transitions
            if (currentNode.cancelSkillOnExit && skillSystem.CurrentState != SkillExecutionState.Uncharged)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"{gameObject.name} cancelling {skillSystem.CurrentSkill} before transition");
                }
                skillSystem.CancelSkill();
            }

            // Execute transition
            currentNode = targetNode;
            timeInCurrentNode = 0f;

            // Reset movement state tracking (for RetreatFixedDistance)
            movementHandler.ResetRetreatState();

            // Roll new random value for this node (for RandomChance conditions)
            context.randomValue = Random.value;

            // Reset hit counters if transition requests it
            if (transition.resetHitCounters)
            {
                ResetHitCounters();
            }

            // Pattern skill control: Start charging if node requests it
            // Note: Removed IsInCombat check to allow charging at game start
            if (currentNode.startChargingSkill)
            {
                combatHandler.StartChargingSkill(currentNode.skillToUse, this, currentNode.telegraph);

                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"{gameObject.name} node '{currentNode.nodeName}' started charging {currentNode.skillToUse}");
                }
            }

            // Start cooldown if transition requests it
            if (transition.startCooldownID > 0 && transition.cooldownDuration > 0f)
            {
                context.StartCooldown(transition.startCooldownID, transition.cooldownDuration);
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogPattern($"{gameObject.name} transitioned from '{previousNodeName}' to '{currentNode.nodeName}' (priority {transition.priority})");
            }
        }

        private void UpdateEvaluationContext()
        {
            UpdateSelfState();
            UpdateRandomValueForDefensiveStates();
            UpdatePlayerState();
        }

        private void UpdateSelfState()
        {
            context.selfHealthPercentage = healthSystem != null
                ? (healthSystem.CurrentHealth / (float)healthSystem.MaxHealth) * 100f
                : 100f;

            context.selfStamina = staminaSystem != null ? staminaSystem.CurrentStamina : 0f;
            context.hitsTaken = hitsTaken;
            context.hitsDealt = hitsDealt;
            context.timeInCurrentNode = timeInCurrentNode;
            context.selfCombatState = combatController != null ? combatController.CurrentCombatState : CombatState.Idle;
            context.selfSkillState = skillSystem != null ? skillSystem.CurrentState : SkillExecutionState.Uncharged;

            context.isCharging = context.selfSkillState == SkillExecutionState.Charging ||
                                context.selfSkillState == SkillExecutionState.Aiming;

            context.isExecuting = context.selfSkillState == SkillExecutionState.Startup ||
                                 context.selfSkillState == SkillExecutionState.Active ||
                                 context.selfSkillState == SkillExecutionState.Recovery;
        }

        private void UpdateRandomValueForDefensiveStates()
        {
            // Re-roll random value when entering defensive states for reactive decisions
            // This allows RandomChance conditions to work for interrupt-based transitions
            bool isInDefensiveState = context.selfCombatState == CombatState.Knockback ||
                                     context.selfCombatState == CombatState.Stunned;
            bool isNewDefensiveState = lastCombatState != context.selfCombatState;

            if (isInDefensiveState && isNewDefensiveState)
            {
                context.randomValue = Random.value;
            }

            lastCombatState = context.selfCombatState;
        }

        private void UpdatePlayerState()
        {
            if (targetPlayer == null)
            {
                SetDefaultPlayerState();
                return;
            }

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
                SetDefaultPlayerState();
            }
        }

        private void SetDefaultPlayerState()
        {
            context.distanceToPlayer = float.MaxValue;
            context.isPlayerCharging = false;
            context.playerSkill = SkillType.Attack;
            context.playerCombatState = CombatState.Idle;
        }

        private void FindPlayer()
        {
            // Find closest hostile target using faction system
            var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            float closestSqrDistance = float.MaxValue;
            Transform closestHostile = null;

            foreach (var combatant in combatants)
            {
                // Skip self
                if (combatant == combatController) continue;

                // Only target hostile factions
                if (!combatController.IsHostileTo(combatant)) continue;

                float sqrDist = (transform.position - combatant.transform.position).sqrMagnitude;
                if (sqrDist < closestSqrDistance)
                {
                    closestSqrDistance = sqrDist;
                    closestHostile = combatant.transform;
                }
            }

            targetPlayer = closestHostile;

            if (targetPlayer != null && enableDebugLogs)
            {
                CombatLogger.LogPattern($"{gameObject.name} found hostile target: {targetPlayer.name}");
            }
        }

        private void OnDamageReceived(int damage, Transform attacker)
        {
            hitsTaken++;

            if (enableDebugLogs)
            {
                CombatLogger.LogPattern($"{gameObject.name} took hit (total: {hitsTaken})", CombatLogger.LogLevel.Debug);
            }
        }

        private void OnHitDealt(Transform target)
        {
            hitsDealt++;

            if (enableDebugLogs)
            {
                CombatLogger.LogPattern($"{gameObject.name} dealt hit to {target.name} (total: {hitsDealt})", CombatLogger.LogLevel.Debug);
            }
        }

        /// <summary>
        /// Cleanup coroutines and state.
        /// </summary>
        private void Cleanup()
        {
            combatHandler.Cleanup(this);
        }

        // N+1 Combo System for AI

        /// <summary>
        /// Attempts to execute an N+1 combo extension if weapon controller has valid window.
        /// Returns true if N+1 was attempted (regardless of success).
        /// </summary>
        public bool TryAINPlusOneCombo()
        {
            // Check if weapon controller has valid N+1 window
            if (weaponController == null || !weaponController.IsInNPlusOneWindow)
            {
                return false;
            }

            // Roll chance to attempt N+1 (configurable per archetype)
            float roll = Random.value;
            if (roll >= nPlusOneChance)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogAI($"[AI N+1] {gameObject.name} rolled {roll:F2} (needed < {nPlusOneChance:F2}) - skipping N+1");
                }
                return false; // Didn't roll high enough
            }

            // Choose appropriate combo finisher
            SkillType finisher = ChooseComboFinisher();

            // Try to execute as N+1 combo
            bool success = skillSystem.TryExecuteSkillFromCombo(finisher);

            if (success)
            {
                CombatLogger.LogAI($"[AI N+1] {gameObject.name} executed N+1 combo with {finisher} " +
                                  $"(chance: {nPlusOneChance:P0}, window: {weaponController.CurrentStunProgress:P0})");
            }
            else if (enableDebugLogs)
            {
                CombatLogger.LogAI($"[AI N+1] {gameObject.name} failed N+1 attempt with {finisher} (not charged or no stamina)");
            }

            return true; // Attempted N+1 (even if failed)
        }

        /// <summary>
        /// Chooses an appropriate combo finisher skill based on situation and preferences.
        /// </summary>
        private SkillType ChooseComboFinisher()
        {
            // If no preferred finishers configured, default to Smash
            if (preferredComboFinishers == null || preferredComboFinishers.Length == 0)
            {
                return SkillType.Smash;
            }

            // Check if Windmill is available and randomly prefer it for variety
            // (In future, could check nearby enemy count if that gets added to context)
            bool hasWindmill = System.Array.Exists(preferredComboFinishers, skill => skill == SkillType.Windmill);
            if (hasWindmill && Random.value < 0.3f) // 30% chance to prefer AoE
            {
                return SkillType.Windmill;
            }

            // Otherwise, pick random from preferred finishers
            int randomIndex = Random.Range(0, preferredComboFinishers.Length);
            return preferredComboFinishers[randomIndex];
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
