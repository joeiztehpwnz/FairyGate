# Comprehensive Combat System Refactoring Plan

## Overview
After analyzing all 26 combat system files, I've identified **8 major categories of redundancy** across the codebase, ranging from code duplication to architectural patterns that could be consolidated.

---

## Category 1: Component Initialization Redundancy (HIGH IMPACT)
**Files Affected**: 12 files
**Impact**: Medium-High

### Problem
Almost every combat system component has identical initialization patterns:

```csharp
// Repeated in 12+ files:
if (characterStats == null)
{
    Debug.LogWarning($"{ComponentName} on {gameObject.name} has no CharacterStats assigned. Using default values.");
    characterStats = CharacterStats.CreateDefaultStats();
}
```

**Found in**:
- CombatController.cs (line 68-72)
- HealthSystem.cs (line 36-40)
- StaminaSystem.cs (line 42-45)
- StatusEffectManager.cs (line 41-45)
- SkillSystem.cs (line 61-65)
- MovementController.cs (line 40-44)
- KnockdownMeterTracker.cs (line 34-38)
- SimpleTestAI.cs (line 33-42)
- PatternedAI.cs (line 36-42)
- TestRepeaterAI.cs
- KnightAI.cs
- CombatDebugVisualizer.cs

### Proposed Solution
Create a **CombatComponentBase** abstract class:

```csharp
public abstract class CombatComponentBase : MonoBehaviour
{
    [SerializeField] protected CharacterStats characterStats;
    [SerializeField] protected bool enableDebugLogs = true;

    protected virtual void Awake()
    {
        InitializeCharacterStats();
        InitializeComponents();
    }

    private void InitializeCharacterStats()
    {
        if (characterStats == null)
        {
            Debug.LogWarning($"{GetType().Name} on {gameObject.name} has no CharacterStats assigned. Using default values.");
            characterStats = CharacterStats.CreateDefaultStats();
        }
    }

    protected abstract void InitializeComponents();
}
```

**Benefits**:
- Removes 12+ identical code blocks
- Ensures consistent initialization across all components
- Single place to update initialization logic

---

## Category 2: Player/Target Finding Duplication (HIGH IMPACT)
**Files Affected**: 4 files
**Impact**: High

### Problem
Identical player-finding logic repeated 4 times:

**Found in**:
- SimpleTestAI.cs (lines 65-100)
- PatternedAI.cs (lines 57-92)
- TestRepeaterAI.cs (inherits from PatternedAI)
- KnightAI.cs (inherits from PatternedAI)

```csharp
// Repeated pattern:
private void FindPlayer()
{
    var combatants = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
    foreach (var combatant in combatants)
    {
        if (combatant != combatController && combatant.name.Contains("Player"))
        {
            player = combatant.transform;
            break;
        }
    }

    // Fallback to closest combatant...
}
```

### Proposed Solution
Create **CombatTargetFinder** utility class:

```csharp
public static class CombatTargetFinder
{
    public static Transform FindPlayer(CombatController self)
    {
        // Single implementation of player-finding logic
    }

    public static Transform FindClosestEnemy(CombatController self, float maxDistance = 10f)
    {
        // Single implementation of closest-enemy logic
    }

    public static Transform[] FindAllEnemies(CombatController self, float range)
    {
        // Shared enemy-finding logic
    }
}
```

**Benefits**:
- Removes 4 duplicate implementations
- Centralized target-finding logic
- Easier to add new targeting strategies (nearest, weakest, strongest, etc.)

---

## Category 3: Movement Direction Calculation Duplication (MEDIUM IMPACT)
**Files Affected**: 4 files
**Impact**: Medium

### Problem
Identical "convert direction to discrete input" logic repeated 4 times:

**Found in**:
- SimpleTestAI.cs (lines 165-178)
- PatternedAI.cs (lines 229-243)
- TestRepeaterAI.cs (lines 241-280, 282-316) - 2 variations
- KnightAI.cs (uses PatternedAI methods)

```csharp
// Repeated pattern:
Vector3 moveInput = Vector3.zero;
if (direction.x > 0.1f) moveInput.x = 1f;
else if (direction.x < -0.1f) moveInput.x = -1f;
if (direction.z > 0.1f) moveInput.z = 1f;
else if (direction.z < -0.1f) moveInput.z = -1f;
movementController.SetMovementInput(moveInput);
```

### Proposed Solution
Add to **PatternedAI** base class or create **AIMovementHelper**:

```csharp
protected Vector3 DirectionToDiscreteInput(Vector3 direction, float threshold = 0.1f)
{
    Vector3 moveInput = Vector3.zero;
    if (direction.x > threshold) moveInput.x = 1f;
    else if (direction.x < -threshold) moveInput.x = -1f;
    if (direction.z > threshold) moveInput.z = 1f;
    else if (direction.z < -threshold) moveInput.z = -1f;
    return moveInput;
}

protected void MoveInDirection(Vector3 worldDirection)
{
    movementController.SetMovementInput(DirectionToDiscreteInput(worldDirection));
}
```

**Benefits**:
- Removes 4 duplicate implementations
- Consistent movement behavior across all AI
- Easy to adjust threshold or add smoothing

---

## Category 4: Knockback/Displacement Calculation (HIGH IMPACT - Already Identified)
**Files Affected**: CombatInteractionManager.cs
**Impact**: High

### Problem
Knockback calculation repeated 4 times in CombatInteractionManager:

```csharp
// Lines 313-315, 360-362, 416-418, 483-487:
Vector3 direction = (defender.position - attacker.position).normalized;
Vector3 displacement = direction * DISTANCE_CONSTANT;
```

### Proposed Solution
Already documented in COMBAT_INTERACTION_REFACTOR_PROPOSAL.md:

```csharp
private Vector3 CalculateKnockbackDisplacement(Transform attacker, Transform defender, float distance)
{
    Vector3 direction = (defender.position - attacker.position).normalized;
    return direction * distance;
}
```

**Benefits**: (as documented in refactor proposal)

---

## Category 5: Skill Stamina Cost Duplication (LOW IMPACT)
**Files Affected**: 2 files
**Impact**: Low

### Problem
Skill stamina cost lookup duplicated:

**Found in**:
- SkillSystem.cs (lines 472-484)
- StaminaSystem.cs (lines 198-209)

```csharp
// Identical switch statement in both files:
return skillType switch
{
    SkillType.Attack => CombatConstants.ATTACK_STAMINA_COST,
    SkillType.Defense => CombatConstants.DEFENSE_STAMINA_COST,
    // etc...
}
```

### Proposed Solution
Move to CombatConstants as static method:

```csharp
public static class CombatConstants
{
    // ... existing constants ...

    public static int GetSkillStaminaCost(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Attack => ATTACK_STAMINA_COST,
            SkillType.Defense => DEFENSE_STAMINA_COST,
            SkillType.Counter => COUNTER_STAMINA_COST,
            SkillType.Smash => SMASH_STAMINA_COST,
            SkillType.Windmill => WINDMILL_STAMINA_COST,
            SkillType.RangedAttack => RANGED_ATTACK_STAMINA_COST,
            _ => 0
        };
    }
}
```

**Benefits**:
- Single source of truth for stamina costs
- Easy to modify costs in one place
- Reduces risk of desynchronization

---

## Category 6: Debug GUI Boilerplate (MEDIUM IMPACT)
**Files Affected**: 9 files
**Impact**: Medium

### Problem
Nearly identical OnGUI debug display code:

**Found in**:
- HealthSystem.cs (lines 172-194)
- SkillSystem.cs (lines 537-560)
- StatusEffectManager.cs (lines 248-263)
- CombatController.cs (lines 388-400)
- KnockdownMeterTracker.cs (lines 188-198)
- PatternedAI.cs (lines 256-266)
- TestRepeaterAI.cs (lines 436-468)
- KnightAI.cs (lines 166-202)
- CombatDebugVisualizer.cs (entire file)

All use similar patterns:
```csharp
Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * offset);
screenPos.y = Screen.height - screenPos.y;
GUI.Label(new Rect(screenPos.x - width/2, screenPos.y, width, height), text);
```

### Proposed Solution
Create **DebugGUIHelper** utility:

```csharp
public static class DebugGUIHelper
{
    public static void DrawWorldLabel(Transform target, string text, float yOffset = 2f, int width = 120, int height = 60)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + Vector3.up * yOffset);
        screenPos.y = Screen.height - screenPos.y;
        GUI.Label(new Rect(screenPos.x - width/2, screenPos.y, width, height), text);
    }

    public static void DrawHealthBar(Transform target, float percentage, int width = 100, float yOffset = 2f)
    {
        // Reusable health bar drawing
    }
}
```

**Benefits**:
- Removes ~9 duplicate screen position calculations
- Consistent debug display across components
- Easy to add new debug visualization features

---

## Category 7: Skill Execution Pattern Duplication (MEDIUM IMPACT)
**Files Affected**: 2 files
**Impact**: Medium

### Problem
Attack skill execution logic duplicated:

**Found in**:
- SimpleTestAI.cs (lines 223-227)
- TestRepeaterAI.cs (lines 139-196)

Both check for Attack and execute immediately vs. other skills that need charging:

```csharp
if (selectedSkill == SkillType.Attack)
{
    skillSystem.ExecuteSkill(SkillType.Attack);
}
else
{
    skillSystem.StartCharging(selectedSkill);
    // Wait for charged, then execute
}
```

### Proposed Solution
Create **AISkillExecutor** helper or add to PatternedAI base:

```csharp
protected IEnumerator ExecuteAnySkill(SkillType skill)
{
    if (skill == SkillType.Attack)
    {
        // Attack executes immediately
        if (skillSystem.CanExecuteAttack() && IsPlayerInRange())
        {
            skillSystem.ExecuteSkill(SkillType.Attack);
        }
        yield return new WaitForSeconds(GetExecutionDuration(skill));
    }
    else if (skill == SkillType.RangedAttack)
    {
        // RangedAttack uses aiming flow
        yield return StartCoroutine(ExecuteRangedAttack());
    }
    else
    {
        // Other skills use charge flow
        yield return StartCoroutine(ExecuteChargeableSkill(skill));
    }
}
```

**Benefits**:
- Unified skill execution interface for AI
- Handles Attack/RangedAttack/other skill differences automatically
- Reduces AI implementation complexity

---

## Category 8: Range Checking Duplication (MEDIUM IMPACT)
**Files Affected**: 5 files
**Impact**: Medium

### Problem
Range checking patterns repeated across multiple files:

**Found in**:
- WeaponController.cs (lines 43-49, 51-59)
- SimpleTestAI.cs (lines 212, 265)
- PatternedAI.cs (lines 223-227)
- CombatInteractionManager.cs (lines 178-181, 194-204)
- SkillSystem.cs (lines 405-412)

Similar patterns:
```csharp
float distance = Vector3.Distance(transform.position, target.position);
return distance <= weaponData.range;
```

### Proposed Solution
Already partially solved by WeaponController.IsInRange(), but could be extended:

```csharp
public static class RangeChecker
{
    public static bool IsInWeaponRange(Transform source, Transform target, WeaponData weapon)
    {
        if (target == null || weapon == null) return false;
        float distance = Vector3.Distance(source.position, target.position);
        return distance <= weapon.range;
    }

    public static bool IsWithinDistance(Transform source, Transform target, float maxDistance)
    {
        if (target == null) return false;
        return Vector3.Distance(source.position, target.position) <= maxDistance;
    }
}
```

**Benefits**:
- Consistent range checking across all systems
- Handles null checks uniformly
- Easy to add debug visualization or logging

---

## Additional Observations

### 9. Passive Interface Methods (LOW PRIORITY)
**CombatController.cs** acts as a facade, forwarding calls to other components (lines 301-367). This is actually **good architecture** (Facade pattern), not redundancy.

### 10. StatusEffectManager Helper Methods (ACCEPTABLE)
Lines 266-299 have multiple helper methods that appear similar but serve distinct purposes:
- `ApplyStun()` - Calculates stun duration
- `ApplyInteractionKnockdown()` - Two overloads
- `ApplyMeterKnockdown()` - Two overloads
- `ApplyRest()` - Special handling

These are **acceptable specializations** providing convenience APIs.

---

## Refactoring Priority

### Priority 1 - High Impact, Low Risk
1. **Knockback Calculation Helper** (Category 4)
   - 4 duplicates in CombatInteractionManager
   - Already proposed in COMBAT_INTERACTION_REFACTOR_PROPOSAL.md

2. **Player/Target Finding Utility** (Category 2)
   - 4 duplicate implementations
   - Clear utility class pattern

3. **Skill Stamina Cost Consolidation** (Category 5)
   - Simple refactor
   - Zero risk

### Priority 2 - Medium Impact, Medium Risk
4. **Component Initialization Base Class** (Category 1)
   - 12+ files affected
   - Requires careful testing
   - Large refactor but very beneficial

5. **Movement Direction Helper** (Category 3)
   - 4 duplicates
   - Easy to extract to base class

6. **Debug GUI Helper** (Category 6)
   - 9 files affected
   - Low risk, quality-of-life improvement

### Priority 3 - Lower Priority
7. **AI Skill Execution Helper** (Category 7)
   - 2 files affected
   - Nice-to-have

8. **Range Checking Utility** (Category 8)
   - Partially solved already
   - Incremental improvement

---

## Proposed Implementation Plan

### Phase 1: Quick Wins (1-2 hours)
- Implement knockback helper in CombatInteractionManager
- Create CombatConstants.GetSkillStaminaCost()
- Create CombatTargetFinder utility

### Phase 2: Utility Classes (2-3 hours)
- Create DebugGUIHelper
- Create AIMovementHelper
- Add RangeChecker utility

### Phase 3: Base Class Refactoring (3-4 hours)
- Create CombatComponentBase
- Migrate 12 components to inherit from base
- Comprehensive testing

### Phase 4: AI Improvements (2-3 hours)
- Extract ExecuteAnySkill to PatternedAI
- Consolidate movement helpers

---

## Testing Strategy

For each refactoring:
1. **Use TestRepeaterAI environment** to verify interactions still work
2. **Test all 36 skill combinations** (F1-F6 hotkeys)
3. **Verify AI behavior** (SimpleTestAI, KnightAI, TestRepeaterAI)
4. **Check debug visualizations** still work
5. **Performance test** (ensure no regressions)

---

## Risks & Mitigation

**Risk**: Breaking existing functionality
**Mitigation**: Test-driven refactoring, one category at a time

**Risk**: Introducing new bugs in shared utilities
**Mitigation**: Extensive testing with TestRepeaterAI, keep changes atomic

**Risk**: Time investment vs. benefit
**Mitigation**: Start with Priority 1 items (highest ROI)

---

## Recommendation

I recommend starting with **Priority 1** refactorings:
1. Knockback calculation helper
2. Target finding utility
3. Skill stamina cost consolidation

These provide immediate value with minimal risk. After testing, proceed to Priority 2 if desired.

---

## Summary Statistics

- **Total Files Analyzed**: 26
- **Redundancy Categories Identified**: 8
- **Code Duplications Found**: 40+
- **Estimated Lines Eliminated**: ~500-700
- **Estimated Implementation Time**: 8-12 hours total
- **Risk Level**: Low-Medium (with proper testing)
