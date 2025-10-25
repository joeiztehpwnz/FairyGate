using UnityEngine;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Runtime hotkey controller for TestRepeaterAI, allowing developers to change enemy skills on the fly.
    /// Works entirely with hotkeys - no UI elements required.
    /// </summary>
    public class TestSkillSelector : MonoBehaviour
    {
        [Header("Target Configuration")]
        [SerializeField] private TestRepeaterAI targetAI;
        [SerializeField] private bool autoFindTargetAI = true;

        [Header("Hotkey Configuration")]
        [SerializeField] private bool enableHotkeys = true;
        [SerializeField] private KeyCode attackHotkey = KeyCode.F1;
        [SerializeField] private KeyCode defenseHotkey = KeyCode.F2;
        [SerializeField] private KeyCode counterHotkey = KeyCode.F3;
        [SerializeField] private KeyCode smashHotkey = KeyCode.F4;
        [SerializeField] private KeyCode windmillHotkey = KeyCode.F5;
        [SerializeField] private KeyCode rangedHotkey = KeyCode.F6;
        [SerializeField] private KeyCode resetHotkey = KeyCode.F12;

        [Header("Quick Settings Hotkeys")]
        [SerializeField] private KeyCode toggleDefensiveMaintenanceKey = KeyCode.F7;
        [SerializeField] private KeyCode toggleInfiniteStaminaKey = KeyCode.F8;
        [SerializeField] private KeyCode toggleMovementKey = KeyCode.F9;
        [SerializeField] private KeyCode increaseDelayKey = KeyCode.Plus;
        [SerializeField] private KeyCode decreaseDelayKey = KeyCode.Minus;

        [Header("Display Options")]
        [SerializeField] private bool showHotkeyHints = true;
        [SerializeField] private bool showStatusInfo = true;

        // Original AI component (if we're replacing it)
        private MonoBehaviour originalAI;
        private bool hasOriginalAI = false;

        // Status message for display
        private string lastActionMessage = "";
        private float messageDisplayTime = 2.0f;
        private float messageTimer = 0f;

        private void Start()
        {
            FindOrCreateTargetAI();
            ShowWelcomeMessage();
        }

        private void Update()
        {
            if (enableHotkeys)
            {
                HandleHotkeys();
            }

            // Update message timer
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
            }
        }

        private void ShowWelcomeMessage()
        {
            if (targetAI != null)
            {
                lastActionMessage = $"Test Mode Ready! Use F1-F6 to control {targetAI.gameObject.name}";
                messageTimer = messageDisplayTime;
                Debug.Log($"[TestSkillSelector] {lastActionMessage}");
            }
        }

        private void FindOrCreateTargetAI()
        {
            if (targetAI != null) return;

            if (!autoFindTargetAI) return;

            // Find enemy with an AI component
            var enemies = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy.name.Contains("Enemy"))
                {
                    // Check if it already has TestRepeaterAI
                    targetAI = enemy.GetComponent<TestRepeaterAI>();

                    if (targetAI == null)
                    {
                        // Store original AI
                        var simpleAI = enemy.GetComponent<SimpleTestAI>();
                        var knightAI = enemy.GetComponent<KnightAI>();
                        var patternedAI = enemy.GetComponent<PatternedAI>();

                        if (simpleAI != null)
                        {
                            originalAI = simpleAI;
                            simpleAI.enabled = false;
                            hasOriginalAI = true;
                        }
                        else if (knightAI != null)
                        {
                            originalAI = knightAI;
                            knightAI.enabled = false;
                            hasOriginalAI = true;
                        }
                        else if (patternedAI != null)
                        {
                            originalAI = patternedAI;
                            patternedAI.enabled = false;
                            hasOriginalAI = true;
                        }

                        // Add TestRepeaterAI
                        targetAI = enemy.gameObject.AddComponent<TestRepeaterAI>();
                        Debug.Log($"[TestSkillSelector] Added TestRepeaterAI to {enemy.name}");
                    }

                    break;
                }
            }

            if (targetAI == null)
            {
                Debug.LogWarning("[TestSkillSelector] Could not find enemy with AI to control");
            }
        }

        private void HandleHotkeys()
        {
            // Skill selection hotkeys
            if (Input.GetKeyDown(attackHotkey))
            {
                SetSkill(SkillType.Attack);
            }
            else if (Input.GetKeyDown(defenseHotkey))
            {
                SetSkill(SkillType.Defense);
            }
            else if (Input.GetKeyDown(counterHotkey))
            {
                SetSkill(SkillType.Counter);
            }
            else if (Input.GetKeyDown(smashHotkey))
            {
                SetSkill(SkillType.Smash);
            }
            else if (Input.GetKeyDown(windmillHotkey))
            {
                SetSkill(SkillType.Windmill);
            }
            else if (Input.GetKeyDown(rangedHotkey))
            {
                SetSkill(SkillType.RangedAttack);
            }
            else if (Input.GetKeyDown(resetHotkey))
            {
                ResetToOriginalAI();
            }

            // Quick settings hotkeys
            if (Input.GetKeyDown(toggleDefensiveMaintenanceKey))
            {
                ToggleDefensiveMaintenance();
            }
            else if (Input.GetKeyDown(toggleInfiniteStaminaKey))
            {
                ToggleInfiniteStamina();
            }
            else if (Input.GetKeyDown(toggleMovementKey))
            {
                ToggleMovement();
            }
            else if (Input.GetKeyDown(increaseDelayKey))
            {
                AdjustDelay(0.5f);
            }
            else if (Input.GetKeyDown(decreaseDelayKey))
            {
                AdjustDelay(-0.5f);
            }
        }

        private void SetSkill(SkillType skill)
        {
            if (targetAI == null)
            {
                Debug.LogWarning("[TestSkillSelector] No target AI found");
                return;
            }

            targetAI.SetSelectedSkill(skill);
            lastActionMessage = $"Enemy now repeating: {skill}";
            messageTimer = messageDisplayTime;

            Debug.Log($"[TestSkillSelector] {lastActionMessage}");
        }

        private void ResetToOriginalAI()
        {
            if (targetAI == null) return;

            if (hasOriginalAI && originalAI != null)
            {
                // Restore original AI
                originalAI.enabled = true;
                Destroy(targetAI);
                targetAI = null;

                lastActionMessage = "Restored original AI";
                messageTimer = messageDisplayTime;
                Debug.Log("[TestSkillSelector] Restored original AI");
            }
            else
            {
                lastActionMessage = "No original AI to restore";
                messageTimer = messageDisplayTime;
                Debug.LogWarning("[TestSkillSelector] No original AI to restore");
            }
        }

        private void ToggleDefensiveMaintenance()
        {
            if (targetAI == null) return;

            bool newValue = !targetAI.MaintainDefensiveState;
            targetAI.SetMaintainDefensiveState(newValue);

            lastActionMessage = $"Maintain Defensive: {(newValue ? "ON" : "OFF")}";
            messageTimer = messageDisplayTime;
            Debug.Log($"[TestSkillSelector] {lastActionMessage}");
        }

        private void ToggleInfiniteStamina()
        {
            if (targetAI == null) return;

            bool newValue = !targetAI.InfiniteStamina;
            targetAI.SetInfiniteStamina(newValue);

            lastActionMessage = $"Infinite Stamina: {(newValue ? "ON" : "OFF")}";
            messageTimer = messageDisplayTime;
            Debug.Log($"[TestSkillSelector] {lastActionMessage}");
        }

        private void ToggleMovement()
        {
            if (targetAI == null) return;

            bool newValue = !targetAI.EnableMovement;
            targetAI.SetEnableMovement(newValue);

            lastActionMessage = $"Movement: {(newValue ? "ON" : "OFF")}";
            messageTimer = messageDisplayTime;
            Debug.Log($"[TestSkillSelector] {lastActionMessage}");
        }

        private void AdjustDelay(float amount)
        {
            if (targetAI == null) return;

            float newDelay = Mathf.Clamp(targetAI.RepeatDelay + amount, 0.1f, 5.0f);
            targetAI.SetRepeatDelay(newDelay);

            lastActionMessage = $"Repeat Delay: {newDelay:F1}s";
            messageTimer = messageDisplayTime;
            Debug.Log($"[TestSkillSelector] {lastActionMessage}");
        }

        // Public methods for external control
        public void SetTargetAI(TestRepeaterAI ai)
        {
            targetAI = ai;
        }

        public TestRepeaterAI GetTargetAI()
        {
            return targetAI;
        }

        // GUI display for hotkey hints and status
        private void OnGUI()
        {
            if (!showHotkeyHints && !showStatusInfo) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.wordWrap = false;

            GUIStyle headerStyle = new GUIStyle(style);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 14;

            GUIStyle messageStyle = new GUIStyle(style);
            messageStyle.normal.textColor = Color.yellow;
            messageStyle.fontSize = 14;
            messageStyle.fontStyle = FontStyle.Bold;

            float x = Screen.width - 280;
            float y = 10;
            float lineHeight = 20;

            // Show action message if active
            if (messageTimer > 0f && !string.IsNullOrEmpty(lastActionMessage))
            {
                float messageX = Screen.width / 2 - 200;
                float messageY = 50;
                GUI.Label(new Rect(messageX, messageY, 400, 30), lastActionMessage, messageStyle);
            }

            if (showHotkeyHints)
            {
                GUI.Label(new Rect(x, y, 270, lineHeight), "Skill Test Hotkeys:", headerStyle);
                y += lineHeight + 5;

                GUI.Label(new Rect(x, y, 270, lineHeight), "F1 - Attack", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F2 - Defense", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F3 - Counter", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F4 - Smash", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F5 - Windmill", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F6 - Ranged Attack", style);
                y += lineHeight + 5;

                GUI.Label(new Rect(x, y, 270, lineHeight), "Settings:", headerStyle);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F7 - Toggle Defensive Maintenance", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F8 - Toggle Infinite Stamina", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "F9 - Toggle Movement", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), "+/- - Adjust Delay", style);
                y += lineHeight + 5;

                GUI.Label(new Rect(x, y, 270, lineHeight), "F12 - Reset AI", style);
                y += lineHeight + 10;
            }

            if (showStatusInfo && targetAI != null)
            {
                GUI.Label(new Rect(x, y, 270, lineHeight), "Test AI Status:", headerStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, 270, lineHeight), $"Skill: {targetAI.SelectedSkill}", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), $"Delay: {targetAI.RepeatDelay:F1}s", style);
                y += lineHeight;
                GUI.Label(new Rect(x, y, 270, lineHeight), $"Phase: {targetAI.GetCurrentPatternPhase()}", style);
                y += lineHeight;

                string flags = "";
                if (targetAI.MaintainDefensiveState) flags += "[Maintain] ";
                if (targetAI.InfiniteStamina) flags += "[∞ Stam] ";
                if (targetAI.SkipRangedAiming) flags += "[Skip Aim] ";
                if (targetAI.AddRandomDelay) flags += "[Random] ";
                if (targetAI.EnableMovement) flags += "[Move] ";

                if (!string.IsNullOrEmpty(flags))
                {
                    GUI.Label(new Rect(x, y, 270, lineHeight), flags.Trim(), style);
                    y += lineHeight;
                }

                if (targetAI.EnableMovement)
                {
                    GUI.Label(new Rect(x, y, 270, lineHeight), $"Range: {targetAI.OptimalRange:F1}", style);
                }
            }
        }
    }
}
