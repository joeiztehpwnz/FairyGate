namespace FairyGate.Combat
{
    /// <summary>
    /// Enemy archetype types for experimental enemy variety.
    /// Each archetype has unique stats and recommended AI behavior.
    /// </summary>
    public enum EnemyArchetype
    {
        Soldier,      // Balanced, all-around fighter (default)
        Berserker,    // Glass cannon - high damage, low defense
        Guardian,     // Tank - high defense, low damage
        Assassin,     // Speedster - fast, evasive, counter-focused
        Archer        // Ranged specialist - keeps distance, ranged attacks
    }
}
