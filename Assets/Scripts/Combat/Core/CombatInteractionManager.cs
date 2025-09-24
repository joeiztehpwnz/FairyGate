using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    public class CombatInteractionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogs = true;

        private static CombatInteractionManager instance;
        public static CombatInteractionManager Instance => instance;

        private Queue<SkillExecution> pendingExecutions = new Queue<SkillExecution>();
        private List<SkillExecution> waitingDefensiveSkills = new List<SkillExecution>();

        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            ProcessPendingExecutions();
        }

        public void ProcessSkillExecution(SkillSystem skillSystem, SkillType skillType)
        {
            var execution = new SkillExecution
            {
                skillSystem = skillSystem,
                skillType = skillType,
                combatant = skillSystem.GetComponent<CombatController>(),
                timestamp = Time.time
            };

            if (SpeedResolver.IsOffensiveSkill(skillType))
            {
                pendingExecutions.Enqueue(execution);
            }
            else if (SpeedResolver.IsDefensiveSkill(skillType))
            {
                waitingDefensiveSkills.Add(execution);
            }
        }

        private void ProcessPendingExecutions()
        {
            if (pendingExecutions.Count == 0) return;

            var offensiveSkills = new List<SkillExecution>();

            // Collect all simultaneous offensive executions
            while (pendingExecutions.Count > 0)
            {
                var execution = pendingExecutions.Dequeue();
                if (Time.time - execution.timestamp < 0.1f) // Small window for simultaneous execution
                {
                    offensiveSkills.Add(execution);
                }
            }

            if (offensiveSkills.Count == 1)
            {
                // Single offensive skill
                ProcessSingleOffensiveSkill(offensiveSkills[0]);
            }
            else if (offensiveSkills.Count > 1)
            {
                // Multiple offensive skills - resolve speed conflicts
                ProcessMultipleOffensiveSkills(offensiveSkills);
            }
        }

        private void ProcessSingleOffensiveSkill(SkillExecution offensiveSkill)
        {
            // Check for defensive responses
            var validDefenses = GetValidDefensiveResponses(offensiveSkill);

            if (validDefenses.Count == 0)
            {
                // No defensive response - execute normally
                ExecuteOffensiveSkillDirectly(offensiveSkill);
            }
            else
            {
                // Process defensive interactions
                foreach (var defense in validDefenses)
                {
                    ProcessSkillInteraction(offensiveSkill, defense);
                }
            }
        }

        private void ProcessMultipleOffensiveSkills(List<SkillExecution> offensiveSkills)
        {
            // Resolve speed conflicts between offensive skills
            var speedResults = ResolveSpeedConflicts(offensiveSkills);

            foreach (var result in speedResults)
            {
                if (result.resolution == SpeedResolution.Tie)
                {
                    // Simultaneous execution
                    foreach (var execution in result.tiedExecutions)
                    {
                        ExecuteOffensiveSkillDirectly(execution);
                    }
                }
                else
                {
                    // Winner executes, loser is cancelled
                    ExecuteOffensiveSkillDirectly(result.winner);
                    CancelSkillExecution(result.loser, "Lost speed resolution");
                }
            }
        }

        private List<SkillExecution> GetValidDefensiveResponses(SkillExecution offensiveSkill)
        {
            var validResponses = new List<SkillExecution>();

            foreach (var defensiveSkill in waitingDefensiveSkills.ToList())
            {
                // Check if defensive skill can respond to this offensive skill
                if (CanDefensiveSkillRespond(defensiveSkill, offensiveSkill))
                {
                    validResponses.Add(defensiveSkill);
                    waitingDefensiveSkills.Remove(defensiveSkill);
                }
            }

            return validResponses;
        }

        private bool CanDefensiveSkillRespond(SkillExecution defensiveSkill, SkillExecution offensiveSkill)
        {
            // Check range requirements
            var defenderWeapon = defensiveSkill.combatant.GetComponent<WeaponController>();
            var attackerTransform = offensiveSkill.combatant.transform;

            // Initial range check when entering waiting state
            if (!defenderWeapon.IsInRange(attackerTransform))
            {
                return false;
            }

            // Check if skills can interact
            if (!SpeedResolver.CanInteract(offensiveSkill.skillType, defensiveSkill.skillType))
            {
                return false;
            }

            // Secondary range check for incoming attack
            var attackerWeapon = offensiveSkill.combatant.GetComponent<WeaponController>();
            if (!attackerWeapon.IsInRange(defensiveSkill.combatant.transform))
            {
                return false;
            }

            return true;
        }

        private void ProcessSkillInteraction(SkillExecution offensiveSkill, SkillExecution defensiveSkill)
        {
            var interaction = DetermineInteraction(offensiveSkill.skillType, defensiveSkill.skillType);

            if (enableDebugLogs)
            {
                Debug.Log($"Processing interaction: {offensiveSkill.combatant.name} {offensiveSkill.skillType} vs " +
                         $"{defensiveSkill.combatant.name} {defensiveSkill.skillType} = {interaction}");
            }

            // Get character stats and weapon data
            var attackerStats = offensiveSkill.combatant.Stats;
            var defenderStats = defensiveSkill.combatant.Stats;
            var attackerWeapon = offensiveSkill.combatant.GetComponent<WeaponController>()?.WeaponData;
            var defenderWeapon = defensiveSkill.combatant.GetComponent<WeaponController>()?.WeaponData;

            if (attackerStats == null || defenderStats == null || attackerWeapon == null || defenderWeapon == null)
            {
                Debug.LogError("Missing required components for skill interaction");
                return;
            }

            // Process damage and effects based on interaction
            ProcessInteractionEffects(interaction, offensiveSkill, defensiveSkill, attackerStats, defenderStats, attackerWeapon, defenderWeapon);

            // Complete defensive skill execution
            CompleteDefensiveSkillExecution(defensiveSkill);
        }

        private InteractionResult DetermineInteraction(SkillType offensive, SkillType defensive)
        {
            // All 17 interactions from the matrix
            switch (offensive)
            {
                case SkillType.Attack:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.AttackerStunned; // Attack vs Defense → Attacker stunned, defender blocks
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Attack vs Counter → Counter reflection
                    }
                    break;

                case SkillType.Smash:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderKnockedDown; // Smash vs Defense → Defender knocked down, takes 75% reduced damage
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Smash vs Counter → Counter reflection
                    }
                    break;

                case SkillType.Windmill:
                    switch (defensive)
                    {
                        case SkillType.Defense: return InteractionResult.DefenderBlocks; // Windmill vs Defense → No status effects, defender blocks
                        case SkillType.Counter: return InteractionResult.CounterReflection; // Windmill vs Counter → Counter reflection
                    }
                    break;
            }

            return InteractionResult.NoInteraction;
        }

        private void ProcessInteractionEffects(
            InteractionResult interaction,
            SkillExecution attacker,
            SkillExecution defender,
            CharacterStats attackerStats,
            CharacterStats defenderStats,
            WeaponData attackerWeapon,
            WeaponData defenderWeapon)
        {
            var attackerHealth = attacker.combatant.GetComponent<HealthSystem>();
            var defenderHealth = defender.combatant.GetComponent<HealthSystem>();
            var attackerStatusEffects = attacker.combatant.GetComponent<StatusEffectManager>();
            var defenderStatusEffects = defender.combatant.GetComponent<StatusEffectManager>();
            var defenderKnockdownMeter = defender.combatant.GetComponent<KnockdownMeterTracker>();

            switch (interaction)
            {
                case InteractionResult.AttackerStunned: // Attack vs Defense
                    // Attacker stunned, defender blocks (0 damage)
                    attackerStatusEffects.ApplyStun(attackerWeapon.stunDuration);
                    defenderStatusEffects.ApplyStun(attackerWeapon.stunDuration * 0.5f); // Defender receives half stun
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{attacker.combatant.name} attack blocked by {defender.combatant.name} defense");
                    }
                    break;

                case InteractionResult.CounterReflection: // Any skill vs Counter
                    // Attacker knocked down, defender takes 0 damage, reflects calculated damage back
                    attackerStatusEffects.ApplyInteractionKnockdown();
                    int reflectedDamage = DamageCalculator.CalculateCounterReflection(attackerStats, attackerWeapon);
                    attackerHealth.TakeDamage(reflectedDamage, defender.combatant.transform);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{defender.combatant.name} counter reflected {reflectedDamage} damage to {attacker.combatant.name}");
                    }
                    break;

                case InteractionResult.DefenderKnockedDown: // Smash vs Defense
                    // Defender knocked down, takes 75% reduced damage
                    defenderStatusEffects.ApplyInteractionKnockdown();
                    int baseDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats);
                    int reducedDamage = DamageCalculator.ApplyDamageReduction(baseDamage, CombatConstants.SMASH_VS_DEFENSE_DAMAGE_REDUCTION, defenderStats);
                    defenderHealth.TakeDamage(reducedDamage, attacker.combatant.transform);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{attacker.combatant.name} smash broke through {defender.combatant.name} defense for {reducedDamage} damage");
                    }
                    break;

                case InteractionResult.DefenderBlocks: // Windmill vs Defense
                    // No status effects, defender blocks (0 damage)
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{defender.combatant.name} blocked {attacker.combatant.name} windmill cleanly");
                    }
                    break;
            }
        }

        private void ExecuteOffensiveSkillDirectly(SkillExecution execution)
        {
            // No defensive response - execute skill normally against target
            var target = execution.combatant.CurrentTarget;
            if (target == null) return;

            var targetHealth = target.GetComponent<HealthSystem>();
            var targetKnockdownMeter = target.GetComponent<KnockdownMeterTracker>();
            var targetStatusEffects = target.GetComponent<StatusEffectManager>();

            if (targetHealth == null) return;

            var attackerStats = execution.combatant.Stats;
            var targetStats = targetHealth.GetComponent<CombatController>()?.Stats;
            var attackerWeapon = execution.combatant.GetComponent<WeaponController>()?.WeaponData;

            if (attackerStats == null || targetStats == null || attackerWeapon == null) return;

            // Calculate and apply damage
            int damage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, targetStats);
            targetHealth.TakeDamage(damage, execution.combatant.transform);

            // Apply status effects based on skill type
            switch (execution.skillType)
            {
                case SkillType.Attack:
                    // Apply stun and knockdown meter buildup
                    targetStatusEffects.ApplyStun(attackerWeapon.stunDuration);
                    targetKnockdownMeter.AddMeterBuildup(damage, attackerStats);
                    break;

                case SkillType.Smash:
                case SkillType.Windmill:
                    // Immediate knockdown (bypasses meter system)
                    targetKnockdownMeter.TriggerImmediateKnockdown();
                    break;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{execution.combatant.name} {execution.skillType} hit {target.name} for {damage} damage");
            }
        }

        private void CompleteDefensiveSkillExecution(SkillExecution defensiveSkill)
        {
            var skillSystem = defensiveSkill.skillSystem;
            if (skillSystem != null)
            {
                // Transition defensive skill out of waiting state to recovery
                skillSystem.ForceTransitionToRecovery();
            }
        }

        private void CancelSkillExecution(SkillExecution execution, string reason)
        {
            execution.skillSystem.CancelSkill();

            if (enableDebugLogs)
            {
                Debug.Log($"{execution.combatant.name} {execution.skillType} cancelled: {reason}");
            }
        }

        private List<SpeedResolutionGroupResult> ResolveSpeedConflicts(List<SkillExecution> offensiveSkills)
        {
            var results = new List<SpeedResolutionGroupResult>();

            // Group skills by simultaneous execution timing
            var groups = GroupSimultaneousSkills(offensiveSkills);

            foreach (var group in groups)
            {
                if (group.Count == 1)
                {
                    results.Add(new SpeedResolutionGroupResult
                    {
                        resolution = SpeedResolution.Player1Wins,
                        winner = group[0],
                        loser = null
                    });
                }
                else
                {
                    // Resolve speed between group members
                    var speedResult = ResolveSpeedBetweenSkills(group);
                    results.Add(speedResult);
                }
            }

            return results;
        }

        private List<List<SkillExecution>> GroupSimultaneousSkills(List<SkillExecution> skills)
        {
            var groups = new List<List<SkillExecution>>();
            const float simultaneousThreshold = 0.1f; // 100ms window

            foreach (var skill in skills.OrderBy(s => s.timestamp))
            {
                bool addedToGroup = false;

                foreach (var group in groups)
                {
                    if (Mathf.Abs(skill.timestamp - group[0].timestamp) <= simultaneousThreshold)
                    {
                        group.Add(skill);
                        addedToGroup = true;
                        break;
                    }
                }

                if (!addedToGroup)
                {
                    groups.Add(new List<SkillExecution> { skill });
                }
            }

            return groups;
        }

        private SpeedResolutionGroupResult ResolveSpeedBetweenSkills(List<SkillExecution> skills)
        {
            if (skills.Count < 2)
            {
                return new SpeedResolutionGroupResult
                {
                    resolution = SpeedResolution.Player1Wins,
                    winner = skills[0],
                    loser = null
                };
            }

            // Calculate speeds for all skills
            var skillSpeeds = skills.Select(skill =>
            {
                var combatant = skill.combatant;
                var stats = combatant.Stats;
                var weaponController = combatant.GetComponent<WeaponController>();
                var weapon = weaponController?.WeaponData;

                return new
                {
                    skill = skill,
                    speed = stats != null && weapon != null
                        ? SpeedResolver.CalculateSpeed(skill.skillType, stats, weapon)
                        : 0f
                };
            }).ToList();

            // Find highest speed
            float maxSpeed = skillSpeeds.Max(s => s.speed);
            var winners = skillSpeeds.Where(s => Mathf.Approximately(s.speed, maxSpeed)).ToList();

            if (winners.Count == 1)
            {
                // Single winner
                var winner = winners[0];
                var loser = skillSpeeds.FirstOrDefault(s => s != winner);

                return new SpeedResolutionGroupResult
                {
                    resolution = SpeedResolution.Player1Wins,
                    winner = winner.skill,
                    loser = loser?.skill
                };
            }
            else
            {
                // Tie - simultaneous execution
                return new SpeedResolutionGroupResult
                {
                    resolution = SpeedResolution.Tie,
                    tiedExecutions = winners.Select(w => w.skill).ToList()
                };
            }
        }

        private class SkillExecution
        {
            public SkillSystem skillSystem;
            public SkillType skillType;
            public CombatController combatant;
            public float timestamp;
        }

        private class SpeedResolutionGroupResult
        {
            public SpeedResolution resolution;
            public SkillExecution winner;
            public SkillExecution loser;
            public List<SkillExecution> tiedExecutions;
        }
    }
}