using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    public class CombatController : MonoBehaviour, ICombatant, ICombatUpdatable
    {
        [Header("Combat Configuration")]
        [SerializeField] private CharacterStats baseStats;
        [SerializeField] private CombatState currentCombatState = CombatState.Idle;
        [SerializeField] private Transform currentTarget;
        [SerializeField] private LayerMask enemyLayerMask = -1;

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

        // ICombatant Properties
        public CharacterStats BaseStats => baseStats;
        public CharacterStats Stats => equipmentManager != null ? equipmentManager.ModifiedStats : baseStats;
        public Transform WeaponTransform => weaponController?.transform;
        public Transform Transform => transform;
        public bool IsInCombat => currentCombatState == CombatState.Combat || currentCombatState == CombatState.Charging || currentCombatState == CombatState.Executing;
        public Transform CurrentTarget => currentTarget;

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
                Debug.LogWarning($"CombatController on {gameObject.name} has no CharacterStats assigned. Using default values.");
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
                    Debug.Log($"{gameObject.name} combat state changed: {oldState} -> {newState}");
                }

                OnCombatStateChanged(oldState, newState);
            }
        }

        private CombatState DetermineCombatState()
        {
            // Death overrides all other states
            if (!IsAlive)
                return CombatState.Dead;

            // Status effects determine state
            if (statusEffectManager.IsKnockedDown)
                return CombatState.KnockedDown;

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
                Debug.Log($"{gameObject.name} entered combat with {target.name}");
            }
        }

        public void ExitCombat()
        {
            // Cannot exit combat during active skill frames
            if (skillSystem.CurrentState == SkillExecutionState.Active)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot exit combat during active skill frames");
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
                Debug.Log($"{gameObject.name} exited combat");
            }
        }

        public void SetTarget(Transform target)
        {
            currentTarget = target;
            OnTargetChanged?.Invoke(target);

            if (enableDebugLogs && target != null)
            {
                Debug.Log($"{gameObject.name} now targeting {target.name}");
            }
        }

        private void CycleTarget()
        {
            var potentialTargets = FindPotentialTargets();

            if (potentialTargets.Length == 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} no targets available");
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
            var targets = new List<Transform>();

            foreach (var collider in colliders)
            {
                if (collider.transform != transform && collider.GetComponent<CombatController>() != null)
                {
                    var targetHealth = collider.GetComponent<HealthSystem>();
                    if (targetHealth != null && targetHealth.IsAlive)
                    {
                        targets.Add(collider.transform);
                    }
                }
            }

            return targets.OrderBy(t => Vector3.Distance(transform.position, t.position)).ToArray();
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

        // ICombatant implementation
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
                Debug.LogError($"{gameObject.name} CombatController requires HealthSystem component");

            if (weaponController == null)
                Debug.LogError($"{gameObject.name} CombatController requires WeaponController component");

            if (skillSystem == null)
                Debug.LogError($"{gameObject.name} CombatController requires SkillSystem component");

            if (statusEffectManager == null)
                Debug.LogError($"{gameObject.name} CombatController requires StatusEffectManager component");

            if (staminaSystem == null)
                Debug.LogError($"{gameObject.name} CombatController requires StaminaSystem component");
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