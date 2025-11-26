# FairyGate Project File Structure

Post-refactor organization (as of 2025-11-14)

## Assets/Scripts/

### Combat/AI/
```
AICoordinator.cs                    # Main AI coordinator (315 lines, -41% post-refactor)

Coordination/
├── AttackCoordinator.cs            # Attack permission system (268 lines)
├── FormationManager.cs             # Formation slot management (256 lines)
├── IAIAgent.cs                     # AI agent interface
└── IAICombatCoordinator.cs         # Coordinator interface

Patterns/
├── PatternCombatHandler.cs         # Combat execution for patterns
├── PatternCondition.cs             # Condition evaluation system (16 types)
├── PatternDefinition.cs            # ScriptableObject pattern definition
├── PatternExecutor.cs              # Runtime pattern execution
├── PatternMovementController.cs    # Pattern-based movement
├── PatternNode.cs                  # Individual pattern nodes
├── PatternWeaponManager.cs         # Weapon management for patterns
├── TelegraphSystem.cs              # Attack telegraphing (TODO: animations)
└── Editor/
    └── PatternGenerator.cs         # Pattern asset generation tool
```

### Combat/Core/
```
CombatController.cs                 # Main character controller
CombatInteractionManager.cs         # Skill interaction resolution system
CombatObjectPoolManager.cs          # Object pooling for performance
CombatUpdateManager.cs              # Centralized update system
GameManager.cs                      # Game state management
MovementController.cs               # Character movement system
SkillExecution.cs                   # Skill execution data
SkillInteractionResolver.cs         # Attack/Defense/Counter interaction logic
SpeedConflictResolver.cs            # Speed-based conflict resolution
```

### Combat/Equipment/
```
EquipmentData.cs                    # Equipment ScriptableObject
EquipmentEnums.cs                   # Equipment type enums
EquipmentManager.cs                 # Equipment system manager
EquipmentSet.cs                     # Pre-configured equipment sets
```

### Combat/Skills/
```
Base/
└── SkillSystem.cs                  # Core skill execution system

States/                             # State Pattern Implementation
├── ISkillState.cs                  # State interface
├── SkillStateBase.cs               # Base state class
├── SkillStateMachine.cs            # State machine controller
├── UnchargedState.cs               # Initial/idle state
├── ChargingState.cs                # Charging skills (Attack/Smash)
├── ChargedState.cs                 # Fully charged, ready to execute
├── AimingState.cs                  # Ranged attack aiming
├── StartupState.cs                 # Skill startup frames
├── ActiveState.cs                  # Skill active execution
├── WaitingState.cs                 # Defensive skills waiting for attacks
└── RecoveryState.cs                # Post-execution recovery
```

### Combat/Stats/
```
DamageCalculator.cs                 # Damage calculation system
SpeedResolver.cs                    # Speed stat resolution
```

### Combat/StatusEffects/
```
StatusEffectManager.cs              # Stun/Knockback/Status effect system
```

### Combat/Systems/
```
AccuracySystem.cs                   # Hit/miss calculation
CameraController.cs                 # Combat camera control
HealthSystem.cs                     # HP management
KnockdownMeterTracker.cs            # Three-tier CC system (stun/knockback/knockdown)
StaminaSystem.cs                    # Stamina drain/regen
```

### Combat/UI/
```
CharacterInfoDisplay.cs             # Main UI coordinator
HealthBarUI.cs                      # Health bar display (gradient red→green)
StaminaBarUI.cs                     # Stamina bar display (tri-color gradient)
KnockdownMeterBarUI.cs              # Knockdown meter (orange→red gradient)
SkillIconDisplay.cs                 # Skill icons + charge meter
StatusEffectDisplay.cs              # Status effect indicators
TargetOutlineManager.cs             # Target highlighting
OutlineEffect.cs                    # Outline shader effect component
```

### Combat/Utilities/
```
CombatLogger.cs                     # Centralized logging (12 categories, 4 levels)
CombatUtilities.cs                  # Helper functions
ComponentExtensions.cs              # C# extension methods

Constants/
├── CombatConstants.cs              # Game balance constants
└── CombatEnums.cs                  # All combat enums

Interfaces/
├── ISkillExecutor.cs               # Skill execution interface
└── IStatusEffectTarget.cs          # Status effect target interface

EnemyArchetypeConfig.cs             # Enemy configuration
PlayerFinder.cs                     # Player reference utility
```

### Combat/Weapons/
```
WeaponController.cs                 # Weapon management system
WeaponTrailController.cs            # Visual weapon trails
```

### Data/
```
CharacterData/
├── CharacterStats.cs               # Character stats ScriptableObject
└── EnemyArchetype.cs               # Enemy archetype definition

WeaponData/
└── WeaponData.cs                   # Weapon stats ScriptableObject
```

### Editor/
```
AI/                                 # Custom inspectors for AI patterns
├── PatternConditionDrawer.cs       # Condition property drawer
├── PatternDefinitionEditor.cs      # Pattern definition inspector
├── PatternNodeDrawer.cs            # Node property drawer
└── PatternTransitionDrawer.cs      # Transition property drawer

CharacterSpawner.cs                 # Character spawning tool
CombatAssetFactory.cs               # Asset creation utilities
CombatLoggerConfigWindow.cs         # Logger configuration window
CombatUpdateManagerExecutionOrder.cs # Script execution order
CompleteCombatSceneSetup.cs         # One-click scene setup
EditorUtilities.cs                  # Editor helper functions
SceneEnvironmentBuilder.cs          # Environment generation
```

## Assets/Data/

### AI/
```
Patterns/                           # AI Pattern Definitions
├── ArcherPattern.asset             # Ranged combat pattern
├── AssassinPattern.asset           # High-speed offensive pattern
├── BerserkerPattern.asset          # Aggressive melee pattern
├── GuardianPattern.asset           # Defensive tank pattern
└── SoldierPattern.asset            # Balanced combat pattern

BerserkerAIConfig.asset             # Legacy config (deprecated)
GuardianAIConfig.asset              # Legacy config (deprecated)
```

### Characters/
```
Player_Stats.asset                  # Player character stats
TestPlayer_Stats.asset              # Test player stats
Enemy_Stats.asset                   # Enemy base stats
TestEnemy_Stats.asset               # Test enemy stats
```

### Equipment/
```
Accessories/                        # Accessory items
├── GuardianRing.asset              # +Protection accessory
├── MeditationAmulet.asset          # +Mana/Will accessory
├── PowerGauntlets.asset            # +Strength accessory
└── SwiftBoots.asset                # +Dexterity accessory

Armor/                              # Armor pieces
├── ClothRobes.asset                # Light armor
├── LeatherTunic.asset              # Medium armor
├── ChainMail.asset                 # Heavy armor
└── HeavyPlatemail.asset            # Tank armor

Sets/                               # Pre-configured sets
├── Berserker_GlassCannonSet.asset  # High damage, low defense
├── Fortress_TankSet.asset          # Max defense, slow
├── Wanderer_BalancedSet.asset      # Balanced stats
└── Windrunner_SpeedSet.asset       # Speed focus
```

### Weapons/
```
# Player Test Weapons
TestSword.asset                     # Balanced melee (1.5m range)
TestDagger.asset                    # Fast melee (1.5m range)
TestMace.asset                      # Heavy melee (1.5m range)
TestSpear.asset                     # Balanced melee (1.5m range)
TestBow.asset                       # Ranged weapon

# Production Weapons
Sword.asset
Spear.asset
Bow.asset
```

## Assets/Shaders/
```
OutlineShader.shader                # Target outline shader
```

## Docs/

### Setup & Quick Start
```
Quick-Start-Guide.md                # Game setup & controls
AIController-Quick-Start.md         # AI system quick start
```

### Architecture Documentation
```
AI-Pattern-System-Architecture.md   # Pattern-based AI design
AI-Movement-Skill-Architecture-Analysis.md
Classic-Mabinogi-Combat-Deep-Dive.md
Mabinogi-vs-FairyGate-Comparison.md
Implementation-Summary-Classic-Mabinogi-Combat.md
```

### Design Documents
```
N+1-Combo-System-Design.md          # Future combo system design
Weapon-Skill-Modifier-System-Design.md
CombatSystemExpansion.md            # Future expansion ideas
CombatSystemExpansionIdeas.md
Classic-Mabinogi-Combat-Enhancement-Plan.md
Target_Indicator_System.md
UI_System_Update.md
```

### Refactoring Documentation
```
Refactoring-Progress.md             # Complete refactoring history
RefactoringPlan.md                  # Original refactoring plan
Refactored-AI-System-Guide.md       # Post-refactor AI guide
Pattern-Only-AI-Migration.md        # Migration from legacy AI
ArchitecturalImprovementsPlan.md
```

### Project Management
```
TODO-Tracking.md                    # Technical debt tracking
PlaytestChecklist_Phase1.md         # Testing checklist
```

## Summary Statistics

**Total C# Scripts:** 73 files
- Combat/AI: 15 files
- Combat/Core: 9 files
- Combat/Skills: 11 files (10 state classes)
- Combat/UI: 7 files
- Combat/Systems: 5 files
- Combat/Utilities: 8 files
- Combat/Other: 6 files
- Data: 3 files
- Editor: 9 files

**Total Data Assets:** 38 files
- AI Patterns: 5 files
- Characters: 4 files
- Equipment: 14 files
- Weapons: 9 files

**Documentation:** 21 markdown files

**Key Refactoring Wins:**
- AICoordinator: 535 → 315 lines (-41%)
- State Pattern: 10 focused state classes
- Pattern System: 15 AI components
- Centralized Logging: 340 lines, 12 categories
- UI Components: 7 single-responsibility classes
