# Combat Interaction Manager Refactor Proposal

## Summary
The CombatInteractionManager has several areas of code redundancy that can be consolidated for better maintainability.

## Identified Redundancies

### 1. RangedAttack Hit Checking (3 duplicates)
**Current State**: `attacker.skillSystem.LastRangedAttackHit` is checked in 3 separate locations
- Line 329: CounterIneffective case
- Line 378: DefenderBlocks case
- Line 452: ExecuteOffensiveSkillDirectly

**Proposal**: Create helper method
```csharp
private bool DidRangedAttackHit(SkillExecution execution)
{
    return execution.skillType == SkillType.RangedAttack &&
           execution.skillSystem.LastRangedAttackHit;
}
```

### 2. RangedAttack Debug Logging (5+ duplicates)
**Current State**: RangedAttack-specific debug logs scattered throughout:
- Lines 90-92, 138-145, 152-155, 186-189, 197-209

**Proposal**: Create debug helper methods
```csharp
private void LogRangedAttackDebug(string context, string message)
{
    if (enableDebugLogs)
    {
        Debug.Log($"[RangedAttack Debug] {context}: {message}");
    }
}

private void LogRangedAttackInteraction(SkillExecution offensive, SkillExecution defensive, string result)
{
    if (enableDebugLogs)
    {
        Debug.Log($"[RangedAttack Debug] {offensive.combatant.name} RangedAttack vs " +
                  $"{defensive.combatant.name} {defensive.skillType} → {result}");
    }
}
```

### 3. Knockback Calculation (4 duplicates)
**Current State**: Knockback direction/displacement calculation repeated 4 times:
- Counter reflection (313-315)
- Smash vs Defense (360-362)
- Windmill vs Counter (416-418)
- Direct hit (483-487)

**Proposal**: Create helper method
```csharp
private Vector3 CalculateKnockbackDisplacement(Transform attacker, Transform defender, float distance)
{
    Vector3 direction = (defender.position - attacker.position).normalized;
    return direction * distance;
}

// Usage examples:
// Counter knockback
Vector3 counterDisplacement = CalculateKnockbackDisplacement(
    attacker.combatant.transform,
    defender.combatant.transform,
    CombatConstants.COUNTER_KNOCKBACK_DISTANCE
);

// Smash knockback
Vector3 smashDisplacement = CalculateKnockbackDisplacement(
    attacker.combatant.transform,
    defender.combatant.transform,
    CombatConstants.SMASH_KNOCKBACK_DISTANCE
);
```

### 4. Complete Defensive Skill Calls (8 duplicates)
**Current State**: `CompleteDefensiveSkillExecution(defender)` called 8 times
- This is actually acceptable since each call is in a different interaction case
- However, we could consolidate some cases

**Proposal**: Group similar interaction results
```csharp
private void ProcessInteractionEffects(...)
{
    // ... existing code ...

    // Determine if defensive skill should be completed
    bool shouldCompleteDefensive = interaction switch
    {
        InteractionResult.AttackerStunned => true,
        InteractionResult.CounterReflection => true,
        InteractionResult.CounterIneffective => attacker.skillSystem.LastRangedAttackHit, // Only if hit
        InteractionResult.DefenderKnockedDown => true,
        InteractionResult.DefenderBlocks => ShouldCompleteDefenseBlock(attacker, defender),
        InteractionResult.WindmillBreaksCounter => true,
        _ => false
    };

    // Apply specific effects for each interaction
    ApplyInteractionDamageAndEffects(interaction, attacker, defender, ...);

    // Complete defensive skill if needed
    if (shouldCompleteDefensive)
    {
        CompleteDefensiveSkillExecution(defender);
    }
}

private bool ShouldCompleteDefenseBlock(SkillExecution attacker, SkillExecution defender)
{
    // RangedAttack vs Defense: only complete if hit
    if (attacker.skillType == SkillType.RangedAttack)
    {
        return attacker.skillSystem.LastRangedAttackHit;
    }
    // Other skills: always complete
    return true;
}
```

### 5. Range Check Logic (2 duplicates)
**Current State**: Range checking appears twice in CanDefensiveSkillRespond
- Initial defender weapon range check (178-181)
- Attacker weapon range check (194-204)

**Proposal**: Extract to helper method
```csharp
private bool IsValidInteractionRange(SkillExecution attacker, SkillExecution defender)
{
    bool isRangedAttack = attacker.skillType == SkillType.RangedAttack;

    // For ranged attacks, defender doesn't need weapon range to block
    // (they're defending against a projectile, not attacking back)
    if (!isRangedAttack)
    {
        var defenderWeapon = defender.combatant.GetComponent<WeaponController>();
        if (!defenderWeapon.IsInRange(attacker.combatant.transform))
        {
            return false;
        }
    }

    // Attacker must be in range to hit the defender
    var attackerWeapon = attacker.combatant.GetComponent<WeaponController>();
    if (!attackerWeapon.IsInRange(defender.combatant.transform))
    {
        LogRangeCheckFailure(attacker, defender, attackerWeapon);
        return false;
    }

    return true;
}

private void LogRangeCheckFailure(SkillExecution attacker, SkillExecution defender, WeaponController attackerWeapon)
{
    if (enableDebugLogs && attacker.skillType == SkillType.RangedAttack)
    {
        float distance = Vector3.Distance(attacker.combatant.transform.position, defender.combatant.transform.position);
        float attackerRange = attackerWeapon?.WeaponData?.range ?? 0f;
        Debug.Log($"[RangedAttack Debug] Attacker {attacker.combatant.name} OUT OF RANGE: distance={distance:F1}, range={attackerRange:F1}");
    }
}
```

## Additional Improvements

### 6. Extract Damage Application Logic
The damage calculation and application code in `ProcessInteractionEffects` could be extracted:

```csharp
private void ApplyDamageToTarget(HealthSystem targetHealth, int damage, Transform source)
{
    targetHealth.TakeDamage(damage, source);
}

private void ApplyKnockdownWithDisplacement(StatusEffectManager statusEffects, Vector3 displacement)
{
    statusEffects.ApplyInteractionKnockdown(displacement);
}
```

### 7. Consolidate Status Effect Application
Group common status effect patterns:

```csharp
private void ApplyOffensiveSkillEffects(SkillType skillType, SkillExecution attacker, Transform target, int damage)
{
    var targetStatusEffects = target.GetComponent<StatusEffectManager>();
    var targetKnockdownMeter = target.GetComponent<KnockdownMeterTracker>();
    var attackerWeapon = attacker.combatant.GetComponent<WeaponController>()?.WeaponData;

    switch (skillType)
    {
        case SkillType.Attack:
            targetStatusEffects.ApplyStun(attackerWeapon.stunDuration);
            targetKnockdownMeter.AddMeterBuildup(damage, attacker.combatant.Stats, attacker.combatant.transform);
            break;

        case SkillType.Smash:
        case SkillType.Windmill:
            Vector3 displacement = CalculateKnockbackDisplacement(
                attacker.combatant.transform,
                target,
                skillType == SkillType.Smash ?
                    CombatConstants.SMASH_KNOCKBACK_DISTANCE :
                    CombatConstants.WINDMILL_KNOCKBACK_DISTANCE
            );
            targetKnockdownMeter.TriggerImmediateKnockdown(displacement);
            break;
    }
}
```

## Benefits of Refactoring

1. **Maintainability**: Changes to knockback logic only need to be made in one place
2. **Readability**: Helper methods with clear names make the code easier to understand
3. **Debugging**: Centralized logging makes it easier to track issues
4. **Testing**: Smaller methods are easier to unit test
5. **Consistency**: Ensures knockback, damage, and effects are always calculated the same way

## Risks

1. **Breaking Changes**: Any bugs in the refactored code will affect multiple interaction types
2. **Testing Burden**: All 36 interaction combinations should be re-tested after refactoring
3. **Performance**: Additional method calls (negligible impact, but worth noting)

## Recommendation

**Priority 1 (High Value, Low Risk)**:
- Knockback calculation helper (used 4 times)
- RangedAttack hit checking helper (used 3 times)
- Range check extraction (cleaner logic)

**Priority 2 (Medium Value, Medium Risk)**:
- Debug logging consolidation (cleaner code, but many call sites)
- Status effect application grouping (better organization)

**Priority 3 (Low Priority)**:
- Complete defensive skill consolidation (current approach is already clear)
- Damage application helpers (minimal benefit)

## Implementation Strategy

If you want to proceed with refactoring:
1. Start with Priority 1 changes (highest value, lowest risk)
2. Test thoroughly using the TestRepeaterAI environment (F1-F6 hotkeys)
3. Verify all 36 interaction combinations still work correctly
4. Move to Priority 2 changes if desired

Would you like me to implement any of these refactoring changes?
