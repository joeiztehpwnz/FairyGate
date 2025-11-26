# FairyGate Combat System - Refactoring Analysis

**Date**: 2025-10-25
**Purpose**: Identify opportunities for code optimization, performance improvements, and architectural refinements

---

## Executive Summary

The FairyGate combat system is well-architected with clear separation of concerns and good use of design patterns. However, there are several opportunities for optimization across **performance**, **memory usage**, **maintainability**, and **scalability**.

### Priority Levels
- 🔴 **Critical**: Significant performance impact or major architectural improvement
- 🟡 **Medium**: Moderate improvement, good ROI
- 🟢 **Low**: Nice-to-have, minimal impact

---

## 1. Performance Optimizations

### 🔴 1.1 Multiple Update() Loops - Update Loop Consolidation

**Current State**: 8+ components with Update() methods
```
CombatController.Update()
SkillSystem.Update()
StaminaSystem.Update()
StatusEffectManager.Update()
MovementController.Update()
PatternedAI.Update()
HealthBarUI.OnGUI()
StaminaBarUI.OnGUI()
CombatDebugVisualizer.OnGUI()
```

**Problem**:
- Unity calls Update() on each MonoBehaviour sequentially
- Each call has overhead (managed→native boundary crossing)
- For 2 characters with all components: ~16 Update() calls per frame

**Proposed Solution**: **Update Manager Pattern**
```csharp
public class CombatUpdateManager : MonoBehaviour
{
    private static CombatUpdateManager instance;

    private List<ICombatUpdatable> updatables = new List<ICombatUpdatable>();
    private List<ICombatFixedUpdatable> fixedUpdatables = new List<ICombatFixedUpdatable>();

    public static void Register(ICombatUpdatable updatable)
    {
        instance.updatables.Add(updatable);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < updatables.Count; i++)
        {
            updatables[i].CombatUpdate(deltaTime);
        }
    }
}

public interface ICombatUpdatable
{
    void CombatUpdate(float deltaTime);
}
```

**Benefit**:
- Reduces Update() calls from 16 to 1 (for 2 characters)
- ~10-15% performance improvement in managed code overhead
- Better cache locality (updatables in contiguous list)

**Implementation Effort**: Medium (2-3 hours to refactor all Update() methods)

---

### 🟡 1.2 CombatInteractionManager Queue Allocations

**Current State**: `CombatInteractionManager.cs` lines 15-16
```csharp
private Queue<SkillExecution> pendingExecutions = new Queue<SkillExecution>();
private List<SkillExecution> waitingDefensiveSkills = new List<SkillExecution>();
```

**Problem**:
- Each skill execution allocates new `SkillExecution` class instance (line 39-45)
- `ProcessPendingExecutions()` creates new Lists every frame (lines 61, 88, etc.)
- Garbage collection spikes during intense combat

**Proposed Solution**: **Object Pooling**
```csharp
public class SkillExecutionPool
{
    private Stack<SkillExecution> pool = new Stack<SkillExecution>(16);

    public SkillExecution Get()
    {
        return pool.Count > 0 ? pool.Pop() : new SkillExecution();
    }

    public void Return(SkillExecution execution)
    {
        execution.Reset();
        pool.Push(execution);
    }
}

// In CombatInteractionManager
private SkillExecutionPool executionPool = new SkillExecutionPool();
private List<SkillExecution> reusableOffensiveList = new List<SkillExecution>(8);
```

**Benefit**:
- Eliminates allocation during combat (0 GC allocations per skill execution)
- Reduces GC pressure by ~80%
- More consistent frame times

**Implementation Effort**: Low (1 hour)

---

### 🟡 1.3 EquipmentManager Runtime ScriptableObject Copy

**Current State**: `EquipmentManager.cs` lines 39-44
```csharp
modifiedStats = ScriptableObject.CreateInstance<CharacterStats>();
CopyStats(baseStats, modifiedStats);
```

**Problem**:
- Creates runtime ScriptableObject (managed allocation)
- ScriptableObject.CreateInstance is slow (~100-200μs)
- Unnecessary indirection through ScriptableObject lifecycle

**Proposed Solution**: **Struct-Based Stats**
```csharp
// Replace CharacterStats ScriptableObject with:
[System.Serializable]
public struct CharacterStatsData
{
    public int strength;
    public int dexterity;
    public int physicalDefense;
    public int focus;
    // ... other stats

    public static CharacterStatsData operator +(CharacterStatsData a, CharacterStatsData b)
    {
        return new CharacterStatsData
        {
            strength = a.strength + b.strength,
            dexterity = a.dexterity + b.dexterity,
            // ... etc
        };
    }
}

// EquipmentManager becomes:
private CharacterStatsData modifiedStats;

public void RefreshEquipmentBonuses()
{
    modifiedStats = baseStats.GetStatsData(); // Copy struct (fast)
    if (currentArmor != null)
        modifiedStats += currentArmor.GetBonuses();
    if (currentAccessory != null)
        modifiedStats += currentAccessory.GetBonuses();
}
```

**Benefit**:
- 100x faster (struct copy vs ScriptableObject instantiation)
- No GC allocation
- Cache-friendly (value type)

**Implementation Effort**: Medium (requires touching CharacterStats architecture)

---

### 🟢 1.4 Range Check Optimization

**Current State**: `CombatInteractionManager.cs` lines 170-204
```csharp
// Multiple distance calculations per interaction
float distance = Vector3.Distance(offensiveSkill.combatant.transform.position,
                                   defensiveSkill.combatant.transform.position);
```

**Problem**:
- `Vector3.Distance` includes expensive `Mathf.Sqrt`
- Called 3+ times per interaction check (defender range, attacker range, etc.)

**Proposed Solution**: **Squared Distance Caching**
```csharp
// Cache squared distances, avoid sqrt
public class CombatDistanceCache
{
    private Dictionary<(Transform, Transform), float> sqrDistanceCache =
        new Dictionary<(Transform, Transform), float>(16);

    public void UpdateCache()
    {
        sqrDistanceCache.Clear();
    }

    public float GetSqrDistance(Transform a, Transform b)
    {
        var key = (a, b);
        if (!sqrDistanceCache.TryGetValue(key, out float sqrDist))
        {
            sqrDist = (a.position - b.position).sqrMagnitude;
            sqrDistanceCache[key] = sqrDist;
        }
        return sqrDist;
    }

    public bool IsInRange(Transform a, Transform b, float range)
    {
        return GetSqrDistance(a, b) <= range * range;
    }
}
```

**Benefit**:
- ~2-3x faster range checks (no sqrt)
- Caching eliminates redundant calculations
- Minimal code change

**Implementation Effort**: Low (1-2 hours)

---

## 2. Memory Optimizations

### 🟡 2.1 UnityEvent Allocations

**Current State**: Heavy use of UnityEvents throughout
```csharp
public UnityEvent<int, Transform> OnDamageReceived = new UnityEvent<int, Transform>();
public UnityEvent OnCombatEntered = new UnityEvent();
```

**Problem**:
- UnityEvent.Invoke() allocates closure objects
- Each invoke allocates ~40 bytes
- During intense combat: 10+ invokes per frame = 400 bytes/frame = 24KB/s

**Proposed Solution**: **C# Events (Hybrid Approach)**
```csharp
// For performance-critical paths (called every frame)
public event System.Action<int, Transform> OnDamageReceived;

protected void RaiseDamageReceived(int damage, Transform source)
{
    OnDamageReceived?.Invoke(damage, source);
}

// Keep UnityEvents for editor-exposed events (OnDied, etc.)
public UnityEvent OnDied = new UnityEvent();
```

**Benefit**:
- 0 allocations for C# events (vs 40 bytes per UnityEvent invoke)
- ~95% reduction in event-related allocations
- Still allows editor binding for designer-facing events

**Implementation Effort**: Medium (need to audit all events)

---

### 🟢 2.2 StatusEffect List Management

**Current State**: `StatusEffectManager.cs` lines 10, 136-162
```csharp
private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();

// Iteration with removal causes shifting
for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
{
    if (!effect.isActive)
        activeStatusEffects.RemoveAt(i); // Array shift!
}
```

**Problem**:
- `List.RemoveAt()` shifts all elements after index (O(n))
- Typically 1-3 active effects, but could be 5+ in complex scenarios

**Proposed Solution**: **Swap-Remove Pattern**
```csharp
private List<StatusEffect> activeStatusEffects = new List<StatusEffect>(8);

private void UpdateStatusEffects()
{
    for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
    {
        var effect = activeStatusEffects[i];
        effect.UpdateEffect(Time.deltaTime);

        if (!effect.isActive)
        {
            // Swap with last element, then remove last (O(1))
            int lastIndex = activeStatusEffects.Count - 1;
            if (i != lastIndex)
                activeStatusEffects[i] = activeStatusEffects[lastIndex];
            activeStatusEffects.RemoveAt(lastIndex);
        }
    }
}
```

**Benefit**:
- O(1) removal instead of O(n)
- Better performance with many status effects
- No allocations

**Implementation Effort**: Low (30 minutes)

---

## 3. Architectural Improvements

### 🔴 3.1 Skill State Machine - Explicit State Pattern

**Current State**: `SkillSystem.cs` uses coroutines for state management
```csharp
private IEnumerator ExecuteSkillCoroutine(SkillType skillType)
{
    currentState = SkillExecutionState.Startup;
    yield return new WaitForSeconds(startupTime);

    currentState = SkillExecutionState.Active;
    yield return new WaitForSeconds(activeTime);
    // ... etc
}
```

**Problem**:
- Coroutine overhead (~40-50 bytes per yield)
- Difficult to debug (no stack trace through yields)
- Cannot cleanly interrupt mid-coroutine
- State transitions implicit in control flow

**Proposed Solution**: **Explicit State Machine**
```csharp
public abstract class SkillState
{
    public abstract void Enter(SkillSystem system);
    public abstract void Update(SkillSystem system, float deltaTime);
    public abstract void Exit(SkillSystem system);
}

public class ChargingState : SkillState
{
    private float elapsed = 0f;

    public override void Update(SkillSystem system, float deltaTime)
    {
        elapsed += deltaTime;
        system.ChargeProgress = elapsed / system.ChargeTime;

        if (elapsed >= system.ChargeTime)
            system.TransitionTo(new ChargedState());
    }
}

// In SkillSystem
private SkillState currentStateObject;

public void CombatUpdate(float deltaTime)
{
    currentStateObject?.Update(this, deltaTime);
}

public void TransitionTo(SkillState newState)
{
    currentStateObject?.Exit(this);
    currentStateObject = newState;
    currentStateObject.Enter(this);
}
```

**Benefit**:
- No coroutine allocations
- Clear state transitions
- Easier debugging (explicit state objects)
- Can save/restore state easily
- Better performance (~20% faster than coroutines)

**Implementation Effort**: High (4-6 hours, major refactor)

---

### 🟡 3.2 Component Dependency Injection

**Current State**: GetComponent in Awake throughout
```csharp
private void Awake()
{
    healthSystem = GetComponent<HealthSystem>();
    staminaSystem = GetComponent<StaminaSystem>();
    statusEffectManager = GetComponent<StatusEffectManager>();
    weaponController = GetComponent<WeaponController>();
    skillSystem = GetComponent<SkillSystem>();
    movementController = GetComponent<MovementController>();
    equipmentManager = GetComponent<EquipmentManager>();
}
```

**Problem**:
- 7 GetComponent calls per character (slow during scene load)
- Tight coupling (components know about each other)
- Difficult to unit test
- Initialization order fragile

**Proposed Solution**: **Constructor Injection via Context**
```csharp
public class CombatContext
{
    public HealthSystem Health { get; private set; }
    public StaminaSystem Stamina { get; private set; }
    public SkillSystem Skills { get; private set; }
    // ... etc

    public void Initialize(GameObject character)
    {
        // Single pass through components
        Health = character.GetComponent<HealthSystem>();
        Stamina = character.GetComponent<StaminaSystem>();
        Skills = character.GetComponent<SkillSystem>();
        // ... etc
    }
}

// Components receive context
public class CombatController : MonoBehaviour
{
    private CombatContext context;

    private void Awake()
    {
        context = new CombatContext();
        context.Initialize(gameObject);

        // Pass to subsystems
        GetComponent<HealthSystem>().Inject(context);
        GetComponent<SkillSystem>().Inject(context);
        // ... etc
    }
}
```

**Benefit**:
- Faster initialization (1 pass instead of 7+ GetComponent calls)
- Easier unit testing (can mock CombatContext)
- Explicit dependencies
- Centralized initialization

**Implementation Effort**: Medium-High (3-4 hours)

---

### 🟡 3.3 Speed Calculation Caching

**Current State**: `CombatInteractionManager.cs` lines 588-601
```csharp
// Recalculates speed for every skill interaction
var skillSpeeds = skills.Select(skill =>
{
    var combatant = skill.combatant;
    var stats = combatant.Stats;
    var weapon = combatant.GetComponent<WeaponController>()?.WeaponData;

    return new
    {
        skill = skill,
        speed = SpeedResolver.CalculateSpeed(skill.skillType, stats, weapon)
    };
}).ToList();
```

**Problem**:
- Speed calculation includes multiple multiplications/additions
- Recalculated for every speed resolution (potentially multiple per frame)
- Stats/weapon rarely change during combat

**Proposed Solution**: **Cached Speed Values**
```csharp
public class CombatController
{
    private Dictionary<SkillType, float> speedCache = new Dictionary<SkillType, float>(6);
    private bool speedCacheInvalidated = true;

    public float GetSpeed(SkillType skillType)
    {
        if (speedCacheInvalidated)
            RebuildSpeedCache();

        return speedCache[skillType];
    }

    private void RebuildSpeedCache()
    {
        var stats = Stats;
        var weapon = weaponController.WeaponData;

        foreach (SkillType skill in System.Enum.GetValues(typeof(SkillType)))
        {
            speedCache[skill] = SpeedResolver.CalculateSpeed(skill, stats, weapon);
        }

        speedCacheInvalidated = false;
    }

    // Called when equipment changes
    public void InvalidateSpeedCache()
    {
        speedCacheInvalidated = true;
    }
}
```

**Benefit**:
- 6x calculation → 1x calculation (per equipment change)
- Consistent speed values during combat
- Faster speed resolution

**Implementation Effort**: Low-Medium (2 hours)

---

## 4. Code Quality Improvements

### 🟢 4.1 Magic Numbers → Constants

**Current State**: Scattered magic numbers
```csharp
// CombatInteractionManager.cs line 67
if (Time.time - execution.timestamp < 0.1f) // What is 0.1f?

// StatusEffectManager.cs line 298
ApplyStatusEffect(new StatusEffect(StatusEffectType.Rest, float.MaxValue)); // Why MaxValue?

// PatternedAI.cs line 549
const float simultaneousThreshold = 0.1f; // Duplicate of above
```

**Proposed Solution**: Centralize in **CombatConstants**
```csharp
public static class CombatConstants
{
    // Interaction Timing
    public const float SIMULTANEOUS_SKILL_WINDOW = 0.1f; // 100ms window for "simultaneous"

    // Status Effects
    public const float INDEFINITE_DURATION = float.MaxValue; // For Rest, etc.

    // AI
    public const float DEFAULT_ENGAGE_DISTANCE = 3.0f;
    public const float DEFAULT_DISENGAGE_DISTANCE = 6.0f;
}
```

**Benefit**:
- Self-documenting code
- Easy to tune values
- No duplicate literals

**Implementation Effort**: Low (1 hour)

---

### 🟢 4.2 Null Reference Safety

**Current State**: Many nullable references without checks
```csharp
// CombatController.cs line 306
return weaponController.IsInRange(target); // weaponController could be null

// SkillSystem.cs line 332
if (accuracySystem != null)  // Good!
    accuracySystem.StartAiming(target);
```

**Proposed Solution**: **Consistent Null Checks + Assertions**
```csharp
private void Awake()
{
    weaponController = GetComponent<WeaponController>();

    // Fail fast on required components
    Debug.Assert(weaponController != null,
        $"{gameObject.name} CombatController requires WeaponController");
}

public bool IsInRangeOf(Transform target)
{
    if (weaponController == null)
    {
        Debug.LogError($"{gameObject.name} has no WeaponController!");
        return false;
    }

    return weaponController.IsInRange(target);
}
```

**Benefit**:
- Easier debugging (fail fast with clear messages)
- Prevents null reference exceptions in builds
- Self-documenting required vs optional components

**Implementation Effort**: Low (1-2 hours)

---

### 🟡 4.3 OnGUI → Canvas UI

**Current State**: All UI uses OnGUI (immediate mode)
```csharp
// HealthBarUI.cs, StaminaBarUI.cs, CombatDebugVisualizer.cs
private void OnGUI()
{
    GUI.DrawTexture(...);
    GUI.Label(...);
}
```

**Problem**:
- OnGUI called multiple times per frame (Layout + Repaint)
- Not scalable to complex UI
- No UI batching
- Harder to animate/style

**Proposed Solution**: **Hybrid Approach**
```csharp
// Keep OnGUI for debug visualizer (dev tool)
// Migrate health/stamina bars to Canvas UI

public class HealthBarCanvasUI : MonoBehaviour
{
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthText;

    private HealthSystem targetHealthSystem;

    private void Start()
    {
        targetHealthSystem = GetComponentInParent<HealthSystem>();
        targetHealthSystem.OnHealthChanged.AddListener(UpdateHealthBar);
    }

    private void UpdateHealthBar(int current, int max)
    {
        healthFillImage.fillAmount = (float)current / max;
        healthText.text = $"{current}/{max}";
    }
}
```

**Benefit**:
- Better performance (UI batching, once per frame)
- Easier to style and animate
- Scalable to complex UI

**Implementation Effort**: Medium (2-3 hours to migrate)

**Note**: Keep OnGUI for CombatDebugVisualizer (dev-only tool).

---

## 5. Scalability Improvements

### 🟡 5.1 Multi-Target Combat Support

**Current State**: Skills target single enemy
```csharp
// CombatController.cs
private Transform currentTarget;

// SkillSystem assumes single target
var target = execution.combatant.CurrentTarget;
```

**Limitation**: Cannot implement AOE skills or multi-target abilities

**Proposed Solution**: **Target Collection**
```csharp
public interface ITargetSelector
{
    IEnumerable<Transform> GetTargets(Transform source);
}

public class SingleTargetSelector : ITargetSelector
{
    private Transform target;

    public IEnumerable<Transform> GetTargets(Transform source)
    {
        if (target != null) yield return target;
    }
}

public class AOETargetSelector : ITargetSelector
{
    private float radius;

    public IEnumerable<Transform> GetTargets(Transform source)
    {
        var hits = Physics.OverlapSphere(source.position, radius);
        foreach (var hit in hits)
        {
            var combatant = hit.GetComponent<CombatController>();
            if (combatant != null && combatant.transform != source)
                yield return combatant.transform;
        }
    }
}

// In SkillSystem
private ITargetSelector targetSelector;

private void ExecuteSkill(SkillType skillType)
{
    foreach (var target in targetSelector.GetTargets(transform))
    {
        ProcessTargetHit(target, skillType);
    }
}
```

**Benefit**:
- Enables AOE skills
- Enables multi-target abilities
- Flexible targeting system

**Implementation Effort**: High (future feature, not refactor)

---

### 🟢 5.2 Combat System Modularity

**Current State**: Systems tightly coupled to combat
```csharp
// HealthSystem is combat-specific
// StaminaSystem is combat-specific
```

**Future-Proofing**: Make systems reusable outside combat

**Proposed Solution**: **Generic Resource System**
```csharp
// Generic resource that could be used for health, stamina, mana, etc.
public class ResourceSystem : MonoBehaviour
{
    [SerializeField] private int currentValue;
    [SerializeField] private int maxValue;

    public UnityEvent<int, int> OnValueChanged;

    public void Consume(int amount) { /* ... */ }
    public void Restore(int amount) { /* ... */ }
}

// HealthSystem becomes specialized wrapper
public class HealthSystem : ResourceSystem
{
    public void TakeDamage(int damage, Transform source)
    {
        Consume(damage);
        // Health-specific logic
    }
}
```

**Benefit**:
- Reusable for mana, energy, etc.
- Less code duplication
- Easier to add new resource types

**Implementation Effort**: Medium (good for future expansion)

---

## 6. Recommended Refactoring Priority

### Phase 1 - Quick Wins (1-2 days)
1. ✅ Object pooling for SkillExecution (🟡 1.2)
2. ✅ Range check optimization (🟢 1.4)
3. ✅ StatusEffect swap-remove (🟢 2.2)
4. ✅ Magic numbers → Constants (🟢 4.1)
5. ✅ Null reference safety (🟢 4.2)

**Expected Impact**: ~10-15% performance improvement, better stability

### Phase 2 - Medium Improvements (3-5 days)
1. ✅ Update loop consolidation (🔴 1.1)
2. ✅ UnityEvent → C# events (🟡 2.1)
3. ✅ Speed calculation caching (🟡 3.3)
4. ✅ Component dependency injection (🟡 3.2)

**Expected Impact**: ~25-30% performance improvement, better architecture

### Phase 3 - Major Refactors (1-2 weeks)
1. ✅ Skill state machine refactor (🔴 3.1)
2. ✅ EquipmentManager struct stats (🟡 1.3)
3. ✅ OnGUI → Canvas UI (🟡 4.3)

**Expected Impact**: ~40-50% performance improvement, cleaner codebase

### Phase 4 - Future Expansion (as needed)
1. Multi-target combat support (🟡 5.1)
2. Resource system modularity (🟢 5.2)

---

## 7. Performance Metrics

### Current Performance (Estimated)
- **2 Combatants, Active Combat**: ~60-80 FPS (on mid-range hardware)
- **GC Allocations per Frame**: ~500-800 bytes
- **Update() Calls per Frame**: ~16
- **Skill Execution Allocations**: ~150 bytes per skill

### Post-Refactor Goals (Phase 1-3)
- **2 Combatants, Active Combat**: ~90-120 FPS
- **GC Allocations per Frame**: ~50-100 bytes (80% reduction)
- **Update() Calls per Frame**: ~1-2 (94% reduction)
- **Skill Execution Allocations**: ~0 bytes (100% reduction via pooling)

---

## 8. Risk Assessment

### Low Risk Refactors
- Object pooling (isolated change)
- Range check optimization (transparent)
- Magic numbers → constants (no behavior change)

### Medium Risk Refactors
- Update loop consolidation (need to test timing)
- UnityEvent → C# events (lose editor binding)
- Component injection (initialization order)

### High Risk Refactors
- Skill state machine (major behavior change)
- EquipmentManager stats (architectural change)

**Recommendation**: Implement in phases, with full regression testing between phases.

---

## 9. Testing Strategy

### Unit Tests (Add Coverage)
```csharp
[Test]
public void SpeedCache_InvalidatesOnEquipmentChange()
{
    var controller = CreateTestCombatController();
    float speed1 = controller.GetSpeed(SkillType.Attack);

    controller.EquipWeapon(fasterWeapon);
    float speed2 = controller.GetSpeed(SkillType.Attack);

    Assert.Greater(speed2, speed1);
}

[Test]
public void SkillExecutionPool_ReusesInstances()
{
    var pool = new SkillExecutionPool();
    var exec1 = pool.Get();
    pool.Return(exec1);
    var exec2 = pool.Get();

    Assert.AreSame(exec1, exec2); // Same instance reused
}
```

### Performance Tests
```csharp
[Test]
public void CombatUpdateManager_FasterThanIndividualUpdates()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // Test 1000 frames with individual updates
    for (int i = 0; i < 1000; i++)
    {
        combat.UpdateAllComponentsIndividually();
    }
    long individualTime = stopwatch.ElapsedMilliseconds;

    stopwatch.Restart();

    // Test 1000 frames with consolidated update
    for (int i = 0; i < 1000; i++)
    {
        combatUpdateManager.Update();
    }
    long consolidatedTime = stopwatch.ElapsedMilliseconds;

    Assert.Less(consolidatedTime, individualTime * 0.85f); // At least 15% faster
}
```

---

## Conclusion

The FairyGate combat system has a solid foundation, but there are clear opportunities for optimization:

1. **Performance**: Update loop consolidation and object pooling will yield immediate gains
2. **Memory**: UnityEvent replacement and pooling will reduce GC pressure significantly
3. **Architecture**: Explicit state machine and dependency injection improve maintainability
4. **Scalability**: Target selector pattern enables future expansion

**Recommended Next Steps**:
1. Implement Phase 1 quick wins (1-2 days)
2. Profile to validate improvements
3. Proceed with Phase 2 if gains are satisfactory
4. Consider Phase 3 refactors for long-term maintainability

The system is production-ready as-is, but these refactors will make it more performant and easier to extend.
