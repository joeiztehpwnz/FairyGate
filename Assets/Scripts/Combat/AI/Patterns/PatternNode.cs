using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Represents a single node in an AI behavior pattern.
    /// Each node defines a skill to use and conditions for when to transition to other nodes.
    /// </summary>
    [System.Serializable]
    public class PatternNode
    {
        [Header("Node Identity")]
        [Tooltip("Unique name for this node (used for transitions)")]
        public string nodeName = "Unnamed Node";

        [Tooltip("Designer notes explaining this node's purpose")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("Skill Selection")]
        [Tooltip("Which skill should the AI use when this node is active")]
        public SkillType skillToUse = SkillType.Attack;

        [Tooltip("Does this skill require charging before execution?")]
        public bool requiresCharge = true;

        [Header("Movement Behavior")]
        [Tooltip("How should the AI move while in this node?")]
        public MovementBehaviorType movementBehavior = MovementBehaviorType.MaintainCustomRange;

        [Tooltip("Custom target range in meters (used by MaintainCustomRange behavior)")]
        public float customTargetRange = 2.0f;

        [Tooltip("How close to target range is acceptable (meters)")]
        public float rangeTolerance = 0.3f;

        [Tooltip("Movement speed multiplier while in this node (1.0 = normal)")]
        [Range(0f, 2f)]
        public float movementSpeedMultiplier = 1.0f;

        [Tooltip("Freeze all movement while in this node?")]
        public bool freezeMovement = false;

        [Header("Execution Conditions")]
        [Tooltip("All conditions must be true for this node to execute (AND logic)")]
        public List<PatternCondition> conditions = new List<PatternCondition>();

        [Header("Transitions")]
        [Tooltip("Possible transitions to other nodes (evaluated by priority)")]
        public List<PatternTransition> transitions = new List<PatternTransition>();

        [Header("Telegraph (Optional)")]
        [Tooltip("Visual/audio warning before executing this skill")]
        public TelegraphData telegraph = null;

        [Header("Timeout Fallback")]
        [Tooltip("Node to transition to if stuck (no valid transitions after timeout). Leave empty to disable.")]
        public string fallbackNodeName = "";

        [Tooltip("Time in seconds before fallback transition occurs (0 = disabled)")]
        public float fallbackTimeout = 5f;

        /// <summary>
        /// Checks if all execution conditions are met.
        /// </summary>
        public bool CanExecute(PatternEvaluationContext context)
        {
            // Empty condition list = always can execute
            if (conditions == null || conditions.Count == 0)
                return true;

            // All conditions must be true (AND logic)
            foreach (var condition in conditions)
            {
                if (!condition.Evaluate(context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the highest priority transition whose conditions are met.
        /// Returns null if no valid transitions exist.
        /// </summary>
        public PatternTransition GetValidTransition(PatternEvaluationContext context)
        {
            if (transitions == null || transitions.Count == 0)
                return null;

            // Sort by priority (highest first)
            var sortedTransitions = new List<PatternTransition>(transitions);
            sortedTransitions.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Return first transition whose conditions are met
            foreach (var transition in sortedTransitions)
            {
                if (transition.EvaluateConditions(context))
                    return transition;
            }

            return null;
        }
    }

    /// <summary>
    /// Defines a transition from one pattern node to another.
    /// </summary>
    [System.Serializable]
    public class PatternTransition
    {
        [Tooltip("Name of the target node to transition to")]
        public string targetNodeName = "";

        [Tooltip("All conditions must be true for this transition to occur (AND logic)")]
        public List<PatternCondition> conditions = new List<PatternCondition>();

        [Tooltip("Higher priority transitions are evaluated first")]
        public int priority = 0;

        [Tooltip("Reset hit counters (hits taken/dealt) when this transition occurs?")]
        public bool resetHitCounters = false;

        [Tooltip("Start a cooldown timer when this transition occurs (0 = none)")]
        public int startCooldownID = 0;

        [Tooltip("Duration of cooldown in seconds (if startCooldownID > 0)")]
        public float cooldownDuration = 0f;

        /// <summary>
        /// Checks if all transition conditions are met.
        /// </summary>
        public bool EvaluateConditions(PatternEvaluationContext context)
        {
            // Empty condition list = always valid
            if (conditions == null || conditions.Count == 0)
                return true;

            // All conditions must be true (AND logic)
            foreach (var condition in conditions)
            {
                if (!condition.Evaluate(context))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Defines telegraph data for visual/audio warnings before skill execution.
    /// </summary>
    [System.Serializable]
    public class TelegraphData
    {
        [Header("Visual Telegraph")]
        [Tooltip("Type of visual telegraph to display")]
        public TelegraphVisual visualType = TelegraphVisual.None;

        [Tooltip("Color tint for visual effects (glow, particles, etc)")]
        public Color glowColor = Color.white;

        [Header("Audio Telegraph")]
        [Tooltip("Audio clip name to play from Resources folder")]
        public string audioClip = "";

        [Tooltip("Volume multiplier for audio playback (0-1)")]
        [Range(0f, 1f)]
        public float audioVolume = 1.0f;

        [Header("Timing")]
        [Tooltip("How long the telegraph lasts before skill execution (seconds)")]
        [Range(0.1f, 1.0f)]
        public float duration = 0.5f;

        [Tooltip("Start telegraph this many seconds BEFORE charging begins")]
        [Range(0f, 0.5f)]
        public float anticipation = 0.3f;
    }

    /// <summary>
    /// Types of visual telegraphs that can be displayed.
    /// </summary>
    public enum TelegraphVisual
    {
        None,               // No visual telegraph
        StanceShift,        // Subtle body position change
        WeaponRaise,        // Weapon moves to attack position
        ShieldRaise,        // Shield/defensive posture
        EyeGlow,            // Eyes glow with skill color
        GroundEffect,       // AoE indicator on ground
        Crouch,             // Lower body before Counter
        BackStep,           // Step back before Lunge/retreat
        ParticleEffect      // Generic particle effect burst
    }

    /// <summary>
    /// Types of movement behaviors the AI can perform while in a pattern node.
    /// </summary>
    public enum MovementBehaviorType
    {
        MaintainCustomRange,     // Maintain specific range (uses customTargetRange field)
        ApproachTarget,          // Move directly toward target aggressively
        RetreatFromTarget,       // Move directly away from target
        CircleStrafeLeft,        // Strafe counterclockwise around target
        CircleStrafeRight,       // Strafe clockwise around target
        HoldPosition,            // Stop all movement, maintain current position
        UseFormationSlot,        // Request and move to formation slot from AICoordinator
        FollowAtDistance         // Follow target at safe distance (good for ranged characters)
    }
}
