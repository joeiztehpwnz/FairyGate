# Equipment System - Complete Guide

> **Consolidated from:** EQUIPMENT_SYSTEM_DESIGN.md, EQUIPMENT_SYSTEM_SETUP_INSTRUCTIONS.md, SAMPLE_EQUIPMENT_GUIDE.md

---

## Overview
Simple, expandable equipment system that modifies character stats and integrates with the existing combat system. Starting with 3 equipment slots and additive stat bonuses.

---

## Quick Start - Setup Instructions

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
   - **F10** - Next equipment preset (Tank → Speed → Glass Cannon → Balanced)
   - **F11** - Previous equipment preset
   - **F12** - Remove all equipment (reset to base stats)
3. Test different builds against different skills:
   - F1 + F10 → Test Tank build vs rapid attacks
   - F4 + F10 twice → Test Speed build vs Smash
   - F2 + F10 three times → Test Glass Cannon vs Defense

---

## Core Architecture

### Equipment Slots (Phase 1)
1. **Armor** - Primary defense and survivability
2. **Weapon** - Already implemented via WeaponData, will integrate
3. **Accessory** - Utility stats and special bonuses

**Future Expansion Slots** (Phase 2+):
- Head, Chest, Legs (split armor into pieces)
- Ring, Necklace (split accessory)
- Off-hand (shield, secondary weapon)

---

## Sample Equipment Reference

### Armor Pieces (4 Total)

#### 1. Heavy Platemail (Tank - Advanced)
**Description:** "Heavy iron armor that provides excellent protection at the cost of mobility."

**Stats:**
- Physical Defense: +15
- Max Health: +30
- Movement Speed: -1.0
- Focus: +3

**Best For:** Tanking damage, surviving prolonged fights
**Weakness:** Slower movement, easier to catch

---

#### 2. Leather Tunic (Speed - Basic)
**Description:** "Light leather armor that allows for quick movement and dodging."

**Stats:**
- Dexterity: +5
- Movement Speed: +2.0
- Physical Defense: +3

**Best For:** Kiting, avoiding slow attacks, speed builds
**Weakness:** Low defense, fragile

---

#### 3. Cloth Robes (Caster/Focus - Basic)
**Description:** "Light magical robes that enhance mental focus and stamina recovery."

**Stats:**
- Focus: +8
- Max Stamina: +20
- Physical Defense: +1

**Best For:** Stamina-heavy playstyles, status effect resistance
**Weakness:** Very low defense, vulnerable to attacks

---

#### 4. Chain Mail (Balanced - Advanced)
**Description:** "A balanced armor providing decent protection without sacrificing too much mobility."

**Stats:**
- Physical Defense: +10
- Max Health: +15
- Movement Speed: -0.5
- Dexterity: +2

**Best For:** Balanced builds, general use
**Weakness:** Jack of all trades, master of none

---

### Accessory Pieces (4 Total)

#### 1. Power Gauntlets (Damage - Advanced)
**Description:** "Enchanted gauntlets that enhance striking power but drain stamina faster."

**Stats:**
- Strength: +8
- Max Stamina: -10

**Best For:** Glass cannon builds, burst damage
**Weakness:** Stamina management becomes critical

---

#### 2. Meditation Amulet (Focus - Advanced)
**Description:** "An amulet that enhances mental fortitude and stamina recovery."

**Stats:**
- Focus: +10
- Max Stamina: +15

**Best For:** Defensive builds, prolonged fights, status effect resistance
**Weakness:** No damage or mobility bonuses

---

#### 3. Swift Boots (Speed - Basic)
**Description:** "Light boots that greatly enhance movement speed and agility."

**Stats:**
- Dexterity: +5
- Movement Speed: +1.5

**Best For:** Kiting, hit-and-run tactics, dodging
**Weakness:** No defensive stats

---

#### 4. Guardian Ring (Tank - Advanced)
**Description:** "A protective ring blessed with defensive magic."

**Stats:**
- Physical Defense: +5
- Max Health: +20
- Focus: +3

**Best For:** Maximizing survivability, reducing status effects
**Weakness:** No offensive bonuses

---

## Equipment Sets (4 Total)

### Set 1: Fortress (Tank Build)
**Description:** "Maximum survivability build focused on absorbing damage and outlasting opponents."

**Equipped:**
- Armor: Heavy Platemail
- Accessory: Guardian Ring

**Total Bonuses:**
- Physical Defense: +20
- Max Health: +50
- Movement Speed: -1.0
- Focus: +6

**Playstyle:** Stand and fight, trade blows, survive burst damage
**Best Against:** Rapid attacks (F1 spam), pressure tactics
**Worst Against:** Kiting enemies, RangedAttack spam

---

### Set 2: Windrunner (Speed Build)
**Description:** "High mobility build for hit-and-run tactics and kiting."

**Equipped:**
- Armor: Leather Tunic
- Accessory: Swift Boots

**Total Bonuses:**
- Dexterity: +10
- Movement Speed: +3.5
- Physical Defense: +3

**Playstyle:** Kite, dodge, hit-and-run, maintain distance
**Best Against:** Slow attacks (F4 Smash), predictable patterns
**Worst Against:** RangedAttack, sustained pressure

---

### Set 3: Berserker (Glass Cannon Build)
**Description:** "High damage output at the cost of stamina management and defense."

**Equipped:**
- Armor: Cloth Robes
- Accessory: Power Gauntlets

**Total Bonuses:**
- Strength: +8
- Focus: +8
- Max Stamina: +10
- Physical Defense: +1

**Playstyle:** Aggressive offense, kill before being killed
**Best Against:** Defensive enemies (F2 Defense), stationary targets
**Worst Against:** Counter-attackers (F3 Counter), rapid attacks

---

### Set 4: Wanderer (Balanced Build)
**Description:** "Well-rounded build suitable for most combat scenarios."

**Equipped:**
- Armor: Chain Mail
- Accessory: Meditation Amulet

**Total Bonuses:**
- Physical Defense: +10
- Max Health: +15
- Movement Speed: -0.5
- Dexterity: +2
- Focus: +10
- Max Stamina: +15

**Playstyle:** Adaptable, good at everything, excels at nothing
**Best Against:** General use, learning combat mechanics
**Worst Against:** Specialized threats (not optimized for any specific scenario)

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

## Data Structure

### EquipmentData (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "New Equipment", menuName = "Combat/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Equipment Info")]
    public string equipmentName;
    public EquipmentSlot slot;
    public EquipmentTier tier;

    [Header("Stat Modifiers")]
    public int strengthBonus;
    public int dexterityBonus;
    public int physicalDefenseBonus;
    public int focusBonus;
    public int maxHealthBonus;
    public int maxStaminaBonus;
    public float movementSpeedBonus;

    [Header("Visual")]
    public Sprite icon;
    public GameObject equipmentPrefab; // Optional visual representation

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}
```

### EquipmentSet (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "New Equipment Set", menuName = "Combat/Equipment Set")]
public class EquipmentSet : ScriptableObject
{
    public string setName;
    public EquipmentData armor;
    public WeaponData weapon;
    public EquipmentData accessory;

    [TextArea(2, 3)]
    public string description;
}
```

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

---

## Equipment Synergies

### High Survivability (Tank + Focus)
Heavy Platemail + Meditation Amulet
- Maximizes health pool and defense
- Strong focus for status effect resistance
- Excellent stamina pool for defensive skills

### Maximum Speed (Speed + Speed)
Leather Tunic + Swift Boots
- Stacks movement speed bonuses
- High dexterity for even more speed
- Trade-off: very fragile

### Stamina Management (Focus + Focus)
Cloth Robes + Meditation Amulet
- Massive stamina pool (+35 total)
- High focus for efficiency
- Trade-off: low defense, vulnerable

### Hybrid Damage Tank (Tank + Damage)
Heavy Platemail + Power Gauntlets
- Strong defense and HP
- Good damage output
- Trade-off: stamina management challenge

---

## Future Expansion Points

### Phase 2 Enhancements:
1. **Equipment Sets**: Bonus when wearing matching pieces
2. **Special Properties**:
   - "Thorns" - Reflect damage back
   - "Regeneration" - Heal over time
   - "Haste" - Reduce skill charge time
3. **Durability**: Equipment degrades with use
4. **Upgrade System**: Enhance equipment stats
5. **Visual Equipment**: Show armor on character model

### Phase 3 Enhancements:
1. **Loot Drops**: Enemies drop equipment
2. **Crafting System**: Combine materials to create equipment
3. **Enchanting**: Add special properties to equipment
4. **Transmogrification**: Change appearance while keeping stats

---

## Testing Checklist

After creating equipment, test:
- [ ] All 4 armor pieces equip correctly
- [ ] All 4 accessories equip correctly
- [ ] All 4 equipment sets apply correctly
- [ ] F10/F11 cycles through sets properly
- [ ] F12 removes all equipment
- [ ] Stat bonuses apply correctly (check debug display)
- [ ] MaxHealth changes when equipped
- [ ] MaxStamina changes when equipped
- [ ] Movement speed changes visibly
- [ ] Cannot equip during combat
- [ ] Tank build survives longer vs F1 attacks
- [ ] Speed build moves noticeably faster
- [ ] Glass cannon deals more damage
- [ ] Balanced build works in all scenarios

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
