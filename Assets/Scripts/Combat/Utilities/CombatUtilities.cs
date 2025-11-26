using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Common utility methods for combat systems.
    /// Eliminates code duplication across multiple combat classes.
    /// </summary>
    public static class CombatUtilities
    {
        /// <summary>
        /// Determines if a GameObject is an AI enemy.
        /// Replaces duplicated enemy detection pattern across multiple UI components.
        /// </summary>
        public static bool IsEnemy(GameObject gameObject)
        {
            if (gameObject == null) return false;

            // Check for AI components that indicate an enemy
            return gameObject.GetComponent<PatternExecutor>() != null ||
                   gameObject.GetComponent<IAIAgent>() != null;
        }

        /// <summary>
        /// Determines if a Component belongs to an AI enemy.
        /// </summary>
        public static bool IsEnemy(Component component)
        {
            return component != null && IsEnemy(component.gameObject);
        }

        /// <summary>
        /// Finds the player GameObject in the scene.
        /// Replaces duplicated player-finding logic.
        /// </summary>
        public static GameObject FindPlayer()
        {
            // First try tag-based search (fastest)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) return player;

            // Fallback to name-based search
            var combatants = Object.FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var combatant in combatants)
            {
                if (combatant.name.Contains("Player"))
                {
                    return combatant.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the player's CombatController component.
        /// </summary>
        public static CombatController FindPlayerCombatController()
        {
            GameObject player = FindPlayer();
            return player != null ? player.GetComponent<CombatController>() : null;
        }

        /// <summary>
        /// Finds all enemy GameObjects in the scene.
        /// </summary>
        public static List<GameObject> FindAllEnemies()
        {
            var enemies = new List<GameObject>();

            // Find all PatternExecutors (new AI system)
            var patternExecutors = Object.FindObjectsByType<PatternExecutor>(FindObjectsSortMode.None);
            foreach (var executor in patternExecutors)
            {
                if (executor != null && executor.gameObject != null)
                {
                    enemies.Add(executor.gameObject);
                }
            }

            // Find any other IAIAgent implementations
            var aiAgents = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb is IAIAgent && !(mb is PatternExecutor))
                .Select(mb => mb.gameObject)
                .Where(go => go != null);

            enemies.AddRange(aiAgents);

            return enemies.Distinct().ToList();
        }

        /// <summary>
        /// Gets a safe name for logging, handling null references.
        /// </summary>
        public static string GetSafeName(GameObject gameObject)
        {
            return gameObject != null ? gameObject.name : "null";
        }

        /// <summary>
        /// Gets a safe name for logging, handling null references.
        /// </summary>
        public static string GetSafeName(Component component)
        {
            return component != null ? component.name : "null";
        }

        /// <summary>
        /// Validates if a transform is within range of another transform.
        /// </summary>
        public static bool IsWithinRange(Transform from, Transform to, float range)
        {
            if (from == null || to == null) return false;
            return Vector3.Distance(from.position, to.position) <= range;
        }

        /// <summary>
        /// Gets the horizontal distance between two transforms (ignoring Y axis).
        /// </summary>
        public static float GetHorizontalDistance(Transform from, Transform to)
        {
            if (from == null || to == null) return float.MaxValue;

            Vector3 fromPos = from.position;
            Vector3 toPos = to.position;
            fromPos.y = 0;
            toPos.y = 0;

            return Vector3.Distance(fromPos, toPos);
        }

        /// <summary>
        /// Gets the direction from one transform to another (normalized, ignoring Y).
        /// </summary>
        public static Vector3 GetHorizontalDirection(Transform from, Transform to)
        {
            if (from == null || to == null) return Vector3.zero;

            Vector3 direction = to.position - from.position;
            direction.y = 0;
            return direction.normalized;
        }

        /// <summary>
        /// Checks if a GameObject is alive (has health and health > 0).
        /// </summary>
        public static bool IsAlive(GameObject gameObject)
        {
            if (gameObject == null) return false;

            HealthSystem healthSystem = gameObject.GetComponent<HealthSystem>();
            return healthSystem != null && healthSystem.IsAlive;
        }

        /// <summary>
        /// Checks if a Component's GameObject is alive.
        /// </summary>
        public static bool IsAlive(Component component)
        {
            return component != null && IsAlive(component.gameObject);
        }
    }
}