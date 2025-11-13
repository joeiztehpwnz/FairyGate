# Weapon-Skill Modifier & Dual-Weapon System Design

**Status:** Design Phase - Not Yet Implemented
**Date:** 2025-10-29
**Context:** Addresses issue where all skills use weapon range, causing absurd scenarios (Archer Windmill from 7.5 units)

---

## Design Goals

1. **Weapon Identity**: Each weapon type should feel unique across all skills, not just damage/speed
2. **No Hard Restrictions**: All weapons can use all skills, but effectiveness varies dramatically
3. **Tactical Depth**: Players choose weapons based on preferred skill usage and combat situation
4. **AI Personality**: Enemy archetypes favor weapons that match their combat style
5. **Dual-Wielding**: Support primary + secondary weapon with manual swapping

---

## User Design Decisions

- **Swap Mechanic**: Manual hotkey swap (traditional weapon switching)
- **Skill Restrictions**: No restrictions (modifiers only)
- **Use Cases**: Support all playstyles (ranged/melee hybrid, specialized swapping, situational adaptation)
- **AI Behavior**: Weapon preference personality (each archetype favors one weapon)
- **Testing Note**: May want to test system with no range values first

---

## Phase 1: Weapon-Skill Modifier Data Structure

### 1.1 Create Modifier Data Structure

Add to `WeaponData.cs`:

```csharp
[System.Serializable]
public class WeaponSkillModifier
{
    public SkillType skillType;
    public float rangeMultiplier = 1.0f;        // How this weapon affects skill range
    public float damageMultiplier = 1.0f;       // Damage modifier for this skill
    public float speedMultiplier = 1.0f;        // Execution speed modifier
    public float stunMultiplier = 1.0f;         // Stun/knockdown modifier
    public float staminaCostMultiplier = 1.0f;  // Stamina efficiency
    public float aiWeightMultiplier = 1.0f;     // AI preference weight for this skill
}

[Header("Skill-Specific Modifiers")]
public WeaponSkillModifier[] skillModifiers;

public WeaponSkillModifier GetModifier(SkillType skill)
{
    var modifier = Array.Find(skillModifiers, m => m.skillType == skill);
    return modifier ?? new WeaponSkillModifier { skillType = skill }; // Default 1.0× fallback
}
```

### 1.2 First-Pass Modifier Values

#### Spear (Thrust master, reach advantage)
- **Attack**: 1.2× range, 1.0× damage - "Thrust attacks leverage reach"
- **Smash**: 0.8× range, 0.8× damage, 0.9× speed - "Overhead swings lose reach advantage"
- **Windmill**: 0.7× range, 0.9× damage, 0.8× speed - "Spinning with long spear is awkward"
- **Lunge**: 1.3× range, 1.1× damage - "Perfect for charging thrust"
- **Counter**: 1.2× range, 1.0× damage - "Excellent for defensive thrust"
- **RangedAttack**: N/A (can't use)
- **AI Weights**: 1.5× Attack, 1.5× Lunge, 1.3× Counter, 0.7× Windmill, 0.8× Smash

#### Sword (Balanced baseline)
- **All Skills**: 1.0× modifiers (reference standard)
- **AI Weights**: 1.0× all skills (no preference)

#### Dagger (Speed demon, counter specialist)
- **Attack**: 0.8× range, 0.9× damage, 1.2× speed - "Quick close-range strikes"
- **Smash**: 0.7× range, 0.7× damage, 1.1× speed - "Lacks guard-break power"
- **Windmill**: 0.6× range, 0.8× damage, 1.3× speed - "Lightning-fast spin"
- **Lunge**: 0.9× range, 0.9× damage, 1.2× speed - "Quick dash attack"
- **Counter**: 0.9× range, 1.0× damage, 1.3× speed - "Perfect for counters"
- **RangedAttack**: N/A
- **AI Weights**: 1.5× Counter, 1.3× Windmill, 0.8× Smash

#### Mace (Guard breaker, raw power)
- **Attack**: 0.9× range, 1.1× damage, 0.9× speed - "Heavy but powerful swings"
- **Smash**: 1.0× range, 1.3× damage, 0.8× speed, 1.2× stun - "Designed for breaking guards"
- **Windmill**: 0.8× range, 1.2× damage, 0.7× speed - "Devastating but slow spin"
- **Lunge**: 0.7× range, 1.0× damage, 0.8× speed - "Not ideal for charging"
- **Counter**: 0.9× range, 1.1× damage, 0.9× speed - "Heavy counter-strike"
- **RangedAttack**: N/A
- **AI Weights**: 1.5× Smash, 1.2× Attack, 0.7× Lunge

#### Bow (Ranged primary, melee emergency)
- **Attack**: 0.4× range, 0.6× damage - "Emergency melee with bow"
- **Smash**: 0.4× range, 0.5× damage, 0.9× speed - "Not designed for this"
- **Windmill**: 0.4× range, 0.6× damage - "Defensive spin"
- **Counter**: 0.5× range, 0.7× damage - "Better defensive option"
- **Lunge**: 0.4× range, 0.5× damage - "Desperate charge"
- **RangedAttack**: 1.0× range, 1.0× damage - "Primary attack"
- **AI Weights**: 2.0× RangedAttack, 0.5× all melee skills

**Range Results with 0.4× melee multiplier:**
- Bow base range: 7.5 units × 1.25 multiplier = 9.375 units
- Bow Windmill: 9.375 × 0.4 = 3.75 units (reasonable melee range)
- Bow RangedAttack: 9.375 × 1.0 = 9.375 units (proper ranged)

### 1.3 Update Weapon Factory Methods

Modify each weapon factory method in `WeaponData.cs`:

```csharp
public static WeaponData CreateSpearData()
{
    var data = new WeaponData
    {
        weaponName = "Spear",
        weaponType = WeaponType.Spear,
        range = 3.0f,
        baseDamage = 15,
        speed = 1.0f,
        stunDuration = 1.0f,
        executionSpeedModifier = 0.0f,
        speedResolutionModifier = 0.0f,
        skillModifiers = new WeaponSkillModifier[]
        {
            new() { skillType = SkillType.Attack, rangeMultiplier = 1.2f, damageMultiplier = 1.0f, aiWeightMultiplier = 1.5f },
            new() { skillType = SkillType.Smash, rangeMultiplier = 0.8f, damageMultiplier = 0.8f, speedMultiplier = 0.9f, aiWeightMultiplier = 0.8f },
            new() { skillType = SkillType.Windmill, rangeMultiplier = 0.7f, damageMultiplier = 0.9f, speedMultiplier = 0.8f, aiWeightMultiplier = 0.7f },
            new() { skillType = SkillType.Lunge, rangeMultiplier = 1.3f, damageMultiplier = 1.1f, aiWeightMultiplier = 1.5f },
            new() { skillType = SkillType.Counter, rangeMultiplier = 1.2f, damageMultiplier = 1.0f, aiWeightMultiplier = 1.3f },
            new() { skillType = SkillType.Defense, rangeMultiplier = 1.0f, damageMultiplier = 1.0f }
        }
    };
    return data;
}

// Repeat for CreateSwordData(), CreateDaggerData(), CreateMaceData(), CreateBowData()
```

---

## Phase 2: Integrate Modifiers Into Combat Systems

### 2.1 Damage System

**File:** `Assets/Scripts/Combat/Stats/DamageCalculator.cs`

**Current (Line 7):**
```csharp
public static int CalculateBaseDamage(
    CharacterStats attackerStats,
    WeaponData weapon,
    CharacterStats defenderStats)
{
    int baseDamage = weapon.baseDamage + attackerStats.strength - defenderStats.physicalDefense;
    return Mathf.Max(baseDamage, CombatConstants.MINIMUM_DAMAGE);
}
```

**Updated:**
```csharp
public static int CalculateBaseDamage(
    CharacterStats attackerStats,
    WeaponData weapon,
    CharacterStats defenderStats,
    SkillType skillType)  // NEW PARAMETER
{
    float damageMultiplier = weapon.GetModifier(skillType).damageMultiplier;
    int weaponDamage = Mathf.RoundToInt(weapon.baseDamage * damageMultiplier);
    int baseDamage = weaponDamage + attackerStats.strength - defenderStats.physicalDefense;
    return Mathf.Max(baseDamage, CombatConstants.MINIMUM_DAMAGE);
}
```

**Update call site in `CombatInteractionManager.cs` (Line 575):**
```csharp
int damage = DamageCalculator.CalculateBaseDamage(
    attackerStats,
    attackerWeapon,
    targetStats,
    execution.skillType);  // Pass skill type
```

### 2.2 Speed System

**File:** `Assets/Scripts/Combat/Stats/SpeedResolver.cs`

**Current (Line 7):**
```csharp
public static float CalculateSpeed(SkillType skillType, CharacterStats stats, WeaponData weapon)
{
    float baseSpeed = weapon.speed + (stats.dexterity / CombatConstants.DEXTERITY_SPEED_DIVISOR);
    float modifiedSpeed = baseSpeed * (1 + weapon.speedResolutionModifier);
    return modifiedSpeed;
}
```

**Updated:**
```csharp
public static float CalculateSpeed(SkillType skillType, CharacterStats stats, WeaponData weapon)
{
    float baseSpeed = weapon.speed + (stats.dexterity / CombatConstants.DEXTERITY_SPEED_DIVISOR);
    float weaponSpeedMod = weapon.GetModifier(skillType).speedMultiplier;
    float modifiedSpeed = baseSpeed * (1 + weapon.speedResolutionModifier) * weaponSpeedMod;
    return modifiedSpeed;
}
```

### 2.3 Stamina System

**File:** `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Current (Line 837):**
```csharp
private int GetSkillStaminaCost(SkillType skillType)
{
    return skillType switch
    {
        SkillType.Attack => CombatConstants.ATTACK_STAMINA_COST,
        SkillType.Smash => CombatConstants.SMASH_STAMINA_COST,
        SkillType.Defense => CombatConstants.DEFENSE_STAMINA_COST,
        SkillType.Counter => CombatConstants.COUNTER_STAMINA_COST,
        SkillType.Windmill => CombatConstants.WINDMILL_STAMINA_COST,
        SkillType.Lunge => CombatConstants.LUNGE_STAMINA_COST,
        SkillType.RangedAttack => CombatConstants.RANGED_ATTACK_STAMINA_COST,
        _ => 0
    };
}
```

**Updated:**
```csharp
private int GetSkillStaminaCost(SkillType skillType)
{
    int baseCost = skillType switch
    {
        SkillType.Attack => CombatConstants.ATTACK_STAMINA_COST,
        SkillType.Smash => CombatConstants.SMASH_STAMINA_COST,
        SkillType.Defense => CombatConstants.DEFENSE_STAMINA_COST,
        SkillType.Counter => CombatConstants.COUNTER_STAMINA_COST,
        SkillType.Windmill => CombatConstants.WINDMILL_STAMINA_COST,
        SkillType.Lunge => CombatConstants.LUNGE_STAMINA_COST,
        SkillType.RangedAttack => CombatConstants.RANGED_ATTACK_STAMINA_COST,
        _ => 0
    };

    float staminaMultiplier = weaponController.WeaponData.GetModifier(skillType).staminaCostMultiplier;
    return Mathf.RoundToInt(baseCost * staminaMultiplier);
}
```

### 2.4 Stun System

**File:** `Assets/Scripts/Combat/Stats/DamageCalculator.cs`

**Current (Line 31):**
```csharp
public static float CalculateStunDuration(float baseStunDuration, CharacterStats targetStats)
{
    return baseStunDuration * (1 - targetStats.focus / CombatConstants.FOCUS_STUN_RESISTANCE_DIVISOR);
}
```

**Updated:**
```csharp
public static float CalculateStunDuration(
    WeaponData weapon,
    SkillType skillType,
    CharacterStats targetStats)
{
    float baseStunDuration = weapon.stunDuration;
    float stunMultiplier = weapon.GetModifier(skillType).stunMultiplier;
    float weaponStun = baseStunDuration * stunMultiplier;
    return weaponStun * (1 - targetStats.focus / CombatConstants.FOCUS_STUN_RESISTANCE_DIVISOR);
}
```

### 2.5 Range Check System

**File:** `Assets/Scripts/Combat/Weapons/WeaponController.cs`

**Add new method:**
```csharp
public float GetSkillRange(SkillType skillType)
{
    if (weaponData == null)
        return 1.0f;

    float baseRange = weaponData.range * CombatConstants.WEAPON_RANGE_MULTIPLIER;
    float rangeMultiplier = weaponData.GetModifier(skillType).rangeMultiplier;
    return baseRange * rangeMultiplier;
}
```

**Update existing method (Line 85):**
```csharp
public bool CheckRangeForSkill(Transform target, SkillType skillType)
{
    if (!SpeedResolver.IsOffensiveSkill(skillType))
        return true; // Defensive skills don't require range checks

    float skillRange = GetSkillRange(skillType);
    float sqrDistance = (transform.position - target.position).sqrMagnitude;
    return sqrDistance <= (skillRange * skillRange);
}
```

---

## Phase 3: AI Weapon-Based Skill Preferences

### 3.1 Update SimpleTestAI Skill Selection

**File:** `Assets/Scripts/Combat/AI/SimpleTestAI.cs`

**In `SelectRandomSkill()` method (after line 614):**

```csharp
// Apply weapon-based skill preferences
if (weaponController != null && weaponController.WeaponData != null)
{
    adjustedAttackWeight *= weaponController.WeaponData.GetModifier(SkillType.Attack).aiWeightMultiplier;
    adjustedDefenseWeight *= weaponController.WeaponData.GetModifier(SkillType.Defense).aiWeightMultiplier;
    adjustedCounterWeight *= weaponController.WeaponData.GetModifier(SkillType.Counter).aiWeightMultiplier;
    adjustedSmashWeight *= weaponController.WeaponData.GetModifier(SkillType.Smash).aiWeightMultiplier;
    adjustedWindmillWeight *= weaponController.WeaponData.GetModifier(SkillType.Windmill).aiWeightMultiplier;
    adjustedLungeWeight *= weaponController.WeaponData.GetModifier(SkillType.Lunge).aiWeightMultiplier;
    adjustedRangedAttackWeight *= weaponController.WeaponData.GetModifier(SkillType.RangedAttack).aiWeightMultiplier;
}
```

**Result:**
- Archer with Bow: RangedAttack gets 2.0× weight multiplier (prefers ranged)
- Berserker with Mace: Smash gets 1.5× weight multiplier (prefers guard breaks)
- Assassin with Dagger: Counter gets 1.5× weight multiplier (counter-focused)

---

## Phase 4: Dual-Weapon System Architecture

### 4.1 WeaponController Refactor

**File:** `Assets/Scripts/Combat/Weapons/WeaponController.cs`

**Current (Line 8):**
```csharp
[SerializeField] private WeaponData weaponData;
```

**Updated:**
```csharp
[Header("Weapon Slots")]
[SerializeField] private WeaponData primaryWeapon;
[SerializeField] private WeaponData secondaryWeapon;
[SerializeField] private WeaponSlot activeSlot = WeaponSlot.Primary;

[Header("Weapon Models")]
private GameObject primaryWeaponModel;
private GameObject secondaryWeaponModel;

// Public accessors
public WeaponData PrimaryWeapon => primaryWeapon;
public WeaponData SecondaryWeapon => secondaryWeapon;
public WeaponData ActiveWeaponData => activeSlot == WeaponSlot.Primary ? primaryWeapon : secondaryWeapon;
public WeaponSlot ActiveSlot => activeSlot;

// Backwards compatibility (deprecated)
public WeaponData WeaponData => ActiveWeaponData;
```

**Add WeaponSlot enum to `CombatEnums.cs`:**
```csharp
public enum WeaponSlot
{
    Primary,
    Secondary
}
```

### 4.2 Weapon Swapping Methods

**Add to WeaponController.cs:**

```csharp
public void SwapWeapon()
{
    if (isSwapping)
        return;

    activeSlot = (activeSlot == WeaponSlot.Primary) ? WeaponSlot.Secondary : WeaponSlot.Primary;
    StartCoroutine(WeaponSwapCooldown());
    UpdateActiveWeaponModel();

    if (enableDebugLogs)
        Debug.Log($"Swapped to {activeSlot} weapon: {ActiveWeaponData?.weaponName}");
}

private bool isSwapping = false;
private IEnumerator WeaponSwapCooldown()
{
    isSwapping = true;
    yield return new WaitForSeconds(0.2f);  // Brief cooldown
    isSwapping = false;
}

public void SetWeapon(WeaponData newWeapon, WeaponSlot slot)
{
    if (slot == WeaponSlot.Primary)
    {
        primaryWeapon = newWeapon;
        if (activeSlot == WeaponSlot.Primary)
            UpdateActiveWeaponModel();
    }
    else
    {
        secondaryWeapon = newWeapon;
        if (activeSlot == WeaponSlot.Secondary)
            UpdateActiveWeaponModel();
    }
}

public void SetActiveSlot(WeaponSlot slot)
{
    if (activeSlot != slot && !isSwapping)
    {
        activeSlot = slot;
        UpdateActiveWeaponModel();
    }
}
```

### 4.3 Weapon Model Management

**Update `UpdateWeaponModel()` method:**

```csharp
private void UpdateActiveWeaponModel()
{
    // Destroy old models if they exist
    if (primaryWeaponModel != null)
        Destroy(primaryWeaponModel);
    if (secondaryWeaponModel != null)
        Destroy(secondaryWeaponModel);

    // Create primary weapon model
    if (primaryWeapon?.weaponPrefab != null)
    {
        primaryWeaponModel = Instantiate(primaryWeapon.weaponPrefab, weaponAttachPoint);
        primaryWeaponModel.SetActive(activeSlot == WeaponSlot.Primary);
    }

    // Create secondary weapon model
    if (secondaryWeapon?.weaponPrefab != null)
    {
        secondaryWeaponModel = Instantiate(secondaryWeapon.weaponPrefab, weaponAttachPoint);
        secondaryWeaponModel.SetActive(activeSlot == WeaponSlot.Secondary);
    }
}
```

### 4.4 Update All WeaponData References

**Files requiring updates (~30+ references):**

1. **SkillSystem.cs**:
   - All `weaponController.WeaponData` → `weaponController.ActiveWeaponData`

2. **DamageCalculator.cs**:
   - Method signatures already use `WeaponData weapon` parameter (no change needed)

3. **SpeedResolver.cs**:
   - Method signatures already use `WeaponData weapon` parameter (no change needed)

4. **CombatInteractionManager.cs**:
   - Lines 292, 549, 575, etc.: `combatant.WeaponController.WeaponData` → `combatant.WeaponController.ActiveWeaponData`

5. **SimpleTestAI.cs**:
   - Line 72: `weaponController.WeaponData` → `weaponController.ActiveWeaponData`
   - Update weapon checks throughout

6. **MovementController.cs**:
   - Update any weapon range checks

---

## Phase 5: Weapon Swapping Input & UI

### 5.1 Player Input

**File:** `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs`

**Add fields:**
```csharp
[Header("Weapon Swapping")]
[SerializeField] private KeyCode swapWeaponKey = KeyCode.Q;
```

**In `HandleSkillInput()` method:**
```csharp
private void HandleSkillInput()
{
    // Check for weapon swap (only when not executing skills)
    if (Input.GetKeyDown(swapWeaponKey) && currentState == SkillExecutionState.Idle)
    {
        weaponController.SwapWeapon();
        return;
    }

    // Existing skill input handling...
}
```

### 5.2 UI Weapon Indicator

**Create new file:** `Assets/Scripts/Combat/UI/WeaponSlotDisplay.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FairyGate.Combat.UI
{
    public class WeaponSlotDisplay : MonoBehaviour
    {
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Image primaryWeaponIcon;
        [SerializeField] private Image secondaryWeaponIcon;
        [SerializeField] private GameObject primaryHighlight;
        [SerializeField] private GameObject secondaryHighlight;
        [SerializeField] private TextMeshProUGUI swapKeyHint;

        private void Update()
        {
            if (weaponController == null)
                return;

            // Update icons
            if (primaryWeaponIcon != null && weaponController.PrimaryWeapon != null)
                primaryWeaponIcon.sprite = weaponController.PrimaryWeapon.weaponIcon;

            if (secondaryWeaponIcon != null && weaponController.SecondaryWeapon != null)
                secondaryWeaponIcon.sprite = weaponController.SecondaryWeapon.weaponIcon;

            // Update highlight
            bool isPrimaryActive = weaponController.ActiveSlot == WeaponSlot.Primary;
            if (primaryHighlight != null)
                primaryHighlight.SetActive(isPrimaryActive);
            if (secondaryHighlight != null)
                secondaryHighlight.SetActive(!isPrimaryActive);
        }
    }
}
```

### 5.3 Visual Feedback

**Add to WeaponController.SwapWeapon():**
```csharp
// Play swap sound
if (weaponSwapSound != null)
    AudioSource.PlayClipAtPoint(weaponSwapSound, transform.position);

// Optional: Brief swap animation
// animator?.SetTrigger("SwapWeapon");
```

---

## Phase 6: AI Dual-Weapon Integration

### 6.1 Archetype Weapon Loadouts

**File:** `Assets/Scripts/Combat/Utilities/EnemyArchetypeConfig.cs`

**Extend ArchetypeData struct:**
```csharp
public struct ArchetypeData
{
    public CharacterStats stats;
    public string aiType;

    // Weapon loadout
    public WeaponType primaryWeaponType;
    public WeaponType secondaryWeaponType;
    public float weaponPreference;  // 0.0-1.0: 1.0 = always use primary, 0.0 = always use secondary

    // Existing skill weights...
    public float attackWeight;
    public float defenseWeight;
    // etc...
}
```

**Update archetype configurations:**

```csharp
case EnemyArchetype.Archer:
    return new ArchetypeData
    {
        stats = CharacterStats.CreateArcherStats(),
        aiType = "SimpleTestAI",
        primaryWeaponType = WeaponType.Bow,
        secondaryWeaponType = WeaponType.Dagger,
        weaponPreference = 0.8f,  // 80% bow, 20% dagger
        attackWeight = 20f,
        defenseWeight = 25f,
        // etc...
    };

case EnemyArchetype.Berserker:
    return new ArchetypeData
    {
        stats = CharacterStats.CreateBerserkerStats(),
        aiType = "SimpleTestAI",
        primaryWeaponType = WeaponType.Mace,
        secondaryWeaponType = WeaponType.Sword,
        weaponPreference = 0.7f,  // 70% mace, 30% sword
        // etc...
    };

case EnemyArchetype.Assassin:
    return new ArchetypeData
    {
        stats = CharacterStats.CreateAssassinStats(),
        aiType = "SimpleTestAI",
        primaryWeaponType = WeaponType.Dagger,
        secondaryWeaponType = WeaponType.Sword,
        weaponPreference = 0.75f,  // 75% dagger, 25% sword
        // etc...
    };
```

### 6.2 AI Weapon Swap Logic

**File:** `Assets/Scripts/Combat/AI/SimpleTestAI.cs`

**Add fields:**
```csharp
[Header("Dual-Weapon Settings")]
[SerializeField] private float weaponPreference = 0.8f;  // How often to use primary weapon
[SerializeField] private float weaponSwapCooldown = 2.0f;
private float lastWeaponSwapTime = -999f;
```

**Add weapon swap evaluation method:**
```csharp
private void EvaluateWeaponSwap(SkillType intendedSkill)
{
    // Don't swap too frequently
    if (Time.time - lastWeaponSwapTime < weaponSwapCooldown)
        return;

    // Check if we have dual weapons
    if (weaponController.PrimaryWeapon == null || weaponController.SecondaryWeapon == null)
        return;

    // Get skill effectiveness for both weapons
    var currentWeapon = weaponController.ActiveWeaponData;
    var otherWeapon = weaponController.ActiveSlot == WeaponSlot.Primary
        ? weaponController.SecondaryWeapon
        : weaponController.PrimaryWeapon;

    float currentEffectiveness = GetWeaponSkillEffectiveness(currentWeapon, intendedSkill);
    float otherEffectiveness = GetWeaponSkillEffectiveness(otherWeapon, intendedSkill);

    // Swap if other weapon is significantly better (1.5x threshold)
    if (otherEffectiveness > currentEffectiveness * 1.5f)
    {
        // Apply weapon preference (personality)
        float swapChance = weaponController.ActiveSlot == WeaponSlot.Primary
            ? (1.0f - weaponPreference)  // Less likely to swap away from primary
            : weaponPreference;          // More likely to swap back to primary

        if (Random.value < swapChance)
        {
            weaponController.SwapWeapon();
            lastWeaponSwapTime = Time.time;

            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} swapped weapons for {intendedSkill} (effectiveness: {currentEffectiveness:F2} → {otherEffectiveness:F2})");
        }
    }

    // Range-based swap for ranged weapons
    float distanceToPlayer = Vector3.Distance(transform.position, GetPlayerPosition());

    if (currentWeapon.isRangedWeapon && distanceToPlayer < 2.0f)
    {
        // Too close for ranged weapon, swap to melee
        if (weaponController.ActiveSlot == WeaponSlot.Primary && !otherWeapon.isRangedWeapon)
        {
            weaponController.SwapWeapon();
            lastWeaponSwapTime = Time.time;
        }
    }
    else if (!currentWeapon.isRangedWeapon && distanceToPlayer > 5.0f && otherWeapon.isRangedWeapon)
    {
        // Far away with melee weapon, swap to ranged if available
        if (Random.value < 0.7f)  // 70% chance
        {
            weaponController.SwapWeapon();
            lastWeaponSwapTime = Time.time;
        }
    }
}

private float GetWeaponSkillEffectiveness(WeaponData weapon, SkillType skill)
{
    var mod = weapon.GetModifier(skill);
    // Combine multiple modifiers into single effectiveness score
    return (mod.damageMultiplier + mod.speedMultiplier + mod.aiWeightMultiplier) / 3.0f;
}
```

**Call in `TryUseSkill()` before executing:**
```csharp
private bool TryUseSkill(SkillType selectedSkill)
{
    // Evaluate whether to swap weapons first
    EvaluateWeaponSwap(selectedSkill);

    // Existing skill execution logic...
}
```

---

## Phase 7: Equipment System Integration

### 7.1 Update EquipmentSet

**File:** `Assets/Scripts/Combat/Equipment/EquipmentSet.cs`

**Current (Line 8-11):**
```csharp
public string setName;
public EquipmentData armor;
public WeaponData weapon;
public EquipmentData accessory;
```

**Updated:**
```csharp
public string setName;
public EquipmentData armor;
public WeaponData primaryWeapon;
public WeaponData secondaryWeapon;
public EquipmentData accessory;
```

### 7.2 Update EquipmentManager

**File:** `Assets/Scripts/Combat/Equipment/EquipmentManager.cs`

**Update `EquipSet()` method:**
```csharp
public bool EquipSet(EquipmentSet set)
{
    if (set == null || combatController.IsInCombat)
        return false;

    // Equip armor
    if (set.armor != null)
        EquipArmor(set.armor);

    // Equip both weapons
    if (set.primaryWeapon != null)
        weaponController.SetWeapon(set.primaryWeapon, WeaponSlot.Primary);

    if (set.secondaryWeapon != null)
        weaponController.SetWeapon(set.secondaryWeapon, WeaponSlot.Secondary);

    // Equip accessory
    if (set.accessory != null)
        EquipAccessory(set.accessory);

    return true;
}
```

### 7.3 Scene Setup Tool Updates

**File:** `Assets/Scripts/Editor/CompleteCombatSceneSetup.cs`

**Update enemy spawning to assign dual weapons:**
```csharp
private void SetupEnemyWeapons(GameObject character, EnemyArchetype archetype)
{
    var config = EnemyArchetypeConfig.GetArchetypeData(archetype);
    var weaponController = character.GetComponent<WeaponController>();

    if (weaponController != null)
    {
        // Create and assign primary weapon
        WeaponData primaryWeapon = CreateWeaponForType(config.primaryWeaponType);
        weaponController.SetWeapon(primaryWeapon, WeaponSlot.Primary);

        // Create and assign secondary weapon
        WeaponData secondaryWeapon = CreateWeaponForType(config.secondaryWeaponType);
        weaponController.SetWeapon(secondaryWeapon, WeaponSlot.Secondary);
    }
}
```

---

## Phase 8: Testing & Balance

### 8.1 Test Weapon Modifiers

**Test scenarios:**
1. **Spear Lunge** - Should reach significantly farther than Dagger Lunge
2. **Mace Smash** - Should deal more damage and stun longer than Dagger Smash
3. **Dagger Counter** - Should execute faster than Mace Counter
4. **Bow Melee Skills** - Should feel weak but functional (emergency use)
5. **Archer using Windmill** - Should now use 3.75 unit range (not 9.375)

### 8.2 Test AI Weapon Swapping

**Test scenarios:**
1. **Archer rushed** - Should swap from Bow to Dagger when player gets close
2. **Berserker at range** - Should swap from Mace to Sword for better Lunge
3. **Swap cooldown** - Verify AI doesn't spam swap every frame
4. **Weapon preference** - High-preference archetypes should mostly stick to primary

### 8.3 Balance Pass

**Metrics to monitor:**
- No weapon/skill combo should be completely useless (min 0.5× damage multiplier)
- High-skill weapons (Dagger) should reward good play with speed
- Heavy weapons (Mace) should have clear trade-offs (damage vs speed)
- Specialized weapons (Spear, Bow) should excel in their niche

**Tuning parameters:**
- Adjust multipliers if any combo feels too weak/strong
- Adjust AI weaponPreference values if swapping too much/too little
- Adjust swap cooldown if AI behavior feels spammy or sluggish

---

## Implementation Order

### Priority 1: Weapon-Skill Modifiers (Phases 1-3)
This establishes weapon identity and can be tested with single-weapon combat:
1. Phase 1.1-1.2: Data structure + first-pass values
2. Phase 1.3: Update weapon factory methods
3. Phase 2: Integrate into all calculation systems
4. Phase 3: AI skill preferences
5. **TEST MILESTONE**: Verify single-weapon combat feels distinct per weapon type

### Priority 2: Dual-Weapon System (Phases 4-5)
Once modifiers work well, add weapon swapping:
6. Phase 4: Refactor WeaponController for dual slots
7. Phase 5: Player input and UI
8. **TEST MILESTONE**: Player can swap weapons manually

### Priority 3: AI Integration (Phases 6-7)
Finally, teach AI to use dual weapons:
9. Phase 6: AI weapon swap logic
10. Phase 7: Equipment system updates
11. Phase 8: Final testing and balance

---

## Alternative Testing Path: Range-Agnostic Testing

**User Note:** "I may want to just test the system with no range values first"

### Modified Phase 1: Range-Free Testing

**Initial Modifier Set (Exclude Range):**
```csharp
public class WeaponSkillModifier
{
    public SkillType skillType;
    // public float rangeMultiplier = 1.0f;  // SKIP FOR NOW
    public float damageMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public float stunMultiplier = 1.0f;
    public float staminaCostMultiplier = 1.0f;
    public float aiWeightMultiplier = 1.0f;
}
```

**Skip Phase 2.5 (Range Check System)** - Leave existing range checks as-is

**Testing Focus:**
1. Test damage variation (Mace Smash vs Dagger Smash)
2. Test speed variation (Dagger Windmill vs Mace Windmill)
3. Test AI skill preferences (does AI pick appropriate skills?)
4. Verify no gameplay-breaking issues with damage/speed/stamina modifiers

**Add Range Later:**
Once damage/speed/stamina/stun modifiers are balanced and feel good, add range as Phase 2.5B.

---

## Technical Considerations

### Performance
- **Dictionary vs Array**: Array-based approach chosen for Unity Inspector compatibility
- **Lookup Cost**: `Array.Find()` on 6-7 element array is negligible
- **Caching**: Consider caching modifier lookups if profiling shows issues

### Backwards Compatibility
- New `ActiveWeaponData` property maintains existing API
- Default modifier values (1.0×) preserve current behavior
- Existing weapon assets need modifier arrays added (migration needed)

### Extensibility
- Easy to add new modifiers (e.g., `critChanceMultiplier`, `knockbackMultiplier`)
- Easy to add new skills (just add to WeaponSkillModifier array)
- System supports more than 2 weapon slots if needed in future

### Edge Cases
- **Missing Modifier Data**: Default 1.0× values ensure graceful fallback
- **Null Weapon**: All calculations check for null and use defaults
- **Swap During Skill**: Input handler blocks swapping during skill execution
- **AI Swap Spam**: Cooldown + personality preference prevents rapid swapping

---

## File Reference Summary

**New Files:**
- `/Docs/Weapon-Skill-Modifier-System-Design.md` (this document)
- `Assets/Scripts/Combat/UI/WeaponSlotDisplay.cs` (new UI component)

**Modified Files:**
- `Assets/Scripts/Data/WeaponData/WeaponData.cs` - Add modifier array
- `Assets/Scripts/Combat/Weapons/WeaponController.cs` - Dual-weapon support
- `Assets/Scripts/Combat/Stats/DamageCalculator.cs` - Skill-specific damage
- `Assets/Scripts/Combat/Stats/SpeedResolver.cs` - Skill-specific speed
- `Assets/Scripts/Combat/Skills/Base/SkillSystem.cs` - Stamina modifiers + swap input
- `Assets/Scripts/Combat/Core/CombatInteractionManager.cs` - Pass skill type to calculations
- `Assets/Scripts/Combat/AI/SimpleTestAI.cs` - Weapon preferences + swap logic
- `Assets/Scripts/Combat/Utilities/EnemyArchetypeConfig.cs` - Dual-weapon loadouts
- `Assets/Scripts/Combat/Utilities/Constants/CombatEnums.cs` - Add WeaponSlot enum
- `Assets/Scripts/Combat/Equipment/EquipmentSet.cs` - Dual-weapon fields
- `Assets/Scripts/Combat/Equipment/EquipmentManager.cs` - Equip both weapons
- `Assets/Scripts/Editor/CompleteCombatSceneSetup.cs` - Spawn with dual weapons

---

## Questions & Decisions Log

1. **Modifier Data Structure**: Array-based (Unity Inspector friendly) ✓
2. **Swap Mechanic**: Manual hotkey (traditional) ✓
3. **Skill Restrictions**: None (modifiers only) ✓
4. **Use Cases**: All playstyles supported ✓
5. **AI Behavior**: Weapon preference personality ✓
6. **Range Testing**: May test without range modifiers first (TBD)

---

**End of Design Document**