# Combat System Expansion - Design Document

**Project**: FairyGate Combat System
**Last Updated**: 2025-10-29
**Status**: Planning Phase

---

## Table of Contents

1. [Current System Analysis](#current-system-analysis)
2. [RPS-Inspired Expansion Framework](#rps-inspired-expansion-framework)
3. [Expansion Categories](#expansion-categories)
4. [Implementation Roadmap](#implementation-roadmap)
5. [Code Architecture References](#code-architecture-references)
6. [Design Philosophy](#design-philosophy)
7. [RPS Theory Corner](#rps-theory-corner)
8. [Mabinogi Alignment](#mabinogi-alignment)
9. [Spicy Variants](#spicy-variants)

---

## Current System Analysis

### Skill Roster (7 Skills)

| Skill | Type | Charge Time | Range | Stamina | Special Properties |
|-------|------|-------------|-------|---------|-------------------|
| **Attack** | Offensive | Instant | 2.0 | 2 | Basic instant attack |
| **Defense** | Defensive | 1.0s | 0 | 3 | Blocks offensive skills |
| **Counter** | Defensive | 1.0s | 0 | 4 | Reflects offensive skills |
| **Smash** | Offensive | 2.0s | 2.5 | 5 | Heavy charged attack, bypasses meter |
| **Windmill** | Offensive | 2.0s | 3.0 AOE | 6 | Area attack, bypasses meter |
| **RangedAttack** | Offensive | Instant + Aim | 10.0 | 3 | Long-range projectile |
| **Lunge** | Offensive | 1.5s | 2.0-4.0 | 4 | Gap-closing dash attack |

### Interaction Matrix

```
              Attack  Defense  Counter  Smash  Windmill  Ranged  Lunge
Attack        Speed   Stunned  Counter  Speed  Speed     Speed   Speed
Defense       Block   -        -        Block  Broken    Block   Block
Counter       Refl    -        -        Refl   Broken    Refl    Refl
Smash         Speed   Broken   Counter  Speed  Speed     Speed   Speed
Windmill      Speed   Broken   Counter  Speed  Speed     Speed   Speed
RangedAttack  Speed   Block    Refl     Speed  Speed     Speed   Speed
Lunge         Speed   Stunned  Counter  Speed  Speed     Speed   Speed
```

**Legend**:
- **Speed**: Dexterity-based resolution (higher Dex wins)
- **Block**: Defender blocks, attacker stunned
- **Refl**: Counter reflects damage back to attacker
- **Broken**: Defense broken, attacker wins
- **Stunned**: Attacker stunned
- **Counter**: Counterattack succeeds

### Three-Tier CC System

1. **Stun** (lowest priority)
   - Duration: Base × (1 - Focus/200)
   - Prevents movement and new skill usage
   - Can charge skills while stunned
   - Overridden by Knockback and Knockdown

2. **Knockback** (medium priority, 50% meter threshold)
   - Fixed duration: 0.5s
   - Displaces character away from attacker
   - Physical displacement via CharacterController
   - Overrides Stun, overridden by Knockdown

3. **Knockdown** (highest priority, 100% meter threshold)
   - Duration: Base × (1 - Focus/200)
   - Complete action lockout
   - Overrides all other CC types
   - Two sources: Meter-based or Interaction-based (Smash/Windmill)

### Knockdown Meter System

**Buildup Formula**:
```
Buildup = Base(5) + (Attacker.STR / 10) - (Defender.FOC / 30)
Minimum: 1.0
```

**Decay**: Continuous -5.0/sec (never resets)

**Thresholds**:
- 50%: Knockback trigger (once per cycle)
- 100%: Meter Knockdown

**Key Properties**:
- Meter NEVER resets (only decays)
- Smash/Windmill bypass meter entirely (Interaction Knockdown)
- Buildup applies to: Attack, RangedAttack, Lunge

### Core Architecture

**Component Structure**:
- `SkillSystem`: Skill execution FSM, input handling, charge/aim logic
- `StatusEffectManager`: CC state management, priority resolution
- `KnockdownMeterTracker`: Meter buildup, decay, threshold detection
- `CombatInteractionManager`: Interaction resolution, outcome processing
- `MovementController`: Movement with CC restrictions
- `HealthSystem`: HP management, death handling
- `SpeedResolver`: Dexterity-based tie-breaking
- `CombatUpdateManager`: Centralized update loop (object pooling)

**Key Design Patterns**:
- Event-driven architecture (C# Events)
- Object pooling for skill executions
- Struct-based status effects (value semantics)
- Centralized update management (ICombatUpdatable)

---

## RPS-Inspired Expansion Framework

### Core RPS Concepts Applied

**Classical RPS**: Each choice beats exactly one other, loses to exactly one other.

**Extended RPS** (Lizard-Spock): Each choice beats exactly TWO others, loses to exactly TWO others. Creates richer decision space while maintaining balance.

**Pattern Exploitation**: Players develop habits and patterns that can be predicted and countered by observant opponents.

**Nash Equilibrium vs Exploitation**: Perfect randomness (1/3, 1/3, 1/3) is unexploitable but suboptimal against human players who exhibit patterns.

**Asymmetric Variants**: Some choices are "stronger" but have specific weaknesses (e.g., Fire beats everything except Water).

**Momentum Mechanics**: Sequential rounds where winning/losing creates advantage/disadvantage states (muk-jji-ppa variant).

**Visual Tells**: Information leakage through animations, positioning, or incomplete commitment creates mind-game layers.

### Application to FairyGate Combat

Our current system already has strong RPS foundations:
- **Rock**: Offensive skills (Attack, Smash, Windmill, Lunge, Ranged)
- **Paper**: Counter (reflects offensive)
- **Scissors**: Defense (blocks most offensive, broken by Smash/Windmill)

The goal is to DEEPEN these interactions without overcomplicating the core loop.

---

## Expansion Categories

### 1. Multi-Choice Balancing

**Complexity**: ⭐⭐
**Estimated Time**: 2-3 weeks
**Mabinogi Alignment**: ⭐⭐⭐⭐⭐

#### 1.1 Five-Skill Offensive Triangle

Create a "Lizard-Spock" variant within offensive skills where each beats 2 others:

```
        Attack
       /      \
   Lunge      Smash
      \        /
      Windmill
         |
    RangedAttack
```

**Example Interaction Updates**:
- Attack beats Lunge + RangedAttack
- Lunge beats Smash + Windmill
- Smash beats Attack + RangedAttack
- Windmill beats Attack + Lunge
- RangedAttack beats Lunge + Smash

**Implementation**:
- Update `CombatInteractionManager.ResolveOffensiveVsOffensive()`
- Add triangular priority checks before Speed resolution
- Keep Dexterity as tiebreaker for unspecified matchups

**Design Considerations**:
- Maintains current Defense/Counter interactions
- Speed resolution still applies when no priority exists
- Creates deeper skill selection meta
- Risk: May complicate learning curve

---

#### 1.2 Asymmetric Power Picks

Add "strong but counterable" skills inspired by "Fire beats everything except Water":

**New Skill Concept**: **Devastate**
- Charge Time: 3.5s (longest in game)
- Stamina Cost: 10 (double Windmill)
- Effect: Beats ALL offensive skills except one specific counter
- Vulnerabilities: Defense → Instant stun, Counter → Triple reflection damage
- Visual Tell: Massive particle buildup during charge

**Implementation**:
- Add to SkillType enum
- Create asymmetric interaction rules
- Balance with high resource cost + long charge
- Add unique VFX/SFX to telegraph danger

**Design Considerations**:
- High risk/reward gameplay
- Creates "oh shit" moments
- Requires opponent awareness and reaction
- Natural disadvantage state for attacker if predictable

---

### 2. Pattern Exploitation

**Complexity**: ⭐⭐⭐⭐
**Estimated Time**: 4-6 weeks
**Mabinogi Alignment**: ⭐⭐

#### 2.1 Iocaine Powder Algorithm (AI Learning)

Implement history-matching AI that adapts to player patterns:

**Algorithm Overview**:
1. Track last N player skill choices (e.g., N=10)
2. Detect sequences/patterns (e.g., "Attack → Smash → Defense")
3. Predict next choice based on historical frequency
4. Counter predicted choice with optimal skill
5. Decay prediction confidence over time

**Data Structure**:
```csharp
public class PatternTracker
{
    private Queue<SkillType> playerHistory = new Queue<SkillType>(10);
    private Dictionary<string, int> sequenceFrequency = new Dictionary<string, int>();
    private float confidenceThreshold = 0.6f;

    public SkillType? PredictNextSkill(int sequenceLength = 3)
    {
        // Generate sequence key from last N-1 skills
        // Look up most frequent next skill
        // Return prediction if confidence > threshold
    }
}
```

**Integration Point**: `SimpleTestAI.cs` → Add pattern analysis before skill selection

**Design Considerations**:
- Balance between exploitable and frustrating
- Show visual cues when AI "reads" player (e.g., opponent glowing eyes)
- Allow player to break patterns intentionally
- Use for boss AI, not basic enemies

---

#### 2.2 Frequency Analysis Counter-Strategy

Player-facing system that shows their own pattern weaknesses:

**Implementation**:
- Post-combat stats screen showing skill usage percentages
- Heat map of common sequences
- Suggestions for diversifying strategy
- "Predictability Score" metric

**UI Mockup**:
```
Combat Report:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Most Used: Attack (45%) ⚔️
Least Used: Windmill (3%) 🌪️

Common Pattern: Attack → Attack → Smash
Predictability: ⚠️ 73% (High)

Tip: Mix in Counter more often!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Design Considerations**:
- Educational, not punishing
- Encourages skill diversity
- Opt-in feature for hardcore players
- Could be tied to in-game training system

---

### 3. Visual Tells & Mind Games

**Complexity**: ⭐⭐⭐
**Estimated Time**: 3-4 weeks
**Mabinogi Alignment**: ⭐⭐⭐⭐

#### 3.1 Charge VFX Skill Identification

Make charge animations visually distinct so opponents can identify incoming skill:

**Current State**: Generic charge glow
**Proposed Enhancement**:
- **Attack**: Fast white flash
- **Smash**: Red upward slash particles
- **Windmill**: Spinning blue wind effect
- **Lunge**: Yellow directional thrust particles
- **Defense**: Green shield bubble formation
- **Counter**: Purple swirling aura

**Implementation**:
- Create VFX prefabs for each skill
- Spawn during `SkillExecutionState.Charging`
- Destroy on state transition
- Add to `SkillSystem.StartCharging()`

**Code Reference**: `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs:299-350`

**Design Considerations**:
- Adds counterplay depth (opponent can react mid-charge)
- Maintains Mabinogi's "telegraphed combat" feel
- Balances charged skills vs instant skills
- Creates "bait and switch" potential with cancel system

---

#### 3.2 Feint/Cancel System

Allow players to cancel charging skills to bait reactions:

**Mechanics**:
- Press opposite input during charge to cancel (e.g., Defense during Smash charge)
- Refunds 50% stamina
- 0.3s cooldown before next action
- Brief "stumble" animation to prevent spam

**Strategic Use Cases**:
- Charge Smash → Opponent uses Defense → Cancel → Use Attack (Defense is on cooldown)
- Charge Defense → Opponent switches to Smash → Cancel → Use Counter

**Implementation**:
```csharp
// In SkillSystem.cs
private void CheckForCancel()
{
    if (currentState == SkillExecutionState.Charging && Input.GetKeyDown(cancelKey))
    {
        float refund = currentSkill.staminaCost * 0.5f;
        stamina.Add(refund);

        SetState(SkillExecutionState.Stumble);
        StartCoroutine(StumbleRecovery(0.3f));

        Debug.Log($"{gameObject.name} cancelled {currentSkill.type}!");
    }
}
```

**Design Considerations**:
- High skill ceiling mind games
- Risk: Can feel "fake" or non-committal
- Requires animation work
- May frustrate new players

---

### 4. Momentum Mechanics

**Complexity**: ⭐⭐⭐
**Estimated Time**: 2-3 weeks
**Mabinogi Alignment**: ⭐⭐⭐

#### 4.1 Advantage/Disadvantage States

Winner of interaction gains temporary buff, loser gets debuff:

**Advantage State** (3s duration):
- +10% movement speed
- -0.2s charge time on next skill
- Visual: Green aura

**Disadvantage State** (3s duration):
- -10% movement speed
- +0.2s charge time on next skill
- Visual: Red aura

**Implementation**:
```csharp
// In CombatInteractionManager.cs
private void ApplyMomentum(Combatant winner, Combatant loser)
{
    winner.statusEffectManager.ApplyStatusEffect(
        new StatusEffect(StatusEffectType.Advantage, 3.0f)
    );

    loser.statusEffectManager.ApplyStatusEffect(
        new StatusEffect(StatusEffectType.Disadvantage, 3.0f)
    );
}
```

**Design Considerations**:
- Snowball prevention: Keep durations short
- Comeback mechanics: Loser gets increased CC decay?
- Visual clarity: Must be obvious who has momentum
- Mabinogi doesn't have this, but fits combat flow

---

#### 4.2 Hot Streak Bonuses

Consecutive interaction wins grant escalating bonuses:

**Streak Tiers**:
- **2 wins**: "On Fire" → +5% damage
- **3 wins**: "Unstoppable" → +10% damage, +0.5 stamina regen/sec
- **4 wins**: "God Mode" → +15% damage, +1.0 stamina regen/sec, immune to stun
- **Loss**: Reset to 0

**Visual Feedback**:
- Particle intensity increases with streak
- Screen border glow for player
- Announcer voice lines (optional)

**Implementation**:
```csharp
public class StreakTracker : MonoBehaviour
{
    private int currentStreak = 0;
    private float damageMultiplier = 1.0f;
    private float staminaBonus = 0f;

    public void OnInteractionWin()
    {
        currentStreak++;
        UpdateBonuses();
    }

    public void OnInteractionLoss()
    {
        currentStreak = 0;
        UpdateBonuses();
    }

    private void UpdateBonuses()
    {
        damageMultiplier = 1.0f + (currentStreak * 0.05f);
        staminaBonus = currentStreak >= 3 ? (currentStreak - 2) * 0.5f : 0f;
    }
}
```

**Design Considerations**:
- Very gamey, not Mabinogi-like
- Can feel unfair in PvP
- Good for PvE power fantasy
- Needs clear counter-strategies

---

### 5. Conditional Outcomes

**Complexity**: ⭐⭐⭐⭐
**Estimated Time**: 4-5 weeks
**Mabinogi Alignment**: ⭐⭐⭐⭐⭐

#### 5.1 State-Dependent Interactions

Interaction outcomes change based on character states:

**Example Rules**:
- Smash vs Defense while defender is at >80% knockdown meter → Defense breaks
- Counter vs Attack while attacker has Advantage state → Counter fails
- Lunge vs Defense while defender is at <20% stamina → Lunge breaks through
- RangedAttack vs Counter from >6 units away → Counter can't reflect

**Implementation**:
```csharp
// In CombatInteractionManager.cs
private InteractionResult ResolveWithContext(
    SkillExecution offensive,
    SkillExecution defensive,
    CombatContext context)
{
    // Check meter thresholds
    if (context.defenderMeterPercentage > 0.8f &&
        offensive.skillType == SkillType.Smash &&
        defensive.skillType == SkillType.Defense)
    {
        return InteractionResult.DefenseBroken;
    }

    // Check distance
    if (context.distance > 6.0f &&
        offensive.skillType == SkillType.RangedAttack &&
        defensive.skillType == SkillType.Counter)
    {
        return InteractionResult.TooFarToCounter;
    }

    // Fall back to standard resolution
    return ResolveStandard(offensive.skillType, defensive.skillType);
}
```

**Design Considerations**:
- VERY Mabinogi-aligned (context-sensitive combat)
- Requires extensive testing for edge cases
- Needs clear UI feedback ("Defense weakened!")
- High skill ceiling, high learning curve

---

#### 5.2 Positional Interactions

Backstabs, flanks, and facing-dependent outcomes:

**Mechanics**:
- **Backstab**: Attack from behind → 150% damage, no defense possible
- **Flank**: Attack from 90° angles → 125% damage, defense effectiveness reduced
- **Face-to-Face**: Normal rules apply

**Detection**:
```csharp
public enum AttackAngle
{
    Front,    // 0-45° from forward
    Flank,    // 45-135° from forward
    Back      // 135-180° from forward
}

public AttackAngle GetAttackAngle(Transform attacker, Transform defender)
{
    Vector3 toAttacker = (attacker.position - defender.position).normalized;
    float angle = Vector3.Angle(defender.forward, toAttacker);

    if (angle < 45f) return AttackAngle.Front;
    if (angle < 135f) return AttackAngle.Flank;
    return AttackAngle.Back;
}
```

**Design Considerations**:
- Encourages positioning and movement
- Mabinogi doesn't have this, but fits the genre
- Risk: Can feel unfair if not well-telegraphed
- Requires camera/control adjustments for lock-on

---

### 6. Interaction Chains

**Complexity**: ⭐⭐⭐⭐⭐
**Estimated Time**: 6-8 weeks
**Mabinogi Alignment**: ⭐⭐⭐

#### 6.1 Riposte Windows

After successful Counter, attacker is vulnerable to follow-up:

**Mechanics**:
- Counter success → Attacker enters "Reeling" state (1.5s)
- Defender can execute instant "Riposte" attack (0s charge time)
- Riposte deals 150% damage and cannot be defended
- Only available immediately after Counter success

**Implementation**:
```csharp
// In CombatInteractionManager.cs
private void ProcessCounterSuccess(Combatant attacker, Combatant defender)
{
    // Apply reflection damage
    ApplyReflectionDamage(attacker, defender);

    // Apply Reeling state to attacker
    attacker.statusEffectManager.ApplyStatusEffect(
        new StatusEffect(StatusEffectType.Reeling, 1.5f)
    );

    // Grant Riposte opportunity to defender
    defender.skillSystem.GrantRiposteOpportunity(1.5f);
}

// In SkillSystem.cs
private bool hasRiposteOpportunity = false;
private float riposteTimer = 0f;

public void GrantRiposteOpportunity(float duration)
{
    hasRiposteOpportunity = true;
    riposteTimer = duration;
}

private void HandleRiposteInput()
{
    if (hasRiposteOpportunity && Input.GetKeyDown(attackKey))
    {
        ExecuteRiposte();
        hasRiposteOpportunity = false;
    }
}

private void ExecuteRiposte()
{
    // Instant attack, no charge, 150% damage
    SkillExecution riposte = new SkillExecution(
        SkillType.Attack,
        this,
        instantExecution: true,
        damageMultiplier: 1.5f
    );

    CombatInteractionManager.Instance.ProcessSkillExecution(riposte);
}
```

**Design Considerations**:
- High skill ceiling combo system
- Rewards successful defensive play
- Risk: Can feel "combo-locked" for attacker
- Requires clear visual/audio cues

---

#### 6.2 Finisher Opportunities

Low HP or high CC meter creates "finisher" states:

**Trigger Conditions**:
- Target at <20% HP AND knocked down
- Target at 100% knockdown meter AND knocked down

**Finisher Properties**:
- Instant execution (press F near downed target)
- Guaranteed kill if successful
- Long animation (2.5s, can be interrupted)
- Grants bonus rewards (extra XP/loot)

**Implementation**:
```csharp
// In HealthSystem.cs
public bool IsFinishable => currentHealth < maxHealth * 0.2f && statusEffectManager.IsKnockedDown;

// In SkillSystem.cs
private void CheckForFinisherOpportunity()
{
    Collider[] nearby = Physics.OverlapSphere(transform.position, 2.5f);
    foreach (var collider in nearby)
    {
        var target = collider.GetComponent<HealthSystem>();
        if (target != null && target.IsFinishable && Input.GetKeyDown(finisherKey))
        {
            ExecuteFinisher(target);
        }
    }
}

private void ExecuteFinisher(HealthSystem target)
{
    // Play dramatic animation
    // Lock both characters
    // Deal guaranteed kill damage after animation
    // Grant bonus rewards
}
```

**Design Considerations**:
- "Glory kill" mechanic from modern games
- High drama and power fantasy
- NOT Mabinogi-like at all
- Risk: Slows down combat pacing
- Needs "mercy" option for honorable players

---

### 7. New Skills

**Complexity**: ⭐⭐⭐ per skill
**Estimated Time**: 1-2 weeks per skill
**Mabinogi Alignment**: ⭐⭐⭐⭐⭐

#### 7.1 Parry (Precision Defense)

**Concept**: Frame-perfect defense with enhanced reward

**Properties**:
- Charge Time: 0.3s (very short window)
- Stamina Cost: 2 (cheap)
- Success: Instant stun + riposte opportunity
- Failure: 0.5s vulnerability
- Timing Window: 0.2s (strict)

**Interaction Rules**:
- vs Offensive (perfect timing): Attacker stunned, defender gets riposte
- vs Offensive (late): Defender takes full damage
- vs Smash/Windmill: No effect (can't parry heavy attacks)

**Implementation**:
```csharp
// Add to SkillType enum
SkillType.Parry

// In SkillSystem.cs
private void ExecuteParry()
{
    float windowStart = Time.time;
    float windowEnd = windowStart + 0.2f;

    StartCoroutine(ParryWindow(windowStart, windowEnd));
}

private IEnumerator ParryWindow(float start, float end)
{
    while (Time.time < end)
    {
        if (CheckIncomingAttack())
        {
            // Perfect parry!
            TriggerParrySuccess();
            yield break;
        }
        yield return null;
    }

    // Missed the window
    TriggerParryFailure();
}
```

**Design Considerations**:
- High skill ceiling alternative to Defense
- Rewards timing and prediction
- Very Mabinogi-like (frame-perfect defense exists)
- Risk: Can feel "cheap" if too strong

---

#### 7.2 Guard Break (Anti-Defense)

**Concept**: Offensive skill specifically designed to beat Defense/Counter

**Properties**:
- Charge Time: 1.8s
- Stamina Cost: 5
- Range: 2.5
- Effect: Breaks through Defense, stuns Counter users

**Interaction Rules**:
- vs Defense: Defense broken, defender stunned, attacker follows with free Attack
- vs Counter: Counter interrupted, defender stunned
- vs Offensive: Loses (slow charge time)
- vs Nothing: Misses, long recovery

**Implementation**:
```csharp
// Add to SkillType enum
SkillType.GuardBreak

// In CombatInteractionManager.cs
case SkillType.GuardBreak:
    switch (defensive)
    {
        case SkillType.Defense:
            return InteractionResult.DefenseBroken;
        case SkillType.Counter:
            return InteractionResult.CounterInterrupted;
        default:
            return InteractionResult.GuardBreakMiss;
    }
```

**Design Considerations**:
- Creates rock-paper-scissors-lizard dynamic
- Punishes defensive turtling
- Mabinogi has similar skills (Shield Bash)
- Risk: Makes Defense less viable if too strong

---

#### 7.3 Provoke (Taunt Mechanic)

**Concept**: Force opponent to attack you, buff defense temporarily

**Properties**:
- Charge Time: 1.0s
- Stamina Cost: 4
- Range: 5.0 (target radius)
- Effect: Target forced to use offensive skill, caster gains +50% defense for 3s

**Interaction Rules**:
- vs AI: Forces target to prioritize caster
- vs Player: UI prompt "Provoked! Attack or resist?" (resist costs stamina)
- Combo with Counter for guaranteed reflection

**Implementation**:
```csharp
// Add to SkillType enum
SkillType.Provoke

// In StatusEffectManager.cs
public enum StatusEffectType
{
    // ... existing types
    Provoked,    // Forced to attack provoker
    Fortified    // +50% defense from Provoke
}

// In SimpleTestAI.cs
private void CheckProvokedState()
{
    if (statusEffectManager.HasStatusEffect(StatusEffectType.Provoked))
    {
        Transform provoker = GetProvokerTarget();
        if (provoker != null)
        {
            // Force attack on provoker
            skillSystem.ExecuteSkill(SkillType.Attack);
            facingTarget = provoker;
        }
    }
}
```

**Design Considerations**:
- Enables tank/support playstyles
- PvE-focused, PvP-questionable
- Not Mabinogi-like (no taunts in Mabi)
- Risk: Removes player agency

---

#### 7.4 Adapt (Dynamic Counter)

**Concept**: Automatically counters opponent's last used skill

**Properties**:
- Charge Time: 1.2s
- Stamina Cost: 6
- Effect: Learns opponent's last skill, executes perfect counter
- Cooldown: 10s

**Interaction Rules**:
- Tracks opponent's last offensive skill
- Automatically selects optimal counter:
  - vs Attack/Lunge → Use Counter
  - vs Smash/Windmill → Use evasion
  - vs RangedAttack → Use block
- If opponent hasn't attacked yet → Use Defense

**Implementation**:
```csharp
// Add to SkillType enum
SkillType.Adapt

// In SkillSystem.cs
private SkillType lastOpponentSkill = SkillType.None;

public void ExecuteAdapt()
{
    SkillType counterSkill = DetermineCounterSkill(lastOpponentSkill);

    Debug.Log($"{gameObject.name} adapted to {lastOpponentSkill} with {counterSkill}!");

    ExecuteSkill(counterSkill);
}

private SkillType DetermineCounterSkill(SkillType opponentSkill)
{
    return opponentSkill switch
    {
        SkillType.Attack => SkillType.Counter,
        SkillType.Lunge => SkillType.Counter,
        SkillType.Smash => SkillType.Defense, // Will break, but better than nothing
        SkillType.Windmill => SkillType.Defense,
        SkillType.RangedAttack => SkillType.Defense,
        _ => SkillType.Defense
    };
}
```

**Design Considerations**:
- "Adaptive" mechanic from fighting games
- Rewards observation and memory
- Not Mabinogi-like at all
- Risk: Can feel "auto-pilot" if too strong
- Better for boss AI than player skill

---

## Implementation Roadmap

### Phase 1: Core Enhancements (4-6 weeks)

**Goal**: Deepen existing systems without new skills

1. **Visual Tells (3 weeks)**
   - Create VFX for each skill's charge animation
   - Update SkillSystem to spawn particles
   - Test visual clarity

2. **Feint/Cancel System (2 weeks)**
   - Implement cancel input detection
   - Add stumble state and animation
   - Balance stamina refund values

3. **Advantage/Disadvantage States (1 week)**
   - Add momentum status effects
   - Hook into interaction outcomes
   - Create visual indicators

**Deliverable**: Combat feels more reactive and skill-expressive

---

### Phase 2: Pattern & Adaptation (6-8 weeks)

**Goal**: Add learning AI and player feedback

1. **Pattern Tracker (4 weeks)**
   - Implement history tracking system
   - Build sequence analysis algorithms
   - Create prediction confidence system

2. **AI Integration (2 weeks)**
   - Hook pattern tracker into SimpleTestAI
   - Add boss-tier AI that uses predictions
   - Visual cues for "reading" player

3. **Player Feedback (2 weeks)**
   - Post-combat stats screen
   - Pattern visualization
   - Predictability score

**Deliverable**: AI feels intelligent, players learn from their mistakes

---

### Phase 3: New Skills & Interactions (8-10 weeks)

**Goal**: Expand skill roster and deepen RPS dynamics

1. **Parry Skill (2 weeks)**
   - Implement frame-perfect defense
   - Add timing window detection
   - Balance risk/reward

2. **Guard Break Skill (2 weeks)**
   - Add anti-defense mechanic
   - Update interaction matrix
   - Test Defense viability

3. **Conditional Outcomes (4 weeks)**
   - State-dependent interaction resolution
   - Positional attack system
   - Context-aware damage calculation

**Deliverable**: 9-skill roster with deeper strategic options

---

### Phase 4: Advanced Mechanics (6-8 weeks)

**Goal**: High-complexity systems for veteran players

1. **Interaction Chains (4 weeks)**
   - Riposte system after Counter
   - Finisher opportunities
   - Combo UI and feedback

2. **Multi-Choice Balancing (2 weeks)**
   - Five-skill offensive triangle
   - Update SpeedResolver priority checks
   - Balance testing

3. **Asymmetric Power Picks (2 weeks)**
   - Devastate skill concept
   - High-risk/high-reward balancing
   - Visual polish

**Deliverable**: Deep, replayable combat system with high skill ceiling

---

## Code Architecture References

### Key Files for Expansion

| File | Lines | Purpose | Expansion Notes |
|------|-------|---------|----------------|
| `SkillSystem.cs` | 1-1200+ | Skill execution FSM | Add new skills here, extend charge/aim logic |
| `CombatInteractionManager.cs` | 1-800+ | Interaction resolution | Update matrix here, add conditional logic |
| `StatusEffectManager.cs` | 1-373 | CC management | Add new status types (Advantage, Reeling, etc.) |
| `KnockdownMeterTracker.cs` | 1-247 | Meter buildup | Modify buildup formulas, add conditional thresholds |
| `SpeedResolver.cs` | N/A | Dexterity resolution | Add triangular priority checks before Speed |
| `SimpleTestAI.cs` | 1-600+ | AI decision-making | Integrate pattern tracker, add adaptive strategies |
| `CharacterInfoDisplay.cs` | 1-298 | UI rendering | Add new visual cues (streak, momentum, riposte) |

### Critical Constants

**File**: `CombatConstants.cs` (location unknown, needs creation?)

```csharp
public static class CombatConstants
{
    // Skill Properties
    public const float ATTACK_CHARGE_TIME = 0f;
    public const float DEFENSE_CHARGE_TIME = 1.0f;
    public const float COUNTER_CHARGE_TIME = 1.0f;
    public const float SMASH_CHARGE_TIME = 2.0f;
    public const float WINDMILL_CHARGE_TIME = 2.0f;
    public const float LUNGE_CHARGE_TIME = 1.5f;

    // Ranges
    public const float MELEE_RANGE = 2.0f;
    public const float SMASH_RANGE = 2.5f;
    public const float WINDMILL_RANGE = 3.0f;
    public const float RANGED_RANGE = 10.0f;
    public const float LUNGE_MIN_RANGE = 2.0f;
    public const float LUNGE_MAX_RANGE = 4.0f;

    // Knockdown Meter
    public const float KNOCKDOWN_METER_THRESHOLD = 100f;
    public const float KNOCKBACK_METER_THRESHOLD = 50f;
    public const float ATTACK_KNOCKDOWN_BUILDUP = 5f;
    public const float KNOCKDOWN_METER_DECAY_RATE = -5f;

    // CC Durations
    public const float KNOCKBACK_DURATION = 0.5f;
    public const float BASE_STUN_DURATION = 2.0f;
    public const float BASE_KNOCKDOWN_DURATION = 3.0f;

    // Stats
    public const float STRENGTH_KNOCKDOWN_DIVISOR = 10f;
    public const float FOCUS_STATUS_RECOVERY_DIVISOR = 30f;

    // Displacement
    public const float KNOCKBACK_DISPLACEMENT_DISTANCE = 2.0f;
    public const float METER_KNOCKBACK_DISTANCE = 3.0f;
    public const float LUNGE_DASH_DISTANCE = 2.0f;
}
```

### Event System Structure

**Pattern**: Observer pattern via C# Events

```csharp
// Publisher (SkillSystem.cs)
public event Action<SkillType, SkillExecutionState> OnSkillStateChanged;

// Subscriber (CharacterInfoDisplay.cs)
skillSystem.OnSkillStateChanged += HandleSkillStateChanged;

// Invocation
OnSkillStateChanged?.Invoke(skillType, state);
```

**Expansion**: Add new events for momentum, streaks, ripostes
```csharp
public event Action<int> OnStreakChanged;
public event Action<bool> OnMomentumShift; // true = advantage, false = disadvantage
public event Action OnRiposteOpportunity;
```

### ICombatUpdatable Interface

**File**: `ICombatUpdatable.cs` (unknown location)

```csharp
public interface ICombatUpdatable
{
    void CombatUpdate(float deltaTime);
}
```

**Registered Components**:
- SkillSystem
- StatusEffectManager
- KnockdownMeterTracker
- (Future) PatternTracker, StreakTracker, etc.

**Expansion**: All new systems should implement this interface and register with `CombatUpdateManager`

---

## Design Philosophy

### Core Principles

1. **Depth Over Complexity**
   - Add strategic layers, not mechanical bloat
   - Every new mechanic should create interesting decisions
   - Avoid "noob trap" skills that are strictly worse

2. **Mabinogi DNA**
   - Maintain rock-paper-scissors core
   - Telegraphed, turn-based feel
   - Context-sensitive outcomes
   - No juggle combos or cancel-heavy systems

3. **Respect Player Time**
   - Clear visual feedback for all mechanics
   - Avoid frustrating "gotcha" moments
   - Allow players to learn by doing, not reading manuals

4. **Balanced Skill Floor/Ceiling**
   - New players can win with basic Attack/Defense/Smash
   - Veterans have advanced techniques (parry timing, pattern exploitation)
   - No mandatory execution barriers (frame-perfect inputs okay if optional)

5. **Asymmetric Balance**
   - Some skills should be "better" in specific contexts
   - Avoid homogenization (every skill shouldn't feel the same)
   - Create archetypes (aggressive, defensive, adaptive)

### Anti-Patterns to Avoid

❌ **Fighting Game Execution Barriers**
- No quarter-circle inputs or complex button sequences
- No frame-perfect links required for basic play
- Avoid "option select" tech that trivializes decisions

❌ **MOBA Power Creep**
- Don't add skills just to add skills
- Every skill should have clear purpose and counterplay
- Avoid "does everything" swiss army knife skills

❌ **Dark Souls Difficulty Spikes**
- Don't punish players for not knowing obscure mechanics
- Telegraph danger clearly
- Allow learning through failure without excessive penalty

❌ **Overwatch Rock-Paper-Scissors Hell**
- Don't create hard counters where skill doesn't matter
- Always allow outplay potential through positioning/timing
- Avoid "you picked wrong, you lose" scenarios

### Expansion Evaluation Criteria

Before implementing any new feature, ask:

1. **Does this create interesting decisions?** (not just more options)
2. **Can new players understand it within 30 seconds?** (visual clarity)
3. **Does it respect existing skill investments?** (don't obsolete old tech)
4. **Is the counterplay obvious?** (no hidden interactions)
5. **Does it fit Mabinogi's vibe?** (telegraphed, turn-based, context-aware)

If 3+ answers are "no", reconsider the feature.

---

## RPS Theory Corner

### The Iocaine Powder Principle

**Origin**: From "The Princess Bride" - both players try to outwit each other's prediction of their strategy.

**Application**:
- Level 1: Player uses Attack often → AI uses Defense more
- Level 2: Player realizes AI using Defense more → Player uses Smash more
- Level 3: AI realizes player using Smash more → AI uses Counter more
- Level 4: Player realizes AI using Counter more → Player uses Attack more
- Level ∞: Recursive prediction spiral

**Implementation Strategy**:
- Track player's recent history (last 5-10 actions)
- AI operates at "Level 2" (counter last 5 action distribution)
- Boss AI operates at "Level 3" (counter predicted next action based on sequence)
- Never go full Level 4 (too frustrating)

**Code Sketch**:
```csharp
public SkillType SelectAISkill(List<SkillType> playerHistory)
{
    // Count frequency of each skill
    var frequency = playerHistory.GroupBy(s => s)
                                 .ToDictionary(g => g.Key, g => g.Count());

    // Find most common skill
    SkillType mostCommon = frequency.OrderByDescending(kv => kv.Value).First().Key;

    // Select counter to most common
    return GetCounterSkill(mostCommon);
}
```

---

### Nash Equilibrium vs Exploitation

**Nash Equilibrium**: In pure RPS, optimal strategy is 1/3, 1/3, 1/3 random distribution. This is unexploitable but also non-exploitative.

**Exploitation Strategy**: Deviate from Nash to exploit opponent's patterns. Higher reward, higher risk.

**Application**:
- **Tutorial AI**: Pure Nash (randomized, teaches fundamentals)
- **Early Game AI**: Slight exploitation (60% counter player's most common, 40% Nash)
- **Boss AI**: Heavy exploitation (80% pattern prediction, 20% Nash for safety)
- **PvP**: Pure player skill, no AI interference

**Balance Consideration**: Players should be able to "beat" prediction AI by intentionally randomizing their play. Nash equilibrium should always be viable, just not optimal.

---

### Visual Tells & Information Asymmetry

**Perfect Information RPS**: Both players choose simultaneously in blind selection. Boring, pure luck.

**Imperfect Information RPS**: One player reveals choice first (charge animation), other player reacts. Creates skill expression.

**Application**:
- **Charged Skills**: Long telegraph, opponent can react mid-charge
- **Instant Skills**: No telegraph, use for mixups
- **Feints**: Show charge VFX, cancel before commitment → mind games

**Risk/Reward Balance**:
- Charged skills = more damage/utility but reactable
- Instant skills = less power but unreactable
- Feints = high skill ceiling mind games

**Implementation**:
```csharp
// In SkillSystem.cs
public float GetChargeTime(SkillType skill)
{
    return skill switch
    {
        SkillType.Attack => 0f,           // Instant, unreactable
        SkillType.Defense => 1.0f,        // Short charge, some react time
        SkillType.Smash => 2.0f,          // Long charge, high react time
        SkillType.Devastate => 3.5f,      // Very long charge, huge react time
        _ => 0.5f
    };
}

public float GetDamageMultiplier(SkillType skill)
{
    return skill switch
    {
        SkillType.Attack => 1.0f,         // Low damage
        SkillType.Smash => 1.5f,          // High damage (justified by long charge)
        SkillType.Devastate => 2.5f,      // Huge damage (justified by huge charge)
        _ => 1.0f
    };
}
```

**Design Principle**: **Charge Time × Risk = Power × Reward**

---

### Momentum & Muk-Jji-Ppa

**Muk-Jji-Ppa**: Korean RPS variant where ties continue the round with accumulated stakes. Winner takes all.

**Application**: Advantage/Disadvantage states accumulate over consecutive interaction wins.

**Expansion Idea**: "Escalation Mode"
- Each tie (Speed resolution) increases next interaction's stakes
- 1st tie: +0% damage
- 2nd tie: +25% damage
- 3rd tie: +50% damage
- 4th tie: +100% damage + guaranteed knockdown

**Implementation**:
```csharp
private int consecutiveTies = 0;

private float GetEscalationMultiplier()
{
    return 1.0f + (consecutiveTies * 0.25f);
}

private void OnInteractionTie()
{
    consecutiveTies++;
    if (consecutiveTies >= 4)
    {
        // Next hit is a "super" hit
        ApplyEscalationBonus();
    }
}

private void OnInteractionResolved()
{
    consecutiveTies = 0;
}
```

**Design Consideration**: High-stakes drama, but can snowball. Keep escalation capped at 3-4 ties max.

---

## Mabinogi Alignment

### What Makes Mabinogi Combat Unique

1. **Telegraphed Turns**
   - All actions have clear start/end
   - No "true combos" or animation cancels
   - Turn-based feel despite real-time execution

2. **Rock-Paper-Scissors Core**
   - Smash beats Defense
   - Defense beats Attack
   - Counter beats Smash
   - (Extensions: Windmill beats multiples, etc.)

3. **Context-Sensitive Outcomes**
   - Skills behave differently based on target state
   - Knockdown system adds layers to basic RPS
   - Positioning matters (for some skills like Charge)

4. **Skill Preparation**
   - Charge phase is commitment and telegraph
   - No instant "I win" buttons
   - Opponent has time to react

5. **Stamina Economy**
   - Limited resource prevents spam
   - Forces strategic skill selection
   - Creates risk/reward tension

### Mabinogi-Aligned Expansions

✅ **Parry** - Frame-perfect defense exists in Mabi (Defense timing)
✅ **Guard Break** - Mabi has skills that break Defense (Shield Bash)
✅ **Conditional Outcomes** - Mabi has state-dependent damage (Critical hits)
✅ **Visual Tells** - Mabi has clear charge animations
✅ **Lunge/Charge** - Literally in Mabinogi

### Non-Mabinogi Expansions (Use Cautiously)

❌ **Combo Chains** - Mabi has NO juggle combos
❌ **Finishers** - Mabi has no QTE-style executions
❌ **Hot Streak Bonuses** - Mabi doesn't track consecutive wins
❌ **Provoke** - Mabi has no taunt mechanics
❌ **Adapt** - Mabi has no "auto-counter" skills

**Guideline**: If you can find a Mabinogi skill that does something similar, it's probably aligned. If it feels like it's from a different game (God of War, Devil May Cry, WoW), reconsider.

---

## Spicy Variants

*These ideas are intentionally wild, experimental, and potentially terrible. Use at your own risk.*

### 🌶️ Skill Roulette Mode

**Concept**: Players don't choose their skills - the game randomly assigns them per round.

**Mechanics**:
- Every 10 seconds, each player is randomly given one of 7 skills
- Must use that skill or forfeit the round
- Encourages adaptation over memorization

**Why It's Spicy**: Removes skill expression, but hilarious for party mode.

---

### 🌶️ Mirror Match Curse

**Concept**: If both players use the same skill simultaneously, both are cursed.

**Mechanics**:
- Same skill at same time → Both take damage + stun
- Curse Duration: 5s (cannot use that skill again)
- Encourages diversification

**Why It's Spicy**: Punishes coincidence, can feel arbitrary.

---

### 🌶️ Combo Breaker

**Concept**: If one player wins 3 interactions in a row, opponent gets instant "Combo Breaker" skill.

**Mechanics**:
- CB skill: Instant, unavoidable stun + knockdown
- One-time use per streak
- Resets after use

**Why It's Spicy**: Rubber-banding mechanic, rewards losing.

---

### 🌶️🌶️ Skill Draft Mode

**Concept**: Before combat, players ban 2 skills each, then draft remaining skills.

**Mechanics**:
- Player 1 bans 1 skill
- Player 2 bans 1 skill
- Player 1 bans 1 skill
- Player 2 bans 1 skill
- Player 1 picks 3 skills
- Player 2 picks 3 skills
- 1 skill remains unpicked

**Why It's Spicy**: MOBA-style draft, way too complex for casual play.

---

### 🌶️🌶️ Asymmetric Boss Mode

**Concept**: One player is "Boss" with overpowered skills, 3 other players are heroes with standard skills.

**Mechanics**:
- Boss has 3× HP, 2× damage, exclusive skills (Devastate, Adapt, etc.)
- Heroes can revive each other
- Boss wins by killing all heroes, heroes win by depleting boss HP

**Why It's Spicy**: Requires 4 players, asymmetric balance is hard.

---

### 🌶️🌶️🌶️ RPS²: Meta-Prediction Layer

**Concept**: Before each round, players predict opponent's next move. Correct prediction grants bonus.

**Mechanics**:
- Pre-round: Secretly predict opponent's skill (5s timer)
- Execute skills normally
- Correct prediction: +50% damage, -50% charge time
- Incorrect prediction: No penalty

**Why It's Spicy**: Doubles the decision space, incredibly slow pacing.

---

### 🌶️🌶️🌶️ Quantum Superposition

**Concept**: Skills exist in "superposition" until observed by opponent.

**Mechanics**:
- Charge any skill with generic VFX
- On release, game randomly assigns one of 3 pre-selected skills
- Opponent doesn't know which until it hits
- Player also doesn't know until release (true randomness)

**Why It's Spicy**: Removes all agency, pure chaos mode.

---

### 🌶️🌶️🌶️🌶️ Friendly Fire + Team RPS

**Concept**: 2v2 where you can hit your teammate.

**Mechanics**:
- All skills affect everyone in range (including ally)
- Team coordination required to avoid hitting each other
- Smash can kill your own teammate

**Why It's Spicy**: Griefing simulator, relationship destroyer.

---

### 🌶️🌶️🌶️🌶️ Permadeath Skills

**Concept**: Each skill can only be used once per match.

**Mechanics**:
- 7 skills, 7 uses total
- After using a skill, it's gone forever (for that match)
- Final round is pure Speed resolution (no skills left)

**Why It's Spicy**: Insane resource management, matches become puzzle games.

---

### 🌶️🌶️🌶️🌶️🌶️ Real-Money RPS

**Concept**: Bet in-game currency on each interaction.

**Mechanics**:
- Before each round, both players wager gold
- Winner takes both wagers
- Can bluff by betting high with weak skill

**Why It's Spicy**: Gambling mechanic, regulatory nightmare, also hilarious.

---

## Conclusion

This document serves as a **living design bible** for FairyGate's combat system expansion. The ideas range from highly practical (visual tells, conditional outcomes) to experimental (pattern exploitation) to deliberately absurd (spicy variants).

**Next Steps**:
1. Review with team/stakeholders
2. Prioritize based on dev time + player impact
3. Prototype Phase 1 enhancements
4. Playtest and iterate
5. Expand skill roster with high-confidence additions (Parry, Guard Break)

**Remember**: Depth over complexity. Every new mechanic should answer "Does this make me think more, or just press more buttons?"

---

**Document Version**: 1.0
**Author**: Claude (FairyGate Combat Designer)
**Feedback**: Open GitHub issue or discuss in #combat-design channel
**"Rock beats scissors, scissors beats paper, paper beats rock. Everything else is just spice."**
