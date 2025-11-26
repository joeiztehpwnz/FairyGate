<objective>
Complete the migration of all Debug.Log calls to the professional CombatLogger system across the entire FairyGate codebase. This will provide consistent, filterable, color-coded logging with zero runtime cost in production builds.

The initial refactoring migrated 45 Debug.Log calls in 2 files (PatternExecutor.cs and SkillSystem.cs). Approximately 268 Debug.Log calls remain across the codebase that need to be migrated.
</objective>

<context>
The FairyGate project has a professional CombatLogger system already in place at `./Assets/Scripts/Combat/Utilities/CombatLogger.cs`. This system provides:

- **Color-coded output** for different log categories
- **Category filtering** to focus on specific systems
- **Zero runtime cost** in production builds (compiled out)
- **Consistent formatting** across all logging

The pattern has been established in PatternExecutor.cs and SkillSystem.cs - we need to apply this same pattern throughout the entire codebase.
</context>

<requirements>
1. **Complete Migration**: Find and migrate ALL remaining Debug.Log, Debug.LogWarning, and Debug.LogError calls
2. **Correct Categories**: Use appropriate CombatLogger categories based on the file/system being logged
3. **Preserve Information**: Maintain all logging information - don't lose any debug context
4. **Consistent Style**: Follow the exact pattern established in the initial migration
5. **No Breaking Changes**: Ensure all logging still works correctly after migration
</requirements>

<process>
1. **Identify All Debug.Log Calls**:
   - Search the entire `./Assets/Scripts/Combat/` directory for Debug.Log, Debug.LogWarning, Debug.LogError
   - Catalog which files have logging calls and how many
   - Prioritize high-value files (core systems, AI, skills, combat)

2. **Determine Appropriate Categories**:
   - AI files → `CombatLogger.Category.AI`
   - Skill files → `CombatLogger.Category.Skills`
   - Combat/interaction files → `CombatLogger.Category.Combat`
   - Movement files → `CombatLogger.Category.Movement`
   - Status effects → `CombatLogger.Category.StatusEffects`
   - UI files → `CombatLogger.Category.UI`
   - General/utilities → `CombatLogger.Category.General`

3. **Migration Pattern**:

   **Before:**
   ```csharp
   Debug.Log($"Some message with {variable}");
   Debug.LogWarning("Warning message");
   Debug.LogError("Error message");
   ```

   **After:**
   ```csharp
   CombatLogger.Log(CombatLogger.Category.Skills, $"Some message with {variable}");
   CombatLogger.LogWarning(CombatLogger.Category.Skills, "Warning message");
   CombatLogger.LogError(CombatLogger.Category.Skills, "Error message");
   ```

4. **Systematic Migration**:
   - Process files in logical groups (AI system, Skills system, Core combat, etc.)
   - Migrate all calls in a file before moving to the next
   - Use batch editing when multiple files are ready
   - Verify each file after migration

5. **Verification**:
   - Ensure no Debug.Log calls remain in the Combat scripts directory
   - Verify all migrated calls use correct categories
   - Check that logging information is preserved
</process>

<implementation_guidelines>
**Category Selection Rules**:
- Choose category based on the **primary responsibility** of the file
- AI pattern files, coordinators, agents → AI
- Skill states, skill system, skill execution → Skills
- Combat interactions, combat managers → Combat
- Movement controllers, arbitrators → Movement
- Status effect manager → StatusEffects
- UI displays, bars, outlines → UI
- Utilities, loggers, validators → General

**What to Focus On**:
- High-impact files first (core systems, frequently-used components)
- Maintain exact same logging messages and context
- Use appropriate log levels (Log vs LogWarning vs LogError)
- Group similar files together for efficient batch editing

**What to Avoid**:
- Don't change logging messages or information - this is a migration only
  - WHY: Changing messages makes it hard to verify the migration is correct
- Don't skip files - this should be comprehensive
  - WHY: Inconsistent logging makes debugging harder
- Don't use wrong categories
  - WHY: Incorrect categories defeat the purpose of filterable logging
</implementation_guidelines>

<output>
Migrate Debug.Log calls in all files under:
- `./Assets/Scripts/Combat/AI/**/*.cs`
- `./Assets/Scripts/Combat/Core/**/*.cs`
- `./Assets/Scripts/Combat/Skills/**/*.cs`
- `./Assets/Scripts/Combat/UI/**/*.cs`
- `./Assets/Scripts/Combat/StatusEffects/**/*.cs`
- `./Assets/Scripts/Combat/Weapons/**/*.cs`
- `./Assets/Scripts/Combat/Utilities/**/*.cs`
- `./Assets/Scripts/Editor/**/*.cs` (if they have Debug.Log calls)

For each file:
- Replace all Debug.Log/LogWarning/LogError calls with CombatLogger equivalents
- Use the appropriate category for that file's system
- Preserve all original logging information
</output>

<verification>
Before declaring complete, verify:

1. **Complete Migration**:
   - Run a search for "Debug.Log" in ./Assets/Scripts/Combat/ - should find ZERO results
   - Run a search for "Debug.LogWarning" in ./Assets/Scripts/Combat/ - should find ZERO results
   - Run a search for "Debug.LogError" in ./Assets/Scripts/Combat/ - should find ZERO results

2. **Correct Implementation**:
   - All CombatLogger calls use appropriate categories
   - All logging messages are preserved exactly
   - Log levels are appropriate (Log/LogWarning/LogError)

3. **No Regressions**:
   - No syntax errors introduced
   - All files compile successfully
   - Logging functionality still works

4. **Documentation**:
   - Update REFACTORING_SUMMARY.md with migration statistics
   - List all files migrated and their log call counts
</verification>

<success_criteria>
- **100% of Debug.Log calls migrated** to CombatLogger in Combat scripts directory
- All categories assigned correctly based on file responsibility
- All original logging information preserved
- Zero Debug.Log/LogWarning/LogError calls remain in ./Assets/Scripts/Combat/
- Migration statistics documented in REFACTORING_SUMMARY.md
- Codebase has consistent, professional logging throughout
</success_criteria>

<parallel_execution>
When reading multiple files to assess logging calls, invoke Read tools simultaneously. When migrating multiple files that don't conflict, batch Edit operations together for efficiency.
</parallel_execution>
