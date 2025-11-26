<objective>
Continue method extraction refactoring with Tier 2 targets: MovementController.GetSkillMovementModifier() and CombatInteractionManager.ExecuteWindmillAoE(). This builds on the successful Tier 1 refactoring that achieved 72-88% complexity reduction.

Focus on converting switch-heavy logic to data-driven approaches and consolidating duplicate damage/effect logic.
</objective>

<context>
**Tier 1 Success:**
- CombatInteractionManager.ExecuteOffensiveSkillDirectly(): 99 → 28 lines (72% reduction)
- MovementController.UpdateMovement(): 104 → 12 lines (88% reduction)
- Created 13 focused helper methods
- Established helper methods that can be reused in Tier 2

**Tier 2 Targets:**
1. `MovementController.GetSkillMovementModifier()` - 57 lines → 10 lines (data-driven approach)
2. `CombatInteractionManager.ExecuteWindmillAoE()` - 70 lines → 25 lines (reuse existing helpers)

Both files are located in `./Assets/Scripts/Combat/Core/`.
</context>

<requirements>
1. **Preserve Functionality**: All existing behavior must work exactly as before
2. **Reduce Complexity**: Target 60-80% reduction in line count
3. **Reuse Existing Helpers**: Leverage helpers created in Tier 1 where applicable
4. **Data-Driven Design**: Convert large switch statements to table lookups where appropriate
5. **No Breaking Changes**: Preserve all public APIs and Unity serialization
</requirements>

<process>
## Part 1: MovementController.GetSkillMovementModifier()

**Current State:** 57 lines with giant switch statement (15+ decision points)

**Problem:** Large switch statement with nested conditionals and repeated patterns for similar skills (Defense/Counter both check charging state the same way).

**Solution: Data-Driven Approach**

**Step 1: Create SkillMovementModifierData structure**
```csharp
private struct SkillMovementModifierData
{
    public float chargingModifier;
    public float executionModifier;  // Startup/Active/Recovery
    public bool checkChargingState;  // True for skills that have charging logic
}
```

**Step 2: Create static lookup table**
```csharp
private static readonly Dictionary<SkillType, SkillMovementModifierData> skillModifiers =
    new Dictionary<SkillType, SkillMovementModifierData>
{
    { SkillType.Attack, new SkillMovementModifierData { chargingModifier = 1.0f, executionModifier = 0f, checkChargingState = true } },
    { SkillType.Smash, new SkillMovementModifierData { chargingModifier = 0.3f, executionModifier = 0f, checkChargingState = true } },
    { SkillType.Defense, new SkillMovementModifierData { chargingModifier = 1.0f, executionModifier = 0f, checkChargingState = true } },
    { SkillType.Counterattack, new SkillMovementModifierData { chargingModifier = 1.0f, executionModifier = 0f, checkChargingState = true } },
    { SkillType.Windmill, new SkillMovementModifierData { chargingModifier = 1.0f, executionModifier = 0f, checkChargingState = true } },
    { SkillType.Ranged, new SkillMovementModifierData { chargingModifier = 0f, executionModifier = 0f, checkChargingState = false } }
};
```

**Step 3: Simplified GetSkillMovementModifier()**
```csharp
private float GetSkillMovementModifier()
{
    if (skillSystem == null || skillSystem.CurrentSkillType == SkillType.None)
        return 1.0f;

    SkillType skillType = skillSystem.CurrentSkillType;

    if (!skillModifiers.TryGetValue(skillType, out SkillMovementModifierData data))
        return 1.0f;

    return GetModifierForSkillState(data);
}

private float GetModifierForSkillState(SkillMovementModifierData data)
{
    SkillExecutionState state = skillSystem.ExecutionState;

    // Execution states (Startup/Active/Recovery) always return execution modifier
    if (state == SkillExecutionState.Startup ||
        state == SkillExecutionState.Active ||
        state == SkillExecutionState.Recovery)
    {
        return data.executionModifier;
    }

    // Charging state handling
    if (data.checkChargingState &&
        (state == SkillExecutionState.Charging || state == SkillExecutionState.Charged))
    {
        return data.chargingModifier;
    }

    return 1.0f; // Default for other states
}
```

**Result:** 57 lines → ~15 lines total (73% reduction) + data table

**Benefits:**
- Easy to add new skills - just add table entry
- No duplicate logic for similar skills
- Clear separation of data and logic
- More maintainable

---

## Part 2: CombatInteractionManager.ExecuteWindmillAoE()

**Current State:** 70 lines with loop over targets and duplicated damage/effect logic

**Problem:** Similar to ExecuteOffensiveSkillDirectly but for multiple targets. Contains duplicated component gathering, damage application, and effect application logic.

**Solution: Reuse Tier 1 Helper Methods**

The Tier 1 refactoring created these reusable helpers:
- `GetTargetComponents(target)` - Already handles component gathering and validation
- `ApplySkillDamage(execution, target, components, stats, weapon)` - Can be reused
- `ApplySkillEffects(execution, target, components, stats, weapon, damage)` - Can be reused

**Target Extractions:**

1. **GetWindmillTargets()** → Returns List<GameObject>
   - Extract target finding and filtering logic
   - Filter out dead targets
   - Filter out self
   - Return valid target list

2. **ExecuteWindmillOnTarget(execution, target, attackerStats, weapon)** → void
   - Execute windmill on single target using existing helpers
   - Calls GetTargetComponents, ApplySkillDamage, ApplySkillEffects
   - Encapsulates single-target logic

**Refactored ExecuteWindmillAoE():**
```csharp
private void ExecuteWindmillAoE(SkillExecution execution)
{
    CharacterStats attackerStats = execution.executor.GetComponent<CharacterStats>();
    WeaponController weapon = execution.executor.GetComponent<WeaponController>();

    List<GameObject> targets = GetWindmillTargets(execution);

    foreach (GameObject target in targets)
    {
        ExecuteWindmillOnTarget(execution, target, attackerStats, weapon);
    }

    NotifyTrackerWindmillExecuted(execution);
}
```

**Result:** 70 lines → ~25 lines main method + 2 focused helpers (65% reduction)

**Benefits:**
- Eliminates duplication with ExecuteOffensiveSkillDirectly
- Reuses proven helper methods from Tier 1
- Clear separation: find targets → execute on each → notify
- Much easier to test

---

## Refactoring Steps

1. **Part 1: MovementController.GetSkillMovementModifier()**
   - Read current implementation
   - Create SkillMovementModifierData structure
   - Build lookup table with all skill data
   - Replace switch with table lookup
   - Extract GetModifierForSkillState helper
   - Verify all skills still behave correctly

2. **Part 2: CombatInteractionManager.ExecuteWindmillAoE()**
   - Read current implementation
   - Identify which Tier 1 helpers can be reused
   - Extract GetWindmillTargets()
   - Extract ExecuteWindmillOnTarget()
   - Refactor main method to orchestrate
   - Verify damage/effects still apply correctly

3. **Verification**:
   - Check all functionality preserved
   - Ensure no syntax errors
   - Verify code is more readable
   - Confirm helpers are properly reused
</process>

<implementation_guidelines>
**Data-Driven Design Pattern:**

1. **Identify repeated patterns** in switch statements
2. **Extract data** from code into structures/tables
3. **Create lookup mechanism** (dictionary, array, etc.)
4. **Simplify logic** to just lookup + simple conditionals
5. **Document data** so future developers understand table entries

**Helper Reuse Pattern:**

1. **Identify duplicate logic** between methods
2. **Check if existing helpers** can be used
3. **Adapt if needed** but preserve existing helper signatures
4. **Call helpers** instead of duplicating code
5. **Test** that behavior is identical

**What to Focus On:**
- Converting procedural code to data-driven
- Eliminating code duplication through reuse
- Maintaining clear, readable code flow
- Making code easier to extend in the future

**What to Avoid:**
- Don't change behavior - refactoring only
  - WHY: We need to verify correctness
- Don't over-complicate the data structures
  - WHY: Simple is better than clever
- Don't break existing helper method signatures
  - WHY: Would require updating all call sites
- Don't skip verification steps
  - WHY: Small bugs can compound
</implementation_guidelines>

<output>
Refactor two methods in two files:

**File 1: `./Assets/Scripts/Combat/Core/MovementController.cs`**
- Create SkillMovementModifierData structure
- Create static skill modifiers lookup table
- Refactor GetSkillMovementModifier() from 57 → ~15 lines
- Extract GetModifierForSkillState() helper

**File 2: `./Assets/Scripts/Combat/Core/CombatInteractionManager.cs`**
- Extract GetWindmillTargets() helper
- Extract ExecuteWindmillOnTarget() helper
- Refactor ExecuteWindmillAoE() from 70 → ~25 lines
- Reuse Tier 1 helpers: GetTargetComponents, ApplySkillDamage, ApplySkillEffects
</output>

<verification>
Before declaring complete, verify:

1. **Functionality Preserved**:
   - All skills have correct movement modifiers
   - Windmill hits all valid targets with correct damage/effects
   - Edge cases handled identically

2. **Complexity Reduced**:
   - GetSkillMovementModifier: 57 → ~15 lines (73% reduction)
   - ExecuteWindmillAoE: 70 → ~25 lines (65% reduction)
   - Helper methods are focused and reusable

3. **Code Quality Improved**:
   - Data-driven approach is clearer and more maintainable
   - No code duplication between ExecuteOffensiveSkillDirectly and ExecuteWindmillAoE
   - Easy to add new skills or modify behavior

4. **No Regressions**:
   - No syntax errors
   - All logging preserved
   - Unity serialization intact
   - Existing Tier 1 helpers work with new code
</verification>

<success_criteria>
- **GetSkillMovementModifier()**: 57 lines → 15 lines (73% reduction)
- **ExecuteWindmillAoE()**: 70 lines → 25 lines (65% reduction)
- Data-driven skill modifier table created and working
- Tier 1 helpers successfully reused in Tier 2
- No functionality broken or changed
- Code is more maintainable and extensible
</success_criteria>

<parallel_execution>
When reading files to understand current structure, you can read both in parallel. Refactor one method at a time for careful verification.
</parallel_execution>
