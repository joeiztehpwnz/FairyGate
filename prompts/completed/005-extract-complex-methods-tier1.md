<objective>
Extract complex methods in CombatInteractionManager and MovementController to reduce complexity and improve maintainability. This follows the successful pattern from PatternExecutor.cs refactoring which achieved 75-78% complexity reduction.

This is Tier 1 of the method extraction refactoring plan, focusing on the two highest-impact methods with the most complexity.
</objective>

<context>
The PatternExecutor.cs refactoring demonstrated the value of method extraction:
- Update() reduced from 127 lines to 27 lines (78% reduction)
- UpdateEvaluationContext() reduced from 75 lines to 3 lines + helpers (96% reduction)
- Created 10 focused helper methods
- Dramatically improved readability and testability

We're applying the same approach to two critical methods:
1. `CombatInteractionManager.ExecuteOffensiveSkillDirectly()` - 99 lines → 20 lines
2. `MovementController.UpdateMovement()` - 104 lines → 25 lines

Both files are located in `./Assets/Scripts/Combat/Core/`.
</context>

<requirements>
1. **Preserve Functionality**: All existing behavior must work exactly as before
2. **Reduce Complexity**: Target 75% reduction in line count for main methods
3. **Single Responsibility**: Each extracted method should do one thing well
4. **Clear Naming**: Use intention-revealing names that make code self-documenting
5. **No Breaking Changes**: Preserve all public APIs and Unity serialization
</requirements>

<process>
## Part 1: CombatInteractionManager.ExecuteOffensiveSkillDirectly()

**Current State:** 99 lines (Lines 437-535), Very High Complexity

**Target Extractions:**

1. **ValidateOffensiveExecution()** → Returns bool
   - Extract death checks and early returns
   - Check if attacker/target are alive
   - Return true if execution should proceed

2. **GetTargetComponents()** → Returns component bundle or null
   - Extract component gathering (HealthSystem, StatusEffectManager, etc.)
   - Validate components exist
   - Return structured data or null if invalid

3. **ApplySkillDamage()** → void
   - Extract damage calculation
   - Extract hit detection for ranged attacks
   - Apply damage to target

4. **ApplySkillEffects()** → void
   - Extract status effect application
   - Extract knockdown meter logic
   - Handle skill-specific effects

5. **HandleRangedAttackMiss()** → void
   - Extract ranged attack miss logic
   - Clean separation of ranged-specific behavior

6. **HandleSpecialSkillExecution()** → Returns bool
   - Check for windmill and route to ExecuteWindmillAoE
   - Return true if special handling occurred

**Result:** Main method becomes ~20 lines that orchestrates these helpers

## Part 2: MovementController.UpdateMovement()

**Current State:** 104 lines (Lines 108-211), Very High Complexity

**Target Extractions:**

1. **ResolveMovementDirection()** → Returns Vector3
   - Main orchestrator for gathering all movement input
   - Returns final movement direction vector
   - Calls other helpers to build result

2. **GetPlayerInputDirection()** → Returns Vector3
   - Extract player keyboard input handling
   - Return normalized direction from WASD

3. **GetAIInputDirection()** → Returns Vector3
   - Extract AI movement input
   - Return direction from movement arbitrator or aiMovementInput

4. **TransformToCameraRelative()** → Returns Vector3
   - Extract camera transformation logic
   - Takes input direction and camera, returns camera-relative direction

5. **ApplyMovementPhysics()** → void
   - Extract gravity application
   - Extract CharacterController.Move call
   - Handle ground detection

6. **ShouldBlockMovement()** → Returns bool
   - Extract early return conditions
   - Check canMove flag
   - Return true if movement should be blocked

**Result:** Main method becomes ~25 lines that orchestrates movement flow

## Refactoring Steps

1. **Read the Files**:
   - Read CombatInteractionManager.cs fully
   - Read MovementController.cs fully
   - Understand the current logic flow

2. **Plan Extractions**:
   - Identify exact line ranges for each extraction
   - Plan method signatures and return types
   - Ensure no overlapping extractions

3. **Extract One Method at a Time**:
   - Start with simplest extraction (validation/early returns)
   - Test after each extraction by reviewing the code
   - Move to next extraction

4. **Refactor Main Methods**:
   - Update main method to call extracted helpers
   - Simplify to orchestration logic only
   - Add comments if flow needs clarification

5. **Verify**:
   - Check all functionality preserved
   - Ensure no syntax errors
   - Verify code is more readable
</process>

<implementation_guidelines>
**Method Extraction Pattern:**

1. **Identify cohesive blocks** of code that do one thing
2. **Create descriptive method names** that reveal intent
3. **Minimize parameters** - pass only what's needed
4. **Use early returns** to reduce nesting
5. **Keep helpers private** unless needed elsewhere

**What to Focus On:**
- Clear separation of concerns
- Reducing nesting depth
- Making code self-documenting through names
- Preserving all edge cases and special handling

**What to Avoid:**
- Don't change behavior - refactoring only
  - WHY: We need to verify correctness, behavior changes make that impossible
- Don't over-extract - keep helpers at appropriate abstraction level
  - WHY: Too many tiny methods can be as hard to follow as one giant method
- Don't break Unity serialization or public APIs
  - WHY: Would break existing references and cause runtime errors
- Don't extract to separate classes yet - keep in same file
  - WHY: Focus on simplification first, architectural changes can come later

**Example Extraction Pattern:**

Before:
```csharp
public void BigMethod()
{
    // Validation
    if (foo == null) return;
    if (!bar) return;

    // Do thing A (10 lines)
    // ...

    // Do thing B (15 lines)
    // ...

    // Do thing C (20 lines)
    // ...
}
```

After:
```csharp
public void BigMethod()
{
    if (!ValidateMethod()) return;

    DoThingA();
    DoThingB();
    DoThingC();
}

private bool ValidateMethod()
{
    if (foo == null) return false;
    if (!bar) return false;
    return true;
}

private void DoThingA() { /* 10 lines */ }
private void DoThingB() { /* 15 lines */ }
private void DoThingC() { /* 20 lines */ }
```
</implementation_guidelines>

<output>
Refactor two files:
- `./Assets/Scripts/Combat/Core/CombatInteractionManager.cs`
- `./Assets/Scripts/Combat/Core/MovementController.cs`

For CombatInteractionManager:
- Extract 5-6 helper methods from ExecuteOffensiveSkillDirectly
- Reduce main method from 99 lines to ~20 lines

For MovementController:
- Extract 5-6 helper methods from UpdateMovement
- Reduce main method from 104 lines to ~25 lines
</output>

<verification>
Before declaring complete, verify:

1. **Functionality Preserved**:
   - Review refactored code to ensure logic remains equivalent
   - Check all edge cases and special handling maintained
   - Verify Unity lifecycle methods unchanged

2. **Complexity Reduced**:
   - CombatInteractionManager.ExecuteOffensiveSkillDirectly: 99 → ~20 lines
   - MovementController.UpdateMovement: 104 → ~25 lines
   - New helper methods are focused and single-purpose

3. **Code Quality Improved**:
   - Methods are shorter and easier to understand
   - Naming is clear and intention-revealing
   - Nesting depth reduced
   - Code is more testable

4. **No Regressions**:
   - No syntax errors
   - Public APIs unchanged
   - Unity serialization preserved
   - All logging and debug code maintained
</verification>

<success_criteria>
- **ExecuteOffensiveSkillDirectly()**: 99 lines → 20-25 lines (75-80% reduction)
- **UpdateMovement()**: 104 lines → 25-30 lines (75% reduction)
- 10-12 new focused helper methods created
- Code is significantly more readable and maintainable
- No functionality broken or changed
- Follows same successful pattern as PatternExecutor.cs refactoring
</success_criteria>

<parallel_execution>
When reading files to understand current structure, you can read both files in parallel. However, refactor one file at a time to minimize risk and allow for careful verification between steps.
</parallel_execution>
