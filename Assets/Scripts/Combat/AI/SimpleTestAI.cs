using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Minimal AI coordinator component.
    ///
    /// DEPRECATED: Most AI functionality has been moved to PatternExecutor.
    /// This component remains only for:
    /// 1. Weapon swapping functionality
    /// 2. AICoordinator API compatibility (requires SimpleTestAI reference)
    ///
    /// TODO: Refactor AICoordinator API to remove SimpleTestAI dependency,
    /// then this component can be fully deleted.
    /// </summary>
    public class SimpleTestAI : MonoBehaviour
    {
        [Header("Weapon Swapping")]
        [SerializeField] private bool enableWeaponSwapping = false;
        [SerializeField] private PreferredRange preferPrimaryAtRange = PreferredRange.Either;
        [SerializeField] private PreferredRange preferSecondaryAtRange = PreferredRange.Either;
        [SerializeField] private float swapDistanceThreshold = 3.0f;
        [SerializeField] private float swapCooldown = 5.0f;

        [Header("Coordination")]
        [SerializeField] private bool useCoordination = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        // Component references
        private WeaponController weaponController;
        private Transform player;
        private float lastWeaponSwapTime = -999f;
        private float cachedDistanceToPlayer;

        // Public property for AICoordinator compatibility
        public bool IsReadyToAttack => true; // PatternExecutor handles this now

        private void Awake()
        {
            weaponController = GetComponent<WeaponController>();
        }

        private void Start()
        {
            // Find player for weapon swapping
            FindPlayer();

            // Register with coordinator if enabled
            if (useCoordination && AICoordinator.Instance != null)
            {
                AICoordinator.Instance.RegisterEnemy(this);
            }
        }

        private void OnDisable()
        {
            // Unregister from coordinator
            if (useCoordination && AICoordinator.Instance != null)
            {
                AICoordinator.Instance.UnregisterEnemy(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister (safety net)
            if (useCoordination && AICoordinator.Instance != null)
            {
                AICoordinator.Instance.UnregisterEnemy(this);
            }
        }

        private void Update()
        {
            // Only handle weapon swapping
            if (enableWeaponSwapping && player != null)
            {
                UpdateDistanceCache();
                ConsiderWeaponSwap();
            }
        }

        private void FindPlayer()
        {
            var combatController = GetComponent<CombatController>();
            var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);

            foreach (var combatant in combatants)
            {
                if (combatant != combatController && combatant.name.Contains("Player"))
                {
                    player = combatant.transform;
                    break;
                }
            }

            // Fallback: find closest
            if (player == null && combatants.Length > 0)
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
                            player = combatant.transform;
                        }
                    }
                }
            }
        }

        private void UpdateDistanceCache()
        {
            if (player != null)
            {
                cachedDistanceToPlayer = Vector3.Distance(transform.position, player.position);
            }
        }

        private void ConsiderWeaponSwap()
        {
            if (!enableWeaponSwapping || weaponController == null || player == null)
                return;

            // Check cooldown
            if (Time.time - lastWeaponSwapTime < swapCooldown)
                return;

            // Determine which weapon should be active
            bool shouldUsePrimary = ShouldUsePrimaryWeapon(cachedDistanceToPlayer);
            bool needsSwap = weaponController.IsPrimaryActive != shouldUsePrimary;

            if (needsSwap)
            {
                bool swapped = weaponController.SwapWeapons();
                if (swapped)
                {
                    lastWeaponSwapTime = Time.time;

                    if (enableDebugLogs)
                    {
                        string weaponName = weaponController.WeaponData?.weaponName ?? "Unknown";
                        string reason = shouldUsePrimary ? "distance > threshold" : "distance <= threshold";
                        Debug.Log($"{gameObject.name} AI swapped to {(shouldUsePrimary ? "primary" : "secondary")} weapon ({weaponName}) - {reason}");
                    }
                }
            }
        }

        private bool ShouldUsePrimaryWeapon(float distanceToPlayer)
        {
            bool isAtFarRange = distanceToPlayer > swapDistanceThreshold;

            // Check primary weapon preference
            if (preferPrimaryAtRange == PreferredRange.Far && isAtFarRange)
                return true;
            if (preferPrimaryAtRange == PreferredRange.Close && !isAtFarRange)
                return true;

            // Check secondary weapon preference (inverse logic)
            if (preferSecondaryAtRange == PreferredRange.Far && isAtFarRange)
                return false;
            if (preferSecondaryAtRange == PreferredRange.Close && !isAtFarRange)
                return false;

            // Default: stick with current weapon
            return weaponController.IsPrimaryActive;
        }
    }
}
