# FairyGate Combat System - Component Reference Guide

**Version**: 1.0
**Last Updated**: 2025-10-25
**Target Audience**: Advanced developers needing technical reference

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Diagrams](#architecture-diagrams)
3. [Quick Component Lookup](#quick-component-lookup)
4. [Core Combat System](#core-combat-system)
5. [AI System](#ai-system)
6. [Equipment System](#equipment-system)
7. [Skill System](#skill-system)
8. [Stats & Status Effects](#stats--status-effects)
9. [UI System](#ui-system)
10. [Editor Tools](#editor-tools)
11. [Data Structures](#data-structures)
12. [Event Flow Map](#event-flow-map)
13. [Common Patterns](#common-patterns)
14. [Troubleshooting](#troubleshooting)

---

## System Overview

The FairyGate combat system is a **rock-paper-scissors** skill interaction system with 6 skills and 17 defined interactions. It uses a component-based architecture with:

- **Facade Pattern**: `CombatController` as the main interface
- **Event-Driven Communication**: UnityEvents for loose coupling
- **Data-Driven Design**: ScriptableObjects for weapons, characters, equipment
- **Speed-Based Resolution**: Stats + weapon modifiers determine execution order
- **Pattern-Based AI**: Abstract base class for predictable enemy behaviors

### Core Design Principles

1. **Single Responsibility**: Each component handles one aspect (health, stamina, skills, etc.)
2. **Dependency Injection**: Components reference each other via GetComponent in Awake
3. **Centralized Interaction Logic**: `CombatInteractionManager` handles all skill resolution
4. **No Circular Dependencies**: Equipment bonuses calculated on-demand, not cached in stats

---

## Architecture Diagrams

### System Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ HealthBarUI  │  │ StaminaBarUI │  │   Debug      │      │
│  │  (OnGUI)     │  │  (OnGUI)     │  │ Visualizer   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                             ▲
                             │
┌─────────────────────────────────────────────────────────────┐
│                      FACADE LAYER                            │
│              ┌──────────────────────────┐                    │
│              │   CombatController       │                    │
│              │  (Implements ICombatant) │                    │
│              └──────────────────────────┘                    │
└─────────────────────────────────────────────────────────────┘
                             ▲
                             │
┌─────────────────────────────────────────────────────────────┐
│                     SYSTEMS LAYER                            │
│  ┌─────────┐  ┌────────┐  ┌──────────┐  ┌──────────┐      │
│  │ Health  │  │Stamina │  │  Skill   │  │ Status   │      │
│  │ System  │  │System  │  │ System   │  │ Effect   │      │
│  └─────────┘  └────────┘  └──────────┘  └──────────┘      │
│  ┌─────────┐  ┌────────┐  ┌──────────┐  ┌──────────┐      │
│  │Movement │  │ Weapon │  │Equipment │  │Knockdown │      │
│  │  Ctrl   │  │  Ctrl  │  │ Manager  │  │  Meter   │      │
│  └─────────┘  └────────┘  └──────────┘  └──────────┘      │
└─────────────────────────────────────────────────────────────┘
                             ▲
                             │
┌─────────────────────────────────────────────────────────────┐
│                   INTERACTION LAYER                          │
│            ┌────────────────────────────┐                    │
│            │ CombatInteractionManager   │                    │
│            │      (Singleton)           │                    │
│            └────────────────────────────┘                    │
└─────────────────────────────────────────────────────────────┘
                             ▲
                             │
┌─────────────────────────────────────────────────────────────┐
│                      DATA LAYER                              │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ Character   │  │   Weapon     │  │  Equipment   │       │
│  │   Stats     │  │    Data      │  │    Data      │       │
│  │ (ScriptObj) │  │ (ScriptObj)  │  │ (ScriptObj)  │       │
│  └─────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

### Component Dependency Graph

```
CombatController (Facade)
├── HealthSystem
│   ├── CharacterStats
│   ├── StaminaSystem
│   ├── StatusEffectManager
│   └── EquipmentManager
├── StaminaSystem
│   ├── CharacterStats
│   ├── CombatController
│   ├── SkillSystem
│   └── EquipmentManager
├── SkillSystem
│   ├── CharacterStats
│   ├── WeaponController
│   ├── StaminaSystem
│   ├── MovementController
│   ├── CombatController
│   ├── StatusEffectManager
│   └── AccuracySystem
├── StatusEffectManager
│   ├── CharacterStats
│   ├── MovementController
│   └── SkillSystem
├── WeaponController
│   └── WeaponData
├── MovementController
│   ├── CharacterStats
│   └── SkillSystem
├── EquipmentManager
│   ├── CombatController
│   ├── EquipmentData
│   └── EquipmentSet
└── KnockdownMeterTracker
    ├── CharacterStats
    └── StatusEffectManager

CombatInteractionManager (Singleton)
└── All CombatControllers in scene
```

---

## Quick Component Lookup

| Component | Path | Primary Purpose |
|-----------|------|-----------------|
| **CombatController** | `Combat/Core/` | Main facade, implements ICombatant |
| **CombatInteractionManager** | `Combat/Core/` | Singleton, resolves all skill interactions |
| **MovementController** | `Combat/Core/` | Handles character movement with skill restrictions |
| **SkillSystem** | `Combat/Skills/Base/` | Skill charging, execution, state machine |
| **HealthSystem** | `Combat/Systems/` | HP management, damage, death |
| **StaminaSystem** | `Combat/Systems/` | Stamina, regen, drain, auto-cancel |
| **StatusEffectManager** | `Combat/StatusEffects/` | Stun, knockdown, rest effects |
| **KnockdownMeterTracker** | `Combat/Systems/` | Meter-based knockdown system |
| **AccuracySystem** | `Combat/Systems/` | Ranged attack accuracy tracking |
| **EquipmentManager** | `Combat/Equipment/` | Armor/accessory stat bonuses |
| **WeaponController** | `Combat/Weapons/` | Weapon handling, range checks |
| **PatternedAI** | `Combat/AI/` | Abstract base for pattern-based AI |
| **KnightAI** | `Combat/AI/` | 8-second pattern implementation |
| **TestRepeaterAI** | `Combat/AI/` | Testing AI, repeats chosen skill |
| **SimpleTestAI** | `Combat/AI/` | Random skill selection |
| **SpeedResolver** | `Combat/Stats/` | Speed calculations, skill classification |
| **DamageCalculator** | `Combat/Stats/` | Damage, stun duration, stat-based modifiers |
| **CombatDebugVisualizer** | `Combat/Debug/` | On-screen debug display |
| **HealthBarUI** | `UI/` | OnGUI health bar |
| **StaminaBarUI** | `UI/` | OnGUI stamina bar |
| **CompleteCombatSceneSetup** | `Editor/` | One-click scene setup tool |

---

## Core Combat System

### CombatController

**File**: `Assets/Scripts/Combat/Core/CombatController.cs`

**Purpose**: Main combat facade that aggregates all combat subsystems and implements `ICombatant`.

**Public API**:
```csharp
// Properties
CharacterStats Stats { get; }           // Equipment-modified stats
Transform CurrentTarget { get; }
bool IsInCombat { get; }
bool IsAlive { get; }
int CurrentHealth { get; }
int MaxHealth { get; }
SkillExecutionState CurrentState { get; }
SkillType CurrentSkill { get; }

// Combat State
void EnterCombat(Transform target)
void ExitCombat()
void SetTarget(Transform target)

// Skill Execution (delegates to SkillSystem)
bool CanChargeSkill(SkillType skillType)
void StartCharging(SkillType skillType)
void ExecuteSkill(SkillType skillType)
void CancelSkill()

// Health (delegates to HealthSystem)
void TakeDamage(int damage, Transform source)
void Die()

// Status Effects (delegates to StatusEffectManager)
void ApplyStatusEffect(StatusEffect effect)
void RemoveStatusEffect(StatusEffectType type)

// Events
UnityEvent OnCombatEntered
UnityEvent OnCombatExited
UnityEvent<Transform> OnTargetChanged
```

**Dependencies**:
- `HealthSystem`
- `StaminaSystem`
- `StatusEffectManager`
- `WeaponController`
- `SkillSystem`
- `MovementController`
- `EquipmentManager` (optional)

**Execution Flow**:
```
Update()
  ├── HandleCombatInput()
  │   ├── Tab → CycleTarget()
  │   ├── Esc → ExitCombat()
  │   └── X → ToggleRest()
  └── UpdateCombatState()
      └── DetermineCombatState()
          ├─ IsAlive? → Dead
          ├─ IsKnockedDown? → KnockedDown
          ├─ IsStunned? → Stunned
          ├─ IsResting? → Resting
          ├─ Skill Charging? → Charging
          ├─ Skill Executing? → Executing
          ├─ Has Target? → Combat
          └─ Default → Idle
```

**Design Rationale**:
- **Facade Pattern**: Provides clean interface hiding complexity of 7+ subsystems
- **No Direct Logic**: All actual logic delegated to specialized components
- **Interface Implementation**: ICombatant allows polymorphic access (AI, interaction manager)

**Common Usage**:
```csharp
// AI script accessing combat capabilities
var combatant = GetComponent<CombatController>();
if (combatant.CanChargeSkill(SkillType.Smash))
{
    combatant.StartCharging(SkillType.Smash);
}

// Damage from external source
combatant.TakeDamage(20, attackerTransform);
```

---

### CombatInteractionManager

**File**: `Assets/Scripts/Combat/Core/CombatInteractionManager.cs`

**Purpose**: Singleton that resolves all skill interactions based on 17-interaction matrix from `minimal.md`.

**Public API**:
```csharp
static CombatInteractionManager Instance { get; }
void ProcessSkillExecution(SkillSystem skillSystem, SkillType skillType)
```

**Execution Flow**:
```
ProcessSkillExecution(skillSystem, skillType)
  │
  ├─ IsOffensiveSkill()?
  │   └─ pendingExecutions.Enqueue(execution)
  │
  └─ IsDefensiveSkill()?
      └─ waitingDefensiveSkills.Add(execution)

Update() → ProcessPendingExecutions()
  │
  ├─ Collect simultaneous offensive skills (0.1s window)
  │
  ├─ Single offensive skill?
  │   ├─ GetValidDefensiveResponses()
  │   │   └─ Check range, can interact, attacker in range
  │   ├─ No defenses? → ExecuteOffensiveSkillDirectly()
  │   └─ Has defenses? → ProcessSkillInteraction()
  │       ├─ DetermineInteraction() → InteractionResult
  │       └─ ProcessInteractionEffects()
  │           ├─ AttackerStunned → Apply stun to both
  │           ├─ CounterReflection → Knockdown + reflect damage
  │           ├─ DefenderKnockedDown → Knockdown + reduced damage
  │           ├─ DefenderBlocks → 0 damage (or stay active on miss)
  │           └─ WindmillBreaksCounter → Knockdown + damage
  │
  └─ Multiple offensive skills?
      └─ ResolveSpeedConflicts()
          ├─ Calculate speeds for all
          ├─ Winner executes
          └─ Losers cancelled
```

**Interaction Matrix** (from minimal.md):
```
Attack vs Defense → AttackerStunned (both get stun, defender blocks)
Attack vs Counter → CounterReflection (attacker knockdown + reflected damage)
Smash vs Defense → DefenderKnockedDown (defender knockdown + 25% damage)
Smash vs Counter → CounterReflection (attacker knockdown + reflected damage)
Windmill vs Defense → DefenderBlocks (0 damage, clean block)
Windmill vs Counter → WindmillBreaksCounter (defender knockdown + damage)
RangedAttack vs Defense → DefenderBlocks (100% block on hit, stay active on miss)
RangedAttack vs Counter → CounterIneffective (full damage on hit, 0 on miss)
```

**Design Rationale**:
- **Centralized Logic**: All 17 interactions in one place, easy to maintain
- **Queue-Based**: Handles simultaneous executions with 0.1s window
- **Range Validation**: Dual range check (defender + attacker weapon range)
- **Special Case Handling**: RangedAttack has different logic (hit/miss affects defensive response)

**Key Implementation Details**:
```csharp
// Lines 167-212: CanDefensiveSkillRespond()
// SPECIAL CASE: Ranged attacks don't require defender to be in melee range
bool isRangedAttack = offensiveSkill.skillType == SkillType.RangedAttack;
if (!isRangedAttack && !defenderWeapon.IsInRange(attackerTransform))
{
    return false; // Defender must be in melee range for non-ranged
}
```

---

### MovementController

**File**: `Assets/Scripts/Combat/Core/MovementController.cs`

**Purpose**: Character movement with skill-based speed modifiers.

**Public API**:
```csharp
// Properties
bool CanMove { get; }
float CurrentSpeed { get; }
Vector3 CurrentVelocity { get; }

// Control
void SetCanMove(bool canMoveValue)
void SetMovementModifier(float modifier)
void SetMovementInput(Vector3 inputDirection)  // For AI control
void ApplySkillMovementRestriction(SkillType skillType, SkillExecutionState executionState)

// Utilities
float GetDistanceTo(Transform target)
bool IsMoving()
void ResetMovementSpeed()
```

**Movement Modifiers by Skill**:
```
Attack/Smash Charging:     100% speed (full movement)
Defense Charging:           70% speed (CombatConstants.DEFENSE_MOVEMENT_SPEED_MODIFIER)
Defense Waiting:            70% speed
Counter Charging:           70% speed (CombatConstants.COUNTER_MOVEMENT_SPEED_MODIFIER)
Counter Waiting:             0% speed (immobilized)
Windmill Charging:          70% speed (CombatConstants.WINDMILL_MOVEMENT_SPEED_MODIFIER)
Windmill Charged:            0% speed (immobilized)
RangedAttack Aiming:        50% speed (CombatConstants.RANGED_ATTACK_AIMING_MOVEMENT_MODIFIER)
All Startup/Active/Recovery: 0% speed (immobilized)
```

**Execution Flow**:
```
Update()
  └─ UpdateMovement()
      ├─ Get input (keyboard if player, AI vector if AI-controlled)
      ├─ Normalize direction
      ├─ Apply currentMovementSpeed
      ├─ Apply gravity
      └─ CharacterController.Move(velocity * deltaTime)
```

**Design Rationale**:
- **Skill-Responsive**: Different skills have different movement restrictions
- **CharacterController Integration**: Uses Unity physics for proper collision
- **Dual Control Mode**: Supports both player keyboard and AI programmatic input

---

## AI System

### PatternedAI (Abstract Base Class)

**File**: `Assets/Scripts/Combat/AI/PatternedAI.cs`

**Purpose**: Abstract base for pattern-based enemy AI behaviors.

**Public API**:
```csharp
// Abstract methods (must implement)
protected abstract IEnumerator ExecutePattern()
protected abstract string GetPatternName()

// Helper methods for concrete implementations
protected void SetPatternPhase(string phaseName, float duration)
protected IEnumerator WaitForPhaseComplete(float duration)
protected bool IsPlayerInRange()
protected void MoveTowardsPlayer()
protected void StopMovement()

// Public queries
string GetCurrentPatternPhase()
float GetCurrentPhaseProgress()
bool IsExecutingPattern()
```

**Component References**:
- `CombatController`
- `SkillSystem`
- `MovementController`
- `StatusEffectManager`
- `Transform player` (auto-found)

**Execution Flow**:
```
Update()
  ├─ UpdateCombatState()
  │   ├─ distanceToPlayer <= engageDistance? → EnterCombat()
  │   └─ distanceToPlayer > disengageDistance? → ExitCombat()
  └─ UpdatePatternState()
      └─ Update currentPhaseProgress for visualization

EnterCombat()
  └─ StartPatternLoop()
      └─ StartCoroutine(PatternLoop())
          └─ while (isAlive && isInCombat)
              ├─ Wait if cannot act (knockdown)
              ├─ ExecutePattern() [ABSTRACT - implemented by subclass]
              └─ Cooldown (patternCooldown seconds)
```

**Design Rationale**:
- **Template Method Pattern**: Base class handles combat state, subclass defines pattern
- **Visualization Support**: Tracks current phase and progress for debug display
- **Interruption Handling**: Automatically pauses pattern during knockdown

**Common Pattern**: See KnightAI implementation below.

---

### KnightAI

**File**: `Assets/Scripts/Combat/AI/KnightAI.cs`

**Purpose**: 8-second repeating pattern demonstrating pattern-based combat.

**Pattern Cycle**:
```
Phase 1: Charge Defense (1.0s)
Phase 2: Wait Defensively (3.0s) ← VULNERABLE WINDOW
Phase 3: Cancel Defense (0.5s)
Phase 4: Charge Smash (1.5s) ← ATTACK WINDOW
Phase 5: Execute Smash (0.5s) ← DANGER WINDOW
Phase 6: Recovery (1.5s) ← COUNTERATTACK WINDOW
         └─ REPEAT
Total: 8 seconds
```

**Implementation**:
```csharp
protected override IEnumerator ExecutePattern()
{
    // Phase 1: Charge Defense
    SetPatternPhase("Charge Defense", 1.0f);
    skillSystem.StartCharging(SkillType.Defense);
    yield return WaitForPhaseComplete(1.0f);

    // Phase 2: Wait Defensively (vulnerable window)
    SetPatternPhase("Wait Defensively", 3.0f);
    yield return WaitForPhaseComplete(3.0f);

    // Phase 3: Cancel Defense
    SetPatternPhase("Cancel Defense", 0.5f);
    skillSystem.CancelSkill();
    yield return WaitForPhaseComplete(0.5f);

    // Phase 4: Charge Smash (attack window)
    SetPatternPhase("Charge Smash", 1.5f);
    skillSystem.StartCharging(SkillType.Smash);
    yield return WaitForPhaseComplete(1.5f);

    // Phase 5: Execute Smash (danger window)
    if (IsPlayerInRange())
    {
        SetPatternPhase("Execute Smash", 0.5f);
        skillSystem.ExecuteSkill(SkillType.Smash);
        yield return WaitForPhaseComplete(0.5f);
    }

    // Phase 6: Recovery (counterattack window)
    SetPatternPhase("Recovery", 1.5f);
    yield return WaitForPhaseComplete(1.5f);
}
```

**Strategic Windows**:
- **Vulnerable**: Phase 2 (waiting defensively) - safe to attack
- **Attack**: Phase 4 (charging Smash) - can interrupt with fast skills
- **Danger**: Phase 5 (executing Smash) - avoid or defend
- **Counterattack**: Phase 6 (recovery) - optimal damage window

---

### TestRepeaterAI

**File**: `Assets/Scripts/Combat/AI/TestRepeaterAI.cs`

**Purpose**: Testing AI that repeats a chosen skill. Controllable via F1-F6 hotkeys.

**Configuration**:
```csharp
[SerializeField] private SkillType selectedSkill = SkillType.Attack
[SerializeField] private float repeatDelay = 1.0f
[SerializeField] private bool addRandomDelay = false
[SerializeField] private bool maintainDefensiveState = false
[SerializeField] private float defensiveWaitDuration = 3.0f
[SerializeField] private bool infiniteStamina = false
[SerializeField] private bool skipRangedAiming = false
[SerializeField] private bool enableMovement = false
```

**Hotkey Control** (via TestSkillSelector):
```
F1 → selectedSkill = Attack
F2 → selectedSkill = Defense
F3 → selectedSkill = Counter
F4 → selectedSkill = Smash
F5 → selectedSkill = Windmill
F6 → selectedSkill = RangedAttack
F7 → Toggle maintainDefensiveState
F8 → Toggle infiniteStamina
F9 → Toggle enableMovement
```

**Design Rationale**:
- **Systematic Testing**: Allows testing specific interactions repeatedly
- **Runtime Reconfiguration**: F1-F6 hotkeys change behavior without restarting
- **Dev Mode Features**: Infinite stamina, instant aim, maintained defense for thorough testing

---

## Equipment System

### EquipmentManager

**File**: `Assets/Scripts/Combat/Equipment/EquipmentManager.cs`

**Purpose**: Manages armor and accessory equipment, applies stat bonuses.

**Public API**:
```csharp
// Properties
EquipmentData CurrentArmor { get; }
EquipmentData CurrentAccessory { get; }
CharacterStats ModifiedStats { get; }  // Base stats + equipment bonuses

// Equipment Management
bool EquipItem(EquipmentData equipment)     // Returns false if in combat
bool UnequipItem(EquipmentSlot slot)
void EquipSet(EquipmentSet set)             // Quick set swap
void RefreshEquipmentBonuses()

// Events
UnityEvent<EquipmentData, EquipmentSlot> OnEquipmentChanged
UnityEvent OnEquipmentRefreshed
```

**Stat Modification Flow**:
```
RefreshEquipmentBonuses()
  ├─ Copy baseStats → modifiedStats
  ├─ ApplyEquipmentBonuses(currentArmor)
  │   ├─ modifiedStats.strength += armor.strengthBonus
  │   ├─ modifiedStats.dexterity += armor.dexterityBonus
  │   ├─ modifiedStats.physicalDefense += armor.physicalDefenseBonus
  │   └─ modifiedStats.focus += armor.focusBonus
  └─ ApplyEquipmentBonuses(currentAccessory)
      └─ [Same as above for accessory]

NOTE: MaxHealth and MaxStamina bonuses handled separately
      in HealthSystem/StaminaSystem to avoid circular dependencies
```

**Design Rationale**:
- **No Circular Dependencies**: ModifiedStats is a runtime copy, not a reference
- **Combat Restriction**: Cannot change equipment during combat (balance decision)
- **Separate HP/Stamina**: Health/Stamina systems query equipment directly for max bonuses

**Usage Example**:
```csharp
// Runtime equipment swap (outside combat)
var manager = GetComponent<EquipmentManager>();
manager.EquipItem(heavyPlatemail);  // Armor
manager.EquipItem(guardianRing);    // Accessory

// Quick set swap
manager.EquipSet(fortressTankSet);

// Access modified stats
int totalStrength = manager.ModifiedStats.strength;
```

---

### EquipmentData (ScriptableObject)

**File**: `Assets/Scripts/Combat/Equipment/EquipmentData.cs`

**Purpose**: Defines stat bonuses for armor and accessories.

**Fields**:
```csharp
string equipmentName
EquipmentSlot slot  // Armor or Accessory
Sprite icon

// Stat bonuses
int strengthBonus
int dexterityBonus
int physicalDefenseBonus
int focusBonus
int maxHealthBonus
int maxStaminaBonus
```

**Asset Location**: `Assets/Data/Equipment/Armor/` and `Assets/Data/Equipment/Accessories/`

**Example Assets**:
- **HeavyPlatemail**: +15 Def, +30 HP, -1 Spd
- **LeatherTunic**: +5 Dex, +10 HP
- **GuardianRing**: +10 Def, +20 HP
- **SwiftBoots**: +2 Spd, +5 Dex

---

### EquipmentSet (ScriptableObject)

**File**: `Assets/Scripts/Combat/Equipment/EquipmentSet.cs`

**Purpose**: Preset combination of armor + accessory for quick builds.

**Fields**:
```csharp
string setName
string description
EquipmentData armor
EquipmentData accessory
// Note: Weapon handled separately by WeaponController
```

**Preset Builds**:
1. **Fortress_TankSet**: Heavy Platemail + Guardian Ring (+30 HP, +15 Def, -1 Spd)
2. **Windrunner_SpeedSet**: Leather Tunic + Swift Boots (+2 Spd, +5 Dex)
3. **Berserker_GlassCannonSet**: Cloth Robes + Power Gauntlets (+10 Str, -5 Def)
4. **Wanderer_BalancedSet**: Chain Mail + Meditation Amulet (moderate all stats)

---

## Skill System

### SkillSystem

**File**: `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Purpose**: Skill state machine, charging, execution, and interaction processing.

**Public API**:
```csharp
// Properties
SkillExecutionState CurrentState { get; }
SkillType CurrentSkill { get; }
float ChargeProgress { get; }  // 0.0 to 1.0
bool LastRangedAttackHit { get; }

// Skill Execution
bool CanChargeSkill(SkillType skillType)
bool CanExecuteSkill(SkillType skillType)
void StartCharging(SkillType skillType)
void ExecuteSkill(SkillType skillType)
void CancelSkill()

// Special: Ranged Attack
void StartAiming(SkillType.RangedAttack)
void CancelAim()

// Internal (called by CombatInteractionManager)
void ForceTransitionToRecovery()  // Completes defensive skills after interaction

// Events
UnityEvent<SkillType> OnSkillCharged
UnityEvent<SkillType, bool> OnSkillExecuted  // skill, success
UnityEvent<SkillType> OnSkillCancelled
```

**Skill State Machine**:
```
Uncharged
  │
  ├─ StartCharging(skill) → Charging
  │   └─ ChargeSkill() coroutine
  │       └─ After chargeTime → Charged
  │           └─ If defensive → Auto-execute
  │
  ├─ StartAiming(RangedAttack) → Aiming
  │   └─ AccuracySystem tracking accuracy
  │       └─ ExecuteSkill() → Active → Recovery → Uncharged
  │
  └─ ExecuteSkill(Attack) → Startup → Active → Recovery → Uncharged

Charged
  └─ ExecuteSkill(skill) → Startup → Active → Recovery → Uncharged
      │
      └─ If defensive skill: Active → Waiting → Recovery
          └─ Waiting state drains stamina until:
              ├─ Interaction occurs → ForceTransitionToRecovery()
              ├─ Stamina depletes → Auto-cancel
              └─ Manual cancel → CancelSkill()
```

**Special Execution Paths**:

**Attack Skill** (instant execution):
```csharp
// Line 122-125: Attack bypasses charging
if (inputSkill.Value == SkillType.Attack)
{
    ExecuteSkill(SkillType.Attack);
}
```

**Ranged Attack** (aiming flow):
```csharp
StartAiming()
  ├─ Check in combat, has target, in range
  ├─ currentState = Aiming
  ├─ AccuracySystem.StartAiming()
  └─ Wait for player to press key again...

ExecuteSkill(RangedAttack)
  ├─ Validate target still in range
  ├─ Consume stamina
  ├─ RollHitChance() → LastRangedAttackHit
  ├─ ProcessSkillExecution() [ALWAYS called, even on miss]
  ├─ DrawRangedAttackTrail() [yellow→red on hit, yellow→gray on miss]
  └─ Recovery → Uncharged
```

**Defensive Skills** (waiting state):
```csharp
ExecuteSkillCoroutine(Defense or Counter)
  ├─ Startup phase
  ├─ Active phase → ProcessSkillExecution()
  │   └─ CombatInteractionManager adds to waitingDefensiveSkills
  ├─ Waiting phase → HandleDefensiveWaitingState()
  │   └─ Drain stamina continuously
  │       └─ while (currentState == Waiting)
  │           └─ StaminaSystem.DrainStamina(rate, deltaTime)
  └─ Recovery phase [only after ForceTransitionToRecovery()]
```

**Design Rationale**:
- **State Machine**: Clear, predictable skill flow
- **Coroutine-Based**: Handles timing without Update() polling
- **Attack is Special**: Instant execution for responsiveness
- **RangedAttack is Special**: Aiming state builds accuracy over time
- **Defensive Skills Wait**: Remain active until interaction or stamina depletion

---

### AccuracySystem

**File**: `Assets/Scripts/Combat/Systems/AccuracySystem.cs`

**Purpose**: Tracks ranged attack accuracy based on target movement and aiming time.

**Public API**:
```csharp
float CurrentAccuracy { get; }  // 0-100%

void StartAiming(Transform target)
void StopAiming()
bool RollHitChance()            // Returns true if attack hits
Vector3 CalculateMissPosition() // Calculates where missed shot goes
```

**Accuracy Calculation**:
```
Base Accuracy Build Rate:
  - Stationary target: 50% per second
  - Moving target:     15% per second (slower)

Modified by:
  - Character dexterity stat
  - Weapon speed stat
  - Target movement speed

Max Accuracy: 95% (never 100% to maintain risk)
```

**Usage** (called by SkillSystem):
```csharp
// Start aiming
accuracySystem.StartAiming(target);

// Build accuracy over time (automatic in Update)
// ...

// Fire when ready
bool hit = accuracySystem.RollHitChance();
if (hit)
{
    // Apply damage
}
else
{
    Vector3 missPos = accuracySystem.CalculateMissPosition();
    // Show miss trail
}
```

---

## Stats & Status Effects

### HealthSystem

**File**: `Assets/Scripts/Combat/Systems/HealthSystem.cs`

**Public API**:
```csharp
// Properties
int CurrentHealth { get; }
int MaxHealth { get; }  // Base + equipment bonuses
bool IsAlive { get; }
float HealthPercentage { get; }

// Damage & Healing
void TakeDamage(int damage, Transform source)
void Heal(int healAmount)
void Die()

// Utilities
void SetHealth(int health)
void RestoreToFull()
void ResetForTesting()

// Events
UnityEvent<int, Transform> OnDamageReceived
UnityEvent<Transform> OnDied
UnityEvent<int, int> OnHealthChanged  // current, max
```

**MaxHealth Calculation**:
```csharp
// Lines 28-43
int MaxHealth
{
    get
    {
        int baseHealth = characterStats?.MaxHealth ?? CombatConstants.BASE_HEALTH;
        int bonus = 0;

        if (equipmentManager != null)
        {
            if (equipmentManager.CurrentArmor != null)
                bonus += equipmentManager.CurrentArmor.maxHealthBonus;
            if (equipmentManager.CurrentAccessory != null)
                bonus += equipmentManager.CurrentAccessory.maxHealthBonus;
        }

        return baseHealth + bonus;
    }
}
```

**Death Flow**:
```
Die()
  ├─ currentHealth = 0
  ├─ statusEffectManager.ClearAllStatusEffects()
  ├─ combatController.ExitCombat()
  ├─ OnDied.Invoke(transform)
  └─ GameManager.Instance?.OnCharacterDied(this)
```

---

### StaminaSystem

**File**: `Assets/Scripts/Combat/Systems/StaminaSystem.cs`

**Public API**:
```csharp
// Properties
int CurrentStamina { get; }
int MaxStamina { get; }  // Base + equipment bonuses
bool IsResting { get; }
float StaminaPercentage { get; }

// Stamina Management
bool HasStaminaFor(int cost)
bool ConsumeStamina(int amount)
void RegenerateStamina(float amount)
void DrainStamina(float drainRate, float deltaTime)

// Rest
void StartResting()
void StopResting()
void InterruptRest()

// Events
UnityEvent<int, int> OnStaminaChanged
UnityEvent OnRestStarted
UnityEvent OnRestStopped
UnityEvent OnStaminaDepleted
UnityEvent<SkillType> OnSkillAutoCancel
```

**Stamina Drain System** (Defensive Skills):
```
Update()
  └─ If defensive skill in Waiting state:
      └─ CheckForAutoCancel()
          ├─ currentStamina < requiredStamina?
          │   └─ Start grace period (default: 0.5s)
          └─ Grace period expired?
              └─ skillSystem.CancelSkill()
```

**Float Accumulator** (for precision):
```csharp
// Lines 14, 115-126: Prevents rounding errors
private float staminaAccumulator;

public void DrainStamina(float drainRate, float deltaTime)
{
    float drainAmount = modifiedDrainRate * deltaTime;
    staminaAccumulator = Mathf.Max(0f, staminaAccumulator - drainAmount);
    currentStamina = Mathf.FloorToInt(staminaAccumulator);
    // Display as int, but track precise float internally
}
```

**Design Rationale**:
- **Auto-Cancel with Grace Period**: Prevents instant cancel if stamina hits 0, allows 0.5s buffer
- **Float Accumulator**: Ensures precise drain calculations (important for 2.5/s rates)
- **Rest Exits Combat**: Design decision to prevent abuse

---

### StatusEffectManager

**File**: `Assets/Scripts/Combat/StatusEffects/StatusEffectManager.cs`

**Purpose**: Manages all status effects (stun, knockdown, rest) with priority and stacking rules.

**Public API**:
```csharp
// Properties
List<StatusEffect> ActiveStatusEffects { get; }
bool IsStunned { get; }
bool IsKnockedDown { get; }
bool IsResting { get; }
bool CanMove { get; }      // !IsStunned && !IsKnockedDown
bool CanAct { get; }       // !IsKnockedDown (can charge while stunned)
StatusEffectType CurrentPrimaryEffect { get; }

// Management
bool HasStatusEffect(StatusEffectType type)
void ApplyStatusEffect(StatusEffect effect)
void RemoveStatusEffect(StatusEffectType type)
void ClearAllStatusEffects()

// Convenience Methods
void ApplyStun(float duration)
void ApplyInteractionKnockdown(Vector3 displacement)
void ApplyMeterKnockdown(Vector3 displacement)
void ApplyRest()

// Events
UnityEvent<StatusEffect> OnStatusEffectApplied
UnityEvent<StatusEffectType> OnStatusEffectRemoved
UnityEvent<StatusEffectType> OnStatusEffectExpired
```

**Status Effect Priority**:
```
Knockdown > Stun > Rest > None

GetPrimaryStatusEffect()
  ├─ IsKnockedDown? → InteractionKnockdown or MeterKnockdown
  ├─ IsStunned? → Stun
  ├─ IsResting? → Rest
  └─ None
```

**Stacking Rules** (Lines 164-194):
```csharp
ProcessStatusEffectStacking(newEffect)
  ├─ Applying Stun?
  │   └─ If IsKnockedDown → Cannot apply (knockdown has priority)
  │
  └─ Applying Knockdown?
      └─ If IsStunned → RemoveStatusEffect(Stun) (knockdown overrides)
```

**Physical Displacement** (Lines 93-97, 301-324):
```csharp
ApplyStatusEffect(effect)
  └─ If effect.hasDisplacement:
      └─ ApplyPhysicalDisplacement(displacementVector)
          └─ characterController.Move(displacementVector)
              └─ Applies knockback with collision detection
```

**Design Rationale**:
- **Priority System**: Clear hierarchy prevents conflicting states
- **Movement Restrictions**: Automatically updates MovementController and SkillSystem
- **Physical Displacement**: Uses CharacterController for proper collision handling

---

### KnockdownMeterTracker

**File**: `Assets/Scripts/Combat/Systems/KnockdownMeterTracker.cs`

**Purpose**: Tracks meter buildup from attacks, triggers meter-based knockdown at 100%.

**Public API**:
```csharp
float CurrentMeter { get; }          // 0.0 to 100.0
float MeterPercentage { get; }       // 0.0 to 1.0

void AddMeterBuildup(int damage, CharacterStats attackerStats, Transform attackerTransform)
void TriggerImmediateKnockdown(Vector3 displacement)  // For Smash/Windmill
void ResetMeter()

UnityEvent<float> OnMeterChanged
UnityEvent<Vector3> OnMeterKnockdownTriggered
```

**Meter Buildup**:
```
AddMeterBuildup(damage, attackerStats, attackerTransform)
  ├─ baseMeterGain = damage * meterGainMultiplier
  ├─ Modified by attacker strength
  ├─ Modified by defender vitality (resistance)
  ├─ currentMeter += modifiedMeterGain
  └─ If currentMeter >= 100:
      ├─ Calculate displacement (based on attacker position)
      └─ TriggerKnockdown(displacement)
```

**Immediate Knockdown** (Smash/Windmill):
```csharp
// Called by CombatInteractionManager for direct hits
TriggerImmediateKnockdown(Vector3 displacement)
  └─ statusEffectManager.ApplyMeterKnockdown(displacement)
      └─ Bypasses meter system entirely
```

**Design Rationale**:
- **Dual Knockdown System**: Meter-based (gradual) vs Immediate (Smash/Windmill)
- **Stat-Based**: Attacker strength increases gain, defender vitality reduces gain
- **Visual Feedback**: UI can subscribe to OnMeterChanged for bar display

---

## UI System

### HealthBarUI

**File**: `Assets/Scripts/UI/HealthBarUI.cs`

**Purpose**: OnGUI-based health bar display.

**Public Fields**:
```csharp
[SerializeField] private HealthSystem targetHealthSystem
[SerializeField] private Vector2 barPosition = new Vector2(10, 10)
[SerializeField] private Vector2 barSize = new Vector2(200, 20)
[SerializeField] private Color healthColor = Color.green
[SerializeField] private Color emptyColor = Color.red
[SerializeField] private Color backgroundColor = Color.black
[SerializeField] private bool showHealthText = true
```

**OnGUI Rendering**:
```csharp
void OnGUI()
  ├─ Draw background (black border)
  ├─ Draw health fill (lerp green→red based on %)
  └─ Draw text "Health: 120/150"
```

**Design**: Immediate mode GUI, no Canvas overhead.

---

### StaminaBarUI

**File**: `Assets/Scripts/UI/StaminaBarUI.cs`

**Purpose**: OnGUI-based stamina bar display.

**Public Fields**: Same as HealthBarUI but with:
```csharp
[SerializeField] private Color staminaColor = Color.cyan
[SerializeField] private Color restingIndicatorColor = Color.yellow
[SerializeField] private bool showRestingIndicator = true
```

**Resting Indicator**: Shows "RESTING" text when stamina regenerating.

---

### CombatDebugVisualizer

**File**: `Assets/Scripts/Combat/Debug/CombatDebugVisualizer.cs`

**Purpose**: Comprehensive on-screen debug display showing all combat state.

**Display Sections**:
```
┌─────────────────────────────┐
│ CHARACTER INFO              │
│ Health: 120/150 (80%)       │
│ Stamina: 45/100 (45%)       │
│ State: Combat               │
├─────────────────────────────┤
│ SKILL INFO                  │
│ Current: Smash              │
│ State: Charging             │
│ Progress: 75%               │
├─────────────────────────────┤
│ STATUS EFFECTS              │
│ Stun: 0.5s remaining        │
├─────────────────────────────┤
│ EQUIPMENT                   │
│ Armor: Heavy Platemail      │
│ Accessory: Guardian Ring    │
│ Weapon: TestSword           │
├─────────────────────────────┤
│ TEST MODE [if TestRepeaterAI]│
│ Repeating: Attack           │
│ Delay: 1.0s                 │
│ Flags: [∞ Stam] [Maintain]  │
└─────────────────────────────┘
```

**Configuration**:
```csharp
[SerializeField] private bool showCharacterInfo = true
[SerializeField] private bool showSkillInfo = true
[SerializeField] private bool showStatusEffects = true
[SerializeField] private bool showEquipmentInfo = true
[SerializeField] private bool showTestAIInfo = true
[SerializeField] private Vector2 displayOffset = new Vector2(10, 10)
```

---

## Editor Tools

### CompleteCombatSceneSetup

**File**: `Assets/Scripts/Editor/CompleteCombatSceneSetup.cs`

**Purpose**: One-click scene setup for complete combat testing environments.

**Menu Options**:
```
Combat → Complete Scene Setup → Testing Sandbox
Combat → Complete Scene Setup → Quick 1v1 Setup
Combat → Complete Scene Setup → Clear All Combat Objects
```

**Testing Sandbox Creates**:
```
Managers:
  ├─ CombatInteractionManager (singleton)
  └─ GameManager

Environment:
  ├─ Ground (30x30 plane with collider)
  ├─ Main Camera (position: 0, 10, -8, rotation: 50° down)
  └─ Directional Light

Player Character (position: -3, 0, 0):
  ├─ Blue capsule visual
  ├─ CombatController + all subsystems
  ├─ HealthBarUI (top-left: 10, 10)
  ├─ StaminaBarUI (below health: 10, 50)
  ├─ TestEquipmentSelector ([ ] bracket hotkeys)
  └─ All 4 equipment sets loaded

Enemy Character (position: 3, 0, 0):
  ├─ Red capsule visual
  ├─ CombatController + all subsystems
  ├─ TestRepeaterAI (F1-F6 skill control)
  ├─ HealthBarUI (top-left: 10, 90)
  └─ StaminaBarUI (below health: 10, 130)

Testing UI:
  └─ TestSkillSelector (F1-F6 hotkeys to control enemy)
```

**Implementation Details**:
```csharp
// Lines 199-222: Equipment preset configuration
private static void AddEquipmentManager(GameObject character)
{
    // Load all equipment sets
    var sets = AssetDatabase.FindAssets("t:EquipmentSet", ...);

    // Assign to EquipmentManager
    SetSerializedProperty(equipmentManager, "availableSets", equipmentSetsArray);

    // Configure TestEquipmentSelector with same sets
    SetSerializedProperty(testEquipmentSelector, "equipmentPresets", equipmentSetsArray);
    SetSerializedProperty(testEquipmentSelector, "targetEquipmentManager", equipmentManager);
}
```

**Design Rationale**:
- **Zero Manual Setup**: Complete environment in one click
- **Asset Auto-Loading**: Finds and assigns weapons, stats, equipment automatically
- **SerializedProperty API**: Configures components programmatically

---

## Data Structures

### CharacterStats (ScriptableObject)

**File**: `Assets/Scripts/Data/CharacterData/CharacterStats.cs`

**Fields**:
```csharp
// Core Stats
int strength      // Damage multiplier
int dexterity     // Movement speed, charge time reduction
int physicalDefense  // Damage reduction
int focus         // Status effect resistance, stamina pool
int intelligence  // (Reserved for magic)
int magicalDefense   // (Reserved for magic)
int vitality      // Knockdown meter resistance

// Derived Stats (calculated)
int MaxHealth     // Base: 100, modified by vitality
int MaxStamina    // Base: 100, modified by focus
float MovementSpeed  // Base: 5.0, modified by dexterity
```

**Static Factory**:
```csharp
static CharacterStats CreateDefaultStats()
{
    // Returns baseline stats (Str:10, Dex:8, Int:6, Foc:8, Def:5, VIT:12)
}
```

---

### WeaponData (ScriptableObject)

**File**: `Assets/Scripts/Data/WeaponData/WeaponData.cs`

**Fields**:
```csharp
// Basic Stats
string weaponName
WeaponType weaponType
float range           // Attack distance
int baseDamage
float speed           // 0.6 to 1.5 (affects execution time)
float stunDuration

// Speed Modifiers
float executionSpeedModifier     // -20% to +30% (execution frames)
float speedResolutionModifier    // -30% to +20% (speed resolution)

// Ranged Properties (if isRangedWeapon = true)
string projectileType
Color trailColorStart
Color trailColorEnd
float trailWidth
AudioClip fireSound
```

**Weapon Archetypes**:
```
Sword:   Range:1.5, Damage:10, Speed:1.0 (balanced baseline)
Spear:   Range:2.5, Damage:8,  Speed:0.8 (range advantage)
Dagger:  Range:1.0, Damage:6,  Speed:1.5 (speed advantage)
Mace:    Range:1.2, Damage:15, Speed:0.6 (damage advantage)
Bow:     Range:6.0, Damage:10, Speed:1.0 (ranged baseline)
```

---

### StatusEffect

**File**: `Assets/Scripts/Combat/StatusEffects/StatusEffect.cs`

**Constructor**:
```csharp
StatusEffect(StatusEffectType type, float duration)
StatusEffect(StatusEffectType type, float duration, Vector3 displacement)
```

**Fields**:
```csharp
StatusEffectType type
float duration
float remainingTime
bool isActive
bool hasDisplacement
Vector3 displacementVector  // For knockback
```

**Update Method**:
```csharp
void UpdateEffect(float deltaTime)
  ├─ remainingTime -= deltaTime
  └─ If remainingTime <= 0: isActive = false
```

---

## Event Flow Map

### Skill Execution Event Chain

```
Player presses "1" (Attack)
  │
  ├─ SkillSystem.HandleSkillInput()
  │   └─ ExecuteSkill(Attack)
  │       ├─ StaminaSystem.ConsumeStamina(10)
  │       ├─ StartCoroutine(ExecuteSkillCoroutine)
  │       │   ├─ Startup phase
  │       │   ├─ Active phase
  │       │   │   └─ ProcessSkillExecution()
  │       │   │       └─ CombatInteractionManager.ProcessSkillExecution(this, Attack)
  │       │   │           └─ pendingExecutions.Enqueue(execution)
  │       │   └─ Recovery phase
  │       └─ OnSkillExecuted.Invoke(Attack, true)
  │
  └─ CombatInteractionManager.Update()
      └─ ProcessPendingExecutions()
          ├─ GetValidDefensiveResponses()
          │   └─ Check waitingDefensiveSkills for target's Defense/Counter
          │
          ├─ Found Defense?
          │   └─ ProcessSkillInteraction(Attack, Defense)
          │       ├─ DetermineInteraction() → AttackerStunned
          │       └─ ProcessInteractionEffects()
          │           ├─ attackerStatusEffects.ApplyStun(1.0s)
          │           ├─ defenderStatusEffects.ApplyStun(0.5s)
          │           └─ CompleteDefensiveSkillExecution(defense)
          │               └─ defense.skillSystem.ForceTransitionToRecovery()
          │
          └─ No Defense?
              └─ ExecuteOffensiveSkillDirectly()
                  ├─ Calculate damage
                  ├─ targetHealth.TakeDamage(damage, source)
                  │   └─ OnDamageReceived.Invoke(damage, source)
                  │       └─ [UI updates health bar]
                  └─ targetStatusEffects.ApplyStun(stunDuration)
```

### Equipment Change Event Chain

```
Player presses "]" (Next Equipment Set)
  │
  └─ TestEquipmentSelector.Update()
      └─ If Input.GetKeyDown("]"):
          ├─ currentPresetIndex++
          ├─ targetEquipmentManager.EquipSet(equipmentPresets[index])
          │   └─ EquipmentManager.EquipSet()
          │       ├─ EquipItem(set.armor)
          │       │   └─ RefreshEquipmentBonuses()
          │       │       ├─ Copy baseStats → modifiedStats
          │       │       ├─ Apply armor bonuses
          │       │       ├─ Apply accessory bonuses
          │       │       └─ OnEquipmentRefreshed.Invoke()
          │       │           └─ [HealthSystem/StaminaSystem recalculate max values]
          │       └─ EquipItem(set.accessory)
          └─ Debug.Log("Equipped set: [SetName]")
```

### Death Event Chain

```
HealthSystem.TakeDamage(damage, source)
  ├─ currentHealth -= damage
  ├─ OnDamageReceived.Invoke(damage, source)
  ├─ If currentHealth <= 0:
  │   └─ Die()
  │       ├─ currentHealth = 0
  │       ├─ statusEffectManager.ClearAllStatusEffects()
  │       ├─ combatController.ExitCombat()
  │       │   └─ OnCombatExited.Invoke()
  │       ├─ OnDied.Invoke(transform)
  │       ├─ OnHealthChanged.Invoke(0, maxHealth)
  │       └─ GameManager.Instance?.OnCharacterDied(this)
  │           └─ [Show reset prompt, pause game, etc.]
  └─ staminaSystem.InterruptRest()
```

---

## Common Patterns

### Creating a Combat Character

```csharp
GameObject character = new GameObject("Character");

// Required components
character.AddComponent<CharacterController>();
var combatController = character.AddComponent<CombatController>();
character.AddComponent<HealthSystem>();
character.AddComponent<StaminaSystem>();
character.AddComponent<StatusEffectManager>();
character.AddComponent<SkillSystem>();
character.AddComponent<WeaponController>();
character.AddComponent<MovementController>();
character.AddComponent<KnockdownMeterTracker>();
character.AddComponent<AccuracySystem>();

// Optional components
character.AddComponent<EquipmentManager>();
character.AddComponent<CombatDebugVisualizer>();

// Assign data
combatController.baseStats = playerStats;  // CharacterStats ScriptableObject
weaponController.WeaponData = swordData;   // WeaponData ScriptableObject
```

### Implementing a Custom AI

```csharp
public class CustomAI : PatternedAI
{
    protected override string GetPatternName() => "Custom Pattern";

    protected override IEnumerator ExecutePattern()
    {
        // Phase 1: Approach
        SetPatternPhase("Approaching", 2.0f);
        while (!IsPlayerInRange())
        {
            MoveTowardsPlayer();
            yield return null;
        }
        StopMovement();

        // Phase 2: Charge Attack
        SetPatternPhase("Charging Attack", 1.0f);
        skillSystem.StartCharging(SkillType.Attack);
        yield return WaitForPhaseComplete(1.0f);

        // Phase 3: Execute
        if (IsPlayerInRange())
        {
            SetPatternPhase("Executing Attack", 0.5f);
            skillSystem.ExecuteSkill(SkillType.Attack);
            yield return WaitForPhaseComplete(0.5f);
        }

        // Phase 4: Retreat
        SetPatternPhase("Retreating", 1.5f);
        yield return WaitForPhaseComplete(1.5f);
    }
}
```

### Creating Custom Equipment

```csharp
// Create armor asset
var heavyPlate = ScriptableObject.CreateInstance<EquipmentData>();
heavyPlate.equipmentName = "Heavy Platemail";
heavyPlate.slot = EquipmentSlot.Armor;
heavyPlate.physicalDefenseBonus = 15;
heavyPlate.maxHealthBonus = 30;
heavyPlate.dexterityBonus = -1;  // Speed penalty

// Create accessory asset
var guardianRing = ScriptableObject.CreateInstance<EquipmentData>();
guardianRing.equipmentName = "Guardian Ring";
guardianRing.slot = EquipmentSlot.Accessory;
guardianRing.physicalDefenseBonus = 10;
guardianRing.maxHealthBonus = 20;

// Create equipment set
var tankSet = ScriptableObject.CreateInstance<EquipmentSet>();
tankSet.setName = "Tank Build";
tankSet.armor = heavyPlate;
tankSet.accessory = guardianRing;

// Save assets (in editor)
AssetDatabase.CreateAsset(heavyPlate, "Assets/Data/Equipment/Armor/HeavyPlatemail.asset");
AssetDatabase.CreateAsset(guardianRing, "Assets/Data/Equipment/Accessories/GuardianRing.asset");
AssetDatabase.CreateAsset(tankSet, "Assets/Data/Equipment/Sets/TankSet.asset");
```

### Subscribing to Combat Events

```csharp
// In your UI or game logic script
void Start()
{
    var combatController = GetComponent<CombatController>();
    var healthSystem = GetComponent<HealthSystem>();
    var skillSystem = GetComponent<SkillSystem>();

    // Combat state events
    combatController.OnCombatEntered.AddListener(OnCombatStarted);
    combatController.OnCombatExited.AddListener(OnCombatEnded);
    combatController.OnTargetChanged.AddListener(OnTargetChanged);

    // Health events
    healthSystem.OnDamageReceived.AddListener(OnDamageTaken);
    healthSystem.OnDied.AddListener(OnCharacterDied);
    healthSystem.OnHealthChanged.AddListener(UpdateHealthUI);

    // Skill events
    skillSystem.OnSkillCharged.AddListener(OnSkillReady);
    skillSystem.OnSkillExecuted.AddListener(OnSkillUsed);
    skillSystem.OnSkillCancelled.AddListener(OnSkillCancelled);
}

void OnDamageTaken(int damage, Transform source)
{
    // Spawn damage numbers, screen shake, etc.
}

void UpdateHealthUI(int current, int max)
{
    healthBar.fillAmount = (float)current / max;
}
```

---

## Troubleshooting

### Common Issues

**Issue**: Character falls through ground
- **Cause**: Missing collider or CharacterController
- **Fix**: Ensure ground has MeshCollider, character has CharacterController

**Issue**: Skills not executing
- **Cause**: Not in combat state or insufficient stamina
- **Fix**: Check `CombatController.IsInCombat`, ensure target set, check stamina

**Issue**: Equipment changes not applying
- **Cause**: Trying to change during combat
- **Fix**: Exit combat first (press Esc), then change equipment

**Issue**: RangedAttack always misses
- **Cause**: Not aiming long enough or target moving
- **Fix**: Hold aim longer for higher accuracy, stationary targets build accuracy faster

**Issue**: Defensive skills auto-cancel immediately
- **Cause**: Stamina too low, drains immediately
- **Fix**: Ensure stamina > skill cost + drain rate × wait duration

**Issue**: AI not engaging
- **Cause**: Player outside engage distance
- **Fix**: Check `PatternedAI.engageDistance` (default: 3.0), move closer

**Issue**: Health/Stamina bars not showing
- **Cause**: Missing HealthBarUI/StaminaBarUI components or unassigned reference
- **Fix**: Ensure components attached to character, `targetHealthSystem`/`targetStaminaSystem` assigned

**Issue**: F1-F6 hotkeys not working
- **Cause**: Missing TestSkillSelector in scene
- **Fix**: Ensure TestingUI_Manager GameObject exists with TestSkillSelector component

**Issue**: Equipment bracket keys `[` `]` not working
- **Cause**: Missing TestEquipmentSelector or in combat
- **Fix**: Ensure component exists, exit combat first

### Debug Tools

**Enable Debug Logs**:
```csharp
// In Inspector for each component:
combatController.enableDebugLogs = true;
skillSystem.enableDebugLogs = true;
healthSystem.enableDebugLogs = true;
// etc.
```

**Use CombatDebugVisualizer**:
- Attach to character
- Shows all combat state in real-time
- Toggle sections in Inspector

**Check Console Logs**:
```
[CombatInteractionManager] Processing interaction: Player Attack vs Enemy Defense = AttackerStunned
[SkillSystem] Player executing Attack
[HealthSystem] Enemy took 15 damage from Player (85/100)
[StatusEffectManager] Player received status effect: Stun for 1.0s
```

**Gizmo Visualization**:
- Select character in Scene view
- **CombatController**: Yellow line to target, cyan sphere (detection range)
- **PatternedAI**: Green sphere (engage distance), red sphere (disengage)
- **WeaponController**: Red sphere (weapon range)

---

## Performance Notes

- **Scene Complexity**: System supports 5+ simultaneous combatants at 60+ FPS
- **OnGUI Overhead**: HealthBarUI/StaminaBarUI use immediate mode (lightweight)
- **Debug Visualizers**: Can be disabled per-character if needed
- **Event System**: UnityEvents have minimal overhead, use liberally

---

## Version History

**v1.0** (2025-10-25)
- Initial component reference documentation
- Covers all core systems, AI, equipment, skills, stats, UI, and editor tools
- Includes execution flows, design rationale, and usage examples

---

**Questions or Issues?**

Check:
1. Unity Console for error messages
2. Component Inspector fields (ensure all required references assigned)
3. This guide's Troubleshooting section
4. Related documentation: `minimal.md`, `SCENE_SETUP_GUIDE.md`, `SKILL_TEST_ENVIRONMENT_USAGE.md`

**Next Steps**: See `SCENE_SETUP_QUICKSTART.md` for getting started with testing.
