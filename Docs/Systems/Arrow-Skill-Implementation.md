# Arrow Skill Implementation Guide

**Date Created:** 2025-10-13
**System Version:** Unity 2023.2.20f1
**Combat System:** FairyGate Rock-Paper-Scissors

---

## Table of Contents
1. [Overview](#overview)
2. [Design Specifications](#design-specifications)
3. [File Changes](#file-changes)
4. [Implementation Steps](#implementation-steps)
5. [Testing Checklist](#testing-checklist)

---

## Overview

### What This Implements
A ranged Arrow skill with accuracy-based instant hit detection (no projectile physics).

### Core Mechanic
- Press key 6 → Start aiming (accuracy builds from 1-100%)
- Press key 6 again → Fire arrow (instant hit/miss based on accuracy roll)
- Press Space → Cancel aim

### Key Features
- ✅ Accuracy builds faster vs stationary targets (40%/s vs 20%/s for moving)
- ✅ Scales with Focus stat (1 + Focus/20 multiplier)
- ✅ Player movement penalty (-10%/s while moving)
- ✅ 50% movement speed while aiming
- ✅ Instant damage application (no projectile travel)
- ✅ Visual LineRenderer trail feedback
- ✅ Integrates with existing combat systems

### Input Pattern Comparison

| Skill | Input Pattern | Execution |
|-------|--------------|-----------|
| Attack | Press 1 | Instant |
| Defense | Press 2 | Charge → Auto-execute |
| Counter | Press 3 | Charge → Auto-execute |
| Smash | Press 4 → Press 4 | Charge → Manual execute |
| Windmill | Press 5 → Press 5 | Charge → Manual execute |
| **Arrow** | **Press 6 → Press 6** | **Aim → Manual fire** |

---

## Design Specifications

### Stats & Balance

```yaml
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
    Focus 10 → 1.5× speed
    Focus 20 → 2.0× speed
    Focus 30 → 2.5× speed

Movement:
  While Aiming: 50% speed
  During Recovery: 0% speed (immobilized)

Miss Scatter:
  Max Angle: 45 degrees
  Formula: Lerp(45°, 0°, accuracy / 100)
  1% accuracy → 45° cone
  50% accuracy → 22.5° cone
  100% accuracy → 0° cone
```

### Damage Formula

```
Arrow Damage = BASE_DAMAGE + Dexterity - Target Physical Defense

Example:
  Base: 10
  Dexterity: 14
  Target Defense: 8
  Final Damage = 10 + 14 - 8 = 16
```

### Accuracy Examples

**Scenario 1: Stationary Target, Focus 10**
- Build Rate: 40% × 1.5 = 60% per second
- Time to 100%: 1.67 seconds
- Time to 75%: 1.25 seconds
- Time to 50%: 0.83 seconds

**Scenario 2: Moving Target, Focus 10**
- Build Rate: 20% × 1.5 = 30% per second
- Time to 100%: 3.33 seconds
- Time to 75%: 2.5 seconds
- Time to 50%: 1.67 seconds

**Scenario 3: Moving Target, Focus 20, Player Moving**
- Build Rate: (20% × 2.0) - 10% = 30% per second
- Time to 100%: 3.33 seconds

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

---

### Modified Files (6)

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
    Arrow        // ADD THIS
}

// Add to SkillExecutionState enum
public enum SkillExecutionState
{
    Uncharged,
    Charging,
    Charged,
    Aiming,      // ADD THIS
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
// Arrow Skill Constants
public const int ARROW_STAMINA_COST = 3;
public const float ARROW_RANGE = 6.0f;
public const int ARROW_BASE_DAMAGE = 10;

// Accuracy System Constants
public const float ACCURACY_BUILD_STATIONARY = 40f;  // % per second
public const float ACCURACY_BUILD_MOVING = 20f;      // % per second
public const float ACCURACY_DECAY_WHILE_MOVING = 10f; // % per second
public const float FOCUS_ACCURACY_DIVISOR = 20f;
public const float MAX_MISS_ANGLE = 45f;             // degrees

// Arrow Movement & Timing
public const float ARROW_AIMING_MOVEMENT_MODIFIER = 0.5f; // 50% speed
public const float ARROW_RECOVERY_TIME = 0.3f;
public const float ARROW_TRAIL_DURATION = 0.5f;
```

---

#### 4. `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

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
[SerializeField] private KeyCode arrowKey = KeyCode.Alpha6;
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

Add Arrow fire input check (after cancel, before other input):
```csharp
// Arrow firing input (if arrow is being aimed)
if (currentState == SkillExecutionState.Aiming && currentSkill == SkillType.Arrow)
{
    if (Input.GetKeyDown(arrowKey))
    {
        FireArrow();
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
            // Arrow skill enters aiming state
            else if (inputSkill.Value == SkillType.Arrow)
            {
                StartAiming(SkillType.Arrow);
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

**E. Add new methods (after existing methods, around line 350):**

```csharp
private void StartAiming(SkillType skillType)
{
    if (skillType != SkillType.Arrow)
    {
        Debug.LogWarning($"StartAiming called with non-arrow skill: {skillType}");
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

    // Check if target in range
    float distanceToTarget = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
    if (distanceToTarget > CombatConstants.ARROW_RANGE)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot aim: target out of range ({distanceToTarget:F1} > {CombatConstants.ARROW_RANGE})");
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
        Debug.Log($"{gameObject.name} started aiming Arrow");
}

private void FireArrow()
{
    if (currentState != SkillExecutionState.Aiming)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot fire: not aiming (state: {currentState})");
        return;
    }

    if (combatController.CurrentTarget == null)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot fire: target lost");
        CancelAim();
        return;
    }

    // Check if target still in range
    float distanceToTarget = Vector3.Distance(transform.position, combatController.CurrentTarget.position);
    if (distanceToTarget > CombatConstants.ARROW_RANGE)
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} cannot fire: target out of range");
        CancelAim();
        return;
    }

    // Consume stamina
    int staminaCost = GetSkillStaminaCost(SkillType.Arrow);
    if (!staminaSystem.ConsumeStamina(staminaCost))
    {
        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} insufficient stamina to fire Arrow");
        CancelAim();
        return;
    }

    // Roll hit chance
    bool isHit = accuracySystem != null ? accuracySystem.RollHitChance() : false;

    if (enableDebugLogs)
    {
        float accuracy = accuracySystem != null ? accuracySystem.CurrentAccuracy : 0f;
        Debug.Log($"{gameObject.name} fired Arrow at {accuracy:F1}% accuracy → {(isHit ? "HIT" : "MISS")}");
    }

    if (isHit)
    {
        // HIT: Apply damage instantly
        int damage = CalculateArrowDamage();
        var targetCombat = combatController.CurrentTarget.GetComponent<CombatController>();
        if (targetCombat != null)
        {
            targetCombat.TakeDamage(damage, transform);

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} Arrow HIT {combatController.CurrentTarget.name} for {damage} damage");
        }

        // Visual: Draw hit trail
        DrawArrowTrail(transform.position, combatController.CurrentTarget.position + Vector3.up * 1f, true);
    }
    else
    {
        // MISS: No damage
        Vector3 missPosition = accuracySystem != null ? accuracySystem.CalculateMissPosition() : combatController.CurrentTarget.position;

        if (enableDebugLogs)
            Debug.Log($"{gameObject.name} Arrow MISSED");

        // Visual: Draw miss trail
        DrawArrowTrail(transform.position, missPosition, false);
    }

    // Stop aiming
    if (accuracySystem != null)
        accuracySystem.StopAiming();

    // Start recovery coroutine
    currentSkillCoroutine = StartCoroutine(ArrowRecoveryCoroutine());
}

private void CancelAim()
{
    if (currentState != SkillExecutionState.Aiming) return;

    if (enableDebugLogs)
        Debug.Log($"{gameObject.name} cancelled Arrow aim");

    if (accuracySystem != null)
        accuracySystem.StopAiming();

    currentState = SkillExecutionState.Uncharged;
    currentSkill = SkillType.Attack;
    movementController.SetMovementModifier(1f);
}

private int CalculateArrowDamage()
{
    // Arrow damage: base + dexterity (scales with ranged skill)
    int damage = CombatConstants.ARROW_BASE_DAMAGE + characterStats.dexterity;
    return damage;
}

private void DrawArrowTrail(Vector3 from, Vector3 to, bool wasHit)
{
    // Create temporary object for trail
    GameObject trailObj = new GameObject("ArrowTrail");
    LineRenderer line = trailObj.AddComponent<LineRenderer>();

    // Configure line appearance
    line.startWidth = 0.08f;
    line.endWidth = 0.08f;
    line.material = new Material(Shader.Find("Sprites/Default"));
    line.startColor = wasHit ? Color.yellow : Color.gray;
    line.endColor = wasHit ? Color.red : Color.gray;
    line.positionCount = 2;

    // Set positions
    line.SetPosition(0, from + Vector3.up * 1.5f); // Archer position
    line.SetPosition(1, to); // Target or miss position

    // Fade out and destroy
    Destroy(trailObj, CombatConstants.ARROW_TRAIL_DURATION);
}

private IEnumerator ArrowRecoveryCoroutine()
{
    // Enter recovery state
    currentState = SkillExecutionState.Recovery;
    movementController.SetMovementModifier(0f);

    yield return new WaitForSeconds(CombatConstants.ARROW_RECOVERY_TIME);

    // Recovery complete
    currentState = SkillExecutionState.Uncharged;
    currentSkill = SkillType.Attack;
    movementController.SetMovementModifier(1f);

    if (enableDebugLogs)
        Debug.Log($"{gameObject.name} Arrow recovery complete");
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
        SkillType.Arrow => CombatConstants.ARROW_STAMINA_COST,  // ADD THIS LINE
        _ => 0
    };
}
```

---

#### 5. `Assets/Scripts/Combat/Core/MovementController.cs`

**Changes:** Add Arrow case to GetSkillMovementModifier() (line ~156):

```csharp
case SkillType.Arrow:
    if (executionState == SkillExecutionState.Aiming)
        return CombatConstants.ARROW_AIMING_MOVEMENT_MODIFIER; // 50% speed while aiming
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

        case SkillType.Arrow:  // ADD THIS ENTIRE CASE
            if (executionState == SkillExecutionState.Aiming)
                return CombatConstants.ARROW_AIMING_MOVEMENT_MODIFIER;
            else
                return 1f;

        default:
            return 1f;
    }
}
```

---

#### 6. `Assets/Scripts/Combat/Core/CombatInteractionManager.cs`

**Changes:** Add Arrow interaction cases to DetermineInteraction() method.

Find the skill interaction logic and add these cases:

```csharp
// Arrow vs Defensive Skills
if (skill1 == SkillType.Arrow && skill2 == SkillType.Defense)
{
    return InteractionResult.DefenderBlocks; // Defense blocks arrow (50% reduction)
}

if (skill1 == SkillType.Arrow && skill2 == SkillType.Counter)
{
    return InteractionResult.CounterReflection; // Counter reflects arrow
}

// Arrow vs Offensive Skills (speed resolution)
if (skill1 == SkillType.Arrow && (skill2 == SkillType.Attack || skill2 == SkillType.Smash || skill2 == SkillType.Windmill))
{
    return InteractionResult.SpeedResolution;
}

// Arrow vs Arrow (simultaneous)
if (skill1 == SkillType.Arrow && skill2 == SkillType.Arrow)
{
    return InteractionResult.SimultaneousExecution;
}

// Reverse cases (other skills vs Arrow)
if (skill2 == SkillType.Arrow && skill1 == SkillType.Defense)
{
    return InteractionResult.DefenderBlocks;
}

if (skill2 == SkillType.Arrow && skill1 == SkillType.Counter)
{
    return InteractionResult.CounterReflection;
}

if (skill2 == SkillType.Arrow && (skill1 == SkillType.Attack || skill1 == SkillType.Smash || skill1 == SkillType.Windmill))
{
    return InteractionResult.SpeedResolution;
}
```

**Note:** Also update ProcessDamage() to handle reduced blocking for Arrow:

```csharp
// In the DefenderBlocks case:
case InteractionResult.DefenderBlocks:
    float damageReductionPercent = 0.75f; // Default 75% reduction

    // Arrows have lower block effectiveness
    if (execution.skillType == SkillType.Arrow)
    {
        damageReductionPercent = 0.50f; // 50% reduction for arrows
    }

    // ... rest of blocking logic
```

---

#### 7. `Assets/Scripts/Combat/Debug/CombatDebugVisualizer.cs`

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

    debugText.AppendLine($"Accuracy: {accuracySystem.CurrentAccuracy:F1}%");
    debugText.AppendLine($"Target: {movementState} (Rate: {buildRate:F1}%/s)");
}
```

Full method context:
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

        // ADD THIS ENTIRE BLOCK
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
    }
}
```

---

## Implementation Steps

### Phase 1: Create AccuracySystem

1. Create new file: `Assets/Scripts/Combat/Systems/AccuracySystem.cs`
2. Copy the full AccuracySystem class implementation (see Appendix A)
3. Save file

### Phase 2: Update Enums and Constants

1. Open `CombatEnums.cs`
2. Add `Arrow` to SkillType enum
3. Add `Aiming` to SkillExecutionState enum
4. Save file

5. Open `CombatConstants.cs`
6. Add all 11 Arrow constants at the end of the class
7. Save file

### Phase 3: Integrate with SkillSystem

1. Open `SkillSystem.cs`
2. Add AccuracySystem component reference
3. Update Awake() to get AccuracySystem component
4. Add arrowKey input field
5. Update HandleSkillInput() with Arrow logic
6. Add 6 new methods: StartAiming(), FireArrow(), CancelAim(), CalculateArrowDamage(), DrawArrowTrail(), ArrowRecoveryCoroutine()
7. Update GetSkillStaminaCost() to include Arrow
8. Save file

### Phase 4: Update Movement System

1. Open `MovementController.cs`
2. Add Arrow case to GetSkillMovementModifier() switch statement
3. Save file

### Phase 5: Combat Interactions

1. Open `CombatInteractionManager.cs`
2. Add Arrow interaction cases to DetermineInteraction()
3. Update ProcessDamage() for reduced Arrow blocking
4. Save file

### Phase 6: Debug Visualization

1. Open `CombatDebugVisualizer.cs`
2. Add AccuracySystem component reference
3. Update Awake() to get AccuracySystem component
4. Update AddSkillInfo() to show accuracy display
5. Save file

### Phase 7: Add Component to GameObjects

1. Open Unity Editor
2. Select Player GameObject
3. Add Component → AccuracySystem
4. Repeat for Enemy GameObject(s)
5. Save scene

### Phase 8: Test

Run through Testing Checklist (see below)

---

## Testing Checklist

### Basic Functionality
- [ ] Press key 6 to start aiming
- [ ] Debug display shows "Skill State: Aiming"
- [ ] Debug display shows "Accuracy: X.X%"
- [ ] Accuracy builds over time (1% → 100%)
- [ ] Press key 6 again to fire arrow
- [ ] See yellow line trail on hit
- [ ] See gray line trail on miss
- [ ] Target takes damage on hit
- [ ] No damage on miss
- [ ] Stamina consumed (3 points)
- [ ] Press Space to cancel aim
- [ ] Aiming cancelled, no stamina consumed

### Accuracy Mechanics
- [ ] Accuracy builds faster vs stationary enemy (~40%/s)
- [ ] Accuracy builds slower vs moving enemy (~20%/s)
- [ ] Higher Focus stat increases build rate
- [ ] Moving while aiming reduces build rate (-10%/s)
- [ ] 100% accuracy always hits (test multiple times)
- [ ] 50% accuracy hits ~half the time (test 10+ times)
- [ ] 1% accuracy almost always misses

### Range & Targeting
- [ ] Can only aim when target in range (6.0 units)
- [ ] Cannot aim without target selected (TAB)
- [ ] Aim auto-cancels if target moves out of range
- [ ] Aim auto-cancels if target dies
- [ ] Cannot fire if target out of range

### Combat Integration
- [ ] Arrow vs Defense → Blocked (reduced damage)
- [ ] Arrow vs Counter → Reflected (future feature)
- [ ] Arrow costs 3 stamina
- [ ] Cannot aim if insufficient stamina
- [ ] Works with existing combat state system

### Movement
- [ ] Player moves at 50% speed while aiming
- [ ] Player immobilized during recovery (0.3s)
- [ ] Movement returns to 100% after recovery

### Debug Display
- [ ] Shows "Current Skill: Arrow" when aiming
- [ ] Shows "Skill State: Aiming"
- [ ] Shows "Accuracy: XX.X%"
- [ ] Shows "Target: MOVING" or "STATIONARY"
- [ ] Shows "Rate: XX.X%/s"
- [ ] Updates in real-time

### Edge Cases
- [ ] Cannot aim if not in combat
- [ ] Aiming cancelled if combat ends
- [ ] Cannot aim at null target
- [ ] Arrow range check works correctly
- [ ] Multiple aim/cancel cycles work correctly
- [ ] Switching to other skills cancels aim

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
        [SerializeField] private bool enableDebugLogs = true;

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

            // Check if target is out of range
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            if (distanceToTarget > CombatConstants.ARROW_RANGE)
            {
                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} target out of range, stopping aim");
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
                return transform.position + transform.forward * CombatConstants.ARROW_RANGE;

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

## Summary

**Files Created:** 1
**Files Modified:** 6
**Lines of Code:** ~400
**Components Added:** AccuracySystem
**Integration:** 90% uses existing systems

**Input:** Press 6 → Aim → Press 6 → Fire
**Mechanic:** Accuracy-based instant hit with visual feedback
**Philosophy:** Strategic timing over execution skill

---

**End of Implementation Guide**
