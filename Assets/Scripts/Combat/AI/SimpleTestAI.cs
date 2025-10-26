using UnityEngine;
using System.Collections;

namespace FairyGate.Combat
{
    public class SimpleTestAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float skillCooldown = 3.0f;
        [SerializeField] private float randomVariance = 2.0f;
        [SerializeField] private float engageDistance = 3.0f;
        [SerializeField] private float optimalRange = 2.0f;
        [SerializeField] private LayerMask playerLayerMask = -1;

        [Header("Skill Selection Weights")]
        [SerializeField] private float attackWeight = 30f;
        [SerializeField] private float defenseWeight = 20f;
        [SerializeField] private float counterWeight = 20f;
        [SerializeField] private float smashWeight = 15f;
        [SerializeField] private float windmillWeight = 15f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private float nextSkillTime;
        private Transform player;
        private CombatController combatController;
        private SkillSystem skillSystem;
        private WeaponController weaponController;
        private MovementController movementController;
        private StatusEffectManager statusEffectManager;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            skillSystem = GetComponent<SkillSystem>();
            weaponController = GetComponent<WeaponController>();
            movementController = GetComponent<MovementController>();
            statusEffectManager = GetComponent<StatusEffectManager>();

            nextSkillTime = Time.time + skillCooldown + Random.Range(-randomVariance, randomVariance);
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (player == null)
            {
                FindPlayer();
                return;
            }

            if (!statusEffectManager.CanAct)
            {
                return; // Cannot act while knocked down
            }

            UpdateAI();
        }

        private void FindPlayer()
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

            if (enableDebugLogs && player != null)
            {
                Debug.Log($"{gameObject.name} AI found target: {player.name}");
            }
        }

        private void UpdateAI()
        {
            // Use squared distance to avoid expensive sqrt operation
            float sqrDistanceToPlayer = (transform.position - player.position).sqrMagnitude;
            float sqrEngageDistance = engageDistance * engageDistance;
            float sqrDisengageDistance = (engageDistance * 2f) * (engageDistance * 2f);

            // Simple movement AI
            UpdateMovement(sqrDistanceToPlayer);

            // Enter combat if player is within engage distance
            if (!combatController.IsInCombat && sqrDistanceToPlayer <= sqrEngageDistance)
            {
                combatController.EnterCombat(player);
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI engaged {player.name} in combat");
                }
            }

            // Exit combat if player is too far
            if (combatController.IsInCombat && sqrDistanceToPlayer > sqrDisengageDistance)
            {
                combatController.ExitCombat();
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI disengaged from combat (too far)");
                }
            }

            // Use skills if in combat
            if (combatController.IsInCombat && Time.time >= nextSkillTime)
            {
                TryUseSkill();
            }
        }

        private void UpdateMovement(float sqrDistanceToPlayer)
        {
            if (!movementController.CanMove)
            {
                // Explicitly stop movement when can't move
                movementController.SetMovementInput(Vector3.zero);
                return;
            }

            Vector3 directionToPlayer = (player.position - transform.position).normalized;

            // Move toward player if too far, away if too close
            // Using squared distance comparisons
            float sqrOptimalFar = (optimalRange + CombatConstants.AI_OPTIMAL_RANGE_BUFFER_NEAR) * (optimalRange + CombatConstants.AI_OPTIMAL_RANGE_BUFFER_NEAR);
            float sqrOptimalNear = (optimalRange - CombatConstants.AI_OPTIMAL_RANGE_BUFFER_NEAR) * (optimalRange - CombatConstants.AI_OPTIMAL_RANGE_BUFFER_NEAR);

            if (sqrDistanceToPlayer > sqrOptimalFar)
            {
                // Move toward player
                MoveInDirection(directionToPlayer);
            }
            else if (sqrDistanceToPlayer < sqrOptimalNear)
            {
                // Move away from player
                MoveInDirection(-directionToPlayer);
            }
            else
            {
                // Stop movement when in optimal range
                movementController.SetMovementInput(Vector3.zero);
            }
        }

        private void MoveInDirection(Vector3 direction)
        {
            // Convert direction to discrete movement input for the MovementController
            Vector3 moveInput = Vector3.zero;

            if (direction.x > 0.1f) moveInput.x = 1f;
            else if (direction.x < -0.1f) moveInput.x = -1f;

            if (direction.z > 0.1f) moveInput.z = 1f;
            else if (direction.z < -0.1f) moveInput.z = -1f;

            // Use the MovementController input system instead of direct transform manipulation
            movementController.SetMovementInput(moveInput);
        }

        private void TryUseSkill()
        {
            // Only use skills if not currently executing one
            if (skillSystem.CurrentState != SkillExecutionState.Uncharged)
            {
                return;
            }

            // Check if we have enough stamina for any skill
            var staminaSystem = GetComponent<StaminaSystem>();
            if (staminaSystem.CurrentStamina < 2) // Minimum stamina for any skill
            {
                // Start resting if very low on stamina
                if (staminaSystem.CurrentStamina < 10)
                {
                    staminaSystem.StartResting();
                }
                return;
            }

            // Select a skill based on weights
            SkillType selectedSkill = SelectRandomSkill();

            // Check if we can use the selected skill
            if (!skillSystem.CanChargeSkill(selectedSkill))
            {
                return;
            }

            // Check if we're in range for offensive skills
            if (SpeedResolver.IsOffensiveSkill(selectedSkill))
            {
                if (!weaponController.IsInRange(player))
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} AI wanted to use {selectedSkill} but target is out of range");
                    }
                    return;
                }
            }

            // Use the skill - Attack executes immediately, others require charging
            if (selectedSkill == SkillType.Attack)
            {
                // Attack executes immediately
                skillSystem.ExecuteSkill(SkillType.Attack);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI executed {selectedSkill} immediately");
                }
            }
            else
            {
                // Other skills require charging
                skillSystem.StartCharging(selectedSkill);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI charging {selectedSkill}");
                }

                // Start coroutine to execute skill when charged
                StartCoroutine(ExecuteSkillWhenCharged(selectedSkill));
            }

            // Set next skill time
            nextSkillTime = Time.time + skillCooldown + Random.Range(-randomVariance, randomVariance);
        }

        private IEnumerator ExecuteSkillWhenCharged(SkillType skillType)
        {
            // Wait for skill to be charged
            while (skillSystem.CurrentState == SkillExecutionState.Charging)
            {
                yield return null;
            }

            // Execute if still charged and conditions are met
            if (skillSystem.CurrentState == SkillExecutionState.Charged && skillSystem.CurrentSkill == skillType)
            {
                // Double-check range for offensive skills
                if (SpeedResolver.IsOffensiveSkill(skillType))
                {
                    if (!weaponController.IsInRange(player))
                    {
                        skillSystem.CancelSkill();
                        if (enableDebugLogs)
                        {
                            Debug.Log($"{gameObject.name} AI cancelled {skillType} - target moved out of range");
                        }
                        yield break;
                    }
                }

                skillSystem.ExecuteSkill(skillType);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI executed {skillType}");
                }
            }
        }

        private SkillType SelectRandomSkill()
        {
            float totalWeight = attackWeight + defenseWeight + counterWeight + smashWeight + windmillWeight;
            float randomValue = Random.Range(0f, totalWeight);

            float currentWeight = 0f;

            currentWeight += attackWeight;
            if (randomValue <= currentWeight) return SkillType.Attack;

            currentWeight += defenseWeight;
            if (randomValue <= currentWeight) return SkillType.Defense;

            currentWeight += counterWeight;
            if (randomValue <= currentWeight) return SkillType.Counter;

            currentWeight += smashWeight;
            if (randomValue <= currentWeight) return SkillType.Smash;

            return SkillType.Windmill;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw AI detection and optimal ranges
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, engageDistance);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, optimalRange);

            // Draw line to current target
            if (player != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, player.position);
            }

            #if UNITY_EDITOR
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 3f,
                    $"AI State\nDistance: {distance:F1}\nNext Skill: {nextSkillTime - Time.time:F1}s"
                );
            }
            #endif
        }
    }
}