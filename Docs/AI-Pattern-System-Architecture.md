# AI Pattern System Architecture
**Phase 5: Classic Mabinogi Knowledge-Based Combat**
**Status:** Implementation Ready
**Priority:** High (Core Combat Philosophy)

## Overview

The AI Pattern System transforms combat from reactive chaos into observation-based mastery. Each enemy archetype has specific, learnable behavioral patterns that reward player knowledge and prediction over reflexes. This is the cornerstone of classic Mabinogi's methodical combat philosophy.

## Design Philosophy

### Core Principles
1. **Consistency Over Randomness** - Enemies follow predictable patterns, not random decisions
2. **Learnable Behaviors** - Patterns telegraph and repeat, rewarding observation
3. **Rule Equality** - AI follows identical combat rules as players (no cheating)
4. **Pattern Variety** - Multiple patterns per archetype prevent monotony
5. **Telegraph System** - Visual/audio cues warn before actions

### Classic Mabinogi Pattern Design
```
Bear Pattern Example:
┌──────────────────────────────────────────────┐
│ 1. Idle (observing)                          │
│ 2. Player approaches → Start Defense charge  │
│ 3. After 3 hits taken → Always Defense       │
│ 4. Defense broken → Smash charge             │
│ 5. Smash hits → Repeat from step 1           │
└──────────────────────────────────────────────┘
```

Players learn:
- "Bear uses Defense after 3 hits - I should use Smash next"
- "Bear telegraphs Smash with stance shift - I'll Counter"
- "Bear always Defenses when low HP - time for my Smash"

## System Architecture

### Component Hierarchy
```
AIPatternSystem (new)
├── PatternDefinition (ScriptableObject)
│   ├── Pattern nodes (states)
│   ├── Transition rules
│   └── Telegraph data
├── PatternExecutor (MonoBehaviour)
│   ├── Executes current pattern node
│   ├── Handles state transitions
│   └── Triggers telegraphs
└── TelegraphSystem (MonoBehaviour)
    ├── Visual telegraphs (stance, effects)
    ├── Audio telegraphs (sounds)
    └── Timing windows
```

### Integration with Existing AI
```
SimpleTestAI (existing)
├── Movement logic ✓ (keep)
├── Range management ✓ (keep)
├── Weapon swapping ✓ (keep)
├── Coordination ✓ (keep)
└── Skill selection ✗ (replace with pattern-based)
    └── NEW: PatternExecutor.GetNextSkill()
```

## Pattern Definition System

### PatternNode Structure
```csharp
[System.Serializable]
public class PatternNode
{
    [Header("Node Identity")]
    public string nodeName;
    public string description; // For designers

    [Header("Skill Selection")]
    public SkillType skillToUse;
    public bool requiresCharge = true;

    [Header("Conditions (All must be true to execute)")]
    public List<PatternCondition> conditions;

    [Header("Transitions")]
    public List<PatternTransition> transitions;

    [Header("Telegraph (Optional)")]
    public TelegraphData telegraph;
}
```

### PatternCondition Types
```csharp
public enum ConditionType
{
    // Health-based
    HealthAbove,        // HP > X%
    HealthBelow,        // HP < X%

    // Hit tracking
    HitsTakenCount,     // Taken N hits since last reset
    HitsDealtCount,     // Dealt N hits since last reset

    // Player state
    PlayerCharging,     // Player is charging a skill
    PlayerSkillType,    // Player charging specific skill
    PlayerCombatState,  // Player in specific state (knockdown, stun, etc)
    PlayerInRange,      // Player within distance

    // Self state
    StaminaAbove,       // Stamina > X%
    StaminaBelow,       // Stamina < X%
    SkillReady,         // Specific skill is charged/ready

    // Timing
    TimeElapsed,        // X seconds since pattern start/node entry
    CooldownExpired,    // Specific cooldown timer expired

    // Random
    RandomChance        // X% probability
}
```

### PatternTransition Structure
```csharp
[System.Serializable]
public class PatternTransition
{
    public string targetNodeName;
    public List<PatternCondition> conditions; // All must be true to transition
    public int priority = 0; // Higher priority evaluated first
    public bool resetHitCounters = false; // Reset hit tracking on transition
}
```

### Example: Bear Pattern Definition
```csharp
// Bear Pattern: Defensive tank that punishes aggression
PatternNode[] bearPattern = new PatternNode[]
{
    new PatternNode
    {
        nodeName = "Observe",
        description = "Watch player, prepare for Defense",
        skillToUse = SkillType.Defense,
        conditions = new[]
        {
            new PatternCondition { type = ConditionType.PlayerInRange, floatValue = 3.0f }
        },
        transitions = new[]
        {
            new PatternTransition
            {
                targetNodeName = "Defensive Stance",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.PlayerCharging, boolValue = true }
                },
                priority = 10
            },
            new PatternTransition
            {
                targetNodeName = "Pressure",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 3.0f }
                },
                priority = 5
            }
        },
        telegraph = new TelegraphData
        {
            visualType = TelegraphVisual.StanceShift,
            audioClip = "bear_grunt",
            duration = 0.5f
        }
    },

    new PatternNode
    {
        nodeName = "Defensive Stance",
        description = "Use Defense to block player attack",
        skillToUse = SkillType.Defense,
        conditions = new[]
        {
            new PatternCondition { type = ConditionType.StaminaAbove, floatValue = 10f }
        },
        transitions = new[]
        {
            new PatternTransition
            {
                targetNodeName = "Punish",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.HitsDealtCount, intValue = 1 } // After successful Defense
                },
                priority = 10
            },
            new PatternTransition
            {
                targetNodeName = "Observe",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.HitsTakenCount, intValue = 1 } // Defense was broken
                },
                priority = 5,
                resetHitCounters = true
            }
        },
        telegraph = new TelegraphData
        {
            visualType = TelegraphVisual.ShieldRaise,
            duration = 0.4f
        }
    },

    new PatternNode
    {
        nodeName = "Punish",
        description = "Smash after successful Defense",
        skillToUse = SkillType.Smash,
        conditions = new[]
        {
            new PatternCondition { type = ConditionType.StaminaAbove, floatValue = 5f }
        },
        transitions = new[]
        {
            new PatternTransition
            {
                targetNodeName = "Observe",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 1.0f } // After Smash completes
                },
                resetHitCounters = true
            }
        },
        telegraph = new TelegraphData
        {
            visualType = TelegraphVisual.WeaponRaise,
            audioClip = "bear_roar",
            duration = 0.6f
        }
    },

    new PatternNode
    {
        nodeName = "Pressure",
        description = "Occasional Attack to maintain pressure",
        skillToUse = SkillType.Attack,
        conditions = new[]
        {
            new PatternCondition { type = ConditionType.PlayerInRange, floatValue = 2.0f }
        },
        transitions = new[]
        {
            new PatternTransition
            {
                targetNodeName = "Observe",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 0.5f }
                },
                priority = 5
            },
            new PatternTransition
            {
                targetNodeName = "Defensive Stance",
                conditions = new[]
                {
                    new PatternCondition { type = ConditionType.HitsTakenCount, intValue = 3 }
                },
                priority = 10,
                resetHitCounters = true
            }
        }
    }
};
```

## Enemy Archetypes & Patterns

### 1. Bear (Defensive Tank)
**Philosophy:** Punishes aggression, rewards patience

**Pattern Flow:**
```
Observe → Defense (if player charges) → Smash (if Defense succeeds)
        ↓
   Attack (after 3s) → Defense (after 3 hits taken)
```

**Learnable Tells:**
- Raises shield before Defense
- Roars before Smash
- Always uses Defense after taking 3 hits

**Counter Strategy:**
- Use Smash to break Defense
- Bait Defense with fake charge → Cancel
- Attack during Smash charge window

### 2. Spider (Reactive Defender)
**Philosophy:** Defensive when pressured, aggressive when safe

**Pattern Flow:**
```
Observe → Attack (if player idle)
        ↓
   Defense (after 2 hits taken) → Counter (if player charges) → Windmill (if surrounded)
```

**Learnable Tells:**
- Crouches before Defense (after 2 hits)
- Legs raise before Counter
- Spins before Windmill

**Counter Strategy:**
- Space out attacks (prevent Defense trigger)
- Use Smash vs Defense
- Use Attack vs Counter
- Don't group up (avoid Windmill)

### 3. Wolf (Opportunistic Aggressor)
**Philosophy:** Exploits vulnerability, retreats when hurt

**Pattern Flow:**
```
Observe → Counter (if player charging) → Smash (after Defense succeeds)
        ↓
   Retreat (HP < 30%) → Attack (from distance)
```

**Learnable Tells:**
- Crouches before Counter
- Snarls before Smash
- Whimpers before Retreat

**Counter Strategy:**
- Don't charge when wolf is idle (baits Counter)
- Use Attack to break Counter safely
- Pressure during Retreat phase

### 4. Soldier (Balanced Fighter)
**Philosophy:** Adapts to player strategy, no strong bias

**Pattern Flow:**
```
Attack → Attack → Defense (if player charges)
      ↓
   Smash (random 20%) → Counter (if player aggressive)
```

**Learnable Tells:**
- Raises weapon before Smash
- Braces before Defense
- Subtle tells (harder to read than beasts)

**Counter Strategy:**
- Adapt to soldier's adaptation
- Mix up attack timings
- Watch for subtle stance shifts

### 5. Archer (Ranged Kiter)
**Philosophy:** Maintain distance, suppress with ranged attacks

**Pattern Flow:**
```
RangedAttack (if distance > 3.0) → Retreat (if player close)
                                 ↓
                            Defense (if cornered)
```

**Learnable Tells:**
- Nocks arrow before shooting
- Backs up before Retreat
- Raises arm before Defense

**Counter Strategy:**
- Close distance quickly
- Use Lunge to gap-close
- Pressure when cornered (low stamina)

### 6. Berserker (Aggressive Rusher)
**Philosophy:** Relentless offense, minimal defense

**Pattern Flow:**
```
Attack → Attack → Smash (80% chance) → Windmill (if low HP)
      ↓
   Defense (10% chance, low stamina only)
```

**Learnable Tells:**
- Always charges aggressively
- Roars before Smash
- Spins frantically before Windmill

**Counter Strategy:**
- Defense to block Smash
- Counter to punish Attack chains
- Finish quickly before Windmill

## Telegraph System Design

### Visual Telegraphs

**Implementation:**
```csharp
public enum TelegraphVisual
{
    None,
    StanceShift,      // Subtle body position change
    WeaponRaise,      // Weapon moves to attack position
    ShieldRaise,      // Shield/defensive posture
    EyeGlow,          // Eyes glow with skill color
    GroundEffect,     // AoE indicator for Windmill
    Crouch,           // Lower body before Counter
    BackStep          // Step back before Lunge/retreat
}
```

**Visual Timing:**
- Telegraph starts: 0.3-0.5s before skill charge begins
- Duration: Matches telegraph.duration field
- Subtle enough to miss if not observant
- Distinct enough to recognize once learned

**Color Coding (Optional):**
```
Red glow    → Smash (offensive)
Blue glow   → Defense (defensive)
Purple glow → Counter (reactive)
Green glow  → Windmill (AoE)
Yellow glow → Lunge (mobility)
```

### Audio Telegraphs

**Implementation:**
```csharp
[System.Serializable]
public class TelegraphData
{
    public TelegraphVisual visualType;
    public string audioClip; // Loaded from Resources
    public float duration = 0.5f;
    public Color glowColor = Color.white;
}
```

**Audio Design:**
- **Bear:** Grunts/roars (deep, intimidating)
- **Spider:** Chittering/hissing (high-pitched, creepy)
- **Wolf:** Snarls/growls (aggressive, sharp)
- **Soldier:** Weapon sounds (metal clinks, armor shifts)
- **Archer:** Bowstring creak (taut, anticipatory)
- **Berserker:** Battle cries (loud, chaotic)

**Audio Timing:**
- Plays simultaneously with visual telegraph start
- 3D spatial audio (louder when closer)
- Distinct per archetype to aid recognition

## Implementation Roadmap

### File Structure
```
Assets/Scripts/Combat/AI/
├── Patterns/
│   ├── AIPatternSystem.cs (core system)
│   ├── PatternDefinition.cs (ScriptableObject)
│   ├── PatternNode.cs (data structures)
│   ├── PatternCondition.cs (condition evaluation)
│   ├── PatternExecutor.cs (MonoBehaviour)
│   └── TelegraphSystem.cs (visual/audio feedback)
├── PatternData/ (ScriptableObject assets)
│   ├── BearPattern.asset
│   ├── SpiderPattern.asset
│   ├── WolfPattern.asset
│   ├── SoldierPattern.asset
│   ├── ArcherPattern.asset
│   └── BerserkerPattern.asset
└── SimpleTestAI.cs (modified to use patterns)
```

### Phase 5.1: Core Pattern System
**Deliverables:**
- [ ] PatternDefinition.cs (ScriptableObject)
- [ ] PatternNode.cs (data structures)
- [ ] PatternCondition.cs (condition types & evaluation)
- [ ] PatternExecutor.cs (pattern state machine)

**Testing:**
- Create simple 2-node pattern
- Verify transitions work correctly
- Test all condition types

### Phase 5.2: Telegraph System
**Deliverables:**
- [ ] TelegraphSystem.cs (MonoBehaviour)
- [ ] TelegraphData.cs (data structures)
- [ ] Visual telegraph implementations (stance, glow, effects)
- [ ] Audio telegraph system (3D spatial audio)

**Testing:**
- Verify telegraphs trigger before skill charge
- Test timing feels "fair" (0.3-0.5s warning)
- Verify audio is distinct per archetype

### Phase 5.3: Enemy Archetypes
**Deliverables:**
- [ ] BearPattern.asset (defensive tank)
- [ ] SpiderPattern.asset (reactive defender)
- [ ] WolfPattern.asset (opportunistic aggressor)
- [ ] SoldierPattern.asset (balanced fighter)
- [ ] ArcherPattern.asset (ranged kiter)
- [ ] BerserkerPattern.asset (aggressive rusher)

**Testing:**
- Playtest each archetype
- Verify patterns are learnable
- Ensure no "unfair" patterns

### Phase 5.4: Integration with SimpleTestAI
**Deliverables:**
- [ ] Modify SimpleTestAI.SelectSkill() to use PatternExecutor
- [ ] Integrate telegraph system into skill charging
- [ ] Preserve existing movement/coordination logic

**Testing:**
- Verify AI still coordinates attacks
- Test movement maintains optimal range
- Ensure weapon swapping still works

### Phase 5.5: Polish & Tuning
**Deliverables:**
- [ ] Telegraph visual effects (particle systems, shader effects)
- [ ] Telegraph audio mixing (volume, spatial blend)
- [ ] Pattern timing tuning (difficulty balance)
- [ ] Debug visualization tools

**Testing:**
- Playtest with fresh players (can they learn patterns?)
- Gather feedback on telegraph clarity
- Tune difficulty per archetype

## Pattern Debugging Tools

### Inspector Visualization
```csharp
#if UNITY_EDITOR
[CustomEditor(typeof(PatternExecutor))]
public class PatternExecutorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PatternExecutor executor = (PatternExecutor)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Node: {executor.CurrentNode?.nodeName ?? "None"}");
            EditorGUILayout.LabelField($"Hit Counter: {executor.HitsTaken} taken / {executor.HitsDealt} dealt");
            EditorGUILayout.LabelField($"Time in Node: {executor.TimeInCurrentNode:F1}s");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Transitions", EditorStyles.boldLabel);
            foreach (var transition in executor.GetAvailableTransitions())
            {
                EditorGUILayout.LabelField($"→ {transition.targetNodeName} (priority {transition.priority})");
            }
        }
    }
}
#endif
```

### Runtime Debug GUI
```csharp
private void OnGUI()
{
    if (!showPatternDebugGUI) return;

    GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));
    GUILayout.BeginVertical("box");

    GUILayout.Label($"<b>Pattern Debug: {gameObject.name}</b>");
    GUILayout.Label($"Pattern: {patternDefinition?.name ?? "None"}");
    GUILayout.Label($"Current Node: {currentNode?.nodeName ?? "None"}");
    GUILayout.Label($"Next Skill: {currentNode?.skillToUse}");
    GUILayout.Label($"Time in Node: {timeInCurrentNode:F1}s");

    GUILayout.Space(5);
    GUILayout.Label("<b>Hit Tracking:</b>");
    GUILayout.Label($"  Hits Taken: {hitsTaken}");
    GUILayout.Label($"  Hits Dealt: {hitsDealt}");

    GUILayout.Space(5);
    GUILayout.Label("<b>Active Transitions:</b>");
    foreach (var transition in GetActiveTransitions())
    {
        bool conditionsMet = EvaluateTransitionConditions(transition);
        string status = conditionsMet ? "✓ READY" : "✗ WAITING";
        GUILayout.Label($"  {status} → {transition.targetNodeName}");
    }

    GUILayout.EndVertical();
    GUILayout.EndArea();
}
```

## Balance Considerations

### Pattern Difficulty Tiers

**Tier 1 (Beginner-Friendly):**
- Bear, Soldier
- Clear telegraphs (0.5s)
- Simple 3-4 node patterns
- Consistent behavior

**Tier 2 (Intermediate):**
- Spider, Archer
- Subtle telegraphs (0.4s)
- 4-6 node patterns
- Some conditional branching

**Tier 3 (Advanced):**
- Wolf, Berserker
- Minimal telegraphs (0.3s)
- 6+ node patterns
- Complex conditional logic

### Pattern Variation

To prevent patterns from feeling too rigid:

**Variation Strategies:**
1. **Multiple Patterns Per Archetype** - Bears can have "Aggressive" or "Defensive" pattern variants
2. **Random Branches** - 20% chance nodes that add unpredictability
3. **Adaptive Conditions** - Patterns that react to player strategy
4. **Health-Based Transitions** - Pattern shifts at 50% HP, 20% HP thresholds

**Example: Bear Pattern Variant**
```
Standard Bear: Defense-focused, punishes aggression
Enraged Bear (HP < 30%): Berserker-like, Smash/Windmill spam
```

## Success Metrics

### Short-Term (2 Weeks)
- [ ] Players recognize patterns after 3-5 encounters
- [ ] Telegraph clarity rated 8/10 or higher
- [ ] Pattern variety prevents monotony
- [ ] No "unfair" or "impossible" patterns reported

### Long-Term (2 Months)
- [ ] Players can predict enemy actions 70%+ accuracy
- [ ] Combat encounters feel strategic, not random
- [ ] New players learn patterns naturally (no tutorial needed)
- [ ] Veteran players exploit patterns for optimal combat

## Conclusion

The AI Pattern System is the culmination of classic Mabinogi's combat philosophy: **knowledge-based mastery through observation and prediction**. By making enemies consistent, telegraphed, and learnable, combat shifts from reactive button-mashing to strategic decision-making.

**Key Success Factors:**
1. **Consistency** - Same pattern, every time (with minor variation)
2. **Telegraphs** - Fair warning before actions (0.3-0.5s)
3. **Learnability** - Patterns obvious after 3-5 encounters
4. **Variety** - Multiple archetypes prevent monotony
5. **Equality** - AI follows same rules as players

This system transforms FairyGate's combat from "fighting AI" to "dueling opponents with personalities and strategies" - exactly as classic Mabinogi intended.
