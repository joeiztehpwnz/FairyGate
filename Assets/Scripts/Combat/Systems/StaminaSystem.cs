using System;
using UnityEngine;

namespace FairyGate.Combat
{
    public class StaminaSystem : MonoBehaviour, ICombatUpdatable
    {
        [Header("Stamina Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private int currentStamina;
        [SerializeField] private bool isResting = false;

        // Float accumulator for precise drain calculations
        private float staminaAccumulator;

        [Header("Auto-Cancel Settings")]
        [SerializeField] private float gracePeriod = CombatConstants.AUTO_CANCEL_GRACE_PERIOD;
        private float graceTimer = 0f;
        private bool isInGracePeriod = false;

        // C# Events (replaces UnityEvents for performance)
        public event Action<int, int> OnStaminaChanged; // current, max
        public event Action OnRestStarted;
        public event Action OnRestStopped;
        public event Action OnStaminaDepleted;
        public event Action<SkillType> OnSkillAutoCancel;

        private CombatController combatController;
        private SkillSystem skillSystem;
        private EquipmentManager equipmentManager;

        public int CurrentStamina => currentStamina;
        public int MaxStamina
        {
            get
            {
                int baseStamina = characterStats?.MaxStamina ?? CombatConstants.BASE_STAMINA;
                int bonus = 0;

                if (equipmentManager != null)
                {
                    if (equipmentManager.CurrentArmor != null)
                        bonus += equipmentManager.CurrentArmor.maxStaminaBonus;
                    if (equipmentManager.CurrentAccessory != null)
                        bonus += equipmentManager.CurrentAccessory.maxStaminaBonus;
                }

                return baseStamina + bonus;
            }
        }
        public bool IsResting => isResting;
        public float StaminaPercentage => (float)currentStamina / MaxStamina;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            skillSystem = GetComponent<SkillSystem>();
            equipmentManager = GetComponent<EquipmentManager>();

            if (characterStats == null)
            {
                CombatLogger.LogCombat($"StaminaSystem on {gameObject.name} has no CharacterStats assigned. Using default values.", CombatLogger.LogLevel.Warning);
                characterStats = CharacterStats.CreateDefaultStats();
            }

            currentStamina = MaxStamina;
            staminaAccumulator = currentStamina;

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
            if (!isResting)
            {
                // Passive stamina regeneration (0.4/s = 24/min out of combat)
                float regenRate = CombatConstants.PASSIVE_STAMINA_REGEN;

                // Reduce/disable regen when in combat (punishing stamina management)
                if (combatController != null && combatController.IsInCombat)
                {
                    regenRate *= CombatConstants.COMBAT_STAMINA_REGEN_MULTIPLIER;
                }

                if (regenRate > 0)
                {
                    RegenerateStamina(regenRate * deltaTime);
                }
            }
            else
            {
                // Active rest regeneration (much faster: 25/s)
                RegenerateStamina(CombatConstants.REST_STAMINA_REGENERATION_RATE * deltaTime);
            }

            HandleGracePeriod();
            CheckForAutoCancel();
        }

        public bool HasStaminaFor(int cost)
        {
            return currentStamina >= cost;
        }

        public bool ConsumeStamina(int amount)
        {
            Debug.Log($"[StaminaSystem] {gameObject.name} ConsumeStamina({amount}) - current={currentStamina}, max={MaxStamina}");

            if (currentStamina >= amount)
            {
                currentStamina -= amount;
                staminaAccumulator = currentStamina; // Keep accumulator in sync
                currentStamina = Mathf.Max(0, currentStamina);
                OnStaminaChanged?.Invoke(currentStamina, MaxStamina);

                Debug.Log($"[StaminaSystem] {gameObject.name} Stamina consumed: now {currentStamina}/{MaxStamina}");

                if (currentStamina == 0)
                {
                    OnStaminaDepleted?.Invoke();
                }

                return true;
            }
            Debug.Log($"[StaminaSystem] {gameObject.name} Insufficient stamina!");
            return false;
        }

        public void RegenerateStamina(float amount)
        {
            int oldStamina = currentStamina;
            staminaAccumulator = Mathf.Min(MaxStamina, staminaAccumulator + amount);
            currentStamina = Mathf.FloorToInt(staminaAccumulator);

            if (currentStamina != oldStamina)
            {
                OnStaminaChanged?.Invoke(currentStamina, MaxStamina);
            }
        }

        public void DrainStamina(float drainRate, float deltaTime)
        {
            if (drainRate <= 0) return;

            float modifiedDrainRate = DamageCalculator.CalculateStaminaDrainRate(drainRate, characterStats);
            float drainAmount = modifiedDrainRate * deltaTime;

            // Use float accumulator for precise calculations
            int oldStamina = currentStamina;
            staminaAccumulator = Mathf.Max(0f, staminaAccumulator - drainAmount);
            currentStamina = Mathf.FloorToInt(staminaAccumulator);

            // Debug logging (reduced frequency to avoid spam)
            if (drainRate > 0 && Time.frameCount % 60 == 0) // Log once per second at 60fps
            {
                CombatLogger.LogStamina($"{gameObject.name} stamina drain: base={drainRate:F2}/s, modified={modifiedDrainRate:F2}/s, accumulator={staminaAccumulator:F2}, displayed={currentStamina}");
            }

            if (currentStamina != oldStamina)
            {
                OnStaminaChanged?.Invoke(currentStamina, MaxStamina);

                if (currentStamina == 0)
                {
                    OnStaminaDepleted?.Invoke();
                }
            }
        }

        public void StartResting()
        {
            if (isResting) return;

            isResting = true;
            OnRestStarted?.Invoke();

            // Rest automatically exits combat state
            if (combatController != null && combatController.IsInCombat)
            {
                combatController.ExitCombat();
            }
        }

        public void StopResting()
        {
            if (!isResting) return;

            isResting = false;
            OnRestStopped?.Invoke();
        }

        public void InterruptRest()
        {
            if (isResting)
            {
                StopResting();
                CombatLogger.LogCombat($"{gameObject.name} rest interrupted by taking damage");
            }
        }

        private void HandleGracePeriod()
        {
            if (isInGracePeriod)
            {
                graceTimer -= Time.deltaTime;
                if (graceTimer <= 0f)
                {
                    isInGracePeriod = false;
                }
            }
        }

        private void CheckForAutoCancel()
        {
            if (skillSystem == null || isInGracePeriod) return;

            // PHASE 3 COMPATIBILITY: If using state machine, WaitingState handles its own auto-cancel
            // This prevents dual ownership and race conditions
            // Note: This check only affects Waiting state - other states still use this auto-cancel logic
            bool usingStateMachine = skillSystem.StateMachine != null && skillSystem.StateMachine.CurrentState != null;
            if (usingStateMachine && skillSystem.CurrentState == SkillExecutionState.Waiting)
            {
                // Skip auto-cancel for Waiting state when using state machine
                // WaitingState.Update() handles stamina depletion internally
                return;
            }

            // Check if we need to auto-cancel any waiting state skills
            if (skillSystem.CurrentState == SkillExecutionState.Waiting)
            {
                SkillType currentSkill = skillSystem.CurrentSkill;
                int requiredStamina = GetSkillStaminaCost(currentSkill);

                if (currentStamina < requiredStamina)
                {
                    // Start grace period before auto-canceling
                    if (!isInGracePeriod)
                    {
                        isInGracePeriod = true;
                        graceTimer = gracePeriod;
                    }
                    else if (graceTimer <= 0f)
                    {
                        // Grace period expired, auto-cancel skill
                        skillSystem.CancelSkill();
                        OnSkillAutoCancel?.Invoke(currentSkill);
                        CombatLogger.LogCombat($"{gameObject.name} auto-cancelled {currentSkill} due to stamina depletion");
                    }
                }
            }
        }

        private int GetSkillStaminaCost(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.Attack => CombatConstants.ATTACK_STAMINA_COST,
                SkillType.Defense => CombatConstants.DEFENSE_STAMINA_COST,
                SkillType.Counter => CombatConstants.COUNTER_STAMINA_COST,
                SkillType.Smash => CombatConstants.SMASH_STAMINA_COST,
                SkillType.Windmill => CombatConstants.WINDMILL_STAMINA_COST,
                _ => 0
            };
        }

        public void SetStamina(int value)
        {
            currentStamina = Mathf.Clamp(value, 0, MaxStamina);
            staminaAccumulator = currentStamina;
            OnStaminaChanged.Invoke(currentStamina, MaxStamina);
        }

        public void RestoreToFull()
        {
            SetStamina(MaxStamina);
        }

        private void OnValidate()
        {
            if (characterStats != null)
            {
                currentStamina = Mathf.Clamp(currentStamina, 0, MaxStamina);
                staminaAccumulator = currentStamina;
            }
        }
    }
}