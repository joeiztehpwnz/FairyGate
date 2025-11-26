# CombatLogger Usage Guide

## Quick Start

The FairyGate combat system now uses a professional logging system called **CombatLogger** instead of Unity's Debug.Log. This provides filterable, color-coded logging with zero runtime cost in production builds.

## Basic Usage

### Standard Logging

```csharp
// Before (Old way - NO LONGER USED)
Debug.Log("Skill executed successfully");

// After (New way - USE THIS)
CombatLogger.Log(CombatLogger.Category.Skills, "Skill executed successfully");
```

### Warning Messages

```csharp
// Before
Debug.LogWarning("Stamina is low");

// After
CombatLogger.LogWarning(CombatLogger.Category.Combat, "Stamina is low");
```

### Error Messages

```csharp
// Before
Debug.LogError("Missing required component");

// After
CombatLogger.LogError(CombatLogger.Category.Combat, "Missing required component");
```

## Category Reference

Choose the category that matches your file's primary responsibility:

### CombatLogger.Category.Combat
Use for combat interactions, damage, and skill execution
```csharp
CombatLogger.Log(CombatLogger.Category.Combat, "Dealing 50 damage to target");
```
**Files using this:** CombatInteractionManager, WeaponController, HealthSystem, etc.

### CombatLogger.Category.AI
Use for AI patterns, coordination, and tactics
```csharp
CombatLogger.Log(CombatLogger.Category.AI, "Transitioning to aggressive pattern");
```
**Files using this:** PatternExecutor, AICoordinator, AttackCoordinator, etc.

### CombatLogger.Category.Skills
Use for skill states and execution
```csharp
CombatLogger.Log(CombatLogger.Category.Skills, "Entering charging state");
```
**Files using this:** SkillSystem, ChargingState, ActiveState, etc.

### CombatLogger.Category.Movement
Use for movement control and arbitration
```csharp
CombatLogger.Log(CombatLogger.Category.Movement, "Movement locked by skill execution");
```
**Files using this:** MovementController, MovementArbitrator

### CombatLogger.Category.StatusEffects
Use for status effect management
```csharp
CombatLogger.Log(CombatLogger.Category.StatusEffects, "Applied stun for 2 seconds");
```
**Files using this:** StatusEffectManager

### CombatLogger.Category.UI
Use for user interface displays
```csharp
CombatLogger.Log(CombatLogger.Category.UI, "Updated health bar display");
```
**Files using this:** SkillIconDisplay, OutlineEffect, CharacterInfoDisplay

### CombatLogger.Category.General
Use for game management and utilities
```csharp
CombatLogger.Log(CombatLogger.Category.General, "Scene reset initiated");
```
**Files using this:** GameManager, CombatUpdateManager, CameraController

## Real-World Examples from the Codebase

### Example 1: Combat Interaction Logging
```csharp
// From CombatInteractionManager.cs
if (enableDebugLogs)
{
    CombatLogger.Log(CombatLogger.Category.Combat, 
        $"Processing interaction: {offensiveSkill.combatant.name} {offensiveSkill.skillType} vs " +
        $"{defensiveSkill.combatant.name} {defensiveSkill.skillType} = {interaction}");
}
```

### Example 2: AI Pattern Logging
```csharp
// From PatternExecutor.cs
CombatLogger.Log(CombatLogger.Category.AI, 
    $"{gameObject.name} transitioned to '{currentNode.nodeName}' " +
    $"(Priority: {currentNode.priority}, Timeout: {currentNode.timeout}s)");
```

### Example 3: Skill State Logging
```csharp
// From SkillSystem.cs
CombatLogger.Log(CombatLogger.Category.Skills, 
    $"{combatController.name} {skillType} → {newState}");
```

### Example 4: Warning Messages
```csharp
// From CombatController.cs
CombatLogger.LogWarning(CombatLogger.Category.Combat, 
    $"CombatController on {gameObject.name} has no CharacterStats assigned. Using default values.");
```

### Example 5: Error Messages
```csharp
// From SkillInteractionResolver.cs
CombatLogger.LogError(CombatLogger.Category.Combat, 
    "Missing required components for skill interaction");
```

## Filtering Logs in Unity

### Opening the Config Window
1. In Unity, go to menu: **Window > Combat > CombatLogger Config**
2. A window will open showing all available categories

### Enabling/Disabling Categories
- **Toggle individual categories** to show/hide their logs
- Changes take effect immediately
- Useful for focusing on specific systems during debugging

### Example Workflow
```
Debugging AI behavior?
→ Enable only "AI" category
→ See AI-related logs without clutter

Debugging skill execution?
→ Enable "Skills" and "Combat" categories
→ See skill transitions and combat interactions

Debugging everything?
→ Enable all categories
→ See complete system behavior
```

## Color Coding

CombatLogger automatically color-codes output for easy visual parsing:
- **Combat** - Yellow
- **AI** - Magenta
- **Skills** - Cyan
- **Movement** - Blue
- **StatusEffects** - Orange
- **UI** - Green
- **General** - White

## Production Builds

**Zero Runtime Cost!**

All CombatLogger calls are automatically compiled out in production builds using conditional compilation:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // CombatLogger code only runs here
#endif
```

This means:
- ✅ **No performance impact** in release builds
- ✅ **No strings allocated** in release builds
- ✅ **No console spam** in release builds
- ✅ **Professional logging** during development

## Migration Pattern Reference

If you're adding new code, follow these patterns:

### Pattern 1: Simple Log
```csharp
// OLD - Don't use this
Debug.Log("Something happened");

// NEW - Use this
CombatLogger.Log(CombatLogger.Category.YourCategory, "Something happened");
```

### Pattern 2: Conditional Log
```csharp
// OLD
if (enableDebugLogs)
{
    Debug.Log($"Value: {value}");
}

// NEW
if (enableDebugLogs)
{
    CombatLogger.Log(CombatLogger.Category.YourCategory, $"Value: {value}");
}
```

### Pattern 3: Warning with Condition
```csharp
// OLD
if (component == null)
{
    Debug.LogWarning("Component missing!");
}

// NEW
if (component == null)
{
    CombatLogger.LogWarning(CombatLogger.Category.YourCategory, "Component missing!");
}
```

### Pattern 4: Error Logging
```csharp
// OLD
Debug.LogError($"Failed to process {name}");

// NEW
CombatLogger.LogError(CombatLogger.Category.YourCategory, $"Failed to process {name}");
```

## Tips and Best Practices

### 1. Choose the Right Category
Match the category to your file's **primary responsibility**, not what it's logging about.

**Example:** 
- In `AICoordinator.cs`, use `Category.AI` even when logging combat interactions
- In `SkillSystem.cs`, use `Category.Skills` even when logging movement restrictions

### 2. Keep Messages Descriptive
```csharp
// ❌ Bad - Too vague
CombatLogger.Log(CombatLogger.Category.Combat, "Success");

// ✅ Good - Clear and informative
CombatLogger.Log(CombatLogger.Category.Combat, 
    $"{attacker.name} dealt {damage} damage to {target.name}");
```

### 3. Use String Interpolation
```csharp
// ✅ Preferred - Clear and concise
CombatLogger.Log(CombatLogger.Category.Skills, 
    $"{skillType} executed in {duration}ms");
```

### 4. Wrap in Conditional When Expensive
```csharp
// If logging requires expensive calculations
if (enableDebugLogs)
{
    string expensiveDebugInfo = CalculateComplexDebugData();
    CombatLogger.Log(CombatLogger.Category.AI, expensiveDebugInfo);
}
```

### 5. Preserve Context with Color Tags (Optional)
```csharp
// You can still use Unity's color tags if needed
CombatLogger.Log(CombatLogger.Category.Combat, 
    $"<color=red>CRITICAL:</color> Health at {health}%");
```

## Common Mistakes to Avoid

### ❌ Don't use Debug.Log anymore
```csharp
Debug.Log("Something"); // NO - Don't use this
```

### ❌ Don't forget the category
```csharp
CombatLogger.Log("Something"); // COMPILER ERROR - Missing category
```

### ❌ Don't use wrong category
```csharp
// In SkillSystem.cs
CombatLogger.Log(CombatLogger.Category.AI, "..."); // Wrong category
```

### ✅ Do this instead
```csharp
// In SkillSystem.cs
CombatLogger.Log(CombatLogger.Category.Skills, "..."); // Correct
```

## FAQ

**Q: Can I still use Debug.Log for quick testing?**
A: No, use CombatLogger instead. It's just as easy and provides better organization.

**Q: What if I'm not sure which category to use?**
A: Use the category that matches your file's location/purpose. When in doubt, use `Category.General`.

**Q: Will this slow down my development builds?**
A: No, CombatLogger is highly optimized and has negligible performance impact in development builds.

**Q: How do I view only AI logs?**
A: Open the CombatLogger Config window (Window > Combat > CombatLogger Config) and enable only the AI category.

**Q: Can I add new categories?**
A: Yes, edit `/Assets/Scripts/Combat/Utilities/CombatLogger.cs` and add your category to the `Category` enum.

## Summary

**Old Way (Don't use):**
```csharp
Debug.Log("Message");
Debug.LogWarning("Warning");
Debug.LogError("Error");
```

**New Way (Use this):**
```csharp
CombatLogger.Log(CombatLogger.Category.YourCategory, "Message");
CombatLogger.LogWarning(CombatLogger.Category.YourCategory, "Warning");
CombatLogger.LogError(CombatLogger.Category.YourCategory, "Error");
```

---

**For more details, see:**
- `MIGRATION_REPORT.md` - Complete migration statistics
- `MIGRATION_FILE_LIST.md` - Full list of migrated files
- `/Assets/Scripts/Combat/Utilities/CombatLogger.cs` - Source code
