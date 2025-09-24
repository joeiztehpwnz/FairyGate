# Unity Combat Game - Minimal Implementation Guide

## Project Overview
- **Engine:** Unity 2023.2.20f1
- **Architecture:** Modular design pattern
- **Version Control:** Git (required)
- **Development Focus:** **Minimal viable combat system**
- **Code Style:** Clean, minimal verbosity
- **Process:** Always query before implementing features
- **Documentation:** Track progress in updates.md
- **Implementation Strategy:** Minimal assets, maximum combat functionality

## Minimal Implementation Strategy

### Core Philosophy
Focus 100% on combat mechanics with placeholder visuals. Build a fully functional combat system that can be tested, balanced, and validated without getting distracted by art, audio, or environment polish.

### Development Phases
- **Phase 1A (Weeks 1-2):** Core combat mechanics
- **Phase 1B (Week 3):** Combat polish and AI opponent
- **Phase 1C (Week 4):** Combat validation and balance testing

### Minimal Asset Requirements

**3D Models:**
- 2x Character representations: Simple capsules with different colors (Player: Blue, Enemy: Red)
- 4x Weapon representations: Basic geometric shapes extending from character
  - Sword: Rectangular cube
  - Spear: Long cylinder
  - Dagger: Small cube
  - Mace: Cube with smaller cube head
- 1x Arena: Flat rectangular plane (10x10 units)

**Materials:**
- Character materials: Basic colored materials (Blue/Red for identification)
- Weapon materials: Simple colored materials (Gray, Brown, Silver, Dark Gray)
- Ground material: Basic gray material
- Status effect materials: Glowing/emissive materials for visual feedback

**UI Elements:**
- Health bar: Simple red rectangle with background
- Stamina bar: Simple blue rectangle with background
- Status indicators: Text displays for current state
- Debug information: Console text for skill interactions and calculations

**Audio (Optional/Placeholder):**
- Hit impact: Single basic impact sound (can use Unity default)
- Skill charge: Simple beep or notification sound
- Status effects: Basic notification sounds
- Background music: None required

**Environment:**
- Simple combat arena: Flat plane with invisible boundary colliders
- Single directional light for basic illumination
- Static camera positioned at 45-degree angle above arena
- Two spawn points at opposite corners

## Combat System Design

### Core Mechanics
The combat system follows **Rock Paper Scissors** logic with 5 skills.

### Skills & Input Mapping
1. **Attack** (Key: 1)
2. **Defense** (Key: 2)
3. **Counterattack** (Key: 3)
4. **Smash** (Key: 4)
5. **Windmill** (Key: 5)

### Skill Interaction Matrix

**Complete Interactions:**
1. Attack vs Defense → Attacker stunned, defender blocks (0 damage)
2. Attack vs Counter → Attacker knocked down, defender takes 0 damage, reflects calculated damage back (see Counter Reflection Details)
3. Attack vs Smash → Speed determines winner
4. Attack vs Windmill → Speed determines winner
5. Counter vs Attack → Attacker knocked down, defender takes 0 damage, reflects calculated damage back (see Counter Reflection Details)
6. Counter vs Smash → Attacker knocked down, defender takes 0 damage, reflects calculated damage back (see Counter Reflection Details)
7. Counter vs Windmill → Attacker knocked down, defender takes 0 damage, reflects calculated damage back (see Counter Reflection Details)
8. Smash vs Defense → Defender knocked down, takes 75% reduced damage
9. Smash vs Counter → Attacker knocked down, defender takes 0 damage, reflects calculated damage back (see Counter Reflection Details)
10. Smash vs Attack → Speed determines winner
11. Smash vs Windmill → Speed determines winner
12. Windmill vs Defense → No status effects, defender blocks (0 damage)
13. Windmill vs Counter → Attacker knocked down, defender takes 0 damage, reflects calculated damage back (see Counter Reflection Details)
14. Windmill vs Attack → Speed determines winner
15. Defense vs Smash → Defender knocked down, takes 75% reduced damage
16. Defense vs Windmill → No status effects, defender blocks (0 damage)
17. Same vs Same → Speed determines winner

**Speed Resolution Rules:**
- Winner: Skill executes successfully, deals full damage + effects
- Loser: Skill cancelled, takes full damage + effects from winner
- Tie: Simultaneous execution, both take damage + effects

**Non-Interactions (Defensive Skills):**
- Counter vs Counter = No effect
- Defense vs Defense = No effect
- Defense vs Counter = No effect

### Weapon System

**Weapon Types & Stats:**

| Weapon | Range | Base Damage | Speed | Stun Duration |
|--------|-------|-------------|-------|---------------|
| **Sword** | 1.5 | 10 | 1.0 | 1.0s |
| **Spear** | 2.5 | 8 | 0.8 | 0.8s |
| **Dagger** | 1.0 | 6 | 1.5 | 0.5s |
| **Mace** | 1.2 | 15 | 0.6 | 1.5s |

**Weapon Properties:**
- **Range**: Attack reach for collision detection (units)
- **Base Damage**: Starting damage before Strength/Defense modifiers
- **Speed**: Used in consolidated speed resolution formula (Weapon Speed + Dexterity ÷ 5) × (1 + Weapon Speed Modifier)
- **Stun Duration**: Base stun time applied to hit opponents (modified by Focus)

**Weapon Balance Philosophy:**
- **Sword**: Balanced baseline for all stats
- **Spear**: Range advantage with moderate damage/speed trade-offs
- **Dagger**: Speed advantage with range/damage trade-offs
- **Mace**: Damage/stun advantage with speed/range trade-offs

### Range System
- All offensive skills require target to be **within weapon range**
- Each skill has **range boxes** (collision detection zones) based on equipped weapon
- Range is primarily determined by weapon type
- Range can be modified by stats and other variables

### Combat Effects System

#### Stun
- **Effect:** Character becomes immobile but can attempt to charge skills
- **Duration:** Base weapon stun duration × (1 - Focus/30)
- **Recovery Time:** Base weapon stun duration (modified by Focus)
- **Recovery:** Player can move when stun ends
- **Skill Charging:** Stunned character can charge skills and use them if successfully charged
- **Stun Stacking:** New stun effects reset duration (don't extend or stack)

#### Knockdown (Two Types)

**Interaction Knockdown:**
- **Effect:** Character is physically displaced and incapacitated (more severe than stun)
- **Trigger:** Specific skill interactions (Counter beats Attack/Smash/Windmill, Smash beats Defense)
- **Duration:** 2.0 seconds × (1 - Focus/30)
- **Physical Displacement:** Character moves/falls during knockdown effect
- **Meter Independence:** Does not interact with knockdown meter system

**Meter Knockdown:**
- **Effect:** Character is physically displaced and incapacitated (same as interaction knockdown)
- **Trigger:** When knockdown meter reaches 100% from accumulated damage
- **Duration:** 2.0 seconds × (1 - Focus/30)
- **Meter Behavior:** Meter continues normal decay (-5/second), no reset

**General Knockdown Properties:**
- **Vulnerability:** Character is NOT invulnerable while knocked down
- **Physical Displacement:** Character moves/falls during knockdown effect
- **Meter Behavior:** Meter continues normal decay in all scenarios, never resets

**Status Effect Stacking Rules:**
- **Stun while Stunned**: New stun resets duration (doesn't extend)
- **Stun while Knocked Down**: Cannot be stunned while knocked down
- **Knockdown while Stunned**: Knockdown overrides stun, cancels stun effect
- **Multiple Knockdowns**: New knockdown resets duration (doesn't extend)
- **Priority Order**: Knockdown > Stun > Normal state

#### Block vs Damage Reduction

**Block (Complete Attack Negation):**
- **Effect:** Attack is completely negated, no damage dealt
- **Attacker Penalty:** Attacker receives full stun duration
- **Defender Penalty:** Defender receives half of attacker's stun duration (modified by defender's Focus stat)
- **Blockable Skills:** Attack, Windmill (when using Defense)
- **Unblockable Skills:** Smash (breaks through Defense)

**Damage Reduction (Separate Mechanic):**
- **Base Reduction:** 75% when defender takes reduced damage
- **Physical Defense Modifier:** Damage Reduction = Min(0.90, Base Reduction × (1 + Physical Defense/20))
- **Final Formula:** Final Damage = Original Damage × (1 - Damage Reduction)
- **Application:** When defender partially mitigates but still takes damage
- **Examples:** Smash vs Defense, failed defensive actions
- **Cap:** Maximum 90% damage reduction to prevent amplification

### Knockdown Meter System
- **Range:** 0-100% per character
- **Buildup Values:**
  - **Attack:** +15 points per hit (affected by stats)
  - **Smash:** Causes immediate knockdown (bypasses meter entirely)
  - **Windmill:** Causes immediate knockdown (bypasses meter entirely)
- **Decay Rate:** -5 points per second continuously (no resets)
- **Knockdown Trigger:** Automatic knockdown at 100%, meter continues normal decay
- **No Meter Resets:** Meter never resets to 0%, only decays naturally
- **Stat Influence:**
  - **Strength:** Increases Attack buildup by (Strength/10)
  - **Focus:** Reduces incoming Attack buildup by (Focus/30)
- **Important:** Smash/Windmill do NOT interact with or affect the knockdown meter system

### Character Stats System

#### Base Stats (7 total)
1. **Strength** - Affects: Damage output, knockdown meter buildup on opponents
2. **Dexterity** - Affects: Attack speed, skill charge speed, movement speed
3. **Intelligence** - Reserved for future magical combat system
4. **Focus** - Affects: Stun resistance, faster recovery from status effects, stamina efficiency
5. **Physical Defense** - Affects: Physical damage reduction
6. **Magical Defense** - Reserved for future magical combat system
7. **Vitality** - Affects: Maximum health points

**Active Stats for Combat:**
- **Strength**: Increases base damage and how much knockdown meter is added to opponents
- **Dexterity**: Determines winner in speed-based skill conflicts, faster skill charging, movement speed
- **Focus**: Reduces incoming stun duration, faster recovery from knockdown, reduces stamina drain rates
- **Physical Defense**: Reduces physical damage taken from all sources
- **Vitality**: Determines maximum health pool

### Health & Damage System

**Health Points:**
- **Base Health**: 100 + (Vitality × 5)
- **Static Health**: No regeneration during combat
- **Healing**: Only through items or abilities

**Damage Calculation:**
- **Base Formula**: (Weapon Base Damage + Strength) - Physical Defense
- **Minimum Damage**: 1 (cannot be reduced to 0)
- **Block Mechanic**: Complete damage negation (0 damage) when attack is blocked
- **Counter Mechanic**: Counter user takes 0 damage, reflects attacker's calculated damage (after attacker's Physical Defense reduction) back to attacker
- **Damage Reduction**: When applicable, uses capped formula from Block vs Damage Reduction section
- **Speed Resolution**: Winner deals full calculated damage
- **Status Effects**: Applied separately from damage calculations

**Counter Reflection Details:**
- **Damage Reflected**: (Attacker's Weapon Base Damage + Attacker's Strength) - Attacker's Physical Defense
- **Reflection Timing**: Applied immediately after counter resolves
- **Defense Application**: Reflected damage is reduced by the attacker's own Physical Defense, then applied to attacker
- **No Double Defense**: Counter user's Physical Defense does not apply to reflected damage calculation
- **Example**: Attacker with 20 base damage and 5 Physical Defense reflects 15 damage back to themselves

**Death:**
- **Trigger**: Character reaches 0 health points
- **Effect**: Character dies and is defeated
- **Match End**: Death immediately ends the current combat test
- **Reset**: Press 'R' key to reset scene for continued testing
- **State Reset**: All meters, timers, and status effects immediately stop and reset

### Movement & Combat States System

**Basic Movement:**
- **Controls**: WASD keys for 8-directional movement
- **Movement Type**: Continuous movement (not grid-based)
- **Speed Formula**: Base Speed 5.0 + (Dexterity × 0.2) units per second
- **Boundaries**: Environmental boundaries (not infinite space)

**Combat State Management:**
- **Idle State**: Free movement, no skill usage allowed
- **Combat State**: Triggered by targeting enemy (TAB key), required for skill charging
- **Target Cycling**: TAB to cycle through enemies within range
- **Exit Combat**: ESC key to return to idle state (skills must be cancelled first)

**Movement During Actions:**
- **While Idle**: Free movement, cannot attack
- **While Charging Skills**: Can move with restrictions (see skill-specific rules)
- **During Skill Execution**: Immobilized for duration of skill
- **While Stunned**: No movement allowed
- **While Knocked Down**: No movement allowed

**Skill-Specific Movement Restrictions:**
- **Attack & Smash**: Normal movement speed while charging
- **Defense**: 30% movement speed reduction while charging AND active (waiting state)
- **Counter**: 30% movement speed reduction while charging, immobilized while active (waiting state)
- **Windmill**: 30% movement speed reduction while charging, immobilized during execution
- **All Skills**: Movement stops instantly when transitioning to execution phase

**Range & Positioning:**
- **Avoiding Attacks**: Can move out of range to avoid incoming attacks
- **Attack Priority**: If in range at execution, attack takes priority
- **Tactical Positioning**: Different weapon ranges create strategic positioning

**Weapon Range Interactions:**
- **Long vs Short Range**: Spear (2.5) can attack Dagger (1.0) from outside counter/defense range
- **Range Advantage**: Longer weapons can initiate attacks outside shorter weapon's reactive range
- **Counter-Play**: Shorter weapons must close distance or use movement to get in range
- **Positioning Strategy**: Players with range advantage should maintain distance
- **Range Overlap**: When both players in each other's range, normal interactions apply

**Unified Range Checking Rules:**
- **Offensive Skills (Attack, Smash, Windmill)**: Range checked at skill execution moment only
- **Reactive Skills (Defense, Counter)**:
  - Initial range check when entering waiting state
  - Secondary range check when responding to incoming attacks
  - Must be within range for both checks to successfully respond
- **Speed Resolution Participation**: Both offensive skills must be within their respective weapon ranges to participate in speed conflicts
- **Post-Resolution Range Check**: After speed resolution determines winner, winner's range determines if attack connects
- **Miss Scenario**: If winner is out of range after resolution, attack misses completely (no damage or effects)

### Stamina System

**Stamina Points:**
- **Base Stamina**: 100 + (Focus × 3)
- **Focus Efficiency**: Stamina drain rates reduced by (Focus ÷ 15)
- **No Passive Regeneration**: Stamina does not regenerate during combat

**Stamina Costs:**
- **Defense (Waiting State)**: -3 stamina/second (modified by Focus)
- **Counter (Waiting State)**: -5 stamina/second (modified by Focus)
- **Attack**: -2 stamina per use
- **Smash**: -4 stamina per use
- **Windmill**: -3 stamina per use

**Stamina Regeneration:**
- **Rest State**: Press 'X' key to enter immobile rest state
- **State Change**: Rest automatically exits combat state and enters idle state
- **Rest Rate**: +25 stamina/second while resting
- **Rest Interruption**: Taking damage cancels rest state
- **Re-entering Combat**: Must target enemy again (TAB) after rest ends
- **Item Restoration**: Stamina items provide instant or overtime restoration
- **Combat Usage**: Items can be used during combat

**Stamina Requirements & Depletion:**
- **Minimum Requirements**: Must have full stamina cost available to use skill
  - Attack: 2 stamina minimum
  - Smash: 4 stamina minimum
  - Windmill: 3 stamina minimum
  - Defense: 3 stamina minimum (for initial activation)
  - Counter: 5 stamina minimum (for initial activation)
- **Skill Lock**: Must regenerate required stamina amount before skill can be used again
- **No Negative Stamina**: Stamina cannot go below 0

**Stamina Depletion Edge Cases:**
- **Waiting State Auto-Cancel**: Reactive skills auto-cancel when stamina drops below their activation cost during waiting states
- **Grace Period**: 0.1 second grace period before auto-cancel triggers to prevent instant cancellation
- **Skill Completion Protection**: Once a skill's active frames begin, it completes regardless of stamina level
- **Partial Drain Tolerance**: Skills don't auto-cancel during execution even if stamina depletes mid-animation

### Combat Flow
1. Player enters combat state by targeting enemy (TAB key)
2. Player inputs skill (Keys 1-5) to begin charging - requires combat state
3. Range check performed based on equipped weapon at execution moment
4. If in range: Execute skill interaction
5. Resolve skill interaction based on matrix (winner determined by speed if applicable)
6. Apply damage modified by stats (Strength vs Physical Defense)
7. Apply status effects (stun/knockdown/block) based on interaction outcome
8. Update knockdown meter for hit character
9. Apply weapon-specific stun duration (modified by Focus stat)
10. Reset used skill to uncharged state
11. Check for knockdown meter threshold (100%)

### Skill Execution System

**Two-Phase Skill System:**

**Phase 1: Skill Charging**
- **Single Use**: Each skill must be charged before use (one skill at a time)
- **Charge Time**: 2.0 seconds base ÷ (1 + Dexterity/10)
- **Charge Input**: Press skill key once to start charging
- **Charge Interruption**: Being hit pauses charging (doesn't reset to 0)
- **Charge Cancellation**: Spacebar cancels current charging/active skill
- **Skill Switching**: Pressing different skill key cancels current and starts new charge
- **Combat Exit Restriction**: Must cancel all active skills before exiting combat state (ESC)

**Phase 2: Skill Execution**
- **Execute Input**: Press charged skill key again to execute
- **Execution Phases**: Startup → Active → Recovery frames
- **Immobilization**: Player immobilized during entire execution

**Standard Skills (Attack, Smash, Windmill):**
- **Attack**: Startup 0.2s, Active 0.2s, Recovery 0.3s
- **Smash**: Startup 0.5s, Active 0.3s, Recovery 0.8s
- **Windmill**: Startup 0.3s, Active 0.4s, Recovery 0.5s

**Reactive Skills (Defense, Counter):**
- **Defense**: Startup 0.1s, Active (waiting state), Recovery 0.2s
- **Counter**: Startup 0.1s, Active (waiting state), Recovery 0.4s
- **Waiting State**: Remains active until interaction, cancellation, or stamina depletion

**Weapon Speed Modifiers (Apply to Both Execution and Speed Resolution):**
- **Dagger**: -20% startup/recovery times, +20% speed resolution modifier
- **Sword**: Baseline (no modifier)
- **Spear**: +10% startup/recovery times, -10% speed resolution modifier
- **Mace**: +30% startup/recovery times, -30% speed resolution modifier

**Weapon Speed Resolution Modifiers:**
| Weapon | Speed Resolution Modifier |
|--------|---------------------------|
| **Dagger** | +0.20 (+20%) |
| **Sword** | +0.00 (baseline) |
| **Spear** | -0.10 (-10%) |
| **Mace** | -0.30 (-30%) |

**Note**: Speed calculation uses the consolidated formula from Speed Resolution System section above.

**Interruption Rules:**
- **Startup Frames**: Can be interrupted by taking damage
- **Active Frames**: Cannot be interrupted (skill completes) - uncancellable by any means
- **Recovery Frames**: Can be interrupted by taking damage
- **Skill Switching Limitation**: Cannot switch skills during active frames
- **Combat Exit Limitation**: Cannot exit combat during active frames
- **Interruption Effect**: Skill cancelled, damage/effects applied normally

### Speed Resolution System

**Speed Calculation:**
- **Formula**: (Weapon Speed + Dexterity ÷ 5) × (1 + Weapon Speed Modifier)
- **Timing**: Speed compared at execution moment (when skills collide)
- **Examples**:
  - Dagger: (1.5 + Dex÷5) × 1.20
  - Sword: (1.0 + Dex÷5) × 1.00
  - Mace: (0.6 + Dex÷5) × 0.70

**Speed Conflict Resolution:**
- **Winner**: Skill executes successfully, deals full damage + status effects
- **Loser**: Skill cancelled, takes full damage + status effects from winner
- **Tie**: Simultaneous execution, both players take damage + effects

**Multi-Character Battle Rules:**
- **Step 1**: Offensive skills resolve speed conflicts first
- **Step 2**: Defensive skills respond to the speed resolution winner
- **Step 3**: Multiple defensive skills resolve simultaneously if in range
- **Parallel Resolution**: All valid defensive responses execute at the same time
- **Effect Stacking**:
  - Multiple blocks = same result as single block (0 damage)
  - Multiple counters = multiple instances of reflection damage applied to attacker
  - **Multi-Counter Mechanics**:
    - Each counter calculates reflection damage independently using their own stats
    - Multiple counters can respond simultaneously to the same attack
    - Each counter applies its own reflection damage instance to the attacker
    - All reflection damage instances resolve simultaneously (not sequentially)
    - Example: Attack deals 20 damage, two counters with different Physical Defense values each reflect their calculated amounts simultaneously
- **Example**: Attack vs Smash (speed resolves) → Defense blocks winner AND Counter reflects damage simultaneously

**Range Checking in Speed Resolution:**
- **Participation Requirement**: Both skills must be within their respective ranges to participate in speed resolution
- **Winner's Range Check**: After speed resolution, winner's range determines if attack connects
- **Miss Scenario**: If winner is out of range after speed resolution, attack misses (no damage/effects)
- **Example**: Spear user loses speed to Dagger user → if Dagger out of Spear's range, Spear attack misses
- **Range Priority**: Each weapon uses its own range for checking, not the opponent's range

**Applicable Interactions:**
Speed resolution applies ONLY to offensive vs offensive skill combinations:
- Attack vs Smash
- Attack vs Windmill
- Smash vs Attack
- Smash vs Windmill
- Windmill vs Attack
- Same skill vs Same skill (Attack vs Attack, Smash vs Smash, Windmill vs Windmill)

**Defensive Skills Priority:**
- Defense and Counter always work when properly activated (not speed-dependent)
- Defensive skills respond to attacks after speed resolution determines offensive winner
- Multiple defensive skills can respond to the same attack simultaneously

**Balancing Considerations:**
- **Dagger**: Significant speed advantage (~44% total over Sword: base 1.5 vs 1.0 + 20% speed resolution modifier) but lowest damage/range
- **Mace**: Substantial speed disadvantage (~50% slower than Sword) but highest damage and stun - extreme risk/reward
- **Balance Assessment**: Dagger's speed advantages are notable but not overwhelming given damage/range trade-offs
- **Potential Balancing**: Requires careful testing and potential stat adjustments
- **Dexterity Investment**: Extremely critical for speed-based builds, compounds with weapon bonuses
- **Positioning**: Essential strategy to avoid speed conflicts and leverage range differences

## Technical Requirements

### Project Structure

**Folder Organization:**
```
Assets/
├── Scripts/
│   ├── Combat/
│   │   ├── Core/              # Main combat controllers and managers
│   │   │   ├── CombatController.cs
│   │   │   ├── CombatStateManager.cs
│   │   │   └── CombatFlowOrchestrator.cs
│   │   ├── Skills/            # Individual skill implementations
│   │   │   ├── Base/          # Abstract skill classes
│   │   │   ├── Offensive/     # Attack, Smash, Windmill
│   │   │   └── Defensive/     # Defense, Counter
│   │   ├── Weapons/           # Weapon data and logic
│   │   │   ├── WeaponController.cs
│   │   │   ├── WeaponData.cs
│   │   │   └── RangeDetector.cs
│   │   ├── StatusEffects/     # Status effect systems
│   │   │   ├── StatusEffectManager.cs
│   │   │   ├── StunController.cs
│   │   │   └── KnockdownController.cs
│   │   ├── Stats/             # Character stats and calculations
│   │   │   ├── CharacterStats.cs
│   │   │   ├── DamageCalculator.cs
│   │   │   └── SpeedResolver.cs
│   │   ├── Systems/           # Core combat systems
│   │   │   ├── StaminaSystem.cs
│   │   │   ├── KnockdownMeterTracker.cs
│   │   │   └── HealthSystem.cs
│   │   └── Utilities/         # Helper classes and interfaces
│   │       ├── Interfaces/
│   │       ├── Extensions/
│   │       └── Constants/
│   ├── Input/                 # Combat input handling
│   │   ├── CombatInputHandler.cs
│   │   └── SkillInputProcessor.cs
│   ├── UI/                    # Combat UI elements
│   │   ├── HealthBarController.cs
│   │   ├── StaminaBarController.cs
│   │   └── StatusEffectDisplay.cs
│   ├── Data/                  # ScriptableObjects and configuration
│   │   ├── WeaponData/        # Weapon stat configurations
│   │   ├── SkillData/         # Skill timing and cost data
│   │   └── CharacterData/     # Base character configurations
│   └── Audio/                 # Combat audio systems
│       ├── CombatAudioManager.cs
│       └── SkillAudioTriggers.cs
├── Prefabs/
│   ├── Characters/            # Player and enemy prefabs
│   ├── Weapons/               # Weapon prefabs with components
│   ├── UI/                    # Combat UI prefabs
│   └── Effects/               # Visual effect prefabs
├── Data/                      # ScriptableObject assets
│   ├── Weapons/               # .asset files for each weapon
│   ├── Skills/                # .asset files for skill configurations
│   └── Characters/            # .asset files for character stats
└── Scenes/
    ├── CombatTest.scene       # Main testing scene
    └── CombatArena.scene      # Combat arena for validation
```

**Script Naming Conventions:**
- **Controllers**: `SystemNameController.cs` (e.g., `CombatController.cs`)
- **Managers**: `SystemNameManager.cs` (e.g., `StatusEffectManager.cs`)
- **Data Classes**: `DataTypeName.cs` (e.g., `WeaponData.cs`)
- **Interfaces**: `IActionName.cs` (e.g., `IDamageable.cs`)
- **ScriptableObjects**: `ConfigurationName.cs` (e.g., `WeaponConfiguration.cs`)
- **Utilities**: `UtilityNameHelper.cs` (e.g., `DamageCalculationHelper.cs`)

### Component Architecture

**Core MonoBehaviour Components:**

#### 1. CombatController
- **Purpose**: Main combat orchestration and state management
- **Responsibilities**:
  - Manages combat state transitions (Idle ↔ Combat)
  - Handles TAB targeting and ESC combat exit
  - Coordinates between all combat systems
  - Manages combat flow steps 1-11 from Combat Flow section
- **Key Methods**: `EnterCombat()`, `ExitCombat()`, `ProcessCombatStep()`

#### 2. SkillSystem
- **Purpose**: Handles 5-skill charging and execution system
- **Responsibilities**:
  - Manages skill charging timers and interruptions
  - Processes skill input (Keys 1-5, Spacebar cancellation)
  - Coordinates skill execution phases (Startup → Active → Recovery)
  - Handles skill switching and combat exit restrictions
- **Key Methods**: `ChargeSkill()`, `ExecuteSkill()`, `CancelSkill()`, `SwitchSkill()`

#### 3. WeaponController
- **Purpose**: Weapon stats, range detection, and modifiers
- **Responsibilities**:
  - Stores current weapon data (range, damage, speed, stun duration)
  - Handles weapon speed modifiers for execution and speed resolution
  - Manages range collision detection and validation
  - Applies weapon-specific timing modifications
- **Key Methods**: `CheckRange()`, `GetSpeedModifier()`, `GetDamageValue()`

#### 4. StatusEffectManager
- **Purpose**: Manages all status effects (stun, knockdown, block)
- **Responsibilities**:
  - Handles stun duration calculations with Focus stat integration
  - Manages knockdown states (interaction and meter-based)
  - Processes status effect stacking rules and priorities
  - Controls character movement restrictions during effects
- **Key Methods**: `ApplyStun()`, `ApplyKnockdown()`, `ProcessStatusStack()`

#### 5. StaminaSystem
- **Purpose**: Complete stamina management with edge cases
- **Responsibilities**:
  - Tracks stamina costs and regeneration
  - Handles auto-cancel logic with grace periods
  - Manages rest state (X key) and combat state interactions
  - Processes Focus stat efficiency modifications
- **Key Methods**: `ConsumeStamina()`, `RegenerateStamina()`, `CheckAutoCancel()`, `EnterRestState()`

#### 6. DamageCalculator
- **Purpose**: All damage calculations and counter reflection
- **Responsibilities**:
  - Processes base damage formula with stat modifications
  - Handles counter reflection calculations with Physical Defense
  - Applies damage reduction formulas with caps
  - Manages minimum damage rules and block mechanics
- **Key Methods**: `CalculateDamage()`, `ProcessCounterReflection()`, `ApplyDamageReduction()`

#### 7. SpeedResolver
- **Purpose**: Speed conflict resolution system
- **Responsibilities**:
  - Calculates speed values using consolidated formula
  - Resolves speed conflicts for offensive vs offensive skills
  - Handles range checking during speed resolution
  - Manages tie scenarios and simultaneous execution
- **Key Methods**: `CalculateSpeed()`, `ResolveSpeedConflict()`, `CheckRangeForResolution()`

#### 8. KnockdownMeterTracker
- **Purpose**: Knockdown meter buildup and decay
- **Responsibilities**:
  - Tracks meter buildup from Attack hits with stat modifiers
  - Manages continuous -5/second decay with no resets
  - Triggers meter knockdown at 100% threshold
  - Handles Smash/Windmill bypass mechanics
- **Key Methods**: `AddMeterBuildup()`, `ProcessMeterDecay()`, `CheckMeterThreshold()`

**Component Communication:**
- **Event-driven architecture** using Unity Events and C# Actions
- **Dependency injection** through constructor or property injection
- **Service locator pattern** for core systems like DamageCalculator
- **Observer pattern** for status effect notifications

### Data Architecture

**ScriptableObject Configurations:**

#### WeaponData.cs
```csharp
[CreateAssetMenu(fileName = "New Weapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Stats")]
    public string weaponName;
    public float range;
    public int baseDamage;
    public float speed;
    public float stunDuration;

    [Header("Speed Modifiers")]
    public float executionSpeedModifier;     // -20% to +30%
    public float speedResolutionModifier;    // -0.30 to +0.20

    [Header("Visual References")]
    public GameObject weaponPrefab;
    public Sprite weaponIcon;
    public AudioClip[] hitSounds;
}
```

#### SkillData.cs
```csharp
[CreateAssetMenu(fileName = "New Skill", menuName = "Combat/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Properties")]
    public string skillName;
    public SkillType skillType;             // Attack, Defense, Counter, Smash, Windmill
    public KeyCode inputKey;

    [Header("Timing")]
    public float chargeTime = 2.0f;         // Base charge time
    public float startupTime;               // Execution startup frames
    public float activeTime;                // Active frames
    public float recoveryTime;              // Recovery frames

    [Header("Costs")]
    public int staminaCost;                 // Per-use or per-second cost
    public bool isDrainOverTime;            // For Defense/Counter waiting states

    [Header("Effects")]
    public bool canBeInterrupted = true;    // During startup/recovery
    public float movementSpeedModifier = 1.0f; // Movement restriction while charging
}
```

#### CharacterStats.cs
```csharp
[CreateAssetMenu(fileName = "New Character Stats", menuName = "Combat/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Combat Stats")]
    public int strength = 10;
    public int dexterity = 10;
    public int intelligence = 10;           // Reserved for future use
    public int focus = 10;
    public int physicalDefense = 10;
    public int magicalDefense = 10;         // Reserved for future use
    public int vitality = 10;

    [Header("Derived Values")]
    public int MaxHealth => 100 + (vitality * 5);
    public int MaxStamina => 100 + (focus * 3);
    public float MovementSpeed => 5.0f + (dexterity * 0.2f);
    public float StaminaEfficiency => focus / 15f;
}
```

**Event System Architecture:**

#### CombatEvents.cs
```csharp
public static class CombatEvents
{
    // Skill Events
    public static UnityAction<SkillType> OnSkillCharged;
    public static UnityAction<SkillType, bool> OnSkillExecuted; // skill, wasSuccessful
    public static UnityAction<SkillType> OnSkillCancelled;

    // Combat State Events
    public static UnityAction OnCombatEntered;
    public static UnityAction OnCombatExited;
    public static UnityAction<Transform> OnTargetChanged;

    // Damage Events
    public static UnityAction<int, Transform> OnDamageDealt;    // damage, target
    public static UnityAction<int, Transform> OnDamageReceived; // damage, source
    public static UnityAction<Transform> OnCharacterDied;

    // Status Effect Events
    public static UnityAction<StatusEffectType, float> OnStatusEffectApplied;
    public static UnityAction<StatusEffectType> OnStatusEffectRemoved;

    // Stamina Events
    public static UnityAction<int, int> OnStaminaChanged; // current, max
    public static UnityAction OnRestStateEntered;
    public static UnityAction OnRestStateExited;
}
```

**State Machine Implementation:**

#### CombatState Enum
```csharp
public enum CombatState
{
    Idle,           // Free movement, no combat actions
    Combat,         // Targeting enemy, can charge skills
    Charging,       // Charging a skill
    Executing,      // Skill in startup/active/recovery frames
    Stunned,        // Cannot move, can charge skills
    KnockedDown,    // Cannot move or act
    Resting,        // X key rest state, stamina regeneration
    Dead            // Character defeated
}
```

#### SkillExecutionState Enum
```csharp
public enum SkillExecutionState
{
    Uncharged,      // Skill ready to be charged
    Charging,       // Skill charging (2 seconds base)
    Charged,        // Skill ready to execute
    Startup,        // Skill startup frames
    Active,         // Skill active frames (uncancellable)
    Recovery,       // Skill recovery frames
    Waiting         // Defense/Counter waiting state
}
```

### Development Commands

**Build Commands:**
```bash
# Development build for testing
Unity.exe -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -buildPath ./Builds/Dev/CombatTest.exe

# Release build optimized
Unity.exe -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -buildPath ./Builds/Release/CombatGame.exe -buildOptions Development
```

**Test Commands:**
```bash
# Run all unit tests
Unity.exe -batchmode -quit -projectPath . -runTests -testResults ./TestResults/results.xml

# Run specific test category
Unity.exe -batchmode -quit -projectPath . -runTests -testCategory "Combat" -testResults ./TestResults/combat-results.xml

# Performance profiling build
Unity.exe -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -buildPath ./Builds/Profile/CombatProfile.exe -buildOptions Development,ConnectWithProfiler
```

**Code Quality Checks:**
```bash
# Static code analysis
dotnet analyze ./Assembly-CSharp.csproj

# Code formatting check
dotnet format --verify-no-changes

# Custom combat system validation script
python Tools/validate_combat_mechanics.py --check-formulas --check-interactions
```

### Testing Strategy

**Unit Testing Approach:**

#### Combat Calculation Tests
```csharp
[TestFixture]
public class DamageCalculatorTests
{
    [Test]
    public void CalculateDamage_WithBasicStats_ReturnsCorrectValue()
    {
        // Test: (Weapon Base Damage + Strength) - Physical Defense
        // with minimum damage = 1
    }

    [Test]
    public void ProcessCounterReflection_WithPhysicalDefense_ReturnsReducedDamage()
    {
        // Test: (Attacker's Weapon Base Damage + Attacker's Strength) - Attacker's Physical Defense
    }

    [Test]
    public void ApplyDamageReduction_WithHighDefense_CapsAt90Percent()
    {
        // Test: Min(0.90, Base Reduction × (1 + Physical Defense/20))
    }
}
```

#### Speed Resolution Tests
```csharp
[TestFixture]
public class SpeedResolverTests
{
    [Test]
    public void CalculateSpeed_WithWeaponModifiers_ReturnsCorrectSpeed()
    {
        // Test: (Weapon Speed + Dexterity ÷ 5) × (1 + Weapon Speed Modifier)
    }

    [Test]
    public void ResolveSpeedConflict_WithTie_ReturnsBothExecute()
    {
        // Test simultaneous execution mechanics
    }
}
```

#### Status Effect Tests
```csharp
[TestFixture]
public class StatusEffectTests
{
    [Test]
    public void ApplyStun_WithFocusStat_ReducesDuration()
    {
        // Test: Base weapon stun duration × (1 - Focus/30)
    }

    [Test]
    public void ProcessStatusStack_KnockdownOverridesStun_CancelsStun()
    {
        // Test status effect priority rules
    }
}
```

**Combat System Testing Methodology:**

#### Integration Testing Framework
- **Combat Flow Tests**: Validate complete 11-step combat flow
- **Skill Interaction Matrix Tests**: Automated testing of all 17 skill interactions
- **Multi-Character Scenario Tests**: Complex battle situations with multiple participants
- **Edge Case Testing**: Stamina depletion, status effect stacking, range checking edge cases

#### Performance Testing Requirements

**Target Performance Metrics:**
- **60 FPS** maintained during complex combat scenarios
- **<16ms** frame time for combat calculations
- **<2ms** for damage calculations and status effect processing
- **Memory usage** under 100MB for combat systems

**Performance Test Suite:**
- **Stress Tests**: 100+ simultaneous combat calculations
- **Memory Leak Detection**: Extended play sessions (30+ minutes)
- **Platform Testing**: Windows, Mac, Linux builds
- **Profiling Integration**: Unity Profiler automation for CI/CD

**Automated Testing Pipeline:**
- **Continuous Integration**: All tests run on each commit
- **Performance Regression Detection**: Automated performance comparison
- **Combat Balance Validation**: Automated weapon/skill balance testing
- **Code Coverage Requirements**: 90%+ coverage for combat-critical systems

### Interface Definitions

**Core Combat Interfaces:**

#### IDamageable.cs
```csharp
public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }

    void TakeDamage(int damage, Transform source);
    void Die();

    // Events
    UnityEvent<int, Transform> OnDamageReceived;
    UnityEvent<Transform> OnDied;
}
```

#### ISkillExecutor.cs
```csharp
public interface ISkillExecutor
{
    SkillExecutionState CurrentState { get; }
    SkillType CurrentSkill { get; }
    float ChargeProgress { get; }

    bool CanChargeSkill(SkillType skillType);
    bool CanExecuteSkill(SkillType skillType);

    void StartCharging(SkillType skillType);
    void ExecuteSkill(SkillType skillType);
    void CancelSkill();

    // Events
    UnityEvent<SkillType> OnSkillCharged;
    UnityEvent<SkillType, bool> OnSkillExecuted;
}
```

#### IStatusEffectTarget.cs
```csharp
public interface IStatusEffectTarget
{
    List<StatusEffect> ActiveStatusEffects { get; }
    bool HasStatusEffect(StatusEffectType type);

    void ApplyStatusEffect(StatusEffect effect);
    void RemoveStatusEffect(StatusEffectType type);
    void ClearAllStatusEffects();

    // Events
    UnityEvent<StatusEffect> OnStatusEffectApplied;
    UnityEvent<StatusEffectType> OnStatusEffectRemoved;
}
```

#### ICombatant.cs
```csharp
public interface ICombatant : IDamageable, ISkillExecutor, IStatusEffectTarget
{
    CharacterStats Stats { get; }
    WeaponController Weapon { get; }
    Transform Transform { get; }

    bool IsInCombat { get; }
    Transform CurrentTarget { get; }

    void EnterCombat(Transform target);
    void ExitCombat();
    void SetTarget(Transform target);

    // Range and positioning
    bool IsInRangeOf(Transform target);
    float GetDistanceTo(Transform target);

    // Events
    UnityEvent OnCombatEntered;
    UnityEvent OnCombatExited;
    UnityEvent<Transform> OnTargetChanged;
}
```

#### IStaminaUser.cs
```csharp
public interface IStaminaUser
{
    int CurrentStamina { get; }
    int MaxStamina { get; }
    bool IsResting { get; }

    bool HasStaminaFor(int cost);
    bool ConsumeStamina(int amount);
    void RegenerateStamina(int amount);

    void StartResting();
    void StopResting();

    // Events
    UnityEvent<int, int> OnStaminaChanged; // current, max
    UnityEvent OnRestStarted;
    UnityEvent OnRestStopped;
}
```

#### ISpeedCalculator.cs
```csharp
public interface ISpeedCalculator
{
    float CalculateSpeed(SkillType skillType, CharacterStats stats, WeaponData weapon);
    SpeedResolution ResolveSpeedConflict(ICombatant combatant1, ICombatant combatant2,
                                       SkillType skill1, SkillType skill2);
}
```

#### IDamageCalculator.cs
```csharp
public interface IDamageCalculator
{
    int CalculateBaseDamage(CharacterStats attackerStats, WeaponData weapon,
                           CharacterStats defenderStats);
    int CalculateCounterReflection(CharacterStats attackerStats, WeaponData attackerWeapon);
    int ApplyDamageReduction(int baseDamage, float reductionPercent,
                            CharacterStats defenderStats);
}
```

### Performance Architecture

**Optimization Strategies:**

#### Object Pooling System
```csharp
public class CombatObjectPool : MonoBehaviour
{
    [Header("Pool Configurations")]
    public GameObject damageNumberPrefab;
    public GameObject hitEffectPrefab;
    public GameObject statusEffectIconPrefab;

    [Header("Pool Sizes")]
    public int damageNumberPoolSize = 50;
    public int hitEffectPoolSize = 30;
    public int statusIconPoolSize = 20;

    // Pools managed by Unity's ObjectPool<T> system
    private ObjectPool<DamageNumber> damageNumberPool;
    private ObjectPool<HitEffect> hitEffectPool;
    private ObjectPool<StatusIcon> statusIconPool;
}
```

#### Update Optimization
```csharp
public class CombatUpdateManager : MonoBehaviour
{
    // Use Unity's Job System for expensive calculations
    private JobHandle speedCalculationJob;
    private JobHandle damageCalculationJob;

    // Stagger updates to spread CPU load
    private readonly FrameStagger staminaUpdater = new FrameStagger(5);  // Every 5 frames
    private readonly FrameStagger meterDecayUpdater = new FrameStagger(3); // Every 3 frames

    void Update()
    {
        // Staggered system updates
        if (staminaUpdater.ShouldUpdate())
            UpdateStaminaSystems();

        if (meterDecayUpdater.ShouldUpdate())
            UpdateKnockdownMeterDecay();
    }
}
```

#### Memory Management
```csharp
public class CombatMemoryManager
{
    // Pre-allocated collections to avoid GC
    private static readonly List<ICombatant> tempCombatantList = new List<ICombatant>(16);
    private static readonly List<StatusEffect> tempStatusEffectList = new List<StatusEffect>(8);

    // String caching for UI updates
    private static readonly Dictionary<int, string> damageStringCache =
        new Dictionary<int, string>();

    // Event argument pooling
    private static readonly Queue<CombatEventArgs> eventArgsPool =
        new Queue<CombatEventArgs>();
}
```

**Performance Monitoring:**
- **Unity Profiler Integration**: Automatic profiling markers for all combat systems
- **Custom Performance Metrics**: Track frame spikes during combat calculations
- **Memory Allocation Tracking**: Monitor GC allocations in critical paths
- **Platform-Specific Optimization**: Different performance profiles for PC vs console

### Development Workflow

**Git Workflow:**
```bash
# Feature branch naming convention
git checkout -b feature/combat-skill-system
git checkout -b bugfix/stamina-auto-cancel
git checkout -b refactor/damage-calculation-optimization

# Commit message format
# [COMBAT] Add speed resolution system with weapon modifiers
# [COMBAT] Fix stamina auto-cancel edge case with grace period
# [COMBAT] Refactor DamageCalculator for better testability
```

**Code Review Requirements:**
- **Combat System Changes**: Require 2 reviewers, including tech lead
- **Performance Critical Changes**: Require profiler results before/after
- **Formula Changes**: Require unit test updates and balance validation
- **Interface Changes**: Require documentation updates

**Continuous Integration Pipeline:**
1. **Code Quality Gates**: Static analysis, formatting, complexity checks
2. **Unit Test Validation**: 90%+ coverage requirement for combat systems
3. **Integration Testing**: Automated skill interaction matrix validation
4. **Performance Regression**: Automated performance comparison vs main branch
5. **Build Verification**: Successful builds for Windows, Mac, Linux targets

**Documentation Standards:**
- **XML Documentation**: Required for all public methods and properties
- **Combat Mechanics**: Document all formulas with examples in code comments
- **Architecture Decisions**: ADR (Architecture Decision Records) for major design choices
- **API Changes**: Changelog maintenance for interface modifications

## Win Conditions & Game Modes

### Testing Phase (Combat System Validation)
- **Victory Type:** Health-based (first to kill opponent)
- **Testing Structure:** Continuous combat testing with instant reset
- **Player Count:** 1v1 focus for combat system testing
- **Reset Control:** Press 'R' key to instantly reset scene and continue testing
- **No Time Limits:** Focus on testing combat mechanics, not timed matches
- **Forfeit Option:** Players can surrender or reset at any time

### Minimal AI Implementation

**Simple Test Enemy (Phase 1B):**
```csharp
public class SimpleTestAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float skillCooldown = 3.0f;          // Time between skill attempts
    public float randomVariance = 2.0f;         // ±2 seconds variance
    public float engageDistance = 3.0f;         // Distance to start combat
    public float optimalRange = 2.0f;           // Preferred fighting distance

    [Header("Skill Selection Weights")]
    public float attackWeight = 30f;            // 30% chance
    public float defenseWeight = 20f;           // 20% chance
    public float counterWeight = 20f;           // 20% chance
    public float smashWeight = 15f;             // 15% chance
    public float windmillWeight = 15f;          // 15% chance

    private float nextSkillTime;
    private Transform player;
    private ICombatant combatant;

    // Simple AI logic:
    // 1. Move toward player if too far
    // 2. Select random skill based on weights
    // 3. Execute skill if in range and cooldown ready
    // 4. No complex strategy or countering
}
```

**AI Behavior Rules:**
- **Movement**: Simple approach player if distance > optimal range
- **Skill Selection**: Weighted random selection every 3-5 seconds
- **No Strategy**: Does not counter player actions or adapt behavior
- **Fixed Stats**: Uses predefined character stats for consistent testing
- **Range Awareness**: Only uses skills when in appropriate range

**AI Testing Configuration:**
```csharp
[Header("Test AI Stats")]
public int aiStrength = 10;        // Balanced for testing
public int aiDexterity = 10;       // Matched to player stats
public int aiFocus = 10;           // Standard resistance
public int aiPhysicalDefense = 10; // Balanced defense
public int aiVitality = 10;        // Standard health pool
```

### Long-Term Vision: Dungeon-Diving Game

**Core Concept:**
- Players descend through dungeon levels using the rock-paper-scissors combat system
- Progressive difficulty with stronger enemies at deeper levels
- Character growth through equipment, stats, and items

**PvE Adaptation:**
- **AI Enemies:** Use same 5-skill system with varying stat distributions
- **Enemy Variety:** Different combinations of weapons, stats, and behavior patterns
- **Boss Encounters:** Unique mechanics building on core combat system
- **Environmental Hazards:** Traps and obstacles using combat mechanics

**Progression Systems:**
- **Character Stats:** Growth through experience and equipment
- **Weapon Variety:** Different weapon types with unique stat distributions
- **Equipment System:** Armor affecting Physical Defense, accessories modifying stats
- **Item System:** Consumables for health/stamina restoration and temporary buffs

**Dungeon Mechanics:**
- **Level Progression:** Increasing difficulty with deeper floors
- **Loot System:** Equipment drops and treasure chests
- **Save/Checkpoint System:** Progress preservation between dungeon runs

## Minimal Implementation Priorities

### **Phase 1A: Core Combat Mechanics (Weeks 1-2)**
**Priority: CRITICAL - Foundation for everything else**

**Week 1:**
1. **Scene Setup**: Simple arena with spawn points and camera
2. **Character Controllers**: Basic capsule characters with WASD movement
3. **Skill System Framework**: Charging, execution states, input handling
4. **Basic Weapon System**: Range detection and weapon switching
5. **Health System**: Damage calculation and death mechanics

**Week 2:**
1. **Status Effects**: Stun and knockdown implementation
2. **Skill Interactions**: All 17 interactions from matrix
3. **Speed Resolution**: Speed calculation and conflict resolution
4. **Debug UI**: Display all states, values, and calculations
5. **Basic Visual Feedback**: Status effect indicators

**Deliverable**: Player can fight using all 5 skills with full mechanics working

### **Phase 1B: Combat Polish & AI (Week 3)**
**Priority: HIGH - Complete combat experience**

1. **Stamina System**: Full implementation with rest mechanics and auto-cancel
2. **Knockdown Meter**: Buildup, decay, and meter-based knockdown
3. **Range System**: Complete unified range checking rules
4. **Simple AI Opponent**: Basic test enemy with weighted skill selection
5. **Combat Flow Validation**: All 11 combat flow steps working correctly

**Deliverable**: Complete 1v1 combat experience with AI opponent

### **Phase 1C: Combat Validation (Week 4)**
**Priority: HIGH - Ensure quality and balance**

1. **Edge Case Testing**: Stamina depletion, status stacking, timing conflicts
2. **Balance Testing**: Weapon effectiveness, stat scaling validation
3. **Performance Optimization**: 60 FPS maintenance during complex combat
4. **Bug Fixes**: Address all discovered issues and edge cases
5. **Reset & Scene Management**: Polish scene reset and state management

**Deliverable**: Polished, balanced, bug-free combat system ready for expansion

### **Future Phases (Post-MVP):**
- **Phase 2:** Enhanced AI with strategic behavior
- **Phase 3:** Visual and audio polish
- **Phase 4:** Multiple arenas and environments
- **Phase 5:** Dungeon progression system

### **Success Metrics for Minimal Implementation:**
- All 17 skill interactions work correctly
- Combat feels responsive and fair
- No game-breaking bugs or infinite loops
- AI provides reasonable challenge for testing
- 60 FPS maintained during combat
- All formulas produce expected results
- Reset functionality works perfectly

## Debug & Development Tools for Minimal Implementation

### **Essential Debug Features**

#### Debug UI Display
```csharp
public class CombatDebugUI : MonoBehaviour
{
    [Header("Debug Display")]
    public bool showDebugInfo = true;
    public bool showRangeCircles = true;
    public bool showStateInfo = true;
    public bool logSkillInteractions = true;

    // On-screen debug information:
    // - Current combat state for each character
    // - Skill charging progress and states
    // - Health/Stamina values with exact numbers
    // - Active status effects with durations
    // - Speed calculation results
    // - Range checking results
    // - Damage calculations with breakdown
}
```

#### Visual Debug Helpers
```csharp
public class CombatVisualizationHelper : MonoBehaviour
{
    [Header("Visualization")]
    public bool showWeaponRanges = true;       // Wireframe circles around characters
    public bool showMovementRestrictions = true; // Color coding for movement speed
    public bool highlightActiveSkills = true;  // Glow effects during skill execution
    public bool showDamageNumbers = true;      // Floating text for damage dealt
    public bool visualizeStatusEffects = true; // Colored auras for stun/knockdown

    // Development-only visual aids:
    // - Range circles for each weapon type
    // - Collision box visualization for skills
    // - Speed comparison indicators
    // - Status effect timers as progress bars
}
```

#### Combat State Logging
```csharp
public class CombatLogger : MonoBehaviour
{
    [Header("Logging Options")]
    public bool logDamageCalculations = true;
    public bool logSkillInteractions = true;
    public bool logStatusEffects = true;
    public bool logRangeChecking = true;
    public bool logSpeedResolution = true;

    // Example log output:
    // [COMBAT] Player Attack vs Enemy Defense -> Player stunned (1.2s), Enemy blocks (0 damage)
    // [DAMAGE] Base: 15 + STR: 10 - DEF: 8 = 17 damage -> Enemy HP: 83/100
    // [SPEED] Player Dagger (2.1) vs Enemy Mace (1.2) -> Player wins
    // [STATUS] Applying Stun to Player: 1.0s base * (1 - 10/30) = 0.67s duration
}
```

### **Testing Shortcuts & Cheats**

#### Development Console Commands
```csharp
public class DeveloperConsole : MonoBehaviour
{
    [Header("Test Commands")]
    public KeyCode toggleConsole = KeyCode.BackQuote; // `~ key

    // Available commands:
    // "health [player/enemy] [amount]" - Set health
    // "stamina [player/enemy] [amount]" - Set stamina
    // "stats [character] [str] [dex] [foc] [def] [vit]" - Modify stats
    // "weapon [character] [sword/spear/dagger/mace]" - Change weapon
    // "kill [player/enemy]" - Instant kill for testing
    // "reset" - Reset scene (same as R key)
    // "speed [multiplier]" - Change game speed for testing
    // "freeze" - Pause combat calculations
}
```

#### Quick Test Scenarios
```csharp
[Header("Quick Test Buttons")]
public KeyCode testAllInteractions = KeyCode.F1;    // Cycle through all 17 interactions
public KeyCode testEdgeCases = KeyCode.F2;          // Test stamina depletion, status stacking
public KeyCode testBalancing = KeyCode.F3;          // Test different stat combinations
public KeyCode testPerformance = KeyCode.F4;       // Spawn multiple combats for stress testing
public KeyCode resetToDefaults = KeyCode.F5;       // Reset all values to default
```

### **Minimal Scene Setup Guide**

#### Arena Setup Checklist
```
1. Create empty GameObject: "CombatArena"
2. Add Plane (10x10 units): Position (0, 0, 0)
3. Add invisible walls: 4 Box Colliders at edges
4. Create spawn points:
   - Player spawn: (-4, 0, -4)
   - Enemy spawn: (4, 0, 4)
5. Add Directional Light: Rotation (50, -30, 0)
6. Position Camera: (0, 8, -8), Rotation (45, 0, 0)
```

#### Character Setup Checklist
```
1. Create Capsule: "Player"
   - Add Character Controller
   - Add all combat component scripts
   - Set material: Blue
2. Create Capsule: "Enemy"
   - Add Character Controller
   - Add all combat component scripts + SimpleTestAI
   - Set material: Red
3. Create weapon child objects for each character
4. Assign WeaponData ScriptableObjects
```

#### UI Setup Checklist
```
1. Create Canvas: "CombatUI"
2. Add Health bars: Simple Image + Fill area
3. Add Stamina bars: Simple Image + Fill area
4. Add Status text: "Idle", "Charging", "Stunned", etc.
5. Add Debug panel: Text area for system values
6. Add Console panel: Input field + text display (toggle with `)
```

This provides everything needed to quickly set up and debug the minimal combat system without getting distracted by complex art or systems.
