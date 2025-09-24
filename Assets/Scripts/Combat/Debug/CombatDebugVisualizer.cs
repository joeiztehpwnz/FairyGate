using UnityEngine;
using System.Text;
using System.Linq;

namespace FairyGate.Combat
{
    public class CombatDebugVisualizer : MonoBehaviour
    {
        [Header("Debug Display Options")]
        [SerializeField] private bool showCharacterInfo = true;
        [SerializeField] private bool showSkillInfo = true;
        [SerializeField] private bool showStatusEffects = true;
        [SerializeField] private bool showRangeVisualization = true;
        [SerializeField] private bool showCombatCalculations = true;
        [SerializeField] private bool showSystemInfo = true;

        [Header("Visualization Settings")]
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color rangeColor = Color.yellow;
        [SerializeField] private Color targetLineColor = Color.cyan;

        [Header("GUI Settings")]
        [SerializeField] private int fontSize = 12;
        [SerializeField] private Vector2 infoPosition = new Vector2(10, 100);
        [SerializeField] private float lineHeight = 20f;

        // Component references
        private CombatController combatController;
        private HealthSystem healthSystem;
        private StaminaSystem staminaSystem;
        private SkillSystem skillSystem;
        private StatusEffectManager statusEffectManager;
        private WeaponController weaponController;
        private KnockdownMeterTracker knockdownMeter;

        private GUIStyle debugStyle;
        private StringBuilder debugText = new StringBuilder();

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            healthSystem = GetComponent<HealthSystem>();
            staminaSystem = GetComponent<StaminaSystem>();
            skillSystem = GetComponent<SkillSystem>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            weaponController = GetComponent<WeaponController>();
            knockdownMeter = GetComponent<KnockdownMeterTracker>();
        }

        private void Start()
        {
            // Initialize GUI style
            debugStyle = new GUIStyle();
            debugStyle.fontSize = fontSize;
            debugStyle.normal.textColor = Color.white;
            debugStyle.wordWrap = false;
        }

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                DrawDebugInfo();
            }
        }

        private void DrawDebugInfo()
        {
            debugText.Clear();
            Vector2 currentPos = infoPosition;

            // Character identification
            bool isPlayer = gameObject.name.Contains("Player");
            debugStyle.normal.textColor = isPlayer ? playerColor : enemyColor;

            debugText.AppendLine($"=== {gameObject.name} ===");
            currentPos.y += lineHeight;

            debugStyle.normal.textColor = Color.white;

            if (showCharacterInfo)
            {
                AddCharacterInfo();
                currentPos.y += lineHeight * 4;
            }

            if (showSkillInfo)
            {
                AddSkillInfo();
                currentPos.y += lineHeight * 3;
            }

            if (showStatusEffects)
            {
                AddStatusEffectInfo();
                currentPos.y += lineHeight * 2;
            }

            if (showCombatCalculations)
            {
                AddCombatCalculationInfo();
                currentPos.y += lineHeight * 3;
            }

            if (showSystemInfo)
            {
                AddSystemInfo();
                currentPos.y += lineHeight * 2;
            }

            // Draw the compiled debug text
            GUI.Label(new Rect(infoPosition.x, infoPosition.y, 400, 600), debugText.ToString(), debugStyle);
        }

        private void AddCharacterInfo()
        {
            if (healthSystem != null)
            {
                debugText.AppendLine($"Health: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth} ({healthSystem.HealthPercentage:P0})");
            }

            if (staminaSystem != null)
            {
                string restStatus = staminaSystem.IsResting ? " [RESTING]" : "";
                debugText.AppendLine($"Stamina: {staminaSystem.CurrentStamina}/{staminaSystem.MaxStamina} ({staminaSystem.StaminaPercentage:P0}){restStatus}");
            }

            if (knockdownMeter != null)
            {
                debugText.AppendLine($"Knockdown Meter: {knockdownMeter.CurrentMeter:F1}/{knockdownMeter.MaxMeter} ({knockdownMeter.MeterPercentage:P0})");
            }

            if (combatController != null)
            {
                string targetInfo = combatController.CurrentTarget != null ? combatController.CurrentTarget.name : "None";
                debugText.AppendLine($"Combat State: {GetCombatState()} | Target: {targetInfo}");
            }
        }

        private void AddSkillInfo()
        {
            if (skillSystem != null)
            {
                debugText.AppendLine($"Current Skill: {skillSystem.CurrentSkill}");
                debugText.AppendLine($"Skill State: {skillSystem.CurrentState}");
                debugText.AppendLine($"Charge Progress: {skillSystem.ChargeProgress:P0}");
            }

            if (weaponController != null && weaponController.WeaponData != null)
            {
                var weapon = weaponController.WeaponData;
                debugText.AppendLine($"Weapon: {weapon.weaponName} (Dmg:{weapon.baseDamage} Spd:{weapon.speed:F1} Rng:{weapon.range:F1})");
            }
        }

        private void AddStatusEffectInfo()
        {
            if (statusEffectManager != null && statusEffectManager.ActiveStatusEffects.Any(e => e.isActive))
            {
                debugText.AppendLine("Status Effects:");
                foreach (var effect in statusEffectManager.ActiveStatusEffects.Where(e => e.isActive))
                {
                    debugText.AppendLine($"  {effect.type}: {effect.remainingTime:F1}s");
                }
            }
            else
            {
                debugText.AppendLine("Status Effects: None");
            }
        }

        private void AddCombatCalculationInfo()
        {
            if (combatController?.Stats != null && weaponController?.WeaponData != null)
            {
                var stats = combatController.Stats;
                var weapon = weaponController.WeaponData;

                // Show speed calculation
                float speed = SpeedResolver.CalculateSpeed(skillSystem?.CurrentSkill ?? SkillType.Attack, stats, weapon);
                debugText.AppendLine($"Combat Speed: {speed:F2}");

                // Show damage calculation (theoretical)
                int theoreticalDamage = weapon.baseDamage + stats.strength;
                debugText.AppendLine($"Base Attack Damage: {theoreticalDamage}");

                // Show movement speed
                debugText.AppendLine($"Movement Speed: {stats.MovementSpeed:F1}");
            }
        }

        private void AddSystemInfo()
        {
            debugText.AppendLine($"FPS: {(1f / Time.deltaTime):F0}");
            debugText.AppendLine($"Game Time: {Time.time:F1}s");

            if (GameManager.Instance != null && GameManager.Instance.IsGameEnded())
            {
                debugText.AppendLine("GAME ENDED - Press R to reset");
            }
        }

        private string GetCombatState()
        {
            if (combatController == null) return "Unknown";

            // Use reflection or create a public property to access the combat state
            // For now, we'll determine it based on available information
            if (!healthSystem.IsAlive) return "Dead";
            if (statusEffectManager.IsKnockedDown) return "Knocked Down";
            if (statusEffectManager.IsStunned) return "Stunned";
            if (statusEffectManager.IsResting) return "Resting";
            if (combatController.IsInCombat) return "In Combat";
            return "Idle";
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showRangeVisualization) return;

            // Draw weapon range
            if (weaponController != null && weaponController.WeaponData != null)
            {
                Gizmos.color = rangeColor;
                Gizmos.DrawWireSphere(transform.position, weaponController.WeaponData.range);

                // Draw weapon name label
                #if UNITY_EDITOR
                UnityEditor.Handles.color = rangeColor;
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (weaponController.WeaponData.range + 1f),
                    weaponController.WeaponData.weaponName
                );
                #endif
            }

            // Draw target line
            if (combatController?.CurrentTarget != null)
            {
                Gizmos.color = targetLineColor;
                Gizmos.DrawLine(transform.position, combatController.CurrentTarget.position);

                // Draw distance info
                float distance = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
                #if UNITY_EDITOR
                UnityEditor.Handles.color = targetLineColor;
                Vector3 midPoint = (transform.position + combatController.CurrentTarget.position) * 0.5f;
                UnityEditor.Handles.Label(midPoint, $"Dist: {distance:F1}");
                #endif
            }

            // Draw character indicator
            bool isPlayer = gameObject.name.Contains("Player");
            Gizmos.color = isPlayer ? playerColor : enemyColor;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.5f);
        }

        // Public methods for runtime configuration
        public void SetShowCharacterInfo(bool show) => showCharacterInfo = show;
        public void SetShowSkillInfo(bool show) => showSkillInfo = show;
        public void SetShowStatusEffects(bool show) => showStatusEffects = show;
        public void SetShowRangeVisualization(bool show) => showRangeVisualization = show;
        public void SetShowCombatCalculations(bool show) => showCombatCalculations = show;
        public void SetShowSystemInfo(bool show) => showSystemInfo = show;

        public void ToggleAllDebugInfo()
        {
            bool newState = !showCharacterInfo;
            showCharacterInfo = newState;
            showSkillInfo = newState;
            showStatusEffects = newState;
            showCombatCalculations = newState;
            showSystemInfo = newState;
        }

        // Console commands for debugging
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogCharacterState()
        {
            debugText.Clear();
            AddCharacterInfo();
            AddSkillInfo();
            AddStatusEffectInfo();
            UnityEngine.Debug.Log($"{gameObject.name} State:\n{debugText.ToString()}");
        }
    }
}