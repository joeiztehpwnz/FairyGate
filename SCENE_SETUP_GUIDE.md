# Complete Combat Scene Setup Guide

## Overview

This guide explains how to use the **CompleteCombatSceneSetup** editor tool to quickly create fully configured combat testing environments in Unity.

---

## Quick Start

### Testing Sandbox (Recommended)

The fastest way to get a complete testing environment:

1. **Open Unity** and load your FairyGate project
2. **Navigate to**: `Combat → Complete Scene Setup → Testing Sandbox`
3. **Click** to create the scene
4. **Press Play** to start testing immediately!

This creates:
- ✅ All managers (CombatInteractionManager, GameManager)
- ✅ Environment (ground, camera, lighting)
- ✅ Player character with default equipment
- ✅ Enemy with TestRepeaterAI (cycles through all skills)
- ✅ All testing UI components
- ✅ Health/Stamina bars
- ✅ Equipment selector (`[` `]` bracket hotkeys)
- ✅ Skill selector (F1-F6 hotkeys)
- ✅ Debug visualizers

---

## Menu Options

### 1. Testing Sandbox

**Path**: `Combat → Complete Scene Setup → Testing Sandbox`

**What it creates**:
- Full combat environment
- Player at position (-3, 0, 0)
- Enemy at position (3, 0, 0) with TestRepeaterAI
- Complete UI system with all testing tools
- All required ScriptableObject assets

**Best for**: Comprehensive testing, skill interaction testing, equipment testing

---

### 2. Quick 1v1 Setup

**Path**: `Combat → Complete Scene Setup → Quick 1v1 Setup`

**What it creates**:
- Minimal environment
- Player vs Enemy with KnightAI (8-second pattern)
- No testing UI (just basic combat)
- Essential managers only

**Best for**: Simple pattern-based combat testing, learning AI patterns

---

### 3. Clear All Combat Objects

**Path**: `Combat → Complete Scene Setup → Clear All Combat Objects`

**What it does**:
- Removes all combat characters
- Removes managers
- Removes UI canvas
- Leaves environment intact

**Best for**: Starting fresh without creating a new scene

---

## Created Assets

The setup script creates the following assets in your project:

### Character Stats

**Location**: `Assets/Data/Characters/`

- `TestPlayer_Stats.asset` - Player stats (Str: 10, Dex: 8, Int: 6, Foc: 8, Def: 5, VIT: 12)
- `TestEnemy_Stats.asset` - Enemy stats (Str: 12, Dex: 6, Int: 5, Foc: 6, Def: 6, VIT: 10)

### Weapons

**Location**: `Assets/Data/Weapons/`

- `TestSword.asset` - Balanced weapon (Range: 1.5, Damage: 10)
- `TestSpear.asset` - Long-range weapon (Range: 2.5, Damage: 8)
- `TestDagger.asset` - Fast weapon (Range: 1.0, Damage: 6, High Speed)
- `TestMace.asset` - Heavy weapon (Range: 1.2, Damage: 15, Low Speed)

### Equipment

**Location**: `Assets/Data/Equipment/`

The script verifies that equipment directories exist. If you need to create equipment, use the existing assets:

**Armor**:
- HeavyPlatemail - Tank armor
- LeatherTunic - Speed armor
- ClothRobes - Glass Cannon armor
- ChainMail - Balanced armor

**Accessories**:
- GuardianRing - Defense bonus
- SwiftBoots - Speed bonus
- PowerGauntlets - Strength bonus
- MeditationAmulet - Focus bonus

**Sets**:
- Fortress_TankSet - High defense build
- Windrunner_SpeedSet - High speed build
- Berserker_GlassCannonSet - High damage build
- Wanderer_BalancedSet - Balanced build

---

## Scene Components

### Managers

**CombatInteractionManager**
- Handles all skill interactions
- Processes damage calculations
- Debug logging enabled

**GameManager**
- Manages game state
- Handles scene-level logic

### Environment

**Ground**
- 30x30 unit plane
- Light greenish-gray color
- Positioned at world origin

**Main Camera**
- Position: (0, 10, -8)
- Rotation: 50° downward angle
- FOV: 60°
- Optimized for combat viewing

**Directional Light**
- Soft shadows enabled
- White light
- Angled for good scene illumination

### Characters

Each character includes:
- CharacterController (for movement and collision)
- CombatController (main combat facade)
- HealthSystem
- StaminaSystem
- StatusEffectManager
- SkillSystem
- WeaponController
- MovementController
- KnockdownMeterTracker
- AccuracySystem (for ranged attacks)
- CombatDebugVisualizer (on-screen debug display)
- EquipmentManager (with `[` `]` bracket equipment switching)

**Player**:
- Blue capsule visual
- Keyboard controlled (WASD + number keys)
- Position: (-3, 0, 0)

**Enemy**:
- Red capsule visual
- AI controlled (TestRepeaterAI by default)
- Position: (3, 0, 0)
- Can be controlled with F1-F6 hotkeys

### UI System

**Health/Stamina Bars** (OnGUI-based):
- Player bars at top-left (10, 10)
- Enemy bars below player bars (10, 90)
- Color-coded (green/red for health, blue/red for stamina)
- Smooth animated transitions
- Dynamic color changes based on levels
- Shows numerical values (e.g., "Health: 120/150")
- Resting indicator for stamina

**Testing UI Manager**: `TestingUI_Manager`
- TestSkillSelector (invisible, hotkey-based F1-F6)
- No visual UI elements, all hotkey-driven

**Debug Visualizers**:
- On-screen combat state display (per character)
- Shows current skill, charge progress, combat state
- Equipment display (shows current equipment stats)

---

## Controls

### Player Combat

| Key | Action |
|-----|--------|
| WASD | Move |
| 1 | Attack |
| 2 | Defense |
| 3 | Counter |
| 4 | Smash |
| 5 | Windmill |
| 6 | Ranged Attack |
| Space | Cancel Skill |
| X | Rest (2x stamina regen) |
| Tab | Cycle Target |
| Esc | Exit Combat |
| R | Reset Combat |

### Testing Hotkeys

| Key | Function |
|-----|----------|
| F1 | Force Enemy Attack |
| F2 | Force Enemy Defense |
| F3 | Force Enemy Counter |
| F4 | Force Enemy Smash |
| F5 | Force Enemy Windmill |
| F6 | Force Enemy Ranged Attack |
| `[` or PgUp | Previous Equipment Set |
| `]` or PgDn | Next Equipment Set |
| `\` or Home | Remove All Equipment |
| F12 | Reset Enemy AI |

---

## Equipment Sets

Switch between equipment sets during runtime using **`[` `]` brackets** (or PgUp/PgDn):

### 1. Fortress (Tank Build)
- **Armor**: Heavy Platemail
- **Accessory**: Guardian Ring
- **Bonuses**: +30 HP, +15 Defense, -1 Speed
- **Strategy**: High survivability, can absorb hits

### 2. Windrunner (Speed Build)
- **Armor**: Leather Tunic
- **Accessory**: Swift Boots
- **Bonuses**: +2 Speed, +5 Dexterity
- **Strategy**: Fast movement, quick skill execution

### 3. Wanderer (Balanced Build)
- **Armor**: Chain Mail
- **Accessory**: Meditation Amulet
- **Bonuses**: Moderate HP, Defense, and Focus
- **Strategy**: Well-rounded stats, adaptable

### 4. Berserker (Glass Cannon Build)
- **Armor**: Cloth Robes
- **Accessory**: Power Gauntlets
- **Bonuses**: +10 Strength, -5 Defense
- **Strategy**: High damage output, low survivability

---

## AI Types

The enemy can use different AI types:

### TestRepeaterAI (Default in Testing Sandbox)
- Cycles through all 6 skills systematically
- Perfect for testing all skill interactions
- 2-second delay between skills
- Use F1-F6 to override and test specific interactions

### KnightAI (Default in Quick 1v1)
- 8-second pattern cycle:
  1. Charge Defense (1s)
  2. Wait Defensively (3s) - **Vulnerable window**
  3. Cancel Defense (0.5s)
  4. Charge Smash (1.5s)
  5. Execute Smash (0.5s) - **Danger window**
  6. Recovery (1.5s) - **Counterattack window**
- Teaches pattern recognition
- Predictable but challenging

### SimpleTestAI
- Random skill selection
- Basic approach behavior
- Good for chaos testing

---

## Workflow Examples

### Testing Skill Interactions

1. Create **Testing Sandbox**
2. Press Play
3. Enemy will cycle through skills automatically (TestRepeaterAI)
4. Use F1-F6 to force specific skills
5. Watch debug visualizer for interaction results
6. Check console for detailed logs

**Example**: Testing Counter vs Attack
1. Enemy performs Attack (or force with F1)
2. You use Counter (press 3)
3. Debug shows: "Counter successfully reflected Attack"
4. Attacker takes reflected damage

### Testing Equipment Builds

1. Create **Testing Sandbox**
2. Press Play
3. Press Esc to exit combat (equipment can only change outside combat)
4. Press `]` to cycle to "Berserker" set
5. Re-engage combat (Tab) and notice increased damage output
6. Press Esc again, then `[` to cycle to "Fortress" set
7. Re-engage and notice reduced damage taken
8. Compare different builds against same enemy patterns

### Learning AI Patterns

1. Create **Quick 1v1 Setup**
2. Press Play
3. Watch KnightAI's 8-second pattern
4. Identify vulnerable windows:
   - After Defense cancel (0.5s)
   - During Smash charge (1.5s)
   - During Recovery (1.5s)
5. Practice counterattacking during windows
6. Use Smash or Windmill when enemy charges Defense

### Testing Ranged Attacks

1. Create **Testing Sandbox**
2. Press Play
3. Press 6 to use Ranged Attack
4. Hold aim to build accuracy
5. Watch accuracy percentage in debug visualizer
6. Release to fire
7. Yellow trail = hit, Gray trail = miss
8. Use F6 to force enemy ranged attacks
9. Practice dodging with WASD movement

---

## Debug Information

### On-Screen Debug Visualizer

Each character shows:
- Current Health / Max Health
- Current Stamina / Max Stamina
- Combat State (Idle/InCombat)
- Current Skill (Attack/Defense/etc.)
- Skill State (Uncharged/Charging/Active/etc.)
- Charge Progress (0-100%)
- Accuracy Info (for Ranged Attack)
- Equipment Info (current set)

### Console Logs

The system logs:
- Skill executions
- Interaction resolutions
- Damage calculations
- Status effect applications
- Equipment changes
- AI decision making

**Tip**: Enable/disable specific system logs by editing the component's `enableDebugLogs` field in the Inspector.

---

## Customization

### Changing Enemy AI

1. Select Enemy in Hierarchy
2. In Inspector, disable current AI component
3. Add desired AI component:
   - `Add Component → KnightAI`
   - `Add Component → SimpleTestAI`
   - `Add Component → TestRepeaterAI`

### Changing Weapons

1. Select character in Hierarchy
2. Find WeaponController component
3. Change `Weapon Data` field to desired weapon:
   - TestSword
   - TestSpear
   - TestDagger
   - TestMace

### Changing Stats

1. Navigate to `Assets/Data/Characters/`
2. Select character stats asset
3. Modify values in Inspector
4. Changes apply immediately in Play mode

### Adjusting UI

**Health/Stamina Bar Colors**:
1. Select character in Hierarchy (Player or Enemy)
2. Find HealthBarUI or StaminaBarUI component
3. Adjust color fields in Inspector

**UI Position**:
1. Select character in Hierarchy
2. Find HealthBarUI or StaminaBarUI component
3. Modify "Bar Position" field (Vector2)

---

## Troubleshooting

### "No CombatInteractionManager found"
- Run the scene setup again
- Check that CombatInteractionManager exists in Hierarchy
- It should be at root level, not inside another GameObject

### Health/Stamina bars not updating
- Check that HealthBarUI/StaminaBarUI components are on the character GameObjects
- Verify they have correct references to HealthSystem/StaminaSystem
- Ensure Health System and Stamina System are on the character
- Look for errors in Console

### F1-F6 hotkeys not working
- Ensure TestSkillSelector exists in scene (TestingUI_Manager GameObject)
- Check Console for "TestSkillSelector not found" warnings
- Ensure enemy has a valid AI component

### `[` `]` bracket equipment switching not working
- Ensure character has EquipmentManager and TestEquipmentSelector components
- Check that TestEquipmentSelector's `equipmentPresets` array has equipment sets assigned
- Equipment can only change outside combat (press Esc first)
- Alternative keys: Try PgUp/PgDn instead of brackets

### Character falls through ground
- Ensure Ground has a Mesh Collider
- Check that character has CharacterController component
- Verify gravity is enabled in CharacterController

### AI not engaging
- Check that characters are within engagement range (default: 6.0 units)
- Ensure enemy has an AI component enabled
- Check that characters have CombatController components

---

## Advanced Usage

### Creating Multiple Enemies

```csharp
// You can manually duplicate the Enemy in Hierarchy
// Or modify the script to create multiple enemies
```

1. Duplicate Enemy GameObject in Hierarchy
2. Reposition: move to different location
3. Change AI: assign different AI component
4. Change Weapon: assign different weapon type
5. Each enemy will independently engage the player

### Custom Equipment Sets

1. Create new EquipmentSet asset:
   - `Assets → Create → Combat → Equipment Set`
2. Assign armor and accessory
3. Add to character's EquipmentManager `availableSets` array
4. Add to character's TestEquipmentSelector `equipmentPresets` array
5. Now accessible via `[` `]` brackets (or PgUp/PgDn)

### Recording Combat Tests

1. Use Unity Recorder package
2. Set up scene with Testing Sandbox
3. Configure specific test scenario (equipment, AI type)
4. Start recording
5. Execute test sequence
6. Review recording for analysis

---

## Performance Notes

- **Scene Complexity**: Testing Sandbox is lightweight, runs at 60+ FPS
- **Debug Visualizers**: Can be disabled per-character if needed
- **Multiple Enemies**: System supports 2-10 enemies (tested up to 5 simultaneously)
- **Asset Loading**: All assets are created once and reused

---

## Next Steps

After setting up your scene:

1. **Learn the basics**: Start with Quick 1v1 to learn KnightAI pattern
2. **Test interactions**: Use Testing Sandbox with F1-F6 to test all 17 interactions
3. **Try builds**: Use `[` `]` brackets to compare Tank vs Speed vs Glass Cannon
4. **Experiment**: Create custom equipment sets and weapons
5. **Extend**: Add new AI patterns using PatternedAI base class

---

## Related Documentation

- `AI_PATTERN_SYSTEM_DESIGN.md` - AI pattern design principles
- `EQUIPMENT_SYSTEM_DESIGN.md` - Equipment system architecture
- `SKILL_TEST_ENVIRONMENT_USAGE.md` - Detailed skill testing guide
- `COMBAT_SYSTEM_REFACTORING_PLAN.md` - Future improvements

---

## Support

If you encounter issues:

1. Check Unity Console for error messages
2. Verify all required assets exist in `Assets/Data/`
3. Try using "Clear All Combat Objects" then recreate
4. Check that all scripts compile without errors
5. Review this guide's Troubleshooting section

---

**Created**: 2025-10-19
**Version**: 1.0
**Compatible with**: Unity 2023.2.20f1+
