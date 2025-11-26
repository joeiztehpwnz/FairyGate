using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Editor window for configuring CombatLogger settings.
    /// Allows real-time toggling of log categories and verbosity levels.
    /// </summary>
    public class CombatLoggerConfigWindow : EditorWindow
    {
        private CombatLogger.LogLevel minimumLevel = CombatLogger.LogLevel.Info;
        private Vector2 scrollPosition;

        [MenuItem("Combat/Debug/Combat Logger Configuration")]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatLoggerConfigWindow>("Combat Logger");
            window.minSize = new Vector2(350, 500);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawMinimumLevelControl();
            EditorGUILayout.Space(10);
            DrawQuickActions();
            EditorGUILayout.Space(10);
            DrawCategoryToggles();
            EditorGUILayout.Space(10);
            DrawInstructions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Combat Logger Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure which categories of combat logs are displayed in the console. " +
                "Categories are color-coded for easy identification.",
                MessageType.Info);
        }

        private void DrawMinimumLevelControl()
        {
            EditorGUILayout.LabelField("Minimum Log Level", EditorStyles.boldLabel);
            minimumLevel = (CombatLogger.LogLevel)EditorGUILayout.EnumPopup("Level", minimumLevel);

            if (GUILayout.Button("Apply Level"))
            {
                CombatLogger.SetMinimumLevel(minimumLevel);
                Debug.Log($"[Combat Logger] Minimum level set to: {minimumLevel}");
            }

            EditorGUILayout.HelpBox(
                "Debug: Most verbose (all messages)\n" +
                "Info: General information\n" +
                "Warning: Potential issues\n" +
                "Error: Critical errors only",
                MessageType.None);
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All"))
            {
                CombatLogger.EnableAll();
                Debug.Log("[Combat Logger] All categories enabled");
            }
            if (GUILayout.Button("Disable All"))
            {
                CombatLogger.DisableAll();
                Debug.Log("[Combat Logger] All categories disabled");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryToggles()
        {
            EditorGUILayout.LabelField("Log Categories", EditorStyles.boldLabel);

            DrawCategoryToggle(CombatLogger.LogCategory.AI, "#00FFFF",
                "AI behavior, pattern execution, decision making");
            DrawCategoryToggle(CombatLogger.LogCategory.Combat, "#FF4444",
                "Combat interactions, damage calculations");
            DrawCategoryToggle(CombatLogger.LogCategory.Skills, "#FFD700",
                "Skill execution, state transitions, charging");
            DrawCategoryToggle(CombatLogger.LogCategory.Movement, "#90EE90",
                "Character movement, positioning (verbose!)");
            DrawCategoryToggle(CombatLogger.LogCategory.Weapons, "#FFA500",
                "Weapon swapping, attacks, range calculations");
            DrawCategoryToggle(CombatLogger.LogCategory.Health, "#FF1493",
                "Health changes, healing, damage taken");
            DrawCategoryToggle(CombatLogger.LogCategory.Stamina, "#32CD32",
                "Stamina costs, regeneration, resting");
            DrawCategoryToggle(CombatLogger.LogCategory.UI, "#87CEEB",
                "UI updates, displays (verbose!)");
            DrawCategoryToggle(CombatLogger.LogCategory.System, "#DDA0DD",
                "Managers, coordinators, initialization");
            DrawCategoryToggle(CombatLogger.LogCategory.Pattern, "#00CED1",
                "AI pattern transitions, node execution");
            DrawCategoryToggle(CombatLogger.LogCategory.Formation, "#FFB6C1",
                "Formation slot management, positioning");
            DrawCategoryToggle(CombatLogger.LogCategory.Attack, "#FF6347",
                "Attack coordination, permissions");
        }

        private void DrawCategoryToggle(CombatLogger.LogCategory category, string colorHex, string description)
        {
            EditorGUILayout.BeginHorizontal();

            // Color indicator
            Color color;
            ColorUtility.TryParseHtmlString(colorHex, out color);
            var originalColor = GUI.color;
            GUI.color = color;
            GUILayout.Label("■", GUILayout.Width(20));
            GUI.color = originalColor;

            // Category name and toggle
            bool isEnabled = CombatLogger.IsCategoryEnabled(category);
            bool newEnabled = EditorGUILayout.Toggle(category.ToString(), isEnabled, GUILayout.Width(120));

            if (newEnabled != isEnabled)
            {
                CombatLogger.SetCategoryEnabled(category, newEnabled);
            }

            // Description
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("Usage", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "How to use CombatLogger in code:\n\n" +
                "// Category-specific methods:\n" +
                "CombatLogger.LogAI(\"Pattern transition\");\n" +
                "CombatLogger.LogCombat(\"Damage dealt: 50\");\n" +
                "CombatLogger.LogSkill(\"Smash executed\");\n\n" +
                "// With log level:\n" +
                "CombatLogger.LogAI(\"Debug info\", CombatLogger.LogLevel.Debug);\n" +
                "CombatLogger.LogCombat(\"Warning!\", CombatLogger.LogLevel.Warning);\n\n" +
                "// Generic method:\n" +
                "CombatLogger.Log(\"Message\", CombatLogger.LogCategory.AI);",
                MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Performance Note: Logging is automatically disabled in release builds " +
                "through conditional compilation. Zero runtime cost!",
                MessageType.Info);
        }
    }
}
