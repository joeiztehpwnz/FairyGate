using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Manages formation slot allocation and positioning for AI enemies.
    /// Handles slot assignment, release, and position calculation in a circular formation around the player.
    /// </summary>
    public class FormationManager
    {
        /// <summary>
        /// Represents a single slot in the formation circle around the player.
        /// </summary>
        private class FormationSlot
        {
            public int slotIndex;
            public Vector3 baseDirection; // Direction from player (0-360 degrees)
            public IAIAgent occupant; // Current AI occupying this slot
            public float lastAssignTime; // For preventing slot thrashing
        }

        private readonly List<FormationSlot> formationSlots = new List<FormationSlot>();
        private Transform playerTransform;
        private readonly bool enableDebugLogs;

        /// <summary>
        /// Gets whether the formation system is currently enabled.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the cached player transform reference.
        /// </summary>
        public Transform PlayerTransform
        {
            get => playerTransform;
            set => playerTransform = value;
        }

        /// <summary>
        /// Gets the count of occupied formation slots.
        /// </summary>
        public int OccupiedSlotCount => formationSlots.Count(s => s.occupant != null);

        /// <summary>
        /// Gets the total count of available formation slots.
        /// </summary>
        public int TotalSlotCount => formationSlots.Count;

        /// <summary>
        /// Initializes a new instance of the FormationManager.
        /// </summary>
        /// <param name="useFormationSystem">Whether formation system should be enabled</param>
        /// <param name="enableDebugLogs">Whether to log debug messages</param>
        public FormationManager(bool useFormationSystem, bool enableDebugLogs)
        {
            this.IsEnabled = useFormationSystem;
            this.enableDebugLogs = enableDebugLogs;
            InitializeFormationSlots();
        }

        /// <summary>
        /// Initializes formation slots in a circular pattern around the player.
        /// Creates slots evenly distributed in a 360-degree circle.
        /// </summary>
        private void InitializeFormationSlots()
        {
            formationSlots.Clear();

            // Create 8 slots in a circle (45 degrees apart)
            float angleStep = 360f / CombatConstants.FORMATION_SLOT_COUNT;

            for (int i = 0; i < CombatConstants.FORMATION_SLOT_COUNT; i++)
            {
                float angle = i * angleStep;
                float radians = angle * Mathf.Deg2Rad;

                formationSlots.Add(new FormationSlot
                {
                    slotIndex = i,
                    baseDirection = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)),
                    occupant = null,
                    lastAssignTime = -999f
                });
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogFormation($"[FormationManager] Initialized {formationSlots.Count} formation slots");
            }
        }

        /// <summary>
        /// Request a formation slot position for the given AI agent.
        /// Returns the world position if successful, null if no slots available or system disabled.
        /// </summary>
        /// <param name="requester">The AI agent requesting a formation slot</param>
        /// <param name="desiredDistance">The desired distance from the player</param>
        /// <returns>World position of the assigned slot, or null if unavailable</returns>
        public Vector3? RequestFormationSlot(IAIAgent requester, float desiredDistance)
        {
            if (!IsEnabled || playerTransform == null)
                return null;

            // Check if requester already has a slot
            FormationSlot currentSlot = formationSlots.Find(slot => slot.occupant == requester);
            if (currentSlot != null)
            {
                // Already has a slot, return updated position
                return CalculateSlotPosition(currentSlot, desiredDistance);
            }

            // Find nearest available slot
            FormationSlot bestSlot = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < formationSlots.Count; i++)
            {
                FormationSlot slot = formationSlots[i];

                // Skip occupied slots
                if (slot.occupant != null && slot.occupant != requester)
                    continue;

                // Skip slots on cooldown (prevent thrashing)
                if (Time.time - slot.lastAssignTime < CombatConstants.FORMATION_SLOT_REASSIGN_COOLDOWN)
                    continue;

                // Calculate score (prefer slots closer to requester's current position)
                Vector3 slotPosition = CalculateSlotPosition(slot, desiredDistance);
                float distance = Vector3.Distance(requester.transform.position, slotPosition);

                if (distance < bestScore)
                {
                    bestScore = distance;
                    bestSlot = slot;
                }
            }

            if (bestSlot != null)
            {
                // Assign slot
                bestSlot.occupant = requester;
                bestSlot.lastAssignTime = Time.time;

                if (enableDebugLogs)
                {
                    CombatLogger.LogFormation($"[FormationManager] Assigned formation slot {bestSlot.slotIndex} to {requester.name}");
                }

                return CalculateSlotPosition(bestSlot, desiredDistance);
            }

            // No slots available
            return null;
        }

        /// <summary>
        /// Releases the formation slot occupied by the specified AI agent.
        /// </summary>
        /// <param name="enemy">The AI agent releasing its slot</param>
        public void ReleaseFormationSlot(IAIAgent enemy)
        {
            for (int i = 0; i < formationSlots.Count; i++)
            {
                if (formationSlots[i].occupant == enemy)
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogFormation($"[FormationManager] Released formation slot {formationSlots[i].slotIndex} from {enemy.name}");
                    }

                    formationSlots[i].occupant = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Clears all formation slots (used during cleanup).
        /// </summary>
        public void ClearAllSlots()
        {
            formationSlots.Clear();

            if (enableDebugLogs)
            {
                CombatLogger.LogFormation("[FormationManager] Cleared all formation slots");
            }
        }

        /// <summary>
        /// Calculates the world position for a formation slot at the given distance from the player.
        /// Includes a small deterministic offset for visual variation.
        /// </summary>
        /// <param name="slot">The formation slot to calculate position for</param>
        /// <param name="distance">The distance from the player</param>
        /// <returns>World position of the slot</returns>
        private Vector3 CalculateSlotPosition(FormationSlot slot, float distance)
        {
            if (playerTransform == null)
                return Vector3.zero;

            // Base position = player + (direction * distance)
            Vector3 basePosition = playerTransform.position + (slot.baseDirection * distance);

            // Add small random offset for variation (per slot, consistent)
            Vector3 offset = new Vector3(
                Mathf.Sin(slot.slotIndex * 1.23f) * CombatConstants.FORMATION_SLOT_OFFSET,
                0f,
                Mathf.Cos(slot.slotIndex * 2.34f) * CombatConstants.FORMATION_SLOT_OFFSET
            );

            return basePosition + offset;
        }

        /// <summary>
        /// Draws debug gizmos for formation slots in the Scene view.
        /// </summary>
        /// <param name="showGizmos">Whether to draw the gizmos</param>
        public void DrawDebugGizmos(bool showGizmos)
        {
            if (!showGizmos || !IsEnabled || playerTransform == null)
                return;

            // Draw formation slots
            for (int i = 0; i < formationSlots.Count; i++)
            {
                FormationSlot slot = formationSlots[i];

                // Use average distance for visualization (2.0 units)
                Vector3 slotPosition = CalculateSlotPosition(slot, 2.0f);

                // Color based on occupancy
                Gizmos.color = slot.occupant != null ? Color.red : Color.green;

                // Draw sphere at slot position
                Gizmos.DrawWireSphere(slotPosition, 0.3f);

                // Draw line from player to slot
                Gizmos.color = slot.occupant != null ? new Color(1f, 0.5f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f, 0.3f);
                Gizmos.DrawLine(playerTransform.position, slotPosition);

                #if UNITY_EDITOR
                // Label slot number
                UnityEditor.Handles.Label(
                    slotPosition + Vector3.up * 0.5f,
                    $"Slot {i}\n{(slot.occupant != null ? slot.occupant.name : "Empty")}"
                );
                #endif
            }
        }
    }
}
