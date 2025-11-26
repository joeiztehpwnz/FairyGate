using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Manages the outline effect for when this character is targeted.
    /// Extracted from CharacterInfoDisplay for single responsibility.
    /// </summary>
    public class TargetOutlineManager : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private OutlineEffect outlineEffect;

        [Header("Settings")]
        [SerializeField] private bool autoDetectEnemy = true;

        private CombatController playerCombatController;
        private bool isEnemy;

        private void Awake()
        {
            // Auto-detect if this is an enemy
            if (autoDetectEnemy)
            {
                // Check for PatternExecutor (new AI) or any AI interface implementation
                isEnemy = GetComponent<PatternExecutor>() != null ||
                         GetComponent<IAIAgent>() != null;
            }

            // If this is an enemy, set up outline effect and find player
            if (isEnemy)
            {
                // Auto-find or add OutlineEffect component
                if (outlineEffect == null)
                {
                    outlineEffect = GetComponent<OutlineEffect>();
                    if (outlineEffect == null)
                    {
                        outlineEffect = gameObject.AddComponent<OutlineEffect>();
                    }
                }

                // Find the player's CombatController for target tracking
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerCombatController = playerObject.GetComponent<CombatController>();
                }
            }
        }

        private void OnEnable()
        {
            // Subscribe to player targeting events (enemies only)
            if (isEnemy && playerCombatController != null)
            {
                playerCombatController.OnTargetChanged += HandleTargetChanged;

                // Check if we're already targeted
                if (playerCombatController.CurrentTarget == transform)
                {
                    HandleTargetChanged(transform);
                }
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from player targeting events
            if (isEnemy && playerCombatController != null)
            {
                playerCombatController.OnTargetChanged -= HandleTargetChanged;
            }

            // Disable outline when component is disabled
            if (outlineEffect != null)
            {
                outlineEffect.SetOutlineEnabled(false);
            }
        }

        private void HandleTargetChanged(Transform newTarget)
        {
            // Enable outline if player is targeting this enemy, disable otherwise
            if (outlineEffect != null)
            {
                bool isTargeted = newTarget == transform;
                outlineEffect.SetOutlineEnabled(isTargeted);
            }
        }

        /// <summary>
        /// Manually set whether this character should be considered an enemy.
        /// </summary>
        public void SetIsEnemy(bool isEnemy)
        {
            this.isEnemy = isEnemy;
            autoDetectEnemy = false;
        }
    }
}