# RangedAttack Skill - Complete Guide

> **Consolidated from:** RANGED_ATTACK_SKILL_IMPLEMENTATION.md, RANGED_ATTACK_UNITY_SETUP.md, RANGED_ATTACK_TEST_PLAN.md

**Date Created:** 2025-10-14
**System Version:** Unity 2023.2.20f1
**Combat System:** FairyGate Rock-Paper-Scissors
**Revision:** 2.0

---

## Table of Contents
1. [Overview](#overview)
2. [Quick Start - Unity Setup](#quick-start---unity-setup)
3. [Design Specifications](#design-specifications)
4. [Weapon-Based Differentiation](#weapon-based-differentiation)
5. [Testing Guide](#testing-guide)
6. [Implementation Reference](#implementation-reference)

---

## Overview

### What This Implements
A weapon-agnostic ranged attack skill with accuracy-based instant hit detection (no projectile physics). Works with Bows, Javelins, Throwing Knives, Slings, Throwing Axes, and any other ranged weapon.

### Core Mechanic
- Press key 6 → Start aiming (accuracy builds from 1-100%)
- Press key 6 again → Fire ranged attack (instant hit/miss based on accuracy roll)
- Press Space → Cancel aim

### Key Features
- ✅ Accuracy builds faster vs stationary targets (40%/s vs 20%/s for moving)
- ✅ Scales with Focus stat (1 + Focus/20 multiplier)
- ✅ Player movement penalty (-10%/s while moving)
- ✅ 50% movement speed while aiming
- ✅ Instant damage application (no projectile travel)
- ✅ Visual LineRenderer trail feedback (weapon-specific colors/width)
- ✅ Weapon-based differentiation (range, damage, visual, speed)
- ✅ Integrates with existing combat systems (interactions, speed resolution)
- ✅ Proper offensive skill classification (triggers CombatInteractionManager)

### Why "RangedAttack" Instead of "Arrow"?
- **Weapon-Agnostic**: Works with any ranged weapon (Bow, Javelin, Knife, Sling, etc.)
- **Naming Consistency**: Mirrors "Attack" skill (action, not weapon)
- **Future-Proof**: No need for separate skills per weapon type
- **Follows Convention**: Skill names are verbs/actions, not items

---

## Quick Start - Unity Setup

### Step 1: Add AccuracySystem Component to GameObjects

1. **Open Unity Editor**
2. **Open your test scene** (e.g., `Assets/Good.unity` or `Assets/Setup.unity`)
3. **Select Player GameObject** in Hierarchy
4. Click **Add Component** in Inspector
5. Type **"AccuracySystem"** and add it
6. Repeat for **Enemy GameObject(s)**

> ✅ **Component will auto-configure** using existing CombatController and CharacterStats

---

### Step 2: Create a Test Bow Weapon

**Option A: Create via Unity Menu (Recommended)**

1. In Project window, right-click in `Assets/Data/Weapons/`
2. Select **Create → Combat → Weapon Data**
3. Name it **"Bow"**
4. In Inspector, configure:
   - **Weapon Name:** Bow
   - **Weapon Type:** Bow (new enum value)
   - **Range:** 6.0
   - **Base Damage:** 10
   - **Speed:** 1.0
   - **Stun Duration:** 0.3
   - **Is Ranged Weapon:** ✅ (check this!)
   - **Projectile Type:** Arrow
   - **Trail Color Start:** Yellow
   - **Trail Color End:** Red
   - **Trail Width:** 0.08
   - **Description:** "Standard ranged weapon with good range and accuracy"

**Option B: Create via Script (Quick)**

1. Create a new script in `Assets/Scripts/Editor/` called `WeaponCreator.cs`:

```csharp
using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

public class WeaponCreator
{
    [MenuItem("Tools/Create Test Bow")]
    public static void CreateTestBow()
    {
        var bow = WeaponData.CreateBowData();
        AssetDatabase.CreateAsset(bow, "Assets/Data/Weapons/Bow.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Test Bow created at Assets/Data/Weapons/Bow.asset");
    }
}
```

2. In Unity, go to **Tools → Create Test Bow**
3. Bow will be created automatically!

---

### Step 3: Equip the Bow

1. **Select Player GameObject** in Hierarchy
2. Find **WeaponController** component in Inspector
3. Drag **Bow.asset** into the **Weapon Data** field
4. Press **Play**!

---

## Design Specifications

### Stats & Balance

```yaml
Base Values (can be overridden by weapon):
  Base Damage: 10
  Range: 6.0 units (vs 1.5 for sword, 2.5 for spear)
  Stamina Cost: 3
  Recovery Time: 0.3 seconds

Accuracy Build Rates:
  Stationary Target: 40% per second
  Moving Target: 20% per second
  Player Movement Penalty: -10% per second

Focus Scaling:
  Build Rate Multiplier = 1 + (Focus / 20)
  Examples:
    Focus 10 → 1.5× speed (100% in 1.67s vs stationary)
    Focus 20 → 2.0× speed (100% in 1.0s vs stationary)
    Focus 30 → 2.5× speed (100% in 0.8s vs stationary)

Movement:
  While Aiming: 50% speed
  During Recovery: 0% speed (immobilized)

Miss Scatter:
  Max Angle: 45 degrees
  Formula: Lerp(45°, 0°, accuracy / 100)
  1% accuracy → 45° cone
  50% accuracy → 22.5° cone
  100% accuracy → 0° cone (should never miss)
```

### Damage Formula

```
RangedAttack Damage = BASE_DAMAGE + Dexterity - Target Physical Defense

Example (Bow):
  Base: 10
  Dexterity: 14
  Target Defense: 8
  Final Damage = 10 + 14 - 8 = 16

Example (Javelin - higher base):
  Base: 14
  Dexterity: 14
  Target Defense: 8
  Final Damage = 14 + 14 - 8 = 20
```

---

## Weapon-Based Differentiation

### How Weapons Customize RangedAttack

All ranged weapons use the **same RangedAttack skill**, but each weapon provides:

1. **Range** - From WeaponData.range (Bow: 6.0, Javelin: 4.5, Knife: 3.5)
2. **Damage** - From WeaponData.baseDamage (Bow: 10, Javelin: 14, Knife: 7)
3. **Speed** - From WeaponData.speed (affects recovery time)
4. **Visual** - Projectile type, trail color, trail width
5. **Audio** - Fire sound effect (optional)

### Example: Three Weapons, Same Skill

```
PLAYER EQUIPS BOW:
  Press 6 → Aim (yellow trail preview)
  Range: 6.0 units, Damage: 10+dex
  Fire → Yellow-to-red trail, 0.3s recovery

PLAYER EQUIPS JAVELIN:
  Press 6 → Aim (gray trail preview)
  Range: 4.5 units, Damage: 14+dex
  Fire → Gray-to-white thick trail, 0.375s recovery (slower weapon)

PLAYER EQUIPS THROWING KNIFE:
  Press 6 → Aim (cyan trail preview)
  Range: 3.5 units, Damage: 7+dex
  Fire → Cyan-to-blue thin trail, 0.23s recovery (fast weapon)
```

**Same skill, different behavior** - just like how "Attack" works differently with Sword vs Dagger.

---

## Testing Guide

### Basic Functionality Test

1. **Press TAB** to enter combat mode (lock onto enemy)
2. **Press 6** → Should see debug text change to "Skill State: Aiming"
3. **Watch accuracy build** → Should increase from 1% to 100%
   - Faster if target is stationary (~40%/s)
   - Slower if target is moving (~20%/s)
   - Even slower if YOU are moving (penalty -10%/s)
4. **Press 6 again** → Fires the ranged attack
   - Yellow-to-red trail appears
   - Enemy takes damage (if hit)
   - Console logs hit/miss percentage
5. **Press Space while aiming** → Cancels aim (no stamina consumed)

### Movement During Aiming

1. **Press 6 to start aiming**
2. **Try moving with WASD** → Should move at 50% speed
3. **Watch accuracy** → Should build slower (moving penalty)
4. **Stop moving** → Accuracy builds faster again
5. **Fire the attack** → Recovery phase immobilizes you briefly

### Combat Interactions

**Test 1: RangedAttack vs Defense**
1. Enemy uses Defense (key 2)
2. You use RangedAttack (key 6 → aim → key 6)
3. **Expected:** Enemy blocks, takes 50% damage (not 0%)
4. Console: "Enemy partially blocked Player RangedAttack for X damage"

**Test 2: RangedAttack vs Counter**
1. Enemy uses Counter (key 3)
2. You use RangedAttack (key 6 → aim → key 6)
3. **Expected:** Counter reflects, YOU get knocked down and take damage
4. Console: "Enemy counter reflected X damage to Player"

**Test 3: RangedAttack vs RangedAttack (Speed Resolution)**
1. Both use RangedAttack simultaneously
2. **Expected:** Speed resolution or tie (both execute)
3. Faster weapon/higher dex wins if not tied

### Accuracy Mechanics

**Test Focus Stat Scaling:**
1. Give Player **Focus = 20** (in CharacterStats)
2. Start aiming → Should reach 100% in ~1 second vs stationary
3. Give Player **Focus = 10**
4. Start aiming → Should reach 100% in ~1.67 seconds vs stationary

**Test Hit/Miss RNG:**
1. Aim at **1% accuracy** → Fire 10 times → Almost all misses (gray trails)
2. Aim at **50% accuracy** → Fire 10 times → ~50% hit rate
3. Aim at **100% accuracy** → Fire 10 times → All hits (red trails)

### Edge Cases

1. **Can't aim without target** → Press 6 with no target selected
   - Console: "cannot aim: no target"
2. **Can't aim without stamina** → Drain stamina, press 6
   - Console: "cannot aim: insufficient stamina"
3. **Can't aim if out of range** → Move far from target, press 6
   - Console: "cannot aim: target out of range (X > 6.0)"
4. **Aim cancels if target dies** → Kill target while aiming
   - Aiming automatically cancels
5. **Switching skills cancels aim** → Press 6 to aim, then press 4 for Smash
   - Aim cancels, switches to Smash

---

## Weapon Variety Testing

If you want to test all 5 ranged weapons:

### Create All Weapons
```
Tools → Create Test Bow        (6.0 range, 10 dmg, yellow trail)
Tools → Create Test Javelin    (4.5 range, 14 dmg, gray trail, slower)
Tools → Create Test Knife      (3.5 range, 7 dmg, cyan trail, faster)
Tools → Create Test Sling      (5.0 range, 6 dmg, brown trail)
Tools → Create Test Axe        (3.0 range, 12 dmg, red trail)
```

### Compare Differences
- **Range:** Bow has longest (6.0), Axe shortest (3.0)
- **Damage:** Javelin highest (14), Sling lowest (6)
- **Recovery Time:** Knife fastest (0.23s), Javelin slowest (0.375s)
- **Visuals:** Each has unique trail color and width

---

## Debug Information

### Console Logs to Watch For

**When Aiming Starts:**
```
Player started aiming RangedAttack
```

**When Firing:**
```
Player fired RangedAttack at 87.3% accuracy → HIT
Player fired RangedAttack at 23.1% accuracy → MISS
```

**When Blocked:**
```
Enemy partially blocked Player RangedAttack for 8 damage
```

**When Reflected:**
```
Enemy counter reflected 15 damage to Player
```

### On-Screen Debug Display

If you have `CombatDebugVisualizer` enabled, you should see:
```
Skill: RangedAttack
State: Aiming
Accuracy: 67.5% (Arrow)
Target: MOVING (Rate: 30.0%/s)
```

---

## Common Issues & Solutions

### Issue: "AccuracySystem component not found"
**Solution:** Make sure you added AccuracySystem to the GameObject (Step 1)

### Issue: "Press 6 does nothing"
**Solution:**
- Check you're in combat mode (press TAB first)
- Check you have a target selected
- Check you have enough stamina (need 3)

### Issue: "Accuracy always 1%, never builds"
**Solution:** Check that Focus stat > 0 in CharacterStats

### Issue: "Trail doesn't appear"
**Solution:**
- Check `isRangedWeapon = true` on weapon
- Check trail width > 0
- Check Shader "Sprites/Default" is available

### Issue: "Defense blocks 100% damage instead of 50%"
**Solution:**
- Check `SkillType.RangedAttack` is in SpeedResolver.IsOffensiveSkill()
- Check CombatInteractionManager has updated DefenderBlocks case

### Issue: "Can aim but can't fire"
**Solution:**
- Make sure you press the SAME key (6) to fire
- Check you're still in range when firing
- Check target didn't die while aiming

---

## Implementation Reference

### Files Created (1 New File)

**`Assets/Scripts/Combat/Systems/AccuracySystem.cs`**
- Tracks accuracy buildup, target state, hit/miss rolling
- Key methods: StartAiming(), StopAiming(), Update(), RollHitChance()
- Size: ~200 lines

### Files Modified (8 Files)

1. **CombatEnums.cs** - Added RangedAttack, Aiming, weapon types
2. **CombatConstants.cs** - Added 11 constants
3. **SpeedResolver.cs** - Added to IsOffensiveSkill (CRITICAL)
4. **SkillSystem.cs** - ~200 lines added for aiming/firing logic
5. **MovementController.cs** - Added RangedAttack case
6. **CombatInteractionManager.cs** - Added interactions
7. **CombatDebugVisualizer.cs** - Added accuracy display
8. **WeaponData.cs** - Added ranged properties + factory methods

### Total Implementation
- **Files Created:** 1
- **Files Modified:** 8
- **Lines of Code:** ~600 new + ~200 modified
- **Components Added:** AccuracySystem
- **Integration:** 95% uses existing systems

---

## Complete Testing Checklist

### Basic Functionality
- [ ] Press key 6 to start aiming
- [ ] Debug display shows "Skill State: Aiming"
- [ ] Debug display shows "Accuracy: X.X%"
- [ ] Accuracy builds over time (1% → 100%)
- [ ] Press key 6 again to fire ranged attack
- [ ] See weapon-specific colored line trail on hit
- [ ] See gray line trail on miss
- [ ] Target takes damage on hit
- [ ] No damage on miss
- [ ] Stamina consumed (3 points)
- [ ] Press Space to cancel aim
- [ ] Aiming cancelled, no stamina consumed
- [ ] Cannot aim without sufficient stamina

### Accuracy Mechanics
- [ ] Accuracy builds faster vs stationary enemy (~40%/s)
- [ ] Accuracy builds slower vs moving enemy (~20%/s)
- [ ] Higher Focus stat increases build rate
- [ ] Moving while aiming reduces build rate (-10%/s)
- [ ] 100% accuracy always hits
- [ ] 50% accuracy hits ~half the time
- [ ] 1% accuracy almost always misses

### Range & Targeting
- [ ] Can only aim when target in range
- [ ] Cannot aim without target selected
- [ ] Aim auto-cancels if target moves out of range
- [ ] Aim auto-cancels if target dies
- [ ] Cannot fire if target out of range
- [ ] Different weapons have different ranges

### Combat Integration (CRITICAL)
- [ ] RangedAttack vs Defense → Blocked with 50% reduction
- [ ] RangedAttack vs Counter → Reflected back to attacker
- [ ] RangedAttack vs RangedAttack → Speed resolution
- [ ] RangedAttack vs Attack/Smash/Windmill → Speed resolution
- [ ] RangedAttack costs 3 stamina
- [ ] Cannot aim if insufficient stamina
- [ ] CombatInteractionManager processes correctly

### Weapon Variety
- [ ] Equip Bow: Yellow-to-red trail, 6.0 range
- [ ] Equip Javelin: Gray-to-white thick trail, slower
- [ ] Equip Throwing Knife: Cyan-to-blue thin trail, faster
- [ ] Debug display shows projectile type
- [ ] Weapon speed affects recovery time

### Movement
- [ ] Player moves at 50% speed while aiming
- [ ] Player immobilized during recovery
- [ ] Movement returns to 100% after recovery
- [ ] Recovery time scales with weapon speed

---

## Next Steps

After confirming basic functionality works:

1. **Create more ranged weapons** with different properties
2. **Implement weapon restrictions** (see Weapon-Skill-Restrictions.md)
3. **Add ranged AI behavior** (see AI-Pattern-System.md)
4. **Create visual effects** (particle systems for hit/miss)
5. **Add audio** (assign fireSound clips to weapons)
6. **Test multiplayer** (see Multiplayer-Implementation.md)

---

**End of RangedAttack System Guide**
