using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles all movement behavior logic for pattern-based AI.
    /// Executes movement commands based on the current pattern node's movement behavior type.
    ///
    /// Classic Mabinogi Design: Movement patterns are consistent and predictable,
    /// allowing players to learn and counter AI behaviors.
    /// </summary>
    public class PatternMovementController
    {
        private readonly MovementController movementController;
        private readonly WeaponController weaponController;
        private readonly Transform transform;
        private readonly bool enableDebugLogs;

        // Current execution context (set during ExecuteMovementBehavior)
        private Transform currentTargetPlayer;
        private PatternEvaluationContext currentContext;

        // RetreatFixedDistance state tracking
        private Vector3 retreatStartPosition;
        private bool isRetreating = false;

        public PatternMovementController(
            MovementController movementController,
            WeaponController weaponController,
            Transform transform,
            bool enableDebugLogs = false)
        {
            this.movementController = movementController;
            this.weaponController = weaponController;
            this.transform = transform;
            this.enableDebugLogs = enableDebugLogs;
        }

        /// <summary>
        /// Immediately stops all movement. Called when character dies.
        /// </summary>
        public void StopMovement()
        {
            movementController?.SetMovementInput(Vector3.zero);
            isRetreating = false;
        }

        /// <summary>
        /// Executes the movement behavior defined by the current pattern node.
        /// This is the core of pattern-driven movement.
        /// </summary>
        public void ExecuteMovementBehavior(
            PatternNode currentNode,
            Transform targetPlayer,
            PatternEvaluationContext context)
        {
            if (currentNode == null || movementController == null || targetPlayer == null)
            {
                movementController?.SetMovementInput(Vector3.zero);
                return;
            }

            // Store current context for use in movement methods
            currentTargetPlayer = targetPlayer;
            currentContext = context;

            // Check if movement is frozen for this node
            if (currentNode.freezeMovement)
            {
                movementController.SetMovementInput(Vector3.zero);
                return;
            }

            // Execute movement based on behavior type
            switch (currentNode.movementBehavior)
            {
                case MovementBehaviorType.MaintainCustomRange:
                    MaintainCustomRange(
                        targetPlayer,
                        context,
                        currentNode.customTargetRange,
                        currentNode.rangeTolerance,
                        currentNode.movementSpeedMultiplier);
                    break;

                case MovementBehaviorType.ApproachTarget:
                    ApproachTarget(targetPlayer, context, currentNode.movementSpeedMultiplier);
                    break;

                case MovementBehaviorType.RetreatFromTarget:
                    RetreatFromTarget(targetPlayer, currentNode.movementSpeedMultiplier);
                    break;

                case MovementBehaviorType.RetreatFixedDistance:
                    RetreatFixedDistance(targetPlayer, currentNode.retreatDistance, currentNode.movementSpeedMultiplier);
                    break;

                case MovementBehaviorType.CircleStrafeLeft:
                    CircleStrafe(targetPlayer, false, currentNode.movementSpeedMultiplier);
                    break;

                case MovementBehaviorType.CircleStrafeRight:
                    CircleStrafe(targetPlayer, true, currentNode.movementSpeedMultiplier);
                    break;

                case MovementBehaviorType.HoldPosition:
                    HoldPosition();
                    break;

                case MovementBehaviorType.UseFormationSlot:
                    UseFormationSlot(currentNode.rangeTolerance);
                    break;

                case MovementBehaviorType.FollowAtDistance:
                    FollowAtDistance(currentNode.rangeTolerance);
                    break;

                default:
                    movementController.SetMovementInput(Vector3.zero);
                    break;
            }
        }

        /// <summary>
        /// Maintains a specific range from target.
        /// </summary>
        private void MaintainCustomRange(
            Transform targetPlayer,
            PatternEvaluationContext context,
            float targetRange,
            float tolerance,
            float speedMultiplier)
        {
            float distance = context.distanceToPlayer;
            Vector3 toTarget = (targetPlayer.position - transform.position).normalized;

            if (distance < targetRange - tolerance)
            {
                // Too close - move away
                movementController.SetMovementInput(-toTarget * speedMultiplier);
            }
            else if (distance > targetRange + tolerance)
            {
                // Too far - move closer
                movementController.SetMovementInput(toTarget * speedMultiplier);
            }
            else
            {
                // Within acceptable range - stop
                movementController.SetMovementInput(Vector3.zero);
            }
        }

        /// <summary>
        /// Moves directly toward target, stopping at weapon range.
        /// </summary>
        private void ApproachTarget(
            Transform targetPlayer,
            PatternEvaluationContext context,
            float speedMultiplier)
        {
            if (weaponController == null || targetPlayer == null)
            {
                movementController?.SetMovementInput(Vector3.zero);
                return;
            }

            // Stop at weapon range (1.5m for melee, varies for ranged)
            float weaponRange = weaponController.WeaponData?.isRangedWeapon ?? false
                ? weaponController.GetRangedRange()
                : weaponController.GetMeleeRange();

            float distance = context.distanceToPlayer;

            // Approach if beyond weapon range, otherwise stop
            if (distance > weaponRange)
            {
                Vector3 toTarget = (targetPlayer.position - transform.position).normalized;
                movementController.SetMovementInput(toTarget * speedMultiplier);
            }
            else
            {
                // At weapon range - stop moving
                movementController.SetMovementInput(Vector3.zero);
            }
        }

        /// <summary>
        /// Moves directly away from target.
        /// </summary>
        private void RetreatFromTarget(Transform targetPlayer, float speedMultiplier)
        {
            Vector3 awayFromTarget = (transform.position - targetPlayer.position).normalized;
            movementController.SetMovementInput(awayFromTarget * speedMultiplier);
        }

        /// <summary>
        /// Retreats exactly X meters from the starting position and stops.
        /// Uses state tracking to remember where retreat started.
        /// </summary>
        private void RetreatFixedDistance(Transform targetPlayer, float targetDistance, float speedMultiplier)
        {
            // Initialize retreat if not already retreating
            if (!isRetreating)
            {
                retreatStartPosition = transform.position;
                isRetreating = true;

                if (enableDebugLogs)
                {
                    CombatLogger.LogMovement($"[PatternMovementController] {transform.name} started fixed retreat from {retreatStartPosition} (target: {targetDistance}m)");
                }
            }

            // Calculate distance traveled from start position
            float distanceTraveled = Vector3.Distance(transform.position, retreatStartPosition);

            // Check if we've reached the target distance
            if (distanceTraveled >= targetDistance)
            {
                // Stop moving - we've retreated far enough
                movementController.SetMovementInput(Vector3.zero);

                if (enableDebugLogs)
                {
                    CombatLogger.LogMovement($"[PatternMovementController] {transform.name} completed fixed retreat ({distanceTraveled:F2}m traveled, {targetDistance}m target)");
                }
            }
            else
            {
                // Continue retreating away from target
                Vector3 awayFromTarget = (transform.position - targetPlayer.position).normalized;
                movementController.SetMovementInput(awayFromTarget * speedMultiplier);
            }
        }

        /// <summary>
        /// Resets the retreat state tracking. Should be called when transitioning between nodes.
        /// </summary>
        public void ResetRetreatState()
        {
            isRetreating = false;
            retreatStartPosition = Vector3.zero;
        }

        /// <summary>
        /// Strafes in a circle around the target.
        /// </summary>
        private void CircleStrafe(Transform targetPlayer, bool clockwise, float speedMultiplier)
        {
            Vector3 toTarget = (targetPlayer.position - transform.position).normalized;
            Vector3 strafeDirection = clockwise
                ? new Vector3(toTarget.z, 0, -toTarget.x)
                : new Vector3(-toTarget.z, 0, toTarget.x);

            movementController.SetMovementInput(strafeDirection.normalized * speedMultiplier);
        }

        /// <summary>
        /// Stops all movement.
        /// </summary>
        private void HoldPosition()
        {
            movementController.SetMovementInput(Vector3.zero);
        }

        /// <summary>
        /// Requests formation slot from AICoordinator and moves to it.
        /// Falls back to weapon range if no coordinator available.
        /// NOTE: Formation system requires SimpleTestAI component, so this is simplified.
        /// </summary>
        private void UseFormationSlot(float rangeTolerance)
        {
            // AICoordinator now uses IAIAgent interface, so we can use it directly
            if (weaponController != null && currentTargetPlayer != null && currentContext != null)
            {
                float weaponRange = weaponController.WeaponData?.isRangedWeapon ?? false
                    ? weaponController.GetRangedRange()
                    : weaponController.GetMeleeRange();
                MaintainCustomRange(currentTargetPlayer, currentContext, weaponRange, rangeTolerance, 1.0f);
            }
            else
            {
                movementController?.SetMovementInput(Vector3.zero);
            }
        }

        /// <summary>
        /// Follows target at a safe distance (good for ranged characters).
        /// Classic Mabinogi: All melee weapons use uniform range.
        /// </summary>
        private void FollowAtDistance(float rangeTolerance)
        {
            // Use a safe follow distance (longer than optimal range)
            float followDistance = 6.0f; // Default safe distance

            if (weaponController != null && currentTargetPlayer != null && currentContext != null)
            {
                // Note: GetMeleeRange() returns uniform range for all melee weapons (classic Mabinogi design)
                bool isRanged = weaponController.WeaponData?.isRangedWeapon ?? false;
                float baseRange = isRanged
                    ? weaponController.GetRangedRange()  // Varies by ranged weapon type
                    : weaponController.GetMeleeRange();  // Uniform for all melee weapons
                followDistance = baseRange * 1.5f; // 150% of weapon range
            }

            if (currentTargetPlayer != null && currentContext != null)
            {
                MaintainCustomRange(currentTargetPlayer, currentContext, followDistance, rangeTolerance, 1.0f);
            }
            else
            {
                movementController?.SetMovementInput(Vector3.zero);
            }
        }
    }
}
