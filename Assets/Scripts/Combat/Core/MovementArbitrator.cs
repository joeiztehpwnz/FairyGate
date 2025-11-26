using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Interface for movement authority sources.
    /// Each authority can provide movement input and declare whether movement is allowed.
    /// </summary>
    public interface IMovementAuthority
    {
        /// <summary>
        /// Priority level of this authority. Higher values take precedence.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Name of this authority for debugging.
        /// </summary>
        string AuthorityName { get; }

        /// <summary>
        /// Whether this authority is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Whether this authority allows movement at all.
        /// </summary>
        bool AllowsMovement();

        /// <summary>
        /// Gets the movement vector from this authority.
        /// </summary>
        Vector3 GetMovementVector();
    }

    /// <summary>
    /// Standard priority levels for movement authorities.
    /// </summary>
    public static class MovementPriority
    {
        public const int Death = 200;           // Death state blocks all movement
        public const int HardCrowdControl = 150; // Stun, Knockdown, Root
        public const int StatusEffects = 100;    // Slow, Root, other debuffs
        public const int SkillExecution = 75;    // Active skill states
        public const int SoftCrowdControl = 60;  // Knockback
        public const int AIPattern = 50;         // AI pattern movement
        public const int PlayerInput = 25;       // Player keyboard/gamepad input
        public const int Default = 0;            // Fallback priority
    }

    /// <summary>
    /// Arbitrates between multiple movement authorities to determine final movement.
    /// Resolves conflicts based on priority levels.
    /// </summary>
    public class MovementArbitrator : MonoBehaviour
    {
        [Header("Authorities")]
        [SerializeField] private List<IMovementAuthority> authorities = new List<IMovementAuthority>();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showDebugGUI = false;

        // The resolved movement vector
        private Vector3 resolvedMovement = Vector3.zero;
        private bool movementAllowed = true;
        private IMovementAuthority activeAuthority = null;

        /// <summary>
        /// Gets the final resolved movement vector after arbitration.
        /// </summary>
        public Vector3 ResolvedMovement => resolvedMovement;

        /// <summary>
        /// Gets whether movement is currently allowed.
        /// </summary>
        public bool IsMovementAllowed => movementAllowed;

        /// <summary>
        /// Gets the currently active movement authority.
        /// </summary>
        public IMovementAuthority ActiveAuthority => activeAuthority;

        /// <summary>
        /// Registers a new movement authority.
        /// </summary>
        public void RegisterAuthority(IMovementAuthority authority)
        {
            if (authority == null) return;

            if (!authorities.Contains(authority))
            {
                authorities.Add(authority);
                authorities = authorities.OrderByDescending(a => a.Priority).ToList();

                if (enableDebugLogs)
                {
                    CombatLogger.LogMovement($"[MovementArbitrator] Registered {authority.AuthorityName} with priority {authority.Priority}");
                }
            }
        }

        /// <summary>
        /// Unregisters a movement authority.
        /// </summary>
        public void UnregisterAuthority(IMovementAuthority authority)
        {
            if (authority == null) return;

            if (authorities.Remove(authority))
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogMovement($"[MovementArbitrator] Unregistered {authority.AuthorityName}");
                }
            }
        }

        /// <summary>
        /// Resolves movement from all registered authorities.
        /// Called every frame by MovementController.
        /// </summary>
        public Vector3 ResolveMovement()
        {
            // Remove any null authorities (destroyed objects)
            authorities.RemoveAll(a => a == null);

            // Sort by priority (highest first)
            var sortedAuthorities = authorities
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Priority)
                .ToList();

            // Check for movement blockers
            var blocker = sortedAuthorities.FirstOrDefault(a => !a.AllowsMovement());
            if (blocker != null)
            {
                movementAllowed = false;
                resolvedMovement = Vector3.zero;
                activeAuthority = blocker;

                if (enableDebugLogs)
                {
                    CombatLogger.LogMovement($"[MovementArbitrator] Movement blocked by {blocker.AuthorityName}");
                }

                return Vector3.zero;
            }

            // Get movement from highest priority authority with non-zero movement
            movementAllowed = true;
            foreach (var authority in sortedAuthorities)
            {
                var movement = authority.GetMovementVector();
                if (movement != Vector3.zero)
                {
                    resolvedMovement = movement;
                    activeAuthority = authority;

                    if (enableDebugLogs)
                    {
                        CombatLogger.LogMovement($"[MovementArbitrator] Movement controlled by {authority.AuthorityName} (priority {authority.Priority})");
                    }

                    return resolvedMovement;
                }
            }

            // No authority provided movement
            resolvedMovement = Vector3.zero;
            activeAuthority = null;
            return Vector3.zero;
        }

        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(10, 200, 300, 400));
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Movement Arbitrator - {name}");
            GUILayout.Label($"Movement Allowed: {movementAllowed}");
            GUILayout.Label($"Active Authority: {activeAuthority?.AuthorityName ?? "None"}");
            GUILayout.Label($"Resolved Movement: {resolvedMovement}");
            GUILayout.Space(10);
            GUILayout.Label("Registered Authorities:");

            foreach (var authority in authorities.OrderByDescending(a => a.Priority))
            {
                if (authority == null) continue;

                var status = authority.IsActive ? "Active" : "Inactive";
                var allows = authority.AllowsMovement() ? "Allows" : "BLOCKS";
                var movement = authority.GetMovementVector();

                GUILayout.Label($"  [{authority.Priority}] {authority.AuthorityName}");
                GUILayout.Label($"      Status: {status}, {allows}, Vec: {movement}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Example implementation of a player input movement authority.
    /// </summary>
    public class PlayerMovementAuthority : IMovementAuthority
    {
        private readonly Transform transform;
        private Vector3 inputVector;

        public int Priority => MovementPriority.PlayerInput;
        public string AuthorityName => "Player Input";
        public bool IsActive => true; // Always active for player

        public PlayerMovementAuthority(Transform transform)
        {
            this.transform = transform;
        }

        public bool AllowsMovement()
        {
            return true; // Player input doesn't block movement
        }

        public Vector3 GetMovementVector()
        {
            // Get input from keyboard/gamepad
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            inputVector = new Vector3(horizontal, 0, vertical).normalized;
            return inputVector;
        }
    }

    /// <summary>
    /// Example implementation of an AI pattern movement authority.
    /// </summary>
    public class AIPatternMovementAuthority : IMovementAuthority
    {
        private readonly Transform transform;
        private Vector3 aiMovement;
        private bool isActive;

        public int Priority => MovementPriority.AIPattern;
        public string AuthorityName => "AI Pattern";
        public bool IsActive => isActive;

        public AIPatternMovementAuthority(Transform transform)
        {
            this.transform = transform;
        }

        public void SetMovementInput(Vector3 movement)
        {
            aiMovement = movement;
            isActive = movement != Vector3.zero;
        }

        public bool AllowsMovement()
        {
            return true; // AI patterns don't block movement
        }

        public Vector3 GetMovementVector()
        {
            return aiMovement;
        }
    }

    /// <summary>
    /// Example implementation of a skill state movement authority.
    /// </summary>
    public class SkillStateMovementAuthority : IMovementAuthority
    {
        private readonly SkillSystem skillSystem;
        private bool blocksMovement;
        private Vector3 skillMovement;

        public int Priority => MovementPriority.SkillExecution;
        public string AuthorityName => "Skill State";
        public bool IsActive { get; private set; }

        public SkillStateMovementAuthority(SkillSystem skillSystem)
        {
            this.skillSystem = skillSystem;
        }

        public void UpdateState(bool blocks, Vector3 movement = default)
        {
            blocksMovement = blocks;
            skillMovement = movement;
            IsActive = blocks || movement != Vector3.zero;
        }

        public bool AllowsMovement()
        {
            return !blocksMovement;
        }

        public Vector3 GetMovementVector()
        {
            return skillMovement;
        }
    }
}