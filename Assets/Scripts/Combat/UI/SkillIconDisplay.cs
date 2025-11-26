using UnityEngine;

namespace FairyGate.Combat
{
    public class SkillIconDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SkillSystem skillSystem;

        [Header("Settings")]
        [SerializeField] private float heightOffset = 2.4f;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private float pulseDuration = 0.5f;
        [SerializeField] private float pulseMinScale = 0.8f;
        [SerializeField] private float pulseMaxScale = 1.2f;
        [SerializeField] private float chargingAlpha = 0.7f;
        [SerializeField] private float executingAlpha = 1.0f;
        [SerializeField] private bool useTextLabels = false; // Use text labels instead of emojis
        [SerializeField] private bool enableDebugLogs = false;

        private Camera mainCamera;
        private float pulseTimer;
        private bool isPulsing;
        private bool isVisible;
        private string currentEmoji;
        private float currentAlpha;

        private void Awake()
        {
            mainCamera = Camera.main;

            // Auto-find SkillSystem if not assigned
            if (skillSystem == null)
            {
                skillSystem = GetComponent<SkillSystem>();
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogUI($"[SkillIconDisplay] Awake on {gameObject.name}: SkillSystem={(skillSystem != null ? "Found" : "NOT FOUND")}, Camera={(mainCamera != null ? "Found" : "NOT FOUND")}");
            }
        }

        private void OnEnable()
        {
            if (skillSystem != null)
            {
                skillSystem.OnSkillStateChanged += HandleSkillStateChanged;
            }
        }

        private void OnDisable()
        {
            if (skillSystem != null)
            {
                skillSystem.OnSkillStateChanged -= HandleSkillStateChanged;
            }
        }

        private void Update()
        {
            // Update pulsing animation for charging state
            if (isPulsing)
            {
                pulseTimer += Time.deltaTime;
            }
        }

        private void HandleSkillStateChanged(SkillType skillType, SkillExecutionState state)
        {
            if (enableDebugLogs)
            {
                CombatLogger.LogUI($"[SkillIconDisplay] {gameObject.name}: {skillType} -> {state}");
            }

            switch (state)
            {
                case SkillExecutionState.Charging:
                    ShowIcon(skillType, chargingAlpha, true);
                    break;

                case SkillExecutionState.Startup:
                case SkillExecutionState.Active:
                case SkillExecutionState.Recovery:
                    ShowIcon(skillType, executingAlpha, false);
                    break;

                case SkillExecutionState.Waiting:
                    // Show defensive skills in waiting state
                    if (skillType == SkillType.Defense || skillType == SkillType.Counter)
                    {
                        ShowIcon(skillType, executingAlpha, false);
                    }
                    else
                    {
                        HideIcon();
                    }
                    break;

                case SkillExecutionState.Aiming:
                    // Show ranged attack while aiming
                    ShowIcon(skillType, chargingAlpha, true);
                    break;

                case SkillExecutionState.Charged:
                    // Show charged skills (ready to execute)
                    ShowIcon(skillType, executingAlpha, false);
                    break;

                case SkillExecutionState.Uncharged:
                default:
                    HideIcon();
                    break;
            }
        }

        private void ShowIcon(SkillType skillType, float alpha, bool pulse)
        {
            currentEmoji = GetSkillEmoji(skillType);
            currentAlpha = alpha;
            isPulsing = pulse;
            isVisible = true;

            if (enableDebugLogs)
            {
                CombatLogger.LogUI($"[SkillIconDisplay] ShowIcon: {skillType} = {currentEmoji}, alpha={alpha}, pulse={pulse}");
            }

            if (!pulse)
            {
                pulseTimer = 0f;
            }
        }

        private void HideIcon()
        {
            isVisible = false;
            isPulsing = false;
            pulseTimer = 0f;
        }

        private string GetSkillEmoji(SkillType skillType)
        {
            if (useTextLabels)
            {
                // Text label fallback
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
                // Emoji icons
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

        private void OnGUI()
        {
            // Render on top of other GUI elements (lower depth = renders on top)
            GUI.depth = -100;

            if (!isVisible || mainCamera == null)
                return;

            // Convert world position to screen position
            Vector3 worldPos = transform.position + Vector3.up * heightOffset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // Only draw if in front of camera
            if (screenPos.z > 0)
            {
                // Flip Y coordinate for GUI
                float screenY = Screen.height - screenPos.y;

                // Calculate scale for pulsing
                float scale = 1f;
                if (isPulsing)
                {
                    float t = Mathf.PingPong(pulseTimer / pulseDuration, 1f);
                    scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);
                }

                // Create GUIStyle for the icon
                GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
                iconStyle.fontSize = Mathf.RoundToInt(fontSize * scale);
                iconStyle.alignment = TextAnchor.MiddleCenter;
                iconStyle.normal.textColor = new Color(1f, 1f, 1f, currentAlpha);

                // Calculate icon rectangle
                float iconHeight = fontSize * scale * 1.5f;
                Rect iconRect = new Rect(
                    screenPos.x - 75,
                    screenY - iconHeight / 2,
                    150,
                    iconHeight
                );

                // Draw the icon
                GUI.Label(iconRect, currentEmoji, iconStyle);

                // Draw charge progress bar if charging
                if (skillSystem != null && isPulsing)
                {
                    DrawChargeProgressBar(screenPos.x, screenY + fontSize + 5);
                }
            }
        }

        private void DrawChargeProgressBar(float centerX, float centerY)
        {
            float progress = skillSystem.ChargeProgress;
            float barWidth = 150f;
            float barHeight = 6f;

            float barLeft = centerX - barWidth / 2;
            Rect bgRect = new Rect(barLeft, centerY, barWidth, barHeight);

            // Background bar (dark gray)
            Color originalColor = GUI.color;
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Progress bar with gradient (yellow to cyan as it charges)
            if (progress > 0)
            {
                // Gradient from yellow (starting) to cyan (fully charged)
                Color barColor = Color.Lerp(
                    new Color(1f, 0.9f, 0.2f, 0.9f), // Yellow (starting charge)
                    new Color(0.2f, 0.9f, 1f, 0.9f), // Cyan (fully charged)
                    progress
                );

                Rect fillRect = new Rect(barLeft, centerY, barWidth * progress, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Reset color
            GUI.color = originalColor;
        }
    }
}
