# Classic Mabinogi Combat Enhancement Plan

## Overview
This plan enhances FairyGate's combat system with authentic classic (pre-2012) Mabinogi mechanics while preserving the tactical, methodical combat that made Mabinogi unique.

## Core Design Philosophy
- **Slow, Methodical Combat**: 2-second charge times create deliberate pacing
- **Prediction-Based Gameplay**: Observe, learn, and counter enemy patterns
- **Tactical Vulnerability Windows**: Commitment to decisions through charge times
- **Knowledge-Based Mastery**: Reward experience and pattern recognition over reflexes

---

## Phase 1: Variable Skill Load Times (Classic Authenticity)
**Goal:** Create tactical variety through asymmetric timing windows

### Implementation Details

#### Skill-Specific Charge Times
```csharp
// Replace uniform BASE_SKILL_CHARGE_TIME with per-skill values
Windmill: 0.8-1.0 seconds   // Fast AoE option (dominated pre-Genesis)
Attack: Instant (0 seconds)  // No charging required
Defense: 1.0 seconds         // Quick defensive option
Counter: 1.0 seconds         // Quick defensive option
Smash: 2.0 seconds          // Current value - matches classic exactly
Lunge: 1.5 seconds          // Medium commitment
RangedAttack: Aim time       // Keep current accuracy buildup system
// Future heavy skills: 2.5-3.0 seconds for high risk/reward
```

### Why This Matters
- Creates "Do I have time for Smash or should I use Windmill?" decisions
- Different skills fit different tactical windows
- Windmill's 0.8s speed advantage was key to classic meta
- Asymmetric timing prevents predictable rhythm

---

## Phase 2: Classic Resource Management
**Goal:** Add constant tension through authentic stamina system

### Passive Stamina Regeneration
```csharp
// Add to StaminaSystem.cs
public const float PASSIVE_STAMINA_REGEN = 0.4f; // per second while standing
public const float REST_STAMINA_REGEN = 25f;     // Keep current for active recovery
public const float HUNGER_THRESHOLD = 0.5f;       // Below 50% hunger
public const float HUNGER_REGEN_PENALTY = 0.2f;   // 20% effectiveness when hungry
```

### Classic Stamina Costs
```csharp
// Adjust to match pre-2012 values
Normal Attack: 1-2 stamina (weapon-dependent)
Smash: 5 stamina (increase from 4)
Windmill: 4 stamina (increase from 3)
Defense: 2 initial + 1/sec drain (reduce from 3 + 3/sec)
Counter: 3 initial + 1/sec drain (reduce from 5 + 5/sec)
Lunge: 4 stamina (keep current)
RangedAttack: 3 stamina (keep current)
```

### Zero Stamina Behavior
- Can still attack but damage reduced to bare-handed levels
- Defense/Counter can continue draining even at 0 stamina
- Creates desperation tactics when resources depleted

---

## Phase 3: Strategic Vulnerability Windows
**Goal:** Enable prediction-based gameplay through commitment mechanics

### Skill Loading While Stunned
```csharp
// In SkillSystem state machine
public bool CanChargeWhileStunned = true; // Classic mechanic

// Implementation:
if (isStunned && CanChargeWhileStunned)
{
    // Continue charging but pause at current progress
    // Cannot move or cancel
    chargeProgress = Mathf.Min(chargeProgress, currentChargeAmount);
}
```

### Movement Speed During Charging
```csharp
// Movement penalties while charging
public const float OFFENSIVE_CHARGE_MOVE_SPEED = 0.5f;  // 50% speed
public const float DEFENSIVE_CHARGE_MOVE_SPEED = 0.0f;  // Rooted in place

// Special cases:
// Counterattack with certain weapons: Allow 0.3f movement
// Windmill: No movement penalty (keep mobile)
```

### Skill Interruption Rules
- Getting knocked down cancels skill preparation (lose progress)
- Stun pauses but doesn't cancel charging (resume after)
- Cannot start new skills during Active frames (commitment)
- Roar/special abilities can disrupt charging

---

## Phase 4: Classic Stun & Combo Mechanics
**Goal:** Implement weapon-based timing for tactical depth

### Weapon Speed-Based Stun Duration
```csharp
// WeaponData.cs already has stunDuration field
// Calibrate values:
Slow weapons (Mace): 1.5-2.0 seconds
Medium weapons (Sword): 1.0-1.2 seconds
Fast weapons (Dagger): 0.3-0.5 seconds
Very Fast (Knuckles): 0.2-0.3 seconds
```

### N+1 Combo System
```csharp
// Enable extending combos through timing
public class ComboExtension
{
    // If player times next attack during stun window:
    // 3-hit weapon → 4-hit possible (N+1)
    // Requires frame-perfect timing based on weapon stun

    float comboWindowStart = stunDuration * 0.7f;
    float comboWindowEnd = stunDuration * 0.95f;

    if (inputTime >= comboWindowStart && inputTime <= comboWindowEnd)
    {
        ExtendComboByOne();
    }
}
```

### Knockdown Accumulation Refinement
```csharp
// Diminishing returns to prevent spam
float GetKnockdownBuildup(int hitNumber)
{
    switch(hitNumber)
    {
        case 1: return 30f;  // First hit: 30%
        case 2: return 25f;  // Second hit: 25%
        case 3: return 20f;  // Third hit: 20%
        default: return 15f; // Subsequent hits: 15%
    }
}
```

---

## Phase 5: AI Pattern System (Knowledge-Based Combat)
**Goal:** Make combat about observation and prediction, not reflexes

### Predictable Enemy Patterns
```csharp
public class ClassicAIPattern
{
    // Each enemy type has specific, learnable patterns

    Bear:
        "Load Smash → Rush forward → Cancel at 0.2s → Normal Attack"
        "If hit 3 times → Always Defense"

    Spider:
        "Defense after taking 2 hits"
        "Counter when player charges Smash"
        "Windmill if surrounded"

    Wolf:
        "Counter when player is charging"
        "Smash after successful Defense"
        "Retreat at 30% HP"

    // Patterns are consistent and learnable
    // Success comes from pattern recognition
}
```

### AI Skill Commitment
```csharp
// AI must follow same rules as players
public const float AI_CHARGE_COMMITMENT = 1.0f; // Cannot cancel once started
public const bool AI_FOLLOWS_PLAYER_RULES = true; // Same stun, stamina, knockdown
```

### Pattern Telegraph System
```csharp
// Subtle visual cues before skill execution
void TelegraphNextSkill(SkillType nextSkill)
{
    // 0.3-0.5 seconds before charging:
    // - Stance shift animation
    // - Eye glow color change
    // - Weapon position adjustment
    // Rewards observant players
}
```

---

## Implementation Priority & Timeline

### Week 1: Variable Load Times
- Implement per-skill charge times
- Test Windmill at 0.8s vs Smash at 2.0s
- Verify tactical variety emerges

### Week 2: Resource Management
- Add 0.4/s passive stamina regeneration
- Implement hunger system effects
- Adjust stamina costs to classic values
- Test zero-stamina combat

### Week 3: Vulnerability Windows
- Enable charging while stunned (paused progress)
- Add movement speed penalties during charging
- Implement skill interruption rules

### Week 4: Weapon-Based Timing
- Calibrate weapon-specific stun durations
- Implement N+1 combo extension system
- Add knockdown buildup diminishing returns

### Week 5: AI Pattern System
- Design learnable patterns for each enemy type
- Implement pattern telegraph system
- Ensure AI follows same combat rules as players

---

## What NOT to Add (Avoiding Modern Mistakes)

### Never Implement
- ❌ **Skill Cooldowns** - Destroyed tactical preparation focus
- ❌ **Instant Cast Skills** (except Attack) - Removed commitment
- ❌ **Animation Canceling** - Commitment is key to depth
- ❌ **Reduced Player Stun** - Asymmetric stun broke equal footing
- ❌ **Multi-Hit Defense** - One-hit block creates risk/reward

### Design Principles to Maintain
- **Commitment**: Once you start charging, you're vulnerable
- **Equality**: Players and enemies follow same rules
- **Knowledge**: Success through pattern learning, not reflexes
- **Deliberation**: Time to think tactically, not button mash

---

## Expected Outcomes

### Combat Feel
- **Chess-like decision making** with 2-second thinking windows
- **Tension from vulnerability** during skill charging
- **Satisfaction from pattern mastery** over reactive play
- **Unique identity** separate from modern action combat

### Player Experience
- New players: Initial struggle → Pattern recognition → Mastery satisfaction
- Veterans: Deep tactical options through timing and positioning
- PvP: Mind games through feints and prediction
- PvE: Boss fights feel like strategic duels

### Balance Considerations
- Windmill at 0.8s will be strong but limited by stamina cost
- Smash at 2.0s requires safe windows but rewards with damage
- Defense/Counter become positional tools, not spam options
- Attack remains the safe but low-reward option

---

## Success Metrics

### Short-Term (1 Month)
- Combat encounters last 30-60 seconds (not 5-10)
- Players use 4+ different skills per fight
- Death from poor timing, not stat checks
- Clear skill usage patterns emerge

### Long-Term (3 Months)
- Players can identify enemy patterns by observation
- Skill mastery reduces death rate by 70%+
- PvP develops meta-game around prediction
- Combat retains engagement without power creep

---

## Conclusion

This plan preserves what made classic Mabinogi special:
- **Slow, deliberate combat** that rewards thinking
- **Prediction over reaction** through pattern learning
- **Meaningful commitment** through vulnerability windows
- **Unique identity** in a sea of generic action combat

Your current 2-second charge foundation is perfect. These enhancements complete the classic experience while maintaining your innovative additions (accuracy system, dual-threshold knockdown) that enhance rather than compromise the tactical depth.