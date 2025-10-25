# Skill Test Environment - Quick Start Guide

## Overview
The Skill Test Environment allows you to control enemy AI behavior in real-time, making it easy to thoroughly test all skill interactions in the combat system.

## Quick Setup (Fastest Method)

### Option 1: Menu Quick Setup (Recommended)
1. Open your test scene in Unity
2. Go to **Tools → Combat → Test Environment → Quick Setup: Player vs Test Enemy**
3. Click "Yes" in the dialog
4. Press Play!

### Option 2: Manual Setup
1. Open your test scene
2. Select the enemy GameObject
3. Go to **Tools → Combat → Test Environment → Setup Test Enemy**
4. Press Play!

## How to Use

### Hotkeys (Available in Play Mode)
Once in play mode, use these hotkeys to instantly change the enemy's repeating skill:

**Skill Selection:**
- **F1** - Enemy repeats Attack
- **F2** - Enemy repeats Defense
- **F3** - Enemy repeats Counter
- **F4** - Enemy repeats Smash
- **F5** - Enemy repeats Windmill
- **F6** - Enemy repeats RangedAttack

**Quick Settings:**
- **F7** - Toggle Defensive Maintenance (auto-reenter Defense/Counter)
- **F8** - Toggle Infinite Stamina
- **F9** - Toggle Movement (enable/disable AI movement)
- **+ (Plus)** - Increase repeat delay by 0.5s
- **- (Minus)** - Decrease repeat delay by 0.5s

**Equipment Control (NEW):**
- **]** (Right Bracket) - Next Equipment Preset (cycle to next build)
- **[** (Left Bracket) - Previous Equipment Preset (cycle to previous build)
- **\\** (Backslash) - Remove All Equipment (reset to base stats)

**System Control:**
- **Backspace** - Restore original AI behavior

### Inspector Configuration
With the enemy selected, you can configure TestRepeaterAI settings in the Inspector:

**Test Configuration:**
- `Selected Skill` - Which skill to repeat (Attack, Defense, Counter, etc.)
- `Repeat Delay` - Time between skill repetitions (default: 1.0s)
- `Add Random Delay` - Adds 0-0.5s random variation (makes AI feel less robotic)
- `Random Delay Max` - Maximum random delay to add

**Defensive Skill Options:**
- `Maintain Defensive State` - If enabled, Defense/Counter will immediately re-enter after completing
- `Defensive Wait Duration` - How long to wait in defensive stance (default: 3.0s)

**Test Mode Features:**
- `Infinite Stamina` - Enemy never runs out of stamina (cheat mode for testing)
- `Skip Ranged Aiming` - RangedAttack fires at max accuracy immediately (dev mode)

**Movement Options:**
- `Enable Movement` - Toggle AI movement during patterns (default: OFF for stationary testing)
- `Optimal Range` - Distance AI tries to maintain from player when movement is enabled (default: 2.0 units)

## Testing All Interactions

### Systematic Testing Approach

#### 1. Test Offensive Skills Against Your Defense
```
F1 (Attack) → Test your Defense → Should block and stun/knockdown
F4 (Smash) → Test your Defense → Should break defense and knockdown you
F5 (Windmill) → Test your Defense → Should hit through (Defense ineffective)
F6 (RangedAttack) → Test your Defense → Should block 100% on hit, stay active on miss
```

#### 2. Test Offensive Skills Against Your Counter
```
F1 (Attack) → Test your Counter → Should reflect damage back to enemy
F4 (Smash) → Test your Counter → Should break counter and knockdown you
F5 (Windmill) → Test your Counter → Should break counter and knockdown you
F6 (RangedAttack) → Test your Counter → Counter ineffective, you take full damage
```

#### 3. Test Your Attacks Against Enemy Defense
```
F2 (Defense) → Enable "Maintain Defensive State" → Test your various attacks
- Your Attack vs their Defense → Block + stun/knockdown to you
- Your Smash vs their Defense → Break through, hit them
- Your Windmill vs their Defense → Hit through (Defense ineffective)
- Your RangedAttack vs their Defense → They block 100% on hit
```

#### 4. Test Your Attacks Against Enemy Counter
```
F3 (Counter) → Enable "Maintain Defensive State" → Test your attacks
- Your Attack vs their Counter → Damage reflected back to you
- Your Smash vs their Counter → Break counter, hit them
- Your Windmill vs their Counter → Break counter, hit them
- Your RangedAttack vs their Counter → Counter ineffective, you hit them
```

#### 5. Test Speed Resolution
```
F1 (Attack) → Use your Attack → Test speed resolution system
- If speeds are equal → Both get knocked down
- Faster character → Wins and deals damage
- Slower character → Gets stunned
```

#### 6. Test RangedAttack Accuracy
```
F6 (RangedAttack) → Stand still vs move around
- Stationary target → Accuracy builds faster (~50%/s)
- Moving target → Accuracy builds slower (~15%/s)
- Test miss behavior → Misses should deal 0 damage
- Test Defense response → Defense should stay active on misses
```

## Debug Information

### On-Screen Display
The CombatDebugVisualizer shows real-time test mode info:

```
=== TEST MODE ===
Repeating: Attack
Delay: 1.0s
Next Action: 0.5s
Flags: [Maintain] [∞ Stam]
```

**Flags Explained:**
- `[Maintain]` - Maintain Defensive State is enabled
- `[∞ Stam]` - Infinite Stamina is enabled
- `[Skip Aim]` - Skip Ranged Aiming is enabled
- `[Random]` - Add Random Delay is enabled

### Above Enemy Head
A visual indicator shows:
```
🧪 TEST MODE
Attack (Yellow/Blue/etc color)
Charging Attack
⏱️ 0.5
Next: 1.0s
🧪 TEST AI
```

## Equipment System Testing

### Overview
The equipment system allows you to test different stat configurations by cycling through equipment presets during gameplay. This lets you see how different builds perform against various skills.

### Equipment Presets
Equipment presets are combinations of Armor + Accessory that modify character stats:

**Example Presets:**
- **Tank Build**: Heavy Armor (+Defense, +HP, -Speed) + Guardian Ring
- **Speed Build**: Light Armor (+Speed, +Dex) + Swift Boots
- **Glass Cannon**: Cloth Armor (+Focus) + Power Gauntlets (+Str, -Stamina)
- **Balanced Build**: Chain Mail (+Defense, +HP) + Meditation Amulet (+Focus, +Stamina)

### How to Use Equipment Testing

**1. Setup Equipment Presets:**
- Create equipment ScriptableObjects in Assets/Data/Equipment/
- Create equipment set ScriptableObjects that combine them
- Assign sets to TestEquipmentSelector component in your scene

**2. Testing Equipment in Play Mode:**
```
1. Press F1 (Enemy attacks repeatedly)
2. Press ] to cycle to Tank Build
3. Observe: You should take less damage
4. Press ] again to cycle to Speed Build
5. Observe: You move faster but take more damage
6. Press \ to remove all equipment (reset to base stats)
```

**3. Testing Different Builds Against Skills:**

**Tank Build vs Rapid Attacks:**
```
1. ] → Cycle to Tank Build (high defense, high HP)
2. F1 → Enemy repeats Attack
3. Set Repeat Delay to 0.5s (rapid attacks)
4. F8 → Enable Infinite Stamina
5. Result: Should survive much longer than other builds
```

**Speed Build vs Smash:**
```
1. ] → Cycle to Speed Build (high speed, low defense)
2. F4 → Enemy repeats Smash
3. F9 → Enable Movement
4. Result: Should be able to kite and avoid Smash more easily
```

**Glass Cannon vs Defense:**
```
1. ] → Cycle to Glass Cannon (high strength, low stamina)
2. F2 → Enemy repeats Defense
3. Result: Your attacks should deal more damage but stamina drains faster
```

### Equipment Stat Bonuses

Equipment modifies these stats:
- **Strength**: Increases damage dealt
- **Dexterity**: Increases movement speed
- **Physical Defense**: Reduces damage taken
- **Focus**: Improves status effect resistance, increases stamina pool
- **Max Health**: Direct HP increase
- **Max Stamina**: Direct stamina increase
- **Movement Speed**: Direct speed increase

### Testing Strategy

**Compare Builds Side-by-Side:**
1. Start combat with base stats (\ to remove equipment)
2. Note performance (time to defeat, damage taken, etc.)
3. Press ] to equip Tank Build
4. Restart combat (same scenario)
5. Compare results

**Find Optimal Build for Scenarios:**
- Which build survives longest against F1 (Attack spam)?
- Which build can kite F4 (Smash) most effectively?
- Which build handles F6 (RangedAttack) best?
- Which build has best stamina management?

## Advanced Usage

### Testing Defensive State Maintenance

**Scenario:** You want to test how your Attack works against an enemy that continuously stays in Defense.

1. Press F2 (Enemy repeats Defense)
2. In Inspector, enable "Maintain Defensive State"
3. Set "Repeat Delay" to 0.3s (fast re-entry)
4. Enemy will now constantly stay in Defense Waiting state
5. Test your various skills against this persistent defense

### Testing Rapid Fire Attacks

**Scenario:** You want to pressure test your defenses with rapid enemy attacks.

1. Press F1 (Enemy repeats Attack)
2. Set "Repeat Delay" to 0.5s (fast attacks)
3. Disable "Add Random Delay" (consistent timing)
4. Enable "Infinite Stamina" (never stop attacking)
5. Test your Defense/Counter skills under pressure

### Testing Ranged Accuracy System

**Scenario:** You want to verify ranged attacks work correctly at different distances and movement states.

1. Press F6 (Enemy repeats RangedAttack)
2. Disable "Skip Ranged Aiming" (realistic accuracy)
3. Set "Repeat Delay" to 2.0s (time to observe)
4. Test by:
   - Standing still → High accuracy hits
   - Moving → Lower accuracy, more misses
   - Different distances → Verify accuracy changes

### Testing Stamina Depletion

**Scenario:** You want to test realistic combat with stamina limits.

1. Choose any offensive skill (F1, F4, F5, F6)
2. Disable "Infinite Stamina"
3. Set "Repeat Delay" to 0.5s (fast repetition)
4. Enemy will eventually run out of stamina and can't attack
5. Test how combat feels with limited resources

## Interaction Matrix Testing

Use this checklist to systematically test all 36 skill combinations:

### Player Attack vs Enemy Skills
- [ ] Attack vs Attack (speed resolution)
- [ ] Attack vs Defense (blocked)
- [ ] Attack vs Counter (reflected)
- [ ] Attack vs Smash (speed resolution)
- [ ] Attack vs Windmill (should hit player)
- [ ] Attack vs RangedAttack (speed resolution)

### Player Defense vs Enemy Skills
- [ ] Defense vs Attack (blocks)
- [ ] Defense vs Smash (broken, player knocked down)
- [ ] Defense vs Windmill (ineffective, player hit)
- [ ] Defense vs RangedAttack (blocks 100% on hit, stays active on miss)

### Player Counter vs Enemy Skills
- [ ] Counter vs Attack (reflects)
- [ ] Counter vs Smash (broken, player knocked down)
- [ ] Counter vs Windmill (broken, player knocked down)
- [ ] Counter vs RangedAttack (ineffective, player takes full damage)

### Player Smash vs Enemy Skills
- [ ] Smash vs Attack (priority)
- [ ] Smash vs Defense (breaks defense)
- [ ] Smash vs Counter (breaks counter)
- [ ] Smash vs Smash (speed resolution)
- [ ] Smash vs Windmill (windmill wins)
- [ ] Smash vs RangedAttack (speed resolution)

### Player Windmill vs Enemy Skills
- [ ] Windmill vs Attack (hits through)
- [ ] Windmill vs Defense (hits through)
- [ ] Windmill vs Counter (breaks counter)
- [ ] Windmill vs Smash (hits through)
- [ ] Windmill vs Windmill (both hit)
- [ ] Windmill vs RangedAttack (both hit)

### Player RangedAttack vs Enemy Skills
- [ ] RangedAttack vs Attack (speed resolution)
- [ ] RangedAttack vs Defense (blocked 100% on hit)
- [ ] RangedAttack vs Counter (counter ineffective, enemy takes damage)
- [ ] RangedAttack vs Smash (speed resolution)
- [ ] RangedAttack vs Windmill (both can hit)
- [ ] RangedAttack vs RangedAttack (both can hit)

## Troubleshooting

### Attack Skill Not Working
**Problem:** Enemy executes other skills but Attack doesn't trigger interactions.

**Cause:** Attack has instant execution (no charge time), so it uses a different execution path.

**Solution:** This was fixed in TestRepeaterAI.cs by adding ExecuteInstantAttack() method. If you encounter this, ensure you have the latest version of TestRepeaterAI.

### Enemy Not Responding to Hotkeys
**Problem:** Pressing F1-F6 doesn't change enemy behavior.

**Solutions:**
1. Ensure you're in Play mode
2. Check that TestRepeaterAI component is enabled on the enemy
3. Check that TestSkillSelector exists in scene (auto-created on first hotkey press)
4. Look for error messages in Console

### Enemy Stops Attacking After a While
**Problem:** Enemy attacks for a bit then stops.

**Solutions:**
1. Enemy probably ran out of stamina
2. Enable "Infinite Stamina" in TestRepeaterAI Inspector
3. Or increase "Repeat Delay" to give stamina time to regenerate

### Defense/Counter Only Triggers Once
**Problem:** Enemy enters Defense/Counter but doesn't re-enter after it completes.

**Solutions:**
1. Enable "Maintain Defensive State" in TestRepeaterAI Inspector
2. This will make the enemy immediately re-enter the defensive skill after it completes

### RangedAttack Fires Instantly
**Problem:** RangedAttack accuracy maxes out immediately instead of building up.

**Solutions:**
1. Disable "Skip Ranged Aiming" in TestRepeaterAI Inspector
2. This is a dev mode feature for rapid testing

### Enemy Won't Execute Skills (Out of Range)
**Problem:** Enemy charges skills but cancels them (too far away).

**Solutions:**
1. Move closer to the enemy
2. For melee skills, you need to be within weapon range
3. RangedAttack works at any distance

## Restoring Normal AI

### During Play Mode
- Press **F12** to restore the original AI behavior

### In Edit Mode
1. Go to **Tools → Combat → Test Environment → Restore Original AI**
2. Or manually:
   - Disable/delete TestRepeaterAI component
   - Re-enable SimpleTestAI or KnightAI component

## Tips for Effective Testing

### 1. Start with Simple Interactions
Begin with Attack vs Defense, then progress to more complex interactions.

### 2. Use Debug Visualizer
Keep an eye on the debug info to understand exactly what's happening:
- Current skill state
- Stamina levels
- Knockdown meter
- Skill execution phase

### 3. Test Edge Cases
- What happens when both skills execute at the same time?
- What happens when a skill misses?
- What happens when stamina runs out mid-combo?
- What happens when knockdown meter reaches 100%?

### 4. Test at Different Distances
Some interactions behave differently based on distance:
- Melee range (1-2 units)
- Medium range (3-5 units)
- Long range (6+ units)

### 5. Document Your Findings
If you find a bug or unexpected behavior:
1. Note which skills were involved (Player X vs Enemy Y)
2. Note the exact sequence of events
3. Check the Console for any error messages
4. Note any relevant debug info (health, stamina, etc.)

## Performance Considerations

### Multiple Test Enemies
You can configure multiple enemies with TestRepeaterAI:
- Use **Tools → Combat → Test Environment → Configure All Enemies for Testing**
- Each enemy can be controlled independently with hotkeys
- The first enemy found will respond to F1-F6 hotkeys

### Reducing Debug Overhead
If debug visualization is impacting performance:
1. Select enemy GameObject
2. Find CombatDebugVisualizer component
3. Disable specific debug sections you don't need:
   - Show Character Info
   - Show Skill Info
   - Show Status Effects
   - Show Range Visualization
   - Show Combat Calculations
   - Show System Info
   - Show Test AI Info

## Next Steps

Once you've tested all basic interactions:
1. Test complex scenarios (3+ characters fighting)
2. Test multiplayer synchronization (if implementing networking)
3. Test AI behavior patterns (SimpleTestAI vs PatternedAI vs TestRepeaterAI)
4. Test edge cases (simultaneous skill execution, rapid state changes)
5. Performance test (many enemies, many effects)

## Questions or Issues?

If you encounter any problems with the test environment:
1. Check the Console for error messages
2. Verify all components are properly attached to GameObjects
3. Ensure the scene has both a player and an enemy
4. Make sure you're in Play mode when using hotkeys

Happy testing!
