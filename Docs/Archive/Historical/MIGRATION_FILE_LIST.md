# Complete File Migration List

## Debug.Log to CombatLogger Migration - File Inventory

### All 48 Files Successfully Migrated

#### Core Systems (9 files)
1. `/Assets/Scripts/Combat/Core/CombatInteractionManager.cs` → Category.Combat
2. `/Assets/Scripts/Combat/Core/CombatController.cs` → Category.Combat
3. `/Assets/Scripts/Combat/Core/CombatStateValidator.cs` → Category.Combat
4. `/Assets/Scripts/Combat/Core/SkillInteractionResolver.cs` → Category.Combat
5. `/Assets/Scripts/Combat/Core/CombatUpdateManager.cs` → Category.General
6. `/Assets/Scripts/Combat/Core/GameManager.cs` → Category.General
7. `/Assets/Scripts/Combat/Core/MovementController.cs` → Category.Movement
8. `/Assets/Scripts/Combat/Core/MovementArbitrator.cs` → Category.Movement
9. `/Assets/Scripts/Combat/Core/SkillExecutionTracker.cs` → Category.Combat

#### Additional Core Support (4 files)
10. `/Assets/Scripts/Combat/Core/CombatObjectPoolManager.cs` → Category.Combat
11. `/Assets/Scripts/Combat/Core/SkillExecution.cs` → Category.Combat
12. `/Assets/Scripts/Combat/Core/SpeedConflictResolver.cs` → Category.Combat

#### AI Systems (12 files)
13. `/Assets/Scripts/Combat/AI/AICoordinator.cs` → Category.AI
14. `/Assets/Scripts/Combat/AI/Coordination/AttackCoordinator.cs` → Category.AI
15. `/Assets/Scripts/Combat/AI/Coordination/FormationManager.cs` → Category.AI
16. `/Assets/Scripts/Combat/AI/Patterns/PatternCombatHandler.cs` → Category.AI
17. `/Assets/Scripts/Combat/AI/Patterns/PatternCondition.cs` → Category.AI
18. `/Assets/Scripts/Combat/AI/Patterns/PatternNode.cs` → Category.AI
19. `/Assets/Scripts/Combat/AI/Patterns/PatternMovementController.cs` → Category.AI
20. `/Assets/Scripts/Combat/AI/Patterns/PatternWeaponManager.cs` → Category.AI
21. `/Assets/Scripts/Combat/AI/Patterns/PatternDefinition.cs` → Category.AI
22. `/Assets/Scripts/Combat/AI/Patterns/TelegraphSystem.cs` → Category.AI
23. `/Assets/Scripts/Combat/AI/Patterns/PatternExecutor.cs` → Category.AI
24. `/Assets/Scripts/Combat/AI/Patterns/Editor/PatternGenerator.cs` → Category.AI

#### Skill System (12 files)
25. `/Assets/Scripts/Combat/Skills/Base/SkillSystem.cs` → Category.Skills
26. `/Assets/Scripts/Combat/Skills/States/ApproachingState.cs` → Category.Skills
27. `/Assets/Scripts/Combat/Skills/States/ChargedState.cs` → Category.Skills
28. `/Assets/Scripts/Combat/Skills/States/AimingState.cs` → Category.Skills
29. `/Assets/Scripts/Combat/Skills/States/RecoveryState.cs` → Category.Skills
30. `/Assets/Scripts/Combat/Skills/States/ActiveState.cs` → Category.Skills
31. `/Assets/Scripts/Combat/Skills/States/StartupState.cs` → Category.Skills
32. `/Assets/Scripts/Combat/Skills/States/ChargingState.cs` → Category.Skills
33. `/Assets/Scripts/Combat/Skills/States/WaitingState.cs` → Category.Skills
34. `/Assets/Scripts/Combat/Skills/States/SkillStateMachine.cs` → Category.Skills
35. `/Assets/Scripts/Combat/Skills/States/SkillStateBase.cs` → Category.Skills
36. `/Assets/Scripts/Combat/Skills/States/UnchargedState.cs` → Category.Skills

#### Weapons (2 files)
37. `/Assets/Scripts/Combat/Weapons/WeaponController.cs` → Category.Combat
38. `/Assets/Scripts/Combat/Weapons/WeaponTrailController.cs` → Category.Combat

#### Systems (5 files)
39. `/Assets/Scripts/Combat/Systems/HealthSystem.cs` → Category.Combat
40. `/Assets/Scripts/Combat/Systems/StaminaSystem.cs` → Category.Combat
41. `/Assets/Scripts/Combat/Systems/KnockdownMeterTracker.cs` → Category.Combat
42. `/Assets/Scripts/Combat/Systems/AccuracySystem.cs` → Category.Combat
43. `/Assets/Scripts/Combat/Systems/CameraController.cs` → Category.General

#### Status Effects (1 file)
44. `/Assets/Scripts/Combat/StatusEffects/StatusEffectManager.cs` → Category.StatusEffects

#### UI (3 files)
45. `/Assets/Scripts/Combat/UI/SkillIconDisplay.cs` → Category.UI
46. `/Assets/Scripts/Combat/UI/OutlineEffect.cs` → Category.UI
47. `/Assets/Scripts/Combat/UI/CharacterInfoDisplay.cs` → Category.UI

#### Equipment (1 file)
48. `/Assets/Scripts/Combat/Equipment/EquipmentManager.cs` → Category.Combat

## Category Summary

| Category | File Count | Primary Use |
|----------|-----------|-------------|
| Combat | 18 | Combat interactions, damage, execution |
| AI | 12 | AI patterns, coordination, tactics |
| Skills | 12 | Skill states and execution flow |
| Movement | 2 | Movement control and arbitration |
| StatusEffects | 1 | Status effect management |
| UI | 3 | User interface displays |
| General | 3 | Game management, utilities |

## Quick Reference: Using CombatLogger

### Basic Usage
```csharp
// Info log
CombatLogger.Log(CombatLogger.Category.Combat, "Message");

// Warning
CombatLogger.LogWarning(CombatLogger.Category.AI, "Warning message");

// Error
CombatLogger.LogError(CombatLogger.Category.Skills, "Error message");
```

### Filtering Logs
Access the CombatLogger Config Window:
- Unity Menu: `Window > Combat > CombatLogger Config`
- Toggle categories on/off
- View color-coded output

### Categories Available
- `CombatLogger.Category.Combat`
- `CombatLogger.Category.AI`
- `CombatLogger.Category.Skills`
- `CombatLogger.Category.Movement`
- `CombatLogger.Category.StatusEffects`
- `CombatLogger.Category.UI`
- `CombatLogger.Category.General`

## Files NOT Migrated (Intentional)

The following directories were intentionally not migrated:
- `/Assets/Scripts/Editor/` (56 Debug.Log calls) - Editor-only tools don't need CombatLogger

## Backup Files

All migrated files have `.bak` backups in the same directory.
After verification, remove backups with:
```bash
find Assets/Scripts/Combat -name "*.bak" -delete
```
