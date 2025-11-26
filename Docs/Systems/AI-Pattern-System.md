# Pattern-Based Multi-Enemy AI System Design

## Design Philosophy

Create **predictable but challenging** AI that rewards player skill and pattern recognition, similar to Dark Souls boss fights. Players must learn enemy attack patterns to achieve mastery, with multi-enemy encounters requiring simultaneous pattern tracking and tactical positioning.

## Core Architecture

### Pattern-Based AI Foundation
```csharp
public abstract class PatternedAI : MonoBehaviour
{
    [SerializeField] protected float patternCooldown = 1f;
    [SerializeField] protected bool enablePatternLogs = true;

    protected abstract IEnumerator ExecutePattern();
    protected abstract string GetPatternName();

    private IEnumerator PatternLoop()
    {
        while (isAlive && isInCombat)
        {
            yield return StartCoroutine(ExecutePattern());
            yield return new WaitForSeconds(patternCooldown);
        }
    }
}
```

### Formation Coordination System
```csharp
public class EnemyFormation : MonoBehaviour
{
    [SerializeField] private List<PatternedAI> enemies;
    [SerializeField] private FormationType formation;
    [SerializeField] private float staggerDelay = 0.5f;

    public enum FormationType
    {
        Synchronized,    // All start together
        Staggered,      // Sequential starts
        Reactive        // Event-driven coordination
    }
}
```

### Cross-Enemy Communication
```csharp
public static class FormationEvents
{
    public static event System.Action<string, PatternedAI> OnPatternSignal;
    public static event System.Action<PatternedAI> OnEnemyDefeated;
    public static event System.Action<Vector3> OnPlayerPositionUpdate;
}
```

## Enemy Pattern Designs

### Knight Pattern (Defensive Tank)
**8-second cycle, predictable windows**
```
1. Charge Defense (1s) → Telegraph: "Raising shield"
2. Wait Defensively (3s) → Vulnerable to positioning
3. Cancel Defense (0.5s) → Telegraph: "Lowering shield"
4. Charge Smash (1.5s) → Telegraph: "Winding up attack"
5. Execute Smash (0.5s) → Danger window
6. Recovery (1.5s) → Vulnerable window for counterattack
```

### Archer Pattern (Ranged Pressure)
**6-second cycle, range-dependent**
```
1. Charge Attack (1s) → Telegraph: "Drawing bow"
2. Execute if in range (0.5s) → Danger window
3. Reposition away (2s) → Movement window
4. Charge Attack (1s) → Second shot
5. Execute if in range (0.5s) → Danger window
6. Cooldown (1s) → Approach opportunity
```

### Berserker Pattern (Aggressive DPS)
**5-second cycle, relentless pressure**
```
1. Charge Attack (0.5s) → Fast telegraph
2. Execute (0.5s) → Quick danger
3. Charge Smash (1s) → Medium telegraph
4. Execute (0.5s) → Heavy danger
5. Charge Windmill (1s) → Slow telegraph
6. Execute (0.5s) → Area danger
7. Exhaustion (0.5s) → Brief vulnerable window
```

## Movement Pattern Integration

### Movement Pattern Interface
```csharp
public interface IMovementPattern
{
    Vector3 GetDesiredMovement(AIMovementContext context);
    bool ShouldUpdateMovement(float deltaTime);
    void OnPatternStart();
    void OnPatternStop();
}
```

### Movement Pattern Examples
- **ApproachPattern**: Direct movement toward target
- **CirclingPattern**: Orbit around target at optimal range
- **FlankingPattern**: Approach from sides/behind
- **RetreatPattern**: Move away from target
- **KitingPattern**: Maintain max range while attacking

### Combat-Movement Coordination
```csharp
public class AITacticalManager : MonoBehaviour
{
    public IMovementPattern movementPattern;
    public IBehaviorScript combatPattern;

    private void UpdateAI()
    {
        var context = BuildAIContext();

        // Movement and combat patterns inform each other
        var desiredMovement = movementPattern.GetDesiredMovement(context);
        var combatDecision = combatPattern.GetNextAction(context, desiredMovement);

        // Execute coordinated behavior
        ExecuteMovement(desiredMovement);
        ExecuteCombatAction(combatDecision);
    }
}
```

## Formation Combinations

### Formation 1: Knight + Archer (2 enemies)
**Synchronized Timing:**
```
Knight:  [Defense ████████][Recovery ██]
Archer:  [Attack ██][Reposition ████████]
Windows: Player can approach during knight defense, but must dodge archer
```

**Strategy**: Knight blocks direct approach, archer forces movement. Player must time approach during knight's defensive phase while avoiding archer shots.

### Formation 2: Berserker + Berserker (2 enemies)
**Staggered Timing:**
```
Berserker A: [Attack][Smash][Windmill][Rest]
Berserker B:      [Attack][Smash][Windmill][Rest]
Result: Constant pressure, no safe moments
```

**Strategy**: Overwhelming aggression with no downtime. Player must use superior positioning and counter-attacks to create openings.

### Formation 3: Knight + Archer + Berserker (3 enemies)
**Coordinated Roles:**
- **Knight**: Front line defense, blocks player escape routes
- **Archer**: Back line pressure, forces player movement
- **Berserker**: Flanking aggression, punishes stationary play

**Strategy**: Multi-layered encounter requiring advanced positioning, target prioritization, and crowd control mastery.

## Multi-Enemy Coordination Patterns

### Cooperative Patterns
```csharp
// Knight + Archer combo
Knight: Charge Defense → Wait (3s) → Cancel → Charge Smash → Execute
Archer: Wait (1s) → Charge Attack → Wait for Knight's smash → Execute together
```
*Player must handle simultaneous threats*

### Alternating Pressure
```csharp
// Berserker + Berserker combo
Berserker A: Attack → Rest → Attack → Rest
Berserker B: Rest → Attack → Rest → Attack
```
*No safe moments - constant pressure*

### Setup + Execution
```csharp
// Mage + Warrior combo
Mage: Charge Counter → Wait → Cancel when warrior attacks
Warrior: Wait for player to attack mage → Charge Smash → Execute
```
*Mage baits player attack, warrior punishes*

## Implementation Phases

### Phase 1: Single Pattern Foundation
1. Create `PatternedAI` base class
2. Implement `KnightAI` with fixed 8-second pattern
3. Add pattern visualization and debug logging
4. Test pattern learning gameplay with single enemy

### Phase 2: Formation System
1. Create `EnemyFormation` manager component
2. Implement synchronized and staggered timing
3. Add cross-enemy communication events
4. Test Knight + Archer formation

### Phase 3: Movement Integration
1. Enhance MovementController with analog input support
2. Create pluggable movement pattern system
3. Integrate movement patterns with combat patterns
4. Test coordinated movement-combat behaviors

### Phase 4: Multi-Enemy Patterns
1. Implement Berserker and additional enemy types
2. Create complex 3+ enemy formations
3. Add formation-specific coordination logic
4. Balance vulnerable windows for multi-enemy encounters

### Phase 5: Advanced Features
1. Formation adaptation when enemies defeated
2. Player positioning awareness for formations
3. Environmental formation positioning
4. Difficulty scaling through formation complexity

## Player Learning Progression

### Single Enemy Mastery (Phase 1)
- **Recognition**: Learn individual 4-8 second patterns
- **Timing**: Master timing of vulnerable windows
- **Counter**: Perfect counter-attacks and positioning
- **Optimization**: Bait enemies into favorable patterns

### Dual Enemy Tactics (Phase 2)
- **Multi-tracking**: Track two overlapping patterns simultaneously
- **Positioning**: Find safe positioning against multiple threats
- **Priority**: Learn target priority decisions
- **Coordination**: Use enemy patterns against each other

### Multi-Enemy Strategy (Phase 3+)
- **Pattern Management**: Handle 3-10 enemy patterns at once
- **Crowd Control**: Advanced positioning and movement
- **Formation Breaking**: Disrupt enemy coordination
- **Resource Management**: Stamina and positioning optimization

## Strategic Depth Examples

### Target Priority System
Players must learn elimination order:
- **High threat, low defense**: Archers, mages (eliminate first)
- **Disruptors**: Enemies that buff/coordinate others
- **Tanks**: Save for last unless blocking access to priority targets

### Positioning Mastery
- **Line Formation**: Force enemies into single-file to reduce simultaneous threats
- **Using Enemy Attacks**: Dodge so enemies hit each other
- **Control Spacing**: Maintain optimal range for multiple threats
- **Safe Zones**: Positions where multiple patterns can't reach

### Pattern Exploitation
- **Synchronized Vulnerabilities**: All enemies vulnerable simultaneously
- **Chain Reactions**: Defeating one enemy disrupts others' patterns
- **Formation Collapse**: How enemy coordination changes as numbers decrease

## Scalable Difficulty Design

### 2 Enemies: Foundation Learning
- Learn individual patterns + basic positioning
- Simple synchronized or staggered timing
- Clear vulnerable windows and telegraph signals

### 3-4 Enemies: Tactical Decisions
- Pattern prioritization + crowd control
- Introduction of coordination between enemy types
- Resource management becomes important

### 5-7 Enemies: Advanced Strategy
- Advanced positioning + target selection
- Complex formation coordination
- Multiple simultaneous pattern tracking

### 8-10 Enemies: Master-Level Encounters
- Perfect execution required
- Complex multi-phase patterns
- Environmental awareness crucial

## Benefits of This System

### For Players
- **Learnable**: Patterns reward study and practice
- **Mastery Curve**: Clear skill progression path
- **Strategic Depth**: Multiple valid approaches per encounter
- **Rewarding**: "Aha!" moments when cracking patterns

### For Developers
- **Scalable**: 2-10 enemies through formation design
- **Maintainable**: Each pattern is self-contained and predictable
- **Modular**: Mix and match patterns for variety
- **Balanceable**: Precise control over difficulty and pacing

### For Game Design
- **Memorable**: Unique enemy personalities through distinct patterns
- **Replayable**: Mastery encourages repeated encounters
- **Emergent**: Complex scenarios from simple pattern combinations
- **Fair**: Skill-based difficulty rather than stat inflation

## Technical Notes

### Performance Considerations
- Coroutine-based patterns are efficient for turn-based timing
- Event-driven coordination minimizes update overhead
- Pattern caching and pooling for large encounters

### Debug and Development Tools
- Pattern visualization overlays
- Timing debug displays
- Formation coordination logs
- Player mastery tracking metrics

### Integration with Existing Systems
- Builds on current SkillSystem and MovementController
- Extends current AI without breaking existing functionality
- Compatible with all existing combat mechanics and skill interactions

---

**Status**: Design Document - Ready for Implementation
**Next Step**: Implement Phase 1 - Single Pattern Foundation
**Dependencies**: Current combat system working as designed