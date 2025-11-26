using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles speed-based conflict resolution between multiple offensive skills.
    /// Extracted from CombatInteractionManager to reduce complexity.
    /// </summary>
    public class SpeedConflictResolver
    {
        private readonly CombatObjectPoolManager poolManager;

        public SpeedConflictResolver(CombatObjectPoolManager poolManager)
        {
            this.poolManager = poolManager;
        }

        /// <summary>
        /// Resolves speed conflicts between multiple offensive skills.
        /// </summary>
        public List<SpeedResolutionGroupResult> ResolveSpeedConflicts(List<SkillExecution> offensiveSkills)
        {
            var results = poolManager.GetResultsList();

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

            // Return groups list to pool
            poolManager.ReturnNestedList(groups);

            return results;
        }

        /// <summary>
        /// Groups skills that should execute simultaneously based on timing window.
        /// </summary>
        public List<List<SkillExecution>> GroupSimultaneousSkills(List<SkillExecution> skills)
        {
            var groups = poolManager.GetNestedList();

            foreach (var skill in skills.OrderBy(s => s.timestamp))
            {
                bool addedToGroup = false;

                foreach (var group in groups)
                {
                    if (Mathf.Abs(skill.timestamp - group[0].timestamp) <= CombatConstants.SIMULTANEOUS_EXECUTION_WINDOW)
                    {
                        group.Add(skill);
                        addedToGroup = true;
                        break;
                    }
                }

                if (!addedToGroup)
                {
                    var newGroup = poolManager.GetSkillExecutionList();
                    newGroup.Add(skill);
                    groups.Add(newGroup);
                }
            }

            return groups;
        }

        /// <summary>
        /// Resolves speed between multiple skills to determine winner/tie.
        /// </summary>
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
    }

    public class SpeedResolutionGroupResult
    {
        public SpeedResolution resolution;
        public SkillExecution winner;
        public SkillExecution loser;
        public List<SkillExecution> tiedExecutions;
    }
}
