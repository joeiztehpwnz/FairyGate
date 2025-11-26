# N+1 System - Deprecated Mechanics

## Summary

The N+1 Combo System requires enemies to remain **stunned in place** during combo chains (no displacement). The old 50% knockback threshold system has been **deprecated by default** to enable Classic Mabinogi-authentic N+1 gameplay.

---

## What Changed

### ✅ KEPT: Core Mechanics

These systems remain **fully functional** and work with N+1:

1. **Stun on Each Hit** - Enemies freeze in place (no displacement) ✓
2. **100% Knockdown Threshold** - Full knockdown when meter reaches 100 ✓
3. **Meter Tracking** - Tracks combo pressure with diminishing returns ✓
4. **Meter Decay** - Continuous -5/second decay ✓
5. **Combo Timeout** - 2 second timeout resets combo counter ✓
6. **Skill-Based Knockdowns** - Smash/Windmill instant knockdowns ✓

### ❌ DEPRECATED: 50% Knockback Threshold

**File:** `KnockdownMeterTracker.cs`

**What It Did:**
- At 50% meter (after ~2 hits), applied knockback with 1.5m displacement
- Enemy pushed backward, interrupting combo flow

**Why Deprecated:**
- **Breaks N+1 Positioning** - Displaces enemy during combo window
- **Makes N+1 Impossible** - Can't land 3+ hits before displacement
- **Not Classic Mabinogi** - Original game had stun in place, not mid-combo knockback

**New Behavior:**
- **Default:** `enableKnockbackThreshold = false` (DISABLED)
- Enemies stay stunned in place during entire combo
- 50% meter threshold does nothing (meter still tracks for UI)
- 100% meter still triggers full knockdown

**Toggle:** Set `enableKnockbackThreshold = true` in inspector to restore old behavior (not recommended)

---

## Combat Flow Comparison

### OLD SYSTEM (Broken for N+1)
```
Hit 1 → +30 meter → Enemy stunned
Hit 2 → +25 meter (55 total) → 50% KNOCKBACK → Enemy displaced 1.5m ❌
Hit 3 → Can't reach enemy / positioning broken ❌
N+1 Window → Can't execute (enemy too far) ❌
```

### NEW SYSTEM (Classic Mabinogi + N+1)
```
Hit 1 → +30 meter → Enemy stunned IN PLACE ✓
Hit 2 → +25 meter (55 total) → Enemy stunned IN PLACE ✓
Hit 3 → +20 meter (75 total) → Enemy stunned IN PLACE ✓
N+1 Window (70-95%) → Player executes Smash → 4-HIT COMBO ✓
100% Meter → Full knockdown (combo naturally ends) ✓
```

---

## Technical Changes

### KnockdownMeterTracker.cs

**Added Configuration:**
```csharp
[Header("N+1 System Compatibility")]
[Tooltip("Disable 50% knockback threshold to enable Classic Mabinogi N+1 combo system.")]
[SerializeField] private bool enableKnockbackThreshold = false; // DISABLED by default
```

**Modified Methods:**

1. **AddToMeter()** - Line 140
   ```csharp
   // Only trigger knockback if threshold system is enabled
   if (enableKnockbackThreshold && !hasTriggeredKnockback && ...)
   {
       TriggerKnockback();
   }
   ```

2. **CombatUpdate()** - Line 77
   ```csharp
   // Only reset flag if threshold system is enabled
   if (enableKnockbackThreshold && hasTriggeredKnockback && ...)
   {
       hasTriggeredKnockback = false;
   }
   ```

3. **TriggerKnockback()** - Line 153
   - Added deprecation documentation
   - Explains why it conflicts with N+1
   - Method still exists for backward compatibility

---

## Migration Guide

### For Existing Scenes

All existing characters with `KnockdownMeterTracker` will have:
- `enableKnockbackThreshold = false` (default)
- 50% knockback disabled automatically
- N+1 system works out of the box ✓

### To Restore Old Behavior

If you want the old 50% knockback system:
1. Select character in hierarchy
2. Find `KnockdownMeterTracker` component
3. Check ☑ "Enable Knockback Threshold"
4. **Warning:** This will break N+1 combo system

---

## Remaining Mechanics

### Still Functional - No Changes Needed

1. **Stun System** (`StatusEffectManager.ApplyStun`)
   - Freezes enemy in place
   - No displacement
   - Works perfectly with N+1 ✓

2. **100% Knockdown** (`TriggerMeterKnockdown`)
   - Still triggers at 100% meter
   - Applies full knockdown with displacement
   - Ends combo naturally ✓

3. **Skill Knockdowns** (`TriggerImmediateKnockdown`)
   - Smash/Windmill bypass meter entirely
   - Instant knockdown
   - Works as designed ✓

4. **Meter UI**
   - Still tracks 0-100%
   - Still shows in UI
   - Just no displacement at 50% ✓

---

## Testing Checklist

Verify N+1 works with deprecated knockback:

- [ ] Land 3-hit combo without enemy displacement
- [ ] Execute N+1 Smash at 70-95% stun window
- [ ] Confirm 50% meter does NOT displace enemy
- [ ] Confirm 100% meter DOES trigger knockdown
- [ ] Test with different weapon speeds (Mace vs Dagger)
- [ ] Test AI N+1 execution

---

## Future Considerations

### If You Need Mid-Combo Feedback

Since 50% knockback is disabled, consider alternatives:

**Visual Indicators:**
- Meter bar color change at 50% (yellow → red)
- Particle effect on enemy at 50% (no displacement)
- Screen shake intensity increases
- Enemy stance change (visual only)

**Audio Cues:**
- Different hit sound at 50%
- "Near knockdown" audio warning
- Intensity ramp in combo music

**Gameplay Alternatives:**
- Reduce enemy defense at 50% instead of displacement
- Increase stagger animation intensity (visual only)
- Grant attacker temporary damage bonus

---

## Backward Compatibility

The `enableKnockbackThreshold` flag ensures:
- ✓ Old behavior available if needed
- ✓ No breaking changes to existing code
- ✓ Method signatures unchanged
- ✓ Events still fire correctly
- ✓ Unity serialization preserved

**Default:** New projects get N+1-compatible behavior automatically.

---

## Questions?

**Q: Why not keep both systems?**  
A: They're fundamentally incompatible. Displacement breaks N+1 positioning.

**Q: Can I have knockback on SOME enemies?**  
A: Yes! Toggle `enableKnockbackThreshold` per-character in inspector.

**Q: What about Counter/Defense knockback?**  
A: Those use different systems (interaction-based) and still work fine.

**Q: Will this affect difficulty?**  
A: Slightly easier - enemies don't escape combos at 50%. Balance with:
  - Higher enemy HP
  - Higher Focus stat (tighter N+1 windows)
  - More aggressive AI patterns

---

## Summary

**Deprecated:** 50% knockback threshold with displacement  
**Reason:** Conflicts with Classic Mabinogi N+1 combo system  
**Default:** Disabled (`enableKnockbackThreshold = false`)  
**Impact:** Enemies stay in place during combos, enabling N+1  
**Backward Compatibility:** Toggle available if needed

The N+1 system is now fully compatible with your existing combat mechanics! 🎉
