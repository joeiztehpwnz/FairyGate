# N+1 Combo System Design Document
**Status:** Future Implementation (Post Phase 5)
**Priority:** Enhancement (Not Critical Path)
**Classic Mabinogi Feature:** Advanced Combat Mechanic

## Overview

The N+1 Combo System is a classic Mabinogi advanced combat technique that allowed skilled players to extend weapon attack chains by executing skills at precise timing windows. This created a skill ceiling for combo execution and rewarded frame-perfect timing.

### What is N+1?

**N+1** refers to extending an N-hit weapon combo chain by +1 additional hit through precise skill timing.

**Example:**
- Weapon has a 3-hit basic attack combo (N=3)
- Player executes Attack → Attack → Attack → **Smash** (at perfect timing)
- Result: 4-hit combo instead of 3 (N+1 = 4)

This was NOT a "cancel" mechanic - it was a **combo extension** that required frame-perfect timing to chain seamlessly.

## Classic Mabinogi Implementation

### Timing Windows - CRITICAL CORRECTION

⚠️ **IMPORTANT:** The N+1 timing window is based on **enemy stun duration**, NOT the attacker's recovery frames.

**Classic Mabinogi Timing Rule:**
```
Timing: Attack during 70-95% of enemy stun duration
```

**Visualization:**
```
Attack 3 lands → Enemy Stunned (1.0s base stun)
                 [-------- Stun Duration --------]
                 0s      0.7s         0.95s     1.0s
                         [== N+1 Window ==]
                         └─ Execute skill here
```

**Window Characteristics:**
- **Duration:** Approximately 25% of enemy stun time (from 70% to 95%)
- **Timing Reference:** Enemy stun duration, NOT attacker recovery time
- **Absolute Duration:** ~0.1-0.5 seconds depending on weapon speed and enemy stats
- **Result:** Skill executes immediately, combo counter resets

### Stun Duration System

The timing window varies significantly based on **weapon speed** and **enemy stats**:

#### Base Stun Duration by Weapon Speed

```
Very Slow (Claymore, Two-Handed):  2.0+ seconds
Slow (Mace, Hammer):                1.5-2.0 seconds
Normal (Sword, Axe):                1.0-1.2 seconds
Fast (Dagger, Short Sword):         0.3-0.5 seconds
Very Fast (Knuckles, Bare Hands):   0.2-0.3 seconds
```

#### N+1 Window Duration by Weapon Type

```
Very Slow (2.0s stun):  1.4s - 1.9s  → 0.5s window
Slow (1.5s stun):       1.05s - 1.425s → 0.375s window
Normal (1.0s stun):     0.7s - 0.95s → 0.25s window
Fast (0.4s stun):       0.28s - 0.38s → 0.1s window
Very Fast (0.25s stun): 0.175s - 0.2375s → 0.0625s window
```

**Key Insight:** Fast weapons have MUCH tighter N+1 windows despite attacking faster. This balances weapon types - slow weapons are easier to N+1 combo but have longer attack animations.

### Stat Interactions - NEW SECTION

#### Focus Stat (Enemy Defense Against N+1)

Enemy Focus stat **reduces stun duration**, making N+1 timing harder:

```
Actual Stun Duration = Base Stun × (1 - Target Focus / 30)
```

**Examples:**
- **0 Focus (normal enemy):** 100% stun duration → standard N+1 window
- **15 Focus (tough enemy):** 50% stun duration → **50% smaller N+1 window**
- **30 Focus (boss):** 0% stun duration → **N+1 IMPOSSIBLE**

**Impact on N+1 Windows:**
```
Normal Sword (1.0s base stun) vs Different Focus Levels:

0 Focus:  1.0s stun → N+1 window: 0.7s - 0.95s (0.25s window)
15 Focus: 0.5s stun → N+1 window: 0.35s - 0.475s (0.125s window)
30 Focus: 0.0s stun → N+1 window: NONE (impossible to execute)
```

This creates **natural difficulty scaling** - stronger enemies with high Focus are harder to N+1 combo.

#### Critical Hits (Player Advantage for N+1)

Critical hits **extend stun duration**, making N+1 timing easier:

**Effects of Critical Hits:**
- Increased stun duration (exact multiplier varies)
- Larger N+1 window (more forgiving timing)
- Synergy with critical-focused builds

**Strategic Implication:** Critical builds have easier N+1 execution, creating build diversity between precision timing (low crit) vs forgiving windows (high crit).

#### Will Stat (Knockdown Resistance)

Target's Will stat affects **knockdown meter buildup** during N+1 extended combos:

```
Knockdown Accumulation factors:
- Hit number in combo
- Weapon knockdown rate
- Character strength
- Target's Will stat (resistance)
```

**High-Will enemies resist knockdown** even from successful N+1 chains, requiring more combo extensions or different tactics.

### Execution Requirements

1. **Timing Precision:** Execute skill during 70-95% of enemy stun duration
2. **Skill State:** Skill must be fully charged/ready
3. **Stamina:** Must have stamina for the skill
4. **Enemy State:** Enemy must be stunned from your combo hit
5. **Combo Validity:** Must be in active combo sequence (not reset)

### Which Skills Could Extend?

In classic Mabinogi, most offensive skills could extend combos:

**Common N+1 Skills:**
- **Smash** - Most popular (high damage finisher)
- **Windmill** - AoE combo ender
- **Counter** - Advanced defensive extension
- **Defense** - Combo-to-block transition
- **Lunge** - Positional extension (if in range)

**NOT Valid:**
- Basic Attack (would just continue combo normally)
- Skills on cooldown or not charged
- Skills without sufficient stamina

### Strategic Value

**Why N+1 Mattered:**

1. **Damage Optimization** - Extended combo chains for more total damage
2. **Skill Ceiling** - Separated expert players from novices
3. **Flow State** - Rewarded rhythm and timing mastery
4. **Tactical Flexibility** - Could adapt combo endings based on situation
   - Example: Combo into Smash if enemy low HP
   - Example: Combo into Windmill if multiple enemies close
   - Example: Combo into Defense if enemy charging Counter
5. **Stamina Efficiency** - Single combo chain vs separate attack + skill
6. **Build Diversity** - Crit builds = easier timing, precision builds = higher risk

## FairyGate Implementation Design

### Phase 1: Core Detection System

**File:** `Assets/Scripts/Combat/Weapons/WeaponController.cs`

**New Fields:**
```csharp
[Header("N+1 Combo System")]
[SerializeField] private bool enableNPlusOneCombo = true;
[SerializeField] private float nPlusOneWindowStart = 0.7f;    // Start at 70% of stun
[SerializeField] private float nPlusOneWindowEnd = 0.95f;     // End at 95% of stun

private bool isInNPlusOneWindow = false;
private float currentStunProgress = 0f;
private float targetStunDuration = 0f;
```

**Detection Logic:**
```csharp
private void UpdateNPlusOneWindow(float deltaTime)
{
    if (!enableNPlusOneCombo) return;

    // Calculate if we're in the N+1 window during enemy stun
    if (currentAttackIndex == weaponData.comboLength - 1) // Last hit of combo
    {
        // Get the actual stun duration (affected by enemy Focus stat and critical hits)
        targetStunDuration = CalculateStunDuration();

        currentStunProgress += deltaTime / targetStunDuration;

        // Check if in timing window (70%-95% through stun by default)
        if (currentStunProgress >= nPlusOneWindowStart &&
            currentStunProgress <= nPlusOneWindowEnd)
        {
            isInNPlusOneWindow = true;
        }
        else
        {
            isInNPlusOneWindow = false;
        }
    }
}

private float CalculateStunDuration()
{
    float baseStun = weaponData.stunDuration; // Weapon-specific base stun

    // Get target's Focus stat
    float targetFocus = targetCombatController != null
        ? targetCombatController.GetFocusStat()
        : 0f;

    // Apply Focus resistance
    float focusMultiplier = 1f - (targetFocus / 30f);
    float actualStun = baseStun * Mathf.Max(0f, focusMultiplier);

    // Apply critical hit extension if this was a crit
    if (lastHitWasCritical)
    {
        actualStun *= CombatConstants.CRITICAL_STUN_MULTIPLIER; // e.g., 1.3x
    }

    return actualStun;
}
```

### Phase 2: Weapon Data Extension

**File:** `Assets/Scripts/Data/WeaponData/WeaponData.cs`

**New Fields:**
```csharp
[Header("Stun System")]
[Tooltip("Base stun duration in seconds when this weapon lands a hit")]
public float stunDuration = 1.0f;

[Tooltip("Knockdown meter buildup rate (multiplier)")]
public float knockdownRate = 1.0f;
```

**Preset Values by Weapon Type:**
```csharp
// Very Slow Weapons (Claymore, Two-Handed)
stunDuration = 2.0f;
knockdownRate = 1.5f;

// Slow Weapons (Mace, Hammer)
stunDuration = 1.7f;
knockdownRate = 1.3f;

// Normal Weapons (Sword, Axe)
stunDuration = 1.0f;
knockdownRate = 1.0f;

// Fast Weapons (Dagger, Short Sword)
stunDuration = 0.4f;
knockdownRate = 0.8f;

// Very Fast Weapons (Knuckles, Bare Hands)
stunDuration = 0.25f;
knockdownRate = 0.6f;
```

### Phase 3: Skill System Integration

**File:** `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Integration Points:**

```csharp
public bool TryExecuteSkillFromCombo(SkillType skillType)
{
    // Check if weapon controller allows N+1 transition
    WeaponController weaponController = GetComponent<WeaponController>();
    if (weaponController == null || !weaponController.IsInNPlusOneWindow)
    {
        return false; // Not in valid combo extension window
    }

    // Verify skill is ready
    if (!CanExecuteSkill(skillType))
    {
        return false; // Skill not charged or not enough stamina
    }

    // Execute skill as combo extension
    ExecuteSkill(skillType);

    // Notify weapon controller to reset combo
    weaponController.OnNPlusOneExecuted();

    if (enableDebugLogs)
    {
        Debug.Log($"[N+1 Combo] {gameObject.name} extended combo with {skillType} " +
                  $"(window: {weaponController.CurrentStunProgress:F2}s)");
    }

    return true;
}
```

### Phase 4: Combat Controller Stat System

**File:** `Assets/Scripts/Combat/Core/CombatController.cs`

**New Methods:**
```csharp
[Header("Combat Stats")]
[SerializeField] private float focusStat = 0f;    // Stun resistance
[SerializeField] private float willStat = 10f;    // Knockdown resistance

public float GetFocusStat() => focusStat;
public float GetWillStat() => willStat;

// Called when taking a hit
public void ApplyStunDuration(float baseDuration, bool wasCritical)
{
    float focusReduction = 1f - (focusStat / 30f);
    float actualDuration = baseDuration * Mathf.Max(0f, focusReduction);

    if (wasCritical)
    {
        actualDuration *= CombatConstants.CRITICAL_STUN_MULTIPLIER;
    }

    currentStunDuration = actualDuration;
    // Apply stun state for this duration...
}
```

### Phase 5: Input Handling

**File:** `Assets/Scripts/Combat/Core/CombatInteractionManager.cs`

**Modified Input Logic:**

```csharp
private void HandleSkillInput()
{
    // Existing skill detection...
    if (Input.GetKeyDown(smashKey))
    {
        // FIRST: Try N+1 combo extension
        if (skillSystem.TryExecuteSkillFromCombo(SkillType.Smash))
        {
            return; // Successfully extended combo, skip normal skill execution
        }

        // FALLBACK: Normal skill execution
        skillSystem.ExecuteSkill(SkillType.Smash);
    }

    // Same pattern for other skills...
}
```

### Phase 6: Visual & Audio Feedback

**Visual Indicators:**

1. **Timing Window Indicator** (Optional UI)
   ```
   Enemy Stun: [███████ ⚡ ░░]
                       ^ N+1 window (70-95%)
   ```

2. **Successful Extension Effect**
   - Flash effect on character
   - "Combo Extended!" text popup
   - Different hit spark color (gold vs white)
   - Screen shake for successful extension

3. **Window Difficulty Indicator**
   - Green flash: Large window (slow weapon or crit)
   - Yellow flash: Normal window
   - Red flash: Tight window (fast weapon or high Focus enemy)

**Audio Feedback:**

- Distinct sound effect for successful N+1 execution
- Different from normal skill activation sound
- Higher pitch / more "crisp" audio signature
- Optional: Audio cue when entering N+1 window (subtle "ding")

### Phase 7: Balance Considerations

**Stamina Cost:**

Should N+1 combos cost MORE stamina to prevent spam?

**Option A:** Normal stamina cost (encourages mastery) - RECOMMENDED
```csharp
// 3-hit combo + Smash = 2+2+2+5 = 11 stamina
```

**Option B:** Increased stamina cost (prevents spam)
```csharp
// N+1 Smash costs 7 instead of 5 (+2 penalty)
int staminaCost = baseStaminaCost + (isNPlusOne ? 2 : 0);
```

**Recommendation:** Start with Option A (no penalty) to reward skill. The tight timing windows and stat interactions already provide sufficient balance.

**Knockdown Buildup:**

Should N+1 combo hits apply full knockdown buildup or reduced?

**Current System:** Each hit has diminishing returns (30% → 25% → 20% → 15%)

**N+1 Extension Behavior:**
```csharp
// Option A: Skill resets combo counter (RECOMMENDED)
// Hit 1 (30% × weapon rate) → Hit 2 (25%) → Hit 3 (20%) → Smash (30% - fresh)

// Option B: Skill continues combo counter (more realistic)
// Hit 1 (30% × weapon rate) → Hit 2 (25%) → Hit 3 (20%) → Smash (15% - 4th hit)
```

**Recommendation:** Option A (reset counter). N+1 requires skill execution and precise timing, which deserves fresh combo status. Additionally, this prevents slow weapons from being overpowered (large knockdown rate + easy N+1 windows).

## Advanced Mechanics: N+2 Combos

### What is N+2? - COMPLETE REDESIGN

⚠️ **CRITICAL CORRECTION:** N+2 is NOT "chain another skill after N+1". It's a **dual-window mechanic** within the same stun.

**Classic Mabinogi N+2 System:**
```
Attack at exactly 50% AND 90% of the same stun window
```

**Visualization:**
```
Attack 3 lands → Enemy Stunned (1.0s)
                 [-------- Stun Duration --------]
                 0s    0.5s              0.9s   1.0s
                       ⚡                 ⚡
                       └─ First N+2      └─ Second N+2
                          window             window
```

### N+2 Execution Requirements

1. **Weapon Restriction:** Bare hands and fast weapons ONLY
   - Very Fast (Knuckles, Bare Hands): Yes
   - Fast (Daggers): Yes
   - Normal and slower: NO

2. **Dual Timing Windows:**
   - First window: 48-52% of stun duration (±2% tolerance)
   - Second window: 88-92% of stun duration (±2% tolerance)
   - Must execute attacks at BOTH windows to achieve N+2

3. **Frame-Perfect Execution:**
   - Much tighter than N+1 (±2% vs ±25% window)
   - Requires exceptional timing and practice
   - Described as "fairly difficult to pull off" in classic

### Why Fast Weapons Only?

Fast weapons have:
- Shorter recovery times (can attack more frequently)
- Better control over timing precision
- Multiple quick hits allow hitting both windows
- Already have tight N+1 windows, so players are skilled at precision

Slow weapons cannot physically execute two attacks fast enough to hit both windows.

### N+2 Implementation (Future Enhancement)

**File:** `Assets/Scripts/Combat/Weapons/WeaponController.cs`

```csharp
[Header("N+2 Combo System (Advanced)")]
[SerializeField] private bool enableNPlusTwoCombo = false;  // Default off
[SerializeField] private float nPlusTwoFirstWindow = 0.5f;  // 50% of stun
[SerializeField] private float nPlusTwoSecondWindow = 0.9f; // 90% of stun
[SerializeField] private float nPlusTwoTolerance = 0.02f;   // ±2%

private bool hitFirstN2Window = false;
private bool hitSecondN2Window = false;

private void CheckNPlusTwoWindows()
{
    if (!enableNPlusTwoCombo) return;
    if (!weaponData.isFastWeapon) return; // Fast weapons only

    // Check first window (48-52%)
    if (currentStunProgress >= nPlusTwoFirstWindow - nPlusTwoTolerance &&
        currentStunProgress <= nPlusTwoFirstWindow + nPlusTwoTolerance)
    {
        if (attackExecutedThisFrame)
            hitFirstN2Window = true;
    }

    // Check second window (88-92%)
    if (currentStunProgress >= nPlusTwoSecondWindow - nPlusTwoTolerance &&
        currentStunProgress <= nPlusTwoSecondWindow + nPlusTwoTolerance)
    {
        if (attackExecutedThisFrame && hitFirstN2Window)
        {
            hitSecondN2Window = true;
            OnNPlusTwoExecuted(); // Rare achievement!
        }
    }
}
```

**Strategic Value of N+2:**
- Extremely rare to execute (requires expert timing)
- Demonstrates mastery of fast weapons
- Provides additional combo extension for skilled players
- Creates memorable "flow state" moments

**Balance Consideration:** N+2 should be disabled by default and only enabled after extensive testing and tuning of the N+1 system.

## Technical Implementation Checklist

### Prerequisites
- [ ] Phase 5 (AI Pattern System) completed
- [ ] Combat system stable and tested
- [ ] Frame timing system verified accurate
- [ ] Stat system (Focus, Will) implemented

### Core Implementation
- [ ] Add stun duration system to WeaponData
- [ ] Add knockdown rate to WeaponData
- [ ] Implement Focus stat in CombatController
- [ ] Implement Will stat in CombatController
- [ ] Add N+1 detection to WeaponController (stun-based timing)
- [ ] Add critical hit stun extension mechanic
- [ ] Add TryExecuteSkillFromCombo() to SkillSystem
- [ ] Modify CombatInteractionManager input handling
- [ ] Add OnNPlusOneExecuted() callback to WeaponController

### Stat Integration
- [ ] Implement Focus stat affecting stun duration
- [ ] Implement Will stat affecting knockdown resistance
- [ ] Add critical hit detection to weapon hits
- [ ] Apply critical stun multiplier to stun duration
- [ ] Test stat scaling at various levels (0, 15, 30 Focus)

### Feedback & Polish
- [ ] Create visual timing indicator (optional)
- [ ] Add success/failure audio cues
- [ ] Implement combo extension VFX
- [ ] Add debug visualization for timing window
- [ ] Add window difficulty indicator (color-coded)
- [ ] Create audio cue for entering N+1 window

### Testing & Tuning
- [ ] Test timing window feels "fair" (not too tight/loose)
- [ ] Verify all skills can extend properly
- [ ] Test stamina balance with N+1 chains
- [ ] Verify knockdown buildup behaves correctly
- [ ] Test Focus stat scaling (0, 15, 30)
- [ ] Test critical hit stun extension
- [ ] Test fast vs slow weapon window differences
- [ ] Verify N+1 impossible at 30 Focus
- [ ] Test AI doesn't abuse N+1 (or implement AI usage)

### Documentation
- [ ] Add code comments explaining N+1 system
- [ ] Document timing window tuning parameters
- [ ] Document stat interaction formulas
- [ ] Create player-facing tutorial/guide
- [ ] Add to combat system architecture docs

## Testing Scenarios

### Test Case 1: Basic N+1 Extension
```
Setup: Normal sword (1.0s stun), enemy with 0 Focus
Input: Attack → Attack → Attack → Smash (at 0.8s)
Expected: 4-hit combo with Smash finisher
Verify: Combo counter shows 4 hits, Smash executes immediately
```

### Test Case 2: Missed Timing Window (Too Late)
```
Setup: Normal sword (1.0s stun), enemy with 0 Focus
Input: Attack → Attack → Attack [wait 0.96s] → Smash
Expected: Combo ends naturally, Smash executes as separate skill
Verify: Combo counter shows 3 hits, Smash has normal startup
```

### Test Case 3: Missed Timing Window (Too Early)
```
Setup: Normal sword (1.0s stun), enemy with 0 Focus
Input: Attack → Attack → Attack [at 0.6s] → Smash
Expected: Combo ends naturally, Smash executes as separate skill
Verify: Combo counter shows 3 hits, Smash has normal startup
```

### Test Case 4: High Focus Enemy (Tight Window)
```
Setup: Normal sword (1.0s base stun), enemy with 15 Focus (0.5s actual stun)
Input: Attack → Attack → Attack → Smash (at 0.4s)
Expected: 4-hit combo with Smash finisher
Verify: Window is much tighter (0.35s-0.475s), requires precision
```

### Test Case 5: Max Focus Enemy (Impossible N+1)
```
Setup: Normal sword (1.0s base stun), enemy with 30 Focus (0.0s stun)
Input: Attack → Attack → Attack → Smash (any timing)
Expected: N+1 impossible, combo ends naturally
Verify: No timing window exists, Smash always executes separately
```

### Test Case 6: Critical Hit (Extended Window)
```
Setup: Normal sword (1.0s base stun), critical hit landed
Input: Attack → Attack → Attack (CRIT) → Smash (at 1.0s)
Expected: 4-hit combo due to extended stun
Verify: Window is larger due to critical stun multiplier
```

### Test Case 7: Fast Weapon (Tight Window)
```
Setup: Dagger (0.4s stun), enemy with 0 Focus
Input: Attack → Attack → Attack → Smash (at 0.32s)
Expected: 4-hit combo with Smash finisher
Verify: Very tight window (0.28s-0.38s = 0.1s window)
```

### Test Case 8: Slow Weapon (Large Window)
```
Setup: Mace (1.7s stun), enemy with 0 Focus
Input: Attack → Attack → Attack → Smash (at 1.3s)
Expected: 4-hit combo with Smash finisher
Verify: Generous window (1.19s-1.615s = 0.425s window)
```

### Test Case 9: Insufficient Stamina
```
Setup: Player has 3 stamina (needs 5 for Smash)
Input: Attack → Attack → Attack → Smash (during window)
Expected: N+1 fails, normal combo ends
Verify: Combo counter shows 3 hits, no stamina error
```

### Test Case 10: Skill Not Charged
```
Setup: Smash not fully charged
Input: Attack → Attack → Attack → Smash (during window)
Expected: N+1 fails, normal combo ends
Verify: Combo counter shows 3 hits, skill stays in charging state
```

### Test Case 11: Multiple Skills
```
Setup: Normal sword, enemy with 0 Focus
Input: Attack → Attack → Attack → Defense (at 0.8s)
Expected: 4-hit combo ending in Defense stance
Verify: Character immediately enters Defense waiting state
```

### Test Case 12: N+2 Combo (Advanced)
```
Setup: Knuckles (0.25s stun), fast weapon, N+2 enabled
Input: Attack → Attack → Attack (at 0.125s) → Attack (at 0.225s)
Expected: 5-hit combo via dual-window N+2
Verify: Both timing windows hit (50% and 90%), rare achievement
```

## Future Enhancements

### Advanced N+1 Mechanics

1. **Rank-Based Scaling**
   - Higher skill ranks = larger timing windows
   - Encourages skill progression
   - Example: Smash Rank 9 = standard window, Rank 1 = +0.1s bonus

2. **Perfect Timing Bonus**
   - Execute skill at EXACT center of window (82.5%) = damage bonus
   - Visual/audio feedback for "perfect" vs "good" timing
   - Adds additional skill expression
   - Synergizes with critical hit system

3. **Weapon Mastery Integration**
   - Higher weapon mastery = slightly larger windows
   - Rewards specialization in specific weapon types
   - Example: Sword Mastery Rank 1 = +0.05s window

4. **Chain N+1 Combos**
   - After successful N+1, immediate combo reset
   - Allows: Attack → Attack → Attack → Smash → Attack → Attack → Attack → Windmill
   - Creates extended combo chains for skilled players

### AI Implementation

Should enemies be able to use N+1 combos?

**Yes:**
- Makes AI feel more skillful and dangerous
- Rewards player for interrupting enemy combos
- Creates memorable "skilled opponent" encounters
- Provides gameplay for countering N+1 (Defense, Counter)

**Implementation:**
```csharp
// PatternExecutor.cs or SimpleTestAI.cs
private void TryNPlusOneCombo()
{
    if (weaponController.IsInNPlusOneWindow && Random.value < nPlusOneChance)
    {
        // Pick appropriate skill for situation
        SkillType comboFinisher = ChooseComboFinisher();
        skillSystem.TryExecuteSkillFromCombo(comboFinisher);
    }
}

private SkillType ChooseComboFinisher()
{
    // Decision tree based on AI archetype and situation
    if (playerIsCharging) return SkillType.Smash; // Interrupt
    if (multipleEnemiesNearby) return SkillType.Windmill; // AoE
    if (lowHealth) return SkillType.Defense; // Defensive
    return SkillType.Smash; // Default
}
```

**Balance:** Adjust `nPlusOneChance` per enemy archetype
- Novice enemies: 0% (never use N+1)
- Intermediate enemies: 20% (occasional extensions)
- Expert enemies: 60% (frequent extensions)
- Boss enemies: 80% (expert-level timing)

**Additional Consideration:** AI should only attempt N+1 when using appropriate weapons (slow weapons for easier execution) and against players with low Focus stats.

## Design Philosophy Alignment

This feature aligns with FairyGate's core combat philosophy:

✅ **Prediction-Based Gameplay** - Requires anticipating combo length and preparing skill
✅ **Knowledge-Based Mastery** - Must learn timing windows through practice
✅ **No Animation Canceling** - Not a cancel, but a seamless extension
✅ **Methodical Pacing** - Rewards planning combo chains in advance
✅ **Tactical Depth** - Multiple valid N+1 options per situation
✅ **Stat-Based Scaling** - Focus and Will create natural difficulty progression
✅ **Build Diversity** - Critical builds vs precision builds have different playstyles

## Stat System Summary

### Focus Stat (Stun Resistance)
- **Range:** 0-30
- **Effect:** Reduces incoming stun duration
- **Formula:** Actual Stun = Base Stun × (1 - Focus/30)
- **N+1 Impact:** Higher Focus = smaller N+1 windows against you
- **Use Case:** Defensive stat for enemies/players who want to resist combos

### Will Stat (Knockdown Resistance)
- **Range:** 0-100 (typical 10-30)
- **Effect:** Reduces knockdown meter buildup
- **Formula:** Applied as resistance modifier to knockdown accumulation
- **N+1 Impact:** Affects effectiveness of N+1 extended combos
- **Use Case:** Prevents being knocked down easily during N+1 chains

### Critical Hits (Stun Extension)
- **Effect:** Multiplies stun duration (e.g., 1.3x)
- **N+1 Impact:** Creates larger N+1 windows (easier timing)
- **Build Synergy:** Critical builds have more forgiving N+1 execution
- **Strategic Trade-off:** Damage vs timing forgiveness

## Conclusion

The N+1 Combo System is a **high-skill, high-reward** enhancement that adds depth without violating core design principles. It should be implemented AFTER Phase 5 when the core combat system is stable and thoroughly tested.

**Priority:** Low-Medium (polish feature, not core mechanic)
**Complexity:** Medium-High (requires precise timing system + stat integration)
**Impact:** High (significantly raises skill ceiling + creates build diversity)
**Risk:** Low (can be disabled if problematic)

**Key Design Decisions Made:**
1. ✅ Timing based on **enemy stun duration** (not attacker recovery)
2. ✅ **Focus stat** provides natural difficulty scaling
3. ✅ **Critical hits** create build diversity (crit = easier N+1)
4. ✅ **Weapon speed** creates inherent window variance (slow = easy, fast = hard)
5. ✅ **N+2** is dual-window mechanic (not chained skills)
6. ✅ **Will stat** affects knockdown effectiveness during N+1

---

**Next Steps:**
1. Complete Phase 5 (AI Pattern System) ✅
2. Implement stat system (Focus, Will)
3. Add stun duration system to WeaponData
4. Implement N+1 timing detection (stun-based)
5. Test and tune timing windows
6. Gather player feedback
7. Consider N+2 implementation (advanced feature)
