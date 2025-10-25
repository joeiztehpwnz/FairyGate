# Sample Equipment Guide

## Overview
This document lists all the sample equipment pieces created for testing the equipment system. These are balanced to showcase different build archetypes.

---

## Armor Pieces (4 Total)

### 1. Heavy Platemail (Tank - Advanced)
**Description:** "Heavy iron armor that provides excellent protection at the cost of mobility."

**Stats:**
- Physical Defense: +15
- Max Health: +30
- Movement Speed: -1.0
- Focus: +3

**Best For:** Tanking damage, surviving prolonged fights
**Weakness:** Slower movement, easier to catch

---

### 2. Leather Tunic (Speed - Basic)
**Description:** "Light leather armor that allows for quick movement and dodging."

**Stats:**
- Dexterity: +5
- Movement Speed: +2.0
- Physical Defense: +3

**Best For:** Kiting, avoiding slow attacks, speed builds
**Weakness:** Low defense, fragile

---

### 3. Cloth Robes (Caster/Focus - Basic)
**Description:** "Light magical robes that enhance mental focus and stamina recovery."

**Stats:**
- Focus: +8
- Max Stamina: +20
- Physical Defense: +1

**Best For:** Stamina-heavy playstyles, status effect resistance
**Weakness:** Very low defense, vulnerable to attacks

---

### 4. Chain Mail (Balanced - Advanced)
**Description:** "A balanced armor providing decent protection without sacrificing too much mobility."

**Stats:**
- Physical Defense: +10
- Max Health: +15
- Movement Speed: -0.5
- Dexterity: +2

**Best For:** Balanced builds, general use
**Weakness:** Jack of all trades, master of none

---

## Accessory Pieces (4 Total)

### 1. Power Gauntlets (Damage - Advanced)
**Description:** "Enchanted gauntlets that enhance striking power but drain stamina faster."

**Stats:**
- Strength: +8
- Max Stamina: -10

**Best For:** Glass cannon builds, burst damage
**Weakness:** Stamina management becomes critical

---

### 2. Meditation Amulet (Focus - Advanced)
**Description:** "An amulet that enhances mental fortitude and stamina recovery."

**Stats:**
- Focus: +10
- Max Stamina: +15

**Best For:** Defensive builds, prolonged fights, status effect resistance
**Weakness:** No damage or mobility bonuses

---

### 3. Swift Boots (Speed - Basic)
**Description:** "Light boots that greatly enhance movement speed and agility."

**Stats:**
- Dexterity: +5
- Movement Speed: +1.5

**Best For:** Kiting, hit-and-run tactics, dodging
**Weakness:** No defensive stats

---

### 4. Guardian Ring (Tank - Advanced)
**Description:** "A protective ring blessed with defensive magic."

**Stats:**
- Physical Defense: +5
- Max Health: +20
- Focus: +3

**Best For:** Maximizing survivability, reducing status effects
**Weakness:** No offensive bonuses

---

## Equipment Sets (4 Total)

### Set 1: Tank Build
**Name:** "Fortress"
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

### Set 2: Speed Build
**Name:** "Windrunner"
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

### Set 3: Glass Cannon Build
**Name:** "Berserker"
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

### Set 4: Balanced Build
**Name:** "Wanderer"
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

## Testing Scenarios

### Tank Build Testing
**Setup:**
1. F10 to equip Fortress set
2. F1 for enemy rapid attacks
3. Set repeat delay to 0.5s
4. F8 for enemy infinite stamina

**Expected Result:** Should survive 2-3x longer than base stats

---

### Speed Build Testing
**Setup:**
1. F10 to equip Windrunner set
2. F4 for enemy Smash
3. F9 to enable enemy movement
4. Practice kiting

**Expected Result:** Should be able to maintain distance and avoid Smash

---

### Glass Cannon Testing
**Setup:**
1. F10 to equip Berserker set
2. F2 for enemy Defense
3. Attack the defensive enemy

**Expected Result:** Should break through defense faster but stamina drains quickly

---

### Balanced Build Testing
**Setup:**
1. F10 to equip Wanderer set
2. Cycle through F1-F6 (test against all skills)

**Expected Result:** Should perform reasonably well in all scenarios

---

## Stat Calculations

### Example: Tank Build Total Stats
**Base Stats** (Default CharacterStats):
- Strength: 10
- Dexterity: 10
- Physical Defense: 10
- Focus: 10
- Vitality: 10

**With Fortress Set:**
- Strength: 10 (no change)
- Dexterity: 10 (no change)
- Physical Defense: 10 + 20 = **30**
- Focus: 10 + 6 = **16**
- Vitality: 10 (no change)
- Max Health: (BASE_HEALTH + vitality × MULTIPLIER) + 50 = **~200 HP**
- Movement Speed: BASE_SPEED - 1.0 = **~6.0**

### Example: Speed Build Total Stats
**With Windrunner Set:**
- Dexterity: 10 + 10 = **20**
- Physical Defense: 10 + 3 = **13**
- Movement Speed: BASE_SPEED + 3.5 + (dex × MULTIPLIER) = **~11.5**

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

## Future Equipment Ideas

### Phase 2 (Advanced Tier):
- **Dragon Scale Armor**: +25 Def, +50 HP, +5 Focus, -2 Speed
- **Assassin's Cloak**: +10 Dex, +3 Speed, +5 Str
- **Berserker Ring**: +15 Str, -20 Stamina, +10 HP
- **Sage's Pendant**: +15 Focus, +30 Stamina, +5 HP

### Phase 3 (Master Tier):
- **Legendary Plate of the Ancients**: +30 Def, +80 HP, +10 Focus
- **Boots of the Wind**: +15 Dex, +5 Speed, +10 Stamina
- **Titan's Gauntlets**: +20 Str, +30 HP
- **Crown of Wisdom**: +20 Focus, +50 Stamina, +30 HP

---

## Notes for Future Development

### Special Properties (Not Yet Implemented)
These could be added in Phase 2:
- **Thorns**: Reflect % of damage back to attacker
- **Regeneration**: Heal X HP per second
- **Haste**: Reduce skill charge time by X%
- **Vampiric**: Heal for % of damage dealt
- **Fortification**: Reduce knockdown meter buildup
- **Swiftness**: Increase dodge chance (if dodge mechanic added)

### Set Bonuses (Not Yet Implemented)
Wearing matching equipment could provide bonuses:
- **2-piece Tank Set**: +10% damage reduction
- **2-piece Speed Set**: +15% movement speed
- **2-piece Damage Set**: +10% damage dealt
- **2-piece Focus Set**: +20% stamina efficiency

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
