# Equipment System - Setup Instructions

## Quick Start

### Step 1: Create Sample Equipment (Automatic)
1. In Unity, go to **Tools → Combat → Equipment → Create All Sample Equipment**
2. Click "Yes" when prompted
3. This will automatically create:
   - 4 Armor pieces in `Assets/Data/Equipment/Armor/`
   - 4 Accessories in `Assets/Data/Equipment/Accessories/`
   - 4 Equipment Sets in `Assets/Data/Equipment/Sets/`

### Step 2: Add Components to Enemy
1. Select your enemy GameObject (e.g., "Enemy" or "Knight")
2. Add Component → **Equipment Manager**
3. The enemy now has an equipment system!

### Step 3: Setup Test Equipment Selector
1. Create an empty GameObject in your scene (or use existing TestSkillSelector)
2. Add Component → **Test Equipment Selector**
3. In the Inspector:
   - Enable "Auto Find Target Enemy" (finds enemy automatically)
   - Set Equipment Presets array size to 4
   - Assign the 4 equipment sets:
     - Element 0: Fortress_TankSet
     - Element 1: Windrunner_SpeedSet
     - Element 2: Berserker_GlassCannonSet
     - Element 3: Wanderer_BalancedSet

### Step 4: Test in Play Mode!
1. Press Play
2. Use these hotkeys:
   - **F10** - Next equipment preset (Tank → Speed → Glass Cannon → Balanced → Tank...)
   - **F11** - Previous equipment preset
   - **F12** - Remove all equipment (reset to base stats)
3. Test different builds against different skills:
   - F1 + F10 → Test Tank build vs rapid attacks
   - F4 + F10 twice → Test Speed build vs Smash
   - F2 + F10 three times → Test Glass Cannon vs Defense

---

## What Was Created

### Equipment Pieces (8 total)

**Armor (4):**
1. **Heavy Platemail** - Tank armor (+15 Def, +30 HP, -1 Speed, +3 Focus)
2. **Leather Tunic** - Speed armor (+5 Dex, +2 Speed, +3 Def)
3. **Cloth Robes** - Focus armor (+8 Focus, +20 Stamina, +1 Def)
4. **Chain Mail** - Balanced armor (+10 Def, +15 HP, -0.5 Speed, +2 Dex)

**Accessories (4):**
1. **Power Gauntlets** - Damage accessory (+8 Str, -10 Stamina)
2. **Meditation Amulet** - Focus accessory (+10 Focus, +15 Stamina)
3. **Swift Boots** - Speed accessory (+5 Dex, +1.5 Speed)
4. **Guardian Ring** - Tank accessory (+5 Def, +20 HP, +3 Focus)

### Equipment Sets (4 total)

1. **Fortress** (Tank): Heavy Platemail + Guardian Ring
   - Total: +20 Def, +50 HP, -1 Speed, +6 Focus
   - Best against: Rapid attacks, pressure tactics

2. **Windrunner** (Speed): Leather Tunic + Swift Boots
   - Total: +10 Dex, +3.5 Speed, +3 Def
   - Best against: Slow attacks, Smash, predictable patterns

3. **Berserker** (Glass Cannon): Cloth Robes + Power Gauntlets
   - Total: +8 Str, +8 Focus, +10 Stamina, +1 Def
   - Best against: Defensive enemies, stationary targets

4. **Wanderer** (Balanced): Chain Mail + Meditation Amulet
   - Total: +10 Def, +15 HP, -0.5 Speed, +2 Dex, +10 Focus, +15 Stamina
   - Best against: General use, all scenarios

---

## Testing Guide

### Quick Test: Tank vs Damage
```
1. F12 - Remove all equipment (note current health/survivability)
2. F1 - Enemy repeats Attack
3. Fight and observe damage taken
4. F10 - Equip Fortress (Tank build)
5. Fight again - should take much less damage
```

### Quick Test: Speed vs Mobility
```
1. F12 - Remove all equipment (note movement speed)
2. F10 twice - Equip Windrunner (Speed build)
3. W/A/S/D - Move around, notice speed increase
4. F4 - Enemy Smash
5. F9 - Enable enemy movement
6. Practice kiting - should be easier with speed
```

### Quick Test: Glass Cannon vs Damage
```
1. F10 three times - Equip Berserker (Glass Cannon)
2. F2 - Enemy Defense
3. Attack the enemy - notice higher damage
4. Check stamina - notice it drains faster
```

---

## Troubleshooting

### Equipment Not Changing
**Problem:** Pressing F10/F11 doesn't seem to change equipment.

**Solutions:**
1. Check Console for "[TestEquipment] Applied preset..." messages
2. Ensure TestEquipmentSelector component is in the scene
3. Ensure equipment sets are assigned to the component
4. Ensure enemy has EquipmentManager component

### No Stat Changes Visible
**Problem:** Equipment equipped but stats don't change.

**Solutions:**
1. Open enemy GameObject in Inspector
2. Check EquipmentManager component - CurrentArmor and CurrentAccessory should show equipped items
3. Check CombatController component - Stats should reflect bonuses
4. Enable debug logs in EquipmentManager to see bonus application

### Cannot Equip During Combat
**Problem:** "Cannot change equipment during combat" warning.

**Solutions:**
1. This is intentional - equipment can only change outside combat
2. Exit combat first (move away from enemy, wait for combat to end)
3. Then try F10/F11 again
4. For testing, you may need to temporarily disable combat state

### Equipment Sets Not Found
**Problem:** TestEquipmentSelector can't find equipment sets.

**Solutions:**
1. Verify equipment was created: Check `Assets/Data/Equipment/Sets/`
2. Re-run **Tools → Combat → Equipment → Create All Sample Equipment**
3. Manually assign sets in TestEquipmentSelector Inspector

---

## Advanced Setup

### Creating Custom Equipment

**Create Individual Equipment:**
1. Right-click in Project → **Create → Combat → Equipment**
2. Name it (e.g., "MyCustomArmor")
3. Set properties in Inspector:
   - Equipment Name
   - Slot (Armor/Accessory)
   - Tier (Basic/Advanced/Master)
   - Stat bonuses (can be positive or negative)
   - Description

**Create Equipment Set:**
1. Right-click in Project → **Create → Combat → Equipment Set**
2. Name it (e.g., "MyCustomSet")
3. Assign Armor and Accessory
4. Set name and description
5. Add to TestEquipmentSelector's preset array

### Assigning Starting Equipment

To give a character equipment from the start:
1. Select character GameObject
2. Find EquipmentManager component
3. Assign equipment to:
   - Current Armor
   - Current Accessory
4. Equipment will be equipped on Start()

---

## Integration with Existing Systems

### Stats Modified by Equipment

Equipment bonuses are applied to:
- **Strength** → Increases damage dealt
- **Dexterity** → Increases movement speed
- **Physical Defense** → Reduces damage taken
- **Focus** → Improves status resistance, stamina efficiency
- **Max Health** → Direct HP increase
- **Max Stamina** → Direct stamina increase
- **Movement Speed** → Direct speed bonus/penalty

### How It Works Internally

1. **CombatController.Stats** now returns modified stats (base + equipment)
2. **HealthSystem.MaxHealth** includes equipment HP bonuses
3. **StaminaSystem.MaxStamina** includes equipment stamina bonuses
4. **MovementController** automatically uses modified dexterity for speed
5. All damage/status calculations use modified stats automatically

### Equipment Restrictions

- Can only equip/unequip **outside combat**
- Weapon slot handled by existing **WeaponController**
- Equipment changes trigger stat refresh
- Stats update immediately on equip/unequip

---

## Next Steps

After testing the sample equipment:

1. **Create more equipment pieces** for different playstyles
2. **Balance the stats** based on testing feedback
3. **Add special properties** (Phase 2 feature)
4. **Implement set bonuses** (Phase 2 feature)
5. **Add equipment loot drops** (future feature)
6. **Create equipment upgrade system** (future feature)

---

## Reference

For detailed equipment stats and testing scenarios, see:
- **SAMPLE_EQUIPMENT_GUIDE.md** - Complete equipment reference
- **SKILL_TEST_ENVIRONMENT_USAGE.md** - Equipment testing section
- **EQUIPMENT_SYSTEM_DESIGN.md** - Technical implementation details

---

## Summary

You now have:
- ✅ Complete equipment system implemented
- ✅ 8 equipment pieces (4 armor, 4 accessories)
- ✅ 4 equipment sets (Tank, Speed, Glass Cannon, Balanced)
- ✅ F10/F11/F12 hotkeys for testing
- ✅ Integration with combat stats
- ✅ Full documentation

**Ready to test different builds and expand your combat system!**
