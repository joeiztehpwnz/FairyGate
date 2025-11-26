using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Singleton service that provides efficient access to the player GameObject and its components.
    /// Caches references to avoid repeated FindObject calls across the codebase.
    /// </summary>
    public class PlayerFinder : AutoCreateSingleton<PlayerFinder>
    {
        private GameObject playerObject;
        private CombatController combatController;
        private Transform playerTransform;
        private HealthSystem healthSystem;
        private SkillSystem skillSystem;
        private StaminaSystem staminaSystem;

        protected override bool PersistAcrossScenes => true;

        /// <summary>
        /// Gets the player GameObject, finding it if necessary.
        /// </summary>
        public static GameObject Player
        {
            get { return Instance.GetPlayer(); }
        }

        /// <summary>
        /// Gets the player's Transform component.
        /// </summary>
        public static Transform PlayerTransform
        {
            get { return Instance.GetPlayerTransform(); }
        }

        /// <summary>
        /// Gets the player's CombatController component.
        /// </summary>
        public static CombatController PlayerCombatController
        {
            get { return Instance.GetPlayerCombatController(); }
        }

        /// <summary>
        /// Gets the player's HealthSystem component.
        /// </summary>
        public static HealthSystem PlayerHealthSystem
        {
            get { return Instance.GetPlayerHealthSystem(); }
        }

        /// <summary>
        /// Gets the player's SkillSystem component.
        /// </summary>
        public static SkillSystem PlayerSkillSystem
        {
            get { return Instance.GetPlayerSkillSystem(); }
        }

        /// <summary>
        /// Gets the player's StaminaSystem component.
        /// </summary>
        public static StaminaSystem PlayerStaminaSystem
        {
            get { return Instance.GetPlayerStaminaSystem(); }
        }

        /// <summary>
        /// Checks if the player exists and is alive.
        /// </summary>
        public static bool IsPlayerAlive
        {
            get
            {
                HealthSystem health = PlayerHealthSystem;
                return health != null && health.IsAlive;
            }
        }


        /// <summary>
        /// Clears cached references. Call when changing scenes.
        /// </summary>
        public static void ClearCache()
        {
            if (instance != null)
            {
                instance.playerObject = null;
                instance.combatController = null;
                instance.playerTransform = null;
                instance.healthSystem = null;
                instance.skillSystem = null;
                instance.staminaSystem = null;
            }
        }

        /// <summary>
        /// Forces a refresh of the player reference.
        /// </summary>
        public static void RefreshPlayer()
        {
            ClearCache();
            Instance.FindPlayer();
        }

        private GameObject GetPlayer()
        {
            if (playerObject == null)
            {
                FindPlayer();
            }
            return playerObject;
        }

        private Transform GetPlayerTransform()
        {
            if (playerTransform == null && GetPlayer() != null)
            {
                playerTransform = playerObject.transform;
            }
            return playerTransform;
        }

        private CombatController GetPlayerCombatController()
        {
            if (combatController == null && GetPlayer() != null)
            {
                combatController = playerObject.GetComponent<CombatController>();
            }
            return combatController;
        }

        private HealthSystem GetPlayerHealthSystem()
        {
            if (healthSystem == null && GetPlayer() != null)
            {
                healthSystem = playerObject.GetComponent<HealthSystem>();
            }
            return healthSystem;
        }

        private SkillSystem GetPlayerSkillSystem()
        {
            if (skillSystem == null && GetPlayer() != null)
            {
                skillSystem = playerObject.GetComponent<SkillSystem>();
            }
            return skillSystem;
        }

        private StaminaSystem GetPlayerStaminaSystem()
        {
            if (staminaSystem == null && GetPlayer() != null)
            {
                staminaSystem = playerObject.GetComponent<StaminaSystem>();
            }
            return staminaSystem;
        }

        private void FindPlayer()
        {
            // First try tag-based search (fastest)
            playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject == null)
            {
                // Fallback to name-based search
                var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
                foreach (var combatant in combatants)
                {
                    if (combatant.name.Contains("Player"))
                    {
                        playerObject = combatant.gameObject;
                        break;
                    }
                }
            }

            // Clear component caches if player changed
            if (playerObject != null)
            {
                playerTransform = null;
                combatController = null;
                healthSystem = null;
                skillSystem = null;
                staminaSystem = null;
            }
        }

        /// <summary>
        /// Called when a scene is loaded to refresh player references.
        /// </summary>
        private void OnLevelWasLoaded(int level)
        {
            ClearCache();
        }
    }
}