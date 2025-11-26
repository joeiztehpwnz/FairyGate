<objective>
Standardize all CombatLogger calls to use the cleaner convenience methods instead of the verbose base Log() method. This will improve code readability and consistency across the entire codebase.

Currently 46 files use the verbose API while 2 files already use the cleaner convenience methods. This task brings all 46 files in line with the better pattern.
</objective>

<context>
The CombatLogger system provides two valid APIs:

**Verbose (current - what needs to change):**
```csharp
CombatLogger.Log("message", CombatLogger.LogCategory.AI, CombatLogger.LogLevel.Info);
CombatLogger.Log("warning", CombatLogger.LogCategory.Combat, CombatLogger.LogLevel.Warning);
CombatLogger.Log("error", CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Error, this);
```

**Convenience methods (target - cleaner and more readable):**
```csharp
CombatLogger.LogAI("message");
CombatLogger.LogCombat("warning", CombatLogger.LogLevel.Warning);
CombatLogger.LogSkill("error", CombatLogger.LogLevel.Error, this);
```

The convenience methods are located in `./Assets/Scripts/Combat/Utilities/CombatLogger.cs` and include:
- `LogAI(message, level = Info, context = null)`
- `LogCombat(message, level = Info, context = null)`
- `LogSkill(message, level = Info, context = null)`
- `LogMovement(message, level = Info, context = null)`
- `LogWeapon(message, level = Info, context = null)`
- `LogHealth(message, level = Info, context = null)`
- `LogStamina(message, level = Info, context = null)`
- `LogUI(message, level = Info, context = null)`
- `LogSystem(message, level = Info, context = null)`
- `LogPattern(message, level = Info, context = null)`
- `LogFormation(message, level = Info, context = null)`
- `LogAttack(message, level = Info, context = null)`

Files that already use convenience methods correctly (don't change these):
- `PatternExecutor.cs` (21 calls)
- `SkillSystem.cs` (24 calls)
</context>

<requirements>
1. **Convert All Verbose Calls**: Replace all verbose `Log()` calls with appropriate convenience methods
2. **Preserve All Information**: Keep exact message content, log levels, and context parameters
3. **Use Correct Mapping**: Match LogCategory to the corresponding convenience method
4. **Handle All Log Levels**: Info, Warning, Error, Debug
5. **Preserve Context Parameters**: Keep `this` or other context objects where used
</requirements>

<process>
1. **Identify Conversion Patterns**:

   **Pattern 1: Info level (most common) - omit level parameter**
   ```csharp
   // From:
   CombatLogger.Log("message", CombatLogger.LogCategory.AI, CombatLogger.LogLevel.Info);
   // To:
   CombatLogger.LogAI("message");
   ```

   **Pattern 2: Warning/Error level - include level parameter**
   ```csharp
   // From:
   CombatLogger.Log("warning", CombatLogger.LogCategory.Combat, CombatLogger.LogLevel.Warning);
   // To:
   CombatLogger.LogCombat("warning", CombatLogger.LogLevel.Warning);

   // From:
   CombatLogger.Log("error", CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Error);
   // To:
   CombatLogger.LogSkill("error", CombatLogger.LogLevel.Error);
   ```

   **Pattern 3: With context parameter**
   ```csharp
   // From:
   CombatLogger.Log("message", CombatLogger.LogCategory.AI, CombatLogger.LogLevel.Info, this);
   // To:
   CombatLogger.LogAI("message", CombatLogger.LogLevel.Info, this);

   // From:
   CombatLogger.Log("error", CombatLogger.LogCategory.Combat, CombatLogger.LogLevel.Error, this);
   // To:
   CombatLogger.LogCombat("error", CombatLogger.LogLevel.Error, this);
   ```

2. **Category to Method Mapping**:
   - `LogCategory.AI` → `LogAI()`
   - `LogCategory.Combat` → `LogCombat()`
   - `LogCategory.Skills` → `LogSkill()`
   - `LogCategory.Movement` → `LogMovement()`
   - `LogCategory.Weapons` → `LogWeapon()`
   - `LogCategory.Health` → `LogHealth()`
   - `LogCategory.Stamina` → `LogStamina()`
   - `LogCategory.UI` → `LogUI()`
   - `LogCategory.System` → `LogSystem()`
   - `LogCategory.Pattern` → `LogPattern()`
   - `LogCategory.Formation` → `LogFormation()`
   - `LogCategory.Attack` → `LogAttack()`

3. **Systematic Conversion**:
   - Process files by category for consistency
   - Convert all calls in a file before moving to next
   - Handle each log level appropriately
   - Preserve all parameters correctly

4. **Special Cases to Handle**:
   - **Info + no context**: Omit both level and context parameters (cleanest)
   - **Info + context**: Keep level parameter to include context
   - **Non-Info level**: Always include level parameter
   - **Debug level**: Less common, but handle same as Warning/Error

5. **Verification**:
   - Search for remaining `CombatLogger.Log(` calls (should only be in CombatLogger.cs itself)
   - Verify all convenience method calls are syntactically correct
   - Ensure no information was lost
</process>

<implementation_guidelines>
**Parameter Rules (CRITICAL):**

1. **Info level, no context** (most common):
   ```csharp
   LogAI("message")  // Cleanest - both defaults used
   ```

2. **Non-Info level, no context**:
   ```csharp
   LogAI("message", CombatLogger.LogLevel.Warning)
   LogAI("message", CombatLogger.LogLevel.Error)
   ```

3. **Info level, with context**:
   ```csharp
   LogAI("message", CombatLogger.LogLevel.Info, this)  // Must include level to pass context
   ```

4. **Non-Info level, with context**:
   ```csharp
   LogAI("message", CombatLogger.LogLevel.Warning, this)
   LogAI("message", CombatLogger.LogLevel.Error, this)
   ```

**What to Focus On**:
- Batch process files by category for efficiency
- Use exact message strings - don't modify content
- Simplify by omitting default parameters when possible
- Double-check category to method mapping

**What to Avoid**:
- Don't change log messages - this is API conversion only
  - WHY: We're standardizing the API, not changing what gets logged
- Don't skip PatternExecutor.cs and SkillSystem.cs - they're already correct
  - WHY: These 2 files already use convenience methods from the first refactoring
- Don't include level parameter when it's Info and there's no context
  - WHY: Defaults make code cleaner - only be explicit when needed
</implementation_guidelines>

<output>
Convert CombatLogger calls in all files under:
- `./Assets/Scripts/Combat/AI/**/*.cs` (except PatternExecutor.cs - already done)
- `./Assets/Scripts/Combat/Core/**/*.cs`
- `./Assets/Scripts/Combat/Skills/**/*.cs` (except SkillSystem.cs - already done)
- `./Assets/Scripts/Combat/UI/**/*.cs`
- `./Assets/Scripts/Combat/StatusEffects/**/*.cs`
- `./Assets/Scripts/Combat/Weapons/**/*.cs`
- `./Assets/Scripts/Combat/Systems/**/*.cs`
- `./Assets/Scripts/Combat/Equipment/**/*.cs`

For each file with verbose CombatLogger.Log() calls:
- Replace with appropriate convenience method
- Simplify parameters (omit defaults when possible)
- Preserve exact message content and context
</output>

<verification>
Before declaring complete, verify:

1. **Complete Conversion:**
   ```bash
   # Should find ONLY the base Log() method definition in CombatLogger.cs
   # All usage sites should be converted
   grep -r "CombatLogger\.Log(" Assets/Scripts/Combat --include="*.cs" | grep -v "CombatLogger.cs"
   # Should return zero results (or very few false positives like comments)
   ```

2. **Correct Usage:**
   - Spot-check 5-10 files to verify conversions are correct
   - Verify message content unchanged
   - Verify log levels preserved
   - Verify context parameters preserved where they existed

3. **No Regressions:**
   - No syntax errors
   - Project compiles successfully
   - All logging information preserved

4. **Consistency:**
   - All files now use the same convenience method pattern
   - Code is more readable and maintainable
</verification>

<success_criteria>
- **Zero verbose CombatLogger.Log() calls remain** (except in CombatLogger.cs definition)
- **All files use convenience methods** - LogAI(), LogCombat(), etc.
- **Parameters simplified** where possible (omit defaults)
- **All information preserved** - messages, levels, context
- **Project compiles successfully**
- **Codebase has consistent, clean logging API usage**
</success_criteria>

<parallel_execution>
When converting multiple files, batch Edit operations together for maximum efficiency. The conversions are independent and can be applied in parallel.
</parallel_execution>

<benefits>
This standardization will result in:
- **More readable code** - `LogAI("msg")` vs `Log("msg", LogCategory.AI, LogLevel.Info)`
- **Consistent style** - All 48 files using same pattern
- **Less verbosity** - Shorter, cleaner lines
- **Better maintainability** - Clearer intent at a glance
- **Easier to write** - Simpler API for developers
</benefits>
