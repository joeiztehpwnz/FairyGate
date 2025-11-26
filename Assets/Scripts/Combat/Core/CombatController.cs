using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    public class CombatController : MonoBehaviour, ICombatUpdatable
    {
        [Header("Combat Configuration")]
        [SerializeField] private CharacterStats baseStats;
        [SerializeField] private CombatState currentCombatState = CombatState.Idle;
        [SerializeField] private Transform currentTarget;
        [SerializeField] private LayerMask enemyLayerMask = -1;

        [Header("Faction")]
        [SerializeField] private Faction faction = Faction.Enemy;

        [Header("Input")]
        [SerializeField] private KeyCode targetKey = KeyCode.Tab;
        [SerializeField] private KeyCode exitCombatKey = KeyCode.Escape;
        [SerializeField] private KeyCode restKey = KeyCode.X;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showCombatGUI = true;

        // C# Events (replaces UnityEvents for performance)
        public event Action OnCombatEntered;
        public event Action OnCombatExited;
        public event Action<Transform> OnTargetChanged;

        // Component references
        private HealthSystem healthSystem;
        private StaminaSystem staminaSystem;
        private StatusEffectManager statusEffectManager;
        private WeaponController weaponController;
        private SkillSystem skillSystem;
        private MovementController movementController;
        private EquipmentManager equipmentManager;

        // Cached list for target finding (Phase 3.4 optimization)
        private List<Transform> cachedTargetsList = new List<Transform>();

        // Public Properties
        public CharacterStats BaseStats => baseStats;
        public CharacterStats Stats => equipmentManager != null ? equipmentManager.ModifiedStats : baseStats;
        public Transform WeaponTransform => weaponController?.transform;
        public Transform Transform => transform;
        public bool IsInCombat => currentCombatState == CombatState.Combat || currentCombatState == CombatState.Charging || currentCombatState == CombatState.Executing;
        public Transform CurrentTarget => currentTarget;
        public CombatState CurrentCombatState => currentCombatState;

        // IDamageable Properties
        public int CurrentHealth => healthSystem?.CurrentHealth ?? 0;
        public int MaxHealth => healthSystem?.MaxHealth ?? 0;
        public bool IsAlive => healthSystem?.IsAlive ?? false;

        // ISkillExecutor Properties
        public SkillExecutionState CurrentState => skillSystem?.CurrentState ?? SkillExecutionState.Uncharged;
        public SkillType CurrentSkill => skillSystem?.CurrentSkill ?? SkillType.Attack;
        public float ChargeProgress => skillSystem?.ChargeProgress ?? 0f;

        // IStatusEffectTarget Properties
        public List<StatusEffect> ActiveStatusEffects => statusEffectManager?.ActiveStatusEffects ?? new List<StatusEffect>();

        // Faction Properties
        public Faction Faction => faction;

        /// <summary>
        /// Checks if this character is hostile to the specified faction.
        /// </summary>
        public bool IsHostileTo(Faction other)
        {
            return faction switch
            {
                Faction.Player => other == Faction.Enemy,
                Faction.Enemy => other == Faction.Player || other == Faction.Ally,
                Faction.Ally => other == Faction.Enemy,
                Faction.Neutral => false,
                _ => false
            };
        }

        /// <summary>
        /// Checks if this character is hostile to another CombatController.
        /// </summary>
        public bool IsHostileTo(CombatController other)
        {
            return other != null && IsHostileTo(other.Faction);
        }

        /// <summary>
        /// Sets the faction for this character. Used by scene setup.
        /// </summary>
        public void SetFaction(Faction newFaction)
        {
            faction = newFaction;
        }

        private void Awake()
        {
            // Get component references
            healthSystem = GetComponent<HealthSystem>();
            staminaSystem = GetComponent<StaminaSystem>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            weaponController = GetComponent<WeaponController>();
            skillSystem = GetComponent<SkillSystem>();
            movementController = GetComponent<MovementController>();
            equipmentManager = GetComponent<EquipmentManager>();

            if (baseStats == null)
            {
                CombatLogger.LogCombat($"CombatController on {gameObject.name} has no CharacterStats assigned. Using default values.", CombatLogger.LogLevel.Warning);
                baseStats = CharacterStats.CreateDefaultStats();
            }

            // Validate required components
            ValidateComponents();

            // Register with combat update manager
            CombatUpdateManager.Register(this);
        }

        private void OnDestroy()
        {
            // Unregister to prevent memory leaks
            CombatUpdateManager.Unregister(this);
        }

        // Renamed from Update() to CombatUpdate() for centralized update management
        public void CombatUpdate(float deltaTime)
        {
            HandleCombatInput();
            UpdateCombatState();
        }

        private void HandleCombatInput()
        {
            // Target cycling
            if (Input.GetKeyDown(targetKey))
            {
                CycleTarget();
            }

            // Exit combat
            if (Input.GetKeyDown(exitCombatKey))
            {
                ExitCombat();
            }

            // Rest
            if (Input.GetKeyDown(restKey))
            {
                ToggleRest();
            }
        }

        private void UpdateCombatState()
        {
            CombatState newState = DetermineCombatState();

            if (newState != currentCombatState)
            {
                var oldState = currentCombatState;
                currentCombatState = newState;

                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{gameObject.name} combat state changed: {oldState} -> {newState}");
                }

                OnCombatStateChanged(oldState, newState);
            }
        }

        private CombatState DetermineCombatState()
        {
            // Death overrides all other states
            if (!IsAlive)
                return CombatState.Dead;

            // Status effects determine state (priority: Knockdown > Knockback > Stun)
            if (statusEffectManager.IsKnockedDown)
                return CombatState.KnockedDown;

            if (statusEffectManager.IsKnockedBack)
                return CombatState.Knockback;

            if (statusEffectManager.IsStunned)
                return CombatState.Stunned;

            if (statusEffectManager.IsResting)
                return CombatState.Resting;

            // Skill execution states
            if (skillSystem.CurrentState == SkillExecutionState.Charging)
                return CombatState.Charging;

            if (skillSystem.CurrentState != SkillExecutionState.Uncharged)
                return CombatState.Executing;

            // Combat targeting state
            if (currentTarget != null)
                return CombatState.Combat;

            // Default idle state
            return CombatState.Idle;
        }

        private void OnCombatStateChanged(CombatState oldState, CombatState newState)
        {
            // Handle state transitions
            switch (newState)
            {
                case CombatState.Combat:
                    if (oldState == CombatState.Idle)
                    {
                        OnCombatEntered?.Invoke();
                    }
                    break;

                case CombatState.Idle:
                    if (IsInCombatState(oldState))
                    {
                        OnCombatExited?.Invoke();
                    }
                    break;
            }
        }

        private bool IsInCombatState(CombatState state)
        {
            return state == CombatState.Combat || state == CombatState.Charging || state == CombatState.Executing;
        }

        public void EnterCombat(Transform target)
        {
            if (target == null || target == transform) return;

            SetTarget(target);

            // Force immediate combat state update instead of waiting for next Update()
            UpdateCombatState();

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{gameObject.name} entered combat with {target.name}");
            }
        }

        public void ExitCombat()
        {
            // Cannot exit combat during active skill frames
            if (skillSystem.CurrentState == SkillExecutionState.Active)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{gameObject.name} cannot exit combat during active skill frames");
                }
                return;
            }

            // Cancel any active skills before exiting
            if (skillSystem.CurrentState != SkillExecutionState.Uncharged)
            {
                skillSystem.CancelSkill();
            }

            currentTarget = null;
            OnTargetChanged?.Invoke(null);

            // Force immediate combat state update instead of waiting for next Update()
            UpdateCombatState();

            if (enableDebugLogs)
            {
                CombatLogger.LogCombat($"{gameObject.name} exited combat");
            }
        }

        public void SetTarget(Transform target)
        {
            // Validate target is hostile before setting
            if (target != null)
            {
                var targetCombat = target.GetComponent<CombatController>();
                if (targetCombat != null && !IsHostileTo(targetCombat))
                {
                    if (enableDebugLogs)
                    {
                        CombatLogger.LogCombat($"{gameObject.name} rejected non-hostile target {target.name} (faction: {targetCombat.Faction})");
                    }
                    return; // Don't set non-hostile targets
                }
            }

            currentTarget = target;
            OnTargetChanged?.Invoke(target);

            if (enableDebugLogs && target != null)
            {
                CombatLogger.LogCombat($"{gameObject.name} now targeting {target.name}");
            }
        }

        private void CycleTarget()
        {
            var potentialTargets = FindPotentialTargets();

            if (potentialTargets.Length == 0)
            {
                if (enableDebugLogs)
                {
                    CombatLogger.LogCombat($"{gameObject.name} no targets available");
                }
                return;
            }

            // Find current target index
            int currentIndex = -1;
            if (currentTarget != null)
            {
                for (int i = 0; i < potentialTargets.Length; i++)
                {
                    if (potentialTargets[i] == currentTarget)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Select next target
            int nextIndex = (currentIndex + 1) % potentialTargets.Length;
            EnterCombat(potentialTargets[nextIndex]);
        }

        private Transform[] FindPotentialTargets()
        {
            var colliders = Physics.OverlapSphere(transform.position, 10f, enemyLayerMask);
            cachedTargetsList.Clear(); // Phase 3.4: Reuse cached list

            foreach (var collider in colliders)
            {
                if (collider.transform == transform) continue;

                var targetCombatController = collider.GetComponent<CombatController>();
                if (targetCombatController == null) continue;

                // Only target hostile factions
                if (!IsHostileTo(targetCombatController)) continue;

                var targetHealth = collider.GetComponent<HealthSystem>();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    cachedTargetsList.Add(collider.transform);
                }
            }

            return cachedTargetsList.OrderBy(t => Vector3.Distance(transform.position, t.position)).ToArray();
        }

        private void ToggleRest()
        {
            if (staminaSystem.IsResting)
            {
                staminaSystem.StopResting();
            }
            else
            {
                staminaSystem.StartResting();
            }
        }

        // Range and Distance Methods
        public bool IsInRangeOf(Transform target)
        {
            return weaponController.IsInRange(target);
        }

        public float GetDistanceTo(Transform target)
        {
            return weaponController.GetDistanceTo(target);
        }

        // ISkillExecutor implementation
        public bool CanChargeSkill(SkillType skillType)
        {
            return skillSystem.CanChargeSkill(skillType);
        }

        public bool CanExecuteSkill(SkillType skillType)
        {
            return skillSystem.CanExecuteSkill(skillType);
        }

        public void StartCharging(SkillType skillType)
        {
            skillSystem.StartCharging(skillType);
        }

        public void ExecuteSkill(SkillType skillType)
        {
            skillSystem.ExecuteSkill(skillType);
        }

        public void CancelSkill()
        {
            skillSystem.CancelSkill();
        }

        // IDamageable implementation
        public void TakeDamage(int damage, Transform source)
        {
            healthSystem.TakeDamage(damage, source);
        }

        public void Die()
        {
            healthSystem.Die();
        }

        // IStatusEffectTarget implementation
        public bool HasStatusEffect(StatusEffectType type)
        {
            return statusEffectManager.HasStatusEffect(type);
        }

        public void ApplyStatusEffect(StatusEffect effect)
        {
            statusEffectManager.ApplyStatusEffect(effect);
        }

        public void RemoveStatusEffect(StatusEffectType type)
        {
            statusEffectManager.RemoveStatusEffect(type);
        }

        public void ClearAllStatusEffects()
        {
            statusEffectManager.ClearAllStatusEffects();
        }

        private void ValidateComponents()
        {
            if (healthSystem == null)
                CombatLogger.LogCombat($"{gameObject.name} CombatController requires HealthSystem component", CombatLogger.LogLevel.Error);

            if (weaponController == null)
                CombatLogger.LogCombat($"{gameObject.name} CombatController requires WeaponController component", CombatLogger.LogLevel.Error);

            if (skillSystem == null)
                CombatLogger.LogCombat($"{gameObject.name} CombatController requires SkillSystem component", CombatLogger.LogLevel.Error);

            if (statusEffectManager == null)
                CombatLogger.LogCombat($"{gameObject.name} CombatController requires StatusEffectManager component", CombatLogger.LogLevel.Error);

            if (staminaSystem == null)
                CombatLogger.LogCombat($"{gameObject.name} CombatController requires StaminaSystem component", CombatLogger.LogLevel.Error);
        }

        // GUI Debug Display
        private void OnGUI()
        {
            if (showCombatGUI && Application.isPlaying)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.5f);
                screenPos.y = Screen.height - screenPos.y;

                string combatText = $"State: {currentCombatState}\n" +
                                   $"Target: {(currentTarget ? currentTarget.name : "None")}";

                GUI.Label(new Rect(screenPos.x - 60, screenPos.y, 120, 40), combatText);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw target line
            if (currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTarget.position);
                Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
            }

            // Draw target detection range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 10f);
        }
    }
}