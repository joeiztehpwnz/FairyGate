using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Coordinates the display of character information UI components.
    /// This is a lightweight coordinator that manages individual UI components.
    /// </summary>
    public class CharacterInfoDisplay : MonoBehaviour
    {
        [Header("Character Settings")]
        [SerializeField] private bool autoDetectCharacterType = true;
        [SerializeField] private bool isPlayerCharacter = true;

        [Header("Display Mode Settings")]
        [SerializeField] private UIDisplayMode healthDisplayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private UIDisplayMode staminaDisplayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private UIDisplayMode knockdownDisplayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private UIDisplayMode statusEffectDisplayMode = UIDisplayMode.OnCharacter;

        [Header("UI Components")]
        [SerializeField] private HealthBarUI healthBarUI;
        [SerializeField] private StaminaBarUI staminaBarUI;
        [SerializeField] private KnockdownMeterBarUI knockdownMeterUI;
        [SerializeField] private SkillIconDisplay skillIconDisplay;
        [SerializeField] private StatusEffectDisplay statusEffectDisplay;
        [SerializeField] private TargetOutlineManager targetOutlineManager;

        [Header("Auto-Setup")]
        [SerializeField] private bool autoCreateComponents = true;

        private void Awake()
        {
            // Auto-detect if this is a player or AI character
            if (autoDetectCharacterType)
            {
                bool hasAIComponents = GetComponent<PatternExecutor>() != null ||
                                       GetComponent<IAIAgent>() != null;
                isPlayerCharacter = !hasAIComponents;
            }

            if (autoCreateComponents)
            {
                SetupUIComponents();
            }
            ApplyDisplaySettings();
        }

        private void SetupUIComponents()
        {
            // Auto-create or find HealthBarUI
            if (healthBarUI == null)
            {
                healthBarUI = GetComponent<HealthBarUI>();
                if (healthBarUI == null && GetComponent<HealthSystem>() != null)
                {
                    healthBarUI = gameObject.AddComponent<HealthBarUI>();
                }
            }

            // Auto-create or find StaminaBarUI
            if (staminaBarUI == null)
            {
                staminaBarUI = GetComponent<StaminaBarUI>();
                if (staminaBarUI == null && GetComponent<StaminaSystem>() != null)
                {
                    staminaBarUI = gameObject.AddComponent<StaminaBarUI>();
                }
            }

            // Auto-create or find KnockdownMeterBarUI
            if (knockdownMeterUI == null)
            {
                knockdownMeterUI = GetComponent<KnockdownMeterBarUI>();
                if (knockdownMeterUI == null && GetComponent<KnockdownMeterTracker>() != null)
                {
                    knockdownMeterUI = gameObject.AddComponent<KnockdownMeterBarUI>();
                }
            }

            // Auto-create or find SkillIconDisplay
            if (skillIconDisplay == null)
            {
                skillIconDisplay = GetComponent<SkillIconDisplay>();
                if (skillIconDisplay == null && GetComponent<SkillSystem>() != null)
                {
                    skillIconDisplay = gameObject.AddComponent<SkillIconDisplay>();
                }
            }

            // Auto-create or find StatusEffectDisplay
            if (statusEffectDisplay == null)
            {
                statusEffectDisplay = GetComponent<StatusEffectDisplay>();
                if (statusEffectDisplay == null && GetComponent<StatusEffectManager>() != null)
                {
                    statusEffectDisplay = gameObject.AddComponent<StatusEffectDisplay>();
                }
            }

            // Auto-create or find TargetOutlineManager (for enemies)
            if (targetOutlineManager == null)
            {
                targetOutlineManager = GetComponent<TargetOutlineManager>();
                // Check if this is an enemy (has AI components)
                bool isEnemy = GetComponent<PatternExecutor>() != null ||
                              GetComponent<IAIAgent>() != null;
                if (targetOutlineManager == null && isEnemy)
                {
                    targetOutlineManager = gameObject.AddComponent<TargetOutlineManager>();
                }
            }
        }

        /// <summary>
        /// Enable all UI components.
        /// </summary>
        public void EnableAllUI()
        {
            if (healthBarUI != null) healthBarUI.enabled = true;
            if (staminaBarUI != null) staminaBarUI.enabled = true;
            if (knockdownMeterUI != null) knockdownMeterUI.enabled = true;
            if (skillIconDisplay != null) skillIconDisplay.enabled = true;
            if (statusEffectDisplay != null) statusEffectDisplay.enabled = true;
            if (targetOutlineManager != null) targetOutlineManager.enabled = true;
        }

        /// <summary>
        /// Disable all UI components.
        /// </summary>
        public void DisableAllUI()
        {
            if (healthBarUI != null) healthBarUI.enabled = false;
            if (staminaBarUI != null) staminaBarUI.enabled = false;
            if (knockdownMeterUI != null) knockdownMeterUI.enabled = false;
            if (skillIconDisplay != null) skillIconDisplay.enabled = false;
            if (statusEffectDisplay != null) statusEffectDisplay.enabled = false;
            if (targetOutlineManager != null) targetOutlineManager.enabled = false;
        }

        /// <summary>
        /// Apply display mode settings to all UI components.
        /// </summary>
        public void ApplyDisplaySettings()
        {
            Debug.Log($"[CharacterInfoDisplay] {gameObject.name}: ApplyDisplaySettings - isPlayerCharacter={isPlayerCharacter}, " +
                      $"knockdown={knockdownDisplayMode}, status={statusEffectDisplayMode}");

            if (healthBarUI != null)
                healthBarUI.SetDisplayMode(healthDisplayMode, isPlayerCharacter);

            if (staminaBarUI != null)
                staminaBarUI.SetDisplayMode(staminaDisplayMode, isPlayerCharacter);

            if (knockdownMeterUI != null)
                knockdownMeterUI.SetDisplayMode(knockdownDisplayMode, isPlayerCharacter);

            if (statusEffectDisplay != null)
                statusEffectDisplay.SetDisplayMode(statusEffectDisplayMode, isPlayerCharacter);
        }

        private void OnValidate()
        {
            // Auto-detect character type in editor so it's visible
            if (autoDetectCharacterType)
            {
                bool hasAIComponents = GetComponent<PatternExecutor>() != null ||
                                       GetComponent<IAIAgent>() != null;
                isPlayerCharacter = !hasAIComponents;
            }

            // Apply settings when changed in editor during play
            if (Application.isPlaying)
            {
                ApplyDisplaySettings();
            }
        }
    }
}
