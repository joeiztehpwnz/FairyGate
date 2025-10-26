using System.Collections;
using UnityEngine;

namespace FairyGate.Combat
{
    public abstract class PatternedAI : MonoBehaviour
    {
        [Header("Pattern Configuration")]
        [SerializeField] protected float patternCooldown = 1f;
        [SerializeField] protected bool enablePatternLogs = true;
        [SerializeField] protected bool enablePatternVisualization = true;

        [Header("Combat Integration")]
        [SerializeField] protected float engageDistance = 3.0f;
        [SerializeField] protected float disengageDistance = 6.0f;

        // Component references
        protected CombatController combatController;
        protected SkillSystem skillSystem;
        protected MovementController movementController;
        protected StatusEffectManager statusEffectManager;
        protected Transform player;

        // Pattern state
        protected bool isAlive = true;
        protected bool isInCombat = false;
        protected Coroutine currentPatternCoroutine;
        protected string currentPatternPhase = "Idle";
        protected float currentPhaseProgress = 0f;
        protected float currentPhaseeDuration = 1f;

        // Abstract methods for concrete implementations
        protected abstract IEnumerator ExecutePattern();
        protected abstract string GetPatternName();

        protected virtual void Awake()
        {
            combatController = GetComponent<CombatController>();
            skillSystem = GetComponent<SkillSystem>();
            movementController = GetComponent<MovementController>();
            statusEffectManager = GetComponent<StatusEffectManager>();
        }

        protected virtual void Start()
        {
            FindPlayer();
        }

        protected virtual void Update()
        {
            if (!isAlive) return;

            UpdateCombatState();
            UpdatePatternState();
        }

        protected virtual void FindPlayer()
        {
            // Find the player by looking for objects with CombatController that aren't this AI
            var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var combatant in combatants)
            {
                if (combatant != combatController && combatant.name.Contains("Player"))
                {
                    player = combatant.transform;
                    break;
                }
            }

            // If no explicit player found, find closest combatant
            if (player == null)
            {
                float closestSqrDistance = float.MaxValue;
                foreach (var combatant in combatants)
                {
                    if (combatant != combatController)
                    {
                        // Use squared distance to avoid expensive sqrt operation
                        float sqrDistance = (transform.position - combatant.transform.position).sqrMagnitude;
                        if (sqrDistance < closestSqrDistance)
                        {
                            closestSqrDistance = sqrDistance;
                            player = combatant.transform;
                        }
                    }
                }
            }

            if (enablePatternLogs && player != null)
            {
                Debug.Log($"{gameObject.name} PatternedAI found target: {player.name}");
            }
        }

        protected virtual void UpdateCombatState()
        {
            if (player == null) return;

            // Use squared distance to avoid expensive sqrt operation
            float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
            float sqrEngageDistance = engageDistance * engageDistance;
            float sqrDisengageDistance = disengageDistance * disengageDistance;

            // Enter combat if player is within engage distance
            if (!isInCombat && sqrDistanceToPlayer <= sqrEngageDistance)
            {
                EnterCombat();
            }
            // Exit combat if player is too far
            else if (isInCombat && sqrDistanceToPlayer > sqrDisengageDistance)
            {
                ExitCombat();
            }
        }

        protected virtual void UpdatePatternState()
        {
            // Update phase progress for visualization
            if (isInCombat && currentPatternCoroutine != null)
            {
                // This will be updated by concrete pattern implementations
                currentPhaseProgress = Mathf.Clamp01(currentPhaseProgress);
            }
        }

        protected virtual void EnterCombat()
        {
            isInCombat = true;
            combatController.EnterCombat(player);

            if (enablePatternLogs)
            {
                Debug.Log($"{gameObject.name} PatternedAI entered combat with {player.name} - Starting {GetPatternName()} pattern");
            }

            StartPatternLoop();
        }

        protected virtual void ExitCombat()
        {
            isInCombat = false;
            combatController.ExitCombat();

            if (enablePatternLogs)
            {
                Debug.Log($"{gameObject.name} PatternedAI exited combat - Stopping pattern");
            }

            StopPatternLoop();
        }

        protected virtual void StartPatternLoop()
        {
            if (currentPatternCoroutine != null)
            {
                StopCoroutine(currentPatternCoroutine);
            }

            currentPatternCoroutine = StartCoroutine(PatternLoop());
        }

        protected virtual void StopPatternLoop()
        {
            if (currentPatternCoroutine != null)
            {
                StopCoroutine(currentPatternCoroutine);
                currentPatternCoroutine = null;
            }

            currentPatternPhase = "Idle";
            currentPhaseProgress = 0f;
        }

        protected virtual IEnumerator PatternLoop()
        {
            while (isAlive && isInCombat)
            {
                if (!statusEffectManager.CanAct)
                {
                    // Pause pattern during knockdown, but don't reset
                    currentPatternPhase = "Interrupted";
                    yield return new WaitUntil(() => statusEffectManager.CanAct);
                }

                yield return StartCoroutine(ExecutePattern());

                if (patternCooldown > 0f)
                {
                    currentPatternPhase = "Cooldown";
                    yield return new WaitForSeconds(patternCooldown);
                }
            }
        }

        // Helper methods for concrete pattern implementations
        protected void SetPatternPhase(string phaseName, float duration)
        {
            currentPatternPhase = phaseName;
            currentPhaseeDuration = duration;
            currentPhaseProgress = 0f;

            if (enablePatternLogs)
            {
                Debug.Log($"{gameObject.name} Pattern Phase: {phaseName} ({duration:F1}s)");
            }
        }

        protected IEnumerator WaitForPhaseComplete(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Pause if stunned/knocked down during phase
                if (!statusEffectManager.CanAct)
                {
                    currentPatternPhase = "Interrupted";
                    yield return new WaitUntil(() => statusEffectManager.CanAct);
                }

                elapsed += Time.deltaTime;
                currentPhaseProgress = elapsed / duration;
                yield return null;
            }
            currentPhaseProgress = 1f;
        }

        protected bool IsPlayerInRange()
        {
            if (player == null) return false;
            // Use squared distance to avoid expensive sqrt operation
            float sqrDistance = (transform.position - player.position).sqrMagnitude;
            float sqrEngageDistance = engageDistance * engageDistance;
            return sqrDistance <= sqrEngageDistance;
        }

        protected void MoveTowardsPlayer()
        {
            if (player == null || !movementController.CanMove) return;

            Vector3 direction = (player.position - transform.position).normalized;
            Vector3 moveInput = Vector3.zero;

            if (direction.x > 0.1f) moveInput.x = 1f;
            else if (direction.x < -0.1f) moveInput.x = -1f;

            if (direction.z > 0.1f) moveInput.z = 1f;
            else if (direction.z < -0.1f) moveInput.z = -1f;

            movementController.SetMovementInput(moveInput);
        }

        protected void StopMovement()
        {
            movementController.SetMovementInput(Vector3.zero);
        }

        // Public interface for external systems
        public string GetCurrentPatternPhase() => currentPatternPhase;
        public float GetCurrentPhaseProgress() => currentPhaseProgress;
        public bool IsExecutingPattern() => currentPatternCoroutine != null && isInCombat;

        // GUI Debug Display
        protected virtual void OnGUI()
        {
            if (enablePatternVisualization && Application.isPlaying && IsExecutingPattern())
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4f);
                screenPos.y = Screen.height - screenPos.y;

                string patternText = $"{GetPatternName()}\n{currentPatternPhase}\nProgress: {currentPhaseProgress:F1}";
                GUI.Label(new Rect(screenPos.x - 60, screenPos.y, 120, 60), patternText);
            }
        }

        // Gizmos for debugging
        protected virtual void OnDrawGizmosSelected()
        {
            // Draw engagement ranges
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, engageDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, disengageDistance);

            // Draw line to current target
            if (player != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}