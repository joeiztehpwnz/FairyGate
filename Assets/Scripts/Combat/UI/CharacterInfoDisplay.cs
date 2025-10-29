using UnityEngine;
using System.Linq;

namespace FairyGate.Combat
{
    public class CharacterInfoDisplay : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private SkillSystem skillSystem;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private KnockdownMeterTracker knockdownMeter;
        [SerializeField] private StatusEffectManager statusEffectManager;

        [Header("Display Settings")]
        [SerializeField] private bool showSkillInfo = true;
        [SerializeField] private bool showHealthInfo = false;
        [SerializeField] private bool showMeterInfo = true;
        [SerializeField] private bool showStatusInfo = true;
        [SerializeField] private float heightOffset = 3.0f;
        [SerializeField] private int fontSize = 14;

        [Header("Skill Icon Settings")]
        [SerializeField] private int iconFontSize = 48;
        [SerializeField] private float pulseDuration = 0.5f;
        [SerializeField] private float pulseMinScale = 0.8f;
        [SerializeField] private float pulseMaxScale = 1.2f;
        [SerializeField] private float chargingAlpha = 0.7f;
        [SerializeField] private float executingAlpha = 1.0f;
        [SerializeField] private bool useTextLabels = false;

        private Camera mainCamera;

        // Cached display data
        private SkillType currentSkill;
        private SkillExecutionState currentSkillState;
        private int currentHealth;
        private int maxHealth;
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
            if (knockdownMeter == null) knockdownMeter = GetComponent<KnockdownMeterTracker>();
            if (statusEffectManager == null) statusEffectManager = GetComponent<StatusEffectManager>();
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

            // Subscribe to meter events
            if (knockdownMeter != null)
            {
                knockdownMeter.OnMeterChanged += HandleMeterChanged;
                maxMeter = knockdownMeter.MaxMeter;
                currentMeter = knockdownMeter.CurrentMeter;
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

            if (knockdownMeter != null)
            {
                knockdownMeter.OnMeterChanged -= HandleMeterChanged;
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

        private void HandleMeterChanged(float meter, float max)
        {
            currentMeter = meter;
            maxMeter = max;
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
            }

            // Draw health info
            if (showHealthInfo && healthSystem != null)
            {
                GUI.Label(new Rect(screenPos.x - panelWidth / 2, currentY, panelWidth, lineHeight),
                    $"HP: {currentHealth}/{maxHealth}");
                currentY += lineHeight;
            }

            // Draw knockdown meter
            if (showMeterInfo && knockdownMeter != null)
            {
                GUI.Label(new Rect(screenPos.x - panelWidth / 2, currentY, panelWidth, lineHeight),
                    $"Meter: {currentMeter:F1}/{maxMeter:F0}");
                currentY += lineHeight;
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
    }
}
