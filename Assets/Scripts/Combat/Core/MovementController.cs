using UnityEngine;

namespace FairyGate.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementController : MonoBehaviour
    {
        [Header("Movement Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private float currentMovementSpeed;
        [SerializeField] private bool canMove = true;

        [Header("Input")]
        [SerializeField] private KeyCode forwardKey = KeyCode.W;
        [SerializeField] private KeyCode backwardKey = KeyCode.S;
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showMovementGizmos = true;

        private CharacterController characterController;
        private SkillSystem skillSystem;
        private Vector3 currentVelocity;
        private float baseMovementSpeed;
        private float skillMovementModifier = 1f;

        public bool CanMove => canMove;
        public float CurrentSpeed => currentMovementSpeed;
        public Vector3 CurrentVelocity => currentVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            skillSystem = GetComponent<SkillSystem>();

            if (characterStats == null)
            {
                Debug.LogWarning($"MovementController on {gameObject.name} has no CharacterStats assigned. Using default values.");
                characterStats = CharacterStats.CreateDefaultStats();
            }

            baseMovementSpeed = characterStats.MovementSpeed;
            currentMovementSpeed = baseMovementSpeed;
        }

        private void Update()
        {
            UpdateMovement();
            CalculateCurrentMovementSpeed();
        }

        private void UpdateMovement()
        {
            if (!canMove)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            Vector3 moveDirection = Vector3.zero;

            // Get input
            if (Input.GetKey(forwardKey))
                moveDirection += Vector3.forward;
            if (Input.GetKey(backwardKey))
                moveDirection += Vector3.back;
            if (Input.GetKey(leftKey))
                moveDirection += Vector3.left;
            if (Input.GetKey(rightKey))
                moveDirection += Vector3.right;

            // Normalize for 8-directional movement
            if (moveDirection.magnitude > 1f)
                moveDirection.Normalize();

            // Apply current movement speed
            currentVelocity = moveDirection * currentMovementSpeed;

            // Apply gravity
            if (!characterController.isGrounded)
            {
                currentVelocity.y -= 9.81f * Time.deltaTime;
            }

            // Move the character
            characterController.Move(currentVelocity * Time.deltaTime);

            if (enableDebugLogs && moveDirection != Vector3.zero)
            {
                Debug.Log($"{gameObject.name} moving at speed {currentMovementSpeed:F1} (modifier: {skillMovementModifier:F2})");
            }
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
                Debug.Log($"{gameObject.name} movement {(canMove ? "enabled" : "disabled")}");
            }
        }

        public void SetMovementModifier(float modifier)
        {
            skillMovementModifier = Mathf.Max(0f, modifier);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} movement modifier set to {modifier:F2}");
            }
        }

        public void ApplySkillMovementRestriction(SkillType skillType, SkillExecutionState executionState)
        {
            float modifier = GetSkillMovementModifier(skillType, executionState);
            SetMovementModifier(modifier);
        }

        private float GetSkillMovementModifier(SkillType skillType, SkillExecutionState executionState)
        {
            // Movement stops completely during execution phase
            if (executionState == SkillExecutionState.Startup ||
                executionState == SkillExecutionState.Active ||
                executionState == SkillExecutionState.Recovery)
            {
                return 0f;
            }

            // Movement restrictions during charging and waiting states
            switch (skillType)
            {
                case SkillType.Attack:
                case SkillType.Smash:
                    return 1f; // Normal movement speed while charging

                case SkillType.Defense:
                    // 30% reduction while charging AND waiting
                    return (executionState == SkillExecutionState.Charging || executionState == SkillExecutionState.Waiting)
                        ? CombatConstants.DEFENSE_MOVEMENT_SPEED_MODIFIER
                        : 1f;

                case SkillType.Counter:
                    if (executionState == SkillExecutionState.Charging)
                        return CombatConstants.COUNTER_MOVEMENT_SPEED_MODIFIER; // 30% reduction while charging
                    else if (executionState == SkillExecutionState.Waiting)
                        return 0f; // Immobilized while waiting
                    else
                        return 1f;

                case SkillType.Windmill:
                    if (executionState == SkillExecutionState.Charging)
                        return CombatConstants.WINDMILL_MOVEMENT_SPEED_MODIFIER; // 30% reduction while charging
                    else if (executionState == SkillExecutionState.Active)
                        return 0f; // Immobilized during execution
                    else
                        return 1f;

                default:
                    return 1f;
            }
        }

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
    }
}