# RangedAttack Skill - Complete Test Plan

**Version:** 1.0
**Date:** 2025-10-14
**Tester:** _________________
**Build:** _________________

---

## Pre-Test Setup Checklist

- [ ] Unity project opened
- [ ] All scripts compiled without errors (check Console)
- [ ] AccuracySystem component added to Player GameObject
- [ ] AccuracySystem component added to Enemy GameObject
- [ ] Test Bow weapon created (`Tools → Combat → Create Ranged Weapons → Bow`)
- [ ] Player equipped with Bow (WeaponController → Weapon Data)
- [ ] Test scene loaded (Good.unity or Setup.unity)
- [ ] Debug logs enabled (SkillSystem → Enable Debug Logs = true)

---

## Test Suite 1: Basic Input & State Transitions

### Test 1.1: Enter Aiming State
**Steps:**
1. Press TAB to enter combat
2. Select enemy target (should auto-select)
3. Press key 6

**Expected:**
- [ ] Debug GUI shows "Skill State: Aiming"
- [ ] Debug GUI shows "Current Skill: RangedAttack"
- [ ] Console: "Player started aiming RangedAttack"
- [ ] Player movement slows to 50% speed
- [ ] Accuracy starts building from 1%

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 1.2: Fire Ranged Attack
**Steps:**
1. Press 6 to start aiming
2. Wait for accuracy to reach ~50%
3. Press 6 again

**Expected:**
- [ ] Yellow-to-red line appears from player to enemy
- [ ] Console: "Player fired RangedAttack at XX.X% accuracy → HIT/MISS"
- [ ] If HIT: Enemy health decreases
- [ ] If MISS: Gray line appears, no damage
- [ ] Player enters Recovery state (immobilized briefly)
- [ ] Stamina decreases by 3

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 1.3: Cancel Aiming with Space
**Steps:**
1. Press 6 to start aiming
2. Wait 1 second
3. Press Space

**Expected:**
- [ ] Aiming cancelled
- [ ] State returns to "Uncharged"
- [ ] Movement returns to 100% speed
- [ ] Stamina NOT consumed
- [ ] Console: "Player cancelled RangedAttack aim"

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

## Test Suite 2: Accuracy Mechanics

### Test 2.1: Accuracy Builds vs Stationary Target
**Steps:**
1. Enemy is NOT moving
2. Press 6 to start aiming
3. Watch accuracy build for 5 seconds
4. Record values at 1s, 2s, 3s intervals

**Expected Build Rate:** ~40%/s (base) × Focus multiplier

| Time | Expected (~40%/s) | Actual | Pass/Fail |
|------|-------------------|--------|-----------|
| 1s   | ~40%              |        | ☐         |
| 2s   | ~80%              |        | ☐         |
| 2.5s | ~100%             |        | ☐         |

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 2.2: Accuracy Builds vs Moving Target
**Setup:** Have enemy move (AI or manual control)

**Steps:**
1. Enemy IS moving
2. Press 6 to start aiming
3. Watch accuracy build for 5 seconds
4. Record values

**Expected Build Rate:** ~20%/s (base) × Focus multiplier

| Time | Expected (~20%/s) | Actual | Pass/Fail |
|------|-------------------|--------|-----------|
| 1s   | ~20%              |        | ☐         |
| 2s   | ~40%              |        | ☐         |
| 5s   | ~100%             |        | ☐         |

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 2.3: Player Movement Penalty
**Steps:**
1. Start aiming (6)
2. Stand still for 2 seconds → record accuracy
3. Move with WASD for 2 seconds → record accuracy
4. Stop moving → record accuracy

**Expected:**
- [ ] Accuracy builds slower while moving (-10%/s penalty)
- [ ] Accuracy builds faster when stopped
- [ ] Movement speed at 50% while aiming

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 2.4: Focus Stat Scaling
**Setup:** Modify Player's CharacterStats

**Test A: Focus = 10**
1. Set Player Focus = 10
2. Aim vs stationary target
3. Time to 100% accuracy

**Expected:** ~1.67 seconds (multiplier = 1.5×)
**Actual:** _______ seconds
**Result:** ☐ Pass ☐ Fail

**Test B: Focus = 20**
1. Set Player Focus = 20
2. Aim vs stationary target
3. Time to 100% accuracy

**Expected:** ~1.0 seconds (multiplier = 2.0×)
**Actual:** _______ seconds
**Result:** ☐ Pass ☐ Fail

**Test C: Focus = 30**
1. Set Player Focus = 30
2. Aim vs stationary target
3. Time to 100% accuracy

**Expected:** ~0.8 seconds (multiplier = 2.5×)
**Actual:** _______ seconds
**Result:** ☐ Pass ☐ Fail


---

## Test Suite 3: Hit/Miss RNG

### Test 3.1: Low Accuracy (~1-10%)
**Steps:**
1. Aim for 0.2 seconds (should be ~1-10% accuracy)
2. Fire 10 times (reload scene between tests)
3. Record hits/misses

**Expected:** 0-1 hits out of 10 (mostly misses)

| Attempt | Accuracy % | Hit/Miss | Trail Color |
|---------|------------|----------|-------------|
| 1       |            |          |             |
| 2       |            |          |             |
| 3       |            |          |             |
| 4       |            |          |             |
| 5       |            |          |             |
| 6       |            |          |             |
| 7       |            |          |             |
| 8       |            |          |             |
| 9       |            |          |             |
| 10      |            |          |             |

**Hit Rate:** _____/10 (%_____)
**Result:** ☐ Pass ☐ Fail


---

### Test 3.2: Medium Accuracy (~50%)
**Steps:**
1. Aim for ~1.25 seconds (should be ~50% accuracy)
2. Fire 10 times
3. Record hits/misses

**Expected:** 4-6 hits out of 10

| Attempt | Accuracy % | Hit/Miss | Damage Dealt |
|---------|------------|----------|--------------|
| 1       |            |          |              |
| 2       |            |          |              |
| 3       |            |          |              |
| 4       |            |          |              |
| 5       |            |          |              |
| 6       |            |          |              |
| 7       |            |          |              |
| 8       |            |          |              |
| 9       |            |          |              |
| 10      |            |          |              |

**Hit Rate:** _____/10 (%_____)
**Result:** ☐ Pass ☐ Fail


---

### Test 3.3: High Accuracy (100%)
**Steps:**
1. Aim until 100% accuracy
2. Fire 10 times
3. Record hits/misses

**Expected:** 10/10 hits (should never miss)

| Attempt | Accuracy % | Hit/Miss | Expected |
|---------|------------|----------|----------|
| 1       | 100%       |          | Hit      |
| 2       | 100%       |          | Hit      |
| 3       | 100%       |          | Hit      |
| 4       | 100%       |          | Hit      |
| 5       | 100%       |          | Hit      |
| 6       | 100%       |          | Hit      |
| 7       | 100%       |          | Hit      |
| 8       | 100%       |          | Hit      |
| 9       | 100%       |          | Hit      |
| 10      | 100%       |          | Hit      |

**Hit Rate:** _____/10 (%_____)
**Result:** ☐ Pass ☐ Fail (MUST be 10/10)


---

## Test Suite 4: Combat Interactions

### Test 4.1: RangedAttack vs Defense
**Steps:**
1. Enemy uses Defense (key 2)
2. Enemy enters Waiting state
3. Player uses RangedAttack (aim + fire)

**Expected:**
- [ ] Defense blocks the attack
- [ ] Enemy takes 50% reduced damage (not 0%)
- [ ] Console: "Enemy partially blocked Player RangedAttack for X damage"
- [ ] Calculate expected: (10 + Player Dex - Enemy Defense) × 0.5
- [ ] Actual damage matches expected

**Expected Damage:** ________
**Actual Damage:** ________
**Result:** ☐ Pass ☐ Fail


---

### Test 4.2: RangedAttack vs Counter
**Steps:**
1. Enemy uses Counter (key 3)
2. Enemy enters Waiting state
3. Player uses RangedAttack (aim + fire)

**Expected:**
- [ ] Counter reflects the attack
- [ ] PLAYER takes damage (not enemy)
- [ ] PLAYER gets knocked down
- [ ] Player displaced 1.5 units backward
- [ ] Console: "Enemy counter reflected X damage to Player"

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 4.3: RangedAttack vs RangedAttack (Speed Resolution)
**Steps:**
1. Both Player and Enemy have Bow equipped
2. Both fire RangedAttack simultaneously
3. Observe outcome

**Expected (if same weapon speed):**
- [ ] Tie - both attacks execute
- [ ] Both take damage
- [ ] Console shows speed resolution result

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

### Test 4.4: RangedAttack vs Attack (Speed Resolution)
**Setup:** Enemy uses regular Attack, Player uses RangedAttack

**Expected:**
- [ ] Speed resolution determines winner
- [ ] Faster skill executes, slower cancelled
- [ ] Console logs winner/loser

**Result:** ☐ Pass ☐ Fail
**Notes:**


---

## Test Suite 5: Movement & Timing

### Test 5.1: Movement Speed While Aiming
**Steps:**
1. Record normal movement speed (run across room, time it)
2. Start aiming
3. Run same distance, time it

**Expected:**
- Normal speed: ______ seconds
- Aiming speed: ______ seconds (should be ~2× slower)
- Speed ratio: ______ (should be ~0.5)

**Result:** ☐ Pass ☐ Fail


---

### Test 5.2: Immobilization During Recovery
**Steps:**
1. Fire RangedAttack
2. Try to move during Recovery state
3. Observe

**Expected:**
- [ ] Player cannot move during Recovery
- [ ] Recovery lasts 0.3 seconds (base)
- [ ] Movement restored after Recovery

**Result:** ☐ Pass ☐ Fail


---

### Test 5.3: Recovery Time Scaling
**Setup:** Test with different weapon speeds

**Bow (speed 1.0):**
- Expected recovery: 0.3s / 1.0 = 0.3s
- Actual: ________ s

**Javelin (speed 0.8):**
- Expected recovery: 0.3s / 0.8 = 0.375s
- Actual: ________ s

**Throwing Knife (speed 1.3):**
- Expected recovery: 0.3s / 1.3 = 0.23s
- Actual: ________ s

**Result:** ☐ Pass ☐ Fail


---

## Test Suite 6: Edge Cases & Error Handling

### Test 6.1: No Target Selected
**Steps:**
1. Press TAB to exit combat
2. Press 6 (no target)

**Expected:**
- [ ] Aiming does NOT start
- [ ] Console: "cannot aim: no target"
- [ ] No state change

**Result:** ☐ Pass ☐ Fail


---

### Test 6.2: Insufficient Stamina
**Steps:**
1. Drain stamina to 2 (need 3)
2. Try to aim (press 6)

**Expected:**
- [ ] Aiming does NOT start
- [ ] Console: "cannot aim: insufficient stamina"
- [ ] No stamina consumed

**Result:** ☐ Pass ☐ Fail


---

### Test 6.3: Target Out of Range
**Steps:**
1. Move 10 units away from enemy (Bow range = 6.0)
2. Try to aim (press 6)

**Expected:**
- [ ] Aiming does NOT start
- [ ] Console: "cannot aim: target out of range (X > 6.0)"

**Result:** ☐ Pass ☐ Fail


---

### Test 6.4: Target Moves Out of Range While Aiming
**Steps:**
1. Start aiming at enemy (within range)
2. Move away from enemy (beyond range)
3. Try to fire

**Expected:**
- [ ] Aiming automatically cancels
- [ ] Console: "cannot fire: target out of range"
- [ ] No stamina consumed

**Result:** ☐ Pass ☐ Fail


---

### Test 6.5: Target Dies While Aiming
**Steps:**
1. Start aiming at enemy
2. Kill enemy (use console command or editor)
3. Observe

**Expected:**
- [ ] Aiming automatically stops
- [ ] State returns to Uncharged
- [ ] No crash or errors

**Result:** ☐ Pass ☐ Fail


---

### Test 6.6: Skill Switching Cancels Aim
**Steps:**
1. Start aiming (press 6)
2. Press 4 (Smash skill)

**Expected:**
- [ ] Aiming cancels
- [ ] Switches to charging Smash
- [ ] No errors

**Result:** ☐ Pass ☐ Fail


---

## Test Suite 7: Weapon Variety

### Test 7.1: Bow
**Properties:**
- Range: 6.0 units
- Damage: 10 + Dex
- Trail: Yellow → Red
- Recovery: 0.3s

**Tests:**
- [ ] Can hit target at 6.0 units away
- [ ] Cannot hit target at 6.5 units away
- [ ] Trail is yellow-to-red on hit
- [ ] Trail is gray on miss
- [ ] Damage = 10 + Dex - Enemy Def

**Result:** ☐ Pass ☐ Fail


---

### Test 7.2: Javelin
**Properties:**
- Range: 4.5 units
- Damage: 14 + Dex
- Trail: Gray → White (thicker)
- Recovery: 0.375s (slower)

**Tests:**
- [ ] Can hit at 4.5 units
- [ ] Cannot hit at 5.0 units
- [ ] Trail is gray-to-white
- [ ] Trail is thicker than Bow
- [ ] Recovery is slower than Bow
- [ ] Damage higher than Bow

**Result:** ☐ Pass ☐ Fail


---

### Test 7.3: Throwing Knife
**Properties:**
- Range: 3.5 units
- Damage: 7 + Dex
- Trail: Cyan → Blue (thin)
- Recovery: 0.23s (faster)

**Tests:**
- [ ] Can hit at 3.5 units
- [ ] Cannot hit at 4.0 units
- [ ] Trail is cyan-to-blue
- [ ] Trail is thinner than Bow
- [ ] Recovery is faster than Bow
- [ ] Damage lower than Bow

**Result:** ☐ Pass ☐ Fail


---

## Test Suite 8: Stamina System Integration

### Test 8.1: Stamina Cost
**Steps:**
1. Record current stamina
2. Fire RangedAttack
3. Check stamina after

**Expected:**
- Before: ______
- After: ______ (should be -3)
- Difference: 3 stamina

**Result:** ☐ Pass ☐ Fail


---

### Test 8.2: Stamina Check on Fire (Not Aim)
**Steps:**
1. Have 5 stamina
2. Start aiming (press 6)
3. Wait while aiming until stamina drains to 2
4. Try to fire (press 6)

**Expected:**
- [ ] Aiming succeeds (5 stamina available when starting)
- [ ] Firing fails (only 2 stamina left)
- [ ] Console: "insufficient stamina to fire RangedAttack"
- [ ] Aiming cancels

**Result:** ☐ Pass ☐ Fail


---

## Test Suite 9: Visual & Audio Feedback

### Test 9.1: Hit Trail Appearance
**Steps:**
1. Fire and hit enemy

**Expected:**
- [ ] Line appears from player to enemy
- [ ] Start color: Yellow (Bow)
- [ ] End color: Red
- [ ] Line starts at player chest height (~1.5m up)
- [ ] Line ends at enemy position
- [ ] Line fades after 0.5 seconds

**Result:** ☐ Pass ☐ Fail


---

### Test 9.2: Miss Trail Appearance
**Steps:**
1. Fire at low accuracy (miss)

**Expected:**
- [ ] Line appears from player
- [ ] End color: Gray (miss color)
- [ ] Line goes in slightly wrong direction (miss scatter)
- [ ] Scatter angle correlates with accuracy (lower = wider)

**Result:** ☐ Pass ☐ Fail


---

### Test 9.3: Fire Sound (if assigned)
**Steps:**
1. Assign audio clip to Bow.fireSound
2. Fire RangedAttack
3. Listen

**Expected:**
- [ ] Sound plays when firing
- [ ] Sound plays from player position
- [ ] Sound does NOT play when canceling aim

**Result:** ☐ Pass ☐ Fail


---

## Test Suite 10: Performance & Stability

### Test 10.1: Rapid Fire Test
**Steps:**
1. Fire RangedAttack 20 times rapidly
2. Monitor frame rate
3. Check for memory leaks

**Expected:**
- [ ] No frame drops
- [ ] No memory leaks
- [ ] All trails clean up after 0.5s
- [ ] No LineRenderer objects left in scene

**Result:** ☐ Pass ☐ Fail


---

### Test 10.2: Long Aiming Session
**Steps:**
1. Start aiming
2. Hold aim for 60 seconds
3. Observe

**Expected:**
- [ ] Accuracy caps at 100%
- [ ] No overflow errors
- [ ] No performance degradation
- [ ] No memory leaks

**Result:** ☐ Pass ☐ Fail


---

### Test 10.3: Component Missing Test
**Steps:**
1. Remove AccuracySystem component from Player
2. Try to aim (press 6)

**Expected:**
- [ ] No crash
- [ ] Graceful fallback (miss always, or cannot aim)
- [ ] Warning logged

**Result:** ☐ Pass ☐ Fail


---

## Final Summary

**Total Tests Run:** _______ / 50+
**Tests Passed:** _______
**Tests Failed:** _______
**Pass Rate:** _______%

### Critical Issues Found:
1.
2.
3.

### Minor Issues Found:
1.
2.
3.

### Performance Notes:


### Recommendations:


---

**Tester Signature:** _________________
**Date Completed:** _________________
**Build Version:** _________________
