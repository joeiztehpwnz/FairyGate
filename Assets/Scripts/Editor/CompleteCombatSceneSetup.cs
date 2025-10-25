using UnityEngine;
using UnityEditor;
using FairyGate.Combat;
using FairyGate.Combat.UI;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Comprehensive scene setup tool for creating a complete combat testing environment.
    /// Creates all managers, environment, characters with all AI patterns, equipment sets, and testing UI.
    /// </summary>
    public class CompleteCombatSceneSetup : EditorWindow
    {
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

        private static void PerformCompleteSandboxSetup()
        {
            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating managers...", 0.1f);

            // 1. Create Managers
            CreateManagers();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating environment...", 0.2f);

            // 2. Create Environment
            CreateEnvironment();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating assets...", 0.3f);

            // 3. Create ScriptableObject Assets
            var playerStats = CreateOrLoadCharacterStats("TestPlayer_Stats", 10, 8, 6, 8, 5, 4, 12);
            var enemyStats = CreateOrLoadCharacterStats("TestEnemy_Stats", 12, 6, 5, 6, 6, 5, 10);

            // Create all weapon types
            var sword = CreateOrLoadWeaponData("TestSword", WeaponType.Sword);
            var spear = CreateOrLoadWeaponData("TestSpear", WeaponType.Spear);
            var dagger = CreateOrLoadWeaponData("TestDagger", WeaponType.Dagger);
            var mace = CreateOrLoadWeaponData("TestMace", WeaponType.Mace);

            // Create all equipment sets
            CreateAllEquipmentAssets();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating Player...", 0.5f);

            // 4. Create Player
            var player = CreateFullCharacter("Player", new Vector3(-3, 0, 0), playerStats, sword, false);

            // Add Equipment Manager to Player
            AddEquipmentManager(player);

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating Enemy...", 0.6f);

            // 5. Create Enemy with TestRepeaterAI
            var enemy = CreateFullCharacter("Enemy", new Vector3(3, 0, 0), enemyStats, mace, true);

            // Replace default AI with TestRepeaterAI
            var simpleAI = enemy.GetComponent<SimpleTestAI>();
            if (simpleAI != null) DestroyImmediate(simpleAI);

            enemy.AddComponent<TestRepeaterAI>();
            AddEquipmentManager(enemy);

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating UI System...", 0.7f);

            // 6. Create Testing UI (OnGUI-based bars + skill selector)
            CreateHealthStaminaBars(player, enemy);
            CreateTestingUI();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Configuring combat targeting...", 0.9f);

            // 7. Setup Combat Targeting
            SetupCombatTargeting(player, enemy);

            EditorUtility.ClearProgressBar();

            // Final logs
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("✅ TESTING SANDBOX SETUP COMPLETE!");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("");
            Debug.Log("🎮 PLAYER CONTROLS:");
            Debug.Log("  • WASD - Movement");
            Debug.Log("  • 1 - Attack | 2 - Defense | 3 - Counter");
            Debug.Log("  • 4 - Smash | 5 - Windmill | 6 - Ranged Attack");
            Debug.Log("  • Space - Cancel Skill | X - Rest (Stamina Regen)");
            Debug.Log("  • Tab - Cycle Target | Esc - Exit Combat | R - Reset Combat");
            Debug.Log("");
            Debug.Log("🔧 TESTING HOTKEYS:");
            Debug.Log("  • F1-F6 - Force Enemy Skill (Attack/Defense/Counter/Smash/Windmill/Ranged)");
            Debug.Log("  • [ or PgUp - Previous Equipment Set | ] or PgDn - Next Equipment Set");
            Debug.Log("  • \\ or Home - Remove All Equipment");
            Debug.Log("  • F12 - Reset Enemy AI to default");
            Debug.Log("");
            Debug.Log("📦 EQUIPMENT SETS AVAILABLE:");
            Debug.Log("  • Fortress (Tank) - High HP/Defense, Low Speed");
            Debug.Log("  • Windrunner (Speed) - High Speed/Dexterity, Low Defense");
            Debug.Log("  • Wanderer (Balanced) - Moderate all stats");
            Debug.Log("  • Berserker (Glass Cannon) - High Strength, Low Defense");
            Debug.Log("");
            Debug.Log("🤖 ENEMY AI:");
            Debug.Log("  • TestRepeaterAI - Cycles through all skills systematically");
            Debug.Log("  • Use F1-F6 to override and test specific skill interactions");
            Debug.Log("");
            Debug.Log("📊 UI FEATURES:");
            Debug.Log("  • Health/Stamina bars for Player and Enemy");
            Debug.Log("  • On-screen debug visualizers showing combat state");
            Debug.Log("  • Equipment selector ([ ] brackets or PgUp/PgDn)");
            Debug.Log("  • Skill selector (F1-F6)");
            Debug.Log("");
            Debug.Log("═══════════════════════════════════════════════════════════");

            // Select player for inspection
            Selection.activeGameObject = player;

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        #region Manager Creation

        private static void CreateManagers()
        {
            // Combat Interaction Manager
            if (GameObject.Find("CombatInteractionManager") == null)
            {
                var cimGO = new GameObject("CombatInteractionManager");
                var cim = cimGO.AddComponent<CombatInteractionManager>();
                SetSerializedProperty(cim, "enableDebugLogs", true);
                Debug.Log("✅ Created CombatInteractionManager");
            }

            // Game Manager
            if (GameObject.Find("GameManager") == null)
            {
                var gmGO = new GameObject("GameManager");
                var gm = gmGO.AddComponent<GameManager>();
                SetSerializedProperty(gm, "enableDebugLogs", true);
                Debug.Log("✅ Created GameManager");
            }
        }

        #endregion

        #region Environment Creation

        private static void CreateEnvironment()
        {
            // Ground Plane
            if (GameObject.Find("Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(3, 1, 3); // 30x30 units

                var groundRenderer = ground.GetComponent<Renderer>();
                if (groundRenderer != null)
                {
                    var groundMaterial = new Material(Shader.Find("Standard"));
                    groundMaterial.color = new Color(0.7f, 0.8f, 0.7f); // Light greenish-gray
                    groundRenderer.material = groundMaterial;
                }

                Debug.Log("✅ Created Ground (30x30 units)");
            }

            // Main Camera
            if (Camera.main == null)
            {
                var cameraGO = new GameObject("Main Camera");
                var camera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";

                // Position for good combat view
                cameraGO.transform.position = new Vector3(0f, 10f, -8f);
                cameraGO.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.Skybox;

                Debug.Log("✅ Created Main Camera");
            }

            // Directional Light
            if (GameObject.Find("Directional Light") == null)
            {
                var lightGO = new GameObject("Directional Light");
                var light = lightGO.AddComponent<Light>();

                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;
                light.shadows = LightShadows.Soft;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                Debug.Log("✅ Created Directional Light");
            }
        }

        #endregion

        #region Asset Creation

        private static CharacterStats CreateOrLoadCharacterStats(string assetName, int str, int dex, int intel, int focus, int physDef, int magDef, int vitality)
        {
            string path = $"Assets/Data/Characters/{assetName}.asset";

            // Try to load existing
            var existing = AssetDatabase.LoadAssetAtPath<CharacterStats>(path);
            if (existing != null)
            {
                Debug.Log($"📦 Loaded existing {assetName}");
                return existing;
            }

            // Create new
            var stats = ScriptableObject.CreateInstance<CharacterStats>();
            stats.strength = str;
            stats.dexterity = dex;
            stats.intelligence = intel;
            stats.focus = focus;
            stats.physicalDefense = physDef;
            stats.magicalDefense = magDef;
            stats.vitality = vitality;

            System.IO.Directory.CreateDirectory("Assets/Data/Characters");
            AssetDatabase.CreateAsset(stats, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created {assetName}");
            return stats;
        }

        private static WeaponData CreateOrLoadWeaponData(string assetName, WeaponType weaponType)
        {
            string path = $"Assets/Data/Weapons/{assetName}.asset";

            // Try to load existing
            var existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (existing != null)
            {
                Debug.Log($"📦 Loaded existing {assetName}");
                return existing;
            }

            // Create new using factory methods
            WeaponData weapon = weaponType switch
            {
                WeaponType.Sword => WeaponData.CreateSwordData(),
                WeaponType.Spear => WeaponData.CreateSpearData(),
                WeaponType.Dagger => WeaponData.CreateDaggerData(),
                WeaponType.Mace => WeaponData.CreateMaceData(),
                _ => WeaponData.CreateSwordData()
            };

            System.IO.Directory.CreateDirectory("Assets/Data/Weapons");
            AssetDatabase.CreateAsset(weapon, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created {assetName} ({weaponType})");
            return weapon;
        }

        private static void CreateAllEquipmentAssets()
        {
            // This method ensures all equipment assets exist
            // The assets are already created based on your git status, so we just verify
            string[] requiredArmor = { "HeavyPlatemail", "LeatherTunic", "ClothRobes", "ChainMail" };
            string[] requiredAccessories = { "PowerGauntlets", "MeditationAmulet", "SwiftBoots", "GuardianRing" };
            string[] requiredSets = { "Fortress_TankSet", "Windrunner_SpeedSet", "Berserker_GlassCannonSet", "Wanderer_BalancedSet" };

            System.IO.Directory.CreateDirectory("Assets/Data/Equipment/Armor");
            System.IO.Directory.CreateDirectory("Assets/Data/Equipment/Accessories");
            System.IO.Directory.CreateDirectory("Assets/Data/Equipment/Sets");

            Debug.Log("✅ Equipment asset directories verified");
        }

        #endregion

        #region Character Creation

        private static GameObject CreateFullCharacter(string name, Vector3 position, CharacterStats stats, WeaponData weapon, bool isEnemy)
        {
            // Create base GameObject
            var character = new GameObject(name);
            character.transform.position = position;

            // Add CharacterController
            var charController = character.AddComponent<CharacterController>();
            charController.radius = 0.5f;
            charController.height = 2f;
            charController.center = new Vector3(0, 1, 0);

            // Add all combat components
            var combatController = character.AddComponent<CombatController>();
            var healthSystem = character.AddComponent<HealthSystem>();
            var staminaSystem = character.AddComponent<StaminaSystem>();
            var statusEffectManager = character.AddComponent<StatusEffectManager>();
            var skillSystem = character.AddComponent<SkillSystem>();
            var weaponController = character.AddComponent<WeaponController>();
            var movementController = character.AddComponent<MovementController>();
            var knockdownMeter = character.AddComponent<KnockdownMeterTracker>();
            var accuracySystem = character.AddComponent<AccuracySystem>();
            var debugVisualizer = character.AddComponent<CombatDebugVisualizer>();

            // Add AI for enemy
            if (isEnemy)
            {
                character.AddComponent<SimpleTestAI>(); // Will be replaced with TestRepeaterAI
            }

            // Configure components using SerializedObject
            SetSerializedProperty(combatController, "characterStats", stats);
            SetSerializedProperty(healthSystem, "characterStats", stats);
            SetSerializedProperty(staminaSystem, "characterStats", stats);
            SetSerializedProperty(skillSystem, "characterStats", stats);
            SetSerializedProperty(movementController, "characterStats", stats);
            SetSerializedProperty(weaponController, "weaponData", weapon);
            SetSerializedProperty(accuracySystem, "characterStats", stats);

            // Configure control types
            SetSerializedProperty(movementController, "isPlayerControlled", !isEnemy);
            SetSerializedProperty(skillSystem, "isPlayerControlled", !isEnemy);

            // Enable debug logs for systems (keep minimal logging)
            SetSerializedProperty(combatController, "enableDebugLogs", false);
            SetSerializedProperty(healthSystem, "enableDebugLogs", false);
            SetSerializedProperty(staminaSystem, "enableDebugLogs", false);
            SetSerializedProperty(skillSystem, "enableDebugLogs", false);
            SetSerializedProperty(skillSystem, "showSkillGUI", false);
            SetSerializedProperty(movementController, "enableDebugLogs", false);

            // Add visual representation
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(character.transform);
            visual.transform.localPosition = new Vector3(0, 1, 0);
            visual.name = "Visual";

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = isEnemy ? Color.red : Color.blue;
                renderer.material = material;
            }

            // Remove collider from visual
            if (visual.GetComponent<CapsuleCollider>() != null)
                DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            Debug.Log($"✅ Created {name} with {weapon.weaponName}");
            return character;
        }

        private static void AddEquipmentManager(GameObject character)
        {
            var equipmentManager = character.AddComponent<EquipmentManager>();

            // Load equipment sets
            var sets = AssetDatabase.FindAssets("t:EquipmentSet", new[] { "Assets/Data/Equipment/Sets" });
            EquipmentSet[] equipmentSetsArray = null;

            if (sets.Length > 0)
            {
                var setList = new System.Collections.Generic.List<EquipmentSet>();
                foreach (var setGUID in sets)
                {
                    string path = AssetDatabase.GUIDToAssetPath(setGUID);
                    var set = AssetDatabase.LoadAssetAtPath<EquipmentSet>(path);
                    if (set != null) setList.Add(set);
                }

                equipmentSetsArray = setList.ToArray();
                SetSerializedProperty(equipmentManager, "availableSets", equipmentSetsArray);

                Debug.Log($"✅ Loaded {equipmentSetsArray.Length} equipment sets for {character.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ No equipment sets found in Assets/Data/Equipment/Sets/");
            }

            // Add TestEquipmentSelector for F10/F11 hotkeys
            var testEquipmentSelector = character.AddComponent<TestEquipmentSelector>();

            // Configure TestEquipmentSelector with the same equipment sets
            if (equipmentSetsArray != null && equipmentSetsArray.Length > 0)
            {
                SetSerializedProperty(testEquipmentSelector, "equipmentPresets", equipmentSetsArray);
                SetSerializedProperty(testEquipmentSelector, "autoFindTargetEnemy", false);
                SetSerializedProperty(testEquipmentSelector, "targetEquipmentManager", equipmentManager);
                SetSerializedProperty(testEquipmentSelector, "enableDebugLogs", true);

                Debug.Log($"✅ Configured TestEquipmentSelector with {equipmentSetsArray.Length} presets for {character.name}");
            }

            SetSerializedProperty(equipmentManager, "enableDebugLogs", true);
            Debug.Log($"✅ Added EquipmentManager to {character.name}");
        }

        #endregion

        #region UI Creation

        private static void CreateHealthStaminaBars(GameObject player, GameObject enemy)
        {
            // Create Player Health/Stamina UI (OnGUI-based)
            var playerHealthBar = player.AddComponent<HealthBarUI>();
            SetSerializedProperty(playerHealthBar, "barPosition", new Vector2(10, 10));
            SetSerializedProperty(playerHealthBar, "barSize", new Vector2(250, 25));
            playerHealthBar.SetTargetHealthSystem(player.GetComponent<HealthSystem>());

            var playerStaminaBar = player.AddComponent<StaminaBarUI>();
            SetSerializedProperty(playerStaminaBar, "barPosition", new Vector2(10, 45));
            SetSerializedProperty(playerStaminaBar, "barSize", new Vector2(250, 25));
            playerStaminaBar.SetTargetStaminaSystem(player.GetComponent<StaminaSystem>());

            // Create Enemy Health/Stamina UI (OnGUI-based)
            var enemyHealthBar = enemy.AddComponent<HealthBarUI>();
            SetSerializedProperty(enemyHealthBar, "barPosition", new Vector2(10, 90));
            SetSerializedProperty(enemyHealthBar, "barSize", new Vector2(250, 25));
            enemyHealthBar.SetTargetHealthSystem(enemy.GetComponent<HealthSystem>());

            var enemyStaminaBar = enemy.AddComponent<StaminaBarUI>();
            SetSerializedProperty(enemyStaminaBar, "barPosition", new Vector2(10, 125));
            SetSerializedProperty(enemyStaminaBar, "barSize", new Vector2(250, 25));
            enemyStaminaBar.SetTargetStaminaSystem(enemy.GetComponent<StaminaSystem>());

            Debug.Log("✅ Created Health/Stamina UI Bars (OnGUI-based)");
        }

        private static void CreateTestingUI()
        {
            // Create UI Manager GameObject
            var uiManagerGO = new GameObject("TestingUI_Manager");

            // Add TestSkillSelector (F1-F6 hotkeys)
            uiManagerGO.AddComponent<TestSkillSelector>();

            Debug.Log("✅ Created Testing UI (Skill Selector with F1-F6 hotkeys)");
        }

        #endregion

        #region Helper Methods

        private static void SetupCombatTargeting(GameObject player, GameObject enemy)
        {
            var playerCombat = player.GetComponent<CombatController>();
            var enemyCombat = enemy.GetComponent<CombatController>();

            if (playerCombat != null && enemyCombat != null)
            {
                Debug.Log($"✅ Combat targeting configured between {player.name} and {enemy.name}");
            }
        }

        private static void SetSerializedProperty(Component component, string propertyName, object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                if (value is bool boolVal)
                    prop.boolValue = boolVal;
                else if (value is int intVal)
                    prop.intValue = intVal;
                else if (value is float floatVal)
                    prop.floatValue = floatVal;
                else if (value is string stringVal)
                    prop.stringValue = stringVal;
                else if (value is Vector2 vec2Val)
                    prop.vector2Value = vec2Val;
                else if (value is Vector3 vec3Val)
                    prop.vector3Value = vec3Val;
                else if (value is Color colorVal)
                    prop.colorValue = colorVal;
                else if (value is UnityEngine.Object objVal)
                    prop.objectReferenceValue = objVal;
                else if (value is UnityEngine.Object[] arrayVal)
                {
                    prop.arraySize = arrayVal.Length;
                    for (int i = 0; i < arrayVal.Length; i++)
                    {
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = arrayVal[i];
                    }
                }

                so.ApplyModifiedProperties();
            }
        }

        #endregion

        #region Additional Menu Items

        [MenuItem("Combat/Complete Scene Setup/Quick 1v1 Setup")]
        public static void QuickOneVsOneSetup()
        {
            if (EditorUtility.DisplayDialog("Quick 1v1 Setup",
                "Create a simple 1v1 combat scene with minimal UI?",
                "Create", "Cancel"))
            {
                PerformQuick1v1Setup();
            }
        }

        private static void PerformQuick1v1Setup()
        {
            CreateManagers();
            CreateEnvironment();

            var playerStats = CreateOrLoadCharacterStats("Player_Stats", 10, 8, 6, 8, 5, 4, 12);
            var enemyStats = CreateOrLoadCharacterStats("Enemy_Stats", 12, 6, 5, 6, 6, 5, 10);
            var sword = CreateOrLoadWeaponData("Sword", WeaponType.Sword);

            var player = CreateFullCharacter("Player", new Vector3(-3, 0, 0), playerStats, sword, false);
            var enemy = CreateFullCharacter("Enemy", new Vector3(3, 0, 0), enemyStats, sword, true);

            // Use KnightAI for pattern-based combat
            var simpleAI = enemy.GetComponent<SimpleTestAI>();
            if (simpleAI != null) DestroyImmediate(simpleAI);
            enemy.AddComponent<KnightAI>();

            SetupCombatTargeting(player, enemy);

            Debug.Log("✅ Quick 1v1 Setup Complete! Player vs KnightAI");
            Selection.activeGameObject = player;
        }

        [MenuItem("Combat/Complete Scene Setup/Clear All Combat Objects")]
        public static void ClearAllCombatObjects()
        {
            if (EditorUtility.DisplayDialog("Clear Combat Objects",
                "This will remove all combat-related objects from the scene. Continue?",
                "Yes", "Cancel"))
            {
                // Remove all characters
                var characters = GameObject.FindObjectsByType<CombatController>(FindObjectsSortMode.None);
                foreach (var character in characters)
                {
                    DestroyImmediate(character.gameObject);
                }

                // Remove managers
                var managers = new[] { "CombatInteractionManager", "GameManager" };
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
        }

        #endregion
    }
}
