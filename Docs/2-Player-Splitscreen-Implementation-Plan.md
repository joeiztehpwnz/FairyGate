# 2-Player Splitscreen Implementation Plan

## Overview
Plan for adding 2-player splitscreen support to the FairyGate combat system for testing purposes.

---

## Current System Analysis

### Camera System
**File:** `CameraController.cs`
- Single camera following one player target
- Isometric view with orbital rotation (Arrow Left/Right)
- Auto-finds player by tag "Player" or by name "Player"
- Uses standard Unity Camera component
- No viewport rect configuration (defaults to full screen)

### Input System
**Current Status:** Unity Legacy Input System (Input.GetKey/GetKeyDown)
- No Unity Input System package
- Direct KeyCode assignments in components

**Player 1 Input Configuration:**
- Movement: WASD
- Skills: Alpha1-7 (1-7 number keys)
- Cancel: Space
- Target: Tab
- Rest: X
- ExitCombat: Escape
- WeaponSwap: C
- Camera Rotation: Arrow Left/Right

### UI System
**Rendering:** OnGUI (immediate mode GUI)

**UI Components Using OnGUI:**
- HealthBarUI.cs - Health bar above characters
- StaminaBarUI.cs - Stamina bar
- KnockdownMeterBarUI.cs - Knockdown meter bar
- SkillIconDisplay.cs - Skill icons and charge progress
- StatusEffectDisplay.cs - Status effect indicators
- NPlusOneTimingIndicatorSimple.cs - N+1 combo timing window

**Current Limitations:**
- All UI uses `Camera.main` which returns first active camera
- `Screen.width` and `Screen.height` return full screen dimensions
- No viewport-aware rendering
- GUI coordinate system doesn't respect camera viewport rects

---

## Recommended Implementation: Horizontal Splitscreen

### Layout Configuration
```
+----------------------------------+
|        Player 1 Camera           |  Viewport: (0, 0.5, 1, 0.5)
|        (Top Half)                |  Top half of screen
+----------------------------------+
|        Player 2 Camera           |  Viewport: (0, 0, 1, 0.5)
|        (Bottom Half)             |  Bottom half of screen
+----------------------------------+
```

**Alternative: Vertical Split**
```
+----------------+----------------+
|   Player 1     |   Player 2    |
|   Camera       |   Camera      |
|   (Left)       |   (Right)     |
|   Viewport:    |   Viewport:   |
|   (0,0,0.5,1)  |   (0.5,0,0.5,1)|
+----------------+----------------+
```

---

## Implementation Plan

### Phase 1: Core Systems (3 New Files)

#### 1.1 SplitScreenManager.cs
**Location:** `/Assets/Scripts/Combat/Core/SplitScreenManager.cs`

**Responsibilities:**
- Manages 2 camera instances
- Configures viewport rects for each camera
- Tracks Player 1 and Player 2 references
- Provides camera lookup by player ID
- Handles splitscreen layout configuration

**Key Structure:**
```csharp
public class SplitScreenManager : MonoBehaviour
{
    public enum SplitScreenLayout { Horizontal, Vertical }

    [Header("Cameras")]
    public Camera player1Camera;
    public Camera player2Camera;

    [Header("Players")]
    public Transform player1Target;
    public Transform player2Target;

    [Header("Configuration")]
    public SplitScreenLayout layout = SplitScreenLayout.Horizontal;

    // Public API
    public Camera GetCameraForPlayer(int playerID);
    public Camera GetCameraForCharacter(Transform character);
    public int GetPlayerID(Transform character);
    public PlayerInputConfig GetInputConfigForPlayer(int playerID);

    void SetupSplitScreen();
    void SetupHorizontalSplit();
    void SetupVerticalSplit();
}
```

#### 1.2 PlayerInputConfig.cs
**Location:** `/Assets/Scripts/Combat/Core/PlayerInputConfig.cs`

**Responsibilities:**
- Stores input mappings for each player
- Provides input query methods that respect player ID
- Serializable data structure for Unity Inspector

**Player Input Mappings:**

**Player 1 (Default):**
```csharp
Movement:
- Forward: W
- Backward: S
- Left: A
- Right: D

Skills:
- Attack: Alpha1
- Defense: Alpha2
- Counter: Alpha3
- Smash: Alpha4
- Windmill: Alpha5
- RangedAttack: Alpha6
- Lunge: Alpha7

Actions:
- Cancel: Space
- Target: Tab
- Rest: X
- ExitCombat: Escape
- WeaponSwap: C

Camera:
- RotateLeft: Q
- RotateRight: E
```

**Player 2 (New):**
```csharp
Movement:
- Forward: UpArrow
- Backward: DownArrow
- Left: LeftArrow
- Right: RightArrow

Skills:
- Attack: Keypad1
- Defense: Keypad2
- Counter: Keypad3
- Smash: Keypad4
- Windmill: Keypad5
- RangedAttack: Keypad6
- Lunge: Keypad7

Actions:
- Cancel: RightShift
- Target: RightControl
- Rest: Period (.)
- ExitCombat: Backspace
- WeaponSwap: Comma (,)

Camera:
- RotateLeft: PageUp
- RotateRight: PageDown
```

**Key Structure:**
```csharp
[System.Serializable]
public class PlayerInputConfig
{
    [Header("Movement")]
    public KeyCode forward = KeyCode.W;
    public KeyCode backward = KeyCode.S;
    public KeyCode left = KeyCode.A;
    public KeyCode right = KeyCode.D;

    [Header("Skills")]
    public KeyCode attackKey = KeyCode.Alpha1;
    public KeyCode defenseKey = KeyCode.Alpha2;
    public KeyCode counterKey = KeyCode.Alpha3;
    public KeyCode smashKey = KeyCode.Alpha4;
    public KeyCode windmillKey = KeyCode.Alpha5;
    public KeyCode rangedAttackKey = KeyCode.Alpha6;
    public KeyCode lungeKey = KeyCode.Alpha7;

    [Header("Actions")]
    public KeyCode cancelKey = KeyCode.Space;
    public KeyCode targetKey = KeyCode.Tab;
    public KeyCode restKey = KeyCode.X;
    public KeyCode exitCombatKey = KeyCode.Escape;
    public KeyCode weaponSwapKey = KeyCode.C;

    [Header("Camera")]
    public KeyCode cameraRotateLeft = KeyCode.Q;
    public KeyCode cameraRotateRight = KeyCode.E;

    // Helper methods
    public bool GetKey(KeyCode key) => Input.GetKey(key);
    public bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);
    public bool GetKeyUp(KeyCode key) => Input.GetKeyUp(key);
}
```

#### 1.3 SplitScreenSetup.cs (Editor Tool)
**Location:** `/Assets/Scripts/Editor/SplitScreenSetup.cs`

**Menu Items:**
- `Combat/Testing/Setup 2-Player Splitscreen`
- `Combat/Testing/Setup 2P Co-op vs Enemies`
- `Combat/Testing/Setup 2P Versus`

**Actions:**
1. Creates SplitScreenManager GameObject
2. Creates 2 Camera instances with CameraControllers
3. Configures viewport rects (horizontal or vertical split)
4. Spawns Player 1 at (-3, 0, 0)
5. Spawns Player 2 at (3, 0, 0)
6. Assigns playerID to each character's components
7. Sets up input configs
8. Optionally spawns enemies

---

### Phase 2: Modify Existing Files (10 Files)

#### 2.1 CameraController.cs
**Changes Required:**
- Add `[SerializeField] private int playerID = 0;` field
- Add `private PlayerInputConfig inputConfig;` reference
- Modify `HandleInput()` to use `inputConfig` instead of hardcoded keys
- Modify `OnGUI()` to use viewport-relative coordinates
- Remove auto-find player logic (let SplitScreenManager assign target)

**Key Modifications:**
```csharp
[SerializeField] private int playerID = 0; // 0 for Player 1, 1 for Player 2
private PlayerInputConfig inputConfig;
private SplitScreenManager splitScreenManager;

void Awake()
{
    splitScreenManager = FindObjectOfType<SplitScreenManager>();
    if (splitScreenManager != null)
    {
        inputConfig = splitScreenManager.GetInputConfigForPlayer(playerID);
    }
}

void HandleInput()
{
    if (inputConfig == null) return;

    // Camera rotation
    if (Input.GetKey(inputConfig.cameraRotateLeft))
    {
        transform.RotateAround(target.position, Vector3.up, rotationSpeed * Time.deltaTime);
    }
    if (Input.GetKey(inputConfig.cameraRotateRight))
    {
        transform.RotateAround(target.position, Vector3.up, -rotationSpeed * Time.deltaTime);
    }
}

void OnGUI()
{
    // Viewport-aware GUI rendering
    Rect viewport = GetComponent<Camera>().pixelRect;

    GUILayout.BeginArea(new Rect(viewport.x, viewport.y, viewport.width, 200));
    // ... existing GUI code
    GUILayout.EndArea();
}
```

#### 2.2 SkillSystem.cs
**Changes Required:**
- Add `[SerializeField] private int playerID = 0;` field
- Add `private PlayerInputConfig inputConfig;` reference
- Replace all hardcoded KeyCode variables with `inputConfig` queries
- Modify `HandleSkillInput()` to use config-based input

**Pattern:**
```csharp
[SerializeField] private int playerID = 0;
private PlayerInputConfig inputConfig;
private SplitScreenManager splitScreenManager;

void Awake()
{
    splitScreenManager = FindObjectOfType<SplitScreenManager>();
    if (splitScreenManager != null)
    {
        inputConfig = splitScreenManager.GetInputConfigForPlayer(playerID);
    }
}

void HandleSkillInput()
{
    if (inputConfig == null) return;

    // N+1 early press detection
    if (weaponController != null && weaponController.CurrentStunProgress > 0f)
    {
        if (Input.GetKeyDown(inputConfig.smashKey))
        {
            // ... existing N+1 logic
        }
        else if (Input.GetKeyDown(inputConfig.windmillKey))
        {
            // ... existing N+1 logic
        }
        // ... etc
    }

    // Normal input handling
    if (Input.GetKeyDown(inputConfig.attackKey))
    {
        // ... existing attack logic
    }
    // ... etc
}
```

#### 2.3 MovementController.cs
**Changes Required:**
- Add `[SerializeField] private int playerID = 0;` field
- Add `private PlayerInputConfig inputConfig;` reference
- Replace hardcoded KeyCode W/A/S/D with `inputConfig` queries
- Modify `HandleMovementInput()` to use config-based input

#### 2.4 CombatController.cs
**Changes Required:**
- Add `[SerializeField] private int playerID = 0;` field
- Add `private PlayerInputConfig inputConfig;` reference
- Replace Tab key (target) with `inputConfig.targetKey`
- Replace Escape key (exit combat) with `inputConfig.exitCombatKey`
- Replace X key (rest) with `inputConfig.restKey`

#### 2.5 UI Components (HealthBarUI, StaminaBarUI, KnockdownMeterBarUI, SkillIconDisplay, StatusEffectDisplay)

**Changes Required for All UI:**
- Add `private Camera assignedCamera;` field
- Replace `Camera.main` with `assignedCamera`
- Implement viewport-aware coordinate calculation
- Get camera from SplitScreenManager based on character

**Pattern for Viewport-Aware OnGUI:**
```csharp
private Camera assignedCamera;
private SplitScreenManager splitScreenManager;

void Awake()
{
    splitScreenManager = FindObjectOfType<SplitScreenManager>();
    if (splitScreenManager != null)
    {
        assignedCamera = splitScreenManager.GetCameraForCharacter(transform);
    }
    else
    {
        assignedCamera = Camera.main; // Fallback for single-player
    }
}

void OnGUI()
{
    if (assignedCamera == null) return;

    Vector3 worldPos = transform.position + Vector3.up * heightOffset;

    // Convert world position to viewport coordinates
    Vector3 viewportPoint = assignedCamera.WorldToViewportPoint(worldPos);

    // Skip if behind camera or too far
    if (viewportPoint.z < 0 || viewportPoint.z > maxDistance) return;

    // Get camera's screen rect
    Rect viewport = assignedCamera.pixelRect;

    // Convert viewport point to screen coordinates within camera's rect
    Vector3 screenPos = new Vector3(
        viewport.x + viewportPoint.x * viewport.width,
        viewport.y + (1 - viewportPoint.y) * viewport.height,
        viewportPoint.z
    );

    // Bounds check
    if (screenPos.x < viewport.x || screenPos.x > viewport.x + viewport.width ||
        screenPos.y < viewport.y || screenPos.y > viewport.y + viewport.height)
    {
        return; // Off-screen in this viewport
    }

    // Draw UI at adjusted screen position
    Rect barRect = new Rect(screenPos.x - barWidth / 2, screenPos.y, barWidth, barHeight);
    GUI.DrawTexture(barRect, barTexture);
}
```

#### 2.6 CharacterInfoDisplay.cs
**Changes Required:**
- Modify to create separate UI instances for each viewport
- Handle camera assignment for child UI components
- Ensure UI components get correct camera reference

---

### Phase 3: Testing & Refinement

#### Testing Checklist:
- [ ] Two cameras render correctly with viewport rects
- [ ] Player 1 input works (WASD, 1-7, Q/E camera)
- [ ] Player 2 input works (Arrows, Numpad 1-7, PageUp/PageDown camera)
- [ ] Input isolation (P1 keys don't affect P2 and vice versa)
- [ ] Health bars render in correct viewports
- [ ] Stamina bars render in correct viewports
- [ ] Knockdown meters render in correct viewports
- [ ] Skill icons render in correct viewports
- [ ] N+1 timing indicator renders in correct viewports
- [ ] Status effects render in correct viewports
- [ ] Camera follows correct player in each viewport
- [ ] Camera rotation independent per player
- [ ] UI doesn't clip at viewport boundaries
- [ ] Performance acceptable with 2 players + enemies

---

## Technical Challenges & Solutions

### Challenge 1: OnGUI Viewport Handling
**Problem:** OnGUI doesn't natively respect camera viewport rects

**Solution:** Manual coordinate transformation
```csharp
// Get camera's pixel rectangle on screen
Rect viewport = camera.pixelRect;

// Convert world → viewport → screen (viewport-relative)
Vector3 viewportPoint = camera.WorldToViewportPoint(worldPos);
Vector3 screenPos = new Vector3(
    viewport.x + viewportPoint.x * viewport.width,
    viewport.y + (1 - viewportPoint.y) * viewport.height,
    viewportPoint.z
);
```

### Challenge 2: Camera.main Replacement
**Problem:** All UI uses `Camera.main` which returns first active camera

**Solution:** Camera assignment via SplitScreenManager
```csharp
Camera GetAssignedCamera()
{
    if (splitScreenManager != null)
    {
        return splitScreenManager.GetCameraForCharacter(transform);
    }
    return Camera.main; // Fallback for single-player
}
```

### Challenge 3: Input Conflicts
**Problem:** Arrow Keys used by Camera1 rotation → Player2 movement

**Solution:** Separate camera controls per player
- Player 1 camera: Q/E
- Player 2 camera: PageUp/PageDown

### Challenge 4: UI Culling
**Problem:** UI might render in wrong viewport or get culled incorrectly

**Solution:** Bounds checking per viewport
```csharp
// Check if screen position is within camera's viewport
if (screenPos.x < viewport.x || screenPos.x > viewport.x + viewport.width ||
    screenPos.y < viewport.y || screenPos.y > viewport.y + viewport.height)
{
    return; // Don't render - outside this viewport
}
```

---

## Implementation Options

### Option A: Full Implementation (Recommended)
**Pros:**
- Complete splitscreen system with all features
- Proper input isolation
- Viewport-aware UI rendering
- Best for serious testing needs

**Cons:**
- ~10 file modifications + 3 new files
- More time investment
- Need to test thoroughly

**Estimated Effort:** 4-6 hours

### Option B: Minimal Proof of Concept
**Pros:**
- Quick validation of approach
- Only creates core manager + cameras
- Can test viewport rendering works

**Cons:**
- Input won't work (still hardcoded)
- UI won't render correctly (still uses Camera.main)
- Not usable for actual testing

**Estimated Effort:** 1 hour

### Option C: Wrapper Components
**Pros:**
- Doesn't modify existing files
- Preserves single-player completely
- Splitscreen as "opt-in" addon
- Safer approach

**Cons:**
- More complex architecture
- Duplicate logic in wrappers
- Less elegant solution

**Estimated Effort:** 5-7 hours

---

## Future Enhancements

### Performance Optimization
- Convert OnGUI to Unity UI Canvas (World Space)
- Use Screen Space Camera canvases per player
- Implement camera culling masks
- Optimize rendering with object pooling

### Advanced Features
- Dynamic splitscreen (switch between layouts)
- 3-4 player support
- Gamepad support via new Input System
- Per-player audio listeners
- Minimap per player
- Split audio (positional audio per viewport)

### Quality of Life
- Save/load splitscreen configurations
- Per-player camera settings (FOV, distance)
- Quick player swap (switch camera targets)
- Spectator mode (watch both players)

---

## File Summary

### New Files (3):
1. `/Assets/Scripts/Combat/Core/SplitScreenManager.cs`
2. `/Assets/Scripts/Combat/Core/PlayerInputConfig.cs`
3. `/Assets/Scripts/Editor/SplitScreenSetup.cs`

### Modified Files (10):
1. `/Assets/Scripts/Combat/Systems/CameraController.cs`
2. `/Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`
3. `/Assets/Scripts/Combat/Core/MovementController.cs`
4. `/Assets/Scripts/Combat/Core/CombatController.cs`
5. `/Assets/Scripts/Combat/UI/HealthBarUI.cs`
6. `/Assets/Scripts/Combat/UI/StaminaBarUI.cs`
7. `/Assets/Scripts/Combat/UI/KnockdownMeterBarUI.cs`
8. `/Assets/Scripts/Combat/UI/SkillIconDisplay.cs`
9. `/Assets/Scripts/Combat/UI/StatusEffectDisplay.cs`
10. `/Assets/Scripts/Combat/UI/CharacterInfoDisplay.cs`

---

## Usage Instructions (Once Implemented)

### Quick Setup:
1. Open Unity Editor
2. Go to `Combat > Testing > Setup 2-Player Splitscreen`
3. Choose layout (Horizontal or Vertical)
4. Press Play

### Manual Setup:
1. Create empty GameObject named "SplitScreenManager"
2. Add SplitScreenManager component
3. Create 2 Camera objects with CameraController components
4. Assign cameras to SplitScreenManager
5. Spawn 2 player characters
6. Assign Player 1 to camera1, Player 2 to camera2
7. Set playerID on each character's components (0 for P1, 1 for P2)
8. Configure input mappings in SplitScreenManager

### Controls:
**Player 1:**
- Move: WASD
- Skills: 1-7
- Camera: Q/E

**Player 2:**
- Move: Arrow Keys
- Skills: Numpad 1-7
- Camera: PageUp/PageDown

---

## Notes
- This plan assumes continued use of OnGUI for UI rendering
- For production, consider migrating to Unity UI Canvas system
- Splitscreen is primarily for testing; single-player should remain primary mode
- Performance should be monitored with 2 players + multiple enemies
- Input System can be upgraded to new Unity Input System in future

---

## Status
**Plan Created:** 2025-11-24
**Status:** Not yet implemented
**Priority:** Medium (testing enhancement)
