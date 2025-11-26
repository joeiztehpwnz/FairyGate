using System;
using System.Collections;
using UnityEngine;

namespace FairyGate.Combat
{
    public class SkillSystem : MonoBehaviour, ISkillExecutor, ICombatUpdatable
    {
        [Header("Skill Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private bool isPlayerControlled = true;
        [SerializeField] private SkillExecutionState currentState = SkillExecutionState.Uncharged;
        [SerializeField] private SkillType currentSkill = SkillType.Attack;
        [SerializeField] private float chargeProgress = 0f;

        [Header("Input Keys")]
        [SerializeField] private KeyCode attackKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode defenseKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode counterKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode smashKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode windmillKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode rangedAttackKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode lungeKey = KeyCode.Alpha7;
        [SerializeField] private KeyCode cancelKey = KeyCode.Space;
        [SerializeField] private KeyCode weaponSwapKey = KeyCode.C;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showSkillGUI = true;

        // C# Events (replaces UnityEvents for performance)
        public event Action<SkillType> OnSkillCharged;
        public event Action<SkillType, bool> OnSkillExecuted; // skill, wasHit
        public event Action<SkillType> OnSkillCancelled;
        public event Action<SkillType, SkillExecutionState> OnSkillStateChanged; // For UI/visual feedback

        // Component references
        private WeaponController weaponController;
        private StaminaSystem staminaSystem;
        private MovementController movementController;
        private CombatController combatController;
        private StatusEffectManager statusEffectManager;
        private AccuracySystem accuracySystem;

        // State Pattern (Phase 1: Infrastructure)
        private SkillStateMachine stateMachine;

        // Skill timing
        private bool canAct = true;

        // Input buffering during CC
        private SkillType? bufferedSkill = null;
        private bool wasUnableToAct = false;

        // Defense block tracking (one-hit block system)
        private bool defenseBlockedHit = false;

        // Properties
        public SkillExecutionState CurrentState => currentState;
        public SkillType CurrentSkill
        {
            get => currentSkill;
            set => currentSkill = value; // Allow states to set current skill
        }
        public float ChargeProgress
        {
            get => chargeProgress;
            set => chargeProgress = Mathf.Clamp01(value); // Allow states to set charge progress
        }
        public bool LastRangedAttackHit { get; set; } // Allow states to set hit result
        public bool HasDefenseBlockedHit => defenseBlockedHit;

        // Public component accessors for states (Phase 1: Infrastructure)
        public MovementController MovementController => movementController;
        public StaminaSystem StaminaSystem => staminaSystem;
        public AccuracySystem AccuracySystem => accuracySystem;
        public StatusEffectManager StatusEffectManager => statusEffectManager;
        public WeaponController WeaponController => weaponController;
        public CombatController CombatController => combatController;
        public CharacterStats Stats => characterStats;
        public bool EnableDebugLogs => enableDebugLogs;

        // State machine accessor (Phase 1: Infrastructure)
        public SkillStateMachine StateMachine => stateMachine;

        // Event triggers for states (Phase 2: Allow states to fire events)
        public void TriggerSkillCharged(SkillType skillType)
        {
            OnSkillCharged?.Invoke(skillType);
        }

        public void TriggerSkillExecuted(SkillType skillType, bool wasSuccessful)
        {
            OnSkillExecuted?.Invoke(skillType, wasSuccessful);
        }

        public void TriggerSkillCancelled(SkillType skillType)
        {
            OnSkillCancelled?.Invoke(skillType);
        }

        private void Awake()
        {
            weaponController = GetComponent<WeaponController>();
            staminaSystem = GetComponent<StaminaSystem>();
            movementController = GetComponent<MovementController>();
            combatController = GetComponent<CombatController>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            accuracySystem = GetComponent<AccuracySystem>();

            if (characterStats == null)
            {
                CombatLogger.LogSystem($"SkillSystem on {gameObject.name} has no CharacterStats assigned. Using default values.", CombatLogger.LogLevel.Warning);
                characterStats = CharacterStats.CreateDefaultStats();
            }

            // Register with combat update manager
            CombatUpdateManager.Register(this);

            // Initialize state machine (Phase 1: Infrastructure)
            stateMachine = new SkillStateMachine(this);
        }

        private void OnDestroy()
        {
            // Unregister to prevent memory leaks
            CombatUpdateManager.Unregister(this);
        }

        // Renamed from Update() to CombatUpdate() for centralized update management
        public void CombatUpdate(float deltaTime)
        {
            // Update state machine
            stateMachine.Update(deltaTime);

            // Check if in a "locked" state (CC or Active phase)
            bool isLocked = !canAct || currentState == SkillExecutionState.Active;

            // Detect locked state ending (transition from locked → free)
            if (wasUnableToAct && !isLocked && bufferedSkill.HasValue)
            {
                ProcessBufferedSkill();
            }

            // Update locked state tracking for next frame
            wasUnableToAct = isLocked;

            if (isLocked)
            {
                // Can't act (CC'd or in Active phase) - buffer input instead
                BufferSkillInput();
                return;
            }

            HandleSkillInput();
        }

        private void HandleSkillInput()
        {
            // Only process keyboard input for player-controlled characters
            if (!isPlayerControlled) return;

            // Weapon swap input (can't swap during Active state)
            if (Input.GetKeyDown(weaponSwapKey))
            {
                if (currentState == SkillExecutionState.Active)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogWeapon($"{gameObject.name}: Cannot swap weapons during Active skill state");
                    }
                    return;
                }

                bool swapped = weaponController?.SwapWeapons() ?? false;
                if (swapped && enableDebugLogs)
                {
                    CombatLogger.LogWeapon($"{gameObject.name}: Swapped weapon successfully");
                }
                return;
            }

            // Cancel skill input
            if (Input.GetKeyDown(cancelKey))
            {
                if (currentState == SkillExecutionState.Aiming)
                {
                    CancelAim();
                    return;
                }

                CancelSkill();
                return;
            }

            // N+1 PRIORITY: Check for N+1 combo extension FIRST (before any other skill logic)
            // Detects both perfect timing (70-95%) and early attempts (<70%) with penalty
            if (weaponController != null && weaponController.CurrentStunProgress > 0f)
            {
                // Check each skill button for N+1 execution or early penalty
                if (Input.GetKeyDown(smashKey))
                {
                    if (weaponController.IsInNPlusOneWindow)
                    {
                        // Perfect timing - execute as N+1 combo
                        if (TryExecuteSkillFromCombo(SkillType.Smash))
                            return;
                    }
                    else if (weaponController.CurrentStunProgress < CombatConstants.N_PLUS_ONE_WINDOW_START)
                    {
                        // Too early - apply penalty and execute normally
                        HandleEarlyN1Attempt(SkillType.Smash);
                        return;
                    }
                    // If too late (>95%), fall through to normal execution
                }
                else if (Input.GetKeyDown(windmillKey))
                {
                    if (weaponController.IsInNPlusOneWindow)
                    {
                        if (TryExecuteSkillFromCombo(SkillType.Windmill))
                            return;
                    }
                    else if (weaponController.CurrentStunProgress < CombatConstants.N_PLUS_ONE_WINDOW_START)
                    {
                        HandleEarlyN1Attempt(SkillType.Windmill);
                        return;
                    }
                }
                else if (Input.GetKeyDown(counterKey))
                {
                    if (weaponController.IsInNPlusOneWindow)
                    {
                        if (TryExecuteSkillFromCombo(SkillType.Counter))
                            return;
                    }
                    else if (weaponController.CurrentStunProgress < CombatConstants.N_PLUS_ONE_WINDOW_START)
                    {
                        HandleEarlyN1Attempt(SkillType.Counter);
                        return;
                    }
                }
                else if (Input.GetKeyDown(defenseKey))
                {
                    if (weaponController.IsInNPlusOneWindow)
                    {
                        if (TryExecuteSkillFromCombo(SkillType.Defense))
                            return;
                    }
                    else if (weaponController.CurrentStunProgress < CombatConstants.N_PLUS_ONE_WINDOW_START)
                    {
                        HandleEarlyN1Attempt(SkillType.Defense);
                        return;
                    }
                }
                // Note: Attack already handles N+1 in its own section below
            }

            // RangedAttack firing input (if ranged attack is being aimed)
            if (currentState == SkillExecutionState.Aiming && currentSkill == SkillType.RangedAttack)
            {
                if (Input.GetKeyDown(rangedAttackKey))
                {
                    ExecuteSkill(SkillType.RangedAttack);
                    return;
                }
            }

            // Skill execution input (if skill is charged) - only for offensive skills
            // Defensive skills auto-execute after charging
            if (currentState == SkillExecutionState.Charged && !IsDefensiveSkill(currentSkill))
            {
                if (Input.GetKeyDown(GetSkillKey(currentSkill)))
                {
                    // N+1 System: Try combo extension first, fallback to normal execution
                    if (!TryExecuteSkillFromCombo(currentSkill))
                    {
                        ExecuteSkill(currentSkill);
                    }
                    return;
                }
            }

            // Skill charging/aiming input (if not currently busy)
            if (currentState == SkillExecutionState.Uncharged || currentState == SkillExecutionState.Charged)
            {
                SkillType? inputSkill = GetSkillFromInput();
                if (inputSkill.HasValue)
                {
                    if (currentSkill != inputSkill.Value || currentState == SkillExecutionState.Uncharged)
                    {
                        // Attack skill executes immediately without charging
                        if (inputSkill.Value == SkillType.Attack)
                        {
                            // N+1 System: Check for combo extension or early penalty
                            if (weaponController != null && weaponController.CurrentStunProgress > 0f)
                            {
                                if (weaponController.IsInNPlusOneWindow)
                                {
                                    // Perfect timing - execute as N+1 combo
                                    if (TryExecuteSkillFromCombo(SkillType.Attack))
                                        return;
                                }
                                else if (weaponController.CurrentStunProgress < CombatConstants.N_PLUS_ONE_WINDOW_START)
                                {
                                    // Too early - apply penalty and execute normally
                                    HandleEarlyN1Attempt(SkillType.Attack);
                                    return;
                                }
                            }
                            // Not in N+1 state or too late - execute normally
                            ExecuteSkill(SkillType.Attack);
                        }
                        // RangedAttack skill enters aiming state
                        else if (inputSkill.Value == SkillType.RangedAttack)
                        {
                            StartAiming(SkillType.RangedAttack);
                        }
                        // Other skills charge normally
                        else
                        {
                            StartCharging(inputSkill.Value);
                        }
                    }
                }
            }
        }

        private void BufferSkillInput()
        {
            // Only process keyboard input for player-controlled characters
            if (!isPlayerControlled) return;

            // Detect skill input and buffer it
            SkillType? inputSkill = GetSkillFromInput();
            if (inputSkill.HasValue)
            {
                // Store buffered skill (replaces previous buffer if any)
                bufferedSkill = inputSkill.Value;

                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"{gameObject.name} buffered skill: {bufferedSkill.Value} (during CC)", CombatLogger.LogLevel.Debug);
                }
            }
        }

        private void ProcessBufferedSkill()
        {
            if (!bufferedSkill.HasValue) return;

            SkillType skillToExecute = bufferedSkill.Value;
            bufferedSkill = null; // Clear buffer

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} executing buffered skill: {skillToExecute}");
            }

            // Execute buffered skill based on type
            if (skillToExecute == SkillType.Attack)
            {
                // Attack executes immediately
                ExecuteSkill(SkillType.Attack);
            }
            else if (skillToExecute == SkillType.RangedAttack)
            {
                // RangedAttack enters aiming state
                StartAiming(SkillType.RangedAttack);
            }
            else
            {
                // Other skills require charging
                StartCharging(skillToExecute);
            }
        }

        public bool CanChargeSkill(SkillType skillType)
        {
            if (!combatController.IsInCombat) return false;
            if (!canAct) return false;
            if (currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Charged) return false;

            // LUNGE RANGE CHECK: Must be in mid-range sweet spot (2.0-4.0 units)
            if (skillType == SkillType.Lunge)
            {
                if (combatController.CurrentTarget == null)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogSkill($"{gameObject.name} cannot lunge: no target", CombatLogger.LogLevel.Debug);
                    }
                    return false;
                }

                float distance = Vector3.Distance(transform.position, combatController.CurrentTarget.position);

                if (distance < CombatConstants.LUNGE_MIN_RANGE)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogSkill($"{gameObject.name} too close for Lunge ({distance:F1} < {CombatConstants.LUNGE_MIN_RANGE})", CombatLogger.LogLevel.Debug);
                    }
                    return false;
                }

                if (distance > CombatConstants.LUNGE_MAX_RANGE)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogSkill($"{gameObject.name} too far for Lunge ({distance:F1} > {CombatConstants.LUNGE_MAX_RANGE})", CombatLogger.LogLevel.Debug);
                    }
                    return false;
                }
            }

            // Check stamina requirements
            int requiredStamina = GetSkillStaminaCost(skillType);
            return staminaSystem.HasStaminaFor(requiredStamina);
        }

        public bool CanExecuteSkill(SkillType skillType)
        {
            // Attack can be executed immediately if basic conditions are met
            if (skillType == SkillType.Attack)
            {
                return CanExecuteAttack();
            }

            // RangedAttack can be executed when aiming
            if (skillType == SkillType.RangedAttack)
            {
                return currentSkill == skillType && currentState == SkillExecutionState.Aiming;
            }

            // Other skills require charging first
            return currentSkill == skillType && currentState == SkillExecutionState.Charged;
        }

        public bool CanExecuteAttack()
        {
            if (!combatController.IsInCombat) return false;
            if (!canAct) return false;
            if (currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Charged) return false;

            // Check stamina requirements
            int requiredStamina = GetSkillStaminaCost(SkillType.Attack);
            return staminaSystem.HasStaminaFor(requiredStamina);
        }

        public void SetState(SkillExecutionState newState)
        {
            if (currentState != newState)
            {
                SkillExecutionState previousState = currentState;
                currentState = newState;
                OnSkillStateChanged?.Invoke(currentSkill, currentState);

                // Melee trail VFX control
                HandleMeleeTrailTransition(previousState, newState);
            }
        }

        private void HandleMeleeTrailTransition(SkillExecutionState previousState, SkillExecutionState newState)
        {
            // Only draw trails for melee offensive skills
            bool isMeleeOffensiveSkill = currentSkill == SkillType.Attack ||
                                         currentSkill == SkillType.Smash ||
                                         currentSkill == SkillType.Windmill ||
                                         currentSkill == SkillType.Lunge;

            if (!isMeleeOffensiveSkill) return;

            // Draw slash line when entering Active state
            if (newState == SkillExecutionState.Active && weaponController != null)
            {
                // Calculate trail duration based on skill execution time
                // Trail should persist through Active + Recovery states for full visual feedback
                float activeTime = SpeedResolver.CalculateExecutionTime(currentSkill, weaponController.WeaponData, SkillExecutionState.Active);
                float recoveryTime = SpeedResolver.CalculateExecutionTime(currentSkill, weaponController.WeaponData, SkillExecutionState.Recovery);
                float duration = activeTime + recoveryTime;

                // Get target from combat controller
                Transform target = combatController?.CurrentTarget;

                weaponController.DrawMeleeSlash(currentSkill, duration, target);

                if (enableDebugLogs)
                {
                    CombatLogger.LogWeapon($"{gameObject.name}: Drew {currentSkill} slash toward {(target != null ? target.name : "no target")} (active: {activeTime:F2}s, recovery: {recoveryTime:F2}s, total: {duration:F2}s)", CombatLogger.LogLevel.Debug);
                }
            }
        }

        public void StartCharging(SkillType skillType)
        {
            if (!CanChargeSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"{gameObject.name} cannot charge skill {skillType} (not in combat or insufficient stamina)", CombatLogger.LogLevel.Debug);
                }
                return;
            }

            // Cancel current skill if switching
            if (currentState != SkillExecutionState.Uncharged)
            {
                CancelSkill();
            }

            currentSkill = skillType;

            // Reset Defense block flag when charging Defense
            if (skillType == SkillType.Defense)
            {
                defenseBlockedHit = false;
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} started charging {skillType}");
            }

            // Phase 2: Use state machine if enabled, otherwise use coroutines
            // State pattern approach
            stateMachine.TransitionTo(new ChargingState(this, skillType));
        }

        public void ExecuteSkill(SkillType skillType)
        {
            if (!CanExecuteSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"{gameObject.name} cannot execute skill {skillType} (not charged or wrong skill)", CombatLogger.LogLevel.Debug);
                }
                return;
            }

            // Classic Mabinogi: Check range for offensive skills and auto-move to target if needed
            if (IsOffensiveSkill(skillType) && combatController != null && combatController.CurrentTarget != null)
            {
                float range = weaponController.GetSkillRange(skillType);
                float distance = Vector3.Distance(transform.position, combatController.CurrentTarget.position);

                if (distance > range)
                {
                    // Out of range - transition to ApproachingState (auto-movement)
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogSkill($"{gameObject.name} out of range for {skillType} ({distance:F1} > {range:F1}) - transitioning to ApproachingState");
                    }

                    // Transition to ApproachingState which will auto-move and execute when in range
                    stateMachine.TransitionTo(new ApproachingState(this, skillType));
                    return;
                }
            }

            // In range or no range check needed - execute immediately
            ExecuteSkillImmediately(skillType);
        }

        private void ExecuteSkillImmediately(SkillType skillType)
        {
            // Consume stamina
            int staminaCost = GetSkillStaminaCost(skillType);
            if (!staminaSystem.ConsumeStamina(staminaCost))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogStamina($"{gameObject.name} insufficient stamina to execute {skillType}", CombatLogger.LogLevel.Debug);
                }
                return;
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} executing {skillType}");
            }

            // Phase 2/5: Use state machine if enabled, otherwise use coroutines
            // Phase 5: RangedAttack goes directly from Aiming to Active (skip Startup)
            if (skillType == SkillType.RangedAttack && currentState == SkillExecutionState.Aiming)
            {
                stateMachine.TransitionTo(new ActiveState(this, skillType));
            }
            else
            {
                // Standard flow: transition from Charged to Startup
                stateMachine.TransitionTo(new StartupState(this, skillType));
            }
        }

        /// <summary>
        /// Attempts to execute a skill as an N+1 combo extension during enemy stun window.
        /// Returns true if successfully executed as N+1, false otherwise.
        /// N+1 execution bypasses charging requirements - skills execute instantly during the window.
        /// </summary>
        public bool TryExecuteSkillFromCombo(SkillType skillType)
        {
            // 1. Check if weapon controller has valid N+1 window
            if (weaponController == null || !weaponController.IsInNPlusOneWindow)
            {
                return false; // Not in valid N+1 window
            }

            // 2. Check stamina availability (N+1 bypasses charging requirement)
            int requiredStamina = GetSkillStaminaCost(skillType);
            if (!staminaSystem.HasStaminaFor(requiredStamina))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"[N+1] {gameObject.name} cannot execute {skillType} - insufficient stamina", CombatLogger.LogLevel.Debug);
                }
                return false; // Not enough stamina
            }

            // 3. N+1 INSTANT EXECUTION: Skip charging, execute immediately
            // Force the skill state to allow execution
            currentSkill = skillType;
            currentState = SkillExecutionState.Charged; // Pretend it's charged for execution

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"[N+1 COMBO] {gameObject.name} executing {skillType} as combo extension (instant, no charge required)");
            }

            // 4. Execute skill immediately as combo extension
            ExecuteSkill(skillType);

            // 5. Reset combo counter on knockdown meter tracker
            var knockdownTracker = GetComponent<KnockdownMeterTracker>();
            if (knockdownTracker != null)
            {
                knockdownTracker.ResetComboCounter();
            }

            // 6. Notify weapon controller that N+1 was executed
            weaponController.OnNPlusOneExecuted();

            // 6. Log successful N+1 execution
            CombatLogger.LogCombat($"[N+1 COMBO] {gameObject.name} extended combo with {skillType} " +
                                  $"at {weaponController.CurrentStunProgress:P0} of stun window");

            return true;
        }

        /// <summary>
        /// Handles early N+1 timing attempts (pressed before 70% window opens).
        /// Applies stamina penalty and executes skill normally with full startup.
        /// N+1 System: Punishes button mashing while still allowing skill execution.
        /// </summary>
        private void HandleEarlyN1Attempt(SkillType skillType)
        {
            // Calculate stamina penalty (50% of skill cost)
            int fullCost = GetSkillStaminaCost(skillType);
            int penaltyCost = Mathf.CeilToInt(fullCost * 0.5f);

            // Apply stamina penalty
            if (staminaSystem != null && staminaSystem.HasStaminaFor(penaltyCost))
            {
                staminaSystem.ConsumeStamina(penaltyCost);
            }

            // Log missed timing
            float missedByPercent = (CombatConstants.N_PLUS_ONE_WINDOW_START - weaponController.CurrentStunProgress) * 100f;
            CombatLogger.LogCombat($"[N+1 MISS] {gameObject.name} pressed {skillType} too early " +
                                  $"({weaponController.CurrentStunProgress:P0} < 70%, missed by {missedByPercent:F0}%) " +
                                  $"- Penalty: {penaltyCost} stamina");

            // Execute skill normally with full startup (if player can charge it)
            if (CanChargeSkill(skillType))
            {
                StartCharging(skillType);
            }
        }

        /// <summary>
        /// Cancels the currently charging or executing skill.
        /// Implements ISkillExecutor.CancelSkill()
        /// </summary>
        public void CancelSkill()
        {
            if (currentState == SkillExecutionState.Uncharged)
            {
                return; // Nothing to cancel
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} cancelling skill {currentSkill} (was in state: {currentState})");
            }

            // Reset state
            SkillType cancelledSkill = currentSkill;
            currentState = SkillExecutionState.Uncharged;
            currentSkill = SkillType.Attack; // Reset to default
            chargeProgress = 0f;

            // Stop any active coroutines
            StopAllCoroutines();

            // Transition state machine to uncharged
            if (stateMachine != null)
            {
                stateMachine.TransitionTo(new UnchargedState(this, SkillType.Attack));
            }

            // Enable movement
            if (movementController != null)
            {
                movementController.EnableMovement();
            }

            // Fire cancellation event
            OnSkillCancelled?.Invoke(cancelledSkill);
            OnSkillStateChanged?.Invoke(cancelledSkill, SkillExecutionState.Uncharged);
        }

        /// <summary>
        /// Starts aiming for RangedAttack skill.
        /// </summary>
        public void StartAiming(SkillType skillType)
        {
            if (skillType != SkillType.RangedAttack)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"{gameObject.name} StartAiming called for non-ranged skill {skillType}", CombatLogger.LogLevel.Warning);
                }
                return;
            }

            if (!combatController.IsInCombat)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"{gameObject.name} cannot aim: not in combat", CombatLogger.LogLevel.Debug);
                }
                return;
            }

            // Check stamina requirements
            int requiredStamina = GetSkillStaminaCost(SkillType.RangedAttack);
            if (!staminaSystem.HasStaminaFor(requiredStamina))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogStamina($"{gameObject.name} cannot aim RangedAttack (insufficient stamina)", CombatLogger.LogLevel.Debug);
                }
                return;
            }

            // Cancel current skill if switching
            if (currentState != SkillExecutionState.Uncharged)
            {
                CancelSkill();
            }

            currentSkill = SkillType.RangedAttack;

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} started aiming RangedAttack");
            }

            // Start accuracy tracking
            if (accuracySystem != null && combatController != null && combatController.CurrentTarget != null)
            {
                accuracySystem.StartAiming(combatController.CurrentTarget);
            }

            // Transition to aiming state
            stateMachine.TransitionTo(new AimingState(this, SkillType.RangedAttack));
        }

        /// <summary>
        /// Cancels aiming for RangedAttack skill.
        /// </summary>
        public void CancelAim()
        {
            if (currentState != SkillExecutionState.Aiming || currentSkill != SkillType.RangedAttack)
            {
                return; // Not aiming
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} cancelled RangedAttack aim");
            }

            // Stop accuracy tracking
            if (accuracySystem != null)
            {
                accuracySystem.StopAiming();
            }

            // Cancel skill normally
            CancelSkill();
        }

        /// <summary>
        /// Returns the stamina cost for a given skill type.
        /// </summary>
        public int GetSkillStaminaCost(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.Attack => CombatConstants.ATTACK_STAMINA_COST,
                SkillType.Defense => CombatConstants.DEFENSE_STAMINA_COST,
                SkillType.Counter => CombatConstants.COUNTER_STAMINA_COST,
                SkillType.Smash => CombatConstants.SMASH_STAMINA_COST,
                SkillType.Windmill => CombatConstants.WINDMILL_STAMINA_COST,
                SkillType.RangedAttack => CombatConstants.RANGED_ATTACK_STAMINA_COST,
                SkillType.Lunge => CombatConstants.LUNGE_STAMINA_COST,
                _ => 0
            };
        }

        /// <summary>
        /// Returns the KeyCode for a given skill type.
        /// </summary>
        public KeyCode GetSkillKey(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.Attack => attackKey,
                SkillType.Defense => defenseKey,
                SkillType.Counter => counterKey,
                SkillType.Smash => smashKey,
                SkillType.Windmill => windmillKey,
                SkillType.RangedAttack => rangedAttackKey,
                SkillType.Lunge => lungeKey,
                _ => KeyCode.None
            };
        }

        /// <summary>
        /// Gets the skill type from keyboard input. Returns null if no skill key is pressed.
        /// </summary>
        private SkillType? GetSkillFromInput()
        {
            if (Input.GetKeyDown(attackKey)) return SkillType.Attack;
            if (Input.GetKeyDown(defenseKey)) return SkillType.Defense;
            if (Input.GetKeyDown(counterKey)) return SkillType.Counter;
            if (Input.GetKeyDown(smashKey)) return SkillType.Smash;
            if (Input.GetKeyDown(windmillKey)) return SkillType.Windmill;
            if (Input.GetKeyDown(rangedAttackKey)) return SkillType.RangedAttack;
            if (Input.GetKeyDown(lungeKey)) return SkillType.Lunge;

            return null;
        }

        /// <summary>
        /// Returns true if the skill type is defensive (Defense, Counter).
        /// </summary>
        public bool IsDefensiveSkill(SkillType skillType)
        {
            return SpeedResolver.IsDefensiveSkill(skillType);
        }

        /// <summary>
        /// Returns true if the skill type is offensive.
        /// </summary>
        public bool IsOffensiveSkill(SkillType skillType)
        {
            return SpeedResolver.IsOffensiveSkill(skillType);
        }

        /// <summary>
        /// Marks that Defense skill has blocked a hit (one-hit block system).
        /// </summary>
        public void MarkDefenseBlocked()
        {
            defenseBlockedHit = true;

            if (enableDebugLogs)
            {
                CombatLogger.LogSkill($"{gameObject.name} Defense blocked a hit");
            }
        }

        /// <summary>
        /// Forces transition to Recovery state (used for interrupting skills).
        /// </summary>
        public void ForceTransitionToRecovery()
        {
            if (currentState == SkillExecutionState.Active ||
                currentState == SkillExecutionState.Startup ||
                currentState == SkillExecutionState.Waiting)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogSkill($"{gameObject.name} forcing transition to Recovery state from {currentState}");
                }

                stateMachine.TransitionTo(new RecoveryState(this, currentSkill));
            }
        }

        /// <summary>
        /// Sets whether the character can act (used for CC effects).
        /// </summary>
        public void SetCanAct(bool canActValue)
        {
            canAct = canActValue;

            if (enableDebugLogs)
            {
                CombatLogger.LogSystem($"{gameObject.name} canAct set to {canActValue}", CombatLogger.LogLevel.Debug);
            }
        }

        /// <summary>
        /// Draws a ranged attack trail visual from source to target position.
        /// </summary>
        public void DrawRangedAttackTrail(Vector3 sourcePosition, Vector3 targetPosition, bool isHit)
        {
            if (weaponController != null)
            {
                weaponController.DrawRangedAttackTrail(sourcePosition, targetPosition, isHit);
            }
        }

        /// <summary>
        /// Enables character movement (wrapper for MovementController).
        /// </summary>
        public void EnableMovement()
        {
            if (movementController != null)
            {
                movementController.SetCanMove(true);
            }
        }
    }
}

