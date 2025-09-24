using UnityEngine;

namespace FairyGate.Combat
{
    public static class CombatSetupHelper
    {
        /// <summary>
        /// Creates a fully configured combat character with all required components
        /// </summary>
        public static GameObject CreateCombatCharacter(string characterName, Vector3 position, bool isPlayer = true, WeaponType weaponType = WeaponType.Sword)
        {
            // Create base GameObject
            var character = new GameObject(characterName);
            character.transform.position = position;

            // Add CharacterController
            var characterController = character.AddComponent<CharacterController>();
            characterController.radius = 0.5f;
            characterController.height = 2f;
            characterController.center = new Vector3(0, 1f, 0);

            // Add visual representation (capsule)
            var visualCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visualCapsule.name = "Visual";
            visualCapsule.transform.SetParent(character.transform);
            visualCapsule.transform.localPosition = Vector3.zero;

            // Remove the capsule's collider since CharacterController handles collision
            Object.DestroyImmediate(visualCapsule.GetComponent<Collider>());

            // Set color based on character type
            var renderer = visualCapsule.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = isPlayer ? Color.blue : Color.red;
            renderer.material = material;

            // Add all combat components
            var healthSystem = character.AddComponent<HealthSystem>();
            var staminaSystem = character.AddComponent<StaminaSystem>();
            var statusEffectManager = character.AddComponent<StatusEffectManager>();
            var skillSystem = character.AddComponent<SkillSystem>();
            var weaponController = character.AddComponent<WeaponController>();
            var movementController = character.AddComponent<MovementController>();
            var combatController = character.AddComponent<CombatController>();
            var knockdownMeter = character.AddComponent<KnockdownMeterTracker>();
            var debugVisualizer = character.AddComponent<CombatDebugVisualizer>();

            // Create and assign character stats
            // Note: In Unity, these need to be assigned in the inspector or through public methods
            // For now, we'll create default stats - you'll need to assign them manually in the inspector
            var characterStats = CreateCharacterStats(isPlayer);
            Debug.Log($"Created character stats for {characterName}. Please assign the CharacterStats ScriptableObject in the inspector.");

            // Set up weapon
            SetupWeapon(weaponController, weaponType);

            // Add AI for enemy characters
            if (!isPlayer)
            {
                character.AddComponent<SimpleTestAI>();
            }

            // Configure layers (optional, for targeting)
            if (isPlayer)
            {
                character.layer = LayerMask.NameToLayer("Player");
            }
            else
            {
                character.layer = LayerMask.NameToLayer("Enemy");
            }

            Debug.Log($"Created combat character: {characterName} at {position} with weapon {weaponType}");
            return character;
        }

        private static CharacterStats CreateCharacterStats(bool isPlayer)
        {
            if (isPlayer)
            {
                return CharacterStats.CreateTestPlayerStats();
            }
            else
            {
                return CharacterStats.CreateTestEnemyStats();
            }
        }

        private static void SetupWeapon(WeaponController weaponController, WeaponType weaponType)
        {
            WeaponData weaponData = weaponType switch
            {
                WeaponType.Sword => WeaponData.CreateSwordData(),
                WeaponType.Spear => WeaponData.CreateSpearData(),
                WeaponType.Dagger => WeaponData.CreateDaggerData(),
                WeaponType.Mace => WeaponData.CreateMaceData(),
                _ => WeaponData.CreateSwordData()
            };

            weaponController.SetWeapon(weaponData);

            // Create simple visual weapon representation
            CreateWeaponVisual(weaponController.transform, weaponType);
        }

        private static void CreateWeaponVisual(Transform parent, WeaponType weaponType)
        {
            GameObject weaponVisual = weaponType switch
            {
                WeaponType.Sword => GameObject.CreatePrimitive(PrimitiveType.Cube),
                WeaponType.Spear => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                WeaponType.Dagger => GameObject.CreatePrimitive(PrimitiveType.Cube),
                WeaponType.Mace => GameObject.CreatePrimitive(PrimitiveType.Cube),
                _ => GameObject.CreatePrimitive(PrimitiveType.Cube)
            };

            weaponVisual.name = $"{weaponType}Visual";
            weaponVisual.transform.SetParent(parent);

            // Position and scale based on weapon type
            switch (weaponType)
            {
                case WeaponType.Sword:
                    weaponVisual.transform.localPosition = new Vector3(0.5f, 1f, 0f);
                    weaponVisual.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
                    break;

                case WeaponType.Spear:
                    weaponVisual.transform.localPosition = new Vector3(0.3f, 1f, 0f);
                    weaponVisual.transform.localScale = new Vector3(0.05f, 1.5f, 0.05f);
                    weaponVisual.transform.localRotation = Quaternion.Euler(0, 0, 45);
                    break;

                case WeaponType.Dagger:
                    weaponVisual.transform.localPosition = new Vector3(0.3f, 1f, 0f);
                    weaponVisual.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
                    break;

                case WeaponType.Mace:
                    weaponVisual.transform.localPosition = new Vector3(0.4f, 1f, 0f);
                    weaponVisual.transform.localScale = new Vector3(0.15f, 1f, 0.15f);
                    break;
            }

            // Remove collider from visual
            Object.DestroyImmediate(weaponVisual.GetComponent<Collider>());

            // Set weapon color
            var renderer = weaponVisual.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = weaponType switch
            {
                WeaponType.Sword => Color.gray,
                WeaponType.Spear => new Color(0.6f, 0.3f, 0f), // Brown
                WeaponType.Dagger => Color.white,
                WeaponType.Mace => Color.black,
                _ => Color.gray
            };
            renderer.material = material;
        }

        /// <summary>
        /// Creates a basic combat arena with ground and boundaries
        /// </summary>
        public static GameObject CreateCombatArena(Vector3 center = default, float size = 10f)
        {
            var arena = new GameObject("Combat Arena");
            arena.transform.position = center;

            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena Ground";
            ground.transform.SetParent(arena.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = Vector3.one * (size / 10f); // Plane is 10x10 by default

            // Set ground material
            var groundRenderer = ground.GetComponent<Renderer>();
            var groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = Color.gray;
            groundRenderer.material = groundMaterial;

            // Create invisible boundary walls
            CreateBoundaryWall(arena.transform, new Vector3(0, 1, size/2), new Vector3(size, 2, 0.1f)); // North
            CreateBoundaryWall(arena.transform, new Vector3(0, 1, -size/2), new Vector3(size, 2, 0.1f)); // South
            CreateBoundaryWall(arena.transform, new Vector3(size/2, 1, 0), new Vector3(0.1f, 2, size)); // East
            CreateBoundaryWall(arena.transform, new Vector3(-size/2, 1, 0), new Vector3(0.1f, 2, size)); // West

            Debug.Log($"Created combat arena at {center} with size {size}x{size}");
            return arena;
        }

        private static void CreateBoundaryWall(Transform parent, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Boundary Wall";
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;

            // Make invisible
            var renderer = wall.GetComponent<Renderer>();
            renderer.enabled = false;
        }

        /// <summary>
        /// Quick setup for a complete combat scene
        /// </summary>
        [ContextMenu("Setup Complete Combat Scene")]
        public static void SetupCompleteCombatScene()
        {
            // Clear existing setup (optional)
            var existingCharacters = GameObject.FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var character in existingCharacters)
            {
                Object.DestroyImmediate(character.gameObject);
            }

            // Create arena
            CreateCombatArena();

            // Create player
            var player = CreateCombatCharacter("Player", new Vector3(-3, 0, -3), true, WeaponType.Sword);

            // Create enemy
            var enemy = CreateCombatCharacter("Enemy", new Vector3(3, 0, 3), false, WeaponType.Mace);

            // Ensure managers exist
            if (FindObject<GameManager>() == null)
            {
                var gameManager = new GameObject("Game Manager");
                gameManager.AddComponent<GameManager>();
            }

            if (FindObject<CombatInteractionManager>() == null)
            {
                var combatManager = new GameObject("Combat Interaction Manager");
                combatManager.AddComponent<CombatInteractionManager>();
            }

            Debug.Log("Complete combat scene setup finished!");
        }

        private static T FindObject<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>();
        }
    }
}