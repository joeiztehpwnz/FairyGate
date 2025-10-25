# Equipment System Design Document

## Overview
Simple, expandable equipment system that modifies character stats and integrates with the existing combat system. Starting with 3 equipment slots and additive stat bonuses.

---

## Core Architecture

### Equipment Slots (Phase 1)
1. **Armor** - Primary defense and survivability
2. **Weapon** - Already implemented via WeaponData, will integrate
3. **Accessory** - Utility stats and special bonuses

**Future Expansion Slots** (Phase 2+):
- Head, Chest, Legs (split armor into pieces)
- Ring, Necklace (split accessory)
- Off-hand (shield, secondary weapon)

---

## Data Structure

### EquipmentData (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "New Equipment", menuName = "Combat/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("Equipment Info")]
    public string equipmentName;
    public EquipmentSlot slot;
    public EquipmentTier tier;

    [Header("Stat Modifiers")]
    public int strengthBonus;
    public int dexterityBonus;
    public int physicalDefenseBonus;
    public int focusBonus;
    public int maxHealthBonus;
    public int maxStaminaBonus;
    public float movementSpeedBonus;

    [Header("Visual")]
    public Sprite icon;
    public GameObject equipmentPrefab; // Optional visual representation

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;

    // Future: Special properties
    // public List<EquipmentProperty> specialProperties;
}
```

### EquipmentSlot Enum
```csharp
public enum EquipmentSlot
{
    Armor,
    Weapon,
    Accessory
    // Future: Head, Chest, Legs, Ring, Necklace, etc.
}
```

### EquipmentTier Enum
```csharp
public enum EquipmentTier
{
    Basic,      // Starter equipment
    Advanced,   // Mid-tier equipment
    Master      // High-tier equipment
    // Future: Legendary, Mythic, etc.
}
```

---

## Equipment Manager Component

### EquipmentManager.cs
```csharp
public class EquipmentManager : MonoBehaviour
{
    [Header("Current Equipment")]
    [SerializeField] private EquipmentData currentArmor;
    [SerializeField] private EquipmentData currentAccessory;
    // Note: Weapon handled by existing WeaponController

    [Header("Equipment Sets (Optional)")]
    [SerializeField] private EquipmentSet[] availableSets;

    [Header("Events")]
    public UnityEvent<EquipmentData, EquipmentSlot> OnEquipmentChanged;
    public UnityEvent OnEquipmentRefreshed;

    private CharacterStats baseStats;
    private CharacterStats modifiedStats;
    private CombatController combatController;

    public EquipmentData CurrentArmor => currentArmor;
    public EquipmentData CurrentAccessory => currentAccessory;
    public CharacterStats ModifiedStats => modifiedStats;

    private void Awake()
    {
        combatController = GetComponent<CombatController>();
        baseStats = combatController.Stats;

        // Create modified stats as a copy of base stats
        modifiedStats = ScriptableObject.CreateInstance<CharacterStats>();
        CopyStats(baseStats, modifiedStats);
    }

    private void Start()
    {
        // Apply initial equipment bonuses
        RefreshEquipmentBonuses();
    }

    public bool EquipItem(EquipmentData equipment)
    {
        if (equipment == null) return false;

        // Can only equip outside combat
        if (combatController.IsInCombat)
        {
            Debug.LogWarning("Cannot change equipment during combat");
            return false;
        }

        // Equip based on slot
        switch (equipment.slot)
        {
            case EquipmentSlot.Armor:
                currentArmor = equipment;
                break;
            case EquipmentSlot.Accessory:
                currentAccessory = equipment;
                break;
            case EquipmentSlot.Weapon:
                // Handled by WeaponController
                Debug.LogWarning("Use WeaponController to change weapons");
                return false;
        }

        RefreshEquipmentBonuses();
        OnEquipmentChanged.Invoke(equipment, equipment.slot);

        return true;
    }

    public bool UnequipItem(EquipmentSlot slot)
    {
        if (combatController.IsInCombat)
        {
            Debug.LogWarning("Cannot change equipment during combat");
            return false;
        }

        switch (slot)
        {
            case EquipmentSlot.Armor:
                currentArmor = null;
                break;
            case EquipmentSlot.Accessory:
                currentAccessory = null;
                break;
        }

        RefreshEquipmentBonuses();
        return true;
    }

    public void RefreshEquipmentBonuses()
    {
        // Reset to base stats
        CopyStats(baseStats, modifiedStats);

        // Apply armor bonuses
        if (currentArmor != null)
        {
            ApplyEquipmentBonuses(currentArmor);
        }

        // Apply accessory bonuses
        if (currentAccessory != null)
        {
            ApplyEquipmentBonuses(currentAccessory);
        }

        // Notify combat controller to use modified stats
        UpdateCombatControllerStats();
        OnEquipmentRefreshed.Invoke();
    }

    private void ApplyEquipmentBonuses(EquipmentData equipment)
    {
        modifiedStats.strength += equipment.strengthBonus;
        modifiedStats.dexterity += equipment.dexterityBonus;
        modifiedStats.physicalDefense += equipment.physicalDefenseBonus;
        modifiedStats.focus += equipment.focusBonus;

        // MaxHealth and MaxStamina need special handling
        // Store bonuses separately to apply properly
    }

    private void CopyStats(CharacterStats source, CharacterStats destination)
    {
        destination.strength = source.strength;
        destination.dexterity = source.dexterity;
        destination.physicalDefense = source.physicalDefense;
        destination.focus = source.focus;
        // Note: MaxHealth/MaxStamina are properties, handle separately
    }

    private void UpdateCombatControllerStats()
    {
        // CombatController needs to reference modifiedStats instead of baseStats
        // This requires a small change to CombatController
    }

    // Quick equipment swapping for testing
    public void EquipSet(EquipmentSet set)
    {
        if (set.armor != null) EquipItem(set.armor);
        if (set.accessory != null) EquipItem(set.accessory);
    }
}
```

---

## Equipment Sets (For Testing)

### EquipmentSet (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "New Equipment Set", menuName = "Combat/Equipment Set")]
public class EquipmentSet : ScriptableObject
{
    public string setName;
    public EquipmentData armor;
    public WeaponData weapon;
    public EquipmentData accessory;

    [TextArea(2, 3)]
    public string description;
}
```

**Preset Examples:**
- **Tank Set**: Heavy Armor (+20 Defense, -2 Speed), Tower Shield, Guardian Ring (+10 HP)
- **Speed Set**: Light Armor (+5 Speed, -5 Defense), Dagger, Swift Boots (+3 Dex)
- **Balanced Set**: Medium Armor (+10 Defense), Sword, Focus Amulet (+5 Focus)
- **Glass Cannon**: Cloth Armor (-5 Defense), Greatsword (+10 Str), Power Ring (+5 Str)

---

## Integration with Existing Systems

### 1. CharacterStats Modification

**Current System:**
```csharp
// CombatController.cs
[SerializeField] private CharacterStats characterStats;
public CharacterStats Stats => characterStats;
```

**Modified System:**
```csharp
// CombatController.cs
[SerializeField] private CharacterStats baseStats;
private EquipmentManager equipmentManager;

public CharacterStats Stats => equipmentManager != null ?
    equipmentManager.ModifiedStats : baseStats;

private void Awake()
{
    equipmentManager = GetComponent<EquipmentManager>();
    // ... existing code
}
```

### 2. Health and Stamina Bonuses

**Current:**
```csharp
// HealthSystem.cs
public int MaxHealth => characterStats?.MaxHealth ?? CombatConstants.BASE_HEALTH;

// StaminaSystem.cs
public int MaxStamina => characterStats?.MaxStamina ?? CombatConstants.BASE_STAMINA;
```

**Modified to Include Equipment:**
```csharp
// HealthSystem.cs
private EquipmentManager equipmentManager;

private void Awake()
{
    equipmentManager = GetComponent<EquipmentManager>();
}

public int MaxHealth
{
    get
    {
        int baseHealth = characterStats?.MaxHealth ?? CombatConstants.BASE_HEALTH;
        int bonus = 0;

        if (equipmentManager != null)
        {
            if (equipmentManager.CurrentArmor != null)
                bonus += equipmentManager.CurrentArmor.maxHealthBonus;
            if (equipmentManager.CurrentAccessory != null)
                bonus += equipmentManager.CurrentAccessory.maxHealthBonus;
        }

        return baseHealth + bonus;
    }
}
```

### 3. Movement Speed Modification

**MovementController** already uses `characterStats.MovementSpeed`, so it will automatically pick up equipment bonuses through the modified stats reference.

---

## Test Environment Integration

### TestEquipmentSelector (New Component)

```csharp
public class TestEquipmentSelector : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private EquipmentManager targetEquipmentManager;
    [SerializeField] private bool autoFindTargetEnemy = true;

    [Header("Equipment Presets")]
    [SerializeField] private EquipmentSet[] equipmentPresets;
    private int currentPresetIndex = 0;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode nextPresetKey = KeyCode.F10;
    [SerializeField] private KeyCode previousPresetKey = KeyCode.F11;
    [SerializeField] private KeyCode removeAllKey = KeyCode.F12;

    private void Start()
    {
        if (autoFindTargetEnemy)
        {
            FindTargetEquipmentManager();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(nextPresetKey))
        {
            CyclePresetForward();
        }
        else if (Input.GetKeyDown(previousPresetKey))
        {
            CyclePresetBackward();
        }
        else if (Input.GetKeyDown(removeAllKey))
        {
            RemoveAllEquipment();
        }
    }

    private void CyclePresetForward()
    {
        if (equipmentPresets.Length == 0) return;

        currentPresetIndex = (currentPresetIndex + 1) % equipmentPresets.Length;
        ApplyCurrentPreset();
    }

    private void CyclePresetBackward()
    {
        if (equipmentPresets.Length == 0) return;

        currentPresetIndex--;
        if (currentPresetIndex < 0) currentPresetIndex = equipmentPresets.Length - 1;
        ApplyCurrentPreset();
    }

    private void ApplyCurrentPreset()
    {
        if (targetEquipmentManager != null && equipmentPresets.Length > 0)
        {
            var preset = equipmentPresets[currentPresetIndex];
            targetEquipmentManager.EquipSet(preset);

            Debug.Log($"[TestEquipment] Applied preset: {preset.setName}");
        }
    }

    private void RemoveAllEquipment()
    {
        if (targetEquipmentManager != null)
        {
            targetEquipmentManager.UnequipItem(EquipmentSlot.Armor);
            targetEquipmentManager.UnequipItem(EquipmentSlot.Accessory);

            Debug.Log("[TestEquipment] Removed all equipment");
        }
    }

    private void FindTargetEquipmentManager()
    {
        // Find enemy with EquipmentManager
        var enemies = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.name.Contains("Enemy"))
            {
                targetEquipmentManager = enemy.GetComponent<EquipmentManager>();
                if (targetEquipmentManager != null)
                {
                    Debug.Log($"[TestEquipment] Found target: {enemy.name}");
                    break;
                }
            }
        }
    }
}
```

### Updated Test Hotkeys:
```
F1-F6: Skill selection (existing)
F7: Toggle Defensive Maintenance (existing)
F8: Toggle Infinite Stamina (existing)
F9: Toggle Movement (existing)
F10: Next Equipment Preset (NEW)
F11: Previous Equipment Preset (NEW)
F12: Remove All Equipment (changed from "Reset AI")
```

---

## Example Equipment Data

### Heavy Armor (Tank Build)
```
Name: Iron Platemail
Slot: Armor
Tier: Advanced

Bonuses:
- Physical Defense: +15
- Max Health: +30
- Movement Speed: -1.0
- Focus: +3

Description: "Heavy iron armor that provides excellent protection at the cost of mobility."
```

### Light Armor (Speed Build)
```
Name: Leather Tunic
Slot: Armor
Tier: Basic

Bonuses:
- Dexterity: +5
- Movement Speed: +2.0
- Physical Defense: +3

Description: "Light leather armor that allows for quick movement and dodging."
```

### Strength Accessory
```
Name: Power Gauntlets
Slot: Accessory
Tier: Advanced

Bonuses:
- Strength: +8
- Max Stamina: -10

Description: "Enchanted gauntlets that enhance striking power but drain stamina faster."
```

### Focus Accessory
```
Name: Meditation Amulet
Slot: Accessory
Tier: Advanced

Bonuses:
- Focus: +10
- Max Stamina: +15

Description: "An amulet that enhances mental fortitude and stamina recovery."
```

---

## Testing Strategy

### Phase 1: Basic Equipment Testing
1. Create 3-4 armor pieces with different stat profiles
2. Create 3-4 accessories with different bonuses
3. Test equipment swapping outside combat
4. Verify stat modifications apply correctly
5. Test with existing TestRepeaterAI (F1-F6 skills)

### Phase 2: Build Testing
Test different equipment combinations:
- **Tank Build**: Heavy armor + defense accessory → Test against rapid attacks (F1)
- **Speed Build**: Light armor + dexterity accessory → Test kiting and mobility
- **Glass Cannon**: Minimal defense + strength accessory → Test high damage output
- **Stamina Build**: Stamina bonuses → Test long defensive stance (F2/F3)

### Phase 3: Integration Testing
Use F10/F11 to cycle equipment during test sessions:
1. F1 (Enemy Attack) + F10 (Cycle to Tank armor) → Should take less damage
2. F4 (Enemy Smash) + F10 (Cycle to Speed armor) → Should be easier to dodge
3. F2 (Enemy Defense) + F10 (Cycle to Strength build) → Should break through easier

---

## Future Expansion Points

### Phase 2 Enhancements:
1. **Equipment Sets**: Bonus when wearing matching pieces
2. **Special Properties**:
   - "Thorns" - Reflect damage back
   - "Regeneration" - Heal over time
   - "Haste" - Reduce skill charge time
3. **Durability**: Equipment degrades with use
4. **Upgrade System**: Enhance equipment stats
5. **Visual Equipment**: Show armor on character model

### Phase 3 Enhancements:
1. **Loot Drops**: Enemies drop equipment
2. **Crafting System**: Combine materials to create equipment
3. **Enchanting**: Add special properties to equipment
4. **Transmogrification**: Change appearance while keeping stats

---

## Implementation Order

### Step 1: Data Structures (30 min)
- Create EquipmentData ScriptableObject
- Create EquipmentSet ScriptableObject
- Add EquipmentSlot and EquipmentTier enums

### Step 2: Core System (1 hour)
- Create EquipmentManager component
- Integrate with CombatController Stats property
- Update HealthSystem/StaminaSystem to use equipment bonuses

### Step 3: Test Integration (30 min)
- Create TestEquipmentSelector component
- Add F10/F11 hotkey support
- Update test documentation

### Step 4: Create Sample Equipment (30 min)
- Create 4-5 armor pieces
- Create 4-5 accessories
- Create 3-4 equipment sets (Tank, Speed, Balanced, Glass Cannon)

### Step 5: Testing & Polish (30 min)
- Test all equipment combinations
- Verify stat calculations
- Test with F1-F6 skill interactions
- Update SKILL_TEST_ENVIRONMENT_USAGE.md

**Total Estimated Time: 3-3.5 hours**

---

## Files to Create

### New Files:
1. `Assets/Scripts/Combat/Equipment/EquipmentData.cs`
2. `Assets/Scripts/Combat/Equipment/EquipmentSet.cs`
3. `Assets/Scripts/Combat/Equipment/EquipmentManager.cs`
4. `Assets/Scripts/Combat/UI/TestEquipmentSelector.cs`
5. `Assets/Scripts/Combat/Utilities/Constants/EquipmentEnums.cs`

### Files to Modify:
1. `Assets/Scripts/Combat/Core/CombatController.cs` - Stats property
2. `Assets/Scripts/Combat/Systems/HealthSystem.cs` - MaxHealth calculation
3. `Assets/Scripts/Combat/Systems/StaminaSystem.cs` - MaxStamina calculation
4. `SKILL_TEST_ENVIRONMENT_USAGE.md` - Add F10/F11 hotkeys

### Sample Data to Create:
1. `Assets/Data/Equipment/Armor/` - 4-5 armor ScriptableObjects
2. `Assets/Data/Equipment/Accessories/` - 4-5 accessory ScriptableObjects
3. `Assets/Data/Equipment/Sets/` - 3-4 equipment set ScriptableObjects

---

## Benefits

✅ **Build Diversity**: Players can customize combat style through stats
✅ **Testing Flexibility**: Easy to test different stat configurations with F10/F11
✅ **Foundation for Progression**: Natural fit for leveling/loot systems later
✅ **Strategic Depth**: Equipment choices matter (tank vs speed vs damage)
✅ **Non-Breaking**: Integrates cleanly with existing combat system
✅ **Expandable**: Clear path for special properties, sets, upgrades, etc.

---

## Risks & Mitigation

**Risk**: Stat bonuses might break combat balance
**Mitigation**: Start with small bonuses (+5-10 range), test thoroughly with F1-F6

**Risk**: Equipment swapping during combat could be exploited
**Mitigation**: Only allow equipping outside combat (enforced in EquipmentManager)

**Risk**: Complex stat calculations might have bugs
**Mitigation**: Simple additive bonuses initially, comprehensive testing

**Risk**: Health/Stamina changes mid-combat if max values change
**Mitigation**: Only allow equipment changes outside combat

---

## Next Steps

Ready to implement when you are! Let me know if you want to:
1. Start with Step 1 (data structures)
2. Modify the design based on your preferences
3. Create the sample equipment data first to visualize the system
