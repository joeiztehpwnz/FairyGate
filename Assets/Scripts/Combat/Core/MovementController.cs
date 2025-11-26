using System.Collections.Generic;
using UnityEngine;

namespace FairyGate.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementController : MonoBehaviour, ICombatUpdatable
    {
        [Header("Movement Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private float currentMovementSpeed;
        [SerializeField] private bool canMove = true;
        [SerializeField] private bool isPlayerControlled = true;

        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private bool useCameraRelativeMovement = true;

        [Header("Input")]
        [SerializeField] private KeyCode forwardKey = KeyCode.W;
        [SerializeField] private KeyCode backwardKey = KeyCode.S;
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Header("Movement Arbitration")]
        [SerializeField] private bool useMovementArbitrator = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showMovementGizmos = true;

        private CharacterController characterController;
        private SkillSystem skillSystem;
        private HealthSystem healthSystem;
        private Vector3 currentVelocity;
        private float baseMovementSpeed;
        private float skillMovementModifier = 1f;
        private Vector3 aiMovementInput = Vector3.zero;
        private Vector3 overrideMovementInput = Vector3.zero;
        private bool hasMovementOverride = false;

        // Movement arbitration
        private MovementArbitrator movementArbitrator;
        private PlayerInputAuthority playerAuthority;
        private AIInputAuthority aiAuthority;
        private SkillOverrideAuthority skillOverrideAuthority;
        private MovementLockAuthority lockAuthority;

        public bool CanMove => canMove;
        public float CurrentSpeed => currentMovementSpeed;
        public Vector3 CurrentVelocity => currentVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            skillSystem = GetComponent<SkillSystem>();
            healthSystem = GetComponent<HealthSystem>();

            if (characterStats == null)
            {
                CombatLogger.LogMovement($"MovementController on {gameObject.name} has no CharacterStats assigned. Using default values.", CombatLogger.LogLevel.Warning);
                characterStats = CharacterStats.CreateDefaultStats();
            }

            // Initialize movement arbitration system
            if (useMovementArbitrator)
            {
                InitializeMovementArbitrator();
            }

            baseMovementSpeed = characterStats.MovementSpeed;
            currentMovementSpeed = baseMovementSpeed;

            // Register with combat update manager
            CombatUpdateManager.Register(this);
        }

        private void Start()
        {
            // Auto-find camera if not assigned and player controlled
            if (isPlayerControlled && cameraTransform == null && useCameraRelativeMovement)
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;

                    // Update PlayerInputAuthority with the newly found camera
                    if (playerAuthority != null)
                    {
                        playerAuthority.SetCameraTransform(cameraTransform);
                    }

                    if (enableDebugLogs)
                    {
                        CombatLogger.LogMovement($"{gameObject.name} MovementController found main camera for camera-relative movement");
                    }
                }
                else
                {
                    CombatLogger.LogMovement($"{gameObject.name} MovementController: Camera-relative movement enabled but no camera found", CombatLogger.LogLevel.Warning);
                }
            }
        }

        private void OnDestroy()
        {
            // Unregister to prevent memory leaks
            CombatUpdateManager.Unregister(this);
        }

        // Renamed from Update() to CombatUpdate() for centralized update management
        public void CombatUpdate(float deltaTime)
        {
            UpdateMovement();
            CalculateCurrentMovementSpeed();
        }

        private void UpdateMovement()
        {
            Vector3 moveDirection = ResolveMovementDirection();

            // Normalize for 8-directional movement
            if (moveDirection.magnitude > 1f)
                moveDirection.Normalize();

            // Apply current movement speed
            currentVelocity = moveDirection * currentMovementSpeed;

            ApplyMovementPhysics();
        }

        private Vector3 ResolveMovementDirection()
        {
            // Block all movement if character is dead
            if (healthSystem != null && !healthSystem.IsAlive)
            {
                currentVelocity = Vector3.zero;
                aiMovementInput = Vector3.zero;
                return Vector3.zero;
            }

            // Use movement arbitrator if enabled
            if (useMovementArbitrator && movementArbitrator != null)
            {
                // Update authority states based on current conditions
                UpdateMovementAuthorities();

                // Resolve movement through arbitrator
                Vector3 moveDirection = movementArbitrator.ResolveMovement();

                // If movement is blocked by arbitrator, apply no movement
                if (!movementArbitrator.IsMovementAllowed)
                {
                    currentVelocity = Vector3.zero;
                    if (characterController.isGrounded)
                    {
                        characterController.Move(Vector3.zero);
                    }
                    return Vector3.zero;
                }

                return moveDirection;
            }
            else
            {
                // Legacy movement system
                return ResolveLegacyMovementDirection();
            }
        }

        private Vector3 ResolveLegacyMovementDirection()
        {
            if (ShouldBlockMovement())
            {
                return Vector3.zero;
            }

            // Check for movement override first (auto-movement for skills)
            if (hasMovementOverride)
            {
                return overrideMovementInput;
            }

            // Get input based on control type
            if (isPlayerControlled)
            {
                return GetPlayerInputDirection();
            }
            else
            {
                return GetAIInputDirection();
            }
        }

        private bool ShouldBlockMovement()
        {
            // Block movement if character is dead
            if (healthSystem != null && !healthSystem.IsAlive)
            {
                currentVelocity = Vector3.zero;
                aiMovementInput = Vector3.zero;
                return true;
            }

            if (!canMove)
            {
                currentVelocity = Vector3.zero;
                // Clear AI movement input when movement is disabled to prevent stale values
                if (!isPlayerControlled)
                {
                    aiMovementInput = Vector3.zero;
                }
                return true;
            }
            return false;
        }

        private Vector3 GetPlayerInputDirection()
        {
            // Player keyboard input - get raw input direction
            Vector3 inputDirection = Vector3.zero;

            if (Input.GetKey(forwardKey))
                inputDirection += Vector3.forward;
            if (Input.GetKey(backwardKey))
                inputDirection += Vector3.back;
            if (Input.GetKey(leftKey))
                inputDirection += Vector3.left;
            if (Input.GetKey(rightKey))
                inputDirection += Vector3.right;

            // Transform input direction based on camera orientation
            if (useCameraRelativeMovement && cameraTransform != null && inputDirection != Vector3.zero)
            {
                return TransformToCameraRelative(inputDirection);
            }
            else
            {
                // Use world-space movement
                return inputDirection;
            }
        }

        private Vector3 GetAIInputDirection()
        {
            // AI programmatic input (always world-space)
            return aiMovementInput;
        }

        private Vector3 TransformToCameraRelative(Vector3 inputDirection)
        {
            // Get camera's forward and right directions (flatten to horizontal plane)
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            // Calculate camera-relative movement direction
            return cameraForward * inputDirection.z + cameraRight * inputDirection.x;
        }

        private void ApplyMovementPhysics()
        {
            // Apply gravity
            if (!characterController.isGrounded)
            {
                currentVelocity.y -= 9.81f * Time.deltaTime;
            }

            // Move the character
            characterController.Move(currentVelocity * Time.deltaTime);
        }

        private void CalculateCurrentMovementSpeed()
        {
            currentMovementSpeed = baseMovementSpeed * skillMovementModifier;
        }

        public void SetCanMove(bool canMoveValue)
        {
            canMove = canMoveValue;

            if (!canMove)
            {
                currentVelocity = Vector3.zero;
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogMovement($"{gameObject.name} movement {(canMove ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Enables character movement (convenience method).
        /// </summary>
        public void EnableMovement()
        {
            SetCanMove(true);
        }

        public void SetMovementModifier(float modifier)
        {
            skillMovementModifier = Mathf.Max(0f, modifier);

            if (enableDebugLogs)
            {
                CombatLogger.LogMovement($"{gameObject.name} movement modifier set to {skillMovementModifier:F2}");
            }
        }

        public void SetMovementInput(Vector3 inputDirection)
        {
            if (isPlayerControlled)
            {
                // Player: Use movement override system (for auto-movement during skills)
                overrideMovementInput = inputDirection;
                hasMovementOverride = inputDirection != Vector3.zero;

                if (enableDebugLogs && inputDirection != Vector3.zero)
                {
                    CombatLogger.LogMovement($"{gameObject.name} Player movement override: {inputDirection}");
                }
            }
            else
            {
                // AI: Use normal AI movement input
                aiMovementInput = inputDirection;
            }
        }

        public void ApplySkillMovementRestriction(SkillType skillType, SkillExecutionState executionState)
        {
            float modifier = GetSkillMovementModifier(skillType, executionState);
            SetMovementModifier(modifier);
        }

        #region Skill Movement Modifier Data-Driven System

        /// <summary>
        /// Data structure for skill-specific movement modifiers.
        /// Defines how each skill affects movement speed in different execution states.
        /// </summary>
        private struct SkillMovementModifierData
        {
            public float chargedModifier;     // Movement modifier when skill is CHARGED or WAITING
            public float aimingModifier;      // Movement modifier when skill is AIMING (ranged only)
            public bool hasChargedState;      // True if skill uses CHARGED/WAITING states
            public bool hasAimingState;       // True if skill uses AIMING state

            public SkillMovementModifierData(float charged, bool usesCharged, float aiming = 1f, bool usesAiming = false)
            {
                chargedModifier = charged;
                aimingModifier = aiming;
                hasChargedState = usesCharged;
                hasAimingState = usesAiming;
            }
        }

        /// <summary>
        /// Static lookup table for skill movement modifiers.
        /// Easy to extend - just add new entries for new skills.
        /// </summary>
        private static readonly Dictionary<SkillType, SkillMovementModifierData> skillModifiers =
            new Dictionary<SkillType, SkillMovementModifierData>
        {
            { SkillType.Attack, new SkillMovementModifierData(1f, false) },
            { SkillType.Smash, new SkillMovementModifierData(1f, true) },
            { SkillType.Lunge, new SkillMovementModifierData(1f, true) },
            { SkillType.Defense, new SkillMovementModifierData(CombatConstants.DEFENSIVE_CHARGE_MOVE_SPEED, true) },
            { SkillType.Counter, new SkillMovementModifierData(CombatConstants.DEFENSIVE_CHARGE_MOVE_SPEED, true) },
            { SkillType.Windmill, new SkillMovementModifierData(CombatConstants.DEFENSIVE_CHARGE_MOVE_SPEED, true) },
            { SkillType.RangedAttack, new SkillMovementModifierData(1f, false, CombatConstants.RANGED_ATTACK_AIMING_MOVEMENT_MODIFIER, true) }
        };

        private float GetSkillMovementModifier(SkillType skillType, SkillExecutionState executionState)
        {
            // Movement stops completely during execution phase (universal rule)
            if (executionState == SkillExecutionState.Startup ||
                executionState == SkillExecutionState.Active ||
                executionState == SkillExecutionState.Recovery)
            {
                return 0f;
            }

            // Universal rule: All skills have 100% movement speed while CHARGING
            if (executionState == SkillExecutionState.Charging)
            {
                return 1f;
            }

            // Look up skill-specific modifiers
            if (!skillModifiers.TryGetValue(skillType, out SkillMovementModifierData data))
            {
                return 1f; // Default for unknown skills
            }

            return GetModifierForSkillState(data, executionState);
        }

        /// <summary>
        /// Determines the movement modifier based on skill data and current execution state.
        /// </summary>
        private float GetModifierForSkillState(SkillMovementModifierData data, SkillExecutionState state)
        {
            // Handle AIMING state (ranged attacks)
            if (data.hasAimingState && state == SkillExecutionState.Aiming)
            {
                return data.aimingModifier;
            }

            // Handle CHARGED/WAITING states
            if (data.hasChargedState &&
                (state == SkillExecutionState.Charged || state == SkillExecutionState.Waiting))
            {
                return data.chargedModifier;
            }

            // Default for other states
            return 1f;
        }

        #endregion

        public void ResetMovementSpeed()
        {
            baseMovementSpeed = characterStats.MovementSpeed;
            SetMovementModifier(1f);
        }

        public float GetDistanceTo(Transform target)
        {
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
        }

        public bool IsMoving()
        {
            return currentVelocity.magnitude > 0.1f && canMove;
        }

        private void OnDrawGizmosSelected()
        {
            if (showMovementGizmos && Application.isPlaying)
            {
                // Draw movement vector
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2f);

                // Draw speed indicator
                Gizmos.color = canMove ? Color.blue : Color.red;
                Gizmos.DrawWireSphere(transform.position, currentMovementSpeed * 0.1f);

                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"Speed: {currentMovementSpeed:F1}\nModifier: {skillMovementModifier:F2}\nCan Move: {canMove}"
                );
                #endif
            }
        }

        private void OnValidate()
        {
            if (characterStats != null)
            {
                baseMovementSpeed = characterStats.MovementSpeed;
                currentMovementSpeed = baseMovementSpeed * skillMovementModifier;
            }
        }

        #region Movement Arbitration

        private void InitializeMovementArbitrator()
        {
            // Get or add movement arbitrator component
            movementArbitrator = GetComponent<MovementArbitrator>();
            if (movementArbitrator == null)
            {
                movementArbitrator = gameObject.AddComponent<MovementArbitrator>();
            }

            // Create movement authorities
            if (isPlayerControlled)
            {
                playerAuthority = new PlayerInputAuthority(this, cameraTransform, useCameraRelativeMovement);
                playerAuthority.SetInputKeys(forwardKey, backwardKey, leftKey, rightKey);
                movementArbitrator.RegisterAuthority(playerAuthority);
            }
            else
            {
                aiAuthority = new AIInputAuthority(this);
                movementArbitrator.RegisterAuthority(aiAuthority);
            }

            // Skill override authority (for both player and AI)
            skillOverrideAuthority = new SkillOverrideAuthority(this);
            movementArbitrator.RegisterAuthority(skillOverrideAuthority);

            // Movement lock authority (highest priority)
            lockAuthority = new MovementLockAuthority(this);
            movementArbitrator.RegisterAuthority(lockAuthority);

            if (enableDebugLogs)
            {
                CombatLogger.LogMovement($"[MovementController] Initialized movement arbitrator for {name}");
            }
        }

        private void UpdateMovementAuthorities()
        {
            // Update player input if applicable
            if (playerAuthority != null)
            {
                playerAuthority.UpdateInput();
            }

            // Update AI input if applicable
            if (aiAuthority != null)
            {
                aiAuthority.SetMovementInput(aiMovementInput);
            }

            // Update skill override
            if (skillOverrideAuthority != null)
            {
                skillOverrideAuthority.SetOverride(hasMovementOverride, overrideMovementInput);
            }

            // Update movement lock
            if (lockAuthority != null)
            {
                lockAuthority.SetLocked(!canMove);
            }
        }

        #endregion

        #region Movement Authority Classes

        /// <summary>
        /// Authority for player keyboard/gamepad input.
        /// </summary>
        private class PlayerInputAuthority : IMovementAuthority
        {
            private readonly MovementController controller;
            private Transform cameraTransform;
            private readonly bool useCameraRelative;
            private Vector3 currentInput;
            private KeyCode forwardKey, backwardKey, leftKey, rightKey;

            public int Priority => MovementPriority.PlayerInput;
            public string AuthorityName => "Player Input";
            public bool IsActive => true;

            public PlayerInputAuthority(MovementController controller, Transform camera, bool cameraRelative)
            {
                this.controller = controller;
                this.cameraTransform = camera;
                this.useCameraRelative = cameraRelative;
            }

            public void SetCameraTransform(Transform camera)
            {
                this.cameraTransform = camera;
            }

            public void SetInputKeys(KeyCode forward, KeyCode back, KeyCode left, KeyCode right)
            {
                forwardKey = forward;
                backwardKey = back;
                leftKey = left;
                rightKey = right;
            }

            public void UpdateInput()
            {
                Vector3 inputDirection = Vector3.zero;

                if (Input.GetKey(forwardKey))
                    inputDirection += Vector3.forward;
                if (Input.GetKey(backwardKey))
                    inputDirection += Vector3.back;
                if (Input.GetKey(leftKey))
                    inputDirection += Vector3.left;
                if (Input.GetKey(rightKey))
                    inputDirection += Vector3.right;

                // Transform to camera space if needed
                if (useCameraRelative && cameraTransform != null && inputDirection != Vector3.zero)
                {
                    Vector3 cameraForward = cameraTransform.forward;
                    cameraForward.y = 0f;
                    cameraForward.Normalize();

                    Vector3 cameraRight = cameraTransform.right;
                    cameraRight.y = 0f;
                    cameraRight.Normalize();

                    currentInput = cameraForward * inputDirection.z + cameraRight * inputDirection.x;
                }
                else
                {
                    currentInput = inputDirection;
                }
            }

            public bool AllowsMovement() => true;
            public Vector3 GetMovementVector() => currentInput;
        }

        /// <summary>
        /// Authority for AI pattern movement input.
        /// </summary>
        private class AIInputAuthority : IMovementAuthority
        {
            private readonly MovementController controller;
            private Vector3 aiMovement;

            public int Priority => MovementPriority.AIPattern;
            public string AuthorityName => "AI Pattern";
            public bool IsActive => aiMovement != Vector3.zero;

            public AIInputAuthority(MovementController controller)
            {
                this.controller = controller;
            }

            public void SetMovementInput(Vector3 input)
            {
                aiMovement = input;
            }

            public bool AllowsMovement() => true;
            public Vector3 GetMovementVector() => aiMovement;
        }

        /// <summary>
        /// Authority for skill-based movement override.
        /// </summary>
        private class SkillOverrideAuthority : IMovementAuthority
        {
            private readonly MovementController controller;
            private bool hasOverride;
            private Vector3 overrideMovement;

            public int Priority => MovementPriority.SkillExecution;
            public string AuthorityName => "Skill Override";
            public bool IsActive => hasOverride;

            public SkillOverrideAuthority(MovementController controller)
            {
                this.controller = controller;
            }

            public void SetOverride(bool active, Vector3 movement)
            {
                hasOverride = active;
                overrideMovement = movement;
            }

            public bool AllowsMovement() => true;
            public Vector3 GetMovementVector() => overrideMovement;
        }

        /// <summary>
        /// Authority for hard movement locks (stun, root, etc).
        /// </summary>
        private class MovementLockAuthority : IMovementAuthority
        {
            private readonly MovementController controller;
            private bool isLocked;

            public int Priority => MovementPriority.HardCrowdControl;
            public string AuthorityName => "Movement Lock";
            public bool IsActive => isLocked;

            public MovementLockAuthority(MovementController controller)
            {
                this.controller = controller;
            }

            public void SetLocked(bool locked)
            {
                isLocked = locked;
            }

            public bool AllowsMovement() => !isLocked;
            public Vector3 GetMovementVector() => Vector3.zero;
        }

        #endregion
    }
}