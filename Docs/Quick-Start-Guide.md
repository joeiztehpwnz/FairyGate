# FairyGate Combat System - Quick Start Guide

**Last Updated:** November 14, 2025
**Purpose:** Step-by-step guide to set up and test the combat system

---

## 🚀 Quick Setup (Recommended)

### Method 1: Complete Sandbox Setup (Fastest)

This creates everything you need in one click!

1. **Open Unity Editor**
2. **Create/Open a Scene**
   - File → New Scene
   - Or open an existing empty scene

3. **Run the Complete Setup**
   - Go to menu: **Combat → Complete Scene Setup → Testing Sandbox**
   - Click **"Yes"** in the dialog
   - Wait for progress bar to complete (~3-5 seconds)

4. **Press Play!** ▶️

**What This Creates:**
- ✅ All required managers (CombatUpdateManager, CombatInteractionManager, GameManager)
- ✅ Environment (ground plane, camera, lighting)
- ✅ Player character with full combat system
- ✅ All weapon types (Sword, Spear, Dagger, Mace, Bow)
- ✅ All equipment sets (Fortress, Windrunner, Wanderer, Berserker)
- ✅ UI system (health/stamina/skill bars)

---

## 🎮 Controls & Gameplay

### Movement
- **W/A/S/D** - Character movement
- **Arrow Keys** - Camera control
  - Left/Right: Rotate camera
  - Up/Down: Zoom in/out

### Combat Skills
- **1** - Attack (Basic melee attack)
- **2** - Defense (Block incoming attacks)
- **3** - Counter (Reflect attacks back)
- **4** - Smash (Break through Defense)
- **5** - Windmill (AoE spin attack)
- **6** - Ranged Attack (Bow attack - requires aiming)
- **7** - Lunge (Forward dash attack)

### Other Controls
- **Space** - Cancel current skill
- **X** - Rest (Stamina regeneration)
- **Tab** - Cycle through enemies (targeting)
- **Esc** - Exit combat mode
- **R** - Reset combat state
- **C** - Swap weapons (if multiple equipped)

### Combat Mechanics
- **Charging**: Hold skill key to charge (progress bar appears)
- **Execution**: Release skill key when fully charged
- **Stamina**: Each skill costs stamina (green bar)
- **Health**: Red bar - don't let it reach zero!
- **Knockdown Meter**: Orange bar - fills when taking hits

---

## 👾 Spawning Enemies

Once the sandbox is set up, you can spawn different enemy types:

### Enemy Archetypes

**Access via:** Combat → Spawn Enemy → [Enemy Type]

1. **Soldier** (Balanced Fighter)
   - Moderate stats across the board
   - Uses Attack, Defense, and Smash
   - Good for testing basic combat

2. **Berserker** (Glass Cannon)
   - High strength, low defense
   - Aggressive attack patterns
   - Uses Attack and Smash heavily

3. **Guardian** (Tank)
   - High health and defense
   - Uses Defense and Counter
   - Slow but durable

4. **Assassin** (Speedster)
   - High speed and dexterity
   - Quick attacks and evasion
   - Uses Counter opportunistically

5. **Archer** (Ranged)
   - Ranged attack specialist
   - Keeps distance from player
   - Uses RangedAttack and retreat tactics

**Tip:** Enemies spawn at incrementing positions. You can spawn multiple enemies to test group combat!

---

## 🛠️ Manual Setup (Advanced)

If you want to set up the scene manually or customize the setup:

### Step 1: Create Managers

**Combat → Create Manager → [Manager Type]**

Required managers:
- **CombatUpdateManager** - Centralized update loop (performance optimization)
- **CombatInteractionManager** - Handles skill interactions and conflicts
- **GameManager** - Overall game state management

Optional managers:
- **AICoordinator** - Auto-created when first enemy spawns

### Step 2: Create Environment

You can create environment elements individually:

**Combat → Create Environment → [Element]**
- Ground Plane
- Main Camera
- Directional Light

Or use the environment builder programmatically:
```csharp
var envBuilder = new SceneEnvironmentBuilder();
envBuilder.CreateEnvironment();
```

### Step 3: Create Characters

#### Player Character:
**Combat → Create Character → Player**

This creates a player at position (-3, 0, 0) with:
- All combat components (CombatController, SkillSystem, HealthSystem, etc.)
- Default stats and equipment
- Movement controller
- UI display

#### Enemy Character:
**Combat → Create Character → Enemy**

Or use the spawn menu for pre-configured archetypes:
**Combat → Spawn Enemy → [Archetype]**

### Step 4: Create Assets (If Needed)

**Combat → Create Assets → [Asset Type]**

Available asset creators:
- Character Stats
- Weapon Data
- Equipment Sets

Assets are automatically created during sandbox setup and cached for reuse.

---

## 🎨 UI & Visualization

### Character Info Display

Each character shows:
- **Top Bar (Red):** Health
- **Middle Bar (Green):** Stamina
- **Bottom Bar (Orange):** Knockdown Meter
- **Icon:** Current skill being charged/executed
- **Progress Bar:** Charge progress (when charging)

### Enemy Targeting

- **White Outline:** Enemy is currently targeted by player
- **Tab Key:** Cycle through available enemies

### Skill Icons

Visual indicators above characters:
- ⚔️ Attack
- 🛡️ Defense
- ↩️ Counter
- 🔨 Smash
- 🌪️ Windmill
- 🏹 Ranged Attack
- ⚡ Lunge

**Pulsing:** Skill is charging
**Solid:** Skill is executing

---

## 🔍 Debugging & Configuration

### Combat Logger

Access the logging configuration window:
**Combat → Debug → Combat Logger Configuration**

**Features:**
- Enable/disable log categories (AI, Combat, Skills, etc.)
- Set minimum log level (Debug, Info, Warning, Error)
- Color-coded console output
- Real-time toggling

**Recommended Settings for Testing:**
- Enable: AI, Combat, Skills, System
- Disable: Movement, UI (too verbose)
- Level: Info

### Debug Logs

Many systems have debug logging that can be enabled:
- Look for `enableDebugLogs` serialized field in Inspector
- Set to `true` for detailed logging
- Set to `false` for clean console

### Scene Inspection

**Key GameObjects to Inspect:**
- **Player** - Check all combat components
- **CombatInteractionManager** - See pending skill executions
- **AICoordinator** - Monitor attack slots and formation
- **Enemy_[Type]_[N]** - Individual enemy state and AI

---

## 📊 Testing Scenarios

### Basic Combat Test
1. Set up sandbox
2. Spawn 1 Soldier enemy
3. Test Attack vs Defense interaction
4. Test Smash breaking Defense
5. Test Counter reflecting attacks

### Skill Charging Test
1. Hold skill key (1-7)
2. Watch charge bar fill
3. Release when full
4. Observe skill execution

### Stamina System Test
1. Use multiple skills rapidly
2. Watch stamina deplete (green bar)
3. Press X to Rest
4. Observe faster stamina regeneration

### Knockdown System Test
1. Take multiple hits from enemy
2. Watch knockdown meter fill (orange bar)
3. At 50%: Knockback triggered
4. At 100%: Knocked down (stun)

### Multi-Enemy Combat
1. Spawn 3-4 enemies of different types
2. Use Tab to cycle targets
3. Test AoE skills (Windmill)
4. Observe AI coordination (formation slots, attack timing)

### Equipment System Test
1. Open Player GameObject in Inspector
2. Find EquipmentManager component
3. Try different equipment sets:
   - Fortress (Tank) - High HP/Defense
   - Windrunner (Speed) - High Speed/Dex
   - Wanderer (Balanced) - Moderate all stats
   - Berserker (Glass Cannon) - High Str, Low Def
4. Observe stat changes in combat

---

## ⚠️ Common Issues & Solutions

### Issue: Nothing happens when I press Play
**Solution:** Make sure you ran the Complete Sandbox Setup first
- Combat → Complete Scene Setup → Testing Sandbox

### Issue: Player doesn't move
**Solution:** Click on the Game view to give it focus
- Movement only works when Game view is focused

### Issue: Enemies don't attack
**Solution:**
1. Check AICoordinator exists in scene
2. Check enemy has PatternExecutor component
3. Check enemy has assigned pattern asset
4. Enable debug logs on PatternExecutor to see AI decisions

### Issue: Skills don't execute
**Solution:**
1. Check stamina level (green bar) - may be depleted
2. Verify you're holding key long enough to charge
3. Check console for skill execution logs
4. Enable debug logs on SkillSystem

### Issue: Combat feels slow
**Solution:**
1. Check character stats - may have low Speed stat
2. Try Windrunner equipment set for faster gameplay
3. Adjust CombatConstants.cs values if needed

### Issue: Camera is stuck
**Solution:**
1. Use Arrow Keys to control camera
2. Check CameraController is attached to Main Camera
3. Verify Main Camera has "MainCamera" tag

### Issue: No UI bars visible
**Solution:**
1. Check CharacterInfoDisplay component is on Player
2. Verify child UI components are enabled
3. Check Main Camera exists and is active

---

## 🎯 Recommended First Steps

For your first test session:

1. **Basic Setup**
   ```
   1. New Scene
   2. Combat → Complete Scene Setup → Testing Sandbox
   3. Press Play
   ```

2. **Movement Test**
   ```
   - Move around with WASD
   - Rotate camera with Arrow Keys
   - Get familiar with controls
   ```

3. **Single Enemy Combat**
   ```
   - Combat → Spawn Enemy → Soldier
   - Use Tab to target
   - Try Attack (1) - hold and release
   - Try Defense (2) when enemy attacks
   - Observe skill interactions
   ```

4. **Skill Variety**
   ```
   - Try all 7 skills
   - Watch charge bars
   - Observe stamina costs
   - Learn skill timings
   ```

5. **Multi-Enemy**
   ```
   - Spawn 2-3 more enemies
   - Test targeting with Tab
   - Try Windmill (5) for AoE
   - Observe AI coordination
   ```

---

## 📚 Next Steps

After testing basic combat:

1. **Explore AI Patterns**
   - Check pattern assets in `Assets/Data/AI/Patterns/`
   - Inspect PatternExecutor component on enemies
   - Modify pattern parameters and test

2. **Customize Equipment**
   - Examine equipment assets in `Assets/Data/Equipment/`
   - Create custom stat combinations
   - Test different playstyles

3. **Adjust Combat Balance**
   - Edit `CombatConstants.cs` for global tuning
   - Modify character stats in Inspector
   - Test different difficulty levels

4. **Visual Customization**
   - Adjust UI colors in bar components
   - Change telegraph visual settings
   - Customize skill icons

---

## 🔗 Additional Resources

**Documentation:**
- `/Docs/Refactoring-Progress.md` - System architecture overview
- `/Docs/TODO-Tracking.md` - Future enhancements and polish items
- `/Docs/Classic-Mabinogi-Combat-Deep-Dive.md` - Combat design philosophy
- `/Docs/AI-Pattern-System-Architecture.md` - AI system details

**Editor Tools:**
- Combat menu - All setup and spawn tools
- Combat Logger Configuration - Debug logging control

**Key Scripts to Review:**
- `SkillSystem.cs` - Core skill execution
- `PatternExecutor.cs` - AI behavior
- `CombatInteractionManager.cs` - Skill interactions
- `CombatConstants.cs` - Global balance tuning

---

**Ready to play! Press that Play button and enjoy testing the combat system!** 🎮

If you encounter any issues not covered here, check the console logs with Combat Logger enabled for detailed debugging information.
