namespace FairyGate.Combat
{
    public enum CombatState
    {
        Idle,           // Free movement, no combat actions
        Combat,         // Targeting enemy, can charge skills
        Charging,       // Charging a skill
        Executing,      // Skill in startup/active/recovery frames
        Stunned,        // Cannot move, can charge skills
        KnockedDown,    // Cannot move or act
        Resting,        // X key rest state, stamina regeneration
        Dead            // Character defeated
    }

    public enum SkillType
    {
        Attack,
        Defense,
        Counter,
        Smash,
        Windmill
    }

    public enum SkillExecutionState
    {
        Uncharged,      // Skill ready to be charged
        Charging,       // Skill charging (2 seconds base)
        Charged,        // Skill ready to execute
        Startup,        // Skill startup frames
        Active,         // Skill active frames (uncancellable)
        Recovery,       // Skill recovery frames
        Waiting         // Defense/Counter waiting state
    }

    public enum StatusEffectType
    {
        None,
        Stun,
        InteractionKnockdown,   // From skill interactions
        MeterKnockdown,         // From knockdown meter reaching 100%
        Block,
        Rest
    }

    public enum WeaponType
    {
        Sword,
        Spear,
        Dagger,
        Mace
    }

    public enum SpeedResolution
    {
        Player1Wins,
        Player2Wins,
        Tie
    }

    public enum DamageType
    {
        Physical,
        Magical,    // Reserved for future use
        Counter     // Reflected damage
    }

    public enum InteractionResult
    {
        AttackerWins,
        DefenderWins,
        AttackerStunned,
        DefenderStunned,
        AttackerKnockedDown,
        DefenderKnockedDown,
        AttackerBlocked,
        DefenderBlocks,
        CounterReflection,
        SpeedResolution,
        NoInteraction,
        SimultaneousExecution
    }
}