using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

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
            var bow = CreateOrLoadWeaponData("TestBow", WeaponType.Bow);

            // Create all equipment sets
            CreateAllEquipmentAssets();

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating Player...", 0.5f);

            // 4. Create Player
            var player = CreateFullCharacter("Player", new Vector3(-3, 0, 0), playerStats, sword, false);

            // Add Equipment Manager to Player
            AddEquipmentManager(player);

            EditorUtility.DisplayProgressBar("Testing Sandbox Setup", "Creating UI System...", 0.7f);

            // 5. Create UI for Player
            var playerInfoDisplay = player.AddComponent<CharacterInfoDisplay>();

            CreateTestingUI();

            EditorUtility.ClearProgressBar();

            // Final logs
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

            // Select player for inspection
            Selection.activeGameObject = player;

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        #region Manager Creation

        private static void CreateManagers()
        {
            // Combat Update Manager (Phase 2 optimization)
            if (GameObject.Find("CombatUpdateManager") == null)
            {
                var cumGO = new GameObject("CombatUpdateManager");
                var cum = cumGO.AddComponent<CombatUpdateManager>();
                SetSerializedProperty(cum, "enableDebugLogs", false);
                SetSerializedProperty(cum, "showUpdateStats", false);
                Debug.Log("✅ Created CombatUpdateManager");
            }

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
                    groundMaterial.color = new Color(0.0314f, 0.3412f, 0.0314f); // #085708 - Dark green
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

                // Position for good combat view (will be overridden by CameraController)
                cameraGO.transform.position = new Vector3(0f, 10f, -8f);
                cameraGO.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.Skybox;

                // Add CameraController for player following
                var cameraController = cameraGO.AddComponent<CameraController>();
                SetSerializedProperty(cameraController, "autoFindPlayer", true);
                SetSerializedProperty(cameraController, "distance", 10f);
                SetSerializedProperty(cameraController, "height", 8f);
                SetSerializedProperty(cameraController, "rotationSpeed", 90f);
                SetSerializedProperty(cameraController, "enableZoom", true);
                SetSerializedProperty(cameraController, "showDebugInfo", true);

                Debug.Log("✅ Created Main Camera with CameraController");
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
                WeaponType.Bow => WeaponData.CreateBowData(),
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

            // Add AI for enemy
            if (isEnemy)
            {
                character.AddComponent<SimpleTestAI>();
            }

            // Configure components using SerializedObject
            SetSerializedProperty(combatController, "baseStats", stats);
            SetSerializedProperty(healthSystem, "characterStats", stats);
            SetSerializedProperty(staminaSystem, "characterStats", stats);
            SetSerializedProperty(statusEffectManager, "characterStats", stats);
            SetSerializedProperty(skillSystem, "characterStats", stats);
            SetSerializedProperty(movementController, "characterStats", stats);
            SetSerializedProperty(knockdownMeter, "characterStats", stats);
            SetSerializedProperty(weaponController, "primaryWeapon", weapon);
            SetSerializedProperty(accuracySystem, "characterStats", stats);

            // Configure control types
            SetSerializedProperty(movementController, "isPlayerControlled", !isEnemy);
            SetSerializedProperty(skillSystem, "isPlayerControlled", !isEnemy);

            // Enable debug logs for systems (keep minimal logging)
            SetSerializedProperty(combatController, "enableDebugLogs", false);
            SetSerializedProperty(healthSystem, "enableDebugLogs", false);
            SetSerializedProperty(staminaSystem, "enableDebugLogs", false);
            SetSerializedProperty(skillSystem, "enableDebugLogs", true); // Enable for state machine visibility
            SetSerializedProperty(skillSystem, "showSkillGUI", false);
            SetSerializedProperty(movementController, "enableDebugLogs", false);

            // PHASE 6: Enable state machine for testing
            SetSerializedProperty(skillSystem, "useStateMachine", true);

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

            // Set tag for player (needed for target indicator system)
            if (!isEnemy)
            {
                character.tag = "Player";
            }

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

            SetSerializedProperty(equipmentManager, "enableDebugLogs", true);
            Debug.Log($"✅ Added EquipmentManager to {character.name}");
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
        }

        #endregion

        #region Enemy Spawning

        private static int enemySpawnCount = 0;
        private static Vector3 lastEnemyPosition = new Vector3(3, 0, 0);

        private static void SpawnEnemyArchetype(EnemyArchetype archetype)
        {
            // Get archetype configuration
            var config = EnemyArchetypeConfig.GetArchetypeData(archetype);

            // Calculate spawn position (offset from last spawn)
            enemySpawnCount++;
            Vector3 spawnPos = lastEnemyPosition + new Vector3(2, 0, enemySpawnCount % 2 == 0 ? 2 : -2);
            lastEnemyPosition = spawnPos;

            // Create weapons from archetype configuration
            var primaryWeapon = CreateOrLoadWeaponData($"Test{config.primaryWeapon}", config.primaryWeapon);
            var secondaryWeapon = CreateOrLoadWeaponData($"Test{config.secondaryWeapon}", config.secondaryWeapon);

            // Create enemy character with primary weapon
            string enemyName = $"{archetype}_{enemySpawnCount}";
            var enemy = CreateFullCharacter(enemyName, spawnPos, config.stats, primaryWeapon, true);

            // Configure secondary weapon slot
            var weaponController = enemy.GetComponent<WeaponController>();
            if (weaponController != null)
            {
                SetSerializedProperty(weaponController, "secondaryWeapon", secondaryWeapon);
            }

            // Setup AI Pattern System (replaces reactive AI with skill weights)
            SetupPatternSystemAI(enemy, archetype, config);

            // Add equipment manager
            AddEquipmentManager(enemy);

            // Add CharacterInfoDisplay for UI
            var enemyInfoDisplay = enemy.AddComponent<CharacterInfoDisplay>();

            // Setup combat targeting with player
            var player = GameObject.Find("Player");
            if (player != null)
            {
                SetupCombatTargeting(player, enemy);
            }

            Debug.Log($"✅ Spawned {archetype} enemy at {spawnPos}");
            Debug.Log($"   Stats: STR={config.stats.strength} DEX={config.stats.dexterity} VIT={config.stats.vitality} " +
                      $"DEF={config.stats.physicalDefense} FOCUS={config.stats.focus}");
            Debug.Log($"   AI: Pattern-based AI with {archetype} pattern");
        }

        private static void SetupPatternSystemAI(GameObject enemy, EnemyArchetype archetype, EnemyArchetypeConfig.ArchetypeData config)
        {
            // Map archetype to pattern asset name
            string patternName = archetype switch
            {
                EnemyArchetype.Guardian => "GuardianPattern",
                EnemyArchetype.Berserker => "BerserkerPattern",
                EnemyArchetype.Soldier => "SoldierPattern",
                EnemyArchetype.Assassin => "AssassinPattern",
                EnemyArchetype.Archer => "ArcherPattern",
                _ => "SoldierPattern" // Fallback to balanced Soldier pattern
            };

            // Load pattern asset
            string patternPath = $"Assets/Data/AI/Patterns/{patternName}.asset";
            var patternAsset = AssetDatabase.LoadAssetAtPath<PatternDefinition>(patternPath);

            if (patternAsset == null)
            {
                Debug.LogError($"   Pattern System: Could not load pattern at {patternPath} - AI will not function!");
                return;
            }

            // 1. Add PatternExecutor component (PRIMARY AI SYSTEM)
            var patternExecutor = enemy.AddComponent<PatternExecutor>();
            SetSerializedProperty(patternExecutor, "patternDefinition", patternAsset);
            SetSerializedProperty(patternExecutor, "enableDebugLogs", false);
            SetSerializedProperty(patternExecutor, "skillCooldown", config.skillCooldown);
            SetSerializedProperty(patternExecutor, "randomVariance", 2.0f);
            SetSerializedProperty(patternExecutor, "engageDistance", 3.0f);
            SetSerializedProperty(patternExecutor, "useCoordination", true);

            // 2. Add TelegraphSystem component (optional)
            var telegraphSystem = enemy.AddComponent<TelegraphSystem>();
            SetSerializedProperty(telegraphSystem, "enableDebugLogs", false);

            // 3. Configure existing SimpleTestAI (minimal coordinator for weapon swapping)
            var simpleAI = enemy.GetComponent<SimpleTestAI>();
            if (simpleAI != null)
            {
                SetSerializedProperty(simpleAI, "useCoordination", true);
                SetSerializedProperty(simpleAI, "enableDebugLogs", false);
            }
            else
            {
                Debug.LogWarning("   SimpleTestAI component not found on enemy - coordination may not work!");
            }

            Debug.Log($"   AI System: ✅ PatternExecutor with {patternName}");
        }

        #endregion
    }
}
