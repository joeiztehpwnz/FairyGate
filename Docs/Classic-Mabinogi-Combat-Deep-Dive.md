# Classic Mabinogi Combat System Deep Dive
## Comprehensive Pre-2012 Mechanics Analysis

---

## Table of Contents
1. [Core Combat Philosophy](#core-combat-philosophy)
2. [Critical Hit & Balance System](#critical-hit--balance-system)
3. [Protection & Defense Layers](#protection--defense-layers)
4. [Injury & Wound System](#injury--wound-system)
5. [Heavy Stander & Auto Defense](#heavy-stander--auto-defense)
6. [Skill Load Times & Execution](#skill-load-times--execution)
7. [Knockdown & Stun Mechanics](#knockdown--stun-mechanics)
8. [Dual Wielding System](#dual-wielding-system)
9. [Combat Power (CP) System](#combat-power-cp-system)
10. [Aggro & Targeting System](#aggro--targeting-system)
11. [Transformation & Special Skills](#transformation--special-skills)
12. [Magic System Depth](#magic-system-depth)
13. [Fighter Combo Chains](#fighter-combo-chains)
14. [Weapon Upgrades & Enchantments](#weapon-upgrades--enchantments)
15. [Environmental Factors](#environmental-factors)
16. [Pet Combat System](#pet-combat-system)

---

## Core Combat Philosophy

### Pre-2012 Design Principles
Classic Mabinogi combat (before the April 12, 2012 "Dynamic Combat System" update) was characterized by:

- **Semi-Turn-Based Combat**: Players and monsters fought on equal footing with similar mechanics
- **AI Prediction-Based**: Success required learning and predicting enemy behavior patterns
- **Methodical Pacing**: 2+ second skill load times created deliberate, tactical gameplay
- **Vulnerability Windows**: Charging skills left players exposed, requiring careful timing
- **Knowledge Over Reflexes**: Mastery came from pattern recognition, not reaction speed

### What Made It Special
Veterans described classic combat as:
> "You have to observe them and gain experience to know your enemies and how to fight them properly"

The timing windows created a **chess-like decision-making process** where players needed to:
- Predict opponent actions 2+ seconds in advance
- Manage vulnerability during skill preparation
- Master weapon-specific timing windows
- Learn consistent AI patterns through observation

---

## Critical Hit & Balance System

### Critical Hit Mechanics

#### Base Formula
```
Actual Critical Chance = (Your Critical %) - (Target Protection × 2)
Maximum Critical Chance = 30% (hard cap)
```

#### Stat Contributions
- Every 10 Will → +1.0% Critical
- Every 5 Luck → +1.0% Critical
- Equipment and enchantments provide additional critical

#### Critical Damage Calculation
```
Critical Damage = Base Damage + [Max Damage × (Critical Damage Multiplier + Skill Crit Bonus)]
```
- Base multiplier: 1.0×
- Critical Hit skill at Rank 1: +2.5× multiplier
- Total at max rank: 3.5× damage on critical

#### Special Mechanics
- **Area Critical**: If primary target crits, ALL targets in range also receive critical
- **Forced Knockdown**: Critical hits force knockdown instead of knockback
- **AI Reset**: Criticals may reset enemy AI patterns and change aggro
- **Extended Stun**: Critical hits increase stun duration

### Balance System

#### Damage Distribution
Balance determines probability of hitting for maximum damage:
- 100% Balance = Always hit for max damage
- 50% Balance = 50% chance for max, 50% for minimum
- 0% Balance = Always hit for minimum damage

#### Balance Caps & Scaling
```
Historical Caps:
- Original: 100% balance cap
- Post-G1: 80% balance cap (current)

Dexterity Scaling (Diminishing Returns):
(20.34565 / (2^(BALANCE/-8.728944))) - 9.814582
- Caps at 50% from Dexterity alone
- Total cap: 80% with all sources
```

#### Sources of Balance
- Base weapon balance
- Dexterity contribution (capped at 50%)
- Skill rank bonuses:
  - Combat Mastery: +1% per rank (melee)
  - Ranged Attack: +1% per rank (archery)
  - Sword Mastery: +1% per rank (×2 when dual wielding)

---

## Protection & Defense Layers

### Protection Stat (Not to be confused with Defense skill)

#### Dual Effect System
Protection provides TWO benefits:

1. **Damage Reduction**: Direct percentage reduction
   ```
   Damage Taken = Base Damage × (1 - Protection%)
   ```

2. **Critical Defense**: Reduces enemy critical chance
   ```
   Enemy Crit Chance = Base Crit - (Your Protection × 2)
   ```

#### Example Calculation
```
Scenario:
- Enemy has 90% critical chance
- You have 16 Protection

Result:
- Enemy actual crit: 90% - (16 × 2) = 58%
- After 30% cap: 30% actual critical chance
- Plus 16% damage reduction on all hits
```

### Defense Skill vs Protection Stat
- **Defense Skill**: Active blocking ability (uses stamina)
- **Protection Stat**: Passive damage reduction (always active)
- Both stack for maximum mitigation

---

## Injury & Wound System

### Core Mechanics

#### What Are Wounds?
- Damage that **cannot be naturally recovered**
- Appears as red portion of health bar
- Reduces maximum effective HP until healed
- Must be healed by NPCs, potions, or campfire rest

#### Injury Rate Calculation
```
Wound Damage = Total Damage × Injury Rate
Injury Rate = Base Rate + Stat Modifiers - Target Protection
```

#### Stat Contributions
- Every 10 Dexterity: +1.0% Max Injury, +0.5% Min Injury
- Every 10 Will: +2.0% Max Injury, +0.5% Min Injury
- Target Protection: -1.0% injury rate per point

#### Strategic Importance
- **Anti-Regeneration**: Prevents bosses from healing
- **Resource Drain**: Forces potion/healer usage
- **Attrition Warfare**: Accumulates over long fights
- **PvP Advantage**: Permanent damage in duels

#### Skills That Cannot Inflict Wounds
- Magic (all types)
- Alchemy (all types)
- Windmill
- Area of effect skills

---

## Heavy Stander & Auto Defense

### Heavy Stander System

#### Two-Layer Defense Structure
1. **Passive Reduction** (Always Active)
   - Reduces damage from covered skill types
   - No activation requirement

2. **Auto Defense Trigger** (Chance-Based)
   - Metallic "ping!" sound on activation
   - Character flashes with colored indicator
   - **100% stun removal** on activation
   - Additional 50% damage reduction
   - Reduces knockback/knockdown time

#### Calculation Order
```
1. Apply Heavy Stander passive reduction
2. Apply defense/protection stats
3. If Auto Defense triggers: × 0.5 final damage
4. Apply stun reduction/removal
```

### Coverage by Type

#### Heavy Stander
Covers: Melee, Fighter, Puppetry, Dischord, Flash Launcher

#### Mana Deflector
Covers: Magic, Alchemy, Gold Strike
Special: 100% activation rate vs magic

#### Natural Shield
Covers: Archery, Dual Gun, Throwing Attack, Gold Strike

### Transformation Bonuses
- **Dark Knights & Beasts**: All three defenses always activate
- **Falcons & Paladins**: Random 1-3 defenses activate per hit

---

## Skill Load Times & Execution

### Classic Load Times (Pre-2012)

#### Confirmed Timings
```
Smash: 2.0 seconds to load and execute
Windmill: ~0.8 seconds at Rank 9+ ("pretty much instant")
Defense: ~1.0 seconds
Counter: ~1.0 seconds
Normal Attack: Instant (no load time)

Magic Examples:
- Icebolt: 1.0 second
- Firebolt: 1.5 seconds
- Lightning Bolt: 2.0 seconds
- Thunder: 2-6 seconds (rank dependent)
```

### Loading Mechanics

#### While Stunned
- **CAN continue loading skills**
- Cannot move or perform other actions
- Load progress pauses but doesn't reset
- Critical for maintaining offensive pressure

#### While Moving
- Most skills cannot load while moving
- Exceptions:
  - Windmill (mobile charging)
  - Some ranged skills
  - Certain weapon-specific allowances

#### Skill Commitment
- Cannot cancel during Active frames
- Can cancel during Charging (lose progress)
- Knockdown cancels all skill progress
- Roar/special abilities can interrupt

---

## Knockdown & Stun Mechanics

### Knockdown Gauge System

#### Gauge Mechanics
```
Maximum Gauge: 120% (visual shows up to 100%)
Knockback Threshold: 50%
Knockdown Threshold: 100%
Natural Decay: -5 points per second
```

#### Hit Accumulation
Amount added depends on:
- Hit number in combo
- Weapon knockdown rate
- Character strength
- Target's Will stat (resistance)

#### Skills That Don't Build Gauge
- All but last hit of Thunder
- Charge, Fury of Light, Shadow Spirit
- Support Shot
- Final Hit (with dual wield/knuckles)
- Rock Throwing
- Initial hit of Crash Shot

### Stun Duration System

#### Base Mechanics
- **Stun begins on first animation frame** (not when damage appears)
- Duration based on weapon speed:
  ```
  Very Slow (Claymore): 2.0+ seconds
  Slow (Two-Handed): 1.5-2.0 seconds
  Normal (Sword): 1.0-1.2 seconds
  Fast (Dagger): 0.3-0.5 seconds
  Very Fast (Knuckles): 0.2-0.3 seconds
  ```

#### Stun Resistance
```
Actual Stun = Base Stun × (1 - Target Focus/30)
```
- 30 Focus = 100% stun immunity
- 15 Focus = 50% stun reduction

### N+1 and N+2 Combo System

#### N+1 Combos
Add one extra hit beyond weapon's base combo:
- 3-hit weapon → 4-hit combo possible
- 2-hit weapon → 3-hit combo possible

Timing: Attack during 70-95% of stun duration

#### N+2 Combos
Bare hands and fast weapons only:
- Requires frame-perfect timing
- "Fairly difficult to pull off"
- Attack at exactly 50% and 90% of stun window

---

## Dual Wielding System

### Damage Calculations

#### Normal Attacks
Each weapon calculated independently:
```
Left Hand: Uses left weapon stats
Right Hand: Uses right weapon stats
Total Hits: Weapon1 hits + Weapon2 hits
```

#### Single-Hit Skills (Smash, Windmill, Counter)
```
Damage = [(Base) + (Weapon1 Damage) + (Weapon2 Damage)] × Skill Multiplier
Balance = (Weapon1 Balance + Weapon2 Balance) / 2
Critical = (Weapon1 Critical + Weapon2 Critical) / 2
```

### Special Mechanics
- **Attack Speed**: Averaged or rounds to slower weapon
- **Stamina Cost**: 2× normal (both weapons consume)
- **Knockback Count**: Combined hits from both weapons
- **Durability Loss**: Both weapons lose durability

### Race Restrictions
- **Humans**: One-handed swords only
- **Giants**: One-handed blunt weapons only
- **Elves**: Cannot dual wield

### Dual Wield Mastery
Adds hidden bonuses not shown on weapons:
- Flat damage increase
- Balance bonus
- Critical bonus
- Armor pierce
- Auto Defense chance

---

## Combat Power (CP) System

### CP Calculation Formula
```
Total CP = Highest CP Skill
         + (0.5 × 2nd Highest CP Skill)
         + (0.5 × Base Health)
         + (0.33 × Base Mana)
         + (0.33 × Base Stamina)
         + Base Strength
         + (0.2 × Base Intelligence)
         + (0.1 × Base Dexterity)
         + (0.5 × Base Will)
         + (0.1 × Base Luck)
         + CP from Enchants
```

**Important**: Only BASE stats count (not buffs/food/titles)

### CP Relationship Classifications
```
Weakest: < 0.8× player CP
Weak: 0.8-1.0× player CP
Normal: 1.0-1.4× player CP
Strong: 1.4-2.0× player CP
Awful: 2.0-3.0× player CP
Boss: > 3.0× player CP
```

### Gameplay Effects
- **Weak/Weakest**: Reduced drops, no EXP (except dungeons)
- **Training Requirements**: Many skills require specific CP ranges
- **Difficulty Matching**: Ensures appropriate challenge
- **Tendering Potion**: Temporarily lowers enemy CP for training

---

## Aggro & Targeting System

### Five Aggro States

1. **No Aggro**
   - Ignores players
   - Wanders randomly
   - No threat indicator

2. **Detect**
   - Shows "!" indicator
   - Follows player
   - Not yet hostile

3. **Stunned**
   - Shows "!" indicator
   - Waiting for current target to die
   - Will switch to Aggro after

4. **Aggro**
   - Shows "!!" indicator
   - Actively attacks
   - Executes combat AI

5. **Retreat**
   - Returns to spawn
   - Loses player interest
   - Resets to No Aggro

### Multi-Aggro Rules

#### M:N System
Different monsters have different multi-aggro limits:
```
Goblins: 1:1 (one at a time)
Goblin Archers: 2:1 (two at a time)
Wolves: 3:1 (pack tactics)
Boss minions: Unlimited
```

#### Aggro Mechanics
- **Direct Attack**: Instant Aggro state
- **Indirect Attack** (AoE): Stunned state first
- **Critical Hits**: May reset AI and change target
- **Combat Mode**: Increases aggro acquisition speed
- **Pets**: Count toward multi-aggro limits

---

## Transformation & Special Skills

### Final Hit (Human-Only)

#### Core Effects
- **20% attack speed increase** (400ms minimum)
- **Teleport to target** on normal attacks
- No weapon durability loss during skill
- Cannot knockdown with dual wield/knuckles
- Green duration bar displayed

#### Restrictions During Final Hit
Can only use:
- Normal attacks (with teleport)
- Meditation
- Mana Shield
- Transformation skills

### Transformation Mastery

#### System Overview
- Requires Dream Catcher item equipped
- Transform into creature forms
- Inherit creature stats and hitbox
- **Full heal** on transformation (HP/MP/SP)
- Heals most wounds

#### Skill Availability
Limited to skills the creature knows:
- Basic combat (Attack, Defense, Counter, Smash, Windmill)
- Rest
- Gestures/emotes
- Use YOUR skill ranks, not creature's

#### Passive Defense Bonuses
- **Beasts/Dark Knights**: All three defenses always activate
- **Paladins/Falcons**: Random 1-3 defenses per hit

### Deadly Status

#### Trigger Conditions
- Receive lethal damage while above 50% HP
- Prevents instant death from full health

#### Deadly State
- HP drops to critical (near zero)
- "DEADLY" displayed in status
- **Next hit is instant knockout** (even 0 damage)
- Rock Throwing can finish Deadly targets
- Adds tension to combat

---

## Magic System Depth

### Bolt Magic Mechanics

#### Charge Stacking
- Stack up to 5 charges simultaneously
- Each charge costs less mana than previous
- Damage scales with charge count
- Lightning Bolt: More targets per charge

#### Bolt Fusion System
```
Process:
1. Cast first bolt (start loading)
2. Cast different bolt while first loads
3. Creates fused bolt (icon changes)
4. Both charge simultaneously
5. +30% damage at 5 charges
6. +15% for entire fused bolt
```

### Intermediate Magic

#### Thunder
- 2-6 second charge time (rank dependent)
- Additional targets per charge level
- All but last hit doesn't build knockdown

#### Ice Spear
- Penetrates through enemies
- Freezes targets
- Splash damage on impact

#### Fireball
- Guided projectile
- Explosion on impact
- Sets enemies ablaze

### Magic Casting Requirements
- **Old System**: Required "charging" staves with bolt magic first
- **Modern System**: Staves ready immediately
- Wands limited to bolt magic only

---

## Fighter Combo Chains

### Three-Tier Chain System

#### Level 1 (Starters)
- Focused Fist
- Charging Strike
- Combo: Counter Punch

#### Level 2 (Continuers)
- Spinning Uppercut
- Somersault Kick

#### Level 3 (Finishers)
- Drop Kick
- Pummel

### Chain Mechanics
- **Limit Gauge** appears after Level 1/2 skills
- Can repeat same level before advancing
- Level 3 ends chain completely
- Gauge expires after ~3 seconds
- Failed/blocked skills don't trigger gauge

### Example Combo
```
Charging Strike (L1) →
Spinning Uppercut (L2) →
Drop Kick (L3, creates distance) →
Tumble (gap close) →
Focused Fist (L1, charged) →
Somersault Kick (L2) →
Pummel (L3, finish)
```

---

## Weapon Upgrades & Enchantments

### Special Upgrade System

#### R-Type (Red) Upgrades
- Focus on critical damage
- Stacks additively with skill bonuses
- Red glow effect (intensity scales with level)

#### S-Type (Blue) Upgrades
- Flat and percentage damage increase
- Blue glow effect
- Generally more consistent than R-Type

#### Upgrade Requirements
1. Complete all regular upgrades
2. Apply gem upgrade
3. Then eligible for special upgrades

#### Failure Penalties
- Steps 0-5: Drop one step
- Steps 5-7: Lose upgrade capability (needs Edern repair)

### Enchantment System

#### Rank Sequences
- Must apply in order (Rank 9 → 8 → 7...)
- Cannot skip ranks
- Higher ranks have better stats but higher risk

#### Failure Effects
- Rank 9-7: No penalty on failure
- Rank 6-4: Item destruction possible
- Rank 3-1: High destruction chance

#### Popular Combat Enchants
```
Prefix:
- Fox Hunter: +2-4 max damage
- Enormous: +6 max damage (giant-only)

Suffix:
- Raven/Crow: +5-8 max, +2-4 min damage
- Stiff: +10 critical on headgear
```

---

## Environmental Factors

### Campfire Combat Bonuses

#### Archery Enhancement
- **+50% damage** with bows near campfire
- Arrows become fire arrows (visual effect)
- Significant tactical advantage

#### Recovery Benefits
```
Rest Effect Scaling:
- Base: 100%
- Rank 5: 190% (shows as 150%)
- Rank 1: 500%
- Accelerated wound recovery
```

#### Strategic Use
- Cannot heal wounds in dungeons without campfire
- Food sharing: +50% effect regardless of party size
- Scented Candles: 1-minute rest = 36-minute buff

### Weather System

#### Weather Types
- Sunny
- Overcast
- Rain
- Thunderstorm

#### Combat Impact (Minimal)
Unlike other games, weather has limited combat effects:
- Rain reduces campfire duration
- No accuracy penalties
- No damage modifiers
- No skill effectiveness changes

---

## Pet Combat System

### AI Modes

#### Four Default Settings
1. **Auto Attack**: Targets nearby enemies
2. **Collaborative**: Attacks with master
3. **Healing**: Continuous heal focus
4. **Command**: Manual control only

### Custom AI Scripting
- Saved locally (client-side)
- Player-created behavior patterns
- Popular tactics:
  - Ice-Counter combo
  - Ninja mode (melee only)
  - Familiar mode (magic only)

### Combat Considerations
- Pets count toward multi-aggro limits
- Can accidentally pull entire rooms
- Strategic for single-target isolation
- Share aggro with owner

### Example AI Script
```
IF enemy is charging Smash THEN
    Cancel current skill
    Use Counter
ELSE IF enemy is using Defense THEN
    Use Smash
ELSE IF enemy distance > 5 THEN
    Use ranged attack
ELSE
    Use normal attack
END
```

---

## Implementation Priority for FairyGate

### High Priority (Core Combat Feel)
1. Critical Hit System (30% cap, Protection interaction)
2. Balance System (80% cap, normal distribution)
3. Injury/Wound System (anti-regeneration)
4. Protection dual effect
5. Deadly Status (two-stage death)
6. Heavy Stander layers

### Medium Priority (Depth & Variety)
7. Dual Wielding calculations
8. Combat Power scaling
9. Enhanced aggro states
10. Knockdown gauge visualization
11. Skill rank balance bonuses
12. Fighter combo chains

### Lower Priority (Polish)
13. Final Hit burst mode
14. Transformation system
15. Special weapon upgrades
16. Complex enchantments
17. Campfire bonuses
18. Magic fusion system
19. Pet AI
20. Animation canceling

---

## Conclusion

Classic Mabinogi's combat depth came from layered systems that rewarded knowledge and mastery over reflexes. The slow, methodical pace wasn't a limitation—it was the foundation that enabled tactical decision-making and meaningful player choice. Every mechanic, from the 2-second Smash charge to the complex Protection calculations, served to create a unique combat experience that stood apart from typical MMO action combat.

The key to recreating this experience is understanding that these mechanics work together as a cohesive whole. The prediction-based gameplay requires slow skill charging. The vulnerability windows require commitment mechanics. The tactical depth requires multiple defensive layers and counter-play options. Remove any piece, and the entire system loses its identity—as proven by the 2012 Dynamic Combat update that transformed Mabinogi into "just another action MMO."