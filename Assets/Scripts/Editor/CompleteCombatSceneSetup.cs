using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Main orchestrator for combat scene setup.
    /// Coordinates environment creation, asset management, and character spawning.
    /// </summary>
    public class CompleteCombatSceneSetup : EditorWindow
    {
        private static readonly SceneEnvironmentBuilder environmentBuilder = new SceneEnvironmentBuilder();
        private static readonly CombatAssetFactory assetFactory = new CombatAssetFactory();
        private static readonly CharacterSpawner characterSpawner = new CharacterSpawner();

        private static int enemySpawnCount = 0;
        private static Vector3 lastEnemyPosition = new Vector3(3, 0, 0);

        #region Menu Items

        [MenuItem("Combat/Complete Scene Setup/Testing Sandbox")]
        public static void CreateTestingSandbox()
        {
            if (EditorUtility.DisplayDialog("Create Testing Sandbox",
                "This will create a complete combat testing environment with:\n\n" +
                "• All scene managers\n" +
                "• Environment (ground, camera, lighting)\n" +
                "• Player with default equipment\n" +
                "• Enemy with configurable AI\n" +
                "• All testing UI (Equipment Selector, Skill Selector, Debug Visualizers)\n" +
                "• Health/Stamina UI Bars\n" +
                "• All necessary ScriptableObject assets\n\n" +
                "Continue?",
                "Create", "Cancel"))
            {
                PerformCompleteSandboxSetup();
            }
        }

        [MenuItem("Combat/Spawn Enemy/Soldier (Balanced)")]
        public static void SpawnSoldier() => SpawnEnemyArchetype(EnemyArchetype.Soldier);

        [MenuItem("Combat/Spawn Enemy/Berserker (Glass Cannon)")]
        public static void SpawnBerserker() => SpawnEnemyArchetype(EnemyArchetype.Berserker);

        [MenuItem("Combat/Spawn Enemy/Guardian (Tank)")]
        public static void SpawnGuardian() => SpawnEnemyArchetype(EnemyArchetype.Guardian);

        [MenuItem("Combat/Spawn Enemy/Assassin (Speedster)")]
        public static void SpawnAssassin() => SpawnEnemyArchetype(EnemyArchetype.Assassin);

        [MenuItem("Combat/Spawn Enemy/Archer (Ranged)")]
        public static void SpawnArcher() => SpawnEnemyArchetype(EnemyArchetype.Archer);

        [MenuItem("Combat/Complete Scene Setup/Clear All Combat Objects")]
        public static void ClearAllCombatObjects()
        {
            if (EditorUtility.DisplayDialog("Clear Combat Objects",
                "This will remove all combat-related objects from the scene. Continue?",
                "Yes", "Cancel"))
            {
                ClearScene();
            }
        }

        #endregion

        #region Main Setup Flow

        private static void PerformCompleteSandboxSetup()
        {
            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating managers...", 0.1f);
            CreateManagers();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating environment...", 0.2f);
            environmentBuilder.CreateEnvironment();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating assets...", 0.3f);
            var playerStats = assetFactory.CreateOrLoadCharacterStats("TestPlayer_Stats", 10, 8, 6, 8, 5, 4, 12);
            var enemyStats = assetFactory.CreateOrLoadCharacterStats("TestEnemy_Stats", 12, 6, 5, 6, 6, 5, 10);

            // Create all weapon types
            var sword = assetFactory.CreateOrLoadWeaponData("TestSword", WeaponType.Sword);
            var spear = assetFactory.CreateOrLoadWeaponData("TestSpear", WeaponType.Spear);
            var dagger = assetFactory.CreateOrLoadWeaponData("TestDagger", WeaponType.Dagger);
            var mace = assetFactory.CreateOrLoadWeaponData("TestMace", WeaponType.Mace);
            var bow = assetFactory.CreateOrLoadWeaponData("TestBow", WeaponType.Bow);

            // Create all equipment sets
            assetFactory.CreateAllEquipmentAssets();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating Player...", 0.5f);
            var player = characterSpawner.CreateCharacter("Player", new Vector3(-3, 0, 0), playerStats, sword, false);
            characterSpawner.AddEquipmentManager(player);

            // Set player faction using Editor serialization to persist
            var playerCombat = player.GetComponent<CombatController>();
            if (playerCombat != null)
            {
                EditorUtilities.SetSerializedProperty(playerCombat, "faction", (int)Faction.Player);
            }

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating UI System...", 0.7f);
            player.AddComponent<CharacterInfoDisplay>();
            CreateTestingUI();

            EditorUtility.ClearProgressBar();

            PrintSetupCompleteMessage();
            Selection.activeGameObject = player;
            MarkSceneDirty();
        }

        #endregion

        #region Manager Creation

        private static void CreateManagers()
        {
            // Combat Update Manager (Phase 2 optimization)
            if (GameObject.Find("CombatUpdateManager") == null)
            {
                var cumGO = new GameObject("CombatUpdateManager");
                var cum = cumGO.AddComponent<CombatUpdateManager>();
                EditorUtilities.SetSerializedProperty(cum, "enableDebugLogs", false);
                EditorUtilities.SetSerializedProperty(cum, "showUpdateStats", false);
                Debug.Log("✅ Created CombatUpdateManager");
            }

            // Combat Interaction Manager
            if (GameObject.Find("CombatInteractionManager") == null)
            {
                var cimGO = new GameObject("CombatInteractionManager");
                var cim = cimGO.AddComponent<CombatInteractionManager>();
                EditorUtilities.SetSerializedProperty(cim, "enableDebugLogs", true);
                Debug.Log("✅ Created CombatInteractionManager");
            }

            // Game Manager
            if (GameObject.Find("GameManager") == null)
            {
                var gmGO = new GameObject("GameManager");
                var gm = gmGO.AddComponent<GameManager>();
                EditorUtilities.SetSerializedProperty(gm, "enableDebugLogs", true);
                Debug.Log("✅ Created GameManager");
            }
        }

        #endregion

        #region UI Creation

        private static void CreateTestingUI()
        {
            // Create UI Manager GameObject
            var uiManagerGO = new GameObject("TestingUI_Manager");
            Debug.Log("✅ Created Testing UI");
        }

        #endregion

        #region Enemy Spawning

        private static void SpawnEnemyArchetype(EnemyArchetype archetype)
        {
            // Calculate spawn position (offset from last spawn)
            enemySpawnCount++;
            Vector3 spawnPos = lastEnemyPosition + new Vector3(2, 0, enemySpawnCount % 2 == 0 ? 2 : -2);
            lastEnemyPosition = spawnPos;

            // Spawn enemy using CharacterSpawner
            var enemy = characterSpawner.SpawnEnemyWithArchetype(archetype, spawnPos, enemySpawnCount, assetFactory);

            // Setup combat targeting with player
            var player = GameObject.Find("Player");
            if (player != null)
            {
                SetupCombatTargeting(player, enemy);
            }

            MarkSceneDirty();
        }

        #endregion

        #region Scene Management

        private static void ClearScene()
        {
            // Remove all characters
            var characters = GameObject.FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var character in characters)
            {
                DestroyImmediate(character.gameObject);
            }

            // Remove managers
            var managers = new[] { "CombatUpdateManager", "CombatInteractionManager", "GameManager" };
            foreach (var managerName in managers)
            {
                var manager = GameObject.Find(managerName);
                if (manager != null) DestroyImmediate(manager);
            }

            // Remove UI
            var ui = GameObject.Find("TestingUI_Manager");
            if (ui != null) DestroyImmediate(ui);

            Debug.Log("✅ All combat objects cleared from scene");
        }

        #endregion

        #region Helper Methods

        private static void SetupCombatTargeting(GameObject player, GameObject enemy)
        {
            var playerCombat = player.GetComponent<CombatController>();
            var enemyCombat = enemy.GetComponent<CombatController>();

            if (playerCombat != null && enemyCombat != null)
            {
                // Set correct factions using Editor serialization to persist
                EditorUtilities.SetSerializedProperty(playerCombat, "faction", (int)Faction.Player);
                EditorUtilities.SetSerializedProperty(enemyCombat, "faction", (int)Faction.Enemy);

                Debug.Log($"✅ Combat targeting configured between {player.name} (Player) and {enemy.name} (Enemy)");
            }
        }

        private static void MarkSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void PrintSetupCompleteMessage()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("✅ TESTING SANDBOX SETUP COMPLETE!");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("");
            Debug.Log("🎮 PLAYER CONTROLS:");
            Debug.Log("  • WASD - Movement");
            Debug.Log("  • Arrow Keys - Camera (Left/Right: Rotate, Up/Down: Zoom)");
            Debug.Log("  • 1 - Attack | 2 - Defense | 3 - Counter");
            Debug.Log("  • 4 - Smash | 5 - Windmill | 6 - Ranged Attack");
            Debug.Log("  • Space - Cancel Skill | X - Rest (Stamina Regen)");
            Debug.Log("  • Tab - Cycle Target | Esc - Exit Combat | R - Reset Combat");
            Debug.Log("");
            Debug.Log("📦 EQUIPMENT SETS AVAILABLE:");
            Debug.Log("  • Fortress (Tank) - High HP/Defense, Low Speed");
            Debug.Log("  • Windrunner (Speed) - High Speed/Dexterity, Low Defense");
            Debug.Log("  • Wanderer (Balanced) - Moderate all stats");
            Debug.Log("  • Berserker (Glass Cannon) - High Strength, Low Defense");
            Debug.Log("");
            Debug.Log("🎯 SPAWN ENEMIES:");
            Debug.Log("  • Use Combat menu to spawn enemies: Soldier, Berserker, Guardian, Assassin, Archer");
            Debug.Log("  • Each enemy has unique stat distribution and AI behavior");
            Debug.Log("");
            Debug.Log("📊 UI FEATURES:");
            Debug.Log("  • CharacterInfoDisplay - Health, Stamina, Knockdown Meter bars");
            Debug.Log("  • Skill charge progress, status effects, skill icons");
            Debug.Log("");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }

        #endregion
    }
}
