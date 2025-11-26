namespace FairyGate.Combat
{
    /// <summary>
    /// Represents a skill execution in progress. Used by the combat interaction system.
    /// Extracted from CombatInteractionManager for better organization.
    /// </summary>
    public class SkillExecution
    {
        public SkillSystem skillSystem;
        public SkillType skillType;
        public CombatController combatant;
        public float timestamp;

        public void Reset()
        {
            skillSystem = null;
            skillType = SkillType.Attack;
            combatant = null;
            timestamp = 0f;
        }
    }
}
