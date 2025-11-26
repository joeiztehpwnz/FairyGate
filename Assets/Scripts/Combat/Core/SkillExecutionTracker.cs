using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Tracks skill executions from queue to completion to ensure proper coordination.
    /// Prevents race conditions between skill state changes and actual execution processing.
    /// </summary>
    public class SkillExecutionTracker : MonoBehaviour
    {
        private static SkillExecutionTracker instance;
        public static SkillExecutionTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SkillExecutionTracker");
                    instance = go.AddComponent<SkillExecutionTracker>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>
        /// Tracks pending executions by combatant.
        /// Key: CombatController instance
        /// Value: Skill type being executed
        /// </summary>
        private readonly Dictionary<CombatController, SkillType> pendingExecutions = new Dictionary<CombatController, SkillType>();

        /// <summary>
        /// Tracks which combatants have attack slots that shouldn't be released yet.
        /// </summary>
        private readonly HashSet<CombatController> executionsInProgress = new HashSet<CombatController>();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        /// <summary>
        /// Called when a skill execution is queued in CombatInteractionManager.
        /// </summary>
        public void OnSkillQueued(CombatController combatant, SkillType skillType)
        {
            if (combatant == null) return;

            pendingExecutions[combatant] = skillType;
            executionsInProgress.Add(combatant);

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"[SkillExecutionTracker] Queued {skillType} for {combatant.name} - blocking slot release");
            }
        }

        /// <summary>
        /// Called when a skill execution begins processing in CombatInteractionManager.
        /// </summary>
        public void OnSkillProcessingStarted(CombatController combatant)
        {
            if (combatant == null) return;

            if (pendingExecutions.ContainsKey(combatant))
            {
                pendingExecutions.Remove(combatant);

                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"[SkillExecutionTracker] Started processing skill for {combatant.name}");
                }
            }
        }

        /// <summary>
        /// Called when a skill execution completes processing.
        /// </summary>
        public void OnSkillProcessingCompleted(CombatController combatant)
        {
            if (combatant == null) return;

            bool wasTracked = executionsInProgress.Remove(combatant);

            if (enableDebugLogs && wasTracked)
            {
                CombatLogger.LogCombat($"[SkillExecutionTracker] Completed processing for {combatant.name} - slot can now be released");
            }
        }

        /// <summary>
        /// Checks if a combatant has a skill execution in progress that should block slot release.
        /// </summary>
        public bool HasExecutionInProgress(CombatController combatant)
        {
            if (combatant == null) return false;
            return executionsInProgress.Contains(combatant);
        }

        /// <summary>
        /// Clears tracking for a combatant (called on death/cleanup).
        /// </summary>
        public void ClearCombatant(CombatController combatant)
        {
            if (combatant == null) return;

            pendingExecutions.Remove(combatant);
            executionsInProgress.Remove(combatant);

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"[SkillExecutionTracker] Cleared tracking for {combatant.name}");
            }
        }

        /// <summary>
        /// Gets debug information about current tracking state.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Pending: {pendingExecutions.Count}, In Progress: {executionsInProgress.Count}";
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}