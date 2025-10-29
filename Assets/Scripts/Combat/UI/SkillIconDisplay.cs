using UnityEngine;

namespace FairyGate.Combat
{
    public class SkillIconDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SkillSystem skillSystem;

        [Header("Settings")]
        [SerializeField] private float heightOffset = 2.5f;
        [SerializeField] private int fontSize = 48;
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
                Debug.Log($"[SkillIconDisplay] Awake on {gameObject.name}: SkillSystem={(skillSystem != null ? "Found" : "NOT FOUND")}, Camera={(mainCamera != null ? "Found" : "NOT FOUND")}");
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
                Debug.Log($"[SkillIconDisplay] {gameObject.name}: {skillType} -> {state}");
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
                Debug.Log($"[SkillIconDisplay] ShowIcon: {skillType} = {currentEmoji}, alpha={alpha}, pulse={pulse}");
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

        // OnGUI removed - now handled by CharacterInfoDisplay component
    }
}
