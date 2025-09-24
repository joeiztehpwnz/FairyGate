using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FairyGate.Combat
{
    public class SkillSystem : MonoBehaviour, ISkillExecutor
    {
        [Header("Skill Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private SkillExecutionState currentState = SkillExecutionState.Uncharged;
        [SerializeField] private SkillType currentSkill = SkillType.Attack;
        [SerializeField] private float chargeProgress = 0f;

        [Header("Input Keys")]
        [SerializeField] private KeyCode attackKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode defenseKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode counterKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode smashKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode windmillKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode cancelKey = KeyCode.Space;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showSkillGUI = true;

        [Header("Events")]
        public UnityEvent<SkillType> OnSkillCharged = new UnityEvent<SkillType>();
        public UnityEvent<SkillType, bool> OnSkillExecuted = new UnityEvent<SkillType, bool>();
        public UnityEvent<SkillType> OnSkillCancelled = new UnityEvent<SkillType>();

        // Component references
        private WeaponController weaponController;
        private StaminaSystem staminaSystem;
        private MovementController movementController;
        private CombatController combatController;
        private StatusEffectManager statusEffectManager;

        // Skill timing
        private Coroutine currentSkillCoroutine;
        private bool canAct = true;

        // Properties
        public SkillExecutionState CurrentState => currentState;
        public SkillType CurrentSkill => currentSkill;
        public float ChargeProgress => chargeProgress;

        private void Awake()
        {
            weaponController = GetComponent<WeaponController>();
            staminaSystem = GetComponent<StaminaSystem>();
            movementController = GetComponent<MovementController>();
            combatController = GetComponent<CombatController>();
            statusEffectManager = GetComponent<StatusEffectManager>();

            if (characterStats == null)
            {
                Debug.LogWarning($"SkillSystem on {gameObject.name} has no CharacterStats assigned. Using default values.");
                characterStats = CharacterStats.CreateDefaultStats();
            }
        }

        private void Update()
        {
            if (!canAct) return;

            HandleSkillInput();
        }

        private void HandleSkillInput()
        {
            // Cancel skill input
            if (Input.GetKeyDown(cancelKey))
            {
                CancelSkill();
                return;
            }

            // Skill execution input (if skill is charged)
            if (currentState == SkillExecutionState.Charged)
            {
                if (Input.GetKeyDown(GetSkillKey(currentSkill)))
                {
                    ExecuteSkill(currentSkill);
                    return;
                }
            }

            // Skill charging input (if not currently busy)
            if (currentState == SkillExecutionState.Uncharged || currentState == SkillExecutionState.Charged)
            {
                SkillType? inputSkill = GetSkillFromInput();
                if (inputSkill.HasValue)
                {
                    if (currentSkill != inputSkill.Value || currentState == SkillExecutionState.Uncharged)
                    {
                        StartCharging(inputSkill.Value);
                    }
                }
            }
        }

        public bool CanChargeSkill(SkillType skillType)
        {
            if (!combatController.IsInCombat) return false;
            if (!canAct) return false;
            if (currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Charged) return false;

            // Check stamina requirements
            int requiredStamina = GetSkillStaminaCost(skillType);
            return staminaSystem.HasStaminaFor(requiredStamina);
        }

        public bool CanExecuteSkill(SkillType skillType)
        {
            return currentSkill == skillType && currentState == SkillExecutionState.Charged;
        }

        public void StartCharging(SkillType skillType)
        {
            if (!CanChargeSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot charge skill {skillType} (not in combat or insufficient stamina)");
                }
                return;
            }

            // Cancel current skill if switching
            if (currentState != SkillExecutionState.Uncharged)
            {
                CancelSkill();
            }

            currentSkill = skillType;
            currentState = SkillExecutionState.Charging;
            chargeProgress = 0f;

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} started charging {skillType}");
            }

            // Apply movement restrictions
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            // Start charging coroutine
            currentSkillCoroutine = StartCoroutine(ChargeSkill(skillType));
        }

        public void ExecuteSkill(SkillType skillType)
        {
            if (!CanExecuteSkill(skillType))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot execute skill {skillType} (not charged or wrong skill)");
                }
                return;
            }

            // Consume stamina
            int staminaCost = GetSkillStaminaCost(skillType);
            if (!staminaSystem.ConsumeStamina(staminaCost))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} insufficient stamina to execute {skillType}");
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} executing {skillType}");
            }

            // Start execution coroutine
            currentSkillCoroutine = StartCoroutine(ExecuteSkillCoroutine(skillType));
        }

        public void CancelSkill()
        {
            if (currentState == SkillExecutionState.Uncharged) return;

            SkillType skilltToCancel = currentSkill;

            // Cannot cancel during active frames
            if (currentState == SkillExecutionState.Active)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} cannot cancel {skilltToCancel} during active frames");
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} cancelled {skilltToCancel}");
            }

            // Stop current coroutine
            if (currentSkillCoroutine != null)
            {
                StopCoroutine(currentSkillCoroutine);
                currentSkillCoroutine = null;
            }

            // Reset skill state
            currentState = SkillExecutionState.Uncharged;
            chargeProgress = 0f;

            // Reset movement restrictions
            movementController.SetMovementModifier(1f);

            OnSkillCancelled.Invoke(skilltToCancel);
        }

        private IEnumerator ChargeSkill(SkillType skillType)
        {
            float chargeTime = CalculateChargeTime(skillType);
            float elapsed = 0f;

            while (elapsed < chargeTime)
            {
                elapsed += Time.deltaTime;
                chargeProgress = elapsed / chargeTime;

                // Check for interruption
                if (statusEffectManager.IsKnockedDown)
                {
                    // Skill charging is paused during knockdown, but not reset
                    yield return new WaitUntil(() => !statusEffectManager.IsKnockedDown);
                }

                yield return null;
            }

            // Skill fully charged
            currentState = SkillExecutionState.Charged;
            chargeProgress = 1f;

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} {skillType} fully charged and ready to execute");
            }

            OnSkillCharged.Invoke(skillType);
        }

        private IEnumerator ExecuteSkillCoroutine(SkillType skillType)
        {
            // Startup phase
            currentState = SkillExecutionState.Startup;
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            float startupTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Startup);
            yield return new WaitForSeconds(startupTime);

            // Active phase (uncancellable)
            currentState = SkillExecutionState.Active;
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            // Process skill effect during active phase
            bool skillSuccessful = ProcessSkillExecution(skillType);

            float activeTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Active);

            // Handle waiting state for defensive skills
            if (IsDefensiveSkill(skillType))
            {
                currentState = SkillExecutionState.Waiting;
                movementController.ApplySkillMovementRestriction(skillType, currentState);

                // Defensive skills enter waiting state
                yield return StartCoroutine(HandleDefensiveWaitingState(skillType));
            }
            else
            {
                // Offensive skills have fixed active time
                yield return new WaitForSeconds(activeTime);
            }

            // Recovery phase
            currentState = SkillExecutionState.Recovery;
            movementController.ApplySkillMovementRestriction(skillType, currentState);

            float recoveryTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Recovery);
            yield return new WaitForSeconds(recoveryTime);

            // Skill complete
            currentState = SkillExecutionState.Uncharged;
            currentSkill = SkillType.Attack; // Reset to default
            chargeProgress = 0f;

            // Reset movement restrictions
            movementController.SetMovementModifier(1f);

            OnSkillExecuted.Invoke(skillType, skillSuccessful);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} {skillType} execution complete (success: {skillSuccessful})");
            }
        }

        private IEnumerator HandleDefensiveWaitingState(SkillType skillType)
        {
            // Defensive skills wait for incoming attacks or manual cancellation
            while (currentState == SkillExecutionState.Waiting)
            {
                // Apply stamina drain during waiting
                float drainRate = skillType == SkillType.Defense ?
                    CombatConstants.DEFENSE_STAMINA_DRAIN :
                    CombatConstants.COUNTER_STAMINA_DRAIN;

                staminaSystem.DrainStamina(drainRate, Time.deltaTime);

                yield return null;
            }
        }

        private bool ProcessSkillExecution(SkillType skillType)
        {
            // Check range for offensive skills
            if (SpeedResolver.IsOffensiveSkill(skillType))
            {
                if (combatController.CurrentTarget == null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} {skillType} failed: no target");
                    }
                    return false;
                }

                if (!weaponController.CheckRangeForSkill(combatController.CurrentTarget, skillType))
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} {skillType} failed: target out of range");
                    }
                    return false;
                }
            }

            // Trigger combat interaction system
            CombatInteractionManager.Instance?.ProcessSkillExecution(this, skillType);

            return true;
        }

        private float CalculateChargeTime(SkillType skillType)
        {
            float baseChargeTime = CombatConstants.BASE_SKILL_CHARGE_TIME;
            float modifiedTime = baseChargeTime / (1 + characterStats.dexterity / 10f);
            return modifiedTime;
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

        private SkillType? GetSkillFromInput()
        {
            if (Input.GetKeyDown(attackKey)) return SkillType.Attack;
            if (Input.GetKeyDown(defenseKey)) return SkillType.Defense;
            if (Input.GetKeyDown(counterKey)) return SkillType.Counter;
            if (Input.GetKeyDown(smashKey)) return SkillType.Smash;
            if (Input.GetKeyDown(windmillKey)) return SkillType.Windmill;
            return null;
        }

        private KeyCode GetSkillKey(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.Attack => attackKey,
                SkillType.Defense => defenseKey,
                SkillType.Counter => counterKey,
                SkillType.Smash => smashKey,
                SkillType.Windmill => windmillKey,
                _ => KeyCode.None
            };
        }

        private bool IsDefensiveSkill(SkillType skillType)
        {
            return skillType == SkillType.Defense || skillType == SkillType.Counter;
        }

        public void SetCanAct(bool canActValue)
        {
            canAct = canActValue;

            if (!canAct && currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Active)
            {
                // Cancel non-active skills when unable to act
                CancelSkill();
            }
        }

        public void ForceTransitionToRecovery()
        {
            if (currentState == SkillExecutionState.Waiting)
            {
                currentState = SkillExecutionState.Recovery;
                movementController.ApplySkillMovementRestriction(currentSkill, currentState);
            }
        }

        // GUI Debug Display
        private void OnGUI()
        {
            if (showSkillGUI && Application.isPlaying)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
                screenPos.y = Screen.height - screenPos.y;

                string skillText = $"Skill: {currentSkill}\nState: {currentState}\nCharge: {chargeProgress:F1}";
                GUI.Label(new Rect(screenPos.x - 50, screenPos.y, 100, 60), skillText);
            }
        }
    }
}