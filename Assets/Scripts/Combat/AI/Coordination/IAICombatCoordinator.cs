using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Interface for AI combat coordination (attack timing and formation positioning).
    /// Allows dependency injection and testing.
    /// </summary>
    public interface IAICombatCoordinator
    {
        /// <summary>
        /// Requests permission to attack. Returns true if granted.
        /// </summary>
        bool RequestAttackPermission(SimpleTestAI requester);

        /// <summary>
        /// Releases attack slot when attack completes.
        /// </summary>
        void ReleaseAttackSlot(SimpleTestAI attacker);

        /// <summary>
        /// Requests a formation slot position. Returns null if unavailable.
        /// </summary>
        Vector3? RequestFormationSlot(SimpleTestAI requester, float desiredDistance);

        /// <summary>
        /// Releases formation slot when leaving combat or dying.
        /// </summary>
        void ReleaseFormationSlot(SimpleTestAI enemy);

        /// <summary>
        /// Registers an AI enemy when it becomes active.
        /// </summary>
        void RegisterEnemy(SimpleTestAI enemy);

        /// <summary>
        /// Unregisters an AI enemy when it dies or exits combat.
        /// </summary>
        void UnregisterEnemy(SimpleTestAI enemy);
    }
}
