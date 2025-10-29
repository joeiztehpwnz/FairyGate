# FairyGate Combat System - Comprehensive Refactoring Plan

**Version**: 1.0
**Date**: 2025-10-27
**Current LOC**: ~7,070 lines across 33 files

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Refactoring Goals](#refactoring-goals)
3. [Phase 1: Extract Skill Handlers](#phase-1-extract-skill-handlers)
4. [Phase 2: Combat Resolution Strategies](#phase-2-combat-resolution-strategies)
5. [Phase 3: Centralized Input System](#phase-3-centralized-input-system)
6. [Phase 4: Configuration ScriptableObjects](#phase-4-configuration-scriptableobjects)
7. [Phase 5: State Machine Pattern](#phase-5-state-machine-pattern)
8. [Phase 6: UI Base Classes](#phase-6-ui-base-classes)
9. [Phase 7: Unit Testing Framework](#phase-7-unit-testing-framework)
10. [Implementation Order](#implementation-order)
11. [Migration Guides](#migration-guides)
12. [Testing Strategy](#testing-strategy)

---

## Current State Analysis

### File Count & LOC Distribution

```
Total Files: 33
Total LOC: ~7,070

Largest Files:
1. SkillSystem.cs                    770 lines
2. CombatInteractionManager.cs       753 lines
3. CompleteCombatSceneSetup.cs       610 lines (Editor only)
4. SimpleTestAI.cs                   495 lines
5. CombatController.cs               432 lines
6. StatusEffectManager.cs            349 lines
7. MovementController.cs             309 lines
8. StaminaSystem.cs                  260 lines
```

### MonoBehaviour Components (18)

**Core Systems**:
- CombatController
- CombatInteractionManager
- CombatUpdateManager
- GameManager
- MovementController

**Combat Systems**:
- SkillSystem
- HealthSystem
- StaminaSystem
- StatusEffectManager
- AccuracySystem
- KnockdownMeterTracker
- WeaponController

**Other**:
- EquipmentManager
- AICoordinator
- SimpleTestAI
- CameraController
- HealthBarUI
- StaminaBarUI

### Current Architecture

```
┌─────────────────────────────────────────────────────┐
│             CombatUpdateManager                     │
│         (Centralized Update Loop)                   │
└──────────────┬──────────────────────────────────────┘
               │ Calls CombatUpdate()
               │
      ┌────────┴────────┐
      │                 │
┌─────▼────────┐  ┌─────▼─────────┐
│CombatControl │  │MovementControl│
│     ler      │  │      ler      │
└──────┬───────┘  └───────────────┘
       │
  ┌────┴────┐
  │         │
┌─▼───┐  ┌──▼───┐  ┌────────┐
│Skill│  │Health│  │Stamina │
│Sys. │  │Sys.  │  │Sys.    │
└─────┘  └──────┘  └────────┘
```

### Pain Points

1. **SkillSystem** (770 lines):
   - Monolithic class handling all skill types
   - Hard to extend with new skills
   - Complex state management
   - Input handling tightly coupled

2. **CombatInteractionManager** (753 lines):
   - Complex nested resolution logic
   - Hard to understand skill interactions
   - Difficult to add new interaction rules

3. **Input Scattered**:
   - SkillSystem handles skill input
   - MovementController handles WASD
   - CameraController handles arrows
   - No unified input abstraction

4. **Configuration Hardcoded**:
   - Magic numbers throughout
   - Can't tune without code changes
   - No designer-friendly workflow

5. **No Unit Tests**:
   - Can't refactor with confidence
   - Regressions hard to catch
   - Manual testing only

---

## Refactoring Goals

### Primary Objectives

1. **Modularity**: Break monolithic classes into focused, single-responsibility components
2. **Extensibility**: Make it easy to add new skills, enemies, interactions
3. **Maintainability**: Reduce cognitive load, improve code readability
4. **Testability**: Enable unit testing of core logic
5. **Designer-Friendly**: Move configuration to ScriptableObjects

### Success Metrics

- **SkillSystem**: 770 → ~400 lines (extraction of handlers)
- **CombatInteractionManager**: 753 → ~400 lines (strategy pattern)
- **Test Coverage**: 0% → 60%+ for core systems
- **Time to Add New Skill**: Days → Hours

---

## Phase 1: Extract Skill Handlers

**Complexity**: Medium
**Estimated Effort**: 8-12 hours
**Impact**: High (enables all skill system improvements)
**Dependencies**: None
**Risk**: Medium (requires careful extraction)

### Goal

Split the monolithic SkillSystem (770 lines) into modular skill handlers using the Strategy pattern. Each skill type gets its own handler class responsible for charging, execution, and cancellation logic.

### Architecture

#### Current (Monolithic)

```
┌──────────────────────────────────────┐
│         SkillSystem                  │
│  (770 lines, all skills in one)     │
│                                      │
│  - HandleAttack()                    │
│  - HandleDefense()                   │
│  - HandleCounter()                   │
│  - HandleSmash()                     │
│  - HandleWindmill()                  │
│  - HandleRangedAttack()              │
│  - 20+ other methods                 │
└──────────────────────────────────────┘
```

#### Proposed (Strategy Pattern)

```
┌──────────────────────────────────────┐
│         SkillSystem                  │
│  (~300 lines, orchestration only)   │
│                                      │
│  - Dictionary<SkillType, ISkillHandler>
│  - StartCharging(type)               │
│  - ExecuteSkill(type)                │
│  - CancelSkill()                     │
└────────────┬─────────────────────────┘
             │ delegates to
    ┌────────┴────────┐
    │  ISkillHandler  │
    │  Interface      │
    └────────┬────────┘
             │
      ┌──────┴──────┬─────────┬─────────┬──────────┬─────────┐
      │             │         │         │          │         │
┌─────▼────┐  ┌─────▼───┐ ┌──▼───┐ ┌───▼────┐ ┌───▼────┐ ┌──▼────┐
│  Attack  │  │Defense  │ │Counter│ │Smash   │ │Windmill│ │Ranged │
│  Handler │  │Handler  │ │Handler│ │Handler │ │Handler │ │Handler│
└──────────┘  └─────────┘ └───────┘ └────────┘ └────────┘ └───────┘
   (~80 LOC)    (~60 LOC)  (~100 LOC) (~120 LOC) (~150 LOC) (~80 LOC)
```

### Files to Create

#### 1. ISkillHandler Interface

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/ISkillHandler.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Interface for skill-specific execution logic.
    /// Each skill type implements this to handle its unique behavior.
    /// </summary>
    public interface ISkillHandler
    {
        /// <summary>
        /// The skill type this handler manages
        /// </summary>
        SkillType SkillType { get; }

        /// <summary>
        /// Check if the skill can be charged right now
        /// </summary>
        bool CanCharge(SkillContext context);

        /// <summary>
        /// Check if the charged skill can be executed
        /// </summary>
        bool CanExecute(SkillContext context);

        /// <summary>
        /// Start charging this skill
        /// </summary>
        void StartCharging(SkillContext context);

        /// <summary>
        /// Execute the charged skill
        /// Returns true if execution started successfully
        /// </summary>
        bool Execute(SkillContext context);

        /// <summary>
        /// Cancel the skill (if chargeable)
        /// </summary>
        void Cancel(SkillContext context);

        /// <summary>
        /// Called each frame while skill is charging
        /// Returns true when fully charged
        /// </summary>
        bool UpdateCharging(SkillContext context, float deltaTime);

        /// <summary>
        /// Get the stamina cost for this skill
        /// </summary>
        int GetStaminaCost(SkillContext context);
    }
}
```

#### 2. SkillContext Data Class

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/SkillContext.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Encapsulates all data needed for skill execution.
    /// Passed to skill handlers to avoid tight coupling.
    /// </summary>
    public class SkillContext
    {
        // Component references
        public SkillSystem SkillSystem { get; set; }
        public CombatController CombatController { get; set; }
        public WeaponController WeaponController { get; set; }
        public StaminaSystem StaminaSystem { get; set; }
        public MovementController MovementController { get; set; }
        public StatusEffectManager StatusEffectManager { get; set; }
        public AccuracySystem AccuracySystem { get; set; }
        public KnockdownMeterTracker KnockdownMeter { get; set; }

        // State
        public SkillExecutionState CurrentState { get; set; }
        public float ChargeProgress { get; set; }
        public float ChargeStartTime { get; set; }
        public Transform CurrentTarget { get; set; }

        // Configuration
        public CharacterStats Stats { get; set; }
        public WeaponData Weapon { get; set; }

        // Utility
        public bool EnableDebugLogs { get; set; }

        public SkillContext(SkillSystem skillSystem)
        {
            SkillSystem = skillSystem;
            CombatController = skillSystem.GetComponent<CombatController>();
            WeaponController = skillSystem.GetComponent<WeaponController>();
            StaminaSystem = skillSystem.GetComponent<StaminaSystem>();
            MovementController = skillSystem.GetComponent<MovementController>();
            StatusEffectManager = skillSystem.GetComponent<StatusEffectManager>();
            AccuracySystem = skillSystem.GetComponent<AccuracySystem>();
            KnockdownMeter = skillSystem.GetComponent<KnockdownMeterTracker>();

            Stats = CombatController.Stats;
            Weapon = WeaponController.WeaponData;
        }
    }
}
```

#### 3. AttackSkillHandler

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/AttackSkillHandler.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles Attack skill execution.
    /// Basic melee attack with charge-to-execute pattern.
    /// </summary>
    public class AttackSkillHandler : ISkillHandler
    {
        public SkillType SkillType => SkillType.Attack;

        public bool CanCharge(SkillContext context)
        {
            // Can only charge if uncharged
            if (context.CurrentState != SkillExecutionState.Uncharged)
                return false;

            // Must have stamina
            if (!context.StaminaSystem.CanConsumeStamina(GetStaminaCost(context)))
                return false;

            // Can't charge while stunned, knocked down, or resting
            if (context.StatusEffectManager.IsStunned ||
                context.StatusEffectManager.IsKnockedDown ||
                context.StatusEffectManager.IsResting)
                return false;

            return true;
        }

        public bool CanExecute(SkillContext context)
        {
            // Must be charged
            if (context.CurrentState != SkillExecutionState.Charged)
                return false;

            // Must have valid target
            Transform target = context.CombatController.CurrentTarget;
            if (target == null)
                return false;

            // Must be in range
            if (!context.WeaponController.IsInRange(target))
                return false;

            return true;
        }

        public void StartCharging(SkillContext context)
        {
            context.CurrentState = SkillExecutionState.Charging;
            context.ChargeProgress = 0f;
            context.ChargeStartTime = Time.time;

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} started charging Attack");
            }
        }

        public bool UpdateCharging(SkillContext context, float deltaTime)
        {
            float chargeTime = context.Weapon.GetChargeTime(SkillType.Attack);
            float elapsed = Time.time - context.ChargeStartTime;
            context.ChargeProgress = Mathf.Clamp01(elapsed / chargeTime);

            if (context.ChargeProgress >= 1f)
            {
                context.CurrentState = SkillExecutionState.Charged;
                return true; // Fully charged
            }

            return false;
        }

        public bool Execute(SkillContext context)
        {
            if (!CanExecute(context))
                return false;

            // Consume stamina
            if (!context.StaminaSystem.ConsumeStamina(GetStaminaCost(context)))
                return false;

            // Send to interaction manager
            CombatInteractionManager.Instance.ProcessSkillExecution(
                context.SkillSystem,
                SkillType.Attack
            );

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} executed Attack");
            }

            return true;
        }

        public void Cancel(SkillContext context)
        {
            if (context.CurrentState == SkillExecutionState.Charging ||
                context.CurrentState == SkillExecutionState.Charged)
            {
                context.CurrentState = SkillExecutionState.Uncharged;
                context.ChargeProgress = 0f;

                if (context.EnableDebugLogs)
                {
                    Debug.Log($"{context.CombatController.name} cancelled Attack");
                }
            }
        }

        public int GetStaminaCost(SkillContext context)
        {
            return CombatConstants.ATTACK_STAMINA_COST;
        }
    }
}
```

#### 4. DefenseSkillHandler

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/DefenseSkillHandler.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles Defense skill execution.
    /// Charge to activate, stays active until cancelled or hit.
    /// </summary>
    public class DefenseSkillHandler : ISkillHandler
    {
        public SkillType SkillType => SkillType.Defense;

        public bool CanCharge(SkillContext context)
        {
            if (context.CurrentState != SkillExecutionState.Uncharged)
                return false;

            if (!context.StaminaSystem.CanConsumeStamina(GetStaminaCost(context)))
                return false;

            if (context.StatusEffectManager.IsStunned ||
                context.StatusEffectManager.IsKnockedDown ||
                context.StatusEffectManager.IsResting)
                return false;

            return true;
        }

        public bool CanExecute(SkillContext context)
        {
            // Defense auto-executes when charged (no target required)
            return context.CurrentState == SkillExecutionState.Charged;
        }

        public void StartCharging(SkillContext context)
        {
            context.CurrentState = SkillExecutionState.Charging;
            context.ChargeProgress = 0f;
            context.ChargeStartTime = Time.time;

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} started charging Defense");
            }
        }

        public bool UpdateCharging(SkillContext context, float deltaTime)
        {
            float chargeTime = context.Weapon.GetChargeTime(SkillType.Defense);
            float elapsed = Time.time - context.ChargeStartTime;
            context.ChargeProgress = Mathf.Clamp01(elapsed / chargeTime);

            if (context.ChargeProgress >= 1f)
            {
                // Auto-execute defense when charged
                Execute(context);
                return true;
            }

            return false;
        }

        public bool Execute(SkillContext context)
        {
            if (!CanExecute(context))
                return false;

            if (!context.StaminaSystem.ConsumeStamina(GetStaminaCost(context)))
                return false;

            // Defense enters waiting state
            context.CurrentState = SkillExecutionState.Waiting;

            // Apply movement speed reduction
            context.MovementController.ApplySkillMovementRestriction(
                SkillType.Defense,
                SkillExecutionState.Waiting
            );

            // Send to interaction manager
            CombatInteractionManager.Instance.ProcessSkillExecution(
                context.SkillSystem,
                SkillType.Defense
            );

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} entered Defense stance");
            }

            return true;
        }

        public void Cancel(SkillContext context)
        {
            if (context.CurrentState == SkillExecutionState.Charging ||
                context.CurrentState == SkillExecutionState.Charged ||
                context.CurrentState == SkillExecutionState.Waiting)
            {
                context.CurrentState = SkillExecutionState.Uncharged;
                context.ChargeProgress = 0f;

                // Restore movement
                context.MovementController.ResetMovementSpeed();

                if (context.EnableDebugLogs)
                {
                    Debug.Log($"{context.CombatController.name} cancelled Defense");
                }
            }
        }

        public int GetStaminaCost(SkillContext context)
        {
            return CombatConstants.DEFENSE_STAMINA_COST;
        }
    }
}
```

#### 5. CounterSkillHandler

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/CounterSkillHandler.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Handles Counter skill execution.
    /// Charge to activate, waits for incoming attack to counter.
    /// </summary>
    public class CounterSkillHandler : ISkillHandler
    {
        public SkillType SkillType => SkillType.Counter;

        public bool CanCharge(SkillContext context)
        {
            if (context.CurrentState != SkillExecutionState.Uncharged)
                return false;

            if (!context.StaminaSystem.CanConsumeStamina(GetStaminaCost(context)))
                return false;

            if (context.StatusEffectManager.IsStunned ||
                context.StatusEffectManager.IsKnockedDown ||
                context.StatusEffectManager.IsResting)
                return false;

            return true;
        }

        public bool CanExecute(SkillContext context)
        {
            return context.CurrentState == SkillExecutionState.Charged;
        }

        public void StartCharging(SkillContext context)
        {
            context.CurrentState = SkillExecutionState.Charging;
            context.ChargeProgress = 0f;
            context.ChargeStartTime = Time.time;

            // Apply movement speed reduction while charging
            context.MovementController.ApplySkillMovementRestriction(
                SkillType.Counter,
                SkillExecutionState.Charging
            );

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} started charging Counter");
            }
        }

        public bool UpdateCharging(SkillContext context, float deltaTime)
        {
            float chargeTime = context.Weapon.GetChargeTime(SkillType.Counter);
            float elapsed = Time.time - context.ChargeStartTime;
            context.ChargeProgress = Mathf.Clamp01(elapsed / chargeTime);

            if (context.ChargeProgress >= 1f)
            {
                // Auto-execute counter when charged
                Execute(context);
                return true;
            }

            return false;
        }

        public bool Execute(SkillContext context)
        {
            if (!CanExecute(context))
                return false;

            if (!context.StaminaSystem.ConsumeStamina(GetStaminaCost(context)))
                return false;

            // Counter enters waiting state (immobilized)
            context.CurrentState = SkillExecutionState.Waiting;

            // Immobilize while waiting
            context.MovementController.ApplySkillMovementRestriction(
                SkillType.Counter,
                SkillExecutionState.Waiting
            );

            // Send to interaction manager
            CombatInteractionManager.Instance.ProcessSkillExecution(
                context.SkillSystem,
                SkillType.Counter
            );

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} entered Counter stance");
            }

            return true;
        }

        public void Cancel(SkillContext context)
        {
            if (context.CurrentState == SkillExecutionState.Charging ||
                context.CurrentState == SkillExecutionState.Charged ||
                context.CurrentState == SkillExecutionState.Waiting)
            {
                context.CurrentState = SkillExecutionState.Uncharged;
                context.ChargeProgress = 0f;

                // Restore movement
                context.MovementController.ResetMovementSpeed();

                if (context.EnableDebugLogs)
                {
                    Debug.Log($"{context.CombatController.name} cancelled Counter");
                }
            }
        }

        public int GetStaminaCost(SkillContext context)
        {
            return CombatConstants.COUNTER_STAMINA_COST;
        }
    }
}
```

#### 6. SmashSkillHandler, WindmillSkillHandler, RangedAttackSkillHandler

**Paths**:
- `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/SmashSkillHandler.cs`
- `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/WindmillSkillHandler.cs`
- `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/RangedAttackSkillHandler.cs`

Follow similar patterns to Attack/Defense/Counter handlers. Each implements ISkillHandler with skill-specific logic.

### Files to Modify

#### Modified SkillSystem.cs

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Changes**:
1. Remove all skill-specific execution methods
2. Add handler dictionary: `Dictionary<SkillType, ISkillHandler> skillHandlers`
3. Initialize handlers in Awake()
4. Delegate all operations to appropriate handler

**Before** (770 lines, monolithic):
```csharp
public class SkillSystem : MonoBehaviour
{
    // 100+ lines of fields and properties

    public bool CanChargeSkill(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Attack:
                // 10 lines of attack-specific logic
            case SkillType.Defense:
                // 10 lines of defense-specific logic
            // ... 50+ lines total
        }
    }

    public void StartCharging(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Attack:
                // Attack charging logic
            // ... 100+ lines total
        }
    }

    // 20+ more methods with similar switch statements
}
```

**After** (~300 lines, delegating):
```csharp
public class SkillSystem : MonoBehaviour
{
    // Core fields
    private Dictionary<SkillType, ISkillHandler> skillHandlers;
    private SkillContext context;
    private SkillType currentSkill;
    private SkillExecutionState currentState;

    private void Awake()
    {
        // Initialize context
        context = new SkillContext(this);

        // Register skill handlers
        skillHandlers = new Dictionary<SkillType, ISkillHandler>
        {
            { SkillType.Attack, new AttackSkillHandler() },
            { SkillType.Defense, new DefenseSkillHandler() },
            { SkillType.Counter, new CounterSkillHandler() },
            { SkillType.Smash, new SmashSkillHandler() },
            { SkillType.Windmill, new WindmillSkillHandler() },
            { SkillType.RangedAttack, new RangedAttackSkillHandler() }
        };

        // Get component references (existing code)
        // ...
    }

    public bool CanChargeSkill(SkillType skillType)
    {
        if (!skillHandlers.TryGetValue(skillType, out var handler))
            return false;

        return handler.CanCharge(context);
    }

    public void StartCharging(SkillType skillType)
    {
        if (!skillHandlers.TryGetValue(skillType, out var handler))
            return;

        if (!handler.CanCharge(context))
            return;

        currentSkill = skillType;
        handler.StartCharging(context);
    }

    public void CombatUpdate(float deltaTime)
    {
        // Update charging
        if (currentState == SkillExecutionState.Charging)
        {
            if (skillHandlers.TryGetValue(currentSkill, out var handler))
            {
                handler.UpdateCharging(context, deltaTime);
            }
        }

        // Handle input (existing code, calls handlers)
        HandleSkillInput();
    }

    // All other methods delegate to handlers
}
```

### Implementation Steps

#### Step 1: Create Handler Infrastructure (2 hours)

1. Create directory: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/Handlers/`
2. Create `ISkillHandler.cs` interface
3. Create `SkillContext.cs` data class
4. Test compilation

#### Step 2: Extract Attack Handler (1 hour)

1. Create `AttackSkillHandler.cs`
2. Copy Attack-specific logic from SkillSystem
3. Implement ISkillHandler interface
4. Test in isolation (if possible)

#### Step 3: Extract Defense & Counter Handlers (2 hours)

1. Create `DefenseSkillHandler.cs`
2. Create `CounterSkillHandler.cs`
3. Copy and adapt logic from SkillSystem
4. Test compilation

#### Step 4: Extract Smash, Windmill, Ranged Handlers (3 hours)

1. Create `SmashSkillHandler.cs`
2. Create `WindmillSkillHandler.cs`
3. Create `RangedAttackSkillHandler.cs`
4. These are more complex (Windmill has multi-hit, Ranged has aiming)

#### Step 5: Refactor SkillSystem (2 hours)

1. Add handler dictionary to SkillSystem
2. Initialize handlers in Awake()
3. Replace all switch statements with handler delegation
4. Remove old skill-specific methods
5. Keep orchestration logic (input handling, state tracking)

#### Step 6: Testing & Validation (2 hours)

1. Test all skills in-game
2. Verify AI still works with handlers
3. Check CombatInteractionManager integration
4. Verify debug logs and UI
5. Performance testing (should be same or better)

### Migration Guide

#### For Existing Scenes

**No migration needed** - SkillSystem API remains the same:
- `CanChargeSkill(type)` still works
- `StartCharging(type)` still works
- `ExecuteSkill(type)` still works
- `CancelSkill()` still works

The refactoring is internal to SkillSystem. External code sees no changes.

#### For Existing Prefabs

No changes needed. All public interfaces remain identical.

#### For AI Scripts

If AI scripts call `SkillSystem` methods, no changes needed. The public API is preserved.

#### For New Features

**Adding a new skill**:

Before (modify SkillSystem.cs directly):
1. Add case to every switch statement (20+ places)
2. Add skill-specific fields
3. Add skill-specific methods
4. Risk breaking existing skills

After (create new handler):
1. Create `NewSkillHandler.cs` implementing `ISkillHandler`
2. Register in `SkillSystem.Awake()`: `{ SkillType.NewSkill, new NewSkillHandler() }`
3. Done - existing skills untouched

### Testing Checklist

- [ ] All 6 skills charge correctly
- [ ] All 6 skills execute correctly
- [ ] Skill cancellation works
- [ ] AI can use all skills
- [ ] Player input works for all skills
- [ ] CombatInteractionManager receives skill executions
- [ ] Defense/Counter enter waiting state
- [ ] Windmill multi-hit works
- [ ] Ranged aiming works
- [ ] Movement restrictions apply correctly
- [ ] Stamina consumption correct
- [ ] Debug logs still show correct info
- [ ] Performance unchanged or improved

### Benefits

✅ **SkillSystem reduced from 770 → ~300 lines**
✅ **Each skill isolated in ~80-150 line handler**
✅ **Easy to add new skills (create handler, register)**
✅ **Easy to modify skill behavior (edit one handler)**
✅ **Testable** (can test handlers in isolation)
✅ **No changes to existing code outside SkillSystem**

---

## Phase 2: Combat Resolution Strategies

**Complexity**: High
**Estimated Effort**: 12-16 hours
**Impact**: High (simplifies complex interaction logic)
**Dependencies**: None (can be done before/after Phase 1)
**Risk**: High (combat resolution is critical system)

### Goal

Refactor CombatInteractionManager (753 lines) to use Strategy pattern for different resolution scenarios. Split complex nested logic into focused resolver classes.

### Architecture

#### Current (Monolithic)

```
┌─────────────────────────────────────────────┐
│     CombatInteractionManager                │
│     (753 lines, all logic in one)           │
│                                             │
│  - ProcessPendingExecutions()               │
│    - ProcessSingleOffensiveSkill()          │
│    - ProcessMultipleOffensiveSkills()       │
│      - PerformSpeedResolution()             │
│      - GroupOffensiveSkillsByCombatant()    │
│      - ProcessSimultaneousGroup()           │
│    - GetValidDefensiveResponses()           │
│    - ResolveOffensiveVsDefensive()          │
│    - HandleDefenseInteraction()             │
│    - HandleCounterInteraction()             │
│    - HandleSmashBreakCounter()              │
│    - ApplyDefenseReduction()                │
│    - ExecuteFinalAttack()                   │
│    - ... 20+ more methods                   │
└─────────────────────────────────────────────┘
```

#### Proposed (Strategy Pattern)

```
┌─────────────────────────────────────────────┐
│     CombatInteractionManager                │
│     (~300 lines, orchestration only)        │
│                                             │
│  - ProcessSkillExecution()                  │
│  - ProcessPendingExecutions()               │
│  - Delegate to resolvers                    │
└──────────────┬──────────────────────────────┘
               │ delegates to
    ┌──────────┴──────────┐
    │ IInteractionResolver│
    │     Interface       │
    └──────────┬──────────┘
               │
     ┌─────────┴─────────┬──────────────┬────────────────┐
     │                   │              │                │
┌────▼─────┐  ┌──────────▼───┐  ┌──────▼────┐  ┌───────▼────┐
│  Speed   │  │  Defensive   │  │Single     │  │ Simultaneous│
│ Conflict │  │  Response    │  │Offensive  │  │   Attack    │
│ Resolver │  │  Resolver    │  │Resolver   │  │  Resolver   │
└──────────┘  └──────────────┘  └───────────┘  └────────────┘
  (~150 LOC)     (~200 LOC)       (~150 LOC)      (~200 LOC)
```

### Files to Create

#### 1. IInteractionResolver Interface

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/IInteractionResolver.cs`

```csharp
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Interface for combat interaction resolution strategies.
    /// Each resolver handles a specific scenario (speed conflicts, defensive responses, etc).
    /// </summary>
    public interface IInteractionResolver
    {
        /// <summary>
        /// Check if this resolver can handle the given scenario
        /// </summary>
        bool CanResolve(InteractionContext context);

        /// <summary>
        /// Resolve the interaction and apply effects
        /// </summary>
        void Resolve(InteractionContext context);
    }
}
```

#### 2. InteractionContext Data Class

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/InteractionContext.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Encapsulates all data for a combat interaction.
    /// Passed to resolvers to determine and execute the outcome.
    /// </summary>
    public class InteractionContext
    {
        // Offensive skills
        public List<SkillExecution> OffensiveSkills { get; set; }

        // Defensive skills
        public List<SkillExecution> DefensiveSkills { get; set; }

        // Resolution results
        public List<SpeedResolutionGroupResult> SpeedResolutionResults { get; set; }

        // Configuration
        public bool EnableDebugLogs { get; set; }

        // Helper methods
        public bool HasMultipleOffensiveSkills => OffensiveSkills != null && OffensiveSkills.Count > 1;
        public bool HasDefensiveSkills => DefensiveSkills != null && DefensiveSkills.Count > 0;
        public bool IsSingleOffensive => OffensiveSkills != null && OffensiveSkills.Count == 1;

        public InteractionContext()
        {
            OffensiveSkills = new List<SkillExecution>();
            DefensiveSkills = new List<SkillExecution>();
            SpeedResolutionResults = new List<SpeedResolutionGroupResult>();
        }
    }
}
```

#### 3. SpeedConflictResolver

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/Resolvers/SpeedConflictResolver.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Resolves speed conflicts when multiple offensive skills execute simultaneously.
    /// Determines execution order and handles skill cancellation.
    /// </summary>
    public class SpeedConflictResolver : IInteractionResolver
    {
        public bool CanResolve(InteractionContext context)
        {
            // Only resolve if multiple offensive skills
            return context.HasMultipleOffensiveSkills;
        }

        public void Resolve(InteractionContext context)
        {
            if (context.EnableDebugLogs)
            {
                Debug.Log($"[SpeedConflictResolver] Resolving {context.OffensiveSkills.Count} simultaneous offensive skills");
            }

            // Group by combatant
            var groups = GroupByCombatant(context.OffensiveSkills);

            // Perform speed resolution between groups
            var resolutionResults = PerformSpeedResolution(groups, context.EnableDebugLogs);

            // Store results in context for downstream resolvers
            context.SpeedResolutionResults = resolutionResults;

            // Process each group based on resolution
            foreach (var result in resolutionResults)
            {
                if (result.winnerGroup != null)
                {
                    // Winner executes
                    foreach (var skill in result.winnerGroup)
                    {
                        ProcessSkillExecution(skill, context);
                    }
                }

                if (result.loserGroup != null)
                {
                    // Loser gets cancelled/stunned
                    foreach (var skill in result.loserGroup)
                    {
                        CancelSkillExecution(skill, context);
                    }
                }
            }
        }

        private List<List<SkillExecution>> GroupByCombatant(List<SkillExecution> skills)
        {
            // Group skills by which combatant executed them
            var grouped = skills.GroupBy(s => s.combatant).Select(g => g.ToList()).ToList();
            return grouped;
        }

        private List<SpeedResolutionGroupResult> PerformSpeedResolution(
            List<List<SkillExecution>> groups,
            bool enableDebugLogs)
        {
            var results = new List<SpeedResolutionGroupResult>();

            // For each pair of groups, resolve speed conflict
            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = i + 1; j < groups.Count; j++)
                {
                    var group1 = groups[i];
                    var group2 = groups[j];

                    // Get representative skills
                    var skill1 = group1[0];
                    var skill2 = group2[0];

                    // Resolve speed
                    var speedResult = SpeedResolver.ResolveSpeedConflict(
                        skill1.combatant.Transform,
                        skill2.combatant.Transform,
                        skill1.skillType,
                        skill2.skillType
                    );

                    // Create group result
                    var groupResult = new SpeedResolutionGroupResult
                    {
                        winnerGroup = speedResult.resolution == SpeedResolution.Player1Wins ? group1 : group2,
                        loserGroup = speedResult.resolution == SpeedResolution.Player1Wins ? group2 : group1,
                        speedResult = speedResult
                    };

                    results.Add(groupResult);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[Speed Resolution] {speedResult.winner?.name} wins vs {speedResult.loser?.name}");
                    }
                }
            }

            return results;
        }

        private void ProcessSkillExecution(SkillExecution skill, InteractionContext context)
        {
            // Delegate to single offensive resolver
            var singleResolver = new SingleOffensiveResolver();
            var singleContext = new InteractionContext
            {
                OffensiveSkills = new List<SkillExecution> { skill },
                DefensiveSkills = context.DefensiveSkills,
                EnableDebugLogs = context.EnableDebugLogs
            };
            singleResolver.Resolve(singleContext);
        }

        private void CancelSkillExecution(SkillExecution skill, InteractionContext context)
        {
            // Apply stun to loser
            skill.combatant.ApplyStatusEffect(new StatusEffect
            {
                type = StatusEffectType.Stunned,
                duration = CombatConstants.STUN_DURATION
            });

            // Reset skill state
            skill.skillSystem.ForceResetState();

            if (context.EnableDebugLogs)
            {
                Debug.Log($"[Speed Loss] {skill.combatant.name}'s {skill.skillType} was cancelled");
            }
        }
    }

    /// <summary>
    /// Result of speed resolution between two groups
    /// </summary>
    public class SpeedResolutionGroupResult
    {
        public List<SkillExecution> winnerGroup;
        public List<SkillExecution> loserGroup;
        public SpeedResolutionResult speedResult;
    }
}
```

#### 4. SingleOffensiveResolver

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/Resolvers/SingleOffensiveResolver.cs`

```csharp
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Resolves a single offensive skill execution.
    /// Checks for defensive responses and delegates to appropriate resolver.
    /// </summary>
    public class SingleOffensiveResolver : IInteractionResolver
    {
        public bool CanResolve(InteractionContext context)
        {
            return context.IsSingleOffensive;
        }

        public void Resolve(InteractionContext context)
        {
            var offensiveSkill = context.OffensiveSkills[0];

            if (context.EnableDebugLogs)
            {
                Debug.Log($"[SingleOffensiveResolver] Processing {offensiveSkill.combatant.name}'s {offensiveSkill.skillType}");
            }

            // Find defensive responses
            var defenses = FindDefensiveResponses(offensiveSkill, context.DefensiveSkills);

            if (defenses.Count > 0)
            {
                // Delegate to defensive response resolver
                var defensiveResolver = new DefensiveResponseResolver();
                var defensiveContext = new InteractionContext
                {
                    OffensiveSkills = new List<SkillExecution> { offensiveSkill },
                    DefensiveSkills = defenses,
                    EnableDebugLogs = context.EnableDebugLogs
                };
                defensiveResolver.Resolve(defensiveContext);
            }
            else
            {
                // No defense, execute directly
                ExecuteUndefendedAttack(offensiveSkill, context);
            }
        }

        private List<SkillExecution> FindDefensiveResponses(
            SkillExecution offensive,
            List<SkillExecution> allDefensive)
        {
            var target = offensive.combatant.CurrentTarget;
            if (target == null) return new List<SkillExecution>();

            // Find defensive skills from the target
            return allDefensive.Where(d =>
                d.combatant.Transform == target &&
                SpeedResolver.CanInteract(offensive.skillType, d.skillType)
            ).ToList();
        }

        private void ExecuteUndefendedAttack(SkillExecution skill, InteractionContext context)
        {
            var target = skill.combatant.CurrentTarget;
            if (target == null)
            {
                if (context.EnableDebugLogs)
                    Debug.LogWarning($"{skill.combatant.name} has no target for {skill.skillType}");
                return;
            }

            var targetCombat = target.GetComponent<CombatController>();
            if (targetCombat == null || !targetCombat.IsAlive)
                return;

            // Calculate and apply damage
            int damage = DamageCalculator.CalculateDamage(
                skill.combatant.Stats,
                skill.combatant.WeaponTransform.GetComponent<WeaponController>().WeaponData,
                skill.skillType
            );

            targetCombat.TakeDamage(damage, skill.combatant.Transform);

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{skill.combatant.name}'s {skill.skillType} hit {target.name} for {damage} damage");
            }

            // Trigger skill execution phases (startup, active, recovery)
            skill.skillSystem.StartExecutionCoroutine(skill.skillType);
        }
    }
}
```

#### 5. DefensiveResponseResolver

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/Resolvers/DefensiveResponseResolver.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Resolves interactions between offensive and defensive skills.
    /// Handles Defense blocking, Counter reflecting, Smash breaking Counter.
    /// </summary>
    public class DefensiveResponseResolver : IInteractionResolver
    {
        public bool CanResolve(InteractionContext context)
        {
            return context.IsSingleOffensive && context.HasDefensiveSkills;
        }

        public void Resolve(InteractionContext context)
        {
            var offensiveSkill = context.OffensiveSkills[0];
            var defensiveSkill = context.DefensiveSkills[0]; // Assume one defense per attacker

            if (context.EnableDebugLogs)
            {
                Debug.Log($"[DefensiveResponseResolver] {offensiveSkill.skillType} vs {defensiveSkill.skillType}");
            }

            // Handle different defensive interactions
            switch (defensiveSkill.skillType)
            {
                case SkillType.Defense:
                    ResolveDefenseInteraction(offensiveSkill, defensiveSkill, context);
                    break;

                case SkillType.Counter:
                    ResolveCounterInteraction(offensiveSkill, defensiveSkill, context);
                    break;
            }
        }

        private void ResolveDefenseInteraction(
            SkillExecution attack,
            SkillExecution defense,
            InteractionContext context)
        {
            var attacker = attack.combatant;
            var defender = defense.combatant;

            // Calculate damage
            int baseDamage = DamageCalculator.CalculateDamage(
                attacker.Stats,
                attacker.WeaponTransform.GetComponent<WeaponController>().WeaponData,
                attack.skillType
            );

            // Apply defense reduction
            int reducedDamage = ApplyDefenseReduction(baseDamage, defender, context);

            // Apply reduced damage
            defender.TakeDamage(reducedDamage, attacker.Transform);

            // Cancel defense
            defense.skillSystem.ForceResetState();

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{defender.name}'s Defense reduced damage from {baseDamage} to {reducedDamage}");
            }

            // Trigger attack execution
            attack.skillSystem.StartExecutionCoroutine(attack.skillType);
        }

        private void ResolveCounterInteraction(
            SkillExecution attack,
            SkillExecution counter,
            InteractionContext context)
        {
            var attacker = attack.combatant;
            var defender = counter.combatant;

            // Special case: Smash breaks Counter
            if (attack.skillType == SkillType.Smash)
            {
                ResolveSmashBreaksCounter(attack, counter, context);
                return;
            }

            // Counter reflects damage
            int attackerDamage = DamageCalculator.CalculateDamage(
                attacker.Stats,
                attacker.WeaponTransform.GetComponent<WeaponController>().WeaponData,
                attack.skillType
            );

            int defenderDamage = DamageCalculator.CalculateDamage(
                defender.Stats,
                defender.WeaponTransform.GetComponent<WeaponController>().WeaponData,
                SkillType.Counter
            );

            // Apply reflected damage to attacker
            int reflectedDamage = Mathf.Max(attackerDamage, defenderDamage);
            attacker.TakeDamage(reflectedDamage, defender.Transform);

            // Apply knockdown
            var knockdownMeter = attacker.GetComponent<KnockdownMeterTracker>();
            if (knockdownMeter != null)
            {
                knockdownMeter.AddKnockdownValue(CombatConstants.COUNTER_KNOCKDOWN_VALUE);
            }

            // Cancel counter
            counter.skillSystem.ForceResetState();

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{defender.name}'s Counter reflected {reflectedDamage} damage to {attacker.name}");
            }

            // Trigger counter execution
            counter.skillSystem.StartExecutionCoroutine(SkillType.Counter);
        }

        private void ResolveSmashBreaksCounter(
            SkillExecution smash,
            SkillExecution counter,
            InteractionContext context)
        {
            var attacker = smash.combatant;
            var defender = counter.combatant;

            // Smash breaks counter and deals full damage
            int damage = DamageCalculator.CalculateDamage(
                attacker.Stats,
                attacker.WeaponTransform.GetComponent<WeaponController>().WeaponData,
                SkillType.Smash
            );

            defender.TakeDamage(damage, attacker.Transform);

            // Apply knockdown to defender
            var knockdownMeter = defender.GetComponent<KnockdownMeterTracker>();
            if (knockdownMeter != null)
            {
                knockdownMeter.AddKnockdownValue(CombatConstants.SMASH_KNOCKDOWN_VALUE);
            }

            // Cancel counter (broken)
            counter.skillSystem.ForceResetState();

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{attacker.name}'s Smash broke {defender.name}'s Counter for {damage} damage");
            }

            // Trigger smash execution
            smash.skillSystem.StartExecutionCoroutine(SkillType.Smash);
        }

        private int ApplyDefenseReduction(int baseDamage, CombatController defender, InteractionContext context)
        {
            // Get defender's defense stat and weapon modifier
            var weapon = defender.WeaponTransform.GetComponent<WeaponController>().WeaponData;
            float reductionPercent = CombatConstants.DEFENSE_DAMAGE_REDUCTION;

            // Apply reduction
            int reducedDamage = DamageCalculator.ApplyDamageReduction(
                baseDamage,
                reductionPercent,
                defender.Stats
            );

            return reducedDamage;
        }
    }
}
```

### Implementation Steps

#### Step 1: Create Resolver Infrastructure (2 hours)

1. Create directory: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/`
2. Create subdirectory: `/home/joe/FairyGate/Assets/Scripts/Combat/Interactions/Resolvers/`
3. Create `IInteractionResolver.cs`
4. Create `InteractionContext.cs`
5. Test compilation

#### Step 2: Extract Speed Conflict Resolver (3 hours)

1. Create `SpeedConflictResolver.cs`
2. Move all speed resolution logic from CombatInteractionManager
3. Test with multiple simultaneous attacks

#### Step 3: Extract Single Offensive Resolver (2 hours)

1. Create `SingleOffensiveResolver.cs`
2. Move single offensive execution logic
3. Test with undefended attacks

#### Step 4: Extract Defensive Response Resolver (3 hours)

1. Create `DefensiveResponseResolver.cs`
2. Move Defense, Counter, Smash-breaks-Counter logic
3. Test all defensive interactions

#### Step 5: Refactor CombatInteractionManager (2 hours)

1. Remove extracted logic
2. Add resolver instances
3. Delegate to resolvers in ProcessPendingExecutions()
4. Keep orchestration (queuing, timing windows)

#### Step 6: Testing & Validation (2 hours)

1. Test all combat interactions
2. Verify speed resolution still works
3. Verify defensive responses work
4. Check edge cases (null targets, dead combatants)
5. Performance testing

### Migration Guide

#### For Existing Scenes

No migration needed. CombatInteractionManager is a singleton accessed via `Instance.ProcessSkillExecution()`, which remains unchanged.

#### For Existing Code

All external APIs remain the same:
- `ProcessSkillExecution(skillSystem, skillType)` unchanged
- Internal refactoring only

#### For New Interactions

**Adding a new skill interaction**:

Before (modify CombatInteractionManager):
1. Find the right method (hard)
2. Add case to switch statement
3. Risk breaking existing interactions

After (create new resolver):
1. Create `NewInteractionResolver.cs` implementing `IInteractionResolver`
2. Add to resolver list in CombatInteractionManager
3. Existing interactions untouched

### Testing Checklist

- [ ] Speed conflicts resolve correctly
- [ ] Single attacks execute
- [ ] Defense blocks and reduces damage
- [ ] Counter reflects damage
- [ ] Smash breaks Counter
- [ ] Windmill hits multiple targets
- [ ] Ranged attacks work
- [ ] Simultaneous attacks from same combatant work
- [ ] Tie resolution (equal speed) works
- [ ] Edge case: null target
- [ ] Edge case: dead target
- [ ] Performance acceptable

### Benefits

✅ **CombatInteractionManager reduced from 753 → ~300 lines**
✅ **Each resolver isolated (~150-200 lines)**
✅ **Easy to add new interactions**
✅ **Easy to modify interaction rules**
✅ **Testable** (can test resolvers in isolation)
✅ **Debugging easier** (follow resolver chain)

---

## Phase 3: Centralized Input System

**Complexity**: Medium
**Estimated Effort**: 8-10 hours
**Impact**: Medium-High (enables rebinding, gamepad support)
**Dependencies**: None
**Risk**: Medium (touches many systems)

### Goal

Create a centralized InputManager to decouple input reading from gameplay systems. Currently input is scattered across SkillSystem, MovementController, CameraController.

### Architecture

#### Current (Scattered)

```
┌──────────────┐  ┌───────────────┐  ┌─────────────┐
│ SkillSystem  │  │MovementControl│  │CameraControl│
│              │  │               │  │             │
│ Input.GetKey │  │ Input.GetKey  │  │ Input.GetKey│
│  (1-6 keys)  │  │ (WASD keys)   │  │ (Arrow keys)│
└──────────────┘  └───────────────┘  └─────────────┘

Each system directly polls Unity Input
Hard-coded key bindings
No rebinding support
No gamepad support
```

#### Proposed (Centralized)

```
┌─────────────────────────────────────────┐
│          InputManager                   │
│  (Centralized input polling)            │
│                                         │
│  - PollInput() each frame               │
│  - Expose input state via properties    │
│  - Support rebinding                    │
│  - Support keyboard + gamepad           │
└──────────────┬──────────────────────────┘
               │ consumed by
      ┌────────┴────────┬─────────────┐
      │                 │             │
┌─────▼────────┐  ┌─────▼─────┐  ┌───▼────┐
│ SkillSystem  │  │ Movement  │  │ Camera │
│ Reads state  │  │ Reads     │  │ Reads  │
│              │  │ state     │  │ state  │
└──────────────┘  └───────────┘  └────────┘
```

### Files to Create

#### 1. InputManager

**Path**: `/home/joe/FairyGate/Assets/Scripts/Input/InputManager.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Centralized input manager for all gameplay input.
    /// Polls input each frame and exposes state via properties.
    /// Supports rebinding and multiple input devices.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Movement Keys")]
        [SerializeField] private KeyCode forwardKey = KeyCode.W;
        [SerializeField] private KeyCode backwardKey = KeyCode.S;
        [SerializeField] private KeyCode leftKey = KeyCode.A;
        [SerializeField] private KeyCode rightKey = KeyCode.D;

        [Header("Camera Keys")]
        [SerializeField] private KeyCode cameraLeftKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode cameraRightKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode cameraUpKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode cameraDownKey = KeyCode.DownArrow;

        [Header("Skill Keys")]
        [SerializeField] private KeyCode attackKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode defenseKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode counterKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode smashKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode windmillKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode rangedAttackKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode cancelKey = KeyCode.Space;

        [Header("Combat Keys")]
        [SerializeField] private KeyCode targetKey = KeyCode.Tab;
        [SerializeField] private KeyCode exitCombatKey = KeyCode.Escape;
        [SerializeField] private KeyCode restKey = KeyCode.X;

        // Singleton
        private static InputManager instance;
        public static InputManager Instance => instance;

        // Movement state
        public bool MoveForward { get; private set; }
        public bool MoveBackward { get; private set; }
        public bool MoveLeft { get; private set; }
        public bool MoveRight { get; private set; }
        public Vector2 MovementInput { get; private set; }

        // Camera state
        public bool CameraLeft { get; private set; }
        public bool CameraRight { get; private set; }
        public bool CameraUp { get; private set; }
        public bool CameraDown { get; private set; }

        // Skill state (GetKeyDown for one-shot actions)
        public bool AttackPressed { get; private set; }
        public bool DefensePressed { get; private set; }
        public bool CounterPressed { get; private set; }
        public bool SmashPressed { get; private set; }
        public bool WindmillPressed { get; private set; }
        public bool RangedAttackPressed { get; private set; }
        public bool CancelPressed { get; private set; }

        // Combat state
        public bool TargetPressed { get; private set; }
        public bool ExitCombatPressed { get; private set; }
        public bool RestPressed { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            PollInput();
        }

        private void PollInput()
        {
            // Movement (GetKey for continuous)
            MoveForward = Input.GetKey(forwardKey);
            MoveBackward = Input.GetKey(backwardKey);
            MoveLeft = Input.GetKey(leftKey);
            MoveRight = Input.GetKey(rightKey);

            // Calculate movement vector
            float x = (MoveRight ? 1f : 0f) - (MoveLeft ? 1f : 0f);
            float z = (MoveForward ? 1f : 0f) - (MoveBackward ? 1f : 0f);
            MovementInput = new Vector2(x, z);

            // Camera (GetKey for continuous)
            CameraLeft = Input.GetKey(cameraLeftKey);
            CameraRight = Input.GetKey(cameraRightKey);
            CameraUp = Input.GetKey(cameraUpKey);
            CameraDown = Input.GetKey(cameraDownKey);

            // Skills (GetKeyDown for one-shot)
            AttackPressed = Input.GetKeyDown(attackKey);
            DefensePressed = Input.GetKeyDown(defenseKey);
            CounterPressed = Input.GetKeyDown(counterKey);
            SmashPressed = Input.GetKeyDown(smashKey);
            WindmillPressed = Input.GetKeyDown(windmillKey);
            RangedAttackPressed = Input.GetKeyDown(rangedAttackKey);
            CancelPressed = Input.GetKeyDown(cancelKey);

            // Combat (GetKeyDown for one-shot)
            TargetPressed = Input.GetKeyDown(targetKey);
            ExitCombatPressed = Input.GetKeyDown(exitCombatKey);
            RestPressed = Input.GetKeyDown(restKey);
        }

        // Rebinding methods
        public void RebindMovementKey(string direction, KeyCode newKey)
        {
            switch (direction.ToLower())
            {
                case "forward": forwardKey = newKey; break;
                case "backward": backwardKey = newKey; break;
                case "left": leftKey = newKey; break;
                case "right": rightKey = newKey; break;
            }
        }

        public void RebindSkillKey(SkillType skillType, KeyCode newKey)
        {
            switch (skillType)
            {
                case SkillType.Attack: attackKey = newKey; break;
                case SkillType.Defense: defenseKey = newKey; break;
                case SkillType.Counter: counterKey = newKey; break;
                case SkillType.Smash: smashKey = newKey; break;
                case SkillType.Windmill: windmillKey = newKey; break;
                case SkillType.RangedAttack: rangedAttackKey = newKey; break;
            }
        }

        // Get current bindings (for UI display)
        public KeyCode GetMovementKey(string direction)
        {
            switch (direction.ToLower())
            {
                case "forward": return forwardKey;
                case "backward": return backwardKey;
                case "left": return leftKey;
                case "right": return rightKey;
                default: return KeyCode.None;
            }
        }

        public KeyCode GetSkillKey(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Attack: return attackKey;
                case SkillType.Defense: return defenseKey;
                case SkillType.Counter: return counterKey;
                case SkillType.Smash: return smashKey;
                case SkillType.Windmill: return windmillKey;
                case SkillType.RangedAttack: return rangedAttackKey;
                default: return KeyCode.None;
            }
        }
    }
}
```

### Files to Modify

#### Modified SkillSystem.cs

**Changes**:
- Remove input key fields
- Replace `Input.GetKeyDown()` with `InputManager.Instance` properties

**Before**:
```csharp
[Header("Input Keys")]
[SerializeField] private KeyCode attackKey = KeyCode.Alpha1;
[SerializeField] private KeyCode defenseKey = KeyCode.Alpha2;
// ... more keys

private void HandleSkillInput()
{
    if (!isPlayerControlled) return;

    if (Input.GetKeyDown(attackKey))
        StartCharging(SkillType.Attack);

    if (Input.GetKeyDown(defenseKey))
        StartCharging(SkillType.Defense);

    // ... more input checks
}
```

**After**:
```csharp
private void HandleSkillInput()
{
    if (!isPlayerControlled) return;

    var input = InputManager.Instance;
    if (input == null) return; // Safety check

    if (input.AttackPressed)
        StartCharging(SkillType.Attack);

    if (input.DefensePressed)
        StartCharging(SkillType.Defense);

    if (input.CounterPressed)
        StartCharging(SkillType.Counter);

    if (input.SmashPressed)
        StartCharging(SkillType.Smash);

    if (input.WindmillPressed)
        StartCharging(SkillType.Windmill);

    if (input.RangedAttackPressed)
        StartCharging(SkillType.RangedAttack);

    if (input.CancelPressed)
        CancelSkill();
}
```

#### Modified MovementController.cs

**Changes**:
- Remove input key fields
- Replace `Input.GetKey()` with `InputManager.Instance.MovementInput`

**Before**:
```csharp
[Header("Input")]
[SerializeField] private KeyCode forwardKey = KeyCode.W;
[SerializeField] private KeyCode backwardKey = KeyCode.S;
[SerializeField] private KeyCode leftKey = KeyCode.A;
[SerializeField] private KeyCode rightKey = KeyCode.D;

private void UpdateMovement()
{
    Vector3 inputDirection = Vector3.zero;

    if (Input.GetKey(forwardKey))
        inputDirection += Vector3.forward;
    if (Input.GetKey(backwardKey))
        inputDirection += Vector3.back;
    if (Input.GetKey(leftKey))
        inputDirection += Vector3.left;
    if (Input.GetKey(rightKey))
        inputDirection += Vector3.right;

    // ... process input
}
```

**After**:
```csharp
private void UpdateMovement()
{
    var input = InputManager.Instance;
    if (input == null) return;

    Vector2 moveInput = input.MovementInput;
    Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

    // Transform direction based on camera
    if (useCameraRelativeMovement && cameraTransform != null && inputDirection != Vector3.zero)
    {
        // ... camera-relative transformation
    }

    // ... process movement
}
```

#### Modified CameraController.cs

**Changes**:
- Remove input key fields
- Replace `Input.GetKey()` with `InputManager.Instance` properties

**Before**:
```csharp
private void HandleInput()
{
    if (Input.GetKey(KeyCode.LeftArrow))
        currentRotationAngle += rotationSpeed * Time.deltaTime;

    if (Input.GetKey(KeyCode.RightArrow))
        currentRotationAngle -= rotationSpeed * Time.deltaTime;

    if (enableZoom)
    {
        if (Input.GetKey(KeyCode.UpArrow))
            distance -= zoomSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.DownArrow))
            distance += zoomSpeed * Time.deltaTime;
    }
}
```

**After**:
```csharp
private void HandleInput()
{
    var input = InputManager.Instance;
    if (input == null) return;

    if (input.CameraLeft)
        currentRotationAngle += rotationSpeed * Time.deltaTime;

    if (input.CameraRight)
        currentRotationAngle -= rotationSpeed * Time.deltaTime;

    if (enableZoom)
    {
        if (input.CameraUp)
            distance = Mathf.Max(minDistance, distance - zoomSpeed * Time.deltaTime);

        if (input.CameraDown)
            distance = Mathf.Min(maxDistance, distance + zoomSpeed * Time.deltaTime);
    }
}
```

#### Modified CombatController.cs

**Changes**:
- Remove input key fields for target/exit combat/rest
- Replace `Input.GetKeyDown()` with `InputManager.Instance`

**Before**:
```csharp
[Header("Input")]
[SerializeField] private KeyCode targetKey = KeyCode.Tab;
[SerializeField] private KeyCode exitCombatKey = KeyCode.Escape;
[SerializeField] private KeyCode restKey = KeyCode.X;

private void HandleCombatInput()
{
    if (Input.GetKeyDown(targetKey))
        CycleTarget();

    if (Input.GetKeyDown(exitCombatKey))
        ExitCombat();

    if (Input.GetKeyDown(restKey))
        ToggleRest();
}
```

**After**:
```csharp
private void HandleCombatInput()
{
    var input = InputManager.Instance;
    if (input == null) return;

    if (input.TargetPressed)
        CycleTarget();

    if (input.ExitCombatPressed)
        ExitCombat();

    if (input.RestPressed)
        ToggleRest();
}
```

### Implementation Steps

#### Step 1: Create InputManager (2 hours)

1. Create directory: `/home/joe/FairyGate/Assets/Scripts/Input/`
2. Create `InputManager.cs`
3. Add InputManager to scene (or have it create itself)
4. Test that input is polled correctly

#### Step 2: Migrate SkillSystem (1 hour)

1. Replace Input.GetKeyDown with InputManager
2. Remove key fields
3. Test all skills still work

#### Step 3: Migrate MovementController (1 hour)

1. Replace Input.GetKey with InputManager.MovementInput
2. Remove key fields
3. Test movement still works

#### Step 4: Migrate CameraController (1 hour)

1. Replace Input.GetKey with InputManager
2. Remove key fields
3. Test camera rotation/zoom works

#### Step 5: Migrate CombatController (30 min)

1. Replace Input.GetKeyDown with InputManager
2. Remove key fields
3. Test targeting/exit/rest work

#### Step 6: Update Scene Setup (30 min)

1. Add InputManager creation to CompleteCombatSceneSetup.cs
2. Ensure InputManager created before characters

#### Step 7: Testing & Validation (2 hours)

1. Test all input works
2. Test in multiple scenes
3. Test rebinding (if implementing UI for it)
4. Performance testing

### Migration Guide

#### For Existing Scenes

**Manual Migration**:
1. Add InputManager GameObject to scene
2. No other changes needed (systems will find InputManager via singleton)

**Automated via Menu**:
Add menu item to CompleteCombatSceneSetup:
```csharp
[MenuItem("Combat/Fix/Add InputManager to Scene")]
public static void AddInputManagerToScene()
{
    if (GameObject.Find("InputManager") == null)
    {
        var go = new GameObject("InputManager");
        go.AddComponent<InputManager>();
        Debug.Log("✅ Added InputManager to scene");
    }
    else
    {
        Debug.Log("⚠️ InputManager already exists");
    }
}
```

#### For Existing Prefabs

No changes needed. Prefabs don't contain input code.

#### For New Features

Use `InputManager.Instance` instead of `Input` class:
```csharp
// Old way
if (Input.GetKeyDown(KeyCode.Space))
    DoAction();

// New way
if (InputManager.Instance.CancelPressed)
    DoAction();
```

### Testing Checklist

- [ ] All movement keys work (WASD)
- [ ] All skill keys work (1-6, Space)
- [ ] All camera keys work (Arrows)
- [ ] All combat keys work (Tab, Esc, X)
- [ ] InputManager persists across scenes (DontDestroyOnLoad)
- [ ] No duplicate InputManagers
- [ ] AI still works (doesn't use InputManager)
- [ ] Rebinding works (if implemented)
- [ ] Performance unchanged

### Benefits

✅ **Single source of truth for input**
✅ **Easy to add rebinding UI**
✅ **Easy to add gamepad support**
✅ **Input code removed from gameplay systems**
✅ **Testable** (can mock InputManager)
✅ **Can replay inputs** (record/playback for testing)

---

## Phase 4: Configuration ScriptableObjects

**Complexity**: Low-Medium
**Estimated Effort**: 6-8 hours
**Impact**: Medium (designer workflow improvement)
**Dependencies**: None
**Risk**: Low (additive, doesn't break existing code)

### Goal

Move hardcoded configuration values from CombatConstants and component fields into designer-friendly ScriptableObjects. Enables tuning without code changes.

### Files to Create

#### 1. SkillConfigData

**Path**: `/home/joe/FairyGate/Assets/Scripts/Data/Configuration/SkillConfigData.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Configuration data for a specific skill type.
    /// Allows designers to tune skill behavior without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillConfig_", menuName = "FairyGate/Configuration/Skill Config")]
    public class SkillConfigData : ScriptableObject
    {
        [Header("Identity")]
        public SkillType skillType;

        [Header("Stamina")]
        public int staminaCost = 2;

        [Header("Timing")]
        public float chargeTime = 1.0f;
        public float startupTime = 0.2f;
        public float activeTime = 0.2f;
        public float recoveryTime = 0.3f;

        [Header("Damage")]
        public int baseDamage = 10;
        public float damageMultiplier = 1.0f;

        [Header("Knockdown")]
        public int knockdownValue = 0;

        [Header("Range")]
        public float baseRange = 2.0f;

        [Header("Movement")]
        [Range(0f, 1f)]
        public float chargingMovementModifier = 1.0f;
        [Range(0f, 1f)]
        public float waitingMovementModifier = 1.0f;

        [Header("Effects")]
        public StatusEffectType[] appliesEffects;
        public float effectDuration = 2.0f;
    }
}
```

#### 2. CombatConfigData

**Path**: `/home/joe/FairyGate/Assets/Scripts/Data/Configuration/CombatConfigData.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Global combat system configuration.
    /// Replaces CombatConstants with designer-friendly values.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "FairyGate/Configuration/Combat Config")]
    public class CombatConfigData : ScriptableObject
    {
        [Header("Timing")]
        [Tooltip("Window in seconds for skills to be considered simultaneous")]
        public float simultaneousExecutionWindow = 0.1f;

        [Header("Defense")]
        [Range(0f, 1f)]
        [Tooltip("Damage reduction percentage when defending (0.5 = 50% reduction)")]
        public float defenseDamageReduction = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Movement speed modifier while defending")]
        public float defenseMovementModifier = 0.7f;

        [Header("Counter")]
        [Tooltip("Knockdown meter value added on successful counter")]
        public int counterKnockdownValue = 3;

        [Range(0f, 1f)]
        [Tooltip("Movement speed modifier while charging counter")]
        public float counterChargingMovementModifier = 0.7f;

        [Header("Smash")]
        [Tooltip("Knockdown meter value added on successful smash")]
        public int smashKnockdownValue = 2;

        [Header("Windmill")]
        [Tooltip("Number of hits in windmill multi-hit")]
        public int windmillHitCount = 3;

        [Tooltip("Delay between windmill hits")]
        public float windmillHitDelay = 0.2f;

        [Range(0f, 1f)]
        [Tooltip("Movement speed modifier while charging windmill")]
        public float windmillMovementModifier = 0.7f;

        [Header("Ranged")]
        [Tooltip("Maximum range for ranged attacks")]
        public float rangedMaxRange = 15f;

        [Tooltip("Time window for aiming ranged attack")]
        public float rangedAimDuration = 2.0f;

        [Range(0f, 1f)]
        [Tooltip("Movement speed modifier while aiming")]
        public float rangedAimingMovementModifier = 0.5f;

        [Header("Status Effects")]
        [Tooltip("Duration of stun applied when losing speed conflict")]
        public float stunDuration = 1.5f;

        [Tooltip("Duration of knockdown state")]
        public float knockdownDuration = 2.0f;

        [Tooltip("Knockdown meter threshold")]
        public int knockdownThreshold = 10;

        [Header("Stats")]
        [Tooltip("Divisor for converting dexterity to speed bonus")]
        public float dexteritySpeedDivisor = 10f;

        [Tooltip("Base health for characters without stats")]
        public int baseHealth = 100;

        [Tooltip("Minimum damage that can be dealt")]
        public int minimumDamage = 1;

        [Header("Rest")]
        [Tooltip("Stamina regeneration per second while resting")]
        public int restStaminaRegenRate = 3;

        [Header("AI")]
        [Tooltip("Maximum number of AI that can attack simultaneously")]
        public int maxSimultaneousAttackers = 1;

        [Tooltip("Minimum time between AI attack grants")]
        public float minTimeBetweenAttacks = 0.8f;
    }
}
```

#### 3. MovementConfigData

**Path**: `/home/joe/FairyGate/Assets/Scripts/Data/Configuration/MovementConfigData.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Movement system configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "FairyGate/Configuration/Movement Config")]
    public class MovementConfigData : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("Base movement speed (units per second)")]
        public float baseMovementSpeed = 5.0f;

        [Header("Camera")]
        [Tooltip("Use camera-relative movement")]
        public bool useCameraRelativeMovement = true;

        [Header("Gravity")]
        [Tooltip("Gravity force applied when not grounded")]
        public float gravity = 9.81f;
    }
}
```

#### 4. CameraConfigData

**Path**: `/home/joe/FairyGate/Assets/Scripts/Data/Configuration/CameraConfigData.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Camera system configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "FairyGate/Configuration/Camera Config")]
    public class CameraConfigData : ScriptableObject
    {
        [Header("Distance")]
        [Tooltip("Default distance from player")]
        public float defaultDistance = 10f;

        [Tooltip("Default height above ground")]
        public float defaultHeight = 8f;

        [Tooltip("Minimum zoom distance")]
        public float minDistance = 5f;

        [Tooltip("Maximum zoom distance")]
        public float maxDistance = 20f;

        [Header("Speed")]
        [Tooltip("Rotation speed in degrees per second")]
        public float rotationSpeed = 90f;

        [Tooltip("Zoom speed")]
        public float zoomSpeed = 5f;

        [Header("Features")]
        [Tooltip("Enable zoom with up/down arrows")]
        public bool enableZoom = true;

        [Tooltip("Auto-find player on start")]
        public bool autoFindPlayer = true;
    }
}
```

### Files to Modify

#### Modified CombatConstants.cs

**Changes**: Keep as fallback values, but allow override from ScriptableObject

**Before**:
```csharp
public static class CombatConstants
{
    public const float SIMULTANEOUS_EXECUTION_WINDOW = 0.1f;
    public const float DEFENSE_DAMAGE_REDUCTION = 0.5f;
    // ... 30+ constants
}
```

**After**:
```csharp
public static class CombatConstants
{
    // Singleton config reference
    private static CombatConfigData config;

    public static void Initialize(CombatConfigData configData)
    {
        config = configData;
    }

    // Properties with fallback to hardcoded values
    public static float SIMULTANEOUS_EXECUTION_WINDOW =>
        config != null ? config.simultaneousExecutionWindow : 0.1f;

    public static float DEFENSE_DAMAGE_REDUCTION =>
        config != null ? config.defenseDamageReduction : 0.5f;

    // ... convert all constants to properties
}
```

#### Systems Consuming Config

SkillSystem, CombatInteractionManager, MovementController, CameraController, etc. would load their respective configs on Awake():

```csharp
[Header("Configuration")]
[SerializeField] private SkillConfigData attackConfig;
[SerializeField] private SkillConfigData defenseConfig;
// ... one config per skill

private void Awake()
{
    // Load configs
    if (attackConfig == null)
    {
        attackConfig = Resources.Load<SkillConfigData>("Configs/Skills/Attack");
    }
    // ... load other configs
}
```

### Implementation Steps

#### Step 1: Create Config ScriptableObjects (2 hours)

1. Create directory: `/home/joe/FairyGate/Assets/Scripts/Data/Configuration/`
2. Create all config ScriptableObject classes
3. Create directory: `/home/joe/FairyGate/Assets/Resources/Configs/`
4. Create default config assets for each

#### Step 2: Modify CombatConstants (1 hour)

1. Convert constants to properties
2. Add Initialize() method
3. Add config reference
4. Keep fallback values

#### Step 3: Update GameManager (1 hour)

1. Add config loading to GameManager
2. Initialize CombatConstants with configs
3. Expose configs via singleton

#### Step 4: Update Systems (2 hours)

1. Add config fields to SkillSystem, MovementController, etc.
2. Load configs in Awake()
3. Replace hardcoded values with config values

#### Step 5: Create Default Assets (1 hour)

1. Create SkillConfigData assets for all 6 skills
2. Create CombatConfigData asset
3. Create MovementConfigData asset
4. Create CameraConfigData asset
5. Save to Resources/Configs/

#### Step 6: Testing (1 hour)

1. Test that systems load configs
2. Test fallback values work if configs missing
3. Test changing config values affects gameplay
4. Performance testing

### Migration Guide

#### For Existing Scenes

**Option 1 - Keep Defaults**:
- No changes needed
- Systems use hardcoded fallback values

**Option 2 - Use Configs**:
1. Create config assets via "Create > FairyGate > Configuration > ..."
2. Assign configs to systems in scene

#### For Existing Prefabs

1. Open prefab
2. Assign config assets to exposed fields
3. Save prefab

#### For Designers

**Tuning Workflow**:
1. Open config asset in Inspector
2. Modify values (e.g., increase Attack damage)
3. Press Play to test
4. Iterate quickly without code changes

### Benefits

✅ **Designer-friendly tuning**
✅ **No code changes for balance adjustments**
✅ **Easy to create variants** (e.g., HardMode config)
✅ **Version control friendly** (configs are .asset files)
✅ **Fallback values prevent breaking**
✅ **Hot-reload in editor** (change config, see results immediately)

---

## Phase 5: State Machine Pattern

**Complexity**: High
**Estimated Effort**: 16-20 hours
**Impact**: Medium (cleaner state management)
**Dependencies**: Phase 1 (Skill Handlers) recommended first
**Risk**: High (major refactoring of state logic)

### Goal

Replace manual state tracking in SkillSystem with a formal State Machine pattern. Makes state transitions explicit and easier to debug.

### Architecture

#### Current (Manual State Tracking)

```csharp
public class SkillSystem : MonoBehaviour
{
    private SkillExecutionState currentState;
    private float chargeProgress;
    private Coroutine executionCoroutine;

    public void StartCharging(SkillType type)
    {
        // Manual state validation
        if (currentState != SkillExecutionState.Uncharged)
            return;

        // Manual state transition
        currentState = SkillExecutionState.Charging;
        chargeProgress = 0f;
        // ... more manual management
    }

    public void ExecuteSkill(SkillType type)
    {
        if (currentState != SkillExecutionState.Charged)
            return;

        currentState = SkillExecutionState.Startup;
        // ... manual transition
    }

    // ... 10+ methods manually managing state
}
```

#### Proposed (State Machine)

```
┌──────────────────────────────────────┐
│       SkillStateMachine              │
│  (Manages state transitions)         │
│                                      │
│  - currentState: ISkillState         │
│  - ChangeState(newState)             │
│  - Update()                          │
└──────────┬───────────────────────────┘
           │
    ┌──────┴──────┐
    │ ISkillState │
    │  Interface  │
    └──────┬──────┘
           │
    ┌──────┴─────┬──────────┬─────────┬────────────┬──────────┐
    │            │          │         │            │          │
┌───▼────┐  ┌───▼───┐  ┌───▼──┐  ┌───▼────┐  ┌───▼───┐  ┌───▼────┐
│Uncharged│  │Charging│  │Charged│  │Startup │  │Active │  │Recovery│
│ State  │  │ State  │  │ State │  │ State  │  │ State │  │ State  │
└────────┘  └────────┘  └───────┘  └────────┘  └───────┘  └────────┘

Each state handles:
- OnEnter() - Called when entering state
- OnUpdate() - Called each frame
- OnExit() - Called when leaving state
- CanTransitionTo(state) - Validate transitions
```

### Files to Create

#### 1. ISkillState Interface

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/StateMachine/ISkillState.cs`

```csharp
namespace FairyGate.Combat
{
    /// <summary>
    /// Interface for skill execution states.
    /// Each state handles entry, update, exit, and transition validation.
    /// </summary>
    public interface ISkillState
    {
        /// <summary>
        /// State type this represents
        /// </summary>
        SkillExecutionState StateType { get; }

        /// <summary>
        /// Called when entering this state
        /// </summary>
        void OnEnter(SkillStateMachineContext context);

        /// <summary>
        /// Called each frame while in this state
        /// Returns true to trigger auto-transition to next state
        /// </summary>
        bool OnUpdate(SkillStateMachineContext context, float deltaTime);

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void OnExit(SkillStateMachineContext context);

        /// <summary>
        /// Check if can transition to target state
        /// </summary>
        bool CanTransitionTo(SkillExecutionState targetState);
    }
}
```

#### 2. SkillStateMachineContext

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/StateMachine/SkillStateMachineContext.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Context data for skill state machine.
    /// Shared across all states.
    /// </summary>
    public class SkillStateMachineContext
    {
        // Component references
        public SkillSystem SkillSystem { get; set; }
        public CombatController CombatController { get; set; }
        public WeaponController WeaponController { get; set; }
        public StaminaSystem StaminaSystem { get; set; }
        public MovementController MovementController { get; set; }

        // Current skill being executed
        public SkillType CurrentSkill { get; set; }
        public ISkillHandler CurrentSkillHandler { get; set; }

        // Charging data
        public float ChargeStartTime { get; set; }
        public float ChargeProgress { get; set; }

        // Execution data
        public float ExecutionStartTime { get; set; }
        public Transform Target { get; set; }

        // Configuration
        public bool EnableDebugLogs { get; set; }

        public SkillStateMachineContext(SkillSystem skillSystem)
        {
            SkillSystem = skillSystem;
            CombatController = skillSystem.GetComponent<CombatController>();
            WeaponController = skillSystem.GetComponent<WeaponController>();
            StaminaSystem = skillSystem.GetComponent<StaminaSystem>();
            MovementController = skillSystem.GetComponent<MovementController>();
        }
    }
}
```

#### 3. SkillStateMachine

**Path**: `/home/joe/FairyGate/Assets/Scripts/Combat/Skills/StateMachine/SkillStateMachine.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Manages skill execution state transitions.
    /// Enforces valid transitions and handles state lifecycle.
    /// </summary>
    public class SkillStateMachine
    {
        private ISkillState currentState;
        private Dictionary<SkillExecutionState, ISkillState> states;
        private SkillStateMachineContext context;

        public SkillExecutionState CurrentStateType => currentState.StateType;

        public SkillStateMachine(SkillStateMachineContext context)
        {
            this.context = context;

            // Register all states
            states = new Dictionary<SkillExecutionState, ISkillState>
            {
                { SkillExecutionState.Uncharged, new UnchargedState() },
                { SkillExecutionState.Charging, new ChargingState() },
                { SkillExecutionState.Charged, new ChargedState() },
                { SkillExecutionState.Startup, new StartupState() },
                { SkillExecutionState.Active, new ActiveState() },
                { SkillExecutionState.Recovery, new RecoveryState() },
                { SkillExecutionState.Waiting, new WaitingState() },
                { SkillExecutionState.Aiming, new AimingState() }
            };

            // Start in uncharged state
            currentState = states[SkillExecutionState.Uncharged];
            currentState.OnEnter(context);
        }

        public void Update(float deltaTime)
        {
            if (currentState == null) return;

            // Update current state
            bool shouldTransition = currentState.OnUpdate(context, deltaTime);

            // Handle auto-transitions
            if (shouldTransition)
            {
                // State determined it should transition
                // Next state determined by state itself
            }
        }

        public bool ChangeState(SkillExecutionState newStateType)
        {
            // Validate transition
            if (!currentState.CanTransitionTo(newStateType))
            {
                if (context.EnableDebugLogs)
                {
                    Debug.LogWarning($"Cannot transition from {currentState.StateType} to {newStateType}");
                }
                return false;
            }

            // Get new state
            if (!states.TryGetValue(newStateType, out var newState))
            {
                Debug.LogError($"State {newStateType} not registered");
                return false;
            }

            if (context.EnableDebugLogs)
            {
                Debug.Log($"[State Machine] {currentState.StateType} → {newStateType}");
            }

            // Exit old state
            currentState.OnExit(context);

            // Enter new state
            currentState = newState;
            currentState.OnEnter(context);

            return true;
        }

        public bool CanTransitionTo(SkillExecutionState targetState)
        {
            return currentState.CanTransitionTo(targetState);
        }
    }
}
```

#### 4. Example State Implementations

**UnchargedState.cs**:
```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Idle state - no skill active, waiting for player input.
    /// </summary>
    public class UnchargedState : ISkillState
    {
        public SkillExecutionState StateType => SkillExecutionState.Uncharged;

        public void OnEnter(SkillStateMachineContext context)
        {
            // Reset all skill data
            context.ChargeProgress = 0f;
            context.ChargeStartTime = 0f;
            context.CurrentSkill = SkillType.Attack; // Default
            context.CurrentSkillHandler = null;

            // Restore movement
            context.MovementController.ResetMovementSpeed();

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} entered Uncharged state");
            }
        }

        public bool OnUpdate(SkillStateMachineContext context, float deltaTime)
        {
            // Uncharged state doesn't auto-transition
            // Waits for player input to start charging
            return false;
        }

        public void OnExit(SkillStateMachineContext context)
        {
            // No cleanup needed
        }

        public bool CanTransitionTo(SkillExecutionState targetState)
        {
            // Can only transition to Charging from Uncharged
            return targetState == SkillExecutionState.Charging;
        }
    }
}
```

**ChargingState.cs**:
```csharp
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Charging state - holding skill button to charge.
    /// </summary>
    public class ChargingState : ISkillState
    {
        public SkillExecutionState StateType => SkillExecutionState.Charging;

        public void OnEnter(SkillStateMachineContext context)
        {
            context.ChargeStartTime = Time.time;
            context.ChargeProgress = 0f;

            // Apply movement restriction
            context.MovementController.ApplySkillMovementRestriction(
                context.CurrentSkill,
                SkillExecutionState.Charging
            );

            if (context.EnableDebugLogs)
            {
                Debug.Log($"{context.CombatController.name} started charging {context.CurrentSkill}");
            }
        }

        public bool OnUpdate(SkillStateMachineContext context, float deltaTime)
        {
            if (context.CurrentSkillHandler == null)
                return false;

            // Update charge progress
            float elapsed = Time.time - context.ChargeStartTime;
            float chargeTime = context.WeaponController.WeaponData.GetChargeTime(context.CurrentSkill);
            context.ChargeProgress = Mathf.Clamp01(elapsed / chargeTime);

            // Auto-transition when fully charged
            if (context.ChargeProgress >= 1f)
            {
                return true; // Signal to transition to Charged
            }

            return false;
        }

        public void OnExit(SkillStateMachineContext context)
        {
            // Movement restriction will be updated by next state
        }

        public bool CanTransitionTo(SkillExecutionState targetState)
        {
            // Can transition to Charged (fully charged) or Uncharged (cancelled)
            return targetState == SkillExecutionState.Charged ||
                   targetState == SkillExecutionState.Uncharged;
        }
    }
}
```

Similar implementations for:
- **ChargedState.cs** - Fully charged, waiting for execute input
- **StartupState.cs** - Execution startup frames
- **ActiveState.cs** - Active hit frames
- **RecoveryState.cs** - Recovery frames after execution
- **WaitingState.cs** - Defensive skills waiting for attacker
- **AimingState.cs** - Ranged attack aiming

### Implementation Effort

This phase is complex and touches core SkillSystem logic. Estimated **16-20 hours** total.

**Recommendation**: Only pursue if state-related bugs are frequent or if adding complex skills with many state transitions.

### Benefits

✅ **Explicit state transitions** (easier to debug)
✅ **Validated transitions** (prevents invalid states)
✅ **State-specific behavior isolated**
✅ **Visual state graph possible** (with editor tools)
✅ **Easier to add new states**

---

## Phase 6: UI Base Classes

**Complexity**: Low
**Estimated Effort**: 2-3 hours
**Impact**: Low (code cleanup, extensibility)
**Dependencies**: None
**Risk**: Low (simple refactoring)

### Goal

Create base class for health/stamina bars to eliminate code duplication. Makes it easy to add new bars (e.g., charge progress bar, knockdown meter bar).

### Architecture

#### Current (Duplicated Code)

```
┌──────────────┐  ┌──────────────┐
│ HealthBarUI  │  │ StaminaBarUI │
│ (~140 LOC)   │  │ (~170 LOC)   │
│              │  │              │
│ Duplicates:  │  │ Duplicates:  │
│ - OnGUI()    │  │ - OnGUI()    │
│ - DrawBar()  │  │ - DrawBar()  │
│ - SetTarget()│  │ - SetTarget()│
└──────────────┘  └──────────────┘
```

#### Proposed (Inheritance)

```
        ┌────────────────┐
        │   BaseBarUI    │
        │   (~100 LOC)   │
        │                │
        │ - OnGUI()      │
        │ - DrawBar()    │
        │ - SetTarget()  │
        └────────┬───────┘
                 │ inherited by
        ┌────────┴────────┐
        │                 │
 ┌──────▼───────┐  ┌──────▼────────┐
 │ HealthBarUI  │  │ StaminaBarUI  │
 │  (~40 LOC)   │  │  (~50 LOC)    │
 │              │  │               │
 │ Overrides:   │  │ Overrides:    │
 │ - GetCurrent │  │ - GetCurrent  │
 │ - GetMax     │  │ - GetMax      │
 │ - GetColor   │  │ - GetColor    │
 └──────────────┘  └───────────────┘
```

### File to Create

#### BaseBarUI.cs

**Path**: `/home/joe/FairyGate/Assets/Scripts/UI/BaseBarUI.cs`

```csharp
using UnityEngine;

namespace FairyGate.Combat.UI
{
    /// <summary>
    /// Base class for all on-screen bars (health, stamina, charge, etc).
    /// Handles common drawing logic, subclasses provide data source.
    /// </summary>
    public abstract class BaseBarUI : MonoBehaviour
    {
        [Header("Position")]
        [SerializeField] protected Vector2 barPosition = new Vector2(10, 10);

        [Header("Size")]
        [SerializeField] protected Vector2 barSize = new Vector2(250, 25);

        [Header("Colors")]
        [SerializeField] protected Color backgroundColor = Color.black;
        [SerializeField] protected Color borderColor = Color.white;

        [Header("Display")]
        [SerializeField] protected bool showText = true;
        [SerializeField] protected bool showBackground = true;

        protected virtual void OnGUI()
        {
            if (!Application.isPlaying) return;
            if (!IsTargetValid()) return;

            // Get current values from subclass
            float current = GetCurrentValue();
            float max = GetMaxValue();
            float percentage = max > 0 ? current / max : 0f;

            // Draw background
            if (showBackground)
            {
                GUI.color = backgroundColor;
                GUI.DrawTexture(new Rect(
                    barPosition.x - 2,
                    barPosition.y - 2,
                    barSize.x + 4,
                    barSize.y + 4
                ), Texture2D.whiteTexture);
            }

            // Draw fill bar
            Color fillColor = GetFillColor(percentage);
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(
                barPosition.x,
                barPosition.y,
                barSize.x * percentage,
                barSize.y
            ), Texture2D.whiteTexture);

            // Draw border
            GUI.color = borderColor;
            DrawBorder(barPosition, barSize);

            // Draw text
            if (showText)
            {
                GUI.color = Color.white;
                string text = GetDisplayText(current, max);
                GUI.Label(new Rect(
                    barPosition.x + barSize.x / 2 - 30,
                    barPosition.y + 2,
                    60,
                    barSize.y
                ), text);
            }

            GUI.color = Color.white; // Reset
        }

        protected void DrawBorder(Vector2 position, Vector2 size)
        {
            // Top
            GUI.DrawTexture(new Rect(position.x, position.y, size.x, 1), Texture2D.whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(position.x, position.y + size.y - 1, size.x, 1), Texture2D.whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(position.x, position.y, 1, size.y), Texture2D.whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(position.x + size.x - 1, position.y, 1, size.y), Texture2D.whiteTexture);
        }

        // Abstract methods for subclasses to implement
        protected abstract bool IsTargetValid();
        protected abstract float GetCurrentValue();
        protected abstract float GetMaxValue();
        protected abstract Color GetFillColor(float percentage);
        protected abstract string GetDisplayText(float current, float max);
    }
}
```

### Files to Modify

#### Modified HealthBarUI.cs

**Before** (~140 lines with duplicated drawing code):
```csharp
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Vector2 barPosition;
    [SerializeField] private Vector2 barSize;
    private HealthSystem targetHealthSystem;

    private void OnGUI()
    {
        // 80 lines of drawing code
    }

    // More methods...
}
```

**After** (~40 lines, inherits from BaseBarUI):
```csharp
public class HealthBarUI : BaseBarUI
{
    private HealthSystem targetHealthSystem;

    public void SetTargetHealthSystem(HealthSystem health)
    {
        targetHealthSystem = health;
    }

    protected override bool IsTargetValid()
    {
        return targetHealthSystem != null;
    }

    protected override float GetCurrentValue()
    {
        return targetHealthSystem.CurrentHealth;
    }

    protected override float GetMaxValue()
    {
        return targetHealthSystem.MaxHealth;
    }

    protected override Color GetFillColor(float percentage)
    {
        return Color.Lerp(Color.red, Color.green, percentage);
    }

    protected override string GetDisplayText(float current, float max)
    {
        return $"{(int)current}/{(int)max}";
    }
}
```

#### Modified StaminaBarUI.cs

Similar refactoring - inherits from BaseBarUI, implements abstract methods for stamina-specific behavior.

### Implementation Steps

1. Create `BaseBarUI.cs` (1 hour)
2. Refactor `HealthBarUI.cs` to inherit (30 min)
3. Refactor `StaminaBarUI.cs` to inherit (30 min)
4. Test both bars still work (30 min)

### Benefits

✅ **HealthBarUI**: 140 → 40 lines
✅ **StaminaBarUI**: 170 → 50 lines
✅ **Easy to add new bars** (inherit BaseBarUI, implement 5 methods)
✅ **Consistent appearance** across all bars
✅ **Single place to fix drawing bugs**

---

## Phase 7: Unit Testing Framework

**Complexity**: Medium
**Estimated Effort**: 12-16 hours
**Impact**: High (confidence in refactoring, catch regressions)
**Dependencies**: Phases 1-2 recommended (easier to test after extraction)
**Risk**: Low (additive, doesn't affect runtime)

### Goal

Add Unity Test Framework tests for core combat logic. Focus on:
- Damage calculation
- Stat modifications
- Skill interactions
- Speed resolution

### Setup

1. Install Unity Test Framework package
2. Create test assembly definitions
3. Create test directory structure

### Example Test Structure

```
Assets/Tests/
├── EditMode/
│   ├── DamageCalculatorTests.cs
│   ├── SpeedResolverTests.cs
│   └── CharacterStatsTests.cs
└── PlayMode/
    ├── SkillExecutionTests.cs
    ├── CombatInteractionTests.cs
    └── DefenseCounterTests.cs
```

### Example Test

```csharp
using NUnit.Framework;
using FairyGate.Combat;

[TestFixture]
public class DamageCalculatorTests
{
    [Test]
    public void CalculateDamage_WithDefaultStats_ReturnsBaseDamage()
    {
        // Arrange
        var stats = CharacterStats.CreateDefaultStats();
        var weapon = WeaponData.CreateSwordData();

        // Act
        int damage = DamageCalculator.CalculateDamage(stats, weapon, SkillType.Attack);

        // Assert
        Assert.Greater(damage, 0);
    }

    [Test]
    public void ApplyDamageReduction_WithFiftyPercent_ReducesHalf()
    {
        // Arrange
        int baseDamage = 100;
        float reduction = 0.5f;
        var stats = CharacterStats.CreateDefaultStats();

        // Act
        int reduced = DamageCalculator.ApplyDamageReduction(baseDamage, reduction, stats);

        // Assert
        Assert.AreEqual(50, reduced);
    }
}
```

### Benefits

✅ **Catch regressions** when refactoring
✅ **Document expected behavior**
✅ **Refactor with confidence**
✅ **Faster iteration** (automated vs. manual testing)

---

## Implementation Order

### Recommended Sequence

**Phase 1 Priority** (Highest Impact, Lowest Risk):
1. **Phase 6: UI Base Classes** (2-3 hours) - Quick win, low risk
2. **Phase 1: Extract Skill Handlers** (8-12 hours) - High impact, enables extensibility
3. **Phase 3: Centralized Input System** (8-10 hours) - Medium impact, enables features

**Phase 2 Priority** (Medium Impact):
4. **Phase 4: Configuration ScriptableObjects** (6-8 hours) - Designer workflow improvement
5. **Phase 2: Combat Resolution Strategies** (12-16 hours) - High complexity, high reward

**Phase 3 Priority** (Long-term):
6. **Phase 7: Unit Testing Framework** (12-16 hours) - Foundation for future confidence
7. **Phase 5: State Machine Pattern** (16-20 hours) - Only if state bugs frequent

### Total Effort Estimate

- **Minimum** (Phases 1, 3, 6): ~18-25 hours
- **Recommended** (Phases 1-4, 6): ~36-51 hours
- **Complete** (All phases): ~64-91 hours

---

## Migration Guides

### Scene Migration

Most refactorings are **backward compatible** and require no scene migration:
- Phase 1: SkillSystem API unchanged
- Phase 2: CombatInteractionManager singleton unchanged
- Phase 3: Requires adding InputManager to scene
- Phase 4: Optional configs, fallback to defaults
- Phase 5: SkillSystem API unchanged
- Phase 6: Component types unchanged

**Only Phase 3 requires scene changes**:
- Add InputManager GameObject to scene
- Can be automated via menu command

### Prefab Migration

No prefab changes needed for any phase. All refactorings maintain public APIs.

### Code Migration for Extensions

If you have custom code extending the combat system:

**Phase 1 (Skill Handlers)**:
- New skills: Create handler class implementing ISkillHandler
- Existing skill modifications: Edit specific handler instead of SkillSystem

**Phase 2 (Combat Resolution)**:
- New interactions: Create resolver class implementing IInteractionResolver
- Existing interaction modifications: Edit specific resolver

**Phase 3 (Input)**:
- New inputs: Add to InputManager
- Read input: Use InputManager.Instance instead of Input class

---

## Testing Strategy

### Per-Phase Testing

Each phase includes testing checklist. General approach:

1. **Unit Tests** (if Phase 7 complete)
   - Test extracted logic in isolation
   - Verify behavior unchanged

2. **Integration Tests**
   - Test in actual gameplay
   - All skills work
   - All interactions work
   - AI still functions

3. **Performance Tests**
   - Measure frame time before/after
   - Should be same or better
   - Profile with Unity Profiler

4. **Regression Tests**
   - Test existing scenes
   - Test existing prefabs
   - Verify no broken references

### Critical Test Cases

- [ ] All 6 skills charge and execute
- [ ] Speed conflicts resolve correctly
- [ ] Defense reduces damage
- [ ] Counter reflects damage
- [ ] Smash breaks Counter
- [ ] Windmill multi-hit works
- [ ] Ranged aiming works
- [ ] AI behavior unchanged
- [ ] Player input responsive
- [ ] Camera follows player
- [ ] Equipment modifiers apply
- [ ] Status effects apply/expire
- [ ] Knockdown meter works
- [ ] Rest regenerates stamina
- [ ] Combat targeting works

---

## Conclusion

This refactoring plan transforms the FairyGate combat system from a ~7,000 line monolithic codebase into a modular, extensible, designer-friendly architecture.

### Key Outcomes

**Code Quality**:
- Reduced LOC in largest files by ~50%
- Single-responsibility classes
- Testable components

**Extensibility**:
- Add new skills: Create handler, register
- Add new interactions: Create resolver, register
- Add new input: Add to InputManager

**Workflow**:
- Designers tune via ScriptableObjects
- No code changes for balance
- Faster iteration

**Maintainability**:
- Smaller files, easier to understand
- Explicit state transitions
- Isolated responsibilities

### Next Steps

1. **Review this plan** with team
2. **Prioritize phases** based on current pain points
3. **Set up branch** for refactoring work
4. **Implement Phase 6** (quick win)
5. **Proceed with Phases 1, 3** (high impact)
6. **Iterate and refine** based on results

---

**Questions or clarifications? Consult this document and update as refactoring progresses.**
