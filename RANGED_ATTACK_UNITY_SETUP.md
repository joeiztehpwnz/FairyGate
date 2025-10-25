# RangedAttack Skill - Unity Setup Guide

**Implementation Date:** 2025-10-14
**Status:** ✅ Code Complete - Ready for Unity Setup

---

## Quick Start (5 Minutes)

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

## Testing Checklist

### Basic Functionality ✅

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

### Movement During Aiming ✅

1. **Press 6 to start aiming**
2. **Try moving with WASD** → Should move at 50% speed
3. **Watch accuracy** → Should build slower (moving penalty)
4. **Stop moving** → Accuracy builds faster again
5. **Fire the attack** → Recovery phase immobilizes you briefly

### Combat Interactions ✅

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

### Accuracy Mechanics ✅

**Test Focus Stat Scaling:**
1. Give Player **Focus = 20** (in CharacterStats)
2. Start aiming → Should reach 100% in ~1 second vs stationary
3. Give Player **Focus = 10**
4. Start aiming → Should reach 100% in ~1.67 seconds vs stationary

**Test Hit/Miss RNG:**
1. Aim at **1% accuracy** → Fire 10 times → Almost all misses (gray trails)
2. Aim at **50% accuracy** → Fire 10 times → ~50% hit rate
3. Aim at **100% accuracy** → Fire 10 times → All hits (red trails)

### Edge Cases ✅

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

## Performance Notes

- **AccuracySystem.Update():** Only runs while actively aiming (~60 FPS)
- **LineRenderer trail:** Auto-destroys after 0.5 seconds
- **No physics calculations:** Instant hit detection, no projectile objects
- **Memory overhead:** ~1KB per AccuracySystem component

---

## Next Steps

After confirming basic functionality works:

1. **Create more ranged weapons** with different properties
2. **Implement weapon restrictions** (see WEAPON_SKILL_RESTRICTIONS.md)
3. **Add ranged AI behavior** (see AI_PATTERN_SYSTEM_DESIGN.md)
4. **Create visual effects** (particle systems for hit/miss)
5. **Add audio** (assign fireSound clips to weapons)
6. **Test multiplayer** (see MULTIPLAYER_IMPLEMENTATION_GUIDE.md)

---

## Files Modified Summary

**1 New File:**
- `Assets/Scripts/Combat/Systems/AccuracySystem.cs`

**7 Modified Files:**
- `CombatEnums.cs` (added RangedAttack, Aiming, weapon types)
- `CombatConstants.cs` (added 11 constants)
- `SpeedResolver.cs` (added to IsOffensiveSkill)
- `SkillSystem.cs` (~200 lines added)
- `MovementController.cs` (added RangedAttack case)
- `CombatInteractionManager.cs` (added interactions)
- `WeaponData.cs` (added ranged properties + 5 factory methods)

**Total:** ~600 new lines + ~200 modified lines

---

## Support

If you encounter any issues:
1. Check console for error messages
2. Verify all files compiled without errors
3. Ensure AccuracySystem component is added to GameObjects
4. Check weapon has `isRangedWeapon = true`
5. Verify you're in combat mode (TAB key)

Happy testing! 🎯🏹
