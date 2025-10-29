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
        [SerializeField] private float lungeWeight = 10f;

        [Header("Reactive AI")]
        [SerializeField] [Range(0f, 1f)] private float reactionChance = 0.6f; // 60% chance to react to player actions

        [Header("Coordination")]
        [SerializeField] private bool useCoordination = true; // Enable attack timing coordination with other AI

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private float nextSkillTime;
        private Transform player;
        private CombatController playerCombatController; // For observing player actions
        private CombatController combatController;
        private SkillSystem skillSystem;
        private WeaponController weaponController;
        private MovementController movementController;
        private StatusEffectManager statusEffectManager;
        private StaminaSystem staminaSystem; // Phase 2.3 optimization
        private bool hasAttackSlot = false; // Tracks if coordinator granted attack permission

        // Public property for coordinator to check if AI is ready to attack
        public bool IsReadyToAttack => Time.time >= nextSkillTime &&
                                        skillSystem.CurrentState == SkillExecutionState.Uncharged &&
                                        staminaSystem.CurrentStamina >= 2;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            skillSystem = GetComponent<SkillSystem>();
            weaponController = GetComponent<WeaponController>();
            movementController = GetComponent<MovementController>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            staminaSystem = GetComponent<StaminaSystem>(); // Phase 2.3 optimization

            nextSkillTime = Time.time + skillCooldown + Random.Range(-randomVariance, randomVariance);
        }

        private void Start()
        {
            FindPlayer();

            // Register with coordinator if coordination is enabled
            if (useCoordination)
            {
                AICoordinator.Instance.RegisterEnemy(this);
            }
        }

        private void OnDisable()
        {
            // Unregister from coordinator when disabled
            if (useCoordination && AICoordinator.Instance != null)
            {
                AICoordinator.Instance.UnregisterEnemy(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from coordinator when destroyed
            if (useCoordination && AICoordinator.Instance != null)
            {
                AICoordinator.Instance.UnregisterEnemy(this);
            }
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
                    playerCombatController = combatant; // Store for reactive AI
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
                            playerCombatController = combatant; // Store for reactive AI
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

            // Move to maintain optimal attack range
            // Using squared distance comparisons
            float weaponRange = weaponController?.CurrentRange ?? 1.5f;
            float optimalRange = weaponRange * 0.70f; // Prefer 70% of max range (guarantees in-range positioning)
            float rangeTolerance = 0.3f; // Dead zone to prevent jittering
            float tooCloseThreshold = 0.8f;

            // Calculate range boundaries BEFORE squaring (fixes jitter bug)
            float outerRange = optimalRange + rangeTolerance;
            float innerRange = optimalRange - rangeTolerance;

            float sqrOuterRange = outerRange * outerRange;
            float sqrInnerRange = innerRange * innerRange;
            float sqrTooClose = tooCloseThreshold * tooCloseThreshold;

            if (sqrDistanceToPlayer > sqrOuterRange)
            {
                // Too far, move toward player
                MoveInDirection(directionToPlayer);
            }
            else if (sqrDistanceToPlayer < sqrTooClose)
            {
                // Too close, move away from player
                MoveInDirection(-directionToPlayer);
            }
            else if (sqrDistanceToPlayer < sqrInnerRange)
            {
                // Slightly inside optimal range, back up
                MoveInDirection(-directionToPlayer);
            }
            else
            {
                // Within optimal range tolerance, stop to avoid jittering
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

        private bool IsWeaponInRange()
        {
            if (player == null || weaponController == null) return false;
            // Delegates to optimized WeaponController.IsInRange() which uses squared distance
            return weaponController.IsInRange(player);
        }

        private void TryUseSkill()
        {
            // Only use skills if not currently executing one
            if (skillSystem.CurrentState != SkillExecutionState.Uncharged)
            {
                return;
            }

            // Check if we have enough stamina for any skill
            if (staminaSystem.CurrentStamina < 2) // Minimum stamina for any skill
            {
                // Start resting if very low on stamina
                if (staminaSystem.CurrentStamina < 10)
                {
                    staminaSystem.StartResting();
                }
                return;
            }

            // Request attack permission from coordinator if coordination is enabled
            if (useCoordination)
            {
                if (!hasAttackSlot)
                {
                    // Try to get permission to attack
                    bool granted = AICoordinator.Instance.RequestAttackPermission(this);
                    if (!granted)
                    {
                        // Permission denied, wait for next opportunity
                        return;
                    }
                    hasAttackSlot = true;
                }
            }

            // Select a skill (reactive or random based on player state)
            SkillType selectedSkill = SelectSkill();

            // Check if we can use the selected skill
            if (!skillSystem.CanChargeSkill(selectedSkill))
            {
                return;
            }

            // Use the skill - Attack executes immediately, others require charging
            if (selectedSkill == SkillType.Attack)
            {
                // Attack executes immediately - movement positioning should have gotten us in range
                skillSystem.ExecuteSkill(SkillType.Attack);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI executed {selectedSkill} immediately");
                }

                // Release attack slot immediately after attack
                ReleaseAttackSlot();
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
                // Execute skill - positioning should have gotten us in range during charge
                skillSystem.ExecuteSkill(skillType);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} AI executed {skillType}");
                }
            }

            // Release attack slot after skill completes
            ReleaseAttackSlot();
        }

        private void ReleaseAttackSlot()
        {
            if (useCoordination && hasAttackSlot)
            {
                AICoordinator.Instance.ReleaseAttackSlot(this);
                hasAttackSlot = false;
            }
        }

        private SkillType SelectSkill()
        {
            // CC EXPLOITATION: React to player being crowd controlled
            if (playerCombatController != null)
            {
                var playerCombatState = playerCombatController.CurrentCombatState;

                // KNOCKDOWN: Player fully disabled for 2.0s - perfect opportunity
                if (playerCombatState == CombatState.KnockedDown)
                {
                    // Use reaction chance to determine if we exploit this window
                    if (Random.value < reactionChance)
                    {
                        // 60% Attack (fast, guaranteed hit), 30% Rest (if low stamina), 10% random
                        float exploitRoll = Random.value;

                        if (exploitRoll < 0.6f)
                        {
                            // Use Attack for guaranteed damage
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI exploiting player knockdown with Attack");
                            }
                            return SkillType.Attack;
                        }
                        else if (exploitRoll < 0.9f && staminaSystem.CurrentStamina < staminaSystem.MaxStamina * 0.3f)
                        {
                            // Rest to recover stamina during safe window
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI using knockdown window to rest");
                            }
                            // Start resting and return Attack as fallback
                            staminaSystem.StartResting();
                            return SkillType.Attack;
                        }
                    }
                    // Fall through to random selection if not exploiting
                }

                // KNOCKBACK: Player frozen 0.8s but can charge - aggressive window
                if (playerCombatState == CombatState.Knockback)
                {
                    if (Random.value < reactionChance)
                    {
                        // 60% Smash (heavy damage), 40% Attack (safe)
                        if (Random.value < 0.6f)
                        {
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI exploiting player knockback with Smash");
                            }
                            return SkillType.Smash;
                        }
                        else
                        {
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI exploiting player knockback with Attack");
                            }
                            return SkillType.Attack;
                        }
                    }
                }

                // STUNNED: Player briefly frozen - good time to charge
                if (playerCombatState == CombatState.Stunned)
                {
                    if (Random.value < reactionChance)
                    {
                        // 60% Smash (prepare heavy hit), 40% normal behavior
                        if (Random.value < 0.6f)
                        {
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI using player stun to charge Smash");
                            }
                            return SkillType.Smash;
                        }
                    }
                }
            }

            // Reactive AI: Observe player and react appropriately
            if (playerCombatController != null)
            {
                var playerSkill = playerCombatController.CurrentSkill;
                var playerState = playerCombatController.CurrentState;

                // React to player charging a skill
                if (playerState == SkillExecutionState.Charging)
                {
                    // React with counter skill based on reaction chance
                    if (Random.value < reactionChance)
                    {
                        SkillType counterSkill = GetCounterSkill(playerSkill);

                        if (enableDebugLogs)
                        {
                            Debug.Log($"{gameObject.name} AI reacting to player's {playerSkill} with {counterSkill}");
                        }

                        return counterSkill;
                    }
                }

                // React to player in defensive waiting state
                if (playerState == SkillExecutionState.Waiting)
                {
                    if (playerSkill == SkillType.Defense)
                    {
                        // Player is blocking, use Smash to break guard
                        if (Random.value < reactionChance)
                        {
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI breaking through player's Defense with Smash");
                            }
                            return SkillType.Smash;
                        }
                    }
                    else if (playerSkill == SkillType.Counter)
                    {
                        // Player is waiting for counter, use safe Attack
                        if (Random.value < reactionChance)
                        {
                            if (enableDebugLogs)
                            {
                                Debug.Log($"{gameObject.name} AI using safe Attack against player's Counter");
                            }
                            return SkillType.Attack;
                        }
                    }
                }
            }

            // No reaction triggered, fall back to personality-based random selection
            // Pass player CC state to adjust weights intelligently
            CombatState playerCCState = playerCombatController != null ? playerCombatController.CurrentCombatState : CombatState.Idle;
            return SelectRandomSkill(playerCCState);
        }

        private SkillType SelectRandomSkill(CombatState playerCombatState = CombatState.Idle)
        {
            // Adjust weights based on player's CC state to avoid wasting defensive skills
            float adjustedAttackWeight = attackWeight;
            float adjustedDefenseWeight = defenseWeight;
            float adjustedCounterWeight = counterWeight;
            float adjustedSmashWeight = smashWeight;
            float adjustedWindmillWeight = windmillWeight;
            float adjustedLungeWeight = lungeWeight;

            // KNOCKDOWN: Player can't attack, eliminate defensive skills and boost offensive
            if (playerCombatState == CombatState.KnockedDown)
            {
                adjustedDefenseWeight = 0f; // No point in defense
                adjustedCounterWeight = 0f; // No point in counter
                adjustedAttackWeight *= 1.5f; // +50% attack weight
                adjustedSmashWeight *= 1.5f; // +50% smash weight
            }
            // KNOCKBACK: Player can't attack for 0.8s, eliminate defensive skills and boost offensive
            else if (playerCombatState == CombatState.Knockback)
            {
                adjustedDefenseWeight = 0f; // No point in defense
                adjustedCounterWeight = 0f; // No point in counter
                adjustedAttackWeight *= 1.3f; // +30% attack weight
                adjustedSmashWeight *= 1.3f; // +30% smash weight
            }
            // STUNNED: Player unlikely to attack immediately, reduce defensive weights
            else if (playerCombatState == CombatState.Stunned)
            {
                adjustedDefenseWeight *= 0.5f; // -50% defense weight
                adjustedCounterWeight *= 0.5f; // -50% counter weight
                adjustedAttackWeight *= 1.2f; // +20% attack weight
            }

            // LUNGE RANGE CHECK: Boost Lunge weight if in sweet spot (2.0-4.0 units)
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer >= CombatConstants.LUNGE_MIN_RANGE && distanceToPlayer <= CombatConstants.LUNGE_MAX_RANGE)
                {
                    // In Lunge sweet spot - boost weight significantly
                    adjustedLungeWeight *= 2.5f; // +150% lunge weight when in ideal range
                }
                else
                {
                    // Outside Lunge range - eliminate weight
                    adjustedLungeWeight = 0f;
                }
            }

            float totalWeight = adjustedAttackWeight + adjustedDefenseWeight + adjustedCounterWeight + adjustedSmashWeight + adjustedWindmillWeight + adjustedLungeWeight;
            float randomValue = Random.Range(0f, totalWeight);

            float currentWeight = 0f;

            currentWeight += adjustedAttackWeight;
            if (randomValue <= currentWeight) return SkillType.Attack;

            currentWeight += adjustedDefenseWeight;
            if (randomValue <= currentWeight) return SkillType.Defense;

            currentWeight += adjustedCounterWeight;
            if (randomValue <= currentWeight) return SkillType.Counter;

            currentWeight += adjustedSmashWeight;
            if (randomValue <= currentWeight) return SkillType.Smash;

            currentWeight += adjustedWindmillWeight;
            if (randomValue <= currentWeight) return SkillType.Windmill;

            return SkillType.Lunge;
        }

        private SkillType GetCounterSkill(SkillType playerSkill)
        {
            // Returns the optimal counter to the player's current skill
            switch (playerSkill)
            {
                case SkillType.Attack:
                    return SkillType.Defense; // Defense stuns attacker

                case SkillType.Lunge:
                    return SkillType.Defense; // Defense stuns lunging attacker (same as Attack)

                case SkillType.Smash:
                    return SkillType.Counter; // Counter reflects Smash damage

                case SkillType.Defense:
                    return SkillType.Smash; // Smash breaks through Defense

                case SkillType.Counter:
                    return SkillType.Attack; // Attack is safe against Counter

                case SkillType.Windmill:
                    // Windmill hits in AoE, Defense blocks it
                    return SkillType.Defense;

                default:
                    // For unknown or defensive skills, use personality-based random
                    return SelectRandomSkill();
            }
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