using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Responsible for spawning and configuring combat characters.
    /// Handles character creation, AI setup, and equipment management.
    /// </summary>
    public class CharacterSpawner
    {
        /// <summary>
        /// Creates a fully configured combat character with all necessary components.
        /// </summary>
        /// <param name="name">Name of the character</param>
        /// <param name="position">Spawn position in world space</param>
        /// <param name="stats">Character stats asset</param>
        /// <param name="weapon">Primary weapon data</param>
        /// <param name="isEnemy">Whether this is an enemy character</param>
        /// <returns>The created character GameObject</returns>
        public GameObject CreateCharacter(string name, Vector3 position, CharacterStats stats, WeaponData weapon, bool isEnemy)
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

            // Configure components using SerializedObject
            EditorUtilities.SetSerializedProperty(combatController, "baseStats", stats);
            EditorUtilities.SetSerializedProperty(combatController, "faction", isEnemy ? (int)Faction.Enemy : (int)Faction.Player);
            EditorUtilities.SetSerializedProperty(healthSystem, "characterStats", stats);
            EditorUtilities.SetSerializedProperty(staminaSystem, "characterStats", stats);
            EditorUtilities.SetSerializedProperty(statusEffectManager, "characterStats", stats);
            EditorUtilities.SetSerializedProperty(skillSystem, "characterStats", stats);
            EditorUtilities.SetSerializedProperty(movementController, "characterStats", stats);
            EditorUtilities.SetSerializedProperty(knockdownMeter, "characterStats", stats);
            EditorUtilities.SetSerializedProperty(weaponController, "primaryWeapon", weapon);
            EditorUtilities.SetSerializedProperty(accuracySystem, "characterStats", stats);

            // Configure control types
            EditorUtilities.SetSerializedProperty(movementController, "isPlayerControlled", !isEnemy);
            EditorUtilities.SetSerializedProperty(skillSystem, "isPlayerControlled", !isEnemy);

            // Enable debug logs for systems (keep minimal logging)
            EditorUtilities.SetSerializedProperty(combatController, "enableDebugLogs", false);
            EditorUtilities.SetSerializedProperty(healthSystem, "enableDebugLogs", false);
            EditorUtilities.SetSerializedProperty(staminaSystem, "enableDebugLogs", false);
            EditorUtilities.SetSerializedProperty(skillSystem, "enableDebugLogs", true); // Enable for state machine visibility
            EditorUtilities.SetSerializedProperty(skillSystem, "showSkillGUI", false);
            EditorUtilities.SetSerializedProperty(movementController, "enableDebugLogs", false);

            // Enable state machine for testing
            EditorUtilities.SetSerializedProperty(skillSystem, "useStateMachine", true);

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
                Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // Set tag for player (needed for target indicator system)
            if (!isEnemy)
            {
                character.tag = "Player";
            }

            Debug.Log($"✅ Created {name} with {weapon.weaponName}");
            return character;
        }

        /// <summary>
        /// Spawns an enemy character with archetype-specific configuration.
        /// </summary>
        /// <param name="archetype">Enemy archetype defining stats and behavior</param>
        /// <param name="spawnPosition">Position to spawn the enemy</param>
        /// <param name="enemyNumber">Unique identifier for this enemy</param>
        /// <param name="assetFactory">Factory for creating weapon assets</param>
        /// <returns>The spawned enemy GameObject</returns>
        public GameObject SpawnEnemyWithArchetype(EnemyArchetype archetype, Vector3 spawnPosition,
            int enemyNumber, CombatAssetFactory assetFactory)
        {
            // Get archetype configuration
            var config = EnemyArchetypeConfig.GetArchetypeData(archetype);

            // Create weapons from archetype configuration
            var primaryWeapon = assetFactory.CreateOrLoadWeaponData($"Test{config.primaryWeapon}", config.primaryWeapon);
            var secondaryWeapon = assetFactory.CreateOrLoadWeaponData($"Test{config.secondaryWeapon}", config.secondaryWeapon);

            // Create enemy character with primary weapon
            string enemyName = $"{archetype}_{enemyNumber}";
            var enemy = CreateCharacter(enemyName, spawnPosition, config.stats, primaryWeapon, true);

            // Set faction using proper Editor serialization to persist
            var combatController = enemy.GetComponent<CombatController>();
            EditorUtilities.SetSerializedProperty(combatController, "faction", (int)Faction.Enemy);

            // Configure secondary weapon slot
            var weaponController = enemy.GetComponent<WeaponController>();
            if (weaponController != null)
            {
                EditorUtilities.SetSerializedProperty(weaponController, "secondaryWeapon", secondaryWeapon);
            }

            // Setup AI Pattern System
            AddPatternSystemToEnemy(enemy, archetype, config);

            // Add equipment manager
            AddEquipmentManager(enemy);

            // Add CharacterInfoDisplay for UI
            enemy.AddComponent<CharacterInfoDisplay>();

            Debug.Log($"✅ Spawned {archetype} enemy at {spawnPosition}");
            Debug.Log($"   Stats: STR={config.stats.strength} DEX={config.stats.dexterity} VIT={config.stats.vitality} " +
                      $"DEF={config.stats.physicalDefense} FOCUS={config.stats.focus}");
            Debug.Log($"   AI: Pattern-based AI with {archetype} pattern");

            return enemy;
        }

        /// <summary>
        /// Adds pattern-based AI system to an enemy character.
        /// </summary>
        /// <param name="enemy">The enemy GameObject to configure</param>
        /// <param name="archetype">Enemy archetype for pattern selection</param>
        /// <param name="config">Archetype configuration data</param>
        public void AddPatternSystemToEnemy(GameObject enemy, EnemyArchetype archetype,
            EnemyArchetypeConfig.ArchetypeData config)
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

            // Add PatternExecutor component (PRIMARY AI SYSTEM)
            var patternExecutor = enemy.AddComponent<PatternExecutor>();
            EditorUtilities.SetSerializedProperty(patternExecutor, "patternDefinition", patternAsset);
            EditorUtilities.SetSerializedProperty(patternExecutor, "enableDebugLogs", true);

            // Set archetype-specific engage distance
            // Archer needs larger engage distance to stay in combat at kiting range (6.0m)
            float engageDistance = archetype == EnemyArchetype.Archer ? 8.0f : 3.0f;
            EditorUtilities.SetSerializedProperty(patternExecutor, "engageDistance", engageDistance);

            EditorUtilities.SetSerializedProperty(patternExecutor, "useCoordination", true);

            // Add TelegraphSystem component
            var telegraphSystem = enemy.AddComponent<TelegraphSystem>();
            EditorUtilities.SetSerializedProperty(telegraphSystem, "enableDebugLogs", false);

            Debug.Log($"   AI System: ✅ PatternExecutor with {patternName}");
        }

        /// <summary>
        /// Adds equipment manager to a character and loads available equipment sets.
        /// </summary>
        /// <param name="character">The character GameObject to add equipment manager to</param>
        public void AddEquipmentManager(GameObject character)
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
                EditorUtilities.SetSerializedProperty(equipmentManager, "availableSets", equipmentSetsArray);

                Debug.Log($"✅ Loaded {equipmentSetsArray.Length} equipment sets for {character.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ No equipment sets found in Assets/Data/Equipment/Sets/");
            }

            EditorUtilities.SetSerializedProperty(equipmentManager, "enableDebugLogs", true);
            Debug.Log($"✅ Added EquipmentManager to {character.name}");
        }
    }
}
