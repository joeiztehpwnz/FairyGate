# Target Indicator System - Documentation

**Date:** 2025-10-29
**Version:** Initial Implementation

---

## Overview

The Target Indicator System provides visual feedback when the player targets an enemy. When an enemy is selected as the player's current target (via Tab key), a **yellow/gold outline** appears around the enemy's 3D model.

---

## How It Works

### Architecture

The system uses two components working together:

1. **OutlineEffect.cs** - Creates and manages the outline visual effect
2. **CharacterInfoDisplay.cs** - Detects targeting changes and enables/disables the outline

### Event Flow

```
Player presses Tab
    ↓
CombatController.SetTarget() called
    ↓
CombatController.OnTargetChanged event fires
    ↓
Enemy's CharacterInfoDisplay.HandleTargetChanged() receives event
    ↓
Checks if newTarget == this.transform
    ↓
If YES: outlineEffect.SetOutlineEnabled(true)
If NO:  outlineEffect.SetOutlineEnabled(false)
```

---

## Components

### OutlineEffect.cs

**Location:** `/Assets/Scripts/Combat/UI/OutlineEffect.cs`

**Purpose:** Creates a mesh-based outline effect by rendering a slightly larger duplicate of the character's mesh(es) behind the original.

#### How It Works

1. **Mesh Duplication**: Finds all MeshFilter components on the character (supports multi-mesh characters)
2. **Scale Increase**: Creates duplicate meshes scaled larger by `outlineWidth` (default: 0.03 = 3% larger)
3. **Backface Rendering**: Uses inverted culling (`CullMode.Front`) to only render the "backside" of the enlarged mesh
4. **Solid Color**: Applies a solid color material (default: yellow/gold `#FFEB00`)
5. **Visibility Control**: Shows/hides the outline based on targeting state

#### Inspector Settings

```csharp
[SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0f, 1f); // Yellow/gold
[SerializeField] private float outlineWidth = 0.03f; // 3% larger than original mesh
[SerializeField] private bool showOutline = false; // Initial state
```

#### Public Methods

```csharp
void SetOutlineEnabled(bool enabled)      // Show or hide outline
void SetOutlineColor(Color color)         // Change outline color at runtime
void SetOutlineWidth(float width)         // Change outline thickness at runtime
```

#### Technical Details

- **Shader Used**: `Custom/OutlineShader` (custom shader with front-face culling)
- **Shader Location**: `/Assets/Shaders/OutlineShader.shader`
- **Culling**: Front-face culling (renders only back faces of scaled mesh)
- **Rendering**: No shadows cast, no shadows received, ZWrite Off, ZTest LEqual
- **Performance**: One additional draw call per mesh on the character (only when outline is visible)
- **Hierarchy**: Creates a child GameObject called "Outline" with sub-objects for each mesh

---

### CharacterInfoDisplay.cs (Modifications)

**Location:** `/Assets/Scripts/Combat/UI/CharacterInfoDisplay.cs`

**Changes Made:**

1. Added `OutlineEffect` component reference
2. Added detection for enemy characters (checks for `BaseAI` component)
3. Auto-finds player's `CombatController` during Awake
4. Subscribes to `playerCombatController.OnTargetChanged` event
5. Added `HandleTargetChanged()` method to enable/disable outline

#### Key Code Sections

**Enemy Detection (Awake):**
```csharp
// Determine if this character is an enemy (has AI component)
isEnemy = GetComponent<SimpleTestAI>() != null;

if (isEnemy)
{
    // Auto-find or add OutlineEffect component
    if (outlineEffect == null)
    {
        outlineEffect = GetComponent<OutlineEffect>();
        if (outlineEffect == null)
        {
            outlineEffect = gameObject.AddComponent<OutlineEffect>();
        }
    }

    // Find the player's CombatController for target tracking
    GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
    if (playerObject != null)
    {
        playerCombatController = playerObject.GetComponent<CombatController>();
    }
}
```

**Event Subscription (OnEnable):**
```csharp
// Subscribe to player targeting events (enemies only)
if (isEnemy && playerCombatController != null)
{
    playerCombatController.OnTargetChanged += HandleTargetChanged;
}
```

**Target Change Handler:**
```csharp
private void HandleTargetChanged(Transform newTarget)
{
    // Enable outline if player is targeting this enemy, disable otherwise
    if (outlineEffect != null)
    {
        bool isTargeted = newTarget == transform;
        outlineEffect.SetOutlineEnabled(isTargeted);
    }
}
```

---

## Setup Requirements

### Shader Requirements

**IMPORTANT:** The custom outline shader must be present in your project:
- **File:** `/Assets/Shaders/OutlineShader.shader`
- **Shader Name:** `Custom/OutlineShader`

This shader is required for proper outline rendering with front-face culling. Without it, the outline effect will not work.

### Player GameObject Requirements

1. Must have tag "Player"
2. Must have `CombatController` component
3. CombatController must fire `OnTargetChanged` event when target changes

### Enemy GameObject Requirements

1. Must have `CharacterInfoDisplay` component (auto-added by scene setup tool)
2. Must have `SimpleTestAI` component (used for enemy detection)
3. Must have at least one `MeshFilter` component (for outline rendering)

**Note:** `OutlineEffect` is automatically added by `CharacterInfoDisplay` - no manual setup needed!

---

## Visual Appearance

### Default Outline

- **Color:** Yellow/Gold `RGB(255, 230, 0)` or `#FFE600`
- **Width:** 3% larger than original mesh (0.03)
- **Style:** Solid color, unlit (no shadows)

### Customization

You can customize the outline appearance in the Inspector:

1. Select an enemy GameObject
2. Find the `OutlineEffect` component
3. Adjust:
   - **Outline Color**: Change to any color (e.g., red for hostile, blue for friendly)
   - **Outline Width**: Increase for thicker outlines (0.05 = 5%), decrease for thinner (0.01 = 1%)
   - **Show Outline**: Manually enable/disable for testing

---

## Performance Considerations

### Draw Calls

- **Inactive outline**: 0 additional draw calls (outline object is disabled)
- **Active outline**: +1 draw call per mesh on the character
- **Example**: Character with 3 meshes (body, weapon, helmet) = +3 draw calls when outlined

### Optimization Tips

1. **Minimize mesh count**: Combine meshes where possible to reduce outline draw calls
2. **LOD**: Consider disabling outlines for distant enemies (future enhancement)
3. **Shader**: Using `Unlit/Color` is already optimal (no lighting calculations)

---

## Testing Checklist

### Basic Functionality
- [ ] Enemy has no outline when not targeted
- [ ] Pressing Tab targets the nearest enemy
- [ ] Targeted enemy shows yellow outline
- [ ] Outline completely surrounds the enemy mesh(es)
- [ ] Outline disappears when targeting a different enemy
- [ ] Outline disappears when exiting combat (no target)

### Edge Cases
- [ ] Multiple enemies: only the targeted enemy shows outline
- [ ] Enemy death: outline disappears with the enemy
- [ ] Player death: outlines disappear (combat ends)
- [ ] Tab through multiple enemies: outline switches correctly
- [ ] Outline works with all enemy archetypes (different models)

### Visual Quality
- [ ] Outline is clearly visible against ground color (#085708 dark green)
- [ ] Outline doesn't flicker or z-fight with original mesh
- [ ] Outline thickness is appropriate (not too thick, not too thin)
- [ ] Outline color contrasts well with enemy model colors

---

## Troubleshooting

### "Outline not appearing"

**Check:**
1. **Custom shader exists**: `/Assets/Shaders/OutlineShader.shader` is present
2. Enemy has `CharacterInfoDisplay` component
3. Enemy has `SimpleTestAI` component (for enemy detection)
4. Enemy has at least one `MeshFilter` component
5. Player GameObject has tag "Player"
6. Player has `CombatController` component
7. `OutlineEffect.showOutline` is true in Inspector (when targeted)

**Debug:**
- Check Console for error: "Custom/OutlineShader not found!"
- Add breakpoint in `CharacterInfoDisplay.HandleTargetChanged()`
- Verify `newTarget == transform` evaluates to true
- Check Console for warnings about missing MeshFilters

### "Outline covers entire character (yellow blob)"

**Cause:** Custom shader not found, falling back to incorrect shader behavior

**Fix:**
1. Verify `/Assets/Shaders/OutlineShader.shader` exists
2. Check Unity Console for "Custom/OutlineShader not found!" error
3. Reimport shader: Select shader file → Right-click → Reimport
4. If shader file is missing, recreate it from documentation

### "Outline appears on all enemies"

**Likely Cause:** Event subscription issue

**Fix:**
- Verify each enemy's CharacterInfoDisplay has a unique `outlineEffect` instance
- Check that `HandleTargetChanged` is correctly comparing `newTarget == transform` (not a static reference)

### "Outline is too thick/thin"

**Fix:**
- Select enemy GameObject
- Find `OutlineEffect` component in Inspector
- Adjust `Outline Width` value:
  - Default: 0.03 (3%)
  - Thicker: 0.05-0.10 (5-10%)
  - Thinner: 0.01-0.02 (1-2%)

### "Outline has wrong color"

**Fix:**
- Select enemy GameObject
- Find `OutlineEffect` component in Inspector
- Change `Outline Color` to desired color

### "Player GameObject not found"

**Error:** `GameObject.FindGameObjectWithTag("Player")` returns null

**Fix:**
- Select Player GameObject in Hierarchy
- In Inspector, set Tag to "Player" (dropdown at top)
- If "Player" tag doesn't exist, create it:
  - Tags dropdown → Add Tag... → Create "Player" tag

---

## Future Enhancements

### Planned Improvements

1. **Multiple Outline Colors**
   - Red: Hostile enemy (currently engaged)
   - Yellow: Targeted enemy (current target)
   - Blue: Friendly NPC
   - Orange: Quest-related NPC

2. **Pulsing Effect**
   - Animate outline width to pulse (0.03 → 0.05 → 0.03)
   - Draw attention to newly targeted enemy

3. **Gradient Outlines**
   - Use gradient shader instead of solid color
   - More visual polish

4. **LOD Support**
   - Disable outlines for distant enemies (performance optimization)
   - Only outline nearby targets

5. **Screen-Space Outline**
   - Alternative implementation using post-processing
   - Better performance for many outlined characters
   - Requires Universal Render Pipeline (URP) or post-processing stack

6. **Outline Thickness Scaling**
   - Scale outline width based on camera distance
   - Maintain consistent visual thickness regardless of zoom

---

## Code References

### Files Created
- `/Assets/Scripts/Combat/UI/OutlineEffect.cs` - New component (146 lines)
- `/Assets/Shaders/OutlineShader.shader` - Custom outline shader with front-face culling

### Files Modified
- `/Assets/Scripts/Combat/UI/CharacterInfoDisplay.cs` - Added targeting integration

### Key Methods

**OutlineEffect.cs:**
- `CreateOutline()` - Lines 34-78 - Generates outline meshes with custom shader
- `SetOutlineEnabled(bool enabled)` - Lines 83-90 - Shows/hides outline
- `SetOutlineColor(Color color)` - Lines 95-103 - Changes color at runtime
- `SetOutlineWidth(float width)` - Lines 108-116 - Changes thickness at runtime

**OutlineShader.shader:**
- Custom shader with `Cull Front` for backface-only rendering
- Properties: `_Color` (outline color)
- Pass settings: `ZWrite Off`, `ZTest LEqual`, `Queue Geometry`

**CharacterInfoDisplay.cs:**
- `Awake()` - Lines 58-92 - Enemy detection and OutlineEffect setup
- `OnEnable()` - Lines 126-130 - Subscribe to OnTargetChanged
- `OnDisable()` - Lines 155-159 - Unsubscribe from OnTargetChanged
- `HandleTargetChanged(Transform newTarget)` - Lines 215-223 - Enable/disable outline

---

## Integration with Existing Systems

### CombatController Integration
- Uses existing `CombatController.OnTargetChanged` event
- No modifications to CombatController required
- Works with existing Tab targeting system

### CharacterInfoDisplay Integration
- Extends existing UI component
- No conflicts with health/stamina/meter bars
- Outline effect is independent of OnGUI rendering

### Scene Setup Integration
- `CompleteCombatSceneSetup.cs` already adds CharacterInfoDisplay to enemies
- OutlineEffect is auto-added by CharacterInfoDisplay
- No manual setup required when spawning enemies

---

## Summary

The Target Indicator System provides clear visual feedback for player targeting using a simple, performant mesh outline approach. It integrates seamlessly with existing combat and UI systems, requires minimal setup, and is fully customizable through Inspector settings.

**Key Benefits:**
- ✅ Zero manual setup (fully automatic)
- ✅ Performant (only 1 draw call per mesh when active)
- ✅ Customizable (color, width, visibility)
- ✅ Event-driven (no per-frame polling)
- ✅ Supports multi-mesh characters
- ✅ Clean integration with existing systems

**Next Steps:**
1. Test targeting with various enemy archetypes
2. Tune outline color/width based on visual feedback
3. Consider future enhancements (pulsing, gradient, multiple colors)
