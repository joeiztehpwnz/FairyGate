using System.Collections;
using UnityEngine;

namespace FairyGate.Combat
{
    public class KnightAI : PatternedAI
    {
        [Header("Knight Pattern Timing")]
        [SerializeField] private float chargeDefenseDuration = 1f;
        [SerializeField] private float waitDefensiveDuration = 3f;
        [SerializeField] private float cancelDefenseDuration = 0.5f;
        [SerializeField] private float chargeSmashDuration = 1.5f;
        [SerializeField] private float executeSmashDuration = 0.5f;
        [SerializeField] private float recoveryDuration = 1.5f;

        [Header("Knight Behavior")]
        [SerializeField] private float optimalRange = 2.0f;
        [SerializeField] private bool approachDuringRecovery = true;

        protected override string GetPatternName() => "Knight Defensive";

        protected override IEnumerator ExecutePattern()
        {
            // 8-second Knight Pattern Cycle:
            // 1. Charge Defense (1s) → Telegraph: "Raising shield"
            // 2. Wait Defensively (3s) → Vulnerable to positioning
            // 3. Cancel Defense (0.5s) → Telegraph: "Lowering shield"
            // 4. Charge Smash (1.5s) → Telegraph: "Winding up attack"
            // 5. Execute Smash (0.5s) → Danger window
            // 6. Recovery (1.5s) → Vulnerable window for counterattack

            // Phase 1: Charge Defense (1s)
            SetPatternPhase("Charge Defense", chargeDefenseDuration);
            if (skillSystem.CanChargeSkill(SkillType.Defense))
            {
                skillSystem.StartCharging(SkillType.Defense);
                if (enablePatternLogs)
                {
                    Debug.Log($"{gameObject.name} Knight raising shield (Defense charging)");
                }
            }
            StopMovement(); // Stand still while charging
            yield return StartCoroutine(WaitForPhaseComplete(chargeDefenseDuration));

            // Phase 2: Wait Defensively (3s)
            SetPatternPhase("Wait Defensively", waitDefensiveDuration);
            if (skillSystem.CurrentState == SkillExecutionState.Charged && skillSystem.CurrentSkill == SkillType.Defense)
            {
                skillSystem.ExecuteSkill(SkillType.Defense);
                if (enablePatternLogs)
                {
                    Debug.Log($"{gameObject.name} Knight executing Defense - waiting for attacks");
                }
            }
            StopMovement(); // Stay defensive
            yield return StartCoroutine(WaitForPhaseComplete(waitDefensiveDuration));

            // Phase 3: Cancel Defense (0.5s)
            SetPatternPhase("Cancel Defense", cancelDefenseDuration);
            if (skillSystem.CurrentState == SkillExecutionState.Waiting)
            {
                skillSystem.CancelSkill();
                if (enablePatternLogs)
                {
                    Debug.Log($"{gameObject.name} Knight lowering shield (Defense cancelled)");
                }
            }
            yield return StartCoroutine(WaitForPhaseComplete(cancelDefenseDuration));

            // Phase 4: Charge Smash (1.5s)
            SetPatternPhase("Charge Smash", chargeSmashDuration);
            if (skillSystem.CanChargeSkill(SkillType.Smash))
            {
                skillSystem.StartCharging(SkillType.Smash);
                if (enablePatternLogs)
                {
                    Debug.Log($"{gameObject.name} Knight winding up heavy attack (Smash charging)");
                }
            }
            // Move toward player during charge if out of range
            float chargeElapsed = 0f;
            while (chargeElapsed < chargeSmashDuration)
            {
                // Use squared distance to avoid expensive sqrt operation
                float sqrDistance = (transform.position - player.position).sqrMagnitude;
                float sqrOptimalFar = (optimalRange + CombatConstants.AI_OPTIMAL_RANGE_BUFFER_NEAR) * (optimalRange + CombatConstants.AI_OPTIMAL_RANGE_BUFFER_NEAR);

                if (IsPlayerInRange() && sqrDistance > sqrOptimalFar)
                {
                    MoveTowardsPlayer();
                }
                else
                {
                    StopMovement();
                }

                chargeElapsed += Time.deltaTime;
                currentPhaseProgress = chargeElapsed / chargeSmashDuration;
                yield return null;
            }

            // Phase 5: Execute Smash (0.5s)
            SetPatternPhase("Execute Smash", executeSmashDuration);
            StopMovement(); // Stop for execution
            if (skillSystem.CurrentState == SkillExecutionState.Charged && skillSystem.CurrentSkill == SkillType.Smash)
            {
                // Check if player is in range before executing
                if (IsPlayerInRange())
                {
                    skillSystem.ExecuteSkill(SkillType.Smash);
                    if (enablePatternLogs)
                    {
                        Debug.Log($"{gameObject.name} Knight executing Smash attack");
                    }
                }
                else
                {
                    // Cancel if out of range
                    skillSystem.CancelSkill();
                    if (enablePatternLogs)
                    {
                        Debug.Log($"{gameObject.name} Knight cancelling Smash - target out of range");
                    }
                }
            }
            yield return StartCoroutine(WaitForPhaseComplete(executeSmashDuration));

            // Phase 6: Recovery (1.5s) - Vulnerable window
            SetPatternPhase("Recovery", recoveryDuration);
            if (enablePatternLogs)
            {
                Debug.Log($"{gameObject.name} Knight recovering - vulnerable window");
            }

            // Optional movement during recovery
            if (approachDuringRecovery)
            {
                float recoveryElapsed = 0f;
                while (recoveryElapsed < recoveryDuration)
                {
                    // Slowly approach if too far, but don't be aggressive
                    // Use squared distance to avoid expensive sqrt operation
                    float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
                    float sqrOptimalFar = (optimalRange + CombatConstants.AI_OPTIMAL_RANGE_BUFFER_FAR) * (optimalRange + CombatConstants.AI_OPTIMAL_RANGE_BUFFER_FAR);

                    if (sqrDistanceToPlayer > sqrOptimalFar)
                    {
                        MoveTowardsPlayer();
                    }
                    else
                    {
                        StopMovement();
                    }

                    recoveryElapsed += Time.deltaTime;
                    currentPhaseProgress = recoveryElapsed / recoveryDuration;
                    yield return null;
                }
            }
            else
            {
                StopMovement();
                yield return StartCoroutine(WaitForPhaseComplete(recoveryDuration));
            }

            if (enablePatternLogs)
            {
                Debug.Log($"{gameObject.name} Knight pattern cycle complete - total duration: 8.0s");
            }
        }

        // Override visualization to show Knight-specific info
        protected override void OnGUI()
        {
            if (enablePatternVisualization && Application.isPlaying && IsExecutingPattern())
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4f);
                screenPos.y = Screen.height - screenPos.y;

                // Color-code phases for better visibility
                string phaseColor = currentPatternPhase switch
                {
                    "Charge Defense" => "<color=blue>",
                    "Wait Defensively" => "<color=cyan>",
                    "Cancel Defense" => "<color=yellow>",
                    "Charge Smash" => "<color=orange>",
                    "Execute Smash" => "<color=red>",
                    "Recovery" => "<color=green>",
                    _ => "<color=white>"
                };

                string patternText = $"🛡️ {GetPatternName()}\n{phaseColor}{currentPatternPhase}</color>\n⏱️ {currentPhaseProgress:F1}";

                GUI.Label(new Rect(screenPos.x - 70, screenPos.y, 140, 60), patternText);

                // Show vulnerability indicator during recovery
                if (currentPatternPhase == "Recovery")
                {
                    GUI.Label(new Rect(screenPos.x - 50, screenPos.y + 65, 100, 20),
                        "<color=green>🎯 VULNERABLE</color>");
                }
                // Show danger indicator during smash execution
                else if (currentPatternPhase == "Execute Smash")
                {
                    GUI.Label(new Rect(screenPos.x - 50, screenPos.y + 65, 100, 20),
                        "<color=red>⚠️ DANGER</color>");
                }
            }
        }

        // Enhanced gizmos for Knight AI
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw optimal range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, optimalRange);

            #if UNITY_EDITOR
            if (player != null && Application.isPlaying)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                string statusText = $"Knight AI\nPattern: {currentPatternPhase}\nDistance: {distance:F1}\nPhase: {currentPhaseProgress:F1}";

                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 3f,
                    statusText
                );
            }
            #endif
        }

        // Public method to get total pattern duration (for formation timing)
        public float GetPatternDuration()
        {
            return chargeDefenseDuration + waitDefensiveDuration + cancelDefenseDuration +
                   chargeSmashDuration + executeSmashDuration + recoveryDuration;
        }

        // Public method to check if currently in vulnerable state
        public bool IsVulnerable()
        {
            return currentPatternPhase == "Recovery" || currentPatternPhase == "Charge Defense" || currentPatternPhase == "Charge Smash";
        }

        // Public method to check if currently dangerous
        public bool IsDangerous()
        {
            return currentPatternPhase == "Execute Smash";
        }
    }
}