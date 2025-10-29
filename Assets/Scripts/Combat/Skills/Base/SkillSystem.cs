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

        // Skill timing
        private Coroutine currentSkillCoroutine;
        private bool canAct = true;

        // Input buffering during CC
        private SkillType? bufferedSkill = null;
        private bool wasUnableToAct = false;

        // Defense block tracking (one-hit block system)
        private bool defenseBlockedHit = false;

        // Properties
        public SkillExecutionState CurrentState => currentState;
        public SkillType CurrentSkill => currentSkill;
        public float ChargeProgress => chargeProgress;
        public bool LastRangedAttackHit { get; private set; }
        public bool HasDefenseBlockedHit => defenseBlockedHit;

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
                Debug.LogWarning($"SkillSystem on {gameObject.name} has no CharacterStats assigned. Using default values.");
                characterStats = CharacterStats.CreateDefaultStats();
            }

            // Register with combat update manager
            CombatUpdateManager.Register(this);
        }

        private void OnDestroy()
        {
            // Unregister to prevent memory leaks
            CombatUpdateManager.Unregister(this);
        }

        // Renamed from Update() to CombatUpdate() for centralized update management
        public void CombatUpdate(float deltaTime)
        {
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
                    ExecuteSkill(currentSkill);
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
                    Debug.Log($"{gameObject.name} buffered skill: {bufferedSkill.Value} (during CC)");
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
                Debug.Log($"{gameObject.name} executing buffered skill: {skillToExecute}");
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
                        Debug.Log($"{gameObject.name} cannot lunge: no target");
                    }
                    return false;
                }

                float distance = Vector3.Distance(transform.position, combatController.CurrentTarget.position);

                if (distance < CombatConstants.LUNGE_MIN_RANGE)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} too close for Lunge ({distance:F1} < {CombatConstants.LUNGE_MIN_RANGE})");
                    }
                    return false;
                }

                if (distance > CombatConstants.LUNGE_MAX_RANGE)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} too far for Lunge ({distance:F1} > {CombatConstants.LUNGE_MAX_RANGE})");
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

        private void SetState(SkillExecutionState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnSkillStateChanged?.Invoke(currentSkill, currentState);
            }
        }

        public void StartCharging(SkillType skillType)
        {
            if (!CanChargeSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot charge skill {skillType} (not in combat or insufficient stamina)");
                }
                return;
            }

            // Cancel current skill if switching
            if (currentState != SkillExecutionState.Uncharged)
            {
                CancelSkill();
            }

            currentSkill = skillType;
            SetState(SkillExecutionState.Charging);
            chargeProgress = 0f;

            // Reset Defense block flag when charging Defense
            if (skillType == SkillType.Defense)
            {
                defenseBlockedHit = false;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} started charging {skillType}");
            }

            // Apply movement restrictions
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            // Start charging coroutine
            currentSkillCoroutine = StartCoroutine(ChargeSkill(skillType));
        }

        public void ExecuteSkill(SkillType skillType)
        {
            if (!CanExecuteSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot execute skill {skillType} (not charged or wrong skill)");
                }
                return;
            }

            // Consume stamina
            int staminaCost = GetSkillStaminaCost(skillType);
            if (!staminaSystem.ConsumeStamina(staminaCost))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} insufficient stamina to execute {skillType}");
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} executing {skillType}");
            }

            // Start execution coroutine
            currentSkillCoroutine = StartCoroutine(ExecuteSkillCoroutine(skillType));
        }

        public void CancelSkill()
        {
            if (currentState == SkillExecutionState.Uncharged) return;

            SkillType skilltToCancel = currentSkill;

            // Cannot cancel during active frames
            if (currentState == SkillExecutionState.Active)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot cancel {skilltToCancel} during active frames");
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} cancelled {skilltToCancel}");
            }

            // Stop current coroutine
            if (currentSkillCoroutine != null)
            {
                StopCoroutine(currentSkillCoroutine);
                currentSkillCoroutine = null;
            }

            // Reset skill state
            SetState(SkillExecutionState.Uncharged);
            chargeProgress = 0f;

            // Reset movement restrictions
            movementController.SetMovementModifier(1f);

            OnSkillCancelled?.Invoke(skilltToCancel);
        }

        private void StartAiming(SkillType skillType)
        {
            if (skillType != SkillType.RangedAttack)
            {
                Debug.LogWarning($"StartAiming called with non-ranged skill: {skillType}");
                return;
            }

            if (!combatController.IsInCombat)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot aim: not in combat");
                return;
            }

            if (combatController.CurrentTarget == null)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot aim: no target");
                return;
            }

            // STAMINA CHECK MOVED HERE (before aiming starts)
            int requiredStamina = GetSkillStaminaCost(SkillType.RangedAttack);
            if (!staminaSystem.HasStaminaFor(requiredStamina))
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot aim: insufficient stamina");
                return;
            }

            // Check if target in range (use weapon range)
            float weaponRange = weaponController.WeaponData != null
                ? weaponController.WeaponData.range
                : CombatConstants.RANGED_ATTACK_BASE_RANGE;

            float distanceToTarget = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
            if (distanceToTarget > weaponRange)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot aim: target out of range ({distanceToTarget:F1} > {weaponRange})");
                return;
            }

            currentSkill = skillType;
            SetState(SkillExecutionState.Aiming);

            // Start accuracy tracking
            if (accuracySystem != null)
                accuracySystem.StartAiming(combatController.CurrentTarget);

            // Apply movement restriction
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} started aiming RangedAttack");
        }

        private void CancelAim()
        {
            if (currentState != SkillExecutionState.Aiming) return;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} cancelled RangedAttack aim");

            if (accuracySystem != null)
                accuracySystem.StopAiming();

            SetState(SkillExecutionState.Uncharged);
            currentSkill = SkillType.Attack;
            movementController.SetMovementModifier(1f);
        }

        private IEnumerator ChargeSkill(SkillType skillType)
        {
            float chargeTime = CalculateChargeTime(skillType);
            float elapsed = 0f;

            while (elapsed < chargeTime)
            {
                elapsed += Time.deltaTime;
                chargeProgress = elapsed / chargeTime;

                // Check for interruption
                if (statusEffectManager.IsKnockedDown)
                {
                    // Skill charging is paused during knockdown, but not reset
                    yield return new WaitUntil(() => !statusEffectManager.IsKnockedDown);
                }

                yield return null;
            }

            // Skill fully charged
            SetState(SkillExecutionState.Charged);
            chargeProgress = 1f;

            // Apply movement restrictions for charged state
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} {skillType} fully charged and ready to execute");
            }

            OnSkillCharged?.Invoke(skillType);

            // Auto-execute defensive skills after charging
            if (IsDefensiveSkill(skillType))
            {
                ExecuteSkill(skillType);
            }
        }

        private IEnumerator ExecuteSkillCoroutine(SkillType skillType)
        {
            // SPECIAL HANDLING FOR RANGED ATTACK
            if (skillType == SkillType.RangedAttack)
            {
                // RangedAttack uses custom flow: Aiming → Fire → Recovery
                yield return StartCoroutine(ExecuteRangedAttackCoroutine());
                yield break;
            }

            // STANDARD FLOW FOR OTHER SKILLS
            // Startup phase
            SetState(SkillExecutionState.Startup);
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            float startupTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Startup);
            yield return new WaitForSeconds(startupTime);

            // Active phase (uncancellable)
            SetState(SkillExecutionState.Active);
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            // Process skill effect during active phase
            bool skillSuccessful = ProcessSkillExecution(skillType);

            float activeTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Active);

            // Handle waiting state for defensive skills
            if (IsDefensiveSkill(skillType))
            {
                SetState(SkillExecutionState.Waiting);
                movementController.ApplySkillMovementRestriction(skillType, currentState);

                // Defensive skills enter waiting state
                yield return StartCoroutine(HandleDefensiveWaitingState(skillType));
            }
            else
            {
                // Offensive skills have fixed active time
                yield return new WaitForSeconds(activeTime);
            }

            // Recovery phase
            SetState(SkillExecutionState.Recovery);
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            float recoveryTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Recovery);
            yield return new WaitForSeconds(recoveryTime);

            // Skill complete
            SetState(SkillExecutionState.Uncharged);
            currentSkill = SkillType.Attack; // Reset to default
            chargeProgress = 0f;

            // Reset movement restrictions
            movementController.SetMovementModifier(1f);

            OnSkillExecuted?.Invoke(skillType, skillSuccessful);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} {skillType} execution complete (success: {skillSuccessful})");
            }
        }

        private IEnumerator ExecuteRangedAttackCoroutine()
        {
            // Validation checks
            if (currentState != SkillExecutionState.Aiming)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot fire: not aiming (state: {currentState})");
                yield break;
            }

            if (combatController.CurrentTarget == null)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot fire: target lost");
                CancelAim();
                yield break;
            }

            // Range check (use weapon range)
            float weaponRange = weaponController.WeaponData != null
                ? weaponController.WeaponData.range
                : CombatConstants.RANGED_ATTACK_BASE_RANGE;

            float distanceToTarget = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
            if (distanceToTarget > weaponRange)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} cannot fire: target out of range");
                CancelAim();
                yield break;
            }

            // Consume stamina
            int staminaCost = GetSkillStaminaCost(SkillType.RangedAttack);
            if (!staminaSystem.ConsumeStamina(staminaCost))
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} insufficient stamina to fire RangedAttack");
                CancelAim();
                yield break;
            }

            // Enter Active state (brief, for interaction processing)
            SetState(SkillExecutionState.Active);
            movementController.SetMovementModifier(0f);

            // Roll hit chance
            bool isHit = accuracySystem != null ? accuracySystem.RollHitChance() : false;
            LastRangedAttackHit = isHit; // Store for interaction manager to check

            if (enableDebugLogs)
            {
                float accuracy = accuracySystem != null ? accuracySystem.CurrentAccuracy : 0f;
                Debug.Log($"{gameObject.name} fired RangedAttack at {accuracy:F1}% accuracy → {(isHit ? "HIT" : "MISS")}");
            }

            // ALWAYS process through interaction manager (even on miss)
            // This allows defensive skills to respond properly
            CombatInteractionManager.Instance?.ProcessSkillExecution(this, SkillType.RangedAttack);

            // Show visual trail based on hit/miss
            if (isHit)
            {
                // HIT: Show hit trail (yellow → red)
                DrawRangedAttackTrail(transform.position, combatController.CurrentTarget.position + Vector3.up * 1f, true);
            }
            else
            {
                // MISS: Show miss trail (yellow → gray)
                Vector3 missPosition = accuracySystem != null
                    ? accuracySystem.CalculateMissPosition()
                    : combatController.CurrentTarget.position;

                DrawRangedAttackTrail(transform.position, missPosition, false);
            }

            // Stop aiming
            if (accuracySystem != null)
                accuracySystem.StopAiming();

            // Brief active time for interaction processing
            yield return new WaitForSeconds(0.1f);

            // Recovery phase
            SetState(SkillExecutionState.Recovery);
            movementController.SetMovementModifier(0f);

            float recoveryTime = CombatConstants.RANGED_ATTACK_RECOVERY_TIME;

            // Scale recovery by weapon speed (faster weapons = faster recovery)
            if (weaponController.WeaponData != null)
            {
                recoveryTime = recoveryTime / weaponController.WeaponData.speed;
            }

            yield return new WaitForSeconds(recoveryTime);

            // Skill complete
            SetState(SkillExecutionState.Uncharged);
            currentSkill = SkillType.Attack;
            chargeProgress = 0f;
            movementController.SetMovementModifier(1f);

            OnSkillExecuted?.Invoke(SkillType.RangedAttack, isHit);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} RangedAttack execution complete (hit: {isHit})");
            }
        }

        private IEnumerator HandleDefensiveWaitingState(SkillType skillType)
        {
            // Defensive skills wait for incoming attacks or manual cancellation
            while (currentState == SkillExecutionState.Waiting)
            {
                // Apply stamina drain during waiting
                float drainRate = skillType == SkillType.Defense ?
                    CombatConstants.DEFENSE_STAMINA_DRAIN :
                    CombatConstants.COUNTER_STAMINA_DRAIN;

                staminaSystem.DrainStamina(drainRate, Time.deltaTime);

                yield return null;
            }
        }

        private bool ProcessSkillExecution(SkillType skillType)
        {
            // LUNGE: Perform dash movement toward target
            if (skillType == SkillType.Lunge)
            {
                if (combatController.CurrentTarget != null)
                {
                    Vector3 dashDirection = (combatController.CurrentTarget.position - transform.position).normalized;
                    dashDirection.y = 0f; // Keep horizontal

                    var characterController = GetComponent<CharacterController>();
                    if (characterController != null)
                    {
                        Vector3 dashMovement = dashDirection * CombatConstants.LUNGE_DASH_DISTANCE;
                        characterController.Move(dashMovement);

                        if (enableDebugLogs)
                        {
                            Debug.Log($"{gameObject.name} lunged forward {CombatConstants.LUNGE_DASH_DISTANCE} units toward {combatController.CurrentTarget.name}");
                        }
                    }
                }

                // Trigger combat interaction (damage handled by CombatInteractionManager)
                CombatInteractionManager.Instance?.ProcessSkillExecution(this, skillType);
                return true;
            }

            // Check range for other offensive skills
            if (SpeedResolver.IsOffensiveSkill(skillType))
            {
                if (combatController.CurrentTarget == null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} {skillType} failed: no target");
                    }
                    return false;
                }

                if (!weaponController.CheckRangeForSkill(combatController.CurrentTarget, skillType))
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} {skillType} failed: target out of range");
                    }
                    return false;
                }
            }

            // Trigger combat interaction system
            CombatInteractionManager.Instance?.ProcessSkillExecution(this, skillType);

            return true;
        }

        private void DrawRangedAttackTrail(Vector3 from, Vector3 to, bool wasHit)
        {
            var weapon = weaponController.WeaponData;

            // Get weapon-specific visual properties or use defaults
            Color startColor = Color.yellow;
            Color endColor = wasHit ? Color.red : Color.gray;
            float width = 0.08f;
            string projectileType = "Projectile";

            if (weapon != null && weapon.isRangedWeapon)
            {
                startColor = weapon.trailColorStart;
                endColor = wasHit ? weapon.trailColorEnd : Color.gray;
                width = weapon.trailWidth;
                projectileType = weapon.projectileType;
            }

            // Create temporary object for trail
            GameObject trailObj = new GameObject($"{projectileType}Trail");
            LineRenderer line = trailObj.AddComponent<LineRenderer>();

            // Configure line appearance
            line.startWidth = width;
            line.endWidth = width;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = startColor;
            line.endColor = endColor;
            line.positionCount = 2;

            // Set positions
            line.SetPosition(0, from + Vector3.up * 1.5f); // Shooter position
            line.SetPosition(1, to); // Target or miss position

            // Play weapon-specific sound if available
            if (weapon != null && weapon.fireSound != null)
            {
                AudioSource.PlayClipAtPoint(weapon.fireSound, from);
            }

            // Fade out and destroy
            Destroy(trailObj, CombatConstants.RANGED_ATTACK_TRAIL_DURATION);
        }

        private float CalculateChargeTime(SkillType skillType)
        {
            // Lunge has custom charge time
            if (skillType == SkillType.Lunge)
            {
                return CombatConstants.LUNGE_CHARGE_TIME;
            }

            float baseChargeTime = CombatConstants.BASE_SKILL_CHARGE_TIME;
            float modifiedTime = baseChargeTime / (1 + characterStats.dexterity / 10f);
            return modifiedTime;
        }

        private int GetSkillStaminaCost(SkillType skillType)
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

        private KeyCode GetSkillKey(SkillType skillType)
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

        private bool IsDefensiveSkill(SkillType skillType)
        {
            return skillType == SkillType.Defense || skillType == SkillType.Counter;
        }

        public void SetCanAct(bool canActValue)
        {
            canAct = canActValue;

            if (!canAct && currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Active)
            {
                // Cancel non-active skills when unable to act
                CancelSkill();
            }
        }

        public void MarkDefenseBlocked()
        {
            defenseBlockedHit = true;
            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} Defense marked as having blocked a hit");
            }
        }

        public void ForceTransitionToRecovery()
        {
            if (currentState == SkillExecutionState.Waiting)
            {
                SetState(SkillExecutionState.Recovery);
                movementController.ApplySkillMovementRestriction(currentSkill, currentState);
            }
        }

        // GUI Debug Display
        private void OnGUI()
        {
            if (showSkillGUI && Application.isPlaying)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
                screenPos.y = Screen.height - screenPos.y;

                string skillText = $"Skill: {currentSkill}\nState: {currentState}";

                // Show charge progress for charging skills
                if (currentState == SkillExecutionState.Charging || currentState == SkillExecutionState.Charged)
                {
                    skillText += $"\nCharge: {chargeProgress:F1}";
                }

                // Show accuracy for aiming skills
                if (currentState == SkillExecutionState.Aiming && accuracySystem != null)
                {
                    skillText += $"\nAccuracy: {accuracySystem.CurrentAccuracy:F1}%";
                }

                GUI.Label(new Rect(screenPos.x - 50, screenPos.y, 100, 80), skillText);
            }
        }
    }
}