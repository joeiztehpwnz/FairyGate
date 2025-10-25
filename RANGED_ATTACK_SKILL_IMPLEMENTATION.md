# RangedAttack Skill Implementation Guide

**Date Created:** 2025-10-14
**System Version:** Unity 2023.2.20f1
**Combat System:** FairyGate Rock-Paper-Scissors
**Revision:** 2.0 (Renamed from Arrow, fixed integration issues, added weapon variety)

---

## Table of Contents
1. [Overview](#overview)
2. [Design Specifications](#design-specifications)
3. [Weapon-Based Differentiation](#weapon-based-differentiation)
4. [File Changes](#file-changes)
5. [Implementation Steps](#implementation-steps)
6. [Testing Checklist](#testing-checklist)
7. [Appendix A: Complete AccuracySystem.cs](#appendix-a-complete-accuracysystemcs)
8. [Appendix B: Example Ranged Weapons](#appendix-b-example-ranged-weapons)

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
- **Follows Convention**: Your skill names are verbs/actions, not items

### Input Pattern Comparison

| Skill | Input Pattern | Execution |
|-------|--------------|-----------|
| Attack | Press 1 | Instant |
| Defense | Press 2 | Charge → Auto-execute |
| Counter | Press 3 | Charge → Auto-execute |
| Smash | Press 4 → Press 4 | Charge → Manual execute |
| Windmill | Press 5 → Press 5 | Charge → Manual execute |
| **RangedAttack** | **Press 6 → Press 6** | **Aim → Manual fire** |

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

### Accuracy Examples

**Scenario 1: Stationary Target, Focus 10, Bow**
- Build Rate: 40% × 1.5 = 60% per second
- Time to 100%: 1.67 seconds
- Time to 75%: 1.25 seconds
- Time to 50%: 0.83 seconds

**Scenario 2: Moving Target, Focus 10, Bow**
- Build Rate: 20% × 1.5 = 30% per second
- Time to 100%: 3.33 seconds
- Time to 75%: 2.5 seconds
- Time to 50%: 1.67 seconds

**Scenario 3: Moving Target, Focus 20, Player Moving, Throwing Knife (Speed 1.3)**
- Build Rate: (20% × 2.0) - 10% = 30% per second
- Time to 100%: 3.33 seconds
- Speed advantage: Faster recovery after firing (0.23s vs 0.3s)

---

## Weapon-Based Differentiation

### How Weapons Customize RangedAttack

All ranged weapons use the **same RangedAttack skill**, but each weapon provides:

1. **Range** - From WeaponData.range (Bow: 6.0, Javelin: 4.5, Knife: 3.5)
2. **Damage** - From WeaponData.baseDamage (Bow: 10, Javelin: 14, Knife: 7)
3. **Speed** - From WeaponData.speed (affects recovery time)
4. **Visual** - Projectile type, trail color, trail width
5. **Audio** - Fire sound effect (optional)

### WeaponData Extensions

Add these optional fields to `WeaponData.cs`:

```csharp
[Header("Ranged Attack Properties (Optional)")]
public bool isRangedWeapon = false;
public string projectileType = "Arrow";  // For debug display
public Color trailColorStart = Color.yellow;
public Color trailColorEnd = Color.red;
public float trailWidth = 0.08f;
public AudioClip fireSound;
```

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

## File Changes

### New Files (1)

#### 1. `Assets/Scripts/Combat/Systems/AccuracySystem.cs`

**Purpose:** Tracks accuracy buildup, target state, hit/miss rolling

**Size:** ~200 lines

**Key Methods:**
- `StartAiming(Transform target)` - Begin tracking
- `StopAiming()` - Reset
- `Update()` - Build accuracy based on conditions
- `RollHitChance()` - Return bool (hit/miss)
- `CalculateMissPosition()` - Return Vector3
- `GetAccuracyBuildRate()` - Return current build rate

**See Appendix A for complete code**

---

### Modified Files (8)

#### 2. `Assets/Scripts/Combat/Utilities/Constants/CombatEnums.cs`

**Changes:**
```csharp
// Add to SkillType enum
public enum SkillType
{
    Attack,
    Defense,
    Counter,
    Smash,
    Windmill,
    RangedAttack  // ADD THIS
}

// Add to SkillExecutionState enum
public enum SkillExecutionState
{
    Uncharged,
    Charging,
    Charged,
    Aiming,       // ADD THIS
    Startup,
    Active,
    Recovery,
    Waiting
}
```

---

#### 3. `Assets/Scripts/Combat/Utilities/Constants/CombatConstants.cs`

**Changes:** Add these constants at the end:

```csharp
// RangedAttack Skill Constants
public const int RANGED_ATTACK_STAMINA_COST = 3;
public const float RANGED_ATTACK_BASE_RANGE = 6.0f;    // Default if weapon doesn't override
public const int RANGED_ATTACK_BASE_DAMAGE = 10;       // Default if weapon doesn't override

// Accuracy System Constants
public const float ACCURACY_BUILD_STATIONARY = 40f;     // % per second
public const float ACCURACY_BUILD_MOVING = 20f;         // % per second
public const float ACCURACY_DECAY_WHILE_MOVING = 10f;   // % per second
public const float FOCUS_ACCURACY_DIVISOR = 20f;
public const float MAX_MISS_ANGLE = 45f;                // degrees

// RangedAttack Movement & Timing
public const float RANGED_ATTACK_AIMING_MOVEMENT_MODIFIER = 0.5f; // 50% speed
public const float RANGED_ATTACK_RECOVERY_TIME = 0.3f;
public const float RANGED_ATTACK_TRAIL_DURATION = 0.5f;
```

---

#### 4. `Assets/Scripts/Combat/Stats/SpeedResolver.cs`

**CRITICAL FIX:** Add RangedAttack to offensive skill classification

**Location:** Find the `IsOffensiveSkill()` method

**Changes:**
```csharp
public static bool IsOffensiveSkill(SkillType skill)
{
    return skill == SkillType.Attack ||
           skill == SkillType.Smash ||
           skill == SkillType.Windmill ||
           skill == SkillType.RangedAttack;  // ADD THIS LINE
}
```

**Why Critical:** Without this, RangedAttack won't be processed by CombatInteractionManager and won't trigger any skill interactions (blocks, counters, speed resolution).

---

#### 5. `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Changes:**

**A. Add component reference (line ~38):**
```csharp
private AccuracySystem accuracySystem;
```

**B. Update Awake() (line ~55):**
```csharp
accuracySystem = GetComponent<AccuracySystem>();
```

**C. Add input key (line ~22):**
```csharp
[SerializeField] private KeyCode rangedAttackKey = KeyCode.Alpha6;
```

**D. Update HandleSkillInput() (line ~71):**

Replace the cancel block:
```csharp
// Cancel skill input
if (Input.GetKeyDown(cancelKey))
{
    if (currentState == SkillExecutionState.Aiming)
    {
        CancelAim();
        return;
    }

    CancelSkill();
    return;
}
```

Add RangedAttack fire input check (after cancel, before other input):
```csharp
// RangedAttack firing input (if ranged attack is being aimed)
if (currentState == SkillExecutionState.Aiming && currentSkill == SkillType.RangedAttack)
{
    if (Input.GetKeyDown(rangedAttackKey))
    {
        ExecuteSkill(SkillType.RangedAttack);
        return;
    }
}
```

Update skill charging/aiming input section:
```csharp
// Skill charging/aiming input (if not currently busy)
if (currentState == SkillExecutionState.Uncharged || currentState == SkillExecutionState.Charged)
{
    SkillType? inputSkill = GetSkillFromInput();
    if (inputSkill.HasValue)
    {
        if (currentSkill != inputSkill.Value || currentState == SkillExecutionState.Uncharged)
        {
            // Attack skill executes immediately without charging
            if (inputSkill.Value == SkillType.Attack)
            {
                ExecuteSkill(SkillType.Attack);
            }
            // RangedAttack skill enters aiming state
            else if (inputSkill.Value == SkillType.RangedAttack)
            {
                StartAiming(SkillType.RangedAttack);
            }
            // Other skills charge normally
            else
            {
                StartCharging(inputSkill.Value);
            }
        }
    }
}
```

**E. Update GetSkillFromInput() (line ~415):**
```csharp
private SkillType? GetSkillFromInput()
{
    if (Input.GetKeyDown(attackKey)) return SkillType.Attack;
    if (Input.GetKeyDown(defenseKey)) return SkillType.Defense;
    if (Input.GetKeyDown(counterKey)) return SkillType.Counter;
    if (Input.GetKeyDown(smashKey)) return SkillType.Smash;
    if (Input.GetKeyDown(windmillKey)) return SkillType.Windmill;
    if (Input.GetKeyDown(rangedAttackKey)) return SkillType.RangedAttack;  // ADD THIS LINE
    return null;
}
```

**F. Update GetSkillStaminaCost() (around line 402):**
```csharp
private int GetSkillStaminaCost(SkillType skillType)
{
    return skillType switch
    {
        SkillType.Attack => CombatConstants.ATTACK_STAMINA_COST,
        SkillType.Defense => CombatConstants.DEFENSE_STAMINA_COST,
        SkillType.Counter => CombatConstants.COUNTER_STAMINA_COST,
        SkillType.Smash => CombatConstants.SMASH_STAMINA_COST,
        SkillType.Windmill => CombatConstants.WINDMILL_STAMINA_COST,
        SkillType.RangedAttack => CombatConstants.RANGED_ATTACK_STAMINA_COST,  // ADD THIS LINE
        _ => 0
    };
}
```

**G. Add new methods (after existing methods, around line 350):**

```csharp
private void StartAiming(SkillType skillType)
{
    if (skillType != SkillType.RangedAttack)
    {
        Debug.LogWarning($"StartAiming called with non-ranged skill: {skillType}");
        return;
    }

    if (!combatController.IsInCombat)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot aim: not in combat");
        return;
    }

    if (combatController.CurrentTarget == null)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot aim: no target");
        return;
    }

    // STAMINA CHECK MOVED HERE (before aiming starts)
    int requiredStamina = GetSkillStaminaCost(SkillType.RangedAttack);
    if (!staminaSystem.HasStaminaFor(requiredStamina))
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot aim: insufficient stamina");
        return;
    }

    // Check if target in range (use weapon range)
    float weaponRange = weaponController.WeaponData != null
        ? weaponController.WeaponData.range
        : CombatConstants.RANGED_ATTACK_BASE_RANGE;

    float distanceToTarget = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
    if (distanceToTarget > weaponRange)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot aim: target out of range ({distanceToTarget:F1} > {weaponRange})");
        return;
    }

    currentSkill = skillType;
    currentState = SkillExecutionState.Aiming;

    // Start accuracy tracking
    if (accuracySystem != null)
        accuracySystem.StartAiming(combatController.CurrentTarget);

    // Apply movement restriction
    movementController.ApplySkillMovementRestriction(skillType, currentState);

    if (enableDebugLogs)
        Debug.Log($"{gameObject.name} started aiming RangedAttack");
}

private void CancelAim()
{
    if (currentState != SkillExecutionState.Aiming) return;

    if (enableDebugLogs)
        Debug.Log($"{gameObject.name} cancelled RangedAttack aim");

    if (accuracySystem != null)
        accuracySystem.StopAiming();

    currentState = SkillExecutionState.Uncharged;
    currentSkill = SkillType.Attack;
    movementController.SetMovementModifier(1f);
}
```

**H. Update ExecuteSkillCoroutine() to handle RangedAttack (around line 293):**

Find the section that processes skill execution and add RangedAttack handling:

```csharp
private IEnumerator ExecuteSkillCoroutine(SkillType skillType)
{
    // SPECIAL HANDLING FOR RANGED ATTACK
    if (skillType == SkillType.RangedAttack)
    {
        // RangedAttack uses custom flow: Aiming → Fire → Recovery
        yield return StartCoroutine(ExecuteRangedAttackCoroutine());
        yield break;
    }

    // STANDARD FLOW FOR OTHER SKILLS
    // Startup phase
    currentState = SkillExecutionState.Startup;
    movementController.ApplySkillMovementRestriction(skillType, currentState);

    float startupTime = SpeedResolver.CalculateExecutionTime(skillType, weaponController.WeaponData, SkillExecutionState.Startup);
    yield return new WaitForSeconds(startupTime);

    // ... rest of existing code ...
}

private IEnumerator ExecuteRangedAttackCoroutine()
{
    // Validation checks
    if (currentState != SkillExecutionState.Aiming)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot fire: not aiming (state: {currentState})");
        yield break;
    }

    if (combatController.CurrentTarget == null)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot fire: target lost");
        CancelAim();
        yield break;
    }

    // Range check (use weapon range)
    float weaponRange = weaponController.WeaponData != null
        ? weaponController.WeaponData.range
        : CombatConstants.RANGED_ATTACK_BASE_RANGE;

    float distanceToTarget = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
    if (distanceToTarget > weaponRange)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot fire: target out of range");
        CancelAim();
        yield break;
    }

    // Consume stamina
    int staminaCost = GetSkillStaminaCost(SkillType.RangedAttack);
    if (!staminaSystem.ConsumeStamina(staminaCost))
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} insufficient stamina to fire RangedAttack");
        CancelAim();
        yield break;
    }

    // Enter Active state (brief, for interaction processing)
    currentState = SkillExecutionState.Active;
    movementController.SetMovementModifier(0f);

    // Roll hit chance
    bool isHit = accuracySystem != null ? accuracySystem.RollHitChance() : false;

    if (enableDebugLogs)
    {
        float accuracy = accuracySystem != null ? accuracySystem.CurrentAccuracy : 0f;
        Debug.Log($"{gameObject.name} fired RangedAttack at {accuracy:F1}% accuracy → {(isHit ? "HIT" : "MISS")}");
    }

    // Process through interaction manager (CRITICAL - enables blocks/counters/speed resolution)
    CombatInteractionManager.Instance?.ProcessSkillExecution(this, SkillType.RangedAttack);

    // Apply hit/miss effects
    if (isHit)
    {
        // HIT: Damage applied by interaction manager, show hit trail
        DrawRangedAttackTrail(transform.position, combatController.CurrentTarget.position + Vector3.up * 1f, true);
    }
    else
    {
        // MISS: No damage, show miss trail
        Vector3 missPosition = accuracySystem != null
            ? accuracySystem.CalculateMissPosition()
            : combatController.CurrentTarget.position;

        DrawRangedAttackTrail(transform.position, missPosition, false);
    }

    // Stop aiming
    if (accuracySystem != null)
        accuracySystem.StopAiming();

    // Brief active time for interaction processing
    yield return new WaitForSeconds(0.1f);

    // Recovery phase
    currentState = SkillExecutionState.Recovery;
    movementController.SetMovementModifier(0f);

    float recoveryTime = CombatConstants.RANGED_ATTACK_RECOVERY_TIME;

    // Scale recovery by weapon speed (faster weapons = faster recovery)
    if (weaponController.WeaponData != null)
    {
        recoveryTime = recoveryTime / weaponController.WeaponData.speed;
    }

    yield return new WaitForSeconds(recoveryTime);

    // Skill complete
    currentState = SkillExecutionState.Uncharged;
    currentSkill = SkillType.Attack;
    chargeProgress = 0f;
    movementController.SetMovementModifier(1f);

    OnSkillExecuted.Invoke(SkillType.RangedAttack, isHit);

    if (enableDebugLogs)
    {
        Debug.Log($"{gameObject.name} RangedAttack execution complete (hit: {isHit})");
    }
}

private void DrawRangedAttackTrail(Vector3 from, Vector3 to, bool wasHit)
{
    var weapon = weaponController.WeaponData;

    // Get weapon-specific visual properties or use defaults
    Color startColor = Color.yellow;
    Color endColor = wasHit ? Color.red : Color.gray;
    float width = 0.08f;
    string projectileType = "Projectile";

    if (weapon != null && weapon.isRangedWeapon)
    {
        startColor = weapon.trailColorStart;
        endColor = wasHit ? weapon.trailColorEnd : Color.gray;
        width = weapon.trailWidth;
        projectileType = weapon.projectileType;
    }

    // Create temporary object for trail
    GameObject trailObj = new GameObject($"{projectileType}Trail");
    LineRenderer line = trailObj.AddComponent<LineRenderer>();

    // Configure line appearance
    line.startWidth = width;
    line.endWidth = width;
    line.material = new Material(Shader.Find("Sprites/Default"));
    line.startColor = startColor;
    line.endColor = endColor;
    line.positionCount = 2;

    // Set positions
    line.SetPosition(0, from + Vector3.up * 1.5f); // Shooter position
    line.SetPosition(1, to); // Target or miss position

    // Play weapon-specific sound if available
    if (weapon != null && weapon.fireSound != null)
    {
        AudioSource.PlayClipAtPoint(weapon.fireSound, from);
    }

    // Fade out and destroy
    Destroy(trailObj, CombatConstants.RANGED_ATTACK_TRAIL_DURATION);
}
```

---

#### 6. `Assets/Scripts/Combat/Core/MovementController.cs`

**Changes:** Add RangedAttack case to GetSkillMovementModifier() (line ~156):

```csharp
case SkillType.RangedAttack:
    if (executionState == SkillExecutionState.Aiming)
        return CombatConstants.RANGED_ATTACK_AIMING_MOVEMENT_MODIFIER; // 50% speed while aiming
    else
        return 1f;
```

Full method context:
```csharp
private float GetSkillMovementModifier(SkillType skillType, SkillExecutionState executionState)
{
    // Movement stops completely during execution phase
    if (executionState == SkillExecutionState.Startup ||
        executionState == SkillExecutionState.Active ||
        executionState == SkillExecutionState.Recovery)
    {
        return 0f;
    }

    // Movement restrictions during charging, aiming, and waiting states
    switch (skillType)
    {
        case SkillType.Attack:
        case SkillType.Smash:
            return 1f;

        case SkillType.Defense:
            return (executionState == SkillExecutionState.Charging || executionState == SkillExecutionState.Waiting)
                ? CombatConstants.DEFENSE_MOVEMENT_SPEED_MODIFIER
                : 1f;

        case SkillType.Counter:
            if (executionState == SkillExecutionState.Charging)
                return CombatConstants.COUNTER_MOVEMENT_SPEED_MODIFIER;
            else if (executionState == SkillExecutionState.Waiting)
                return 0f;
            else
                return 1f;

        case SkillType.Windmill:
            if (executionState == SkillExecutionState.Charging)
                return CombatConstants.WINDMILL_MOVEMENT_SPEED_MODIFIER;
            else if (executionState == SkillExecutionState.Charged || executionState == SkillExecutionState.Active)
                return 0f;
            else
                return 1f;

        case SkillType.RangedAttack:  // ADD THIS ENTIRE CASE
            if (executionState == SkillExecutionState.Aiming)
                return CombatConstants.RANGED_ATTACK_AIMING_MOVEMENT_MODIFIER;
            else
                return 1f;

        default:
            return 1f;
    }
}
```

---

#### 7. `Assets/Scripts/Combat/Core/CombatInteractionManager.cs`

**Changes:** Add RangedAttack interaction cases to DetermineInteraction() method.

Find the skill interaction logic and add these cases:

```csharp
// RangedAttack vs Defensive Skills
case SkillType.RangedAttack:
    switch (defensive)
    {
        case SkillType.Defense:
            return InteractionResult.DefenderBlocks; // Defense blocks ranged attack (50% reduction)
        case SkillType.Counter:
            return InteractionResult.CounterReflection; // Counter reflects ranged attack
    }
    break;
```

**Full context of updated DetermineInteraction():**
```csharp
private InteractionResult DetermineInteraction(SkillType offensive, SkillType defensive)
{
    // All interactions from the matrix (now includes RangedAttack)
    switch (offensive)
    {
        case SkillType.Attack:
            switch (defensive)
            {
                case SkillType.Defense: return InteractionResult.AttackerStunned;
                case SkillType.Counter: return InteractionResult.CounterReflection;
            }
            break;

        case SkillType.Smash:
            switch (defensive)
            {
                case SkillType.Defense: return InteractionResult.DefenderKnockedDown;
                case SkillType.Counter: return InteractionResult.CounterReflection;
            }
            break;

        case SkillType.Windmill:
            switch (defensive)
            {
                case SkillType.Defense: return InteractionResult.DefenderBlocks;
                case SkillType.Counter: return InteractionResult.WindmillBreaksCounter;
            }
            break;

        case SkillType.RangedAttack:  // ADD THIS ENTIRE CASE
            switch (defensive)
            {
                case SkillType.Defense: return InteractionResult.DefenderBlocks;
                case SkillType.Counter: return InteractionResult.CounterReflection;
            }
            break;
    }

    return InteractionResult.NoInteraction;
}
```

**Also update ProcessInteractionEffects() for RangedAttack damage reduction:**

Find the `DefenderBlocks` case and update it:

```csharp
case InteractionResult.DefenderBlocks:
    float damageReductionPercent = 0.75f; // Default 75% reduction for most attacks

    // RangedAttacks have lower block effectiveness (harder to block projectiles)
    if (attacker.skillType == SkillType.RangedAttack)
    {
        damageReductionPercent = 0.50f; // 50% reduction for ranged attacks
    }
    // Windmill has no damage reduction (already handled by blocking entirely)
    else if (attacker.skillType == SkillType.Windmill)
    {
        damageReductionPercent = 0f; // 0 damage (clean block)
    }

    if (damageReductionPercent > 0f)
    {
        int baseDamage = DamageCalculator.CalculateBaseDamage(attackerStats, attackerWeapon, defenderStats);
        int reducedDamage = DamageCalculator.ApplyDamageReduction(baseDamage, damageReductionPercent, defenderStats);
        defenderHealth.TakeDamage(reducedDamage, attacker.combatant.transform);
    }

    if (enableDebugLogs)
    {
        Debug.Log($"{defender.combatant.name} blocked {attacker.combatant.name} {attacker.skillType}");
    }
    break;
```

---

#### 8. `Assets/Scripts/Combat/Debug/CombatDebugVisualizer.cs`

**Changes:**

**A. Add component reference (line ~35):**
```csharp
private AccuracySystem accuracySystem;
```

**B. Update Awake() (line ~40):**
```csharp
accuracySystem = GetComponent<AccuracySystem>();
```

**C. Update AddSkillInfo() method (line ~141):**

Add this block after the charge progress display:

```csharp
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

    // Show weapon-specific info if available
    string weaponInfo = "";
    if (weaponController?.WeaponData != null && weaponController.WeaponData.isRangedWeapon)
    {
        weaponInfo = $" ({weaponController.WeaponData.projectileType})";
    }

    debugText.AppendLine($"Accuracy: {accuracySystem.CurrentAccuracy:F1}%{weaponInfo}");
    debugText.AppendLine($"Target: {movementState} (Rate: {buildRate:F1}%/s)");
}
```

---

#### 9. `Assets/Scripts/Data/WeaponData/WeaponData.cs`

**Changes:** Add optional ranged weapon properties

**Location:** After the `description` field (around line 29)

**Add:**
```csharp
[Header("Ranged Attack Properties (Optional)")]
public bool isRangedWeapon = false;
[Tooltip("Projectile type name for debug display")]
public string projectileType = "Arrow";
[Tooltip("Trail start color")]
public Color trailColorStart = Color.yellow;
[Tooltip("Trail end color (on hit)")]
public Color trailColorEnd = Color.red;
[Tooltip("Trail width")]
public float trailWidth = 0.08f;
[Tooltip("Sound effect when firing")]
public AudioClip fireSound;
```

**See Appendix B for complete example weapon factory methods**

---

## Implementation Steps

### Phase 1: Create AccuracySystem

1. Create new file: `Assets/Scripts/Combat/Systems/AccuracySystem.cs`
2. Copy the full AccuracySystem class implementation (see Appendix A)
3. Save file

### Phase 2: Update Enums and Constants

1. Open `CombatEnums.cs`
2. Add `RangedAttack` to SkillType enum
3. Add `Aiming` to SkillExecutionState enum
4. Save file

5. Open `CombatConstants.cs`
6. Add all 11 RangedAttack constants at the end of the class
7. Save file

### Phase 3: Fix Skill Classification (CRITICAL)

1. Open `SpeedResolver.cs`
2. Find `IsOffensiveSkill()` method
3. Add `SkillType.RangedAttack` to the return statement
4. Save file

### Phase 4: Integrate with SkillSystem

1. Open `SkillSystem.cs`
2. Add AccuracySystem component reference
3. Update Awake() to get AccuracySystem component
4. Add rangedAttackKey input field
5. Update HandleSkillInput() with RangedAttack logic
6. Update GetSkillFromInput() to include rangedAttackKey
7. Add StartAiming() and CancelAim() methods
8. Update ExecuteSkillCoroutine() to handle RangedAttack
9. Add ExecuteRangedAttackCoroutine() method
10. Add DrawRangedAttackTrail() method
11. Update GetSkillStaminaCost() to include RangedAttack
12. Save file

### Phase 5: Update Movement System

1. Open `MovementController.cs`
2. Add RangedAttack case to GetSkillMovementModifier() switch statement
3. Save file

### Phase 6: Combat Interactions

1. Open `CombatInteractionManager.cs`
2. Add RangedAttack case to DetermineInteraction()
3. Update ProcessInteractionEffects() DefenderBlocks case for reduced RangedAttack blocking
4. Save file

### Phase 7: Debug Visualization

1. Open `CombatDebugVisualizer.cs`
2. Add AccuracySystem component reference
3. Update Awake() to get AccuracySystem component
4. Update AddSkillInfo() to show accuracy display with weapon info
5. Save file

### Phase 8: Weapon Data Extensions

1. Open `WeaponData.cs`
2. Add ranged weapon property fields (isRangedWeapon, projectileType, colors, etc.)
3. Save file

### Phase 9: Create Ranged Weapons (Optional)

1. Add factory methods for ranged weapons to `WeaponData.cs` (see Appendix B)
2. Or create weapons via Unity Editor using ScriptableObject menu
3. Set isRangedWeapon = true and configure visual properties

### Phase 10: Add Component to GameObjects

1. Open Unity Editor
2. Select Player GameObject
3. Add Component → AccuracySystem
4. Repeat for Enemy GameObject(s)
5. Save scene

### Phase 11: Test

Run through Testing Checklist (see below)

---

## Testing Checklist

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
- [ ] Cannot aim without sufficient stamina (check before aiming starts)

### Accuracy Mechanics
- [ ] Accuracy builds faster vs stationary enemy (~40%/s)
- [ ] Accuracy builds slower vs moving enemy (~20%/s)
- [ ] Higher Focus stat increases build rate
- [ ] Moving while aiming reduces build rate (-10%/s)
- [ ] 100% accuracy always hits (test multiple times)
- [ ] 50% accuracy hits ~half the time (test 10+ times)
- [ ] 1% accuracy almost always misses

### Range & Targeting
- [ ] Can only aim when target in range (uses weapon.range)
- [ ] Cannot aim without target selected (TAB)
- [ ] Aim auto-cancels if target moves out of range
- [ ] Aim auto-cancels if target dies
- [ ] Cannot fire if target out of range
- [ ] Different weapons have different ranges (Bow 6.0, Javelin 4.5, etc.)

### Combat Integration (CRITICAL TESTS)
- [ ] RangedAttack vs Defense → Blocked with reduced damage (50% reduction)
- [ ] RangedAttack vs Counter → Reflected back to attacker (knockdown + damage)
- [ ] RangedAttack vs RangedAttack → Speed resolution or simultaneous
- [ ] RangedAttack vs Attack/Smash/Windmill → Speed resolution
- [ ] RangedAttack costs 3 stamina
- [ ] Cannot aim if insufficient stamina
- [ ] Works with existing combat state system
- [ ] CombatInteractionManager processes RangedAttack correctly

### Weapon Variety
- [ ] Equip Bow: Yellow-to-red trail, 6.0 range, 10 base damage
- [ ] Equip Javelin: Gray-to-white thick trail, 4.5 range, 14 base damage, slower recovery
- [ ] Equip Throwing Knife: Cyan-to-blue thin trail, 3.5 range, 7 base damage, faster recovery
- [ ] Equip Sling: Brown-to-gray trail, 5.0 range, 6 base damage
- [ ] Equip Throwing Axe: Red-to-dark-red trail, 3.0 range, 12 base damage
- [ ] Debug display shows projectile type (Arrow, Javelin, Knife, Stone, Axe)
- [ ] Weapon speed affects recovery time (fast weapons = faster recovery)

### Movement
- [ ] Player moves at 50% speed while aiming
- [ ] Player immobilized during recovery
- [ ] Movement returns to 100% after recovery
- [ ] Recovery time scales with weapon speed

### Debug Display
- [ ] Shows "Current Skill: RangedAttack" when aiming
- [ ] Shows "Skill State: Aiming"
- [ ] Shows "Accuracy: XX.X%"
- [ ] Shows projectile type if weapon has it (e.g., "(Arrow)")
- [ ] Shows "Target: MOVING" or "STATIONARY"
- [ ] Shows "Rate: XX.X%/s"
- [ ] Updates in real-time

### Edge Cases
- [ ] Cannot aim if not in combat
- [ ] Aiming cancelled if combat ends
- [ ] Cannot aim at null target
- [ ] Range check uses weapon.range correctly
- [ ] Multiple aim/cancel cycles work correctly
- [ ] Switching to other skills cancels aim
- [ ] Weapon swap while aiming cancels aim
- [ ] RangedAttack classified as offensive skill (check IsOffensiveSkill() returns true)
- [ ] Melee weapons cannot use RangedAttack (if weapon restrictions enabled)

### AI Integration
- [ ] AI can select RangedAttack if equipped with ranged weapon
- [ ] AI aims at appropriate accuracy threshold before firing
- [ ] AI respects range limitations

---

## Appendix A: Complete AccuracySystem.cs

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    public class AccuracySystem : MonoBehaviour
    {
        [Header("Accuracy Configuration")]
        [SerializeField] private float stationaryBuildRate = 40f; // % per second
        [SerializeField] private float movingBuildRate = 20f; // % per second
        [SerializeField] private float playerMovementDecayRate = 10f; // % per second
        [SerializeField] private float focusScalingDivisor = 20f;
        [SerializeField] private float maxMissAngle = 45f; // degrees

        [Header("Current State")]
        [SerializeField] private float currentAccuracy = 1f;
        [SerializeField] private bool isAiming = false;
        [SerializeField] private Transform currentTarget;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        // Component references
        private CharacterStats characterStats;
        private CombatController combatController;
        private MovementController movementController;

        // Properties
        public float CurrentAccuracy => currentAccuracy;
        public bool IsAiming => isAiming;
        public Transform CurrentTarget => currentTarget;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            movementController = GetComponent<MovementController>();

            // Get character stats
            characterStats = combatController?.Stats;
            if (characterStats == null)
            {
                Debug.LogWarning($"AccuracySystem on {gameObject.name} could not find CharacterStats");
                characterStats = CharacterStats.CreateDefaultStats();
            }
        }

        private void Update()
        {
            if (isAiming)
            {
                UpdateAccuracy();
            }
        }

        public void StartAiming(Transform target)
        {
            if (target == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"{gameObject.name} cannot start aiming: no target");
                return;
            }

            isAiming = true;
            currentTarget = target;
            currentAccuracy = 1f;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} started aiming at {target.name}");
        }

        public void StopAiming()
        {
            isAiming = false;
            currentTarget = null;
            currentAccuracy = 1f;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} stopped aiming");
        }

        private void UpdateAccuracy()
        {
            if (currentTarget == null)
            {
                StopAiming();
                return;
            }

            // Calculate base build rate based on target movement
            bool targetIsMoving = IsTargetMoving();
            float baseBuildRate = targetIsMoving ? movingBuildRate : stationaryBuildRate;

            // Apply focus multiplier
            float focusMultiplier = CalculateFocusMultiplier();
            float effectiveBuildRate = baseBuildRate * focusMultiplier;

            // Apply decay if player is moving
            if (movementController.IsMoving())
            {
                effectiveBuildRate -= playerMovementDecayRate;
            }

            // Update accuracy
            currentAccuracy += effectiveBuildRate * Time.deltaTime;
            currentAccuracy = Mathf.Clamp(currentAccuracy, 1f, 100f);
        }

        private bool IsTargetMoving()
        {
            if (currentTarget == null) return false;

            var targetMovement = currentTarget.GetComponent<MovementController>();
            if (targetMovement == null) return false;

            return targetMovement.IsMoving();
        }

        private float CalculateFocusMultiplier()
        {
            // Higher focus = faster accuracy buildup
            // Formula: 1 + (Focus / 20)
            // 10 Focus = 1.5x, 20 Focus = 2.0x, 30 Focus = 2.5x
            return 1f + (characterStats.focus / focusScalingDivisor);
        }

        public bool RollHitChance()
        {
            float hitRoll = Random.Range(0f, 100f);
            bool isHit = hitRoll <= currentAccuracy;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} rolled {hitRoll:F1} vs {currentAccuracy:F1}% accuracy → {(isHit ? "HIT" : "MISS")}");

            return isHit;
        }

        public Vector3 CalculateMissPosition()
        {
            if (currentTarget == null)
                return transform.position + transform.forward * 6f;

            Vector3 targetPosition = currentTarget.position + Vector3.up * 1f;

            // Lower accuracy = wider miss cone
            // 100% accuracy = 0° cone (shouldn't miss, but just in case)
            // 50% accuracy = 22.5° cone
            // 1% accuracy = 45° cone
            float missAngle = Mathf.Lerp(maxMissAngle, 0f, currentAccuracy / 100f);

            // Random angle within cone (horizontal and vertical)
            float randomHorizontalAngle = Random.Range(-missAngle, missAngle);
            float randomVerticalAngle = Random.Range(-missAngle * 0.5f, missAngle * 0.5f);

            // Direction to target
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;

            // Apply random rotation
            Quaternion rotation = Quaternion.Euler(randomVerticalAngle, randomHorizontalAngle, 0f);
            Vector3 missDirection = rotation * directionToTarget;

            // Calculate position at target distance
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            return transform.position + (missDirection * distanceToTarget);
        }

        public float GetAccuracyBuildRate()
        {
            if (!isAiming || currentTarget == null) return 0f;

            bool targetIsMoving = IsTargetMoving();
            float baseBuildRate = targetIsMoving ? movingBuildRate : stationaryBuildRate;
            float focusMultiplier = CalculateFocusMultiplier();
            float effectiveBuildRate = baseBuildRate * focusMultiplier;

            if (movementController.IsMoving())
            {
                effectiveBuildRate -= playerMovementDecayRate;
            }

            return Mathf.Max(effectiveBuildRate, 0f);
        }
    }
}
```

---

## Appendix B: Example Ranged Weapons

Add these factory methods to `WeaponData.cs`:

```csharp
// BOW - Standard archer weapon
public static WeaponData CreateBowData()
{
    var bow = CreateInstance<WeaponData>();
    bow.weaponName = "Bow";
    bow.weaponType = WeaponType.Bow;
    bow.range = 6.0f;
    bow.baseDamage = 10;
    bow.speed = 1.0f;
    bow.stunDuration = 0.3f;
    bow.executionSpeedModifier = 0f;
    bow.speedResolutionModifier = 0f;
    bow.isRangedWeapon = true;
    bow.projectileType = "Arrow";
    bow.trailColorStart = Color.yellow;
    bow.trailColorEnd = Color.red;
    bow.trailWidth = 0.08f;
    bow.description = "Standard ranged weapon with good range and accuracy";
    return bow;
}

// JAVELIN - Heavy thrown weapon
public static WeaponData CreateJavelinData()
{
    var javelin = CreateInstance<WeaponData>();
    javelin.weaponName = "Javelin";
    javelin.weaponType = WeaponType.Javelin;
    javelin.range = 4.5f;
    javelin.baseDamage = 14;
    javelin.speed = 0.8f; // Slower weapon = longer recovery
    javelin.stunDuration = 1.2f;
    javelin.executionSpeedModifier = 0.1f;
    javelin.speedResolutionModifier = -0.15f;
    javelin.isRangedWeapon = true;
    javelin.projectileType = "Javelin";
    javelin.trailColorStart = new Color(0.6f, 0.6f, 0.6f); // Gray
    javelin.trailColorEnd = Color.white;
    javelin.trailWidth = 0.12f; // Thicker trail
    javelin.description = "Heavy thrown weapon - high damage, shorter range, slower";
    return javelin;
}

// THROWING KNIFE - Fast, low damage
public static WeaponData CreateThrowingKnifeData()
{
    var knife = CreateInstance<WeaponData>();
    knife.weaponName = "Throwing Knife";
    knife.weaponType = WeaponType.ThrowingKnife;
    knife.range = 3.5f;
    knife.baseDamage = 7;
    knife.speed = 1.3f; // Fast weapon = quick recovery
    knife.stunDuration = 0.2f;
    knife.executionSpeedModifier = -0.15f;
    knife.speedResolutionModifier = 0.15f;
    knife.isRangedWeapon = true;
    knife.projectileType = "Knife";
    knife.trailColorStart = Color.cyan;
    knife.trailColorEnd = Color.blue;
    knife.trailWidth = 0.05f; // Thin trail
    knife.description = "Quick ranged attack - low damage, fast speed, short range";
    return knife;
}

// SLING - Ancient projectile weapon
public static WeaponData CreateSlingData()
{
    var sling = CreateInstance<WeaponData>();
    sling.weaponName = "Sling";
    sling.weaponType = WeaponType.Sling;
    sling.range = 5.0f;
    sling.baseDamage = 6;
    sling.speed = 1.1f;
    sling.stunDuration = 0.8f; // Blunt impact
    sling.executionSpeedModifier = 0f;
    sling.speedResolutionModifier = 0f;
    sling.isRangedWeapon = true;
    sling.projectileType = "Stone";
    sling.trailColorStart = new Color(0.5f, 0.4f, 0.3f); // Brown/tan
    sling.trailColorEnd = Color.gray;
    sling.trailWidth = 0.06f;
    sling.description = "Simple ranged weapon using stones as projectiles";
    return sling;
}

// THROWING AXE - Hybrid melee/ranged
public static WeaponData CreateThrowingAxeData()
{
    var axe = CreateInstance<WeaponData>();
    axe.weaponName = "Throwing Axe";
    axe.weaponType = WeaponType.ThrowingAxe;
    axe.range = 3.0f; // Short range when thrown
    axe.baseDamage = 12;
    axe.speed = 0.9f;
    axe.stunDuration = 1.0f;
    axe.executionSpeedModifier = 0.05f;
    axe.speedResolutionModifier = -0.1f;
    axe.isRangedWeapon = true; // Can use RangedAttack
    axe.projectileType = "Axe";
    axe.trailColorStart = Color.red;
    axe.trailColorEnd = new Color(0.5f, 0f, 0f); // Dark red
    axe.trailWidth = 0.10f;
    axe.description = "Versatile weapon - can use melee attacks AND ranged throw";
    return axe;
}
```

**Note:** You'll also need to add these weapon types to the `WeaponType` enum in `CombatEnums.cs`:
```csharp
public enum WeaponType
{
    Sword,
    Spear,
    Dagger,
    Mace,
    Bow,            // ADD
    Javelin,        // ADD
    ThrowingKnife,  // ADD
    Sling,          // ADD
    ThrowingAxe     // ADD
}
```

---

## Summary

**Files Created:** 1 (AccuracySystem.cs)
**Files Modified:** 8
**Lines of Code:** ~600 new + ~200 modified
**Components Added:** AccuracySystem
**Integration:** 95% uses existing systems

**Input:** Press 6 → Aim → Press 6 → Fire
**Mechanic:** Accuracy-based instant hit with weapon-specific visuals
**Philosophy:** Strategic timing over execution skill, weapon variety without skill bloat

**Key Improvements Over Original "Arrow" Design:**
- ✅ Fixed critical combat integration issues
- ✅ Proper offensive skill classification
- ✅ Stamina check timing improved
- ✅ Weapon-based differentiation (one skill, multiple weapons)
- ✅ Better naming (RangedAttack vs Arrow)
- ✅ Works with weapon restriction system
- ✅ Supports hybrid weapons (Throwing Axe)

---

**End of Implementation Guide**
