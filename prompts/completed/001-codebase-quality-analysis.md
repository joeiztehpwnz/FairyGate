<objective>
Perform a comprehensive code quality analysis of the entire FairyGate Unity combat system codebase.

The goal is to produce a prioritized refactoring plan that identifies the most impactful improvements, technical debt, and cleanup opportunities. This analysis will guide future development efforts and help maintain code health.
</objective>

<context>
This is a Unity C# combat system project implementing a pattern-based AI system with:
- Combat mechanics (skills, weapons, stats, status effects)
- AI coordination and pattern execution
- UI systems for combat display
- Equipment and weapon management

The codebase has accumulated .bak files, multiple documentation files, and potentially inconsistent patterns as features have evolved.

Read CLAUDE.md for project conventions and context.
</context>

<analysis_requirements>

<architecture_patterns>
Thoroughly analyze the codebase architecture:
- Evaluate adherence to SOLID principles across all modules
- Identify design pattern usage and consistency (state machines, observers, etc.)
- Assess separation of concerns between Combat, AI, UI, and Data layers
- Check for proper dependency injection vs tight coupling
- Evaluate interface usage and abstraction levels
</architecture_patterns>

<tech_debt_identification>
Identify all forms of technical debt:
- Locate and catalog all .bak files (these indicate incomplete refactoring)
- Find dead code, unused methods, and orphaned files
- Identify code duplication across files
- Flag outdated patterns or deprecated approaches
- Find TODO/FIXME/HACK comments that indicate known issues
- Identify inconsistent naming conventions or coding styles
</tech_debt_identification>

<maintainability_assessment>
Evaluate code maintainability:
- Measure cyclomatic complexity of key methods
- Identify overly long methods or classes (god objects)
- Check for proper error handling patterns
- Assess readability and self-documenting code quality
- Identify documentation gaps in critical systems
- Evaluate test coverage gaps (if tests exist)
</maintainability_assessment>

<specific_areas>
Pay special attention to:
- Assets/Scripts/Combat/Core/ - Core combat systems
- Assets/Scripts/Combat/AI/ - AI pattern system
- Assets/Scripts/Combat/Skills/States/ - Skill state machine
- Assets/Scripts/Combat/UI/ - UI components
- Untracked files shown in git status (potential cleanup candidates)
</specific_areas>

</analysis_requirements>

<data_gathering>
Use these approaches to gather data:

1. File structure analysis:
   - Glob patterns to find all .cs files
   - Identify file organization and module boundaries

2. Code pattern search:
   - Grep for TODO, FIXME, HACK comments
   - Search for common anti-patterns
   - Find .bak files and orphaned code

3. Dependency analysis:
   - Trace using statements and class dependencies
   - Identify circular dependencies if present

4. Git status review:
   - Analyze untracked files (potential orphans or work-in-progress)
   - Check deleted files that may have dependencies
</data_gathering>

<output_format>
Create a comprehensive analysis report saved to: `./analyses/codebase-quality-analysis.md`

Structure the report as follows:

## Executive Summary
- Overall code health score (1-10 with justification)
- Top 3 most critical issues
- Quick wins (easy improvements with high impact)

## Architecture Analysis
- Pattern adherence assessment
- Dependency structure evaluation
- Module boundary clarity

## Technical Debt Inventory
### Critical (blocks development or causes bugs)
### High (significant maintainability impact)
### Medium (should address during related work)
### Low (nice to have)

## File Cleanup Recommendations
- .bak files to review and delete
- Orphaned files to remove
- Documentation files to consolidate or remove

## Refactoring Plan (Prioritized)
For each item include:
- Description of the issue
- Location (file paths)
- Recommended fix
- Estimated effort (Small/Medium/Large)
- Dependencies (what else might be affected)

Priority order should consider:
1. Risk of bugs/breakage
2. Developer productivity impact
3. Effort required
4. Dependencies on other changes

## Quick Reference
- Files with highest complexity
- Most coupled components
- Largest files by line count
</output_format>

<verification>
Before completing, verify:
- All major directories under Assets/Scripts/ have been examined
- .bak files have been cataloged
- Untracked files from git status have been assessed
- The refactoring plan is actionable with clear next steps
- Priority rankings are justified
</verification>

<success_criteria>
- Comprehensive coverage of all Combat system code
- Clear, actionable refactoring recommendations
- Prioritization that balances impact vs effort
- Specific file paths and line numbers where relevant
- Analysis saved to ./analyses/codebase-quality-analysis.md
</success_criteria>
