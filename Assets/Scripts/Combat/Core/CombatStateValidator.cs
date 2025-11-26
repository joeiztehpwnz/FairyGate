using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Centralized validator for combat state queries.
    /// Consolidates all "can perform action" logic to eliminate duplication
    /// and provide a single source of truth for state validation.
    /// </summary>
    public interface ICombatStateValidator
    {
        /// <summary>
        /// Checks if the entity can move based on all movement restrictions.
        /// </summary>
        bool CanMove();

        /// <summary>
        /// Checks if the entity can start charging/aiming a new skill.
        /// </summary>
        bool CanStartSkill(SkillType skillType);

        /// <summary>
        /// Checks if the entity can cancel their current skill.
        /// </summary>
        bool CanCancelSkill();

        /// <summary>
        /// Checks if the AI pattern can transition to a new node.
        /// </summary>
        bool CanTransitionNode();

        /// <summary>
        /// Checks if the entity can execute a charged/aimed skill.
        /// </summary>
        bool CanExecuteSkill(SkillType skillType);

        /// <summary>
        /// Checks if the entity is in a state that allows skill interruption.
        /// </summary>
        bool CanBeInterrupted();

        /// <summary>
        /// Checks if the entity can target another combatant.
        /// </summary>
        bool CanTarget(Transform target);

        /// <summary>
        /// Gets a debug string describing the current state restrictions.
        /// </summary>
        string GetStateDebugInfo();
    }

    /// <summary>
    /// Implementation of centralized combat state validator.
    /// </summary>
    public class CombatStateValidator : MonoBehaviour, ICombatStateValidator
    {
        // Required components
        private SkillSystem skillSystem;
        private CombatController combatController;
        private MovementController movementController;
        private StatusEffectManager statusEffectManager;
        private HealthSystem healthSystem;
        private WeaponController weaponController;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private void Awake()
        {
            // Cache component references
            skillSystem = GetComponent<SkillSystem>();
            combatController = GetComponent<CombatController>();
            movementController = GetComponent<MovementController>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            healthSystem = GetComponent<HealthSystem>();
            weaponController = GetComponent<WeaponController>();

            ValidateComponents();
        }

        private void ValidateComponents()
        {
            if (skillSystem == null)
                CombatLogger.LogCombat($"[CombatStateValidator] Missing SkillSystem on {name}", CombatLogger.LogLevel.Error);
            if (combatController == null)
                CombatLogger.LogCombat($"[CombatStateValidator] Missing CombatController on {name}", CombatLogger.LogLevel.Error);
            if (movementController == null)
                CombatLogger.LogCombat($"[CombatStateValidator] Missing MovementController on {name}", CombatLogger.LogLevel.Error);
            if (healthSystem == null)
                CombatLogger.LogCombat($"[CombatStateValidator] Missing HealthSystem on {name}", CombatLogger.LogLevel.Error);
        }

        /// <summary>
        /// Checks if the entity can move based on all movement restrictions.
        /// Priority: Death > Knockdown > Stun > Knockback > Status Effects > Skill States > Movement Controller
        /// </summary>
        public bool CanMove()
        {
            // Dead entities can't move
            if (healthSystem != null && !healthSystem.IsAlive)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot move - dead");
                return false;
            }

            // Check combat states that prevent movement
            var combatState = combatController?.CurrentCombatState ?? CombatState.Idle;
            switch (combatState)
            {
                case CombatState.KnockedDown:
                case CombatState.Stunned:
                case CombatState.Knockback:
                case CombatState.Dead:
                    if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot move - state: {combatState}");
                    return false;
            }

            // Check status effects (root, freeze, etc.)
            if (statusEffectManager != null && !statusEffectManager.CanMove)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot move - status effect");
                return false;
            }

            // Check if skill state prevents movement
            if (skillSystem != null)
            {
                var skillState = skillSystem.CurrentState;

                // These states typically prevent movement
                if (skillState == SkillExecutionState.Startup ||
                    skillState == SkillExecutionState.Active ||
                    skillState == SkillExecutionState.Recovery)
                {
                    // Some skills allow movement during execution
                    if (!IsMovementAllowedDuringSkill(skillSystem.CurrentSkill))
                    {
                        if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot move - skill state: {skillState}");
                        return false;
                    }
                }
            }

            // Check movement controller's canMove flag (highest priority lock)
            if (movementController != null && !movementController.CanMove)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot move - movement controller locked");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the entity can start charging/aiming a new skill.
        /// </summary>
        public bool CanStartSkill(SkillType skillType)
        {
            // Dead entities can't use skills
            if (healthSystem != null && !healthSystem.IsAlive)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot start {skillType} - dead");
                return false;
            }

            // Check combat states that prevent skill use
            var combatState = combatController?.CurrentCombatState ?? CombatState.Idle;
            switch (combatState)
            {
                case CombatState.KnockedDown:
                case CombatState.Stunned:
                case CombatState.Dead:
                    if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot start {skillType} - state: {combatState}");
                    return false;
                case CombatState.Knockback:
                    // Some defensive skills can be started during knockback
                    if (!IsDefensiveSkill(skillType))
                    {
                        if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot start {skillType} - in knockback");
                        return false;
                    }
                    break;
            }

            // Check skill system readiness
            if (skillSystem != null)
            {
                var currentState = skillSystem.CurrentState;

                // Can only start new skills when uncharged
                if (currentState != SkillExecutionState.Uncharged)
                {
                    if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot start {skillType} - wrong state: {currentState}");
                    return false;
                }

                // Check stamina and other skill-specific requirements
                if (!skillSystem.CanChargeSkill(skillType))
                {
                    if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot start {skillType} - insufficient stamina or cooldown");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the entity can cancel their current skill.
        /// </summary>
        public bool CanCancelSkill()
        {
            if (skillSystem == null) return false;

            var currentState = skillSystem.CurrentState;

            // Can cancel during charging, charged, aiming, or waiting states
            return currentState == SkillExecutionState.Charging ||
                   currentState == SkillExecutionState.Charged ||
                   currentState == SkillExecutionState.Aiming ||
                   currentState == SkillExecutionState.Waiting;
        }

        /// <summary>
        /// Checks if the AI pattern can transition to a new node.
        /// </summary>
        public bool CanTransitionNode()
        {
            if (skillSystem == null) return true;

            var currentState = skillSystem.CurrentState;

            // Standard transitions allowed in these states
            if (currentState == SkillExecutionState.Uncharged ||
                currentState == SkillExecutionState.Charging ||
                currentState == SkillExecutionState.Charged ||
                currentState == SkillExecutionState.Aiming)
            {
                return true;
            }

            // Check if in a defensive interrupt-capable state
            var combatState = combatController?.CurrentCombatState ?? CombatState.Idle;
            if (combatState == CombatState.Knockback || combatState == CombatState.Stunned)
            {
                // Allow defensive transitions during knockback/stun
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the entity can execute a charged/aimed skill.
        /// </summary>
        public bool CanExecuteSkill(SkillType skillType)
        {
            if (skillSystem == null) return false;

            // Check if skill is ready for execution
            if (skillType == SkillType.RangedAttack)
            {
                // Ranged attacks execute from Aiming state
                return skillSystem.CurrentState == SkillExecutionState.Aiming &&
                       skillSystem.CurrentSkill == skillType;
            }
            else
            {
                // Other skills execute from Charged state
                return skillSystem.CurrentState == SkillExecutionState.Charged &&
                       skillSystem.CurrentSkill == skillType;
            }
        }

        /// <summary>
        /// Checks if the entity is in a state that allows skill interruption.
        /// </summary>
        public bool CanBeInterrupted()
        {
            if (skillSystem == null) return false;

            var currentState = skillSystem.CurrentState;

            // Can interrupt charging, charged, aiming, and waiting states
            // Cannot interrupt startup, active, or recovery states
            return currentState == SkillExecutionState.Charging ||
                   currentState == SkillExecutionState.Charged ||
                   currentState == SkillExecutionState.Aiming ||
                   currentState == SkillExecutionState.Waiting;
        }

        /// <summary>
        /// Checks if the entity can target another combatant.
        /// </summary>
        public bool CanTarget(Transform target)
        {
            if (target == null) return false;

            // Check if target is valid
            if (!target.gameObject.activeInHierarchy)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot target {target.name} - inactive");
                return false;
            }

            // Check if target is alive
            var targetHealth = target.GetComponent<HealthSystem>();
            if (targetHealth != null && !targetHealth.IsAlive)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot target {target.name} - dead");
                return false;
            }

            // Check if target is a valid combatant
            var targetCombat = target.GetComponent<CombatController>();
            if (targetCombat == null)
            {
                if (enableDebugLogs) CombatLogger.LogCombat($"[CombatStateValidator] {name} cannot target {target.name} - not a combatant");
                return false;
            }

            // Additional checks could include faction, stealth, etc.
            return true;
        }

        /// <summary>
        /// Gets a debug string describing the current state restrictions.
        /// </summary>
        public string GetStateDebugInfo()
        {
            var info = $"[{name} State Info]\n";
            info += $"  Health: {(healthSystem?.IsAlive ?? false ? "Alive" : "Dead")}\n";
            info += $"  Combat State: {combatController?.CurrentCombatState ?? CombatState.Idle}\n";
            info += $"  Skill State: {skillSystem?.CurrentState ?? SkillExecutionState.Uncharged}\n";
            info += $"  Current Skill: {(skillSystem != null ? skillSystem.CurrentSkill.ToString() : "None")}\n";
            info += $"  Can Move: {CanMove()}\n";
            info += $"  Can Start Skill: {CanStartSkill(SkillType.Attack)}\n";
            info += $"  Can Cancel: {CanCancelSkill()}\n";
            info += $"  Can Transition: {CanTransitionNode()}\n";
            info += $"  Can Be Interrupted: {CanBeInterrupted()}\n";
            return info;
        }

        // Helper methods
        private bool IsDefensiveSkill(SkillType skillType)
        {
            return skillType == SkillType.Defense || skillType == SkillType.Counter;
        }

        private bool IsMovementAllowedDuringSkill(SkillType skillType)
        {
            // Some skills might allow movement during execution
            // This could be configured per skill
            return false; // Default: no movement during skills
        }
    }
}