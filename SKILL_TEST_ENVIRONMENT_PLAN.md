# Skill Test Environment Implementation Plan

## Overview
A configurable AI system that allows developers to select a specific skill for an enemy to repeat continuously, enabling thorough testing of all skill interactions in the combat system.

## Goals
1. Enable runtime selection of which skill the enemy should repeat
2. Support all skill types: Attack, Defense, Counter, Smash, Windmill, RangedAttack
3. Provide easy-to-use UI/inspector controls for skill selection
4. Maintain compatibility with existing combat and AI systems
5. Support both single-skill repetition and charge-execute loops

## Architecture

### Core Component: TestRepeaterAI
A new AI class that extends the existing PatternedAI system but allows dynamic skill selection.

**Key Features:**
- Inherits from `PatternedAI` abstract base class
- Single-phase pattern that repeats one skill indefinitely
- Runtime skill selection via inspector dropdown or UI menu
- Configurable delays between skill executions
- Optional randomization of timing to simulate human behavior

**Base Class Relationship:**
```
MonoBehaviour
    └── PatternedAI (abstract)
            ├── KnightAI
            ├── SimpleTestAI
            └── TestRepeaterAI (NEW)
```

## Implementation Components

### 1. TestRepeaterAI.cs
**Location**: `Assets/Scripts/Combat/AI/TestRepeaterAI.cs`

**Properties:**
- `SkillType selectedSkill` - Skill to repeat (Inspector dropdown)
- `float repeatDelay` - Delay between repetitions (default: 1.0s)
- `bool addRandomDelay` - Add 0-0.5s random delay to feel more natural
- `bool maintainDefensiveState` - Keep Defense/Counter in Waiting state

**Pattern Structure:**
```csharp
protected override void DefinePattern()
{
    // Single-phase pattern that loops infinitely
    AIPattern testPattern = new AIPattern("Test Loop");
    testPattern.AddPhase(new AIPhase
    {
        skillToExecute = selectedSkill,
        waitTime = repeatDelay,
        description = $"Repeat {selectedSkill}"
    });

    patterns.Add(testPattern);
    currentPattern = testPattern;
}
```

**Special Handling:**
- **Offensive Skills** (Attack, Smash, Windmill, RangedAttack): Execute → Recovery → Delay → Repeat
- **Defensive Skills** (Defense, Counter): Enter Waiting state → Respond to attacks → Optional re-enter or delay
- **Defensive State Maintenance**: If `maintainDefensiveState` is true, immediately re-enter Defense/Counter after it completes

### 2. Runtime Skill Selection UI
**Location**: `Assets/Scripts/Combat/UI/TestSkillSelector.cs` (NEW)

**UI Elements:**
- Dropdown menu with all skill types
- "Apply" button to change enemy skill
- "Reset" button to return enemy to default AI
- Delay slider (0.5s - 5.0s)
- Toggle for random delay
- Toggle for defensive state maintenance

**UI Layout:**
```
┌─────────────────────────────────────┐
│  Skill Test Environment             │
├─────────────────────────────────────┤
│  Enemy Skill: [Dropdown: Attack  ▼] │
│  Repeat Delay: [Slider] 1.0s        │
│  □ Add Random Delay (±0.5s)         │
│  □ Maintain Defensive State         │
│  [Apply]  [Reset to Default AI]     │
└─────────────────────────────────────┘
```

**Hotkey Support:**
- `F1` - Attack
- `F2` - Defense
- `F3` - Counter
- `F4` - Smash
- `F5` - Windmill
- `F6` - RangedAttack
- `F12` - Reset to default AI

### 3. Integration with CombatDebugVisualizer
**Updates to**: `Assets/Scripts/Combat/Debug/CombatDebugVisualizer.cs`

**New Display Section:**
```csharp
private void AddTestAIInfo()
{
    var testAI = GetComponent<TestRepeaterAI>();
    if (testAI != null)
    {
        debugText.AppendLine("=== TEST MODE ===");
        debugText.AppendLine($"Repeating: {testAI.selectedSkill}");
        debugText.AppendLine($"Delay: {testAI.repeatDelay:F1}s");
        debugText.AppendLine($"Next Action: {testAI.TimeUntilNextAction:F1}s");
    }
}
```

### 4. Enemy Setup Helper
**Location**: `Assets/Scripts/Editor/TestEnvironmentSetup.cs` (NEW)

**Editor Menu Items:**
```
Tools/Combat/Test Environment/
    ├── Setup Test Enemy
    ├── Add Test UI to Scene
    └── Create Test Scene
```

**"Setup Test Enemy" Function:**
1. Finds enemy in scene (or prompts to select)
2. Disables existing AI components (SimpleTestAI, KnightAI, etc.)
3. Adds TestRepeaterAI component
4. Sets default skill to Attack
5. Configures CombatDebugVisualizer for test mode

## Test Scenarios

### Offensive Skill Testing
**Attack vs All Defenses:**
1. Set enemy to repeat Attack
2. Player tests Defense → Should block with stun/knockdown
3. Player tests Counter → Should reflect damage
4. Player tests own Attack → Speed resolution

**Smash Testing:**
1. Set enemy to repeat Smash
2. Test vs Defense → Should break and knockdown
3. Test vs Counter → Should break and knockdown
4. Test vs Attack → Smash should win via priority

**Windmill Testing:**
1. Set enemy to repeat Windmill
2. Test vs Defense → Should hit (Defense ineffective)
3. Test vs Counter → Should break counter and knockdown
4. Test vs Attack/Smash → Windmill should hit

**RangedAttack Testing:**
1. Set enemy to repeat RangedAttack
2. Test vs Defense → Should block 100% on hit, Defense stays active on miss
3. Test vs Counter → Counter ineffective, takes full damage
4. Test accuracy at different distances
5. Test vs moving target (strafe while enemy aims)

### Defensive Skill Testing
**Defense Pattern:**
1. Set enemy to repeat Defense (with maintainDefensiveState ON)
2. Enemy enters Defense Waiting state
3. Player tests various attacks against waiting Defense
4. Enemy automatically re-enters Defense after each interaction

**Counter Pattern:**
1. Set enemy to repeat Counter (with maintainDefensiveState ON)
2. Enemy enters Counter Waiting state
3. Player tests attacks → Verify damage reflection
4. Player tests RangedAttack → Verify Counter ineffective
5. Player tests Windmill → Verify Counter breaks

### Interaction Matrix Testing
**Systematic Testing Grid:**
```
Player Skill → | Attack | Defense | Counter | Smash | Windmill | Ranged
─────────────────────────────────────────────────────────────────────
Enemy Attack   |   ⚔️   |   🛡️   |   🔄   |  💥  |    🌀   |   🏹
Enemy Defense  |  🛡️   |    -    |    -    |  💥  |    🌀   |   🏹
Enemy Counter  |  🔄   |    -    |    -    |  💥  |    🌀   |   🏹
Enemy Smash    |  💥  |   💥   |   💥   |  ⚔️   |    🌀   |   🏹
Enemy Windmill |  🌀   |   🌀   |   🌀   |  🌀   |    🌀   |   🏹
Enemy Ranged   |  🏹   |   🛡️   |   ❌   |  🏹   |    🌀   |   🏹

Legend:
⚔️ = Speed resolution
🛡️ = Defender blocks
🔄 = Counter reflects
💥 = Smash breaks defense
🌀 = Windmill hits through
🏹 = Ranged attack (accuracy-based)
❌ = Counter ineffective
```

## Usage Workflow

### Scene Setup
1. Open combat test scene
2. Ensure player and enemy are present
3. Add TestSkillSelector UI to scene (or use hotkeys)
4. Assign TestRepeaterAI to enemy

### Testing Process
1. **Select Skill**: Use UI dropdown or hotkey (F1-F6)
2. **Configure Timing**: Adjust delay slider as needed
3. **Apply Changes**: Click Apply or hotkey auto-applies
4. **Test Interactions**: Execute player skills against repeating enemy skill
5. **Observe Results**: Check CombatDebugVisualizer for detailed state info
6. **Iterate**: Change enemy skill and repeat

### Example Test Session
```
Goal: Test Defense blocking all offensive skills

1. Press F2 → Enemy repeats Defense
2. Enable "Maintain Defensive State"
3. Set delay to 0.5s (fast re-entry)
4. Test player Attack → Verify block + stun
5. Test player Smash → Verify Defense breaks
6. Test player Windmill → Verify hits through Defense
7. Test player RangedAttack → Verify 100% block on hit, stay active on miss
8. Review debug logs for any issues
```

## Implementation Order

### Phase 1: Core AI Component
1. Create `TestRepeaterAI.cs` extending PatternedAI
2. Implement single-skill pattern loop
3. Add inspector properties for skill selection and timing
4. Test basic repetition with Attack skill

### Phase 2: UI Controls
1. Create `TestSkillSelector.cs` UI component
2. Implement dropdown and controls
3. Add hotkey support (F1-F6, F12)
4. Wire up UI to TestRepeaterAI

### Phase 3: Debug Integration
1. Update CombatDebugVisualizer to show test mode info
2. Add visual indicators for test AI state
3. Enhance logging for test scenario analysis

### Phase 4: Editor Tools
1. Create `TestEnvironmentSetup.cs` editor helper
2. Add menu items for quick setup
3. Create helper functions for enemy configuration

### Phase 5: Testing & Polish
1. Test all skill combinations using interaction matrix
2. Verify defensive state maintenance works correctly
3. Add documentation comments
4. Create example test scene

## Edge Cases & Considerations

### Defensive Skill Maintenance
- **Problem**: Defense/Counter complete after one interaction
- **Solution**: `maintainDefensiveState` flag immediately re-charges and re-executes defensive skill after completion
- **Implementation**: Override `OnPatternPhaseComplete()` to check if skill was defensive and flag is enabled

### Speed Resolution Loops
- **Problem**: Two Attack skills at same speed causes tie → knockdown both → repeat forever
- **Solution**: Add minimum delay after knockdown recovery before enemy re-enters Attack
- **Default**: 1.5s post-knockdown delay

### RangedAttack Aiming State
- **Problem**: RangedAttack requires aiming time, but test might want instant firing
- **Solution**: Add `skipAiming` option that maxes out accuracy immediately (dev mode only)
- **Usage**: Enable for rapid-fire testing, disable for realistic accuracy testing

### Stamina Depletion
- **Problem**: Repeated skills drain stamina → enemy can't execute
- **Solution**: Add `infiniteStamina` toggle for test AI (optional cheat mode)
- **Alternative**: Use normal stamina to test realistic combat scenarios

### Target Lost Handling
- **Problem**: Player dies or moves out of range → AI stuck
- **Solution**: TestRepeaterAI should detect lost target and re-acquire or idle until target returns
- **Fallback**: Reset enemy AI if player respawns

## Configuration Presets

### Quick Test Profiles
Saved configurations for common test scenarios:

**Profile 1: Aggressive Attacker**
- Skill: Attack
- Delay: 0.5s
- Random Delay: OFF
- Purpose: Test player defensive skills under pressure

**Profile 2: Defensive Wall**
- Skill: Defense
- Delay: 0.3s
- Random Delay: OFF
- Maintain Defensive: ON
- Purpose: Test offensive skills vs consistent Defense

**Profile 3: Counter Punisher**
- Skill: Counter
- Delay: 0.5s
- Random Delay: OFF
- Maintain Defensive: ON
- Purpose: Test reflected damage and counter breaks

**Profile 4: Smash Breaker**
- Skill: Smash
- Delay: 2.0s
- Random Delay: ON
- Purpose: Test defense breaks and knockdowns

**Profile 5: Windmill Chaos**
- Skill: Windmill
- Delay: 3.0s
- Random Delay: ON
- Purpose: Test AoE interactions and timing windows

**Profile 6: Ranged Sniper**
- Skill: RangedAttack
- Delay: 1.5s
- Random Delay: ON
- Purpose: Test accuracy system and ranged defenses

## Success Criteria

The test environment is successful when:
1. ✅ All 6 skills can be selected and repeated by enemy
2. ✅ UI/hotkeys allow instant skill switching during runtime
3. ✅ Defensive skills properly maintain Waiting state when flag enabled
4. ✅ Debug visualizer clearly shows test mode status
5. ✅ All 36 interaction matrix combinations can be tested systematically
6. ✅ Edge cases (stamina, knockdown, target loss) handled gracefully
7. ✅ Setup takes <30 seconds to configure from blank scene
8. ✅ Developer can test entire interaction matrix in <10 minutes

## Future Enhancements

### Advanced Features (Optional)
1. **Pattern Recording**: Record player actions and replay as enemy AI
2. **Multi-Enemy Testing**: Control multiple enemies with different repeated skills
3. **Sequence Testing**: Enemy cycles through skill sequence (A→D→C→repeat)
4. **Conditional Patterns**: "If player charges Smash, enemy uses Defense"
5. **Performance Metrics**: Track success rates, damage dealt/received, interaction outcomes
6. **Test Report Generation**: Export CSV with all interaction results for analysis

### Integration Ideas
1. **Unit Test Framework**: Automated testing of all interactions with assertions
2. **CI/CD Integration**: Run interaction matrix tests on each build
3. **Bug Reproduction Mode**: Save exact sequence that caused a bug for debugging

## File Structure Summary

```
Assets/
├── Scripts/
│   ├── Combat/
│   │   ├── AI/
│   │   │   ├── PatternedAI.cs (existing)
│   │   │   ├── SimpleTestAI.cs (existing)
│   │   │   ├── KnightAI.cs (existing)
│   │   │   └── TestRepeaterAI.cs (NEW)
│   │   ├── UI/
│   │   │   └── TestSkillSelector.cs (NEW)
│   │   └── Debug/
│   │       └── CombatDebugVisualizer.cs (update)
│   └── Editor/
│       └── TestEnvironmentSetup.cs (NEW)
└── Scenes/
    └── SkillTestEnvironment.unity (NEW)
```

## Conclusion

This test environment will dramatically improve development velocity by:
- Eliminating manual AI scripting for each test case
- Enabling systematic testing of all 36 skill interactions
- Providing instant feedback through debug visualization
- Supporting both rapid iteration and thorough testing

The implementation leverages the existing PatternedAI system, maintaining consistency with the codebase architecture while adding powerful testing capabilities.
