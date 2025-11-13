using UnityEngine;

namespace FairyGate.Combat
{
    public class AccuracySystem : MonoBehaviour, ICombatUpdatable
    {
        [Header("Accuracy Configuration")]
        [SerializeField] private float stationaryBuildRate = 40f; // % per second
        [SerializeField] private float movingBuildRate = 20f; // % per second
        [SerializeField] private float playerMovementDecayRate = 10f; // % per second
        [SerializeField] private float focusScalingDivisor = 20f;
        [SerializeField] private float maxMissAngle = 45f; // degrees

        [Header("Current State")]
        [SerializeField] private float currentAccuracy = 1f;
        [SerializeField] private bool isAiming = false;
        [SerializeField] private Transform currentTarget;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        // Component references
        private CharacterStats characterStats;
        private CombatController combatController;
        private MovementController movementController;

        // Cached target component (Phase 2.3 optimization)
        private MovementController cachedTargetMovement;
        private Transform lastCachedTarget;

        // Properties
        public float CurrentAccuracy => currentAccuracy;
        public bool IsAiming => isAiming;
        public Transform CurrentTarget => currentTarget;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            movementController = GetComponent<MovementController>();

            // Get character stats
            characterStats = combatController?.Stats;
            if (characterStats == null)
            {
                Debug.LogWarning($"AccuracySystem on {gameObject.name} could not find CharacterStats");
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
            if (isAiming)
            {
                UpdateAccuracy(deltaTime);
            }
        }

        public void StartAiming(Transform target)
        {
            if (target == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"{gameObject.name} cannot start aiming: no target");
                return;
            }

            isAiming = true;
            currentTarget = target;
            currentAccuracy = 1f;

            // Cache target's MovementController (Phase 2.3 optimization)
            cachedTargetMovement = target.GetComponent<MovementController>();
            lastCachedTarget = target;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} started aiming at {target.name}");
        }

        public void StopAiming()
        {
            isAiming = false;
            currentTarget = null;
            currentAccuracy = 1f;

            // Clear cached target component
            cachedTargetMovement = null;
            lastCachedTarget = null;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} stopped aiming");
        }

        private void UpdateAccuracy(float deltaTime)
        {
            if (currentTarget == null)
            {
                StopAiming();
                return;
            }

            // Calculate base build rate based on target movement
            bool targetIsMoving = IsTargetMoving();
            float baseBuildRate = targetIsMoving ? movingBuildRate : stationaryBuildRate;

            // Apply focus multiplier
            float focusMultiplier = CalculateFocusMultiplier();
            float effectiveBuildRate = baseBuildRate * focusMultiplier;

            // Apply decay if player is moving
            if (movementController.IsMoving())
            {
                effectiveBuildRate -= playerMovementDecayRate;
            }

            // Update accuracy
            currentAccuracy += effectiveBuildRate * deltaTime;
            currentAccuracy = Mathf.Clamp(currentAccuracy, 1f, 100f);
        }

        private bool IsTargetMoving()
        {
            if (currentTarget == null) return false;

            // Use cached MovementController (Phase 2.3 optimization)
            // Re-cache if target changed
            if (lastCachedTarget != currentTarget)
            {
                cachedTargetMovement = currentTarget.GetComponent<MovementController>();
                lastCachedTarget = currentTarget;
            }

            if (cachedTargetMovement == null) return false;

            return cachedTargetMovement.IsMoving();
        }

        private float CalculateFocusMultiplier()
        {
            // Higher focus = faster accuracy buildup
            // Formula: 1 + (Focus / 20)
            // 10 Focus = 1.5x, 20 Focus = 2.0x, 30 Focus = 2.5x
            return 1f + (characterStats.focus / focusScalingDivisor);
        }

        public bool RollHitChance()
        {
            float hitRoll = Random.Range(0f, 100f);
            bool isHit = hitRoll <= currentAccuracy;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} rolled {hitRoll:F1} vs {currentAccuracy:F1}% accuracy → {(isHit ? "HIT" : "MISS")}");

            return isHit;
        }

        public Vector3 CalculateMissPosition()
        {
            if (currentTarget == null)
                return transform.position + transform.forward * 6f;

            Vector3 targetPosition = currentTarget.position + Vector3.up * 1f;

            // Lower accuracy = wider miss cone
            // 100% accuracy = 0° cone (shouldn't miss, but just in case)
            // 50% accuracy = 22.5° cone
            // 1% accuracy = 45° cone
            float missAngle = Mathf.Lerp(maxMissAngle, 0f, currentAccuracy / 100f);

            // Random angle within cone (horizontal and vertical)
            float randomHorizontalAngle = Random.Range(-missAngle, missAngle);
            float randomVerticalAngle = Random.Range(-missAngle * 0.5f, missAngle * 0.5f);

            // Direction to target
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;

            // Apply random rotation
            Quaternion rotation = Quaternion.Euler(randomVerticalAngle, randomHorizontalAngle, 0f);
            Vector3 missDirection = rotation * directionToTarget;

            // Calculate position at target distance
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            return transform.position + (missDirection * distanceToTarget);
        }

        public float GetAccuracyBuildRate()
        {
            if (!isAiming || currentTarget == null) return 0f;

            bool targetIsMoving = IsTargetMoving();
            float baseBuildRate = targetIsMoving ? movingBuildRate : stationaryBuildRate;
            float focusMultiplier = CalculateFocusMultiplier();
            float effectiveBuildRate = baseBuildRate * focusMultiplier;

            if (movementController.IsMoving())
            {
                effectiveBuildRate -= playerMovementDecayRate;
            }

            return Mathf.Max(effectiveBuildRate, 0f);
        }
    }
}
