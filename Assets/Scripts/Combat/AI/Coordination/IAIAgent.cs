using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Common interface for all AI agents (SimpleTestAI, PatternExecutor, etc).
    /// Allows AICoordinator to work with any AI implementation.
    /// </summary>
    public interface IAIAgent
    {
        /// <summary>
        /// The GameObject this AI agent is attached to.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// The Transform component of this AI agent.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// The name of this AI agent (typically gameObject.name).
        /// </summary>
        string name { get; }

        /// <summary>
        /// Whether this AI agent is ready to attack.
        /// </summary>
        bool IsReadyToAttack { get; }

        /// <summary>
        /// Whether this AI agent is actively in combat.
        /// </summary>
        bool IsInCombat { get; }
    }
}