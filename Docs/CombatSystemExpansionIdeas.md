# Combat System Expansion Ideas

**Document Version:** 1.0
**Last Updated:** 2025-11-01
**State Machine Migration Status:** ✅ Complete (Phases 1-5)

---

## Table of Contents

1. [State Machine Architecture Overview](#state-machine-architecture-overview)
2. [Current System Capabilities](#current-system-capabilities)
3. [Quick Wins](#quick-wins-easy-implementation-high-impact)
4. [Medium-Term Goals](#medium-term-goals-moderate-complexity)
5. [Long-Term Vision](#long-term-vision-complex-but-transformative)
6. [Implementation Priority](#implementation-priority)
7. [Best Practices](#best-practices)

---

## State Machine Architecture Overview

### Core Components

**State Pattern Structure:**
- **ISkillState Interface**: Defines lifecycle contract (OnEnter/Update/OnExit/GetNextState)
- **SkillStateBase**: Abstract base providing common functionality
- **SkillStateMachine**: Orchestrator guaranteeing lifecycle execution
- **8 State Implementations**: Uncharged, Charging, Charged, Aiming, Startup, Active, Waiting, Recovery

### Key Architectural Strengths

1. **Guaranteed Lifecycle Management**
   - `OnExit()` ALWAYS runs on transitions (prevents memory leaks)
   - `OnEnter()` handles setup with centralized movement restrictions
   - `Update()` provides per-frame logic with clear transition conditions

2. **Clean Separation of Concerns**
   - States own their transition logic (`GetNextState()`)
   - SkillSystem provides component access (StaminaSystem, WeaponController, etc.)
   - CombatInteractionManager handles multi-combatant interactions

3. **Event-Driven Architecture**
   - TriggerSkillCharged/Executed/Cancelled events
   - External systems can force transitions via `ForceTransitionToRecovery()`
   - States fire events at lifecycle boundaries

4. **Deterministic Behavior**
   - No coroutine race conditions
   - Single source of truth (`currentState`)
   - Inspector-debuggable (`currentStateName` visible)

5. **Memory Management**
   - Object pooling (SkillExecutionPool, List pools)
   - Cleanup guarantees prevent leaks
   - Efficient struct usage for temporary data

---

## Current System Capabilities

### Skill Types & Flows

| Skill Type | State Flow | Special Mechanics |
|------------|-----------|-------------------|
| **Attack** | Uncharged → Startup → Active → Recovery | Instant execution, no charge |
| **Smash/Windmill/Lunge** | Uncharged → Charging → Charged → Startup → Active → Recovery | Heavy attacks, knockdown effects |
| **Defense/Counter** | Uncharged → Charging → Charged → Startup → Active → **Waiting** → Recovery | Stamina drain during waiting, one-hit block |
| **RangedAttack** | Uncharged → **Aiming** → Active → Recovery | Accuracy system, hit/miss rolls |

### Interaction Matrix

**9 Unique Offensive vs Defensive Interactions:**
1. Attack vs Defense → Attacker Stunned
2. Attack vs Counter → Counter Reflection
3. Smash vs Defense → Defender Knocked Down (75% damage reduction)
4. Smash vs Counter → Counter Reflection
5. Windmill vs Defense → Defender Blocks (0 damage)
6. Windmill vs Counter → Windmill Breaks Counter (knockdown)
7. Lunge vs Defense → Attacker Stunned
8. Lunge vs Counter → Counter Reflection
9. RangedAttack vs Defense/Counter → Defender Blocks / Counter Ineffective

### Status Effects

**Three-Tier CC System:**
- **Stun** (0.8s) - Brief freeze, can still charge skills
- **Knockback** (0.8s) - Movement displacement, can still charge skills
- **Knockdown** (2.0s) - Full disable, cancels active skills

### Resource System

**Stamina Management:**
- Skill execution costs (Attack: 30, Defense: 40, Counter: 40, Smash: 50, Windmill: 60, Lunge: 40)
- Continuous drain during defensive waiting (Defense: 3/s, Counter: 5/s)
- Auto-cancel on depletion
- Regeneration during rest (20/s)

---

## Quick Wins (Easy Implementation, High Impact)

### 1. Charge Level System

**Complexity:** ⭐ Easy
**Impact:** High skill expression, risk/reward timing

**Description:**
Allow skills to be executed at different charge levels (25%, 50%, 75%, 100%) with varying effects.

**Implementation Approach:**
- Modify `ChargingState.Update()` to accept execution at any charge level
- Track `chargeLevel` (0.0-1.0) for damage scaling
- Add visual/audio feedback for charge milestones

**Example Code:**
```csharp
// ChargingState.cs modification
public override bool Update(float deltaTime)
{
    if (Input.GetKeyDown(executeKey)) // Allow early execution
    {
        if (skillSystem.ChargeProgress >= 0.25f) // Minimum 25% charge
        {
            return true; // Allow transition to Charged
        }
    }

    // Existing charging logic continues...
}

// DamageCalculator.cs modification
public static int CalculateBaseDamage(CharacterStats attacker, WeaponData weapon,
    CharacterStats defender, SkillType skillType, float chargeLevel = 1.0f)
{
    int baseDamage = weapon.damage + attacker.strength;
    baseDamage = Mathf.RoundToInt(baseDamage * chargeLevel); // Scale by charge
    // ... rest of damage calculation
}
```

**Use Case:**
- 50% charge Smash = 50% damage, 0.5s startup (fast but weak)
- 100% charge Smash = 100% damage, 1.0s startup, guaranteed knockdown

---

### 2. Skill Cancel System (Recovery Cancels)

**Complexity:** ⭐ Easy
**Impact:** Combat fluidity, combo potential

**Description:**
Allow canceling recovery frames into another skill by consuming extra stamina.

**Implementation Approach:**
- Add cancel window to `RecoveryState` (first 30% of recovery)
- Consume stamina penalty (e.g., 2x normal cost)
- Transition directly to new skill's Charging state

**Example Code:**
```csharp
// RecoveryState.cs modification
public override bool Update(float deltaTime)
{
    elapsedTime += deltaTime;

    // Check for cancel input during window
    float cancelWindowEnd = recoveryTime * 0.3f; // First 30%
    if (elapsedTime <= cancelWindowEnd)
    {
        SkillType? cancelInput = skillSystem.GetSkillFromInput();
        if (cancelInput.HasValue && CanCancelInto(cancelInput.Value))
        {
            int cancelCost = CombatConstants.RECOVERY_CANCEL_COST; // e.g., 20
            if (skillSystem.StaminaSystem.ConsumeStamina(cancelCost))
            {
                skillSystem.StateMachine.TransitionTo(
                    new ChargingState(skillSystem, cancelInput.Value));
                return false;
            }
        }
    }

    // Normal recovery continues...
}
```

**Use Case:**
- Player executes Attack (30 stamina)
- During recovery, presses Smash
- Costs: Attack (30) + Smash (50) + Cancel Penalty (20) = 100 total
- Skips recovery, immediately begins Smash charging

---

### 3. Perfect Defense (Timed Blocking)

**Complexity:** ⭐ Easy
**Impact:** Skill-based depth, satisfying defense mechanics

**Description:**
Reward precise defense timing with bonus effects (no stamina drain, enhanced stun).

**Implementation Approach:**
- Track if attack hits during first 0.5s of `WaitingState`
- Perfect block = no stamina drain, 2x stun duration, instant recovery
- Add visual/audio feedback for perfect timing

**Example Code:**
```csharp
// WaitingState.cs modification
private float perfectWindowDuration = 0.5f;
public bool IsPerfectDefense() => elapsedTime <= perfectWindowDuration;

// CombatInteractionManager.cs modification
private void ProcessSkillInteraction(SkillExecution offensive, SkillExecution defensive)
{
    if (defensive.skillType == SkillType.Defense)
    {
        var waitingState = defensive.skillSystem.StateMachine.CurrentState as WaitingState;
        if (waitingState?.IsPerfectDefense() == true)
        {
            // Perfect block bonuses
            attackerStatusEffects.ApplyStun(attackerWeapon.stunDuration * 2.0f);
            CompleteDefensiveSkillExecution(defensive, instant: true);
            return; // Skip normal stamina drain
        }
    }

    // Normal interaction continues...
}
```

**Use Case:**
- Enemy winds up Smash (2s startup)
- Player activates Defense just before impact (0.5s window)
- Perfect Block: No stamina drain, instant recovery, enemy stunned for 1.6s (vs 0.8s)

---

### 4. Cooldown System

**Complexity:** ⭐ Easy
**Impact:** Better balance, tactical choices

**Description:**
Add per-skill cooldowns independent of stamina to limit powerful skill spam.

**Implementation Approach:**
- Track `lastUsed` timestamp per skill in `SkillSystem`
- Check cooldown in `CanChargeSkill()`
- Add cooldown constants (e.g., SMASH_COOLDOWN = 10f)

**Example Code:**
```csharp
// SkillSystem.cs addition
private Dictionary<SkillType, float> lastSkillUse = new Dictionary<SkillType, float>();

public bool CanChargeSkill(SkillType skillType)
{
    // Existing checks...

    // Cooldown check
    float cooldown = GetSkillCooldown(skillType);
    if (cooldown > 0f)
    {
        if (lastSkillUse.TryGetValue(skillType, out float lastUse))
        {
            if (Time.time - lastUse < cooldown)
                return false; // Still on cooldown
        }
    }

    return true;
}

private float GetSkillCooldown(SkillType skillType)
{
    return skillType switch
    {
        SkillType.Smash => 10f,
        SkillType.Windmill => 15f,
        _ => 0f // No cooldown
    };
}

// In RecoveryState.OnExit() or similar
skillSystem.RecordSkillUse(skillType); // Sets lastSkillUse[skillType] = Time.time
```

**Use Case:**
- Smash has 10s cooldown (prevents spam)
- Player uses Smash → must wait 10s before charging again
- Can still use Attack/Defense/Counter during cooldown

---

## Medium-Term Goals (Moderate Complexity)

### 5. Combo Chain System

**Complexity:** ⭐⭐ Medium
**Impact:** Skill sequencing depth, rewards mastery

**Description:**
Specific skill sequences (Attack → Attack → Smash) grant damage/speed bonuses.

**Implementation Approach:**
- Create `ComboTracker` MonoBehaviour component
- Define combos via `ComboDefinition` ScriptableObjects
- Track recent skill history (last 3 skills with timestamps)
- Apply bonuses when patterns match

**Example Code:**
```csharp
// ComboDefinition.cs
[CreateAssetMenu(menuName = "Combat/Combo Definition")]
public class ComboDefinition : ScriptableObject
{
    public string comboName = "Triple Assault";
    public SkillType[] sequence = { SkillType.Attack, SkillType.Attack, SkillType.Smash };
    public float damageBonus = 0.2f;     // +20% damage
    public float recoveryReduction = 0.5f; // -50% recovery time
    public float windowTime = 2.0f;      // Must complete within 2s
}

// ComboTracker.cs
public class ComboTracker : MonoBehaviour
{
    [SerializeField] private ComboDefinition[] availableCombos;
    private List<(SkillType, float)> skillHistory = new List<(SkillType, float)>();

    public event Action<ComboDefinition> OnComboComplete;
    public ComboDefinition ActiveCombo { get; private set; }

    public void RecordSkill(SkillType skillType)
    {
        skillHistory.Add((skillType, Time.time));
        skillHistory.RemoveAll(entry => Time.time - entry.Item2 > 5f); // Cleanup old
        CheckForCombos();
    }

    private void CheckForCombos()
    {
        foreach (var combo in availableCombos)
        {
            if (MatchesCombo(combo))
            {
                ActiveCombo = combo;
                OnComboComplete?.Invoke(combo);
            }
        }
    }

    private bool MatchesCombo(ComboDefinition combo)
    {
        if (skillHistory.Count < combo.sequence.Length) return false;

        // Check last N skills match sequence
        for (int i = 0; i < combo.sequence.Length; i++)
        {
            int historyIndex = skillHistory.Count - combo.sequence.Length + i;
            if (skillHistory[historyIndex].Item1 != combo.sequence[i])
                return false;
        }

        // Check timing window
        float firstTime = skillHistory[skillHistory.Count - combo.sequence.Length].Item2;
        return Time.time - firstTime <= combo.windowTime;
    }
}
```

**Use Case:**
- Player executes Attack → Attack → Smash within 2s
- "Triple Assault" combo triggers
- Final Smash gets +20% damage, -50% recovery time
- Visual combo counter appears in UI

---

### 6. Feint/Fake-Out Skills

**Complexity:** ⭐⭐ Medium
**Impact:** Mind games, baiting reactions, PvP depth

**Description:**
Skills that pretend to be one type but switch mid-execution.

**Implementation Approach:**
- Create `FeintState` that mimics another skill's startup
- After feint window (0.3s), branch to real skill's Active state
- Display fake skill to opponent during feint window

**Example Code:**
```csharp
// FeintState.cs
public class FeintState : SkillStateBase
{
    private SkillType fakeSkill;
    private SkillType realSkill;
    private float feintWindowDuration = 0.3f;

    public FeintState(SkillSystem system, SkillType fake, SkillType real)
        : base(system, real)
    {
        fakeSkill = fake;
        realSkill = real;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        skillSystem.CurrentSkill = fakeSkill; // Show fake to opponent

        // Play fake skill's startup animation
        // Show fake visual effects
    }

    public override bool Update(float deltaTime)
    {
        elapsedTime += deltaTime;

        if (elapsedTime >= feintWindowDuration)
        {
            skillSystem.CurrentSkill = realSkill; // Reveal real skill
            return true; // Transition to Active
        }

        return false;
    }

    public override SkillExecutionState GetStateType()
    {
        return SkillExecutionState.Startup; // Appears as startup
    }

    public override ISkillState GetNextState()
    {
        return new ActiveState(skillSystem, realSkill);
    }
}
```

**Use Case:**
- Player starts "Smash" startup animation
- Enemy sees Smash, activates Counter (expecting to reflect heavy damage)
- Player feints into Attack instead
- Attack vs Counter = CounterReflection, but feint deals reduced damage

---

### 7. Channeled/Sustained Skills

**Complexity:** ⭐⭐ Medium
**Impact:** New skill archetype, sustained pressure

**Description:**
Skills that require holding input and drain stamina continuously while active.

**Implementation Approach:**
- Create `ChannelingState` that loops while input held
- Continuous stamina drain per frame
- Apply effects per tick (damage, buffs, debuffs)
- Ends on input release, stamina depletion, or CC

**Example Code:**
```csharp
// ChannelingState.cs
public class ChannelingState : SkillStateBase
{
    private float drainRate;
    private float tickRate = 0.5f; // Apply effect every 0.5s
    private float tickTimer = 0f;

    public ChannelingState(SkillSystem system, SkillType type) : base(system, type)
    {
        drainRate = type == SkillType.BerserkerStance ? 10f : 5f;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyChannelStart(); // Initial buffs/effects
    }

    public override bool Update(float deltaTime)
    {
        // Check if input still held
        if (!skillSystem.IsSkillInputHeld(skillType))
            return true; // Input released, end channel

        // Drain stamina
        skillSystem.StaminaSystem.DrainStamina(drainRate, deltaTime);
        if (skillSystem.StaminaSystem.CurrentStamina <= 0)
            return true; // Out of stamina, end channel

        // Apply tick effects
        tickTimer += deltaTime;
        if (tickTimer >= tickRate)
        {
            ApplyChannelTick();
            tickTimer = 0f;
        }

        return false; // Continue channeling
    }

    private void ApplyChannelTick()
    {
        // Example: AoE damage around character
        var nearbyEnemies = Physics.OverlapSphere(
            skillSystem.transform.position,
            CombatConstants.CHANNEL_RADIUS);

        foreach (var collider in nearbyEnemies)
        {
            var enemy = collider.GetComponent<HealthSystem>();
            if (enemy != null)
            {
                int tickDamage = 10; // 20 DPS (0.5s tick rate)
                enemy.TakeDamage(tickDamage, skillSystem.transform);
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        RemoveChannelEffects(); // Remove buffs
    }

    public override SkillExecutionState GetStateType()
    {
        return SkillExecutionState.Active; // Treated as active
    }

    public override ISkillState GetNextState()
    {
        return new RecoveryState(skillSystem, skillType);
    }
}
```

**Use Case:**
- "Berserker Stance" - Hold button to channel
- Drains 10 stamina/second
- +50% damage buff while active
- AoE damage pulse every 0.5s
- Cannot move while channeling

---

### 8. Status Effect Expansion (DoT/Buffs/Debuffs)

**Complexity:** ⭐⭐ Medium
**Impact:** Combat variety, build diversity

**Description:**
Add damage-over-time, buffs, and debuffs with durations and stacking.

**Implementation Approach:**
- Create `StatusEffect` base class with duration, tick rate, magnitude
- Derived classes: `DamageOverTime`, `BuffEffect`, `DebuffEffect`
- Expand `StatusEffectManager` to track active effects
- Per-frame update to tick effects

**Example Code:**
```csharp
// StatusEffect.cs
public abstract class StatusEffect
{
    public string effectName;
    public float duration;
    public float tickRate = 1.0f;
    public int stacks = 1;
    public int maxStacks = 1;

    protected GameObject target;
    protected float elapsedSinceLastTick = 0f;

    public abstract void OnApply(GameObject target);
    public abstract void OnTick();
    public abstract void OnRemove();

    public bool Update(float deltaTime)
    {
        duration -= deltaTime;
        elapsedSinceLastTick += deltaTime;

        if (elapsedSinceLastTick >= tickRate)
        {
            OnTick();
            elapsedSinceLastTick = 0f;
        }

        return duration <= 0f; // Returns true when expired
    }

    public void AddStack()
    {
        if (stacks < maxStacks)
        {
            stacks++;
            OnStackAdded();
        }
        else
        {
            RefreshDuration(); // Reset duration instead
        }
    }

    protected virtual void OnStackAdded() { }
    protected virtual void RefreshDuration() { duration = initialDuration; }
}

// PoisonEffect.cs
public class PoisonEffect : StatusEffect
{
    public int damagePerTick = 5;

    public override void OnApply(GameObject target)
    {
        this.target = target;
        // Visual: Green poison particles
    }

    public override void OnTick()
    {
        int totalDamage = damagePerTick * stacks;
        target.GetComponent<HealthSystem>().TakeDamage(totalDamage, null);
    }

    public override void OnRemove()
    {
        // Visual: Stop poison particles
    }
}

// Usage in weapon
public class WeaponData : ScriptableObject
{
    public StatusEffect onHitEffect; // Assign PoisonEffect ScriptableObject
}
```

**Use Case:**
- Poison Dagger applies "Poisoned" debuff on hit
- 5 damage per second for 10 seconds
- Stacks up to 3 times (15 DPS max)
- Green visual effect on poisoned character

---

## Long-Term Vision (Complex but Transformative)

### 9. Conditional State Branching

**Complexity:** ⭐⭐⭐ Hard
**Impact:** Adaptive combat, dynamic difficulty

**Description:**
States transition differently based on runtime conditions (health, combo meter, enemy type).

**Key Concept:**
Modify `GetNextState()` to evaluate context and choose different paths.

**Example:**
```csharp
// ConditionalActiveState.cs
public class ConditionalActiveState : SkillStateBase
{
    public override ISkillState GetNextState()
    {
        float healthPercent = skillSystem.CombatController.CurrentHealth /
                              (float)skillSystem.CombatController.MaxHealth;

        // Low HP: Skip recovery (desperate play)
        if (healthPercent < 0.3f && skillType == SkillType.Attack)
        {
            return new UnchargedState(skillSystem, SkillType.Attack);
        }

        // Normal flow
        return new RecoveryState(skillSystem, skillType);
    }
}
```

---

### 10. Multi-Target/AoE System

**Complexity:** ⭐⭐⭐ Hard
**Impact:** Crowd control, tactical positioning

**Description:**
Skills hit multiple enemies simultaneously (Windmill already does this, expand to others).

**Implementation:**
Modify `ActiveState.ProcessSkillExecution()` to find targets in radius, process interaction with each.

---

### 11. Skill Customization/Modding System

**Complexity:** ⭐⭐⭐⭐ Very Hard
**Impact:** Progression, build variety, replayability

**Description:**
Runtime skill modification through talents, equipment, or upgrades.

**Key Components:**
- `SkillModifier` ScriptableObjects
- Query modifiers before damage/stamina/timing calculations
- Allow stacking modifiers (multiplicative/additive)

---

### 12. AI Team Coordination

**Complexity:** ⭐⭐⭐⭐ Very Hard
**Impact:** Dynamic team battles, emergent gameplay

**Description:**
Multiple AI allies coordinate attacks, buffs, and positioning via `TeamCoordinator` singleton.

**Example Tactics:**
- Focus Fire (all attack same target)
- Flank (surround enemies)
- Protect (guard low-health ally)
- Setup Combo (coordinate skill timing)

---

## Implementation Priority

### Phase 1: Quick Wins (This Month)
1. ✅ Charge Level System
2. ✅ Perfect Defense
3. ✅ Cooldown System

### Phase 2: Combat Depth (Next Month)
4. ✅ Skill Cancel System
5. ✅ Combo Chain System
6. ✅ Status Effect Expansion

### Phase 3: New Archetypes (Next Quarter)
7. ✅ Channeled Skills
8. ✅ Feint System
9. ✅ Multi-Target/AoE

### Phase 4: Advanced Systems (6+ Months)
10. ✅ Conditional Branching
11. ✅ Skill Customization
12. ✅ AI Coordination

---

## Best Practices

### When Adding New Features

**✅ DO:**
- Use existing states when possible (add logic to `Update()` or `GetNextState()`)
- Follow the lifecycle pattern (setup in `OnEnter()`, cleanup in `OnExit()`)
- Use events to communicate with external systems
- Pool allocations (follow existing `SkillExecutionPool` pattern)
- Trust the state machine (`TransitionTo()` guarantees `OnExit()` calls)
- Add debug logging with color tags for state transitions
- Test with both AI and player characters

**❌ DON'T:**
- Bypass `TransitionTo()` - breaks cleanup guarantees
- Put complex logic in `Update()` - use helper methods
- Create states for every skill variant - use conditionals
- Forget to call `base.OnEnter()` or `base.OnExit()`
- Allocate without pooling (causes GC pressure)
- Modify state directly from external systems - use public methods
- Skip testing with edge cases (stamina depletion, CC during execution, etc.)

### Code Patterns to Follow

**State Modification Pattern:**
```csharp
// Good: Modify existing state
public class RecoveryState : SkillStateBase
{
    public override bool Update(float deltaTime)
    {
        // Add new feature logic here
        if (SomeCondition())
        {
            // Handle special case
        }

        // Existing logic continues...
    }
}
```

**Component Augmentation Pattern:**
```csharp
// Good: Add component instead of modifying SkillSystem
public class ComboTracker : MonoBehaviour
{
    private SkillSystem skillSystem;

    void Awake()
    {
        skillSystem = GetComponent<SkillSystem>();
        skillSystem.OnSkillExecuted += RecordSkill;
    }
}
```

**Conditional Branching Pattern:**
```csharp
// Good: Use GetNextState() for conditionals
public override ISkillState GetNextState()
{
    if (specialCondition)
        return new SpecialState(skillSystem, skillType);

    return new NormalState(skillSystem, skillType);
}
```

---

## Conclusion

The state machine architecture provides a **solid foundation** for combat system expansion:

- ✅ **Guaranteed lifecycle** prevents bugs
- ✅ **Event-driven** enables clean integration
- ✅ **Deterministic** behavior aids debugging
- ✅ **Extensible** without breaking existing features

Most expansions require **modifying existing states** rather than creating new ones. The architecture's flexibility comes from:
1. Conditional `GetNextState()` logic
2. Event subscriptions in external components
3. Helper methods for complex behaviors
4. ScriptableObject-based data definitions

Start with Quick Wins to build confidence, then progressively tackle more complex features. The architecture scales beautifully from simple tweaks to transformative systems!

---

**Happy Coding! 🎮⚔️**
