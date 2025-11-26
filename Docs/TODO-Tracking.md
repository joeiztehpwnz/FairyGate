# FairyGate Combat System - TODO Tracking

**Last Updated:** November 14, 2025
**Purpose:** Track technical debt and planned enhancements

---

## 🎨 Visual Polish & Animation (Priority: Medium)

### TelegraphSystem Animation Integration
**Status:** Deferred to Polish Phase
**Complexity:** Medium
**Dependencies:** Requires animator setup and animation assets

#### TODO Items:

1. **Stance Shift Animation** (Line 237)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs:237`
   - **Current:** Simple eye glow effect
   - **Desired:** Full animator integration for stance shifts
   - **Effort:** 4-8 hours
   - **Notes:** Needs animation clips for each skill stance

2. **Weapon Raise Animation** (Line 247)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs:247`
   - **Current:** Particle effect at weapon position
   - **Desired:** Animated weapon movement to attack position
   - **Effort:** 2-4 hours
   - **Notes:** Coordinate with WeaponController transform

3. **Shield Raise Animation** (Line 256)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs:256`
   - **Current:** Blue eye glow
   - **Desired:** Defensive posture animation with shield
   - **Effort:** 3-6 hours
   - **Notes:** May need shield GameObject if not present

4. **Ground Decal/Ring** (Line 282)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs:282`
   - **Current:** Particle effect at ground level
   - **Desired:** Projected ground decal or ring for AoE skills
   - **Effort:** 6-10 hours
   - **Notes:** Implement decal projection system or use mesh renderer

5. **Crouch Animation** (Line 292)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs:292`
   - **Current:** Purple eye glow
   - **Desired:** Crouch animation before Counter
   - **Effort:** 2-4 hours
   - **Notes:** Blend with idle animation

6. **Backward Movement** (Line 301)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs:301`
   - **Current:** Yellow eye glow
   - **Desired:** Actual backward movement before Lunge
   - **Effort:** 1-2 hours
   - **Notes:** Integrate with MovementController

**Total Estimated Effort:** 18-34 hours

**Implementation Plan:**
1. Set up basic Animator Controller for enemies
2. Create animation clips for each telegraph type
3. Integrate TelegraphSystem with Animator component
4. Test and tune animation timing with telegraph durations
5. Add animation events if needed for precise timing

---

## 🤖 AI Pattern Implementation (Priority: Low)

### Pattern Generator Placeholders
**Status:** Partially Complete
**Complexity:** Medium
**Dependencies:** Pattern system knowledge

#### TODO Items:

1. **Assassin Pattern** (Line 419)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/Editor/PatternGenerator.cs:419`
   - **Description:** Counter-focused, opportunistic behavior
   - **Current Status:** Placeholder method exists
   - **Design Notes:**
     - Heavy use of Counter skill
     - Wait for player to attack, then Counter
     - Quick strikes with hit-and-run tactics
     - High Dexterity, moderate Strength
   - **Effort:** 4-6 hours
   - **Priority:** Low (current patterns are sufficient)

2. **Archer Pattern** (Line 425)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/Editor/PatternGenerator.cs:425`
   - **Description:** Ranged kiter
   - **Current Status:** Placeholder method exists
   - **Design Notes:**
     - Primarily RangedAttack skill
     - Maintain distance from player
     - Retreat when player closes
     - Occasional Defense when cornered
   - **Effort:** 4-6 hours
   - **Priority:** Low (Bow weapon exists, pattern needed for dedicated archer)

3. **Soldier Pattern** (Line 431)
   - **File:** `Assets/Scripts/Combat/AI/Patterns/Editor/PatternGenerator.cs:431`
   - **Status:** ✅ **COMPLETE** (Already implemented)
   - **Notes:** Remove this TODO comment

**Total Estimated Effort:** 8-12 hours (excluding completed Soldier)

**Implementation Priority:**
- Soldier: ✅ Done
- Assassin: Could add variety, but not essential
- Archer: Useful for ranged enemy variety

---

## 🎬 Visual Effects (Priority: Low)

### Ranged Attack Trail
**Status:** Placeholder Implementation
**Complexity:** Low-Medium

#### TODO Item:

1. **Ranged Attack Visual Trail** (Line 420)
   - **File:** `Assets/Scripts/Combat/Weapons/WeaponController.cs:420`
   - **Current:** Debug.DrawLine (scene view only)
   - **Desired:** Actual visual projectile or trail
   - **Options:**
     - **Option A:** Particle System trail (2-4 hours)
       - Create particle prefab
       - Configure emission and lifetime
       - Spawn at source, move to target
     - **Option B:** Line Renderer (1-2 hours)
       - Simple line with gradient
       - Fade out over time
     - **Option C:** Projectile GameObject (4-6 hours)
       - Arrow/bolt mesh
       - Travel animation
       - Hit particle on impact
   - **Recommended:** Option A (best balance of visual quality and effort)
   - **Effort:** 2-6 hours depending on option
   - **Priority:** Low (gameplay works without it)

---

## 📋 Cleanup Tasks (Priority: High)

### Debug Log Migration
**Status:** Not Started
**Complexity:** Low (but time-consuming)
**Tool Available:** ✅ CombatLogger.cs created

#### Migration Plan:

**Scope:** 298 Debug.Log calls across 35+ files

**Process:**
1. Identify all Debug.Log/LogWarning/LogError calls
2. Categorize each by system (AI, Combat, Skills, etc.)
3. Replace with appropriate CombatLogger method
4. Test that logs still appear correctly
5. Configure default enabled categories

**Example Replacements:**
```csharp
// Before:
Debug.Log($"[AI] Pattern transition to {node.name}");

// After:
CombatLogger.LogAI($"Pattern transition to {node.name}");
```

```csharp
// Before:
Debug.LogWarning($"Skill execution failed: {reason}");

// After:
CombatLogger.LogSkill($"Execution failed: {reason}", CombatLogger.LogLevel.Warning);
```

**Estimated Effort:** 6-10 hours
**Priority:** High (improves debugging experience significantly)

**Files to Migrate (High Priority):**
1. PatternExecutor.cs - ~30 logs
2. AICoordinator.cs - ~25 logs
3. SkillSystem.cs - ~40 logs
4. CombatInteractionManager.cs - ~35 logs
5. MovementController.cs - ~20 logs

**Files to Migrate (Medium Priority):**
- All other AI classes
- Combat systems
- Skill states
- UI components

**Files to Skip:**
- Editor scripts (keep regular Debug.Log)
- One-time initialization logs
- Critical error logs (can use CombatLogger.LogSystem)

---

## 📊 Status Summary

### By Priority:

**High Priority (Do Soon):**
- ⏳ Debug Log Migration (6-10 hours)

**Medium Priority (Future Sprint):**
- ⏳ TelegraphSystem Animation Integration (18-34 hours)

**Low Priority (Backlog):**
- ⏳ AI Pattern Implementation - Assassin & Archer (8-12 hours)
- ⏳ Ranged Attack Visual Trail (2-6 hours)

### By Category:

| Category | Items | Status | Total Effort |
|----------|-------|--------|--------------|
| Animation | 6 | Deferred | 18-34 hours |
| AI Patterns | 3 | 1/3 Complete | 8-12 hours |
| Visual Effects | 1 | Placeholder | 2-6 hours |
| Code Cleanup | 1 | Ready to Start | 6-10 hours |

---

## 🎯 Recommended Next Steps

1. **Immediate (This Week):**
   - Start Debug.Log migration to CombatLogger
   - Remove completed "Soldier Pattern" TODO comment

2. **Short-term (Next 2 Weeks):**
   - Complete Debug.Log migration
   - Test CombatLogger in various scenarios
   - Tune default log category settings

3. **Medium-term (Next Month):**
   - Decide on visual polish timeline
   - Plan animation work if greenlit
   - Prioritize AI pattern variety

4. **Long-term (Backlog):**
   - Full animation integration for telegraphs
   - Additional AI patterns (Assassin, Archer)
   - Ranged attack visual effects

---

## 📝 Notes

### Decision Points:
- **Animation Integration:** Requires animator setup - wait until art pipeline is established
- **AI Patterns:** Current variety is sufficient for testing; add more when needed
- **Visual Effects:** Nice-to-have; gameplay is functional without them

### Technical Debt Eliminated:
- ✅ Removed coroutine skill system
- ✅ Deleted SimpleTestAI deprecated code
- ✅ Fixed all compilation errors
- ✅ Removed empty directories
- ✅ Implemented all missing interface methods

### Refactoring Complete:
- ✅ UI components decomposed
- ✅ PatternExecutor modularized
- ✅ CombatInteractionManager refactored
- ✅ CompleteCombatSceneSetup split
- ✅ AICoordinator decomposed
- ✅ Utility classes created
- ✅ Centralized logging system implemented

**The codebase is now in excellent shape for continued development!**

---

**Last Updated:** November 14, 2025
