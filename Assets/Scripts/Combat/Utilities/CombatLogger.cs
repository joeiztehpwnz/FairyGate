using UnityEngine;

#if UNITY_EDITOR
using System.Text;
#endif

namespace FairyGate.Combat
{
    /// <summary>
    /// Centralized logging system for combat-related debug messages.
    /// Provides categorized, color-coded logging with configurable verbosity levels.
    /// Zero performance cost in release builds through conditional compilation.
    /// </summary>
    public static class CombatLogger
    {
        /// <summary>
        /// Log categories for filtering and color-coding.
        /// </summary>
        public enum LogCategory
        {
            AI,          // AI behavior, patterns, decisions
            Combat,      // Combat interactions, damage, skills
            Skills,      // Skill execution, states, transitions
            Movement,    // Character movement, positioning
            Weapons,     // Weapon swapping, attacks
            Health,      // Health, healing, damage taken
            Stamina,     // Stamina costs, regeneration
            UI,          // UI updates, displays
            System,      // Managers, coordinators, systems
            Pattern,     // Pattern execution, transitions
            Formation,   // Formation slot management
            Attack       // Attack coordination, permissions
        }

        /// <summary>
        /// Log levels for controlling verbosity.
        /// </summary>
        public enum LogLevel
        {
            Debug,      // Verbose debugging information
            Info,       // General information
            Warning,    // Potential issues
            Error       // Critical errors
        }

        // Configuration flags (can be set via inspector or code)
        private static bool enableAI = true;
        private static bool enableCombat = true;
        private static bool enableSkills = true;
        private static bool enableMovement = false; // Usually too verbose
        private static bool enableWeapons = true;
        private static bool enableHealth = true;
        private static bool enableStamina = true;
        private static bool enableUI = false; // Usually too verbose
        private static bool enableSystem = true;
        private static bool enablePattern = true;
        private static bool enableFormation = true;
        private static bool enableAttack = true;

        private static LogLevel minimumLevel = LogLevel.Info;

        #region Public API

        /// <summary>
        /// Logs a message with the specified category and level.
        /// </summary>
        public static void Log(string message, LogCategory category, LogLevel level = LogLevel.Info, Object context = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!IsCategoryEnabled(category)) return;
            if (level < minimumLevel) return;

            string formattedMessage = FormatMessage(message, category, level);

            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, context);
                    break;
                default:
                    Debug.Log(formattedMessage, context);
                    break;
            }
#endif
        }

        /// <summary>
        /// Logs an AI-related message.
        /// </summary>
        public static void LogAI(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.AI, level, context);
        }

        /// <summary>
        /// Logs a combat-related message.
        /// </summary>
        public static void LogCombat(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Combat, level, context);
        }

        /// <summary>
        /// Logs a skill-related message.
        /// </summary>
        public static void LogSkill(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Skills, level, context);
        }

        /// <summary>
        /// Logs a movement-related message.
        /// </summary>
        public static void LogMovement(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Movement, level, context);
        }

        /// <summary>
        /// Logs a weapon-related message.
        /// </summary>
        public static void LogWeapon(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Weapons, level, context);
        }

        /// <summary>
        /// Logs a health-related message.
        /// </summary>
        public static void LogHealth(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Health, level, context);
        }

        /// <summary>
        /// Logs a stamina-related message.
        /// </summary>
        public static void LogStamina(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Stamina, level, context);
        }

        /// <summary>
        /// Logs a UI-related message.
        /// </summary>
        public static void LogUI(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.UI, level, context);
        }

        /// <summary>
        /// Logs a system-related message.
        /// </summary>
        public static void LogSystem(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.System, level, context);
        }

        /// <summary>
        /// Logs a pattern-related message.
        /// </summary>
        public static void LogPattern(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Pattern, level, context);
        }

        /// <summary>
        /// Logs a formation-related message.
        /// </summary>
        public static void LogFormation(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Formation, level, context);
        }

        /// <summary>
        /// Logs an attack coordination message.
        /// </summary>
        public static void LogAttack(string message, LogLevel level = LogLevel.Info, Object context = null)
        {
            Log(message, LogCategory.Attack, level, context);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets whether a specific category is enabled.
        /// </summary>
        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            switch (category)
            {
                case LogCategory.AI: enableAI = enabled; break;
                case LogCategory.Combat: enableCombat = enabled; break;
                case LogCategory.Skills: enableSkills = enabled; break;
                case LogCategory.Movement: enableMovement = enabled; break;
                case LogCategory.Weapons: enableWeapons = enabled; break;
                case LogCategory.Health: enableHealth = enabled; break;
                case LogCategory.Stamina: enableStamina = enabled; break;
                case LogCategory.UI: enableUI = enabled; break;
                case LogCategory.System: enableSystem = enabled; break;
                case LogCategory.Pattern: enablePattern = enabled; break;
                case LogCategory.Formation: enableFormation = enabled; break;
                case LogCategory.Attack: enableAttack = enabled; break;
            }
        }

        /// <summary>
        /// Sets the minimum log level that will be displayed.
        /// </summary>
        public static void SetMinimumLevel(LogLevel level)
        {
            minimumLevel = level;
        }

        /// <summary>
        /// Checks if a category is currently enabled.
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            return category switch
            {
                LogCategory.AI => enableAI,
                LogCategory.Combat => enableCombat,
                LogCategory.Skills => enableSkills,
                LogCategory.Movement => enableMovement,
                LogCategory.Weapons => enableWeapons,
                LogCategory.Health => enableHealth,
                LogCategory.Stamina => enableStamina,
                LogCategory.UI => enableUI,
                LogCategory.System => enableSystem,
                LogCategory.Pattern => enablePattern,
                LogCategory.Formation => enableFormation,
                LogCategory.Attack => enableAttack,
                _ => true
            };
        }

        /// <summary>
        /// Enables all log categories.
        /// </summary>
        public static void EnableAll()
        {
            enableAI = enableCombat = enableSkills = enableMovement = true;
            enableWeapons = enableHealth = enableStamina = enableUI = true;
            enableSystem = enablePattern = enableFormation = enableAttack = true;
        }

        /// <summary>
        /// Disables all log categories.
        /// </summary>
        public static void DisableAll()
        {
            enableAI = enableCombat = enableSkills = enableMovement = false;
            enableWeapons = enableHealth = enableStamina = enableUI = false;
            enableSystem = enablePattern = enableFormation = enableAttack = false;
        }

        #endregion

        #region Formatting

        private static string FormatMessage(string message, LogCategory category, LogLevel level)
        {
#if UNITY_EDITOR
            // Rich text color formatting for Unity console (editor only)
            string color = GetCategoryColorHex(category);
            string levelPrefix = GetLevelPrefix(level);
            return $"<color={color}>[{category}]</color> {levelPrefix}{message}";
#else
            // Simple formatting for builds
            return $"[{category}] {message}";
#endif
        }

        private static string GetCategoryColorHex(LogCategory category)
        {
            return category switch
            {
                LogCategory.AI => "#00FFFF",          // Cyan
                LogCategory.Combat => "#FF4444",      // Red
                LogCategory.Skills => "#FFD700",      // Gold
                LogCategory.Movement => "#90EE90",    // Light Green
                LogCategory.Weapons => "#FFA500",     // Orange
                LogCategory.Health => "#FF1493",      // Deep Pink
                LogCategory.Stamina => "#32CD32",     // Lime Green
                LogCategory.UI => "#87CEEB",          // Sky Blue
                LogCategory.System => "#DDA0DD",      // Plum
                LogCategory.Pattern => "#00CED1",     // Dark Turquoise
                LogCategory.Formation => "#FFB6C1",   // Light Pink
                LogCategory.Attack => "#FF6347",      // Tomato
                _ => "#FFFFFF"                        // White
            };
        }

        private static string GetLevelPrefix(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "[DEBUG] ",
                LogLevel.Warning => "[WARN] ",
                LogLevel.Error => "[ERROR] ",
                _ => ""
            };
        }

        #endregion
    }
}
