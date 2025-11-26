<objective>
Refactor the entire FairyGate game codebase to improve code quality and reduce complexity. The goal is to make the code simpler, more maintainable, and easier to understand while preserving all existing functionality.

This refactoring will make the codebase easier to extend with new features and reduce the cognitive load for developers working on the project.
</objective>

<context>
This is a Unity game project (FairyGate) with a combat system including:
- AI systems with pattern-based behavior
- Combat mechanics (skills, weapons, status effects)
- Character systems (health, stamina, knockdown)
- UI systems (health bars, status displays, skill icons)
- Movement and interaction systems

The codebase has accumulated complexity over time and would benefit from systematic simplification while maintaining all current functionality.

Before refactoring, read the CLAUDE.md file (if it exists) to understand project conventions and coding standards.
</context>

<requirements>
1. **Preserve Functionality**: All existing game features must continue to work exactly as before
2. **Reduce Complexity**: Identify and simplify overly complex code patterns
3. **Improve Readability**: Make code easier to understand at a glance
4. **Eliminate Redundancy**: Remove duplicate code and consolidate similar patterns
5. **Consistent Patterns**: Ensure similar problems are solved in similar ways throughout the codebase
6. **Better Organization**: Improve file structure and class organization where beneficial
</requirements>

<process>
1. **Analyze the Codebase**:
   - Thoroughly examine the entire codebase structure
   - Identify complexity hotspots (long methods, deep nesting, unclear logic)
   - Look for code duplication and inconsistent patterns
   - Understand dependencies between systems
   - Pay special attention to files in the git status that were recently modified

2. **Identify Refactoring Opportunities**:
   - Methods/classes that are too long or do too much
   - Unclear naming or confusing abstractions
   - Tight coupling between components
   - Missing or poor separation of concerns
   - Overly complex conditional logic
   - Dead or commented-out code

3. **Create a Refactoring Strategy**:
   - Prioritize high-impact, low-risk improvements
   - Group related refactorings together
   - Consider dependencies and order of operations
   - Plan to refactor incrementally, not all at once

4. **Execute Refactorings Systematically**:
   - Start with the highest-impact simplifications
   - Make one logical change at a time
   - Extract complex methods into smaller, well-named methods
   - Simplify conditional logic where possible
   - Consolidate duplicate code
   - Improve naming for clarity
   - Remove unused code and imports

5. **Verify Continuously**:
   - After each refactoring, verify the code still makes sense
   - Check that interfaces and public APIs remain stable
   - Ensure Unity-specific patterns (MonoBehaviour lifecycle, etc.) are preserved
</process>

<implementation_guidelines>
**What to Focus On**:
- Extract method: Break down long methods into smaller, single-purpose methods
- Simplify conditionals: Replace complex if-else chains with clearer patterns
- Improve naming: Use descriptive names that reveal intent
- Reduce nesting: Flatten deeply nested code with early returns or extracted methods
- Consolidate duplication: Create shared utilities or base classes for repeated patterns
- Organize responsibilities: Ensure each class has a clear, focused purpose

**What to Avoid**:
- Don't over-engineer: Simpler is better than more abstract
  - WHY: Adding unnecessary abstractions increases complexity rather than reducing it
- Don't change functionality: This is refactoring only, not feature work
  - WHY: Mixing refactoring with behavior changes makes it impossible to verify correctness
- Don't break Unity conventions: Respect MonoBehaviour patterns, serialization, etc.
  - WHY: Unity has specific requirements for how components work; breaking these causes runtime errors
- Don't refactor everything at once: Focus on high-impact areas
  - WHY: Attempting to refactor everything simultaneously increases risk and makes verification difficult

**Unity-Specific Considerations**:
- Preserve serialized field names and types (or Unity will lose data)
- Maintain MonoBehaviour lifecycle method signatures
- Keep component references and dependencies working
- Don't break prefab connections
</implementation_guidelines>

<output>
Refactor code files throughout the codebase, focusing on:
- `./Assets/Scripts/Combat/AI/**/*.cs` - AI system simplification
- `./Assets/Scripts/Combat/Core/**/*.cs` - Core combat system improvements
- `./Assets/Scripts/Combat/Skills/**/*.cs` - Skills system cleanup
- `./Assets/Scripts/Combat/UI/**/*.cs` - UI code organization
- Any other files identified as complexity hotspots

For each file you refactor:
- Make targeted improvements that reduce complexity
- Preserve all functionality and public interfaces
- Ensure code is more readable after changes than before
</output>

<verification>
Before declaring the refactoring complete, verify:

1. **Functionality Preserved**:
   - Review refactored code to ensure logic remains equivalent
   - Check that all public APIs and interfaces are unchanged (or properly updated everywhere they're used)
   - Verify Unity serialization won't break (field names/types preserved)

2. **Complexity Reduced**:
   - Methods are shorter and more focused
   - Nesting depth is reduced
   - Code is more readable and self-documenting
   - Duplicate code has been consolidated

3. **Quality Improved**:
   - Naming is clearer and more consistent
   - Responsibilities are better separated
   - Code follows consistent patterns
   - Unnecessary complexity has been removed

4. **No Regressions Introduced**:
   - No syntax errors
   - No obvious logic errors
   - Unity-specific patterns intact
   - Dependencies still resolve correctly
</verification>

<success_criteria>
- At least 5-10 significant simplifications made across the codebase
- Code complexity visibly reduced in key areas
- No functionality broken or changed
- Codebase is measurably more maintainable and easier to understand
- Refactoring creates a foundation for easier future development
</success_criteria>

<parallel_execution>
For maximum efficiency, when you need to read multiple files to understand the codebase, invoke Read tools simultaneously rather than sequentially. Similarly, when writing multiple refactored files, batch those operations together.
</parallel_execution>

<reflection>
After analyzing the codebase and identifying refactoring opportunities, take time to carefully consider your refactoring strategy. Think through the implications of each change and how they might affect other parts of the system. The goal is deliberate, thoughtful simplification - not hasty changes.
</reflection>
