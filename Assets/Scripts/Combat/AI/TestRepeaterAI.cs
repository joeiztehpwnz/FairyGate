using System.Collections;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Test AI that repeats a single selected skill indefinitely for thorough interaction testing.
    /// Extends PatternedAI to maintain compatibility with the combat system architecture.
    /// </summary>
    public class TestRepeaterAI : PatternedAI
    {
        [Header("Test Configuration")]
        [SerializeField] private SkillType selectedSkill = SkillType.Attack;
        [SerializeField] private float repeatDelay = 1.0f;
        [SerializeField] private bool addRandomDelay = false;
        [SerializeField] private float randomDelayMax = 0.5f;

        [Header("Defensive Skill Options")]
        [SerializeField] private bool maintainDefensiveState = true;
        [SerializeField] private float defensiveWaitDuration = 3.0f;

        [Header("Test Mode Features")]
        [SerializeField] private bool infiniteStamina = false;
        [SerializeField] private bool skipRangedAiming = false;

        [Header("Movement Options")]
        [SerializeField] private bool enableMovement = false;
        [SerializeField] private float optimalRange = 2.0f;

        // State tracking
        private float timeUntilNextAction = 0f;

        protected override string GetPatternName() => $"Test: {selectedSkill}";

        protected override IEnumerator ExecutePattern()
        {
            // Special handling for defensive skills with state maintenance
            if (IsDefensiveSkill(selectedSkill) && maintainDefensiveState)
            {
                yield return StartCoroutine(ExecuteDefensivePattern());
            }
            else
            {
                yield return StartCoroutine(ExecuteOffensivePattern());
            }
        }

        private IEnumerator ExecuteOffensivePattern()
        {
            // SPECIAL CASE: Attack has instant execution (no charge time)
            if (selectedSkill == SkillType.Attack)
            {
                yield return StartCoroutine(ExecuteInstantAttack());
                yield break;
            }

            // Phase 1: Charge Skill
            float chargeDuration = GetChargeDuration(selectedSkill);
            SetPatternPhase($"Charging {selectedSkill}", chargeDuration);

            if (skillSystem.CanChargeSkill(selectedSkill))
            {
                skillSystem.StartCharging(selectedSkill);
                if (enablePatternLogs)
                {
                    Debug.Log($"[TestAI] {gameObject.name} charging {selectedSkill}");
                }
            }

            // Handle movement during charge phase
            if (enableMovement && chargeDuration > 0f)
            {
                yield return StartCoroutine(ChargeWithMovement(chargeDuration));
            }
            else
            {
                StopMovement();

                // For RangedAttack, handle aiming state
                if (selectedSkill == SkillType.RangedAttack)
                {
                    yield return StartCoroutine(HandleRangedAttackAiming(chargeDuration));
                }
                else
                {
                    yield return StartCoroutine(WaitForPhaseComplete(chargeDuration));
                }
            }

            // Phase 2: Execute Skill
            float executionDuration = GetExecutionDuration(selectedSkill);
            SetPatternPhase($"Executing {selectedSkill}", executionDuration);

            if (skillSystem.CurrentState == SkillExecutionState.Charged ||
                skillSystem.CurrentState == SkillExecutionState.Aiming)
            {
                // Execute skill - chase during charge should have gotten us in range
                skillSystem.ExecuteSkill(selectedSkill);
                if (enablePatternLogs)
                {
                    Debug.Log($"[TestAI] {gameObject.name} executing {selectedSkill}");
                }
            }

            yield return StartCoroutine(WaitForPhaseComplete(executionDuration));

            // Phase 3: Repeat Delay
            float actualDelay = repeatDelay;
            if (addRandomDelay)
            {
                actualDelay += Random.Range(0f, randomDelayMax);
            }

            SetPatternPhase("Delay", actualDelay);
            timeUntilNextAction = actualDelay;

            // Handle movement during delay phase
            if (enableMovement)
            {
                yield return StartCoroutine(DelayWithMovement(actualDelay));
            }
            else
            {
                yield return StartCoroutine(WaitForPhaseComplete(actualDelay));
            }
        }

        private IEnumerator ExecuteInstantAttack()
        {
            // Attack skill has instant execution (no charge phase)
            SetPatternPhase("Executing Attack", 0.1f);

            // Check basic conditions
            if (!skillSystem.CanExecuteAttack())
            {
                if (enablePatternLogs)
                {
                    Debug.Log($"[TestAI] {gameObject.name} cannot execute Attack (not in combat or insufficient stamina)");
                }
                yield return StartCoroutine(WaitForPhaseComplete(0.5f));
                yield break;
            }

            // Move to get within weapon range if movement enabled
            if (enableMovement)
            {
                float maxChaseTime = 2.0f; // Don't chase forever
                float chaseElapsed = 0f;

                while (!IsWeaponInRange() && chaseElapsed < maxChaseTime)
                {
                    // Pause if stunned/knocked down during chase
                    if (!statusEffectManager.CanAct)
                    {
                        StopMovement();
                        yield return new WaitUntil(() => statusEffectManager.CanAct);
                    }

                    if (player != null && weaponController != null)
                    {
                        // Use squared distance to avoid expensive sqrt operation
                        float sqrDistance = (transform.position - player.position).sqrMagnitude;
                        float weaponRange = weaponController.WeaponData.range;
                        float sqrWeaponRange = weaponRange * weaponRange;

                        if (sqrDistance > sqrWeaponRange)
                        {
                            MoveTowardsPlayer(); // Chase to get in range
                        }
                        else
                        {
                            break; // In range now!
                        }
                    }

                    chaseElapsed += Time.deltaTime;
                    yield return null;
                }

                StopMovement();
            }

            // Execute Attack immediately - chase should have gotten us in range
            skillSystem.ExecuteSkill(SkillType.Attack);
            if (enablePatternLogs)
            {
                Debug.Log($"[TestAI] {gameObject.name} executing Attack");
            }

            // Wait for execution duration
            float executionDuration = GetExecutionDuration(SkillType.Attack);
            yield return StartCoroutine(WaitForPhaseComplete(executionDuration));

            // Phase 2: Repeat Delay
            float actualDelay = repeatDelay;
            if (addRandomDelay)
            {
                actualDelay += Random.Range(0f, randomDelayMax);
            }

            SetPatternPhase("Delay", actualDelay);
            timeUntilNextAction = actualDelay;

            // Handle movement during delay phase
            if (enableMovement)
            {
                yield return StartCoroutine(DelayWithMovement(actualDelay));
            }
            else
            {
                yield return StartCoroutine(WaitForPhaseComplete(actualDelay));
            }
        }

        private IEnumerator ExecuteDefensivePattern()
        {
            // Phase 1: Charge Defensive Skill
            float chargeDuration = 2.0f; // Standard charge time for Defense/Counter
            SetPatternPhase($"Charging {selectedSkill}", chargeDuration);

            if (skillSystem.CanChargeSkill(selectedSkill))
            {
                skillSystem.StartCharging(selectedSkill);
                if (enablePatternLogs)
                {
                    Debug.Log($"[TestAI] {gameObject.name} charging {selectedSkill}");
                }
            }

            StopMovement();
            yield return StartCoroutine(WaitForPhaseComplete(chargeDuration));

            // Phase 2: Execute and Wait for Interactions
            SetPatternPhase($"Waiting {selectedSkill}", defensiveWaitDuration);

            if (skillSystem.CurrentState == SkillExecutionState.Charged)
            {
                skillSystem.ExecuteSkill(selectedSkill);
                if (enablePatternLogs)
                {
                    Debug.Log($"[TestAI] {gameObject.name} executing {selectedSkill} - waiting for attacks");
                }
            }

            StopMovement();

            // Wait for defensive duration or until skill completes
            float elapsed = 0f;
            while (elapsed < defensiveWaitDuration)
            {
                // Check if defensive skill was completed by an interaction
                if (skillSystem.CurrentState != SkillExecutionState.Waiting ||
                    skillSystem.CurrentSkill != selectedSkill)
                {
                    if (enablePatternLogs)
                    {
                        Debug.Log($"[TestAI] {gameObject.name} {selectedSkill} completed by interaction");
                    }
                    break;
                }

                elapsed += Time.deltaTime;
                currentPhaseProgress = elapsed / defensiveWaitDuration;
                yield return null;
            }

            // Phase 3: Cancel if still active
            if (skillSystem.CurrentState == SkillExecutionState.Waiting)
            {
                SetPatternPhase("Cancelling", 0.5f);
                skillSystem.CancelSkill();
                if (enablePatternLogs)
                {
                    Debug.Log($"[TestAI] {gameObject.name} cancelling {selectedSkill} - no interaction occurred");
                }
                yield return StartCoroutine(WaitForPhaseComplete(0.5f));
            }

            // Phase 4: Brief delay before re-entering defensive state
            float actualDelay = repeatDelay;
            if (addRandomDelay)
            {
                actualDelay += Random.Range(0f, randomDelayMax);
            }

            SetPatternPhase("Delay", actualDelay);
            timeUntilNextAction = actualDelay;
            yield return StartCoroutine(WaitForPhaseComplete(actualDelay));
        }

        private IEnumerator HandleRangedAttackAiming(float aimDuration)
        {
            // Wait for aiming state to be entered
            yield return new WaitUntil(() => skillSystem.CurrentState == SkillExecutionState.Aiming);

            if (skipRangedAiming)
            {
                // Dev mode: Max out accuracy immediately
                var accuracySystem = GetComponent<AccuracySystem>();
                if (accuracySystem != null)
                {
                    // Wait a tiny bit for system to initialize
                    yield return new WaitForSeconds(0.1f);

                    if (enablePatternLogs)
                    {
                        Debug.Log($"[TestAI] {gameObject.name} skipping aim time (dev mode)");
                    }

                    // Hold for remaining duration at max accuracy
                    yield return StartCoroutine(WaitForPhaseComplete(aimDuration - 0.1f));
                }
            }
            else
            {
                // Normal aiming - wait for full duration
                yield return StartCoroutine(WaitForPhaseComplete(aimDuration));
            }
        }

        private IEnumerator ChargeWithMovement(float chargeDuration)
        {
            float elapsed = 0f;
            while (elapsed < chargeDuration)
            {
                // Pause if stunned/knocked down during phase
                if (!statusEffectManager.CanAct)
                {
                    StopMovement();
                    currentPatternPhase = "Interrupted";
                    yield return new WaitUntil(() => statusEffectManager.CanAct);
                }

                // Move to get within weapon range
                if (player != null && weaponController != null)
                {
                    // Use squared distance to avoid expensive sqrt operation
                    float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
                    float weaponRange = weaponController.WeaponData.range;
                    float sqrWeaponRange = weaponRange * weaponRange;
                    float tooCloseThreshold = 0.8f; // Don't get closer than this
                    float sqrTooClose = tooCloseThreshold * tooCloseThreshold;

                    if (sqrDistanceToPlayer > sqrWeaponRange)
                    {
                        MoveTowardsPlayer(); // Too far, close the gap
                    }
                    else if (sqrDistanceToPlayer < sqrTooClose)
                    {
                        StopMovement(); // Too close, back off
                    }
                    else
                    {
                        StopMovement(); // Within weapon range, stay put
                    }
                }

                elapsed += Time.deltaTime;
                currentPhaseProgress = elapsed / chargeDuration;
                yield return null;
            }
            currentPhaseProgress = 1f;
            StopMovement();
        }

        private IEnumerator DelayWithMovement(float delayDuration)
        {
            float elapsed = 0f;
            while (elapsed < delayDuration)
            {
                // Pause if stunned/knocked down during phase
                if (!statusEffectManager.CanAct)
                {
                    StopMovement();
                    currentPatternPhase = "Interrupted";
                    yield return new WaitUntil(() => statusEffectManager.CanAct);
                }

                // Move to get within weapon range
                if (player != null && weaponController != null)
                {
                    // Use squared distance to avoid expensive sqrt operation
                    float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
                    float weaponRange = weaponController.WeaponData.range;
                    float sqrWeaponRange = weaponRange * weaponRange;
                    float tooCloseThreshold = 0.8f; // Don't get closer than this
                    float sqrTooClose = tooCloseThreshold * tooCloseThreshold;

                    if (sqrDistanceToPlayer > sqrWeaponRange)
                    {
                        MoveTowardsPlayer(); // Too far, close the gap
                    }
                    else if (sqrDistanceToPlayer < sqrTooClose)
                    {
                        StopMovement(); // Too close, back off
                    }
                    else
                    {
                        StopMovement(); // Within weapon range, stay put
                    }
                }

                elapsed += Time.deltaTime;
                currentPhaseProgress = elapsed / delayDuration;
                yield return null;
            }
            currentPhaseProgress = 1f;
            StopMovement();
        }

        private float GetChargeDuration(SkillType skill)
        {
            return skill switch
            {
                SkillType.Attack => 0f,        // No charge time
                SkillType.Defense => 2f,       // Standard charge
                SkillType.Counter => 2f,       // Standard charge
                SkillType.Smash => 2f,         // Standard charge
                SkillType.Windmill => 2f,      // Standard charge
                SkillType.RangedAttack => 2f,  // Aiming time
                _ => 2f
            };
        }

        private float GetExecutionDuration(SkillType skill)
        {
            return skill switch
            {
                SkillType.Attack => 1.0f,
                SkillType.Smash => 1.5f,
                SkillType.Windmill => 2.0f,
                SkillType.RangedAttack => 0.5f,
                _ => 1.0f
            };
        }

        private bool IsDefensiveSkill(SkillType skill)
        {
            return skill == SkillType.Defense || skill == SkillType.Counter;
        }

        // Override Update to handle infinite stamina cheat
        protected override void Update()
        {
            base.Update();

            if (infiniteStamina && Application.isPlaying)
            {
                var staminaSystem = GetComponent<StaminaSystem>();
                if (staminaSystem != null && staminaSystem.CurrentStamina < staminaSystem.MaxStamina * 0.9f)
                {
                    // Refill stamina when it drops below 90%
                    staminaSystem.SetStamina(staminaSystem.MaxStamina);
                }
            }

            // Update action timer
            if (timeUntilNextAction > 0f)
            {
                timeUntilNextAction -= Time.deltaTime;
            }
        }

        // Public interface for runtime configuration
        public void SetSelectedSkill(SkillType skill)
        {
            selectedSkill = skill;

            if (enablePatternLogs)
            {
                Debug.Log($"[TestAI] {gameObject.name} skill changed to {skill}");
            }

            // Restart pattern if currently executing
            if (IsExecutingPattern())
            {
                StopPatternLoop();
                StartPatternLoop();
            }
        }

        public void SetRepeatDelay(float delay)
        {
            repeatDelay = Mathf.Max(0.1f, delay);
        }

        public void SetAddRandomDelay(bool enabled)
        {
            addRandomDelay = enabled;
        }

        public void SetMaintainDefensiveState(bool enabled)
        {
            maintainDefensiveState = enabled;
        }

        public void SetInfiniteStamina(bool enabled)
        {
            infiniteStamina = enabled;
        }

        public void SetSkipRangedAiming(bool enabled)
        {
            skipRangedAiming = enabled;
        }

        public void SetEnableMovement(bool enabled)
        {
            enableMovement = enabled;
        }

        public void SetOptimalRange(float range)
        {
            optimalRange = Mathf.Max(0.5f, range);
        }

        // Getters for UI display
        public SkillType SelectedSkill => selectedSkill;
        public float RepeatDelay => repeatDelay;
        public bool AddRandomDelay => addRandomDelay;
        public bool MaintainDefensiveState => maintainDefensiveState;
        public bool InfiniteStamina => infiniteStamina;
        public bool SkipRangedAiming => skipRangedAiming;
        public bool EnableMovement => enableMovement;
        public float OptimalRange => optimalRange;
        public float TimeUntilNextAction => timeUntilNextAction;

        // Enhanced visualization for test mode
        protected override void OnGUI()
        {
            if (enablePatternVisualization && Application.isPlaying && IsExecutingPattern())
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4f);
                screenPos.y = Screen.height - screenPos.y;

                // Color code based on skill type
                string skillColor = selectedSkill switch
                {
                    SkillType.Attack => "<color=white>",
                    SkillType.Defense => "<color=blue>",
                    SkillType.Counter => "<color=cyan>",
                    SkillType.Smash => "<color=orange>",
                    SkillType.Windmill => "<color=purple>",
                    SkillType.RangedAttack => "<color=yellow>",
                    _ => "<color=grey>"
                };

                string patternText = $"🧪 TEST MODE\n{skillColor}{selectedSkill}</color>\n{currentPatternPhase}\n⏱️ {currentPhaseProgress:F1}";

                if (timeUntilNextAction > 0f)
                {
                    patternText += $"\nNext: {timeUntilNextAction:F1}s";
                }

                GUI.Label(new Rect(screenPos.x - 70, screenPos.y, 140, 80), patternText);

                // Show test mode indicator
                GUI.Label(new Rect(screenPos.x - 60, screenPos.y + 85, 120, 20),
                    "<color=yellow>🧪 TEST AI</color>");
            }
        }

        // Enhanced gizmos
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                string statusText = $"Test AI - {selectedSkill}\n" +
                                  $"Phase: {currentPatternPhase}\n" +
                                  $"Progress: {currentPhaseProgress:F1}\n" +
                                  $"Next: {timeUntilNextAction:F1}s";

                if (maintainDefensiveState && IsDefensiveSkill(selectedSkill))
                {
                    statusText += "\n[Maintain Defensive]";
                }

                if (infiniteStamina)
                {
                    statusText += "\n[∞ Stamina]";
                }

                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 3f,
                    statusText
                );
            }
            #endif
        }
    }
}
