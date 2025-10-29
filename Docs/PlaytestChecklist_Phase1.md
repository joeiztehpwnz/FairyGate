# Phase 1 Playtest Checklist

**Date:** 2025-10-29
**Version:** Sprint 1 - Initial Playtest
**Tester:** ___________

---

## Test Environment Setup

- [ ] Scene loaded: ___________
- [ ] Player character equipped with: Weapon: _______, Armor: _______
- [ ] Enemy character: Type: _______, Archetype: _______
- [ ] CharacterInfoDisplay visible above both characters
- [ ] Camera following player correctly

---

## Section 1: Individual Skill Testing

Test each skill in isolation to verify basic functionality.

### 1.1 Attack (Instant Skill)
- [ ] Press skill key, attack executes immediately
- [ ] Stamina cost: 2 (verify in UI)
- [ ] Animation plays: Startup 0.2s → Active 0.2s → Recovery 0.3s
- [ ] Hit connects at range ≤1.5 units (sword)
- [ ] Damage dealt: _______ (record actual value)
- [ ] No bugs observed

**Notes:**
```


```

### 1.2 Defense (Chargeable Skill)
- [ ] Hold skill key to charge (2.0s base time)
- [ ] Stamina cost: 3 upfront + 3/s drain while waiting
- [ ] Charge progress visible in CharacterInfoDisplay
- [ ] Auto-executes after charging completes
- [ ] Blocks first incoming attack
- [ ] Defense breaks after blocking once (one-hit block mechanic)
- [ ] Stamina drain stops after block
- [ ] No bugs observed

**Notes:**
```


```

### 1.3 Counter (Chargeable Skill)
- [ ] Hold skill key to charge (2.0s base time)
- [ ] Stamina cost: 5 upfront + 5/s drain while waiting
- [ ] Charge progress visible
- [ ] Auto-executes after charging
- [ ] Reflects incoming attack damage back to attacker
- [ ] Attacker knocked down on reflection
- [ ] Counter ends after first reflection
- [ ] No bugs observed

**Notes:**
```


```

### 1.4 Smash (Chargeable Skill)
- [ ] Hold skill key to charge (2.0s base time)
- [ ] Stamina cost: 4
- [ ] Charge progress visible
- [ ] Auto-executes or manual release works
- [ ] Heavy overhead attack animation
- [ ] Execution: Startup 0.5s → Active 0.3s → Recovery 0.8s
- [ ] Damage dealt: _______ (should be higher than Attack)
- [ ] No bugs observed

**Notes:**
```


```

### 1.5 Windmill (Chargeable Skill)
- [ ] Hold skill key to charge (2.0s base time)
- [ ] Stamina cost: 3
- [ ] Movement speed reduced by 30% while charging
- [ ] 360° spinning attack animation
- [ ] Execution: Startup 0.3s → Active 0.4s → Recovery 0.5s
- [ ] Hits enemies in AoE range
- [ ] Multiple hits registered (test with multiple enemies)
- [ ] No bugs observed

**Notes:**
```


```

### 1.6 Lunge (Chargeable Skill)
- [ ] Hold skill key to charge (1.5s custom time)
- [ ] Stamina cost: 4
- [ ] Requires range 2.0-4.0 units to execute
- [ ] Range indicator visible/clear
- [ ] Dashes forward 2.0 units during execution
- [ ] Execution: Startup 0.1s → Active 0.15s → Recovery 0.2s
- [ ] Collision detection works (doesn't dash through walls)
- [ ] No bugs observed

**Notes:**
```


```

### 1.7 RangedAttack (Aiming Skill)
- [ ] Press skill key to enter aiming mode
- [ ] Stamina cost: 3 (consumed on fire, not on aim start)
- [ ] Accuracy builds: 40%/s stationary, 20%/s moving
- [ ] Accuracy decays: 10%/s when lost focus
- [ ] Accuracy indicator visible
- [ ] Fire projectile at target
- [ ] Projectile trail visible (weapon-specific color)
- [ ] Hit/miss calculated based on accuracy
- [ ] No bugs observed

**Notes:**
```


```

---

## Section 2: Skill Interaction Testing

Test the core combat rock-paper-scissors interactions.

### 2.1 Attack vs Defense
- [ ] Player attacks while enemy defends
- [ ] **Expected:** Defender blocks, attacker stunned
- [ ] Stun duration: _______ seconds
- [ ] Player can charge skills during stun
- [ ] Defense breaks after blocking (one-hit mechanic)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.2 Attack vs Counter
- [ ] Player attacks while enemy counters
- [ ] **Expected:** Counter reflects damage, attacker knocked down
- [ ] Knockdown duration: ~2.0s (modified by Focus)
- [ ] Player cannot act during knockdown
- [ ] Reflected damage: _______ (record value)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.3 Smash vs Defense
- [ ] Player smashes while enemy defends
- [ ] **Expected:** Defender knocked down + takes 75% reduced damage
- [ ] Knockdown occurs immediately
- [ ] Damage dealt: _______ (should be reduced)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.4 Smash vs Counter
- [ ] Player smashes while enemy counters
- [ ] **Expected:** Counter reflects, attacker knocked down
- [ ] Same as Attack vs Counter

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.5 Windmill vs Counter
- [ ] Player windmills while enemy counters
- [ ] **Expected:** Windmill breaks counter, defender knocked down
- [ ] Counter does NOT reflect damage
- [ ] Windmill damage applies fully

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.6 RangedAttack vs Defense
- [ ] Player shoots while enemy defends
- [ ] **Expected:** Defender blocks projectile (if it hits)
- [ ] Defense stays active if projectile misses
- [ ] Defense breaks after blocking projectile

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.7 RangedAttack vs Counter
- [ ] Player shoots while enemy counters
- [ ] **Expected:** Counter ineffective, full damage dealt
- [ ] Counter does NOT reflect projectile
- [ ] Projectile passes through counter stance

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 2.8 Lunge vs Defense
- [ ] Player lunges while enemy defends
- [ ] **Expected:** Defender blocks, attacker stunned
- [ ] Same behavior as Attack vs Defense

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 3: Speed Resolution Testing

Test simultaneous skill execution and speed conflicts.

### 3.1 Attack vs Attack
- [ ] Both execute Attack at same time (within 100ms window)
- [ ] **Expected:** Speed resolution based on Dexterity + weapon modifier
- [ ] Higher speed character strikes first
- [ ] Lower speed character interrupted/stunned

**Winner:** Player / Enemy (Dex: ___ vs ___, Weapon: ___ vs ___)
**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 3.2 Dagger vs Mace (Speed Test)
- [ ] Equip Dagger (Speed 1.5, +20% speed resolution)
- [ ] Enemy equips Mace (Speed 0.6, -30% speed resolution)
- [ ] Both execute same skill simultaneously
- [ ] **Expected:** Dagger wins almost every time

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 3.3 Exact Tie (Same Stats, Same Weapon)
- [ ] Both characters identical stats and equipment
- [ ] Both execute same skill simultaneously
- [ ] **Expected:** Random 50/50 winner selection

**Tested 5 times:** Player wins: ___ / 5
**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 4: Status Effect Testing

Test all status effects and their priorities.

### 4.1 Stun
- [ ] Get stunned (blocked by Defense, failed Attack)
- [ ] Duration: _______ seconds (weapon-dependent)
- [ ] Cannot move during stun
- [ ] CAN charge skills during stun
- [ ] CharacterInfoDisplay shows "Stun: X.Xs" indicator
- [ ] Stun wears off naturally

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 4.2 Knockback (50% Meter)
- [ ] Take damage to build meter to 50%
- [ ] Knockback triggers automatically
- [ ] Duration: 0.8s fixed
- [ ] Displacement: 0.8 units away from attacker
- [ ] Cannot move during knockback
- [ ] CAN charge skills during knockback
- [ ] CharacterInfoDisplay shows "Knockback: X.Xs"

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 4.3 Knockdown (100% Meter)
- [ ] Take damage to build meter to 100%
- [ ] Knockdown triggers automatically
- [ ] Duration: ~2.0s (modified by Focus stat)
- [ ] Displacement: 1.2 units away from attacker
- [ ] Cannot move OR act during knockdown
- [ ] CharacterInfoDisplay shows "Knockdown: X.Xs"
- [ ] Meter continues decaying during knockdown

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 4.4 Interaction Knockdown (Smash/Windmill)
- [ ] Get hit by Smash or Windmill breaking Counter
- [ ] Immediate knockdown (bypasses meter system)
- [ ] Duration: ~2.0s
- [ ] Does NOT affect knockdown meter value
- [ ] CharacterInfoDisplay shows status

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 4.5 Status Priority Hierarchy
- [ ] Test Knockdown overriding Stun
- [ ] Test Knockback overriding Stun
- [ ] Verify higher priority status always wins

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 5: Resource Management Testing

Test stamina system and knockdown meter.

### 5.1 Stamina Consumption
- [ ] Attack costs 2 stamina ✓
- [ ] Defense costs 3 upfront + 3/s drain ✓
- [ ] Counter costs 5 upfront + 5/s drain ✓
- [ ] Smash costs 4 ✓
- [ ] Windmill costs 3 ✓
- [ ] Lunge costs 4 ✓
- [ ] RangedAttack costs 3 ✓

**All values match spec:** ✅ Yes / ❌ No
**Notes:**
```


```

### 5.2 Stamina Drain (Defensive Skills)
- [ ] Defense drains 3/s while waiting
- [ ] Counter drains 5/s while waiting
- [ ] Drain stops immediately after blocking/reflecting
- [ ] Grace period (0.1s) prevents instant cancel on drain spike

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 5.3 Stamina Depletion
- [ ] Use skills until stamina reaches 0
- [ ] Cannot execute skills with insufficient stamina
- [ ] Defensive skills auto-cancel when stamina depleted
- [ ] Stamina UI shows depletion clearly

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 5.4 Rest (Stamina Regeneration)
- [ ] Press Rest key to start resting
- [ ] Regenerates 25 stamina/s
- [ ] Exits combat (AI stops attacking?)
- [ ] Interrupted when taking damage
- [ ] Can cancel Rest manually

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 5.5 Knockdown Meter
- [ ] Meter builds on taking damage: 15 + (Str/10) - (Focus/30)
- [ ] Minimum buildup: 1 per hit
- [ ] Meter decays continuously: -5/s
- [ ] 50% threshold triggers Knockback (once per cycle)
- [ ] 100% threshold triggers Knockdown
- [ ] Meter visualization visible in UI

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 6: AI Testing

Test enemy AI behavior and decision-making.

### 6.1 AI Skill Selection
- [ ] AI uses all 7 skills appropriately
- [ ] Skill weights feel reasonable (not spamming one skill)
- [ ] AI checks stamina before skill use
- [ ] AI rests when stamina < 10

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 6.2 AI Reactive Behavior
- [ ] AI observes player charging skills
- [ ] AI counter-picks optimal skills (60% reaction chance)
  - Player Attack → AI Defense
  - Player Smash → AI Counter
  - Player Defense → AI Smash/Windmill
- [ ] Reactive behavior feels intelligent but not unfair

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 6.3 AI CC Exploitation
- [ ] AI attacks during player knockdown
- [ ] AI uses Smash during player stun
- [ ] AI opportunistically rests during player knockdown
- [ ] Exploitation feels fair (not too punishing)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 6.4 AI Movement
- [ ] AI maintains optimal weapon range
- [ ] AI moves toward player when too far
- [ ] AI backs away when too close
- [ ] No jittering or movement bugs
- [ ] Lunge weight increases when in range (2.0-4.0 units)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 6.5 AI Coordination (Multi-Enemy)
- [ ] Test with 2+ enemies
- [ ] AI uses attack slot system (not all attacking simultaneously)
- [ ] Coordination feels natural, not robotic

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 7: Equipment & Stats Testing

Test weapon properties and stat scaling.

### 7.1 Weapon Comparison
Test each weapon's feel and properties:

**Sword (Baseline):**
- Range: 1.5, Damage: 10, Speed: 1.0, Stun: 1.0s
- Feels: ✅ Balanced / ⚠️ Needs Tuning / ❌ Broken
- Notes: ___________

**Spear (Range):**
- Range: 2.5, Damage: 8, Speed: 0.8, Stun: 0.8s
- Modifiers: +10% execution, -10% speed resolution
- Feels: ✅ Balanced / ⚠️ Needs Tuning / ❌ Broken
- Notes: ___________

**Dagger (Speed):**
- Range: 1.0, Damage: 6, Speed: 1.5, Stun: 0.5s
- Modifiers: -20% execution, +20% speed resolution
- Feels: ✅ Balanced / ⚠️ Needs Tuning / ❌ Broken
- Notes: ___________

**Mace (Power):**
- Range: 1.2, Damage: 15, Speed: 0.6, Stun: 1.5s
- Modifiers: +30% execution, -30% speed resolution
- Feels: ✅ Balanced / ⚠️ Needs Tuning / ❌ Broken
- Notes: ___________

### 7.2 Stat Scaling
- [ ] Strength increases damage noticeably
- [ ] Dexterity affects charge time (test with 5 vs 15 Dex)
- [ ] Dexterity affects speed resolution (test conflicts)
- [ ] Focus increases max stamina
- [ ] Focus reduces stun/knockdown duration
- [ ] Physical Defense reduces incoming damage
- [ ] Vitality increases max health

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 8: Edge Cases & Bugs

Test unusual scenarios and potential bugs.

### 8.1 Input Buffering
- [ ] Buffer skill input during stun → executes on recovery
- [ ] Buffer skill input during knockback → executes on recovery
- [ ] Does NOT buffer during knockdown
- [ ] Only buffers one skill (latest input wins)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 8.2 Skill Cancellation
- [ ] Can cancel chargeable skills before execution (during Charging state)
- [ ] Cannot cancel during Startup/Active/Recovery
- [ ] Stamina refunded when canceling? (verify behavior)

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 8.3 Out of Range
- [ ] Attempt to attack enemy out of weapon range
- [ ] Skill executes but doesn't hit
- [ ] No crash or softlock

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 8.4 Simultaneous Knockdown
- [ ] Both characters reach 100% meter simultaneously
- [ ] Both knocked down at same time
- [ ] Both recover and resume combat

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

### 8.5 Defense One-Hit Block
- [ ] Defense blocks first attack and breaks
- [ ] Subsequent attacks (from second attacker in multi-enemy) hit through
- [ ] No exploits or bugs

**Result:** ✅ Pass / ❌ Fail
**Notes:**
```


```

---

## Section 9: UI/UX Observations

Rate the current UI/UX experience.

### 9.1 CharacterInfoDisplay
- **Skill Icon Display:** ✅ Good / ⚠️ Needs Work / ❌ Broken
- **Health Info:** ✅ Good / ⚠️ Needs Work / ❌ Broken
- **Knockdown Meter:** ✅ Good / ⚠️ Needs Work / ❌ Broken
- **Status Effects:** ✅ Good / ⚠️ Needs Work / ❌ Broken
- **Readability:** ✅ Good / ⚠️ Needs Work / ❌ Broken

**Missing Features:**
```


```

### 9.2 Skill Charge Feedback
- **Current State:** Icon changes, no progress bar
- **Is it clear when skills are charged?** ✅ Yes / ❌ No
- **Improvements needed:**
```


```

### 9.3 Stamina Feedback
- **Is stamina visible?** ✅ Yes / ❌ No (where?)
- **Is drain rate clear?** ✅ Yes / ❌ No
- **Improvements needed:**
```


```

### 9.4 Overall Combat Clarity
- **Can you tell what's happening in combat?** ✅ Yes / ⚠️ Sometimes / ❌ No
- **What's confusing:**
```


```

---

## Section 10: Overall Assessment

### 10.1 Combat Feel
Rate the overall combat experience (1-10):

- **Responsiveness:** ___/10
- **Impact (hits feel powerful):** ___/10
- **Strategy depth:** ___/10
- **Fairness:** ___/10
- **Fun factor:** ___/10

**What feels great:**
```


```

**What feels bad:**
```


```

### 10.2 Top 3 Priority Fixes
1. ___________________________________________
2. ___________________________________________
3. ___________________________________________

### 10.3 Balance Issues
List any skills/weapons that feel too strong or too weak:
```


```

### 10.4 Bug Summary
List all bugs found (severity: Critical / Major / Minor):
```


```

---

## Conclusion

**Overall Phase 1 Readiness:** ____%

**Recommendation:**
- [ ] Ready for Sprint 2 (VFX/Audio)
- [ ] Needs critical fixes first
- [ ] Needs major balance pass

**Next Steps:**
```


```

---

**Playtest completed by:** ___________
**Date:** ___________
**Duration:** ___ hours
