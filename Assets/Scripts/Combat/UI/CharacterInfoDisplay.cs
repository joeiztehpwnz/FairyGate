using UnityEngine;
using System.Linq;

namespace FairyGate.Combat
{
    public class CharacterInfoDisplay : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private SkillSystem skillSystem;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private StaminaSystem staminaSystem;
        [SerializeField] private KnockdownMeterTracker knockdownMeter;
        [SerializeField] private StatusEffectManager statusEffectManager;
        [SerializeField] private OutlineEffect outlineEffect;

        [Header("Display Settings")]
        [SerializeField] private bool showSkillInfo = true;
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private bool showStaminaBar = true;
        [SerializeField] private bool showMeterBar = true;
        [SerializeField] private bool showStatusInfo = true;
        [SerializeField] private float heightOffset = 3.3f;
        [SerializeField] private int fontSize = 14;

        [Header("Bar Settings")]
        [SerializeField] private float barWidth = 150f;
        [SerializeField] private float barHeight = 8f;
        [SerializeField] private float barSpacing = 3f;

        [Header("Skill Icon Settings")]
        [SerializeField] private int iconFontSize = 24;
        [SerializeField] private float pulseDuration = 0.5f;
        [SerializeField] private float pulseMinScale = 0.8f;
        [SerializeField] private float pulseMaxScale = 1.2f;
        [SerializeField] private float chargingAlpha = 0.7f;
        [SerializeField] private float executingAlpha = 1.0f;
        [SerializeField] private bool useTextLabels = false;

        private Camera mainCamera;
        private CombatController playerCombatController; // For target outline tracking
        private bool isEnemy; // True if this character is an AI enemy

        // Cached display data
        private SkillType currentSkill;
        private SkillExecutionState currentSkillState;
        private int currentHealth;
        private int maxHealth;
        private int currentStamina;
        private int maxStamina;
        private float currentMeter;
        private float maxMeter;
        private string currentStatusText = "";

        // Skill icon animation
        private float pulseTimer;
        private bool isPulsing;

        private void Awake()
        {
            mainCamera = Camera.main;

            // Auto-find components if not assigned
            if (skillSystem == null) skillSystem = GetComponent<SkillSystem>();
            if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
            if (staminaSystem == null) staminaSystem = GetComponent<StaminaSystem>();
            if (knockdownMeter == null) knockdownMeter = GetComponent<KnockdownMeterTracker>();
            if (statusEffectManager == null) statusEffectManager = GetComponent<StatusEffectManager>();

            // Determine if this character is an enemy (has AI component)
            isEnemy = GetComponent<SimpleTestAI>() != null;

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
            // Subscribe to skill events
            if (skillSystem != null)
            {
                skillSystem.OnSkillStateChanged += HandleSkillStateChanged;
            }

            // Subscribe to health events
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
                maxHealth = healthSystem.MaxHealth;
                currentHealth = healthSystem.CurrentHealth;
            }

            // Subscribe to stamina events
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged += HandleStaminaChanged;
                maxStamina = staminaSystem.MaxStamina;
                currentStamina = staminaSystem.CurrentStamina;
            }

            // Subscribe to meter events
            if (knockdownMeter != null)
            {
                knockdownMeter.OnMeterChanged += HandleMeterChanged;
                maxMeter = knockdownMeter.MaxMeter;
                currentMeter = knockdownMeter.CurrentMeter;
            }

            // Subscribe to player targeting events (enemies only)
            if (isEnemy && playerCombatController != null)
            {
                playerCombatController.OnTargetChanged += HandleTargetChanged;
            }
        }

        private void OnDisable()
        {
            if (skillSystem != null)
            {
                skillSystem.OnSkillStateChanged -= HandleSkillStateChanged;
            }

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }

            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged -= HandleStaminaChanged;
            }

            if (knockdownMeter != null)
            {
                knockdownMeter.OnMeterChanged -= HandleMeterChanged;
            }

            // Unsubscribe from player targeting events
            if (isEnemy && playerCombatController != null)
            {
                playerCombatController.OnTargetChanged -= HandleTargetChanged;
            }
        }

        private void Update()
        {
            // Update pulsing animation
            if (isPulsing)
            {
                pulseTimer += Time.deltaTime;
            }

            // Update status text
            if (showStatusInfo && statusEffectManager != null)
            {
                UpdateStatusText();
            }
        }

        private void HandleSkillStateChanged(SkillType skillType, SkillExecutionState state)
        {
            currentSkill = skillType;
            currentSkillState = state;

            // Update pulsing based on state
            switch (state)
            {
                case SkillExecutionState.Charging:
                case SkillExecutionState.Aiming:
                    isPulsing = true;
                    break;

                default:
                    isPulsing = false;
                    pulseTimer = 0f;
                    break;
            }
        }

        private void HandleHealthChanged(int health, int max)
        {
            currentHealth = health;
            maxHealth = max;
        }

        private void HandleStaminaChanged(int stamina, int max)
        {
            currentStamina = stamina;
            maxStamina = max;
        }

        private void HandleMeterChanged(float meter, float max)
        {
            currentMeter = meter;
            maxMeter = max;
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

        private void UpdateStatusText()
        {
            currentStatusText = "";
            var activeEffects = statusEffectManager.ActiveStatusEffects.Where(e => e.isActive).ToList();

            foreach (var effect in activeEffects)
            {
                if (!string.IsNullOrEmpty(currentStatusText))
                    currentStatusText += ", ";
                currentStatusText += $"{effect.type}: {effect.remainingTime:F1}s";
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || mainCamera == null) return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position + Vector3.up * heightOffset);

            // Only draw if in front of camera
            if (screenPos.z <= 0) return;

            // Validate position
            if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y) || float.IsNaN(screenPos.z))
                return;

            screenPos.y = Screen.height - screenPos.y;

            // Check if on screen
            if (screenPos.x < -100 || screenPos.x > Screen.width + 100 ||
                screenPos.y < -100 || screenPos.y > Screen.height + 100)
                return;

            // Calculate panel dimensions
            float panelWidth = 150f;
            float lineHeight = 20f;
            float currentY = screenPos.y;

            // Draw skill icon or placeholder (always show to maintain layout stability)
            if (showSkillInfo)
            {
                if (ShouldShowSkillIcon())
                {
                    DrawSkillIcon(screenPos.x, currentY);
                }
                else
                {
                    DrawPlaceholderIcon(screenPos.x, currentY);
                }
                currentY += iconFontSize + 5; // Always advance to maintain stability

                // Draw skill charge progress bar (only when charging/aiming)
                if (skillSystem != null && (currentSkillState == SkillExecutionState.Charging || currentSkillState == SkillExecutionState.Aiming))
                {
                    DrawChargeProgressBar(screenPos.x, currentY);
                    currentY += barHeight + barSpacing;
                }
            }

            // Draw health bar
            if (showHealthBar && healthSystem != null)
            {
                DrawHealthBar(screenPos.x, currentY);
                currentY += barHeight + barSpacing;
            }

            // Draw stamina bar
            if (showStaminaBar && staminaSystem != null)
            {
                DrawStaminaBar(screenPos.x, currentY);
                currentY += barHeight + barSpacing;
            }

            // Draw knockdown meter bar
            if (showMeterBar && knockdownMeter != null)
            {
                DrawKnockdownMeterBar(screenPos.x, currentY);
                currentY += barHeight + barSpacing;
            }

            // Draw status effects
            if (showStatusInfo && !string.IsNullOrEmpty(currentStatusText))
            {
                GUI.Label(new Rect(screenPos.x - panelWidth / 2, currentY, panelWidth, lineHeight * 2),
                    currentStatusText);
            }
        }

        private bool ShouldShowSkillIcon()
        {
            return currentSkillState == SkillExecutionState.Charging ||
                   currentSkillState == SkillExecutionState.Charged ||
                   currentSkillState == SkillExecutionState.Startup ||
                   currentSkillState == SkillExecutionState.Active ||
                   currentSkillState == SkillExecutionState.Recovery ||
                   currentSkillState == SkillExecutionState.Aiming ||
                   (currentSkillState == SkillExecutionState.Waiting &&
                    (currentSkill == SkillType.Defense || currentSkill == SkillType.Counter));
        }

        private void DrawSkillIcon(float centerX, float centerY)
        {
            // Calculate scale for pulsing
            float scale = 1f;
            if (isPulsing)
            {
                float t = Mathf.PingPong(pulseTimer / pulseDuration, 1f);
                scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);
            }

            // Determine alpha based on state
            float alpha = executingAlpha;
            if (currentSkillState == SkillExecutionState.Charging || currentSkillState == SkillExecutionState.Aiming)
            {
                alpha = chargingAlpha;
            }

            // Create GUIStyle for the icon
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = Mathf.RoundToInt(iconFontSize * scale);
            iconStyle.alignment = TextAnchor.MiddleCenter;
            iconStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

            // Use same panel width as text elements for consistent alignment
            float panelWidth = 150f;
            float iconHeight = iconFontSize * scale * 1.5f;
            Rect iconRect = new Rect(
                centerX - panelWidth / 2,
                centerY - iconHeight / 2,
                panelWidth,
                iconHeight
            );

            // Draw the icon
            string icon = GetSkillIcon(currentSkill);
            GUI.Label(iconRect, icon, iconStyle);
        }

        private void DrawPlaceholderIcon(float centerX, float centerY)
        {
            // Create GUIStyle for the placeholder
            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = iconFontSize;
            iconStyle.alignment = TextAnchor.MiddleCenter;
            iconStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gray, very transparent

            // Use same panel width as text elements for consistent alignment
            float panelWidth = 150f;
            float iconHeight = iconFontSize * 1.5f;
            Rect iconRect = new Rect(
                centerX - panelWidth / 2,
                centerY - iconHeight / 2,
                panelWidth,
                iconHeight
            );

            // Draw placeholder icon
            string placeholder = useTextLabels ? "---" : "❌";
            GUI.Label(iconRect, placeholder, iconStyle);
        }

        private string GetSkillIcon(SkillType skillType)
        {
            if (useTextLabels)
            {
                return skillType switch
                {
                    SkillType.Attack => "ATK",
                    SkillType.Defense => "DEF",
                    SkillType.Counter => "CTR",
                    SkillType.Smash => "SMH",
                    SkillType.Windmill => "WML",
                    SkillType.RangedAttack => "RNG",
                    SkillType.Lunge => "LNG",
                    _ => "???"
                };
            }
            else
            {
                return skillType switch
                {
                    SkillType.Attack => "⚔️",
                    SkillType.Defense => "🛡️",
                    SkillType.Counter => "↩️",
                    SkillType.Smash => "🔨",
                    SkillType.Windmill => "🌪️",
                    SkillType.RangedAttack => "🏹",
                    SkillType.Lunge => "⚡",
                    _ => "❓"
                };
            }
        }

        private void DrawChargeProgressBar(float centerX, float centerY)
        {
            float progress = skillSystem.ChargeProgress;

            // Background bar (dark gray)
            Rect bgRect = new Rect(centerX - barWidth / 2, centerY, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Progress bar (cyan/blue for charging)
            if (progress > 0)
            {
                Rect fillRect = new Rect(centerX - barWidth / 2, centerY, barWidth * progress, barHeight);
                GUI.color = new Color(0f, 0.8f, 1f, 0.9f); // Cyan
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Reset color
            GUI.color = Color.white;
        }

        private void DrawHealthBar(float centerX, float centerY)
        {
            float percentage = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

            // Background bar (dark gray)
            Rect bgRect = new Rect(centerX - barWidth / 2, centerY, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Health bar (red to green gradient based on percentage)
            if (percentage > 0)
            {
                Color barColor = Color.Lerp(
                    new Color(0.9f, 0.1f, 0.1f, 0.9f), // Red (low health)
                    new Color(0.1f, 0.9f, 0.1f, 0.9f), // Green (full health)
                    percentage
                );

                Rect fillRect = new Rect(centerX - barWidth / 2, centerY, barWidth * percentage, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Reset color
            GUI.color = Color.white;
        }

        private void DrawStaminaBar(float centerX, float centerY)
        {
            float percentage = maxStamina > 0 ? (float)currentStamina / maxStamina : 0f;

            // Background bar (dark gray)
            Rect bgRect = new Rect(centerX - barWidth / 2, centerY, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Stamina bar (green to yellow to red based on percentage)
            if (percentage > 0)
            {
                Color barColor;
                if (percentage > 0.6f)
                    barColor = new Color(0.2f, 0.8f, 0.2f, 0.9f); // Green
                else if (percentage > 0.3f)
                    barColor = new Color(0.9f, 0.9f, 0.2f, 0.9f); // Yellow
                else
                    barColor = new Color(0.9f, 0.2f, 0.2f, 0.9f); // Red

                Rect fillRect = new Rect(centerX - barWidth / 2, centerY, barWidth * percentage, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Reset color
            GUI.color = Color.white;
        }

        private void DrawKnockdownMeterBar(float centerX, float centerY)
        {
            float percentage = maxMeter > 0 ? currentMeter / maxMeter : 0f;

            // Background bar (dark gray)
            Rect bgRect = new Rect(centerX - barWidth / 2, centerY, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Meter bar (orange, intensifies toward red as it fills)
            if (percentage > 0)
            {
                Color barColor;
                if (percentage < 0.5f)
                    barColor = new Color(1f, 0.6f, 0f, 0.9f); // Orange
                else if (percentage < 1.0f)
                    barColor = new Color(1f, 0.4f, 0f, 0.9f); // Deep orange (past 50% knockback threshold)
                else
                    barColor = new Color(1f, 0f, 0f, 0.9f); // Red (at 100% knockdown)

                Rect fillRect = new Rect(centerX - barWidth / 2, centerY, barWidth * percentage, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Draw 50% threshold marker (knockback)
            float threshold50X = centerX - barWidth / 2 + barWidth * 0.5f;
            Rect threshold50Rect = new Rect(threshold50X - 1, centerY, 2, barHeight);
            GUI.color = new Color(1f, 1f, 0f, 0.8f); // Yellow line
            GUI.DrawTexture(threshold50Rect, Texture2D.whiteTexture);

            // Draw 100% threshold marker (knockdown) - technically the end, but helps visual clarity
            float threshold100X = centerX - barWidth / 2 + barWidth * 1.0f;
            Rect threshold100Rect = new Rect(threshold100X - 2, centerY, 2, barHeight);
            GUI.color = new Color(1f, 0f, 0f, 0.9f); // Red line
            GUI.DrawTexture(threshold100Rect, Texture2D.whiteTexture);

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Reset color
            GUI.color = Color.white;
        }
    }
}
