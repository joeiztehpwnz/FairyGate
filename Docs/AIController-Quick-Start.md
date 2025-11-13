# AIController Quick Start Guide

## Latest Changes (Pattern-Only Architecture)

✅ **Reactive AI Removed** - All weighted random and CC exploitation logic has been removed
✅ **Pattern System Required** - AI now exclusively uses deterministic pattern-based behavior
✅ **Cleaner Codebase** - ~500 lines of conflicting reactive logic eliminated

## Fixed Issues

✅ **NullReferenceException** - Added safety checks for null CombatRanges
✅ **Coordination dependency** - AIController now works without SimpleTestAI
✅ **Formation system** - Properly handles missing components

---

## Setup Instructions

### **Pattern System Setup (REQUIRED)**

Pattern System is now **REQUIRED** for all AI - reactive AI has been removed.

1. **Create AI Configuration**
   - Right-click in Project → `Create → FairyGate → AI → AI Configuration`
   - Name it: `TestAIConfig`

2. **Create or Assign Pattern Definition**
   - Use existing patterns: `BerserkerPattern`, `GuardianPattern`, `SoldierPattern`
   - Or create new: Right-click → `Create → FairyGate → AI → Pattern Definition`

3. **Configure Settings**
   ```
   Config Name: Test AI
   Skill Cooldown: 2.0
   Random Variance: 1.0
   Engage Distance: 3.0

   Use Coordination: FALSE  ← IMPORTANT: Disable for standalone
   Use Pattern System: TRUE  ← REQUIRED!
   Pattern Definition: [Assign your pattern asset]
   ```

4. **Add Components to Enemy GameObject**
   - Add Component → `AIController`
   - Add Component → `PatternExecutor`
   - Add Component → `TelegraphSystem` (optional, for visual telegraphs)
   - Drag `TestAIConfig` to AIController's `Config` field
   - **DO NOT add SimpleTestAI component**

5. **Test**
   - AI should now move toward player and execute pattern
   - Movement is direct (no formation when coordination disabled)
   - Attacks follow deterministic pattern

---

### **AIController with Coordination (Full Features)**

Use AIController with formation positioning and attack timing coordination:

1. **Create AI Configuration**
   ```
   Use Coordination: TRUE  ← Enable coordination
   Use Pattern System: TRUE  ← Required!
   Pattern Definition: [Assign pattern]
   ```

2. **Add Components to Enemy**
   - Add `AIController` component
   - Add `PatternExecutor` component
   - Add `SimpleTestAI` component ← **Required for coordination**
   - Add `TelegraphSystem` (optional)
   - Drag config to AIController's `Config` field

3. **Configure SimpleTestAI**
   - Enable `Use Pattern System: true`
   - Assign same `Pattern Definition` as AIController
   - SimpleTestAI is used as reference for AICoordinator
   - All actual AI logic is in AIController

4. **Ensure AICoordinator Exists**
   - Check scene has `AICoordinator` GameObject
   - Or it will auto-create one on first AI spawn

---

## Troubleshooting

### **Issue: "Pattern System is disabled" error**

**Error:** `Pattern System is disabled! Reactive AI has been removed.`

**This means:**
- Reactive AI has been completely removed from the codebase
- Pattern System is now **REQUIRED** for all AI

**To fix:**
1. Enable `Use Pattern System: true` in AIConfiguration
2. Assign a Pattern Definition to the config
3. Add `PatternExecutor` component to enemy GameObject
4. Optionally add `TelegraphSystem` for visual telegraphs

### **Issue: AI doesn't move or attack**

**Check:**
- ✅ AIConfiguration asset is assigned to AIController
- ✅ `Use Pattern System: true` is enabled
- ✅ Pattern Definition is assigned
- ✅ PatternExecutor component exists on enemy
- ✅ Enemy has all required components (CombatController, SkillSystem, etc.)
- ✅ Player exists in scene with CombatController
- ✅ `Engage Distance` in config is high enough (try 5.0)

**Debug:**
- Enable `Enable Debug Logs` in AIConfiguration
- Watch Console for warnings/errors

### **Issue: "Coordination disabled" warning**

**This is normal if:**
- You set `Use Coordination: false` in config
- You didn't add SimpleTestAI component

**To fix (if you want coordination):**
- Set `Use Coordination: true`
- Add `SimpleTestAI` component to enemy
- Ensure AICoordinator exists in scene

### **Issue: Pattern validation errors**

**Error:** `Pattern has no nodes defined!`

**This happens when:**
- Empty/unnamed PatternDefinition asset exists
- Pattern asset is corrupted
- Pattern Definition has no state nodes configured

**To fix:**
- Delete unused/empty pattern assets
- Create a proper Pattern Definition with state nodes
- Assign the correct pattern asset to your AIConfiguration

### **Issue: AI charges skills but doesn't move properly**

**This is expected behavior when:**
- Formation system is disabled
- AI is waiting for attack permission

**To test movement:**
1. Disable coordination: `Use Coordination: false`
2. Increase engage distance: `Engage Distance: 5.0`
3. Check AI moves directly toward player

---

## Migration from SimpleTestAI

### **Gradual Migration (Recommended)**

Keep both components during testing:

```
Enemy GameObject:
├── AIController (NEW - handles logic)
├── SimpleTestAI (OLD - only for coordinator reference)
├── CombatController
├── SkillSystem
└── ... other components
```

Eventually remove SimpleTestAI once fully migrated.

### **Clean Migration (Advanced)**

1. Create AIConfiguration for each enemy type
2. Test AIController on NEW enemies first
3. Keep existing enemies using SimpleTestAI
4. Gradually migrate old enemies
5. Eventually deprecate SimpleTestAI entirely

---

## Performance Tips

1. **Disable Debug Logs** in production
   - `Enable Debug Logs: false` in config

2. **Adjust Cooldowns** for performance
   - Higher cooldowns = less CPU usage
   - `Skill Cooldown: 3.0+` for large groups

3. **Formation System**
   - Works best with 3-8 enemies
   - For 10+ enemies, consider disabling formation

---

## Next Steps

1. ✅ Test standalone AIController (no coordination)
2. ✅ Verify AI moves and attacks properly
3. ✅ Create configs for different archetypes
4. ✅ Enable coordination for multi-enemy battles
5. ✅ Test pattern system for boss enemies

The refactored system should now work properly!
