<objective>
Fix all CombatLogger API calls to use the correct method signatures. The previous migration used incorrect method signatures that don't exist in CombatLogger, causing compilation errors.

This is a critical hotfix to restore compilation by correcting approximately 300+ CombatLogger calls across 48 files.
</objective>

<context>
The CombatLogger migration used incorrect API calls. The actual CombatLogger API is:

**Correct API:**
```csharp
CombatLogger.Log(message, CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Info);
CombatLogger.Log(message, CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Warning);
CombatLogger.Log(message, CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Error);
```

**Incorrect (what was migrated):**
```csharp
CombatLogger.Log(CombatLogger.Category.Skills, message);  // WRONG!
CombatLogger.LogWarning(CombatLogger.Category.Skills, message);  // WRONG!
CombatLogger.LogError(CombatLogger.Category.Skills, message);  // WRONG!
```

The CombatLogger class is at `./Assets/Scripts/Combat/Utilities/CombatLogger.cs`.
</context>

<requirements>
1. **Fix All Calls**: Correct every CombatLogger method call to use the proper signature
2. **Preserve Categories**: Keep the category assignments that were determined during migration
3. **Use Correct LogLevels**: Map LogWarning → LogLevel.Warning, LogError → LogLevel.Error, Log → LogLevel.Info
4. **Fix Enum Names**: Change `Category` to `LogCategory`
5. **No Breaking Changes**: Ensure all fixes compile successfully
</requirements>

<process>
1. **Identify the Pattern Errors**:
   - All calls using `CombatLogger.Category.X` need to become `CombatLogger.LogCategory.X`
   - All calls using `CombatLogger.LogWarning(...)` need to become `CombatLogger.Log(..., LogLevel.Warning)`
   - All calls using `CombatLogger.LogError(...)` need to become `CombatLogger.Log(..., LogLevel.Error)`
   - All calls need message FIRST, then category, then level

2. **Correction Patterns**:

   **Pattern 1: Info logs**
   ```csharp
   // From:
   CombatLogger.Log(CombatLogger.Category.Skills, $"Message");
   // To:
   CombatLogger.Log($"Message", CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Info);
   ```

   **Pattern 2: Warning logs**
   ```csharp
   // From:
   CombatLogger.LogWarning(CombatLogger.Category.Skills, $"Message");
   // To:
   CombatLogger.Log($"Message", CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Warning);
   ```

   **Pattern 3: Error logs**
   ```csharp
   // From:
   CombatLogger.LogError(CombatLogger.Category.Skills, $"Message");
   // To:
   CombatLogger.Log($"Message", CombatLogger.LogCategory.Skills, CombatLogger.LogLevel.Error);
   ```

3. **Category Mapping**:
   Keep existing category assignments (the migration got these right):
   - AI files → LogCategory.AI
   - Skills files → LogCategory.Skills
   - Combat files → LogCategory.Combat
   - Movement files → LogCategory.Movement
   - UI files → LogCategory.UI
   - StatusEffects files → LogCategory.System
   - General/utilities → LogCategory.System

4. **Systematic Correction**:
   - Process files in batches for efficiency
   - Use search/replace for each incorrect pattern
   - Verify parameter order: message, category, level
   - Ensure all enum references use `LogCategory` not `Category`
   - Ensure all enum references use `LogLevel` not raw method names

5. **Verification**:
   - Search for any remaining `CombatLogger.Category` (should be zero)
   - Search for any remaining `CombatLogger.LogWarning` (should be zero)
   - Search for any remaining `CombatLogger.LogError` (should be zero)
   - Verify project compiles successfully
</process>

<implementation_guidelines>
**Parameter Order (CRITICAL):**
```csharp
CombatLogger.Log(
    message,        // 1st: The log message (string)
    category,       // 2nd: CombatLogger.LogCategory enum
    level          // 3rd: CombatLogger.LogLevel enum
);
```

**Enum Names (CRITICAL):**
- Use `CombatLogger.LogCategory.X` NOT `CombatLogger.Category.X`
- Use `CombatLogger.LogLevel.Info/Warning/Error` NOT separate methods

**What to Focus On:**
- Batch edit files with similar errors together for efficiency
- Double-check message extraction - ensure the full message string is preserved
- Verify category assignments remain correct
- Use appropriate LogLevel based on original Debug method (Log→Info, LogWarning→Warning, LogError→Error)

**What to Avoid:**
- Don't change log messages - this is API correction only
  - WHY: We want to preserve the logging information exactly
- Don't change category assignments
  - WHY: The categories were correctly determined during initial migration
- Don't skip any files
  - WHY: Project must compile - even one error blocks everything
</implementation_guidelines>

<output>
Fix CombatLogger calls in all files under:
- `./Assets/Scripts/Combat/AI/**/*.cs`
- `./Assets/Scripts/Combat/Core/**/*.cs`
- `./Assets/Scripts/Combat/Skills/**/*.cs`
- `./Assets/Scripts/Combat/UI/**/*.cs`
- `./Assets/Scripts/Combat/StatusEffects/**/*.cs`
- `./Assets/Scripts/Combat/Weapons/**/*.cs`
- `./Assets/Scripts/Combat/Systems/**/*.cs`
- `./Assets/Scripts/Combat/Equipment/**/*.cs`
- `./Assets/Scripts/Combat/Utilities/**/*.cs`

For each file with CombatLogger calls:
- Correct the method signature to use `Log(message, category, level)`
- Change `Category` to `LogCategory`
- Map LogWarning/LogError to appropriate LogLevel
- Preserve exact message content and category assignments
</output>

<verification>
Before declaring complete, verify:

1. **Zero Compilation Errors:**
   - Project compiles successfully
   - No CombatLogger-related errors

2. **Pattern Verification:**
   ```bash
   # Should find ZERO results:
   grep -r "CombatLogger\.Category\." Assets/Scripts/Combat --include="*.cs"
   grep -r "CombatLogger\.LogWarning" Assets/Scripts/Combat --include="*.cs"
   grep -r "CombatLogger\.LogError" Assets/Scripts/Combat --include="*.cs"

   # Should find 300+ results (all the corrected calls):
   grep -r "CombatLogger\.Log(" Assets/Scripts/Combat --include="*.cs" | wc -l
   ```

3. **Spot Check:**
   - Open 3-5 random files and verify calls look correct
   - Verify message is first parameter, category second, level third
   - Verify LogCategory and LogLevel enums used correctly

4. **No Regressions:**
   - All messages preserved
   - All categories preserved
   - No syntax errors
</verification>

<success_criteria>
- **Zero compilation errors** - project builds successfully
- **All CombatLogger calls use correct API** - Log(message, category, level)
- **All enum references correct** - LogCategory and LogLevel, not Category
- **Zero incorrect patterns remain** - no LogWarning/LogError methods, no Category enum
- Codebase compiles and is ready for testing
</success_criteria>

<parallel_execution>
When fixing multiple files, batch Edit operations together for maximum efficiency. The fixes are independent and can be applied in parallel.
</parallel_execution>

<critical_note>
This is a HOTFIX to restore compilation. Speed is important, but correctness is critical. Every single CombatLogger call must be fixed correctly or the project won't compile.
</critical_note>
