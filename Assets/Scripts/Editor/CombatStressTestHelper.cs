using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Editor
{
    public class CombatStressTestHelper : EditorWindow
    {
        [MenuItem("FairyGate/Combat Stress Test Helper")]
        public static void ShowWindow()
        {
            GetWindow<CombatStressTestHelper>("Combat Stress Tests");
        }

        private Vector2 scrollPosition;
        private Transform playerTransform;
        private GameObject testEnemyPrefab;

        // Test configuration
        private int multiEnemyCount = 3;
        private float spawnRadius = 5f;
        private bool autoStartCombat = true;
        private bool showDebugGizmos = true;

        private void OnEnable()
        {
            FindPlayer();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Combat Stress Test Helper", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Configuration Section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            playerTransform = EditorGUILayout.ObjectField("Player Transform", playerTransform, typeof(Transform), true) as Transform;
            testEnemyPrefab = EditorGUILayout.ObjectField("Enemy Prefab", testEnemyPrefab, typeof(GameObject), false) as GameObject;

            EditorGUILayout.Space();
            multiEnemyCount = EditorGUILayout.IntSlider("Multi-Enemy Count", multiEnemyCount, 2, 10);
            spawnRadius = EditorGUILayout.Slider("Spawn Radius", spawnRadius, 2f, 15f);
            autoStartCombat = EditorGUILayout.Toggle("Auto Start Combat", autoStartCombat);
            showDebugGizmos = EditorGUILayout.Toggle("Show Debug Gizmos", showDebugGizmos);

            EditorGUILayout.Space();

            if (GUILayout.Button("Find Player in Scene"))
            {
                FindPlayer();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            // Quick cleanup
            if (GUILayout.Button("Clear All Test Enemies", GUILayout.Height(30)))
            {
                ClearAllTestEnemies();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stress Test Scenarios", EditorStyles.boldLabel);

            // Test 1: Multiple Enemies
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 1: Multiple Enemies", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns multiple enemies with mixed AI types to test concurrent combat.", MessageType.Info);
            if (GUILayout.Button("Spawn Mixed AI Enemies", GUILayout.Height(25)))
            {
                SpawnMixedEnemies(multiEnemyCount);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Test 2: Identical AI Pattern
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 2: Identical AI Patterns", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns enemies with same AI type to test pattern synchronization.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Knights", GUILayout.Height(25)))
            {
                SpawnSpecificAI(multiEnemyCount, "KnightAI");
            }
            if (GUILayout.Button("Spawn Repeaters", GUILayout.Height(25)))
            {
                SpawnSpecificAI(multiEnemyCount, "TestRepeaterAI");
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Spawn Simple AI", GUILayout.Height(25)))
            {
                SpawnSpecificAI(multiEnemyCount, "SimpleTestAI");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Test 3: Range Boundary Testing
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 3: Range Boundary", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns enemy at exact weapon range boundary for edge case testing.", MessageType.Info);
            if (GUILayout.Button("Spawn at Weapon Range Edge", GUILayout.Height(25)))
            {
                SpawnAtRangeBoundary();
            }
            if (GUILayout.Button("Spawn Just Inside Range", GUILayout.Height(25)))
            {
                SpawnAtRangeBoundary(0.95f);
            }
            if (GUILayout.Button("Spawn Just Outside Range", GUILayout.Height(25)))
            {
                SpawnAtRangeBoundary(1.05f);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Test 4: Circle Formation
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 4: Surrounded Formation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns enemies in a circle around the player.", MessageType.Info);
            if (GUILayout.Button("Spawn Circle Formation", GUILayout.Height(25)))
            {
                SpawnCircleFormation(multiEnemyCount);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Test 5: Linear Formation
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 5: Linear Formation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns enemies in a line facing the player.", MessageType.Info);
            if (GUILayout.Button("Spawn Line Formation", GUILayout.Height(25)))
            {
                SpawnLineFormation(multiEnemyCount);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Test 6: Close Quarters
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 6: Close Quarters Combat", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns enemies very close to test collision and movement.", MessageType.Info);
            if (GUILayout.Button("Spawn Close Quarters", GUILayout.Height(25)))
            {
                SpawnCloseQuarters(multiEnemyCount);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Test 7: Performance Test
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Test 7: Performance Stress Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Spawns many enemies to test performance limits.", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn 5 Enemies", GUILayout.Height(25)))
            {
                SpawnMixedEnemies(5);
            }
            if (GUILayout.Button("Spawn 10 Enemies", GUILayout.Height(25)))
            {
                SpawnMixedEnemies(10);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Monitoring Section
            EditorGUILayout.LabelField("Monitoring", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (Application.isPlaying)
            {
                int enemyCount = 0;
                try
                {
                    enemyCount = GameObject.FindGameObjectsWithTag("Enemy")?.Length ?? 0;
                }
                catch (UnityException)
                {
                    // Enemy tag doesn't exist, count by AI components
                    enemyCount = FindObjectsOfType<SimpleTestAI>().Length +
                                FindObjectsOfType<KnightAI>().Length +
                                FindObjectsOfType<TestRepeaterAI>().Length;
                }
                EditorGUILayout.LabelField($"Active Enemies: {enemyCount}");

                var combatManager = FindObjectOfType<CombatInteractionManager>();
                if (combatManager != null)
                {
                    EditorGUILayout.LabelField("Combat Manager: Active");
                }
                else
                {
                    EditorGUILayout.HelpBox("CombatInteractionManager not found in scene!", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void FindPlayer()
        {
            if (!Application.isPlaying) return;

            // Try to find by tag first
            try
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    Debug.Log($"[Stress Test] Found player by tag: {player.name}");
                    return;
                }
            }
            catch (UnityException)
            {
                // Player tag doesn't exist, try component search
            }

            // Fallback: Search for object with CombatController that has "Player" in name
            var allCombatants = FindObjectsOfType<CombatController>();
            foreach (var combatant in allCombatants)
            {
                if (combatant.name.Contains("Player"))
                {
                    playerTransform = combatant.transform;
                    Debug.Log($"[Stress Test] Found player by component search: {combatant.name}");
                    return;
                }
            }

            Debug.LogWarning("[Stress Test] No player found. Please assign Player Transform manually or ensure a GameObject with 'Player' in its name has a CombatController component.");
        }

        private void ClearAllTestEnemies()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Must be in Play Mode to clear enemies", "OK");
                return;
            }

            int count = 0;

            // Try to find enemies by tag
            try
            {
                var enemies = GameObject.FindGameObjectsWithTag("Enemy");
                count = enemies.Length;

                foreach (var enemy in enemies)
                {
                    DestroyImmediate(enemy);
                }
            }
            catch (UnityException)
            {
                // Enemy tag doesn't exist, search by name pattern
                var allObjects = FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.StartsWith("TestEnemy_") || obj.name.StartsWith("BoundaryTest_") ||
                        obj.name.StartsWith("Circle_") || obj.name.StartsWith("Line_") || obj.name.StartsWith("CloseQuarters_"))
                    {
                        DestroyImmediate(obj);
                        count++;
                    }
                }
            }

            Debug.Log($"[Stress Test] Cleared {count} test enemies");
        }

        private void SpawnMixedEnemies(int count)
        {
            if (!ValidateSetup()) return;

            string[] aiTypes = { "SimpleTestAI", "KnightAI", "TestRepeaterAI" };

            for (int i = 0; i < count; i++)
            {
                string aiType = aiTypes[i % aiTypes.Length];
                Vector3 position = GetRandomSpawnPosition();
                SpawnEnemy(position, aiType, $"TestEnemy_{aiType}_{i}");
            }

            Debug.Log($"[Stress Test] Spawned {count} mixed AI enemies");
        }

        private void SpawnSpecificAI(int count, string aiType)
        {
            if (!ValidateSetup()) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetRandomSpawnPosition();
                SpawnEnemy(position, aiType, $"TestEnemy_{aiType}_{i}");
            }

            Debug.Log($"[Stress Test] Spawned {count} {aiType} enemies");
        }

        private void SpawnAtRangeBoundary(float rangeMultiplier = 1.0f)
        {
            if (!ValidateSetup()) return;

            // Get weapon range from player
            var playerWeapon = playerTransform.GetComponent<WeaponController>();
            float weaponRange = playerWeapon != null ? playerWeapon.WeaponData.range : 1.5f;

            float distance = weaponRange * rangeMultiplier;
            Vector3 direction = playerTransform.forward;
            Vector3 position = playerTransform.position + direction * distance;

            SpawnEnemy(position, "SimpleTestAI", $"BoundaryTest_{rangeMultiplier:F2}x");

            Debug.Log($"[Stress Test] Spawned enemy at {rangeMultiplier:F2}x weapon range ({distance:F2}m)");
        }

        private void SpawnCircleFormation(int count)
        {
            if (!ValidateSetup()) return;

            string[] aiTypes = { "SimpleTestAI", "KnightAI", "TestRepeaterAI" };
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
                Vector3 position = playerTransform.position + offset;

                string aiType = aiTypes[i % aiTypes.Length];
                SpawnEnemy(position, aiType, $"Circle_{aiType}_{i}");
            }

            Debug.Log($"[Stress Test] Spawned {count} enemies in circle formation");
        }

        private void SpawnLineFormation(int count)
        {
            if (!ValidateSetup()) return;

            Vector3 lineDirection = playerTransform.right;
            float spacing = 2f;
            float startOffset = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = playerTransform.position
                    + playerTransform.forward * spawnRadius
                    + lineDirection * (startOffset + i * spacing);

                SpawnEnemy(position, "SimpleTestAI", $"Line_{i}");
            }

            Debug.Log($"[Stress Test] Spawned {count} enemies in line formation");
        }

        private void SpawnCloseQuarters(int count)
        {
            if (!ValidateSetup()) return;

            float closeRadius = 1.5f;

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * closeRadius;
                Vector3 position = playerTransform.position + offset;

                SpawnEnemy(position, "SimpleTestAI", $"CloseQuarters_{i}");
            }

            Debug.Log($"[Stress Test] Spawned {count} enemies in close quarters");
        }

        private void SpawnEnemy(Vector3 position, string aiType, string name)
        {
            // Create enemy GameObject
            GameObject enemy = new GameObject(name);
            enemy.transform.position = position;

            // Try to set Enemy tag, but don't fail if it doesn't exist
            try
            {
                enemy.tag = "Enemy";
            }
            catch (UnityException)
            {
                // Enemy tag doesn't exist - that's okay, we'll use component search instead
                Debug.LogWarning("[Stress Test] Enemy tag doesn't exist. Consider adding it via Edit → Project Settings → Tags and Layers for easier tracking.");
            }

            // Add required components based on the scene setup pattern
            var combatController = enemy.AddComponent<CombatController>();
            var healthSystem = enemy.AddComponent<HealthSystem>();
            var staminaSystem = enemy.AddComponent<StaminaSystem>();
            var statusEffectManager = enemy.AddComponent<StatusEffectManager>();
            var skillSystem = enemy.AddComponent<SkillSystem>();
            var movementController = enemy.AddComponent<MovementController>();
            var knockdownMeter = enemy.AddComponent<KnockdownMeterTracker>();
            var weaponController = enemy.AddComponent<WeaponController>();
            var accuracySystem = enemy.AddComponent<AccuracySystem>();

            // Add AI component
            switch (aiType)
            {
                case "SimpleTestAI":
                    enemy.AddComponent<SimpleTestAI>();
                    break;
                case "KnightAI":
                    enemy.AddComponent<KnightAI>();
                    break;
                case "TestRepeaterAI":
                    enemy.AddComponent<TestRepeaterAI>();
                    break;
            }

            // Add CharacterController
            var charController = enemy.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1, 0);
            charController.radius = 0.5f;
            charController.height = 2f;

            // Add visual representation
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            DestroyImmediate(capsule.GetComponent<Collider>()); // Remove collider, CharacterController handles collision
            capsule.transform.SetParent(enemy.transform);
            capsule.transform.localPosition = new Vector3(0, 1, 0);
            capsule.GetComponent<Renderer>().material.color = GetColorForAI(aiType);

            // Load and assign stats/weapon from Resources
            var stats = Resources.Load<CharacterStats>("Combat/Stats/EnemyStats");
            var weapon = Resources.Load<WeaponData>("Combat/Weapons/Sword");

            if (stats != null)
            {
                // Use SerializedObject for proper assignment
                SetComponentField(combatController, "baseStats", stats);
                SetComponentField(healthSystem, "characterStats", stats);
                SetComponentField(staminaSystem, "characterStats", stats);
                SetComponentField(statusEffectManager, "characterStats", stats);
                SetComponentField(skillSystem, "characterStats", stats);
                SetComponentField(movementController, "characterStats", stats);
                SetComponentField(knockdownMeter, "characterStats", stats);
                SetComponentField(accuracySystem, "characterStats", stats);
            }

            if (weapon != null)
            {
                SetComponentField(weaponController, "weaponData", weapon);
            }

            // CRITICAL: Set AI movement mode (not player-controlled)
            SetComponentField(movementController, "isPlayerControlled", false);

            Debug.Log($"[Stress Test] Spawned {name} at {position}");
        }

        private void SetComponentField(Component component, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(fieldName);

            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[Stress Test] Field '{fieldName}' not found on {component.GetType().Name}");
            }
        }

        private void SetComponentField(Component component, string fieldName, bool value)
        {
            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(fieldName);

            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[Stress Test] Field '{fieldName}' not found on {component.GetType().Name}");
            }
        }

        private Color GetColorForAI(string aiType)
        {
            return aiType switch
            {
                "SimpleTestAI" => new Color(1f, 0.5f, 0.5f), // Light red
                "KnightAI" => new Color(0.5f, 0.5f, 1f),      // Light blue
                "TestRepeaterAI" => new Color(0.5f, 1f, 0.5f), // Light green
                _ => Color.gray
            };
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
            return playerTransform.position + offset;
        }

        private bool ValidateSetup()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Must be in Play Mode to spawn enemies", "OK");
                return false;
            }

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null)
                {
                    EditorUtility.DisplayDialog("Error", "No player found in scene. Please assign Player Transform.", "OK");
                    return false;
                }
            }

            return true;
        }
    }
}
