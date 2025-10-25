# Weapon-Skill Restriction System Implementation Guide

**Date Created:** 2025-10-13
**System Version:** Unity 2023.2.20f1
**Combat System:** FairyGate Rock-Paper-Scissors
**Related Document:** RANGED_ATTACK_SKILL_IMPLEMENTATION.md

---

## Table of Contents
1. [Overview](#overview)
2. [Design Philosophy](#design-philosophy)
3. [Weapon Categories](#weapon-categories)
4. [Skill Compatibility Matrix](#skill-compatibility-matrix)
5. [Implementation Details](#implementation-details)
6. [File Changes](#file-changes)
7. [Weapon Definitions](#weapon-definitions)
8. [Testing Checklist](#testing-checklist)

---

## Overview

### What This Implements
A flexible weapon-skill restriction system that determines which skills can be used with each weapon type.

### Core Concept
- Each weapon belongs to a **category** (Melee, Ranged, Magic)
- Each category has **default allowed skills**
- Individual weapons can **override** defaults with custom skill lists
- SkillSystem validates weapon compatibility before allowing skill use

### Key Features
- ✅ Category-based defaults (simple, scalable)
- ✅ Per-weapon overrides (flexible, unique weapons)
- ✅ Runtime validation (prevents illegal skill use)
- ✅ Debug display shows allowed skills
- ✅ Supports hybrid weapons (e.g., Spear can throw)

---

## Design Philosophy

### Core Principles

**1. Realism & Theme**
- Melee weapons can't fire arrows
- Ranged weapons are poor at melee combat
- Magic weapons channel spells, not physical attacks

**2. Gameplay Balance**
- Ranged weapons get defensive options (Defense/Counter)
- Prevents ranged from being helpless in melee
- Forces strategic weapon choice

**3. Flexibility**
- Hybrid weapons possible (Spear throws, Staff melees)
- Easy to create unique weapons with custom restrictions
- Expandable for future weapon types

**4. Simplicity**
- Default to category rules
- Only specify exceptions
- Clear error messages when skill blocked

---

## Weapon Categories

### Category Definitions

#### **Melee Weapons**
```yaml
Category: Melee
Philosophy: Close-quarters combat specialists
Range: 1.0 - 2.5 units

Default Skills:
  - Attack (basic melee)
  - Defense (block with weapon/shield)
  - Counter (parry and riposte)
  - Smash (heavy attack)
  - Windmill (area spin)

Cannot Use:
  - RangedAttack (no projectile capability)
  - Magic spells (future)

Examples:
  - Sword (balanced)
  - Spear (reach)
  - Dagger (speed)
  - Mace (power)
```

#### **Ranged Weapons**
```yaml
Category: Ranged
Philosophy: Distance attackers with limited melee
Range: 6.0+ units

Default Skills:
  - RangedAttack (ranged attack)
  - Defense (block/dodge)
  - Counter (defensive counter-shot)

Cannot Use:
  - Attack (ineffective melee)
  - Smash (requires melee weapon)
  - Windmill (cannot spin projectile weapon)

Examples:
  - Bow (standard ranged)
  - Crossbow (heavy ranged)
  - Throwing Knives (light ranged)
```

#### **Magic Weapons** (Future)
```yaml
Category: Magic
Philosophy: Spellcasters with minimal physical combat
Range: Varies by spell

Default Skills:
  - Defense (magical barrier)
  - Magic spells (Fireball, Lightning, etc.)

Cannot Use:
  - Physical attacks (Attack, Smash, Windmill)
  - RangedAttack (magic, not projectiles)

Examples:
  - Staff (melee-capable magic)
  - Wand (pure magic)
  - Tome (spell focus)
```

---

## Skill Compatibility Matrix

### Default Skill Access by Category

| Skill | Melee | Ranged | Magic | Notes |
|-------|-------|--------|-------|-------|
| **Attack** | ✅ | ❌ | ❌ | Basic melee strike |
| **Defense** | ✅ | ✅ | ✅ | Universal defensive option |
| **Counter** | ✅ | ✅ | ❌ | Physical counter-attack |
| **Smash** | ✅ | ❌ | ❌ | Heavy melee attack |
| **Windmill** | ✅ | ❌ | ❌ | Spinning area attack |
| **RangedAttack** | ❌ | ✅ | ❌ | Ranged projectile |
| **Fireball** (future) | ❌ | ❌ | ✅ | Magic projectile |
| **Lightning** (future) | ❌ | ❌ | ✅ | Magic instant ray |

### Weapon-Specific Access

| Weapon | Category | Skills | Override Reason |
|--------|----------|--------|-----------------|
| **Sword** | Melee | ATK/DEF/CTR/SMS/WND | Default melee |
| **Spear** | Melee | ATK/DEF/CTR/SMS/WND/RNG* | *Can throw spear |
| **Dagger** | Melee | ATK/DEF/CTR/SMS/WND | Default melee |
| **Mace** | Melee | ATK/DEF/CTR/SMS | No Windmill (too heavy) |
| **Bow** | Ranged | RNG/DEF/CTR | Default ranged |
| **Crossbow** | Ranged | RNG/DEF | No Counter (slow reload) |
| **Staff** | Magic | ATK/DEF/SMS/Magic* | Melee-capable caster |
| **Wand** | Magic | DEF/Magic | Pure spellcaster |

*Future implementation

---

## Implementation Details

### System Architecture

```
WeaponData (ScriptableObject)
├─ weaponCategory (enum: Melee/Ranged/Magic)
├─ allowedSkills (List<SkillType>) - Optional override
└─ CanUseSkill(SkillType) - Validation method
    ├─ If allowedSkills defined → Check list
    └─ Else → Check category defaults

SkillSystem (MonoBehaviour)
├─ CanChargeSkill(SkillType)
│   └─ Calls IsSkillAllowedWithCurrentWeapon()
└─ IsSkillAllowedWithCurrentWeapon(SkillType)
    └─ Calls weaponController.WeaponData.CanUseSkill()
```

### Validation Flow

```
Player presses skill key (e.g., key 6 for RangedAttack)
↓
SkillSystem.HandleSkillInput() detects input
↓
SkillSystem.CanChargeSkill(RangedAttack) called
↓
IsSkillAllowedWithCurrentWeapon(RangedAttack) called
↓
WeaponController.WeaponData.CanUseSkill(RangedAttack) called
↓
Check allowedSkills list (if defined) OR category defaults
↓
Return true/false
↓
If false: Log "cannot use RangedAttack with current weapon"
If true: Proceed with StartAiming(RangedAttack)
```

---

## File Changes

### Files to Modify (4)

#### 1. `Assets/Scripts/Combat/Utilities/Constants/CombatEnums.cs`
#### 2. `Assets/Scripts/Data/WeaponData/WeaponData.cs`
#### 3. `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`
#### 4. `Assets/Scripts/Combat/Debug/CombatDebugVisualizer.cs`

---

### FILE 1: CombatEnums.cs (MODIFY)

**Location:** `Assets/Scripts/Combat/Utilities/Constants/CombatEnums.cs`

**Changes:**

**A. Update WeaponType enum:**
```csharp
public enum WeaponType
{
    // Melee Weapons
    Sword,
    Spear,
    Dagger,
    Mace,

    // Ranged Weapons
    Bow,        // ADD THIS
    Crossbow,   // ADD THIS

    // Magic Weapons (Future)
    Staff,      // ADD THIS
    Wand        // ADD THIS
}
```

**B. Add WeaponCategory enum:**
```csharp
// ADD THIS ENTIRE ENUM
public enum WeaponCategory
{
    Melee,      // Close-quarters combat
    Ranged,     // Distance attacks
    Magic       // Spellcasting
}
```

---

### FILE 2: WeaponData.cs (MODIFY)

**Location:** `Assets/Scripts/Data/WeaponData/WeaponData.cs`

**Changes:**

**A. Add using statement:**
```csharp
using UnityEngine;
using System.Collections.Generic;  // ADD THIS
```

**B. Add new fields after weaponType (line ~10):**
```csharp
[Header("Basic Stats")]
public string weaponName;
public WeaponType weaponType;
public WeaponCategory weaponCategory;  // ADD THIS LINE

[Header("Skill Restrictions")]  // ADD THIS ENTIRE SECTION
[Tooltip("Leave empty to use default skills for weapon category")]
public List<SkillType> allowedSkills = new List<SkillType>();

public float range;
public int baseDamage;
// ... rest of fields
```

**C. Add skill validation methods (before factory methods, around line 30):**
```csharp
// ADD THESE THREE METHODS

/// <summary>
/// Check if this weapon can use a specific skill
/// </summary>
public bool CanUseSkill(SkillType skill)
{
    // If explicit list is defined, use it
    if (allowedSkills != null && allowedSkills.Count > 0)
    {
        return allowedSkills.Contains(skill);
    }

    // Otherwise use category defaults
    return GetDefaultSkillsForCategory(weaponCategory).Contains(skill);
}

/// <summary>
/// Get list of all allowed skills for this weapon
/// </summary>
public List<SkillType> GetAllowedSkills()
{
    if (allowedSkills != null && allowedSkills.Count > 0)
    {
        return new List<SkillType>(allowedSkills);
    }

    return GetDefaultSkillsForCategory(weaponCategory);
}

/// <summary>
/// Get default skills for a weapon category
/// </summary>
private static List<SkillType> GetDefaultSkillsForCategory(WeaponCategory category)
{
    switch (category)
    {
        case WeaponCategory.Melee:
            return new List<SkillType>
            {
                SkillType.Attack,
                SkillType.Defense,
                SkillType.Counter,
                SkillType.Smash,
                SkillType.Windmill
            };

        case WeaponCategory.Ranged:
            return new List<SkillType>
            {
                SkillType.RangedAttack,
                SkillType.Defense,
                SkillType.Counter
            };

        case WeaponCategory.Magic:
            return new List<SkillType>
            {
                SkillType.Defense
                // Future: Fireball, Lightning, etc.
            };

        default:
            return new List<SkillType>();
    }
}
```

**D. Update existing factory methods to include category:**

**Sword (around line 31):**
```csharp
public static WeaponData CreateSwordData()
{
    var sword = CreateInstance<WeaponData>();
    sword.weaponName = "Sword";
    sword.weaponType = WeaponType.Sword;
    sword.weaponCategory = WeaponCategory.Melee;  // ADD THIS LINE
    sword.range = 1.5f;
    // ... rest unchanged
}
```

**Spear (around line 46):**
```csharp
public static WeaponData CreateSpearData()
{
    var spear = CreateInstance<WeaponData>();
    spear.weaponName = "Spear";
    spear.weaponType = WeaponType.Spear;
    spear.weaponCategory = WeaponCategory.Melee;  // ADD THIS LINE
    spear.range = 2.5f;
    // ... rest unchanged
}
```

**Dagger (around line 61):**
```csharp
public static WeaponData CreateDaggerData()
{
    var dagger = CreateInstance<WeaponData>();
    dagger.weaponName = "Dagger";
    dagger.weaponType = WeaponType.Dagger;
    dagger.weaponCategory = WeaponCategory.Melee;  // ADD THIS LINE
    dagger.range = 1.0f;
    // ... rest unchanged
}
```

**Mace (around line 76):**
```csharp
public static WeaponData CreateMaceData()
{
    var mace = CreateInstance<WeaponData>();
    mace.weaponName = "Mace";
    mace.weaponType = WeaponType.Mace;
    mace.weaponCategory = WeaponCategory.Melee;  // ADD THIS LINE
    mace.range = 1.2f;
    // ... rest unchanged
}
```

**E. Add new factory methods (at end of class, after CreateMaceData):**
```csharp
// ADD THESE NEW FACTORY METHODS

public static WeaponData CreateBowData()
{
    var bow = CreateInstance<WeaponData>();
    bow.weaponName = "Bow";
    bow.weaponType = WeaponType.Bow;
    bow.weaponCategory = WeaponCategory.Ranged;
    bow.range = 6.0f; // Match RangedAttack skill range
    bow.baseDamage = 10;
    bow.speed = 1.2f;
    bow.stunDuration = 0.5f;
    bow.executionSpeedModifier = -0.1f; // -10% (slightly faster)
    bow.speedResolutionModifier = 0.1f; // +10%
    bow.description = "Standard ranged weapon. Can use RangedAttack skill. Limited melee capabilities.";
    return bow;
}

public static WeaponData CreateCrossbowData()
{
    var crossbow = CreateInstance<WeaponData>();
    crossbow.weaponName = "Crossbow";
    crossbow.weaponType = WeaponType.Crossbow;
    crossbow.weaponCategory = WeaponCategory.Ranged;
    crossbow.range = 7.0f; // Longer range than bow
    crossbow.baseDamage = 15; // Higher damage
    crossbow.speed = 0.8f; // Slower
    crossbow.stunDuration = 1.0f; // Higher stun
    crossbow.executionSpeedModifier = 0.2f; // +20% (slower)
    crossbow.speedResolutionModifier = -0.2f; // -20%

    // Custom skill list: No Counter (too slow to reload)
    crossbow.allowedSkills = new List<SkillType>
    {
        SkillType.RangedAttack,
        SkillType.Defense
        // No Counter - crossbow too slow to counter-attack
    };

    crossbow.description = "Heavy ranged weapon. High damage, slow reload. Cannot counter-attack.";
    return crossbow;
}

public static WeaponData CreateStaffData()
{
    var staff = CreateInstance<WeaponData>();
    staff.weaponName = "Staff";
    staff.weaponType = WeaponType.Staff;
    staff.weaponCategory = WeaponCategory.Magic;
    staff.range = 1.8f;
    staff.baseDamage = 8;
    staff.speed = 1.0f;
    staff.stunDuration = 0.8f;
    staff.executionSpeedModifier = 0f;
    staff.speedResolutionModifier = 0f;

    // Hybrid: Can do basic melee combat + magic
    staff.allowedSkills = new List<SkillType>
    {
        SkillType.Attack,
        SkillType.Defense,
        SkillType.Smash
        // Future: Add magic skills (Fireball, Lightning)
    };

    staff.description = "Magic-channeling staff. Can perform basic melee attacks. Future: Cast spells.";
    return staff;
}

public static WeaponData CreateWandData()
{
    var wand = CreateInstance<WeaponData>();
    wand.weaponName = "Wand";
    wand.weaponType = WeaponType.Wand;
    wand.weaponCategory = WeaponCategory.Magic;
    wand.range = 1.0f;
    wand.baseDamage = 5;
    wand.speed = 1.5f;
    wand.stunDuration = 0.3f;
    wand.executionSpeedModifier = -0.3f; // -30% (very fast)
    wand.speedResolutionModifier = 0.3f; // +30%

    // Pure magic: Only defensive + spells
    wand.allowedSkills = new List<SkillType>
    {
        SkillType.Defense
        // Future: Add magic skills (Fireball, Lightning)
    };

    wand.description = "Pure spellcasting focus. Cannot perform physical attacks. Future: Cast spells.";
    return wand;
}

// HYBRID EXAMPLE: Spear that can throw
public static WeaponData CreateThrowingSpearData()
{
    var spear = CreateInstance<WeaponData>();
    spear.weaponName = "Throwing Spear";
    spear.weaponType = WeaponType.Spear;
    spear.weaponCategory = WeaponCategory.Melee;
    spear.range = 2.5f;
    spear.baseDamage = 9;
    spear.speed = 0.9f;
    spear.stunDuration = 0.9f;
    spear.executionSpeedModifier = 0f;
    spear.speedResolutionModifier = 0f;

    // Custom: All melee skills + can throw (RangedAttack)
    spear.allowedSkills = new List<SkillType>
    {
        SkillType.Attack,
        SkillType.Defense,
        SkillType.Counter,
        SkillType.Smash,
        SkillType.Windmill,
        SkillType.RangedAttack // Can throw spear as projectile
    };

    spear.description = "Hybrid melee/ranged spear. Can be thrown using RangedAttack skill.";
    return spear;
}
```

---

### FILE 3: SkillSystem.cs (MODIFY)

**Location:** `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Changes:**

**A. Update CanChargeSkill() method (around line 116):**

Replace the existing method with:
```csharp
public bool CanChargeSkill(SkillType skillType)
{
    if (!combatController.IsInCombat) return false;
    if (!canAct) return false;
    if (currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Charged) return false;

    // NEW: Check weapon compatibility
    if (!IsSkillAllowedWithCurrentWeapon(skillType))
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot use {skillType} with current weapon ({weaponController.WeaponData.weaponName})");
        return false;
    }

    // Check stamina requirements
    int requiredStamina = GetSkillStaminaCost(skillType);
    return staminaSystem.HasStaminaFor(requiredStamina);
}
```

**B. Update CanExecuteSkill() method (around line 127):**

Replace the existing method with:
```csharp
public bool CanExecuteSkill(SkillType skillType)
{
    // Check weapon compatibility first
    if (!IsSkillAllowedWithCurrentWeapon(skillType))
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot execute {skillType} with current weapon");
        return false;
    }

    // Attack can be executed immediately if basic conditions are met
    if (skillType == SkillType.Attack)
    {
        return CanExecuteAttack();
    }

    // Other skills require charging first
    return currentSkill == skillType && currentState == SkillExecutionState.Charged;
}
```

**C. Update CanExecuteAttack() method (around line 139):**

Replace the existing method with:
```csharp
public bool CanExecuteAttack()
{
    if (!combatController.IsInCombat) return false;
    if (!canAct) return false;
    if (currentState != SkillExecutionState.Uncharged && currentState != SkillExecutionState.Charged) return false;

    // NEW: Check weapon compatibility
    if (!IsSkillAllowedWithCurrentWeapon(SkillType.Attack))
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot use Attack with current weapon");
        return false;
    }

    // Check stamina requirements
    int requiredStamina = GetSkillStaminaCost(SkillType.Attack);
    return staminaSystem.HasStaminaFor(requiredStamina);
}
```

**D. Add new validation method (after CanExecuteAttack, around line 148):**
```csharp
// ADD THIS ENTIRE METHOD
/// <summary>
/// Check if the current weapon allows use of specified skill
/// </summary>
private bool IsSkillAllowedWithCurrentWeapon(SkillType skillType)
{
    // If no weapon equipped, allow all skills (fallback)
    if (weaponController == null || weaponController.WeaponData == null)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"{gameObject.name} has no weapon equipped, allowing all skills");
        return true;
    }

    bool allowed = weaponController.WeaponData.CanUseSkill(skillType);

    if (!allowed && enableDebugLogs)
    {
        Debug.Log($"{gameObject.name} weapon '{weaponController.WeaponData.weaponName}' ({weaponController.WeaponData.weaponCategory}) cannot use skill '{skillType}'");
    }

    return allowed;
}
```

**E. Add helper method to show allowed skills (optional, for debugging):**
```csharp
// ADD THIS OPTIONAL METHOD
/// <summary>
/// Get list of skills allowed with current weapon (for UI/debugging)
/// </summary>
public List<SkillType> GetAllowedSkills()
{
    if (weaponController == null || weaponController.WeaponData == null)
    {
        // Return all skills if no weapon
        return new List<SkillType>
        {
            SkillType.Attack,
            SkillType.Defense,
            SkillType.Counter,
            SkillType.Smash,
            SkillType.Windmill,
            SkillType.RangedAttack
        };
    }

    return weaponController.WeaponData.GetAllowedSkills();
}
```

---

### FILE 4: CombatDebugVisualizer.cs (MODIFY)

**Location:** `Assets/Scripts/Combat/Debug/CombatDebugVisualizer.cs`

**Changes:**

**Update AddSkillInfo() method (around line 141):**

Replace the existing method with:
```csharp
private void AddSkillInfo()
{
    if (skillSystem != null)
    {
        debugText.AppendLine($"Current Skill: {skillSystem.CurrentSkill}");
        debugText.AppendLine($"Skill State: {skillSystem.CurrentState}");

        // Show charge progress for charging skills
        if (skillSystem.CurrentState == SkillExecutionState.Charging ||
            skillSystem.CurrentState == SkillExecutionState.Charged)
        {
            debugText.AppendLine($"Charge Progress: {skillSystem.ChargeProgress:P0}");
        }

        // Show accuracy for aiming skills
        if (skillSystem.CurrentState == SkillExecutionState.Aiming && accuracySystem != null)
        {
            bool targetMoving = false;
            if (accuracySystem.CurrentTarget != null)
            {
                var targetMovement = accuracySystem.CurrentTarget.GetComponent<MovementController>();
                targetMoving = targetMovement != null && targetMovement.IsMoving();
            }

            string movementState = targetMoving ? "MOVING" : "STATIONARY";
            float buildRate = accuracySystem.GetAccuracyBuildRate();

            debugText.AppendLine($"Accuracy: {accuracySystem.CurrentAccuracy:F1}%");
            debugText.AppendLine($"Target: {movementState} (Rate: {buildRate:F1}%/s)");
        }
    }

    if (weaponController != null && weaponController.WeaponData != null)
    {
        var weapon = weaponController.WeaponData;
        debugText.AppendLine($"Weapon: {weapon.weaponName} (Dmg:{weapon.baseDamage} Spd:{weapon.speed:F1} Rng:{weapon.range:F1})");

        // NEW: Show weapon category
        debugText.AppendLine($"Category: {weapon.weaponCategory}");

        // NEW: Show allowed skills (abbreviated)
        string skillAbbrev = GetSkillAbbreviations(weapon);
        debugText.AppendLine($"Allowed: {skillAbbrev}");
    }
}

// ADD THIS NEW HELPER METHOD
private string GetSkillAbbreviations(WeaponData weapon)
{
    var allowedSkills = weapon.GetAllowedSkills();
    var abbreviations = new System.Collections.Generic.List<string>();

    foreach (var skill in allowedSkills)
    {
        string abbrev = skill switch
        {
            SkillType.Attack => "ATK",
            SkillType.Defense => "DEF",
            SkillType.Counter => "CTR",
            SkillType.Smash => "SMS",
            SkillType.Windmill => "WND",
            SkillType.RangedAttack => "RNG",
            _ => skill.ToString()
        };
        abbreviations.Add(abbrev);
    }

    return string.Join("/", abbreviations);
}
```

---

## Weapon Definitions

### Complete Weapon Roster

#### **Melee Weapons (Standard)**

**1. Sword**
```yaml
Type: Sword
Category: Melee
Skills: ATK/DEF/CTR/SMS/WND
Range: 1.5
Damage: 10
Speed: 1.0
Description: Balanced all-rounder. Standard melee weapon.
```

**2. Spear**
```yaml
Type: Spear
Category: Melee
Skills: ATK/DEF/CTR/SMS/WND
Range: 2.5
Damage: 8
Speed: 0.8
Description: Extended reach. Longer range than other melee.
```

**3. Dagger**
```yaml
Type: Dagger
Category: Melee
Skills: ATK/DEF/CTR/SMS/WND
Range: 1.0
Damage: 6
Speed: 1.5
Description: Fast and nimble. Lower damage, highest speed.
```

**4. Mace**
```yaml
Type: Mace
Category: Melee
Skills: ATK/DEF/CTR/SMS (No Windmill)
Range: 1.2
Damage: 15
Speed: 0.6
Description: Heavy hitter. Cannot perform Windmill (too heavy).
Override Reason: Mace too heavy to spin effectively
```

---

#### **Ranged Weapons**

**5. Bow**
```yaml
Type: Bow
Category: Ranged
Skills: ARW/DEF/CTR
Range: 6.0
Damage: 10
Speed: 1.2
Description: Standard ranged weapon. Fires arrows at distance.
```

**6. Crossbow**
```yaml
Type: Crossbow
Category: Ranged
Skills: ARW/DEF (No Counter)
Range: 7.0
Damage: 15
Speed: 0.8
Description: Heavy ranged weapon. Cannot counter (slow reload).
Override Reason: Reload time prevents counter-attacks
```

---

#### **Hybrid Weapons**

**7. Throwing Spear**
```yaml
Type: Spear
Category: Melee
Skills: ATK/DEF/CTR/SMS/WND/RNG
Range: 2.5 (melee), 6.0 (thrown)
Damage: 9
Speed: 0.9
Description: Can be thrown using RangedAttack skill.
Override Reason: Versatile weapon with ranged option
```

---

#### **Magic Weapons** (Future)

**8. Staff**
```yaml
Type: Staff
Category: Magic
Skills: ATK/DEF/SMS + Magic (future)
Range: 1.8
Damage: 8
Speed: 1.0
Description: Melee-capable magic focus. Can cast spells.
Override Reason: Hybrid magic/physical combat
```

**9. Wand**
```yaml
Type: Wand
Category: Magic
Skills: DEF + Magic (future)
Range: 1.0
Damage: 5
Speed: 1.5
Description: Pure spellcasting. No physical attacks.
Override Reason: Designed for spells only
```

---

## Expandability & Special Cases

### Design Patterns for Unique Weapons

#### **Pattern 1: Reduced Skill Set**
```csharp
// Example: Two-Handed Greatsword (no Defense)
greatsword.allowedSkills = new List<SkillType>
{
    SkillType.Attack,
    // No Defense - requires both hands
    SkillType.Counter,
    SkillType.Smash,
    SkillType.Windmill
};
```

#### **Pattern 2: Hybrid Category**
```csharp
// Example: Chakram (thrown + melee)
chakram.weaponCategory = WeaponCategory.Ranged;
chakram.allowedSkills = new List<SkillType>
{
    SkillType.Attack,         // Can melee with circular blade
    SkillType.Defense,
    SkillType.RangedAttack,   // Throw and return
    SkillType.Windmill        // Spin before throwing
};
```

#### **Pattern 3: Stance-Based Weapon** (Future Consideration)
```csharp
// Example: Switchable weapon modes
// Spear: Melee Stance vs Throwing Stance
// Could track stance and dynamically change allowedSkills
// Not implementing now, but architecture supports it
```

---

## Integration with RangedAttack Skill

### Dependency

This system **must be implemented before or alongside** the RangedAttack skill (see RANGED_ATTACK_SKILL_IMPLEMENTATION.md).

### Why?

Without weapon restrictions:
- ❌ Sword users can throw projectiles (breaks immersion)
- ❌ No incentive to switch weapons
- ❌ Ranged becomes strictly better (safer)

With weapon restrictions:
- ✅ Must equip ranged weapon to use RangedAttack
- ✅ Ranged weapons lose melee options (trade-off)
- ✅ Weapon choice becomes strategic decision

---

## Implementation Steps

### Phase 1: Enums & Data Structure

**Step 1:** Update `CombatEnums.cs`
- Add Bow, Crossbow, Staff, Wand to WeaponType
- Add WeaponCategory enum

**Step 2:** Update `WeaponData.cs`
- Add using System.Collections.Generic
- Add weaponCategory field
- Add allowedSkills field
- Add CanUseSkill() method
- Add GetAllowedSkills() method
- Add GetDefaultSkillsForCategory() method

**Step 3:** Update existing weapon factory methods
- Add weaponCategory = WeaponCategory.Melee to Sword/Spear/Dagger/Mace

---

### Phase 2: New Weapons

**Step 4:** Add new weapon factory methods to `WeaponData.cs`
- CreateBowData()
- CreateCrossbowData()
- CreateStaffData() (optional, for future)
- CreateWandData() (optional, for future)
- CreateThrowingSpearData() (optional, hybrid example)

---

### Phase 3: Skill Validation

**Step 5:** Update `SkillSystem.cs`
- Add IsSkillAllowedWithCurrentWeapon() method
- Update CanChargeSkill() to check weapon compatibility
- Update CanExecuteSkill() to check weapon compatibility
- Update CanExecuteAttack() to check weapon compatibility
- Add GetAllowedSkills() helper method (optional)

---

### Phase 4: Debug Display

**Step 6:** Update `CombatDebugVisualizer.cs`
- Update AddSkillInfo() to show weapon category
- Add GetSkillAbbreviations() helper method
- Display allowed skills in debug text

---

### Phase 5: Testing

**Step 7:** Test weapon restrictions
- See Testing Checklist below

---

## Testing Checklist

### Basic Functionality

**Weapon Category Assignment:**
- [ ] Sword shows "Category: Melee"
- [ ] Bow shows "Category: Ranged"
- [ ] Staff shows "Category: Magic"

**Default Skill Access:**
- [ ] Melee weapons show "Allowed: ATK/DEF/CTR/SMS/WND"
- [ ] Ranged weapons show "Allowed: ARW/DEF/CTR"
- [ ] Magic weapons show "Allowed: DEF"

**Skill Validation:**
- [ ] Equipped with Sword → Cannot press key 6 for Arrow
- [ ] Equipped with Bow → Cannot press key 1 for Attack
- [ ] Equipped with Bow → CAN press key 2 for Defense
- [ ] Equipped with Bow → CAN press key 3 for Counter
- [ ] Equipped with Bow → CAN press key 6 for Arrow

---

### Per-Weapon Testing

**Sword (Melee Default):**
- [ ] Can use: Attack, Defense, Counter, Smash, Windmill
- [ ] Cannot use: RangedAttack
- [ ] Debug shows: "Allowed: ATK/DEF/CTR/SMS/WND"

**Bow (Ranged Default):**
- [ ] Can use: RangedAttack, Defense, Counter
- [ ] Cannot use: Attack, Smash, Windmill
- [ ] Debug shows: "Allowed: RNG/DEF/CTR"

**Crossbow (Ranged Override):**
- [ ] Can use: RangedAttack, Defense
- [ ] Cannot use: Attack, Smash, Windmill, Counter
- [ ] Debug shows: "Allowed: RNG/DEF"

**Mace (Melee Override):**
- [ ] Can use: Attack, Defense, Counter, Smash
- [ ] Cannot use: Windmill, RangedAttack
- [ ] Debug shows: "Allowed: ATK/DEF/CTR/SMS"

**Throwing Spear (Hybrid Override):**
- [ ] Can use: Attack, Defense, Counter, Smash, Windmill, RangedAttack
- [ ] Debug shows: "Allowed: ATK/DEF/CTR/SMS/WND/RNG"

---

### Error Handling

**No Weapon Equipped:**
- [ ] If no weapon → Allow all skills (fallback)
- [ ] Log warning: "has no weapon equipped, allowing all skills"

**Blocked Skill Attempt:**
- [ ] Press RangedAttack key with Sword equipped
- [ ] Console shows: "cannot use RangedAttack with current weapon (Sword)"
- [ ] No stamina consumed
- [ ] No state change

---

### Debug Display

- [ ] Shows weapon name
- [ ] Shows weapon category
- [ ] Shows allowed skills (abbreviated)
- [ ] Updates when weapon changes
- [ ] Format example: "Weapon: Bow (Dmg:10 Spd:1.2 Rng:6.0)"
- [ ] Format example: "Category: Ranged"
- [ ] Format example: "Allowed: RNG/DEF/CTR"

---

### Integration with RangedAttack Skill

**Prerequisite:** RangedAttack skill must be implemented (see RANGED_ATTACK_SKILL_IMPLEMENTATION.md)

- [ ] Equip Bow
- [ ] Press key 6 to aim (allowed)
- [ ] Accuracy builds normally
- [ ] Press key 6 to fire
- [ ] RangedAttack fires successfully
- [ ] Switch to Sword
- [ ] Press key 6 to aim (blocked)
- [ ] Console shows restriction message
- [ ] Switch back to Bow
- [ ] RangedAttack works again

---

## Future Expansions

### Potential Additions

**1. Weapon Swapping System**
```csharp
// Switch between equipped weapons mid-combat
public class WeaponInventory : MonoBehaviour
{
    public WeaponData primaryWeapon;
    public WeaponData secondaryWeapon;

    public void SwapWeapon()
    {
        // Switch active weapon
        // Update allowed skills
    }
}
```

**2. Conditional Skill Access**
```csharp
// Skills that require specific conditions
// Example: Arrow only usable when not in melee range
public bool CanUseSkill(SkillType skill)
{
    if (!allowedSkills.Contains(skill))
        return false;

    // Additional conditions
    if (skill == SkillType.RangedAttack && IsInMeleeRange())
        return false; // Can't use ranged attack too close

    return true;
}
```

**3. Skill Proficiency System**
```csharp
// Some weapons better at certain skills
public float GetSkillEffectiveness(SkillType skill)
{
    // Example: Spear does 80% damage with Attack, 120% with Smash
    return skill switch
    {
        SkillType.Attack => 0.8f,
        SkillType.Smash => 1.2f,
        _ => 1.0f
    };
}
```

**4. Dual-Wield Restrictions**
```csharp
// Different rules for two weapons equipped
// Dagger + Dagger = fast attacks, no defense
// Sword + Shield = defense bonus, slower attacks
```

---

## Summary

**Files Modified:** 4
**New Weapon Types:** 4 (Bow, Crossbow, Staff, Wand)
**New Enums:** 1 (WeaponCategory)
**New Methods:** 5 (validation + helpers)
**Lines of Code:** ~200

**Core Feature:** Weapons restrict which skills can be used
**Flexibility:** Override defaults for unique weapons
**Integration:** Works seamlessly with Arrow skill system

---

**End of Implementation Guide**
