using UnityEngine;
using UnityEditor;
using FairyGate.Combat;
using FairyGate.Combat.UI;

namespace FairyGate.Combat.Editor
{
    public class SceneSetup : EditorWindow
    {
        [MenuItem("Combat/Setup Scene")]
        public static void SetupCombatScene()
        {
            if (EditorUtility.DisplayDialog("Setup Combat Scene",
                "This will create Player, Enemy, and Manager objects in the current scene. Continue?",
                "Yes", "Cancel"))
            {
                PerformSetup();
            }
        }

        private static void PerformSetup()
        {
            EditorUtility.DisplayProgressBar("Setting up Combat Scene", "Creating managers...", 0.1f);

            // Create managers
            CreateCombatManagers();

            EditorUtility.DisplayProgressBar("Setting up Combat Scene", "Creating environment...", 0.2f);

            // Create environment
            CreateEnvironment();

            EditorUtility.DisplayProgressBar("Setting up Combat Scene", "Creating character assets...", 0.4f);

            // Create ScriptableObject assets
            var playerStats = CreateCharacterStats("PlayerStats", 10, 8, 6, 8, 4, 3, 12); // str, dex, int, focus, physDef, magDef, vitality
            var enemyStats = CreateCharacterStats("EnemyStats", 12, 6, 5, 6, 6, 5, 10); // Different stats for variety
            var swordWeapon = CreateWeaponData("Sword");

            EditorUtility.DisplayProgressBar("Setting up Combat Scene", "Creating Player...", 0.6f);

            // Create Player
            var player = CreateCharacter("Player", Vector3.zero, playerStats, swordWeapon, false);

            EditorUtility.DisplayProgressBar("Setting up Combat Scene", "Creating Enemy...", 0.8f);

            // Create Enemy
            var enemy = CreateCharacter("Enemy", new Vector3(5, 0, 0), enemyStats, swordWeapon, true);

            EditorUtility.DisplayProgressBar("Setting up Combat Scene", "Finalizing setup...", 0.9f);

            // Set up combat targeting
            SetupCombatTargeting(player, enemy);

            EditorUtility.ClearProgressBar();

            Debug.Log("✅ Combat scene setup complete! Press Play to test the pattern-based AI system.");
            Debug.Log("🎮 Controls: WASD (move), 1-5 (skills), Space (cancel), Tab (target), Esc (exit combat), X (rest), R (reset)");
            Debug.Log("🤖 Enemy AI: KnightAI with 8-second defensive pattern cycle");
            Debug.Log("📚 Learn the pattern: Charge Defense → Wait → Cancel → Charge Smash → Execute → Recovery");

            // Select the Player for easy inspection
            Selection.activeGameObject = player;
        }

        private static void CreateCombatManagers()
        {
            // Combat Interaction Manager
            if (GameObject.Find("CombatInteractionManager") == null)
            {
                var cimGO = new GameObject("CombatInteractionManager");
                var cim = cimGO.AddComponent<CombatInteractionManager>();
                // Enable debug logs
                var so = new SerializedObject(cim);
                var enableDebugProp = so.FindProperty("enableDebugLogs");
                if (enableDebugProp != null)
                {
                    enableDebugProp.boolValue = true;
                    so.ApplyModifiedProperties();
                }
            }

            // Game Manager
            if (GameObject.Find("GameManager") == null)
            {
                var gmGO = new GameObject("GameManager");
                var gm = gmGO.AddComponent<GameManager>();
                // Enable debug logs
                var so = new SerializedObject(gm);
                var enableDebugProp = so.FindProperty("enableDebugLogs");
                if (enableDebugProp != null)
                {
                    enableDebugProp.boolValue = true;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void CreateEnvironment()
        {
            // Create Ground Plane
            if (GameObject.Find("Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(2, 1, 2); // 20x20 units

                // Add a simple material
                var groundRenderer = ground.GetComponent<Renderer>();
                if (groundRenderer != null)
                {
                    var groundMaterial = new Material(Shader.Find("Standard"));
                    groundMaterial.color = new Color(0.8f, 0.8f, 0.8f); // Light gray
                    groundRenderer.material = groundMaterial;
                }

                Debug.Log("✅ Ground plane created (20x20 units)");
            }

            // Create Main Camera
            if (Camera.main == null)
            {
                var cameraGO = new GameObject("Main Camera");
                var camera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";

                // Position camera for good combat view
                cameraGO.transform.position = new Vector3(2.5f, 8f, -6f);
                cameraGO.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

                // Configure camera settings
                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.Skybox;

                Debug.Log("✅ Main Camera created with combat-optimized positioning");
            }

            // Create Directional Light
            if (GameObject.Find("Directional Light") == null)
            {
                var lightGO = new GameObject("Directional Light");
                var light = lightGO.AddComponent<Light>();

                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;
                light.shadows = LightShadows.Soft;

                // Position light for good scene illumination
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                Debug.Log("✅ Directional Light created with soft shadows");
            }
        }

        private static CharacterStats CreateCharacterStats(string name, int str, int dex, int intel, int focus, int physDef, int magDef, int vitality)
        {
            var stats = ScriptableObject.CreateInstance<CharacterStats>();
            stats.strength = str;
            stats.dexterity = dex;
            stats.intelligence = intel;
            stats.focus = focus;
            stats.physicalDefense = physDef;
            stats.magicalDefense = magDef;
            stats.vitality = vitality;

            // Save as asset
            string path = $"Assets/Data/Characters/{name}.asset";
            System.IO.Directory.CreateDirectory("Assets/Data/Characters");
            AssetDatabase.CreateAsset(stats, path);
            AssetDatabase.SaveAssets();

            return stats;
        }

        private static WeaponData CreateWeaponData(string weaponType)
        {
            // Use existing static factory methods
            WeaponData weapon = weaponType.ToLower() switch
            {
                "sword" => WeaponData.CreateSwordData(),
                "spear" => WeaponData.CreateSpearData(),
                "dagger" => WeaponData.CreateDaggerData(),
                "mace" => WeaponData.CreateMaceData(),
                _ => WeaponData.CreateSwordData()
            };

            // Save as asset
            string path = $"Assets/Data/Weapons/{weaponType}.asset";
            System.IO.Directory.CreateDirectory("Assets/Data/Weapons");
            AssetDatabase.CreateAsset(weapon, path);
            AssetDatabase.SaveAssets();

            return weapon;
        }

        private static GameObject CreateCharacter(string name, Vector3 position, CharacterStats stats, WeaponData weapon, bool isEnemy)
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
            var debugVisualizer = character.AddComponent<CombatDebugVisualizer>();

            // Add AI for enemy - Use KnightAI for pattern-based behavior
            if (isEnemy)
            {
                var ai = character.AddComponent<KnightAI>();
                // Configure AI settings
                var aiSO = new SerializedObject(ai);
                var enablePatternLogsProp = aiSO.FindProperty("enablePatternLogs");
                var enableVisualizationProp = aiSO.FindProperty("enablePatternVisualization");
                if (enablePatternLogsProp != null)
                {
                    enablePatternLogsProp.boolValue = true;
                }
                if (enableVisualizationProp != null)
                {
                    enableVisualizationProp.boolValue = true;
                }
                aiSO.ApplyModifiedProperties();

                Debug.Log($"✅ {character.name} equipped with KnightAI (8-second defensive pattern cycle)");
            }

            // Configure components using SerializedObject for private fields
            ConfigureComponent(combatController, "characterStats", stats);
            ConfigureComponent(healthSystem, "characterStats", stats);
            ConfigureComponent(staminaSystem, "characterStats", stats);
            ConfigureComponent(skillSystem, "characterStats", stats);
            ConfigureComponent(movementController, "characterStats", stats);
            ConfigureComponent(weaponController, "weaponData", weapon);

            // Configure control types (Player = keyboard, Enemy = AI)
            ConfigureComponent(movementController, "isPlayerControlled", !isEnemy);
            ConfigureComponent(skillSystem, "isPlayerControlled", !isEnemy);

            // Enable debug logs but disable visual GUI overlays
            ConfigureComponent(combatController, "enableDebugLogs", false);
            ConfigureComponent(healthSystem, "enableDebugLogs", false);
            ConfigureComponent(staminaSystem, "enableDebugLogs", false);
            ConfigureComponent(skillSystem, "enableDebugLogs", false);
            ConfigureComponent(skillSystem, "showSkillGUI", false);
            ConfigureComponent(movementController, "enableDebugLogs", false);

            // Add visual representation (simple capsule)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(character.transform);
            visual.transform.localPosition = new Vector3(0, 1, 0);
            visual.name = "Visual";

            // Color code: Player = Blue, Enemy = Red
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = isEnemy ? Color.red : Color.blue;
                renderer.material = material;
            }

            // Remove collider from visual (CharacterController handles collision)
            if (visual.GetComponent<CapsuleCollider>() != null)
                DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            return character;
        }

        private static void ConfigureComponent(Component component, string propertyName, object value)
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
                else if (value is UnityEngine.Object objVal)
                    prop.objectReferenceValue = objVal;

                so.ApplyModifiedProperties();
            }
        }

        private static void SetupCombatTargeting(GameObject player, GameObject enemy)
        {
            // Get combat controllers
            var playerCombat = player.GetComponent<CombatController>();
            var enemyCombat = enemy.GetComponent<CombatController>();

            if (playerCombat != null && enemyCombat != null)
            {
                // Set them as potential targets for each other
                // The targeting system will handle this dynamically during gameplay
                Debug.Log($"Combat targeting configured between {player.name} and {enemy.name}");
            }
        }
    }
}