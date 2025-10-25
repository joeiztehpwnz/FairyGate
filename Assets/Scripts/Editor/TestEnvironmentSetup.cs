using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Editor
{
    /// <summary>
    /// Editor helper tools for quickly setting up the skill test environment.
    /// Provides menu items to add TestRepeaterAI to enemies and configure test scenes.
    /// </summary>
    public class TestEnvironmentSetup
    {
        private const string MenuPath = "Tools/Combat/Test Environment/";

        [MenuItem(MenuPath + "Setup Test Enemy")]
        public static void SetupTestEnemy()
        {
            // Find selected enemy or first enemy in scene
            GameObject enemy = GetSelectedOrFirstEnemy();

            if (enemy == null)
            {
                EditorUtility.DisplayDialog("No Enemy Found",
                    "Could not find an enemy GameObject. Please select an enemy or ensure one exists in the scene.",
                    "OK");
                return;
            }

            // Disable existing AI components
            var simpleAI = enemy.GetComponent<SimpleTestAI>();
            var knightAI = enemy.GetComponent<KnightAI>();
            var patternedAI = enemy.GetComponent<PatternedAI>();

            int disabledCount = 0;
            if (simpleAI != null)
            {
                simpleAI.enabled = false;
                disabledCount++;
            }
            if (knightAI != null)
            {
                knightAI.enabled = false;
                disabledCount++;
            }
            if (patternedAI != null && patternedAI != simpleAI && patternedAI != knightAI)
            {
                patternedAI.enabled = false;
                disabledCount++;
            }

            // Add TestRepeaterAI if not already present
            var testAI = enemy.GetComponent<TestRepeaterAI>();
            if (testAI == null)
            {
                testAI = enemy.AddComponent<TestRepeaterAI>();
                Debug.Log($"✅ Added TestRepeaterAI to {enemy.name}");
            }
            else
            {
                testAI.enabled = true;
                Debug.Log($"✅ Enabled existing TestRepeaterAI on {enemy.name}");
            }

            // Ensure CombatDebugVisualizer is present and configured
            var debugVisualizer = enemy.GetComponent<CombatDebugVisualizer>();
            if (debugVisualizer == null)
            {
                debugVisualizer = enemy.AddComponent<CombatDebugVisualizer>();
                Debug.Log($"✅ Added CombatDebugVisualizer to {enemy.name}");
            }

            // Mark scene as dirty
            EditorUtility.SetDirty(enemy);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(enemy.scene);

            // Select the enemy to show in Inspector
            Selection.activeGameObject = enemy;

            string message = $"Test environment setup complete on {enemy.name}!\n\n";
            if (disabledCount > 0)
            {
                message += $"Disabled {disabledCount} existing AI component(s).\n";
            }
            message += "\nUse F1-F6 hotkeys to change enemy skills during play mode:\n" +
                      "F1 - Attack\n" +
                      "F2 - Defense\n" +
                      "F3 - Counter\n" +
                      "F4 - Smash\n" +
                      "F5 - Windmill\n" +
                      "F6 - Ranged Attack\n" +
                      "F12 - Reset to original AI";

            EditorUtility.DisplayDialog("Test Environment Ready", message, "OK");
        }

        [MenuItem(MenuPath + "Add Test UI to Scene")]
        public static void AddTestUIToScene()
        {
            // Check if TestSkillSelector already exists
            var existing = Object.FindFirstObjectByType<TestSkillSelector>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("UI Already Exists",
                    "TestSkillSelector UI already exists in the scene.",
                    "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create UI GameObject
            GameObject uiRoot = new GameObject("TestSkillSelector_UI");
            var selector = uiRoot.AddComponent<TestSkillSelector>();

            // Try to find existing Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                uiRoot.transform.SetParent(canvas.transform, false);
            }
            else
            {
                Debug.LogWarning("No Canvas found in scene. TestSkillSelector UI created at root. Add it to a Canvas manually.");
            }

            EditorUtility.SetDirty(uiRoot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiRoot.scene);

            Selection.activeGameObject = uiRoot;

            EditorUtility.DisplayDialog("Test UI Added",
                "TestSkillSelector UI has been added to the scene.\n\n" +
                "Note: The UI component will work with hotkeys (F1-F6) even without UI elements.\n" +
                "If you want visual controls, you'll need to add UI elements (Dropdown, Buttons, etc.) manually.",
                "OK");
        }

        [MenuItem(MenuPath + "Restore Original AI")]
        public static void RestoreOriginalAI()
        {
            GameObject enemy = GetSelectedOrFirstEnemy();

            if (enemy == null)
            {
                EditorUtility.DisplayDialog("No Enemy Found",
                    "Could not find an enemy GameObject. Please select an enemy.",
                    "OK");
                return;
            }

            // Remove TestRepeaterAI
            var testAI = enemy.GetComponent<TestRepeaterAI>();
            if (testAI != null)
            {
                Object.DestroyImmediate(testAI);
                Debug.Log($"✅ Removed TestRepeaterAI from {enemy.name}");
            }

            // Re-enable other AI components
            var simpleAI = enemy.GetComponent<SimpleTestAI>();
            var knightAI = enemy.GetComponent<KnightAI>();
            var patternedAI = enemy.GetComponent<PatternedAI>();

            int enabledCount = 0;
            if (simpleAI != null)
            {
                simpleAI.enabled = true;
                enabledCount++;
            }
            if (knightAI != null)
            {
                knightAI.enabled = true;
                enabledCount++;
            }
            if (patternedAI != null && patternedAI != simpleAI && patternedAI != knightAI)
            {
                patternedAI.enabled = true;
                enabledCount++;
            }

            EditorUtility.SetDirty(enemy);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(enemy.scene);

            string message = $"Restored original AI on {enemy.name}.";
            if (enabledCount > 0)
            {
                message += $"\nRe-enabled {enabledCount} AI component(s).";
            }

            EditorUtility.DisplayDialog("AI Restored", message, "OK");
        }

        [MenuItem(MenuPath + "Configure All Enemies for Testing")]
        public static void ConfigureAllEnemiesForTesting()
        {
            var enemies = Object.FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            int configuredCount = 0;

            foreach (var combatant in enemies)
            {
                if (combatant.name.Contains("Enemy"))
                {
                    // Disable existing AI
                    var simpleAI = combatant.GetComponent<SimpleTestAI>();
                    var knightAI = combatant.GetComponent<KnightAI>();

                    if (simpleAI != null) simpleAI.enabled = false;
                    if (knightAI != null) knightAI.enabled = false;

                    // Add TestRepeaterAI
                    var testAI = combatant.GetComponent<TestRepeaterAI>();
                    if (testAI == null)
                    {
                        testAI = combatant.gameObject.AddComponent<TestRepeaterAI>();
                    }
                    else
                    {
                        testAI.enabled = true;
                    }

                    // Ensure debug visualizer
                    var debugVisualizer = combatant.GetComponent<CombatDebugVisualizer>();
                    if (debugVisualizer == null)
                    {
                        combatant.gameObject.AddComponent<CombatDebugVisualizer>();
                    }

                    EditorUtility.SetDirty(combatant.gameObject);
                    configuredCount++;
                }
            }

            if (configuredCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                EditorUtility.DisplayDialog("Configuration Complete",
                    $"Configured {configuredCount} enemy(ies) for test mode.\n\n" +
                    "All enemies now have TestRepeaterAI and can be controlled with hotkeys.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Enemies Found",
                    "No enemies found in the scene to configure.",
                    "OK");
            }
        }

        [MenuItem(MenuPath + "Quick Setup: Player vs Test Enemy")]
        public static void QuickSetupPlayerVsTestEnemy()
        {
            // This creates a simple test scene setup
            bool proceed = EditorUtility.DisplayDialog("Quick Setup",
                "This will setup the current scene for testing:\n" +
                "1. Configure first enemy as TestRepeaterAI\n" +
                "2. Add TestSkillSelector UI\n" +
                "3. Ensure debug visualizers are present\n\n" +
                "Proceed?",
                "Yes", "Cancel");

            if (!proceed) return;

            // Setup enemy
            GameObject enemy = GetSelectedOrFirstEnemy();
            if (enemy != null)
            {
                SetupTestEnemy();
            }
            else
            {
                Debug.LogWarning("No enemy found in scene. Skipping enemy setup.");
            }

            // Add UI
            var existing = Object.FindFirstObjectByType<TestSkillSelector>();
            if (existing == null)
            {
                AddTestUIToScene();
            }

            Debug.Log("✅ Quick setup complete! Press Play and use F1-F6 to control enemy skills.");
        }

        // Helper method to get enemy GameObject
        private static GameObject GetSelectedOrFirstEnemy()
        {
            // Check if an enemy is selected
            if (Selection.activeGameObject != null)
            {
                var combatController = Selection.activeGameObject.GetComponent<CombatController>();
                if (combatController != null && Selection.activeGameObject.name.Contains("Enemy"))
                {
                    return Selection.activeGameObject;
                }
            }

            // Find first enemy in scene
            var combatants = Object.FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var combatant in combatants)
            {
                if (combatant.name.Contains("Enemy"))
                {
                    return combatant.gameObject;
                }
            }

            return null;
        }

        // Validation for menu items
        [MenuItem(MenuPath + "Setup Test Enemy", true)]
        [MenuItem(MenuPath + "Restore Original AI", true)]
        private static bool ValidateEnemyInScene()
        {
            return GetSelectedOrFirstEnemy() != null;
        }
    }
}
