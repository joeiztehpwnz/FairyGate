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
        bool RequestAttackPermission(IAIAgent requester);

        /// <summary>
        /// Releases attack slot when attack completes.
        /// </summary>
        void ReleaseAttackSlot(IAIAgent attacker);

        /// <summary>
        /// Requests a formation slot position. Returns null if unavailable.
        /// </summary>
        Vector3? RequestFormationSlot(IAIAgent requester, float desiredDistance);

        /// <summary>
        /// Releases formation slot when leaving combat or dying.
        /// </summary>
        void ReleaseFormationSlot(IAIAgent enemy);

        /// <summary>
        /// Registers an AI enemy when it becomes active.
        /// </summary>
        void RegisterEnemy(IAIAgent enemy);

        /// <summary>
        /// Unregisters an AI enemy when it dies or exits combat.
        /// </summary>
        void UnregisterEnemy(IAIAgent enemy);
    }
}
