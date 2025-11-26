using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles weapon swapping logic for pattern-based AI.
    /// Manages automatic weapon switching based on distance to target and weapon preferences.
    ///
    /// Classic Mabinogi Design: Enemies can adapt their weapon choice based on range,
    /// making combat more dynamic and challenging.
    /// </summary>
    public class PatternWeaponManager
    {
        private readonly WeaponController weaponController;
        private readonly Transform transform;
        private readonly bool enableDebugLogs;

        // Weapon swapping configuration
        private readonly bool enableWeaponSwapping;
        private readonly PreferredRange preferPrimaryAtRange;
        private readonly PreferredRange preferSecondaryAtRange;
        private readonly float swapDistanceThreshold;
        private readonly float swapCooldown;

        // State
        private float lastWeaponSwapTime = -999f;
        private bool hasRangedWeapon = false;

        public bool HasRangedWeapon => hasRangedWeapon;

        public PatternWeaponManager(
            WeaponController weaponController,
            Transform transform,
            bool enableWeaponSwapping,
            PreferredRange preferPrimaryAtRange,
            PreferredRange preferSecondaryAtRange,
            float swapDistanceThreshold,
            float swapCooldown,
            bool enableDebugLogs = false)
        {
            this.weaponController = weaponController;
            this.transform = transform;
            this.enableWeaponSwapping = enableWeaponSwapping;
            this.preferPrimaryAtRange = preferPrimaryAtRange;
            this.preferSecondaryAtRange = preferSecondaryAtRange;
            this.swapDistanceThreshold = swapDistanceThreshold;
            this.swapCooldown = swapCooldown;
            this.enableDebugLogs = enableDebugLogs;
        }

        /// <summary>
        /// Updates weapon capability flags based on current weapon.
        /// Should be called when weapons are initially equipped or swapped.
        /// </summary>
        public void UpdateWeaponCapabilities()
        {
            if (weaponController != null && weaponController.WeaponData != null)
            {
                hasRangedWeapon = weaponController.WeaponData.isRangedWeapon;

                if (enableDebugLogs)
                {
                    CombatLogger.LogWeapon($"[PatternWeaponManager] {transform.name} weapon capabilities: ranged={hasRangedWeapon}");
                }
            }
        }

        /// <summary>
        /// Consider swapping weapons based on distance to target.
        /// Call this from Update() when weapon swapping is enabled.
        /// </summary>
        public void ConsiderWeaponSwap(Transform targetPlayer, float distanceToPlayer)
        {
            if (!enableWeaponSwapping || weaponController == null || targetPlayer == null)
                return;

            // Check cooldown
            if (Time.time - lastWeaponSwapTime < swapCooldown)
                return;

            // Determine which weapon should be active
            bool shouldUsePrimary = ShouldUsePrimaryWeapon(distanceToPlayer);
            bool needsSwap = weaponController.IsPrimaryActive != shouldUsePrimary;

            if (needsSwap)
            {
                bool swapped = weaponController.SwapWeapons();
                if (swapped)
                {
                    lastWeaponSwapTime = Time.time;

                    // Update weapon capabilities after swap
                    UpdateWeaponCapabilities();

                    if (enableDebugLogs)
                    {
                        string weaponName = weaponController.WeaponData?.weaponName ?? "Unknown";
                        string reason = shouldUsePrimary ? "distance > threshold" : "distance <= threshold";
                        CombatLogger.LogWeapon($"[PatternWeaponManager] {transform.name} swapped to {(shouldUsePrimary ? "primary" : "secondary")} weapon ({weaponName}) - {reason}");
                    }
                }
            }
        }

        /// <summary>
        /// Determine if primary weapon should be used based on distance and preferences.
        /// </summary>
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

            // Default: primary weapon
            return true;
        }
    }
}
