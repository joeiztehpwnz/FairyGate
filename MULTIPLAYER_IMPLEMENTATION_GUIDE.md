# FairyGate Combat System - Multiplayer Implementation Guide

**Date Created:** 2025-10-14
**System Version:** Unity 2023.2.20f1
**Current State:** Single-player local combat system
**Target:** Client-Server multiplayer with 2-4 players
**Estimated Effort:** 45-70 hours (6-9 weeks part-time)
**Recommended Solution:** Unity Netcode for GameObjects

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current System Analysis](#current-system-analysis)
3. [Multiplayer Architecture Overview](#multiplayer-architecture-overview)
4. [Core Changes Required](#core-changes-required)
5. [Networking Solution Comparison](#networking-solution-comparison)
6. [Implementation Roadmap](#implementation-roadmap)
7. [Code Patterns & Examples](#code-patterns--examples)
8. [Challenges & Solutions](#challenges--solutions)
9. [Testing Strategy](#testing-strategy)
10. [Reference Architecture](#reference-architecture)
11. [Appendices](#appendices)

---

## Executive Summary

### Current State Assessment

Your FairyGate combat system is **surprisingly multiplayer-friendly** due to excellent architectural decisions:

**✅ Strengths:**
- Component-based architecture (no global player references)
- Separated input handling (player vs AI distinction already exists)
- Event-driven communication (UnityEvents)
- ScriptableObject data (no embedded state)
- Deterministic combat logic (rock-paper-scissors matrix)

**⚠️ Challenges:**
- Direct Input.GetKey calls (needs abstraction)
- Local-only state storage (needs NetworkVariables)
- Time.time usage (needs network time)
- Random.Range calls (needs deterministic RNG)
- Singleton pattern (needs network awareness)

### Effort Breakdown

| Category | Hours | Complexity |
|----------|-------|------------|
| Learning Unity Netcode | 10-15 | Medium |
| Input System Refactor | 2-3 | Low |
| State Synchronization | 10-15 | Medium |
| Authority & Validation | 8-12 | High |
| Combat Interaction Sync | 6-8 | High |
| Movement & Physics | 4-6 | Medium |
| RNG Determinism | 2-3 | Medium |
| VFX/Audio Sync | 2-3 | Low |
| Testing & Debug | 10-20 | High |
| **TOTAL** | **45-70** | **Medium-High** |

### Recommended Approach

**Phase 1 (Week 1-2):** Foundation - Add Unity Netcode, convert to NetworkBehaviour
**Phase 2 (Week 3-4):** Input & Movement - Abstraction layer, synchronization
**Phase 3 (Week 5-7):** Combat Core - Interaction manager, skill execution
**Phase 4 (Week 8-9):** Polish - VFX sync, testing, optimization

### Key Decision: Server-Authoritative Model

**Why:** Your rock-paper-scissors combat requires precise timing resolution. Client-authoritative would allow cheating and create desyncs.

**Trade-off:** Requires dedicated server or host migration, adds input latency.

---

## Current System Analysis

### Architecture Overview

```
Player/Enemy GameObject
├── CombatController (combat state, targeting)
├── SkillSystem (skill execution, charging)
├── MovementController (WASD input, movement)
├── HealthSystem (HP, damage)
├── StaminaSystem (stamina, drain)
├── StatusEffectManager (stun, knockdown)
├── KnockdownMeterTracker (meter buildup)
├── WeaponController (weapon data, range)
└── AI (SimpleTestAI, KnightAI, PatternedAI)

Global Singletons
├── CombatInteractionManager (processes skill interactions)
└── GameManager (scene reset, game end)

Data (ScriptableObjects)
├── CharacterStats (strength, dex, focus, etc.)
└── WeaponData (range, damage, speed, stun)
```

### Input Flow

```
Keyboard Input (Local Player)
    ↓
SkillSystem.HandleSkillInput()
    ↓
Input.GetKeyDown(attackKey) → StartCharging()
    ↓
ChargeSkill() coroutine → OnSkillCharged event
    ↓
Input.GetKeyDown(attackKey) → ExecuteSkill()
    ↓
CombatInteractionManager.ProcessSkillExecution()
    ↓
DetermineInteraction() → ProcessInteractionEffects()
    ↓
HealthSystem.TakeDamage() → UnityEvent
```

**Multiplayer Issue:** All input is direct keyboard polling. No abstraction for network inputs.

### State Management

**Current Storage:** Local fields in MonoBehaviour
```csharp
// SkillSystem.cs
[SerializeField] private SkillExecutionState currentState;
[SerializeField] private SkillType currentSkill;
[SerializeField] private float chargeProgress;
```

**Multiplayer Issue:** State is not synchronized. Remote players can't see these values.

### Timing & Synchronization

**Current Timing:** Uses `Time.time` for simultaneous execution detection
```csharp
// CombatInteractionManager.cs
if (Time.time - execution.timestamp < 0.1f)
{
    offensiveSkills.Add(execution);
}
```

**Multiplayer Issue:** `Time.time` is different on each client. 100ms window won't work with network latency.

### Random Number Generation

**Current RNG:** Uses Unity's `Random.Range()`
```csharp
// AccuracySystem.cs (from RangedAttack implementation)
float hitRoll = Random.Range(0f, 100f);
bool isHit = hitRoll <= currentAccuracy;
```

**Multiplayer Issue:** Each client will roll different numbers. Hit/miss will desync.

### What's Already Good

#### 1. Component-Based Design
```csharp
// No global "player" reference - perfect for multiplayer!
public class SkillSystem : MonoBehaviour
{
    [SerializeField] private bool isPlayerControlled = true;

    // Can be instantiated multiple times
}
```

#### 2. Separated Input Handling
```csharp
// MovementController.cs - Already has programmatic API!
public void SetMovementInput(Vector3 inputDirection)
{
    if (!isPlayerControlled)
    {
        aiMovementInput = inputDirection; // Network can use this!
    }
}
```

#### 3. Event-Driven Communication
```csharp
// SkillSystem.cs
[Header("Events")]
public UnityEvent<SkillType> OnSkillCharged;
public UnityEvent<SkillType, bool> OnSkillExecuted;

// Easy to hook network sync to these events!
```

#### 4. ScriptableObject Data
```csharp
// CharacterStats.cs, WeaponData.cs
// No state stored in code - perfect for network instantiation
```

---

## Multiplayer Architecture Overview

### Client-Server Model

```
┌─────────────────────────────────────────────────────────────┐
│                    SERVER (Authority)                       │
│                                                             │
│  ┌──────────────────────────────────────────────────┐     │
│  │  CombatInteractionManager                        │     │
│  │  - Processes ALL skill executions                │     │
│  │  - Resolves timing conflicts                     │     │
│  │  - Validates damage calculations                 │     │
│  │  - Generates RNG seeds                           │     │
│  │  - Maintains master game state                   │     │
│  └──────────────────────────────────────────────────┘     │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  Player 1    │  │  Player 2    │  │  Player 3    │    │
│  │  (Server)    │  │  (Authority) │  │  (Authority) │    │
│  └──────────────┘  └──────────────┘  └──────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
        ↓ NetworkVariable (auto-sync)
        ↓ ServerRpc (client → server)
        ↓ ClientRpc (server → all clients)
┌──────────────────────┐              ┌──────────────────────┐
│   CLIENT 1 (Host)    │              │   CLIENT 2           │
│                      │              │                      │
│  ┌────────────────┐  │              │  ┌────────────────┐  │
│  │ Local Player   │  │              │  │ Local Player   │  │
│  │ - Input        │  │              │  │ - Input        │  │
│  │ - Prediction   │  │              │  │ - Prediction   │  │
│  │ - Visuals      │  │              │  │ - Visuals      │  │
│  └────────────────┘  │              │  └────────────────┘  │
│                      │              │                      │
│  ┌────────────────┐  │              │  ┌────────────────┐  │
│  │ Remote Player  │  │              │  │ Remote Player  │  │
│  │ (Player 2)     │  │              │  │ (Player 1)     │  │
│  │ - Interpolate  │  │              │  │ - Interpolate  │  │
│  │ - Visuals      │  │              │  │ - Visuals      │  │
│  └────────────────┘  │              │  └────────────────┘  │
└──────────────────────┘              └──────────────────────┘
```

### Authority Distribution

| System | Authority | Reason |
|--------|-----------|--------|
| **Combat Interactions** | Server | Timing-sensitive, prevents cheating |
| **Damage Calculation** | Server | Prevents health hacking |
| **Skill Execution** | Server (validated) | Ensures consistency |
| **Movement Input** | Client (owner) | Reduces latency, predicted |
| **Visual Effects** | Local (all) | Cosmetic only |
| **Audio** | Local (all) | Cosmetic only |
| **UI** | Local (all) | Cosmetic only |

### Data Flow Example: Attack Execution

```
CLIENT 1 (Attacker)
    ↓ [1] Player presses Attack key
    ↓ [2] Local prediction: Play attack animation
    ↓ [3] Send ExecuteSkillServerRpc(SkillType.Attack)
    ↓
SERVER
    ↓ [4] Validate: Can player attack? (stamina, state, range)
    ↓ [5] If valid: Process through CombatInteractionManager
    ↓ [6] Check for defensive responses (Defense, Counter)
    ↓ [7] Resolve interaction (rock-paper-scissors logic)
    ↓ [8] Calculate damage (server-authoritative)
    ↓ [9] Apply damage to target's NetworkVariable<int> health
    ↓ [10] Broadcast result via ClientRpc (animation, VFX)
    ↓
ALL CLIENTS
    ↓ [11] Receive DamageAppliedClientRpc()
    ↓ [12] Update health bar UI
    ↓ [13] Play hit VFX/audio
    ↓ [14] Apply knockback if applicable
```

### Synchronization Strategy

**NetworkVariable (Auto-Sync):**
- Health, Stamina (frequently changing)
- Current Skill, Skill State
- Position, Rotation (movement)
- Status Effects (stun, knockdown)

**ServerRpc (Client → Server):**
- Skill input (Attack, Defense, etc.)
- Movement input (WASD)
- Combat target selection

**ClientRpc (Server → All Clients):**
- Damage application
- VFX spawning
- Audio triggers
- Animation triggers

---

## Core Changes Required

### 1. Input System Abstraction

**Priority:** 1 (Do First)
**Effort:** 2-3 hours
**Complexity:** Low
**Files:** SkillSystem.cs, MovementController.cs, CombatController.cs

#### Current Code (SkillSystem.cs)

```csharp
private void HandleSkillInput()
{
    // Only process keyboard input for player-controlled characters
    if (!isPlayerControlled) return;

    // Cancel skill input
    if (Input.GetKeyDown(cancelKey))
    {
        CancelSkill();
        return;
    }

    // Skill charging input
    if (currentState == SkillExecutionState.Uncharged)
    {
        if (Input.GetKeyDown(attackKey))
            ExecuteSkill(SkillType.Attack);
        if (Input.GetKeyDown(defenseKey))
            StartCharging(SkillType.Defense);
        // ... etc
    }
}
```

**Problem:** Direct keyboard polling. Can't receive network inputs.

#### Multiplayer Solution

**Step 1:** Create input abstraction interface

```csharp
// NEW FILE: Assets/Scripts/Combat/Input/IInputProvider.cs
namespace FairyGate.Combat
{
    public interface IInputProvider
    {
        bool GetSkillInput(SkillType skill);
        bool GetCancelInput();
        Vector2 GetMovementInput();
        bool GetTargetInput();
        bool GetRestInput();
    }
}
```

**Step 2:** Local keyboard implementation

```csharp
// NEW FILE: Assets/Scripts/Combat/Input/LocalInputProvider.cs
using UnityEngine;

namespace FairyGate.Combat
{
    public class LocalInputProvider : IInputProvider
    {
        private KeyCode attackKey = KeyCode.Alpha1;
        private KeyCode defenseKey = KeyCode.Alpha2;
        private KeyCode counterKey = KeyCode.Alpha3;
        private KeyCode smashKey = KeyCode.Alpha4;
        private KeyCode windmillKey = KeyCode.Alpha5;
        private KeyCode rangedAttackKey = KeyCode.Alpha6;
        private KeyCode cancelKey = KeyCode.Space;
        private KeyCode targetKey = KeyCode.Tab;
        private KeyCode restKey = KeyCode.X;

        public bool GetSkillInput(SkillType skill)
        {
            return skill switch
            {
                SkillType.Attack => Input.GetKeyDown(attackKey),
                SkillType.Defense => Input.GetKeyDown(defenseKey),
                SkillType.Counter => Input.GetKeyDown(counterKey),
                SkillType.Smash => Input.GetKeyDown(smashKey),
                SkillType.Windmill => Input.GetKeyDown(windmillKey),
                SkillType.RangedAttack => Input.GetKeyDown(rangedAttackKey),
                _ => false
            };
        }

        public bool GetCancelInput() => Input.GetKeyDown(cancelKey);

        public Vector2 GetMovementInput()
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) input.y += 1f;
            if (Input.GetKey(KeyCode.S)) input.y -= 1f;
            if (Input.GetKey(KeyCode.A)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D)) input.x += 1f;
            return input;
        }

        public bool GetTargetInput() => Input.GetKeyDown(targetKey);
        public bool GetRestInput() => Input.GetKeyDown(restKey);
    }
}
```

**Step 3:** Network input implementation

```csharp
// NEW FILE: Assets/Scripts/Combat/Input/NetworkInputProvider.cs
using System.Collections.Generic;
using UnityEngine;

namespace FairyGate.Combat
{
    public class NetworkInputProvider : IInputProvider
    {
        // Input state received from network
        private Dictionary<SkillType, bool> skillInputs = new Dictionary<SkillType, bool>();
        private bool cancelInput = false;
        private Vector2 movementInput = Vector2.zero;
        private bool targetInput = false;
        private bool restInput = false;

        // Called by network system when input received
        public void SetSkillInput(SkillType skill, bool pressed)
        {
            skillInputs[skill] = pressed;
        }

        public void SetCancelInput(bool pressed)
        {
            cancelInput = pressed;
        }

        public void SetMovementInput(Vector2 input)
        {
            movementInput = input;
        }

        public void SetTargetInput(bool pressed)
        {
            targetInput = pressed;
        }

        public void SetRestInput(bool pressed)
        {
            restInput = pressed;
        }

        // Clear inputs after processing (simulate GetKeyDown behavior)
        public void ClearInputs()
        {
            skillInputs.Clear();
            cancelInput = false;
            targetInput = false;
            restInput = false;
        }

        // IInputProvider implementation
        public bool GetSkillInput(SkillType skill)
        {
            return skillInputs.GetValueOrDefault(skill, false);
        }

        public bool GetCancelInput() => cancelInput;
        public Vector2 GetMovementInput() => movementInput;
        public bool GetTargetInput() => targetInput;
        public bool GetRestInput() => restInput;
    }
}
```

**Step 4:** Update SkillSystem to use abstraction

```csharp
// MODIFIED: Assets/Scripts/Combat/Skills/Base/SkillSystem.cs

using Unity.Netcode; // ADD THIS

public class SkillSystem : NetworkBehaviour // was: MonoBehaviour
{
    // REMOVE: All [SerializeField] KeyCode fields

    // ADD: Input provider
    private IInputProvider inputProvider;

    private void Awake()
    {
        // ... existing component caching ...

        // NEW: Initialize input provider
        if (IsOwner || isPlayerControlled) // Owner of this network object
        {
            inputProvider = new LocalInputProvider();
        }
        else
        {
            inputProvider = new NetworkInputProvider();
        }
    }

    private void HandleSkillInput()
    {
        // Cancel input
        if (inputProvider.GetCancelInput())
        {
            if (currentState == SkillExecutionState.Aiming)
                CancelAim();
            else
                CancelSkill();
            return;
        }

        // Fire ranged attack if aiming
        if (currentState == SkillExecutionState.Aiming && currentSkill == SkillType.RangedAttack)
        {
            if (inputProvider.GetSkillInput(SkillType.RangedAttack))
            {
                ExecuteSkill(SkillType.RangedAttack);
                return;
            }
        }

        // Skill execution input for charged skills
        if (currentState == SkillExecutionState.Charged && !IsDefensiveSkill(currentSkill))
        {
            if (inputProvider.GetSkillInput(currentSkill))
            {
                ExecuteSkill(currentSkill);
                return;
            }
        }

        // Skill charging/aiming input
        if (currentState == SkillExecutionState.Uncharged || currentState == SkillExecutionState.Charged)
        {
            // Check each skill
            foreach (SkillType skill in System.Enum.GetValues(typeof(SkillType)))
            {
                if (inputProvider.GetSkillInput(skill))
                {
                    if (skill == SkillType.Attack)
                        ExecuteSkill(SkillType.Attack);
                    else if (skill == SkillType.RangedAttack)
                        StartAiming(SkillType.RangedAttack);
                    else
                        StartCharging(skill);
                    return;
                }
            }
        }
    }
}
```

**Step 5:** Update MovementController similarly

```csharp
// MODIFIED: Assets/Scripts/Combat/Core/MovementController.cs

public class MovementController : NetworkBehaviour // was: MonoBehaviour
{
    private IInputProvider inputProvider;

    private void Awake()
    {
        // ... existing code ...

        // Initialize input provider
        if (IsOwner || isPlayerControlled)
        {
            inputProvider = new LocalInputProvider();
        }
        else
        {
            inputProvider = new NetworkInputProvider();
        }
    }

    private void UpdateMovement()
    {
        if (!canMove)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        Vector3 moveDirection = Vector3.zero;

        // Get input from provider
        if (IsOwner || isPlayerControlled)
        {
            Vector2 input = inputProvider.GetMovementInput();
            moveDirection = new Vector3(input.x, 0f, input.y);
        }
        else
        {
            // AI or network input
            moveDirection = aiMovementInput;
        }

        // ... rest of movement logic ...
    }
}
```

#### Benefits of This Approach

✅ **Testability:** Can inject mock input providers for unit tests
✅ **Flexibility:** Easy to add gamepad, touch, or AI inputs later
✅ **Clean Separation:** Input logic decoupled from skill logic
✅ **Network Ready:** Just swap provider for network vs local

---

### 2. State Synchronization

**Priority:** 2 (Core Feature)
**Effort:** 10-15 hours
**Complexity:** Medium
**Files:** All MonoBehaviour combat scripts (~13 files)

#### What Needs Syncing

**SkillSystem:**
- currentSkill (SkillType enum)
- currentState (SkillExecutionState enum)
- chargeProgress (float 0-1)
- canAct (bool)

**HealthSystem:**
- currentHealth (int)
- isDead (bool)

**StaminaSystem:**
- currentStamina (int)
- isResting (bool)

**StatusEffectManager:**
- activeEffects (List<StatusEffectData>)
- isStunned (bool)
- isKnockedDown (bool)

**MovementController:**
- currentMovementSpeed (float)
- canMove (bool)

**CombatController:**
- isInCombat (bool)
- currentTarget (NetworkObjectReference)

#### NetworkVariable Pattern

**Before (Local Only):**
```csharp
[SerializeField] private int currentHealth;
public int CurrentHealth => currentHealth;
```

**After (Network Synced):**
```csharp
private NetworkVariable<int> networkHealth = new NetworkVariable<int>(
    100, // Default value
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server // Only server can change
);

public int CurrentHealth => networkHealth.Value;

// Subscribe to changes
private void Awake()
{
    networkHealth.OnValueChanged += OnHealthChanged;
}

private void OnHealthChanged(int oldHealth, int newHealth)
{
    // Update UI, trigger events, etc.
    Debug.Log($"Health changed: {oldHealth} → {newHealth}");
}
```

#### Example: HealthSystem with Network Sync

```csharp
// MODIFIED: Assets/Scripts/Combat/Systems/HealthSystem.cs

using Unity.Netcode;
using UnityEngine;

namespace FairyGate.Combat
{
    public class HealthSystem : NetworkBehaviour // was: MonoBehaviour
    {
        [Header("Health Configuration")]
        [SerializeField] private CharacterStats characterStats;

        // REPLACE: private int currentHealth;
        // WITH: NetworkVariable
        private NetworkVariable<int> networkCurrentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // REPLACE: private bool isDead;
        private NetworkVariable<bool> networkIsDead = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Properties read from network variables
        public int CurrentHealth => networkCurrentHealth.Value;
        public int MaxHealth { get; private set; }
        public bool IsDead => networkIsDead.Value;

        // Events
        public UnityEvent<int, int> OnHealthChanged = new UnityEvent<int, int>(); // old, new
        public UnityEvent OnDeath = new UnityEvent();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Subscribe to network variable changes
            networkCurrentHealth.OnValueChanged += HandleHealthChanged;
            networkIsDead.OnValueChanged += HandleDeathChanged;

            // Initialize on server
            if (IsServer)
            {
                MaxHealth = characterStats != null
                    ? characterStats.MaxHealth
                    : CombatConstants.BASE_HEALTH;

                networkCurrentHealth.Value = MaxHealth;
            }
        }

        public override void OnNetworkDespawn()
        {
            // Unsubscribe
            networkCurrentHealth.OnValueChanged -= HandleHealthChanged;
            networkIsDead.OnValueChanged -= HandleDeathChanged;

            base.OnNetworkDespawn();
        }

        private void HandleHealthChanged(int oldHealth, int newHealth)
        {
            OnHealthChanged.Invoke(oldHealth, newHealth);

            // Update UI on all clients
            if (enableDebugLogs)
                Debug.Log($"{gameObject.name} health: {oldHealth} → {newHealth}");
        }

        private void HandleDeathChanged(bool oldDead, bool newDead)
        {
            if (newDead)
            {
                OnDeath.Invoke();

                if (enableDebugLogs)
                    Debug.Log($"{gameObject.name} has died!");
            }
        }

        public void TakeDamage(int damage, Transform attacker = null)
        {
            // CRITICAL: Only server can modify health
            if (!IsServer)
            {
                Debug.LogWarning($"Client tried to apply damage - must be server-authoritative!");
                return;
            }

            if (networkIsDead.Value) return;

            // Calculate final damage
            int finalDamage = Mathf.Max(damage, CombatConstants.MINIMUM_DAMAGE);

            // Apply damage
            networkCurrentHealth.Value = Mathf.Max(networkCurrentHealth.Value - finalDamage, 0);

            // Check for death
            if (networkCurrentHealth.Value <= 0 && !networkIsDead.Value)
            {
                networkIsDead.Value = true;
                GameManager.Instance?.OnCharacterDied(this);
            }
        }

        public void Heal(int amount)
        {
            if (!IsServer) return;
            if (networkIsDead.Value) return;

            networkCurrentHealth.Value = Mathf.Min(networkCurrentHealth.Value + amount, MaxHealth);
        }

        public void ResetHealth()
        {
            if (!IsServer) return;

            networkCurrentHealth.Value = MaxHealth;
            networkIsDead.Value = false;
        }
    }
}
```

#### NetworkVariable Types & Limitations

**Supported Types:**
- ✅ Primitives: int, float, bool, byte, etc.
- ✅ Unity types: Vector3, Quaternion, Color
- ✅ Enums: SkillType, SkillExecutionState
- ✅ Structs: Custom serializable structs

**NOT Supported:**
- ❌ Lists, Arrays (use NetworkList<T> instead)
- ❌ Classes (use INetworkSerializable structs)
- ❌ Interfaces

**Example: Syncing Lists (Status Effects)**

```csharp
// StatusEffectManager.cs - Status effects list

// BEFORE:
private List<StatusEffectData> activeEffects = new List<StatusEffectData>();

// AFTER:
using Unity.Netcode;
private NetworkList<StatusEffectData> activeEffects;

private void Awake()
{
    // Initialize in Awake (before OnNetworkSpawn)
    activeEffects = new NetworkList<StatusEffectData>();
}

public override void OnNetworkSpawn()
{
    // Subscribe to list changes
    activeEffects.OnListChanged += OnEffectsChanged;
}

private void OnEffectsChanged(NetworkListEvent<StatusEffectData> changeEvent)
{
    // React to added/removed effects
    Debug.Log($"Status effects changed: {changeEvent.Type}");
}

public void ApplyStatusEffect(StatusEffectData effect)
{
    if (!IsServer) return; // Server authority

    activeEffects.Add(effect);
}
```

**StatusEffectData Struct (must be serializable):**

```csharp
using Unity.Netcode;

public struct StatusEffectData : INetworkSerializable
{
    public StatusEffectType type;
    public float duration;
    public float appliedTime;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref duration);
        serializer.SerializeValue(ref appliedTime);
    }
}
```

---

### 3. Authority & Ownership

**Priority:** 2 (Core Feature)
**Effort:** 8-12 hours
**Complexity:** High
**Files:** All gameplay logic files

#### Authority Model

```
┌───────────────────────────────────────────────────┐
│              AUTHORITY LAYERS                     │
├───────────────────────────────────────────────────┤
│                                                   │
│  ┌─────────────────────────────────────────┐    │
│  │  SERVER (Host)                          │    │
│  │  - CombatInteractionManager             │    │
│  │  - Damage calculation                   │    │
│  │  - Skill validation                     │    │
│  │  - RNG generation                       │    │
│  │  - Game state                           │    │
│  └─────────────────────────────────────────┘    │
│                   ↓ Validates                    │
│  ┌─────────────────────────────────────────┐    │
│  │  OWNER (Player)                         │    │
│  │  - Input (immediate)                    │    │
│  │  - Movement prediction                  │    │
│  │  - Animation triggers                   │    │
│  └─────────────────────────────────────────┘    │
│                   ↓ Visual only                  │
│  ┌─────────────────────────────────────────┐    │
│  │  NON-OWNER (Remote viewer)              │    │
│  │  - Interpolation                        │    │
│  │  - Visual effects                       │    │
│  │  - Audio                                │    │
│  └─────────────────────────────────────────┘    │
│                                                   │
└───────────────────────────────────────────────────┘
```

#### Authority Checks Pattern

**SkillSystem Example:**

```csharp
public class SkillSystem : NetworkBehaviour
{
    public void ExecuteSkill(SkillType skillType)
    {
        // CLIENT: Instant visual feedback (prediction)
        if (IsOwner)
        {
            // Play animation immediately
            // Show skill effect
            // Apply movement restrictions
        }

        // SEND TO SERVER: For validation and processing
        if (IsOwner)
        {
            ExecuteSkillServerRpc(skillType);
        }
    }

    [ServerRpc]
    private void ExecuteSkillServerRpc(SkillType skillType)
    {
        // SERVER: Validate request
        if (!CanExecuteSkill(skillType))
        {
            // Reject invalid request
            Debug.LogWarning($"{gameObject.name} attempted invalid skill execution: {skillType}");

            // Inform client of rejection
            RejectSkillExecutionClientRpc(skillType, "Invalid state or insufficient stamina");
            return;
        }

        // SERVER: Consume stamina (server-authoritative)
        int staminaCost = GetSkillStaminaCost(skillType);
        staminaSystem.ConsumeStamina(staminaCost); // Only runs on server

        // SERVER: Process through combat system
        CombatInteractionManager.Instance?.ProcessSkillExecution(this, skillType);

        // SERVER: Broadcast result to all clients
        SkillExecutedClientRpc(skillType, true);
    }

    [ClientRpc]
    private void SkillExecutedClientRpc(SkillType skillType, bool success)
    {
        // ALL CLIENTS: Play effects, update UI
        if (!IsOwner) // Remote players
        {
            // Play skill animation
            // Show VFX
            // Apply movement restrictions
        }

        // Trigger event for UI updates
        OnSkillExecuted.Invoke(skillType, success);
    }

    [ClientRpc]
    private void RejectSkillExecutionClientRpc(SkillType skillType, string reason)
    {
        // Owner client reverts prediction
        if (IsOwner)
        {
            Debug.Log($"Skill execution rejected: {reason}");
            // Cancel animation
            // Restore movement
        }
    }
}
```

#### ServerRpc vs ClientRpc

**ServerRpc (Client → Server):**
```csharp
[ServerRpc]
private void DoSomethingServerRpc(int parameter)
{
    // Runs ONLY on server
    // Called by client owner
    // Use for: Input, requests, validations
}
```

**ClientRpc (Server → All Clients):**
```csharp
[ClientRpc]
private void DoSomethingClientRpc(int parameter)
{
    // Runs on ALL clients (including server-host)
    // Called by server
    // Use for: Visual effects, audio, UI updates
}
```

**Naming Convention:**
- Always suffix with `ServerRpc` or `ClientRpc`
- Unity Netcode requires this for code generation

#### Damage Flow with Authority

```csharp
// CombatController.cs

public class CombatController : NetworkBehaviour
{
    public void TakeDamage(int damage, Transform attacker)
    {
        // ONLY SERVER processes damage
        if (!IsServer)
        {
            Debug.LogWarning("Client attempted to apply damage - ignoring!");
            return;
        }

        // Validate damage
        if (damage <= 0) return;

        // Apply through HealthSystem (server-authoritative)
        healthSystem.TakeDamage(damage, attacker);

        // Broadcast visual feedback to all clients
        PlayDamageEffectClientRpc(damage, attacker.position);
    }

    [ClientRpc]
    private void PlayDamageEffectClientRpc(int damage, Vector3 attackerPosition)
    {
        // ALL CLIENTS: Show damage number, hit effect, screen shake, etc.
        // Spawn floating damage text
        // Play hit sound
        // Trigger hurt animation
    }
}
```

#### Ownership Checks

```csharp
// Common patterns

if (IsServer)
{
    // Code that only runs on server
    // Use for: Validation, game state changes
}

if (IsOwner)
{
    // Code that only runs for the owning client
    // Use for: Input reading, client prediction
}

if (IsClient)
{
    // Code that runs on all clients (including host)
    // Use for: Visual updates, UI, audio
}

if (IsHost)
{
    // Code that runs when server is also a client
    // Special case: Server + Local player
}

if (!IsOwner)
{
    // Code for remote players only
    // Use for: Interpolation, remote animations
}
```

---

### 4. Combat Interaction Manager Network Sync

**Priority:** 3 (Critical System)
**Effort:** 6-8 hours
**Complexity:** High
**Impact:** CRITICAL - Core combat relies on this

#### Current Architecture

```csharp
// Singleton instance
private static CombatInteractionManager instance;

// Uses Time.time for simultaneity detection
if (Time.time - execution.timestamp < 0.1f)
{
    offensiveSkills.Add(execution);
}

// Processes locally
private void ProcessSkillExecution(SkillSystem skillSystem, SkillType skillType)
{
    var execution = new SkillExecution
    {
        skillSystem = skillSystem,
        skillType = skillType,
        combatant = skillSystem.GetComponent<CombatController>(),
        timestamp = Time.time // LOCAL TIME
    };

    pendingExecutions.Enqueue(execution);
}
```

**Problems for Multiplayer:**
1. `Time.time` is different on each client
2. Singleton pattern doesn't work across network
3. Local processing can't handle distributed skills
4. 0.1s window too small for network latency (100-200ms typical)

#### Multiplayer Solution

```csharp
// MODIFIED: Assets/Scripts/Combat/Core/CombatInteractionManager.cs

using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    public class CombatInteractionManager : NetworkBehaviour // was: MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float simultaneousWindowSeconds = 0.15f; // Wider for network latency

        // Singleton (network-aware)
        private static CombatInteractionManager instance;
        public static CombatInteractionManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<CombatInteractionManager>();
                return instance;
            }
        }

        // Server-side queues (only exist on server)
        private Queue<NetworkSkillExecution> pendingExecutions = new Queue<NetworkSkillExecution>();
        private List<NetworkSkillExecution> waitingDefensiveSkills = new List<NetworkSkillExecution>();

        public override void OnNetworkSpawn()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            // ONLY SERVER processes interactions
            if (!IsServer) return;

            ProcessPendingExecutions();
        }

        // Called by SkillSystem when skill is executed
        public void ProcessSkillExecution(SkillSystem skillSystem, SkillType skillType)
        {
            if (!IsServer)
            {
                Debug.LogWarning("ProcessSkillExecution called on client - should only be server!");
                return;
            }

            var execution = new NetworkSkillExecution
            {
                networkObjectId = skillSystem.GetComponent<NetworkObject>().NetworkObjectId,
                skillType = skillType,
                combatant = skillSystem.GetComponent<CombatController>(),
                timestamp = (float)NetworkManager.Singleton.ServerTime.Time // NETWORK TIME
            };

            if (SpeedResolver.IsOffensiveSkill(skillType))
            {
                pendingExecutions.Enqueue(execution);
            }
            else if (SpeedResolver.IsDefensiveSkill(skillType))
            {
                waitingDefensiveSkills.Add(execution);
            }
        }

        private void ProcessPendingExecutions()
        {
            if (pendingExecutions.Count == 0) return;

            var offensiveSkills = new List<NetworkSkillExecution>();
            float currentNetworkTime = (float)NetworkManager.Singleton.ServerTime.Time;

            // Collect simultaneous offensive executions (wider window for network latency)
            while (pendingExecutions.Count > 0)
            {
                var execution = pendingExecutions.Dequeue();
                if (currentNetworkTime - execution.timestamp < simultaneousWindowSeconds)
                {
                    offensiveSkills.Add(execution);
                }
            }

            if (offensiveSkills.Count == 1)
            {
                ProcessSingleOffensiveSkill(offensiveSkills[0]);
            }
            else if (offensiveSkills.Count > 1)
            {
                ProcessMultipleOffensiveSkills(offensiveSkills);
            }
        }

        // Network-aware skill execution data
        private struct NetworkSkillExecution
        {
            public ulong networkObjectId; // Network reference instead of direct reference
            public SkillType skillType;
            public CombatController combatant;
            public float timestamp; // Network time, not local time
        }

        // ... rest of interaction logic (mostly unchanged) ...
    }
}
```

#### Network Time vs Local Time

**Problem:**
```csharp
// Each client has different Time.time
Client A: Time.time = 125.7
Client B: Time.time = 98.3  // Started later
Server:   Time.time = 200.1  // Been running longer

// Can't compare these directly!
```

**Solution:**
```csharp
// Use network time (synchronized across all clients)
float networkTime = (float)NetworkManager.Singleton.ServerTime.Time;

// All clients see roughly the same value:
Client A: networkTime = 50.2
Client B: networkTime = 50.2
Server:   networkTime = 50.2

// Small differences due to ping, but within tolerance
```

#### Wider Simultaneity Window

```csharp
// Single-player: 0.1s window (100ms)
// Multiplayer: 0.15s-0.2s window (150-200ms)

// Accounts for:
// - Network latency (50-100ms typical)
// - Clock drift
// - Processing delays

[SerializeField] private float simultaneousWindowSeconds = 0.15f;
```

---

### 5. Deterministic RNG

**Priority:** 4 (Important)
**Effort:** 2-3 hours
**Complexity:** Medium
**Files:** AccuracySystem.cs, SpeedResolver.cs (any RNG usage)

#### Problem

```csharp
// AccuracySystem.cs (from RangedAttack implementation)
public bool RollHitChance()
{
    float hitRoll = Random.Range(0f, 100f); // DIFFERENT ON EACH CLIENT!
    bool isHit = hitRoll <= currentAccuracy;
    return isHit;
}

// Result: Client A rolls 75 (hit), Client B rolls 20 (miss) → DESYNC!
```

#### Solution 1: Server-Rolled RNG (Recommended)

**Server rolls, broadcasts result:**

```csharp
// MODIFIED: AccuracySystem.cs

using Unity.Netcode;

public class AccuracySystem : NetworkBehaviour
{
    // Store result from server
    private NetworkVariable<bool> lastRollResult = new NetworkVariable<bool>();
    private bool waitingForRollResult = false;

    public bool RollHitChance()
    {
        if (IsServer)
        {
            // SERVER: Roll locally
            float hitRoll = Random.Range(0f, 100f);
            bool isHit = hitRoll <= currentAccuracy;

            lastRollResult.Value = isHit;

            if (enableDebugLogs)
                Debug.Log($"[SERVER] Rolled {hitRoll:F1} vs {currentAccuracy:F1}% → {(isHit ? "HIT" : "MISS")}");

            return isHit;
        }
        else
        {
            // CLIENT: Request roll from server, use cached result
            if (!waitingForRollResult)
            {
                RequestRollServerRpc();
                waitingForRollResult = true;
            }

            return lastRollResult.Value;
        }
    }

    [ServerRpc]
    private void RequestRollServerRpc()
    {
        // Server rolls and NetworkVariable auto-syncs to clients
        RollHitChance();
    }
}
```

**Pro:** Simple, guaranteed sync
**Con:** Clients wait for server (adds latency)

#### Solution 2: Seeded RNG (Advanced)

**All clients use same seed, generate same sequence:**

```csharp
// NEW FILE: Assets/Scripts/Combat/Network/NetworkRandom.cs

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FairyGate.Combat.Network
{
    /// <summary>
    /// Deterministic RNG synchronized across network
    /// </summary>
    public class NetworkRandom : NetworkBehaviour
    {
        private static NetworkRandom instance;
        public static NetworkRandom Instance => instance;

        // Synchronized seed
        private NetworkVariable<int> currentSeed = new NetworkVariable<int>();
        private System.Random rng;

        // Request queue (server assigns seeds to requests)
        private Queue<System.Action<int>> pendingRollRequests = new Queue<System.Action<int>>();

        public override void OnNetworkSpawn()
        {
            instance = this;

            // Initialize RNG when seed changes
            currentSeed.OnValueChanged += OnSeedChanged;

            if (IsServer)
            {
                // Server generates initial seed
                currentSeed.Value = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
        }

        private void OnSeedChanged(int oldSeed, int newSeed)
        {
            rng = new System.Random(newSeed);
            Debug.Log($"NetworkRandom seed set to {newSeed}");
        }

        /// <summary>
        /// Get a random float in range [min, max]
        /// </summary>
        public float Range(float min, float max)
        {
            if (rng == null)
            {
                Debug.LogWarning("RNG not initialized, using seed 0");
                rng = new System.Random(0);
            }

            return (float)(rng.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// Get a random int in range [min, max)
        /// </summary>
        public int Range(int min, int max)
        {
            if (rng == null)
            {
                Debug.LogWarning("RNG not initialized, using seed 0");
                rng = new System.Random(0);
            }

            return rng.Next(min, max);
        }

        /// <summary>
        /// Request a new seed from server (for events that need fresh randomness)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestNewSeedServerRpc()
        {
            if (IsServer)
            {
                currentSeed.Value = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
        }
    }
}
```

**Usage in AccuracySystem:**

```csharp
public bool RollHitChance()
{
    // Request fresh seed for this roll (ensures unpredictability)
    if (IsServer)
    {
        NetworkRandom.Instance.RequestNewSeedServerRpc();
    }

    // All clients use same seed, get same result
    float hitRoll = NetworkRandom.Instance.Range(0f, 100f);
    bool isHit = hitRoll <= currentAccuracy;

    if (enableDebugLogs)
        Debug.Log($"Rolled {hitRoll:F1} vs {currentAccuracy:F1}% → {(isHit ? "HIT" : "MISS")}");

    return isHit;
}
```

**Pro:** No latency, client-side prediction possible
**Con:** More complex, requires synchronization of RNG calls

#### Recommendation

**Use Solution 1 (Server-Rolled)** for initial implementation:
- ✅ Simpler to implement
- ✅ Guaranteed synchronization
- ✅ Easier to debug
- ⚠️ Adds ~50-100ms latency (acceptable for turn-based combat)

**Use Solution 2 (Seeded)** if latency becomes issue:
- Requires careful call order management
- All clients must make RNG calls in same order
- Good for action games, overkill for tactical combat

---

### 6. Movement & Physics Synchronization

**Priority:** 3 (Core Feature)
**Effort:** 4-6 hours
**Complexity:** Medium
**Files:** MovementController.cs

#### Current Movement

```csharp
// Local only, no prediction or interpolation
private void UpdateMovement()
{
    Vector3 moveDirection = GetInputDirection();
    currentVelocity = moveDirection * currentMovementSpeed;
    characterController.Move(currentVelocity * Time.deltaTime);
}
```

#### Multiplayer Solution: Client Prediction + Server Reconciliation

```csharp
// MODIFIED: Assets/Scripts/Combat/Core/MovementController.cs

using Unity.Netcode;
using UnityEngine;

namespace FairyGate.Combat
{
    public class MovementController : NetworkBehaviour
    {
        // Network-synced position (server authority)
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

        // Client-side prediction
        private Vector3 predictedPosition;
        private Vector3 positionError;
        private float reconciliationSpeed = 10f;

        // Input buffering
        private Vector3 lastMovementInput;
        private int clientTick = 0;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
            }

            networkPosition.OnValueChanged += OnPositionChanged;
        }

        private void Update()
        {
            if (IsOwner)
            {
                // LOCAL PLAYER: Client-side prediction
                UpdateOwnerMovement();
            }
            else
            {
                // REMOTE PLAYER: Interpolation
                UpdateRemoteMovement();
            }
        }

        private void UpdateOwnerMovement()
        {
            if (!canMove)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            // Get input
            Vector3 moveDirection = GetMovementDirection();
            currentVelocity = moveDirection * currentMovementSpeed;

            // Apply movement locally (instant feedback)
            characterController.Move(currentVelocity * Time.deltaTime);

            // Send to server for validation
            clientTick++;
            SendMovementServerRpc(transform.position, transform.rotation, moveDirection, clientTick);

            // Apply server correction if exists
            if (positionError.magnitude > 0.01f)
            {
                // Smoothly correct position to match server
                Vector3 correction = positionError * reconciliationSpeed * Time.deltaTime;
                characterController.Move(correction);
                positionError -= correction;
            }
        }

        [ServerRpc]
        private void SendMovementServerRpc(Vector3 position, Quaternion rotation, Vector3 input, int tick)
        {
            // SERVER: Validate movement
            Vector3 expectedPosition = ValidateMovement(position, input, tick);

            // Update network variables (auto-synced to all clients)
            networkPosition.Value = expectedPosition;
            networkRotation.Value = rotation;

            // Send correction back to owner if needed
            float error = Vector3.Distance(position, expectedPosition);
            if (error > 0.1f)
            {
                SendCorrectionClientRpc(expectedPosition, tick);
            }
        }

        [ClientRpc]
        private void SendCorrectionClientRpc(Vector3 correctedPosition, int tick)
        {
            if (!IsOwner) return; // Only owner needs correction

            // Store error for smooth reconciliation
            positionError = correctedPosition - transform.position;

            if (enableDebugLogs)
                Debug.Log($"Position corrected by {positionError.magnitude:F2} units");
        }

        private Vector3 ValidateMovement(Vector3 requestedPosition, Vector3 input, int tick)
        {
            // SERVER: Validate client's movement is legal
            // - Check speed
            // - Check collision
            // - Check movement restrictions (skill states)

            Vector3 currentPos = networkPosition.Value;
            float maxDistance = currentMovementSpeed * Time.deltaTime * 2f; // 2x tolerance

            Vector3 delta = requestedPosition - currentPos;
            if (delta.magnitude > maxDistance)
            {
                // Client moved too far, clamp
                Debug.LogWarning($"Client moved too far: {delta.magnitude:F2} > {maxDistance:F2}");
                return currentPos + delta.normalized * maxDistance;
            }

            // TODO: Additional validation (collision, skill restrictions, etc.)

            return requestedPosition;
        }

        private void UpdateRemoteMovement()
        {
            // REMOTE PLAYER: Smoothly interpolate to network position
            float lerpSpeed = 10f;

            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition.Value,
                Time.deltaTime * lerpSpeed
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                networkRotation.Value,
                Time.deltaTime * lerpSpeed
            );
        }

        private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
        {
            // React to position changes (for animations, audio, etc.)
            if (!IsOwner)
            {
                // Remote player moved
                // Could trigger footstep sounds, dust particles, etc.
            }
        }
    }
}
```

#### Why Client Prediction?

**Without Prediction (Lag):**
```
Player presses W
  ↓ 50ms network delay
Server receives input
  ↓ Processes movement
  ↓ 50ms network delay
Client sees movement

Total Delay: 100ms (feels sluggish!)
```

**With Prediction:**
```
Player presses W
  ↓ Immediate: Client moves locally
  ↓ 50ms network delay
Server receives input, validates
  ↓ If valid: No correction needed
  ↓ If invalid: Send correction
  ↓ 50ms network delay
Client applies smooth correction if needed

Perceived Delay: 0ms (feels responsive!)
```

---

### 7. Visual Effects & Audio

**Priority:** 5 (Polish)
**Effort:** 2-3 hours
**Complexity:** Low
**Files:** SkillSystem.cs, StatusEffectManager.cs

#### Current VFX Spawning

```csharp
// SkillSystem.cs - DrawRangedAttackTrail()
private void DrawRangedAttackTrail(Vector3 from, Vector3 to, bool wasHit)
{
    // Creates LineRenderer locally only
    GameObject trailObj = new GameObject("RangedAttackTrail");
    LineRenderer line = trailObj.AddComponent<LineRenderer>();
    // ... configure line ...
    Destroy(trailObj, 0.5f);
}
```

**Problem:** Only local player sees effects. Remote players see nothing.

#### Multiplayer Solution

```csharp
// MODIFIED: SkillSystem.cs

private void DrawRangedAttackTrail(Vector3 from, Vector3 to, bool wasHit)
{
    if (IsServer)
    {
        // Server spawns effect for all clients
        DrawRangedAttackTrailClientRpc(from, to, wasHit);
    }
}

[ClientRpc]
private void DrawRangedAttackTrailClientRpc(Vector3 from, Vector3 to, bool wasHit)
{
    // ALL CLIENTS see the effect
    GameObject trailObj = new GameObject("RangedAttackTrail");
    LineRenderer line = trailObj.AddComponent<LineRenderer>();

    // Configure line appearance
    line.startWidth = 0.08f;
    line.endWidth = 0.08f;
    line.material = new Material(Shader.Find("Sprites/Default"));
    line.startColor = wasHit ? Color.yellow : Color.gray;
    line.endColor = wasHit ? Color.red : Color.gray;
    line.positionCount = 2;
    line.SetPosition(0, from + Vector3.up * 1.5f);
    line.SetPosition(1, to);

    // Cleanup
    Destroy(trailObj, CombatConstants.RANGED_ATTACK_TRAIL_DURATION);
}
```

#### Audio Synchronization

```csharp
// Example: Play hit sound on all clients

[ClientRpc]
private void PlayHitSoundClientRpc(Vector3 position)
{
    if (hitSound != null)
    {
        AudioSource.PlayClipAtPoint(hitSound, position);
    }
}

// Called from server after damage applied
public void TakeDamage(int damage, Transform attacker)
{
    if (!IsServer) return;

    // Apply damage...
    healthSystem.TakeDamage(damage);

    // Play sound on all clients
    PlayHitSoundClientRpc(transform.position);
}
```

#### Particle Effects

```csharp
// For networked particle effects, use NetworkObject

[ClientRpc]
private void SpawnParticleEffectClientRpc(Vector3 position, Quaternion rotation)
{
    GameObject effect = Instantiate(particleEffectPrefab, position, rotation);
    Destroy(effect, 2f); // Auto-cleanup
}
```

**Note:** Particle prefabs don't need NetworkObject component (cosmetic only).

---

### 8. Game Manager & Scene Flow

**Priority:** 5 (Polish)
**Effort:** 1-2 hours
**Complexity:** Low
**Files:** GameManager.cs

#### Current Scene Reset

```csharp
// GameManager.cs - Reset on keypress
private void Update()
{
    if (Input.GetKeyDown(resetKey))
    {
        ResetScene();
    }
}
```

**Problem:** Each client resets independently. Desync.

#### Multiplayer Solution

```csharp
// MODIFIED: GameManager.cs

using Unity.Netcode;

public class GameManager : NetworkBehaviour // was: MonoBehaviour
{
    // Remove singleton pattern (NetworkManager handles this)

    public override void OnNetworkSpawn()
    {
        // Don't destroy on load for persistent manager
        if (IsServer)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        // ONLY HOST can reset scene
        if (!IsServer) return;

        if (Input.GetKeyDown(resetKey))
        {
            ResetSceneServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetSceneServerRpc()
    {
        // Server initiates reset for all clients
        ResetSceneClientRpc();
    }

    [ClientRpc]
    private void ResetSceneClientRpc()
    {
        // ALL CLIENTS reload scene synchronously
        if (enableDebugLogs)
            Debug.Log("Resetting scene (network synchronized)");

        // Use NetworkManager's scene management
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                combatSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }
    }

    public void OnCharacterDied(HealthSystem deadCharacter)
    {
        if (!IsServer) return; // Only server handles game end
        if (gameEnded) return;

        gameEnded = true;

        // Broadcast game end to all clients
        GameEndedClientRpc(deadCharacter.name);
    }

    [ClientRpc]
    private void GameEndedClientRpc(string deadCharacterName)
    {
        // ALL CLIENTS show game over UI
        if (enableDebugLogs)
            Debug.Log($"Game ended! {deadCharacterName} has died.");

        ShowDeathMessage(deadCharacterName);
    }
}
```

---

## Networking Solution Comparison

### Option 1: Unity Netcode for GameObjects ⭐ RECOMMENDED

**Overview:**
Official Unity networking solution (formerly MLAPI). Component-based, similar to MonoBehaviour.

**Pros:**
- ✅ **Free & Open Source**
- ✅ **Official Unity Support** - Integrated with Unity ecosystem
- ✅ **NetworkBehaviour** similar to MonoBehaviour (easy migration)
- ✅ **NetworkVariable** auto-sync (simple state management)
- ✅ **ServerRpc/ClientRpc** built-in (easy remote calls)
- ✅ **Good Documentation** - Unity Learn tutorials available
- ✅ **Active Development** - Regular updates
- ✅ **Relay Support** - Unity Relay for NAT traversal
- ✅ **Server-Authoritative** - Perfect for your combat system

**Cons:**
- ⚠️ **Learning Curve** - Networking concepts take time
- ⚠️ **Boilerplate** - More code than some alternatives
- ⚠️ **Relatively New** - Less mature than Mirror/Photon

**Best For:**
- Server-authoritative games
- Turn-based or tactical combat (like yours)
- Long-term projects (future-proof)

**Code Example:**
```csharp
using Unity.Netcode;

public class SkillSystem : NetworkBehaviour
{
    private NetworkVariable<SkillType> networkSkill = new NetworkVariable<SkillType>();

    [ServerRpc]
    private void ExecuteSkillServerRpc(SkillType skill)
    {
        // Server logic
    }

    [ClientRpc]
    private void SkillExecutedClientRpc()
    {
        // Visual feedback
    }
}
```

**Installation:**
```
Window → Package Manager → Unity Registry → "Netcode for GameObjects"
```

**Resources:**
- [Official Docs](https://docs-multiplayer.unity3d.com/)
- [Unity Learn](https://learn.unity.com/tutorial/get-started-with-netcode-for-gameobjects)

---

### Option 2: Mirror Networking

**Overview:**
Community-driven open-source networking (fork of old Unity UNET).

**Pros:**
- ✅ **Free & Open Source**
- ✅ **Mature & Stable** - Years of development
- ✅ **Large Community** - Active Discord, forums
- ✅ **SyncVar** - Simple state sync (like NetworkVariable)
- ✅ **Command/ClientRpc** pattern (similar to Netcode)
- ✅ **Lots of Examples** - Many games built with it

**Cons:**
- ⚠️ **Not Official** - Community support only
- ⚠️ **API Changes** - Breaking changes between versions
- ⚠️ **Less Unity Integration** - Not as seamless

**Best For:**
- Developers familiar with old UNET
- Community-driven projects
- Games that need proven stability

**Code Example:**
```csharp
using Mirror;

public class SkillSystem : NetworkBehaviour
{
    [SyncVar]
    private SkillType currentSkill;

    [Command]
    private void CmdExecuteSkill(SkillType skill)
    {
        // Server logic
    }

    [ClientRpc]
    private void RpcSkillExecuted()
    {
        // Visual feedback
    }
}
```

**Installation:**
```
Asset Store → "Mirror Networking" (free)
```

**Resources:**
- [Official Site](https://mirror-networking.com/)
- [Discord Community](https://discord.gg/N9QVxbM)

---

### Option 3: Photon PUN 2

**Overview:**
Commercial networking solution with managed cloud hosting.

**Pros:**
- ✅ **Easy to Learn** - Simplest API
- ✅ **Managed Hosting** - No server setup needed
- ✅ **Matchmaking Built-In** - Room-based lobby system
- ✅ **Great for Prototyping** - Fast initial setup
- ✅ **Cross-Platform** - Mobile, console, PC

**Cons:**
- ❌ **Costs Money** - Free up to 20 CCU, then paid plans
- ❌ **Peer-to-Peer Focus** - Not ideal for server authority
- ❌ **Less Control** - Managed hosting limits server logic
- ⚠️ **Network Optimization Harder** - Less low-level access

**Best For:**
- Small indie games
- Prototypes
- Peer-to-peer games (party games, co-op)

**NOT Recommended For Your Project:**
- Your rock-paper-scissors combat NEEDS server authority
- Photon PUN is designed for P2P (Photon Fusion/Quantum better, but expensive)

**Code Example:**
```csharp
using Photon.Pun;

public class SkillSystem : MonoBehaviourPun
{
    [PunRPC]
    private void ExecuteSkill(SkillType skill)
    {
        // Logic
    }

    // Call RPC
    photonView.RPC("ExecuteSkill", RpcTarget.All, SkillType.Attack);
}
```

**Installation:**
```
Asset Store → "PUN 2 - FREE" (requires account)
```

---

### Recommendation Summary

| Feature | Unity Netcode | Mirror | Photon PUN 2 |
|---------|--------------|--------|--------------|
| **Cost** | Free | Free | Free→Paid |
| **Server Authority** | ✅ Excellent | ✅ Good | ⚠️ Difficult |
| **Learning Curve** | Medium | Medium | Easy |
| **Official Support** | ✅ Unity | ❌ Community | ✅ Photon |
| **Your Combat System** | ⭐ **Perfect** | ✅ Good | ❌ Poor Fit |
| **Hosting** | Self/Relay | Self | Managed |
| **Documentation** | ✅ Good | ✅ Good | ✅ Excellent |

**Final Verdict: Unity Netcode for GameObjects**

Reasons:
1. ✅ Server-authoritative (required for your combat)
2. ✅ Free & official (long-term support)
3. ✅ NetworkBehaviour pattern (easy migration)
4. ✅ Future-proof (Unity's official solution)
5. ✅ Your architecture already matches its design

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2)

**Goal:** Set up networking infrastructure and basic spawning.

**Tasks:**
1. **Install Unity Netcode (1 hour)**
   - Window → Package Manager
   - Search "Netcode for GameObjects"
   - Install latest stable version
   - Verify installation (check Unity.Netcode namespace)

2. **Create Network Manager (1 hour)**
   ```csharp
   // NEW: NetworkManagerSetup.cs
   - Add NetworkManager component to scene
   - Configure transport (Unity Transport)
   - Set connection type (Host/Client)
   - Create network prefabs list
   ```

3. **Convert MonoBehaviour → NetworkBehaviour (2-3 hours)**
   - SkillSystem
   - MovementController
   - HealthSystem
   - StaminaSystem
   - StatusEffectManager
   - CombatController
   - WeaponController
   - GameManager
   - CombatInteractionManager

   Change: `public class X : MonoBehaviour` → `public class X : NetworkBehaviour`

4. **Create Player Prefab (2 hours)**
   - Add NetworkObject component
   - Configure ownership
   - Set synchronization mode
   - Test spawning

5. **Test Basic Networking (1 hour)**
   - Build → Run 2 clients
   - Verify connection
   - Verify spawning
   - Debug issues

**Deliverables:**
- ✅ Unity Netcode installed
- ✅ NetworkManager configured
- ✅ Player spawns on network
- ✅ 2 clients can connect

**Validation:**
```
1. Start Host
2. Start Client
3. Both see each other's GameObject
4. Log: "Player connected: ClientId X"
```

---

### Phase 2: Input & Movement (Week 3-4)

**Goal:** Players can move and see each other move.

**Tasks:**
1. **Create Input Abstraction (2-3 hours)**
   - IInputProvider interface
   - LocalInputProvider implementation
   - NetworkInputProvider implementation
   - Hook into SkillSystem/MovementController

2. **Implement Movement Sync (3-4 hours)**
   - NetworkVariable<Vector3> position
   - NetworkVariable<Quaternion> rotation
   - Client prediction (owner moves immediately)
   - Server validation (anti-cheat)
   - Remote interpolation (smooth other players)

3. **Test Movement (1 hour)**
   - 2 players walk around
   - Verify smooth movement
   - Test latency simulation
   - Debug rubber-banding

4. **Add Movement Restrictions (1 hour)**
   - Sync skill movement modifiers
   - Test while aiming/charging
   - Verify immobilization (Windmill, Counter)

**Deliverables:**
- ✅ Input abstraction working
- ✅ Players see each other move
- ✅ Movement feels responsive
- ✅ Skill restrictions synced

**Validation:**
```
1. Player A presses W
2. Player A moves immediately (prediction)
3. Player B sees Player A move (interpolation)
4. Player A charges Windmill
5. Both players see reduced speed
```

---

### Phase 3: Combat Core (Week 5-7)

**Goal:** Full combat system working across network.

**Tasks:**
1. **State Synchronization (4-5 hours)**
   - NetworkVariable for all state
   - Health, Stamina, Skill State
   - Status Effects (NetworkList)
   - Subscribe to OnValueChanged

2. **Skill Execution (4-6 hours)**
   - ServerRpc for skill input
   - Server validates execution
   - ClientRpc for visual feedback
   - Test Attack, Defense, Counter, Smash, Windmill

3. **Combat Interaction Manager (3-4 hours)**
   - Server-authoritative processing
   - Network time (not Time.time)
   - Wider simultaneity window (0.15s)
   - Broadcast interaction results

4. **Damage & Effects (2-3 hours)**
   - Server calculates damage
   - NetworkVariable health updates
   - ClientRpc for hit effects
   - Status effects sync (stun, knockdown)

5. **Deterministic RNG (2 hours)**
   - Server-rolled accuracy
   - NetworkRandom class
   - Test RangedAttack hit/miss sync

6. **Combat Testing (3-4 hours)**
   - Test all 17 skill interactions
   - Test speed resolution
   - Test Counter reflection
   - Test knockdown system
   - Debug desyncs

**Deliverables:**
- ✅ All 5 skills working networked
- ✅ Combat interactions synced
- ✅ Damage applies correctly
- ✅ No desyncs during combat

**Validation:**
```
1. Player A uses Attack
2. Player B uses Counter
3. Server resolves: Counter wins
4. Player A knocked down
5. Both clients see knockdown
6. Damage reflects back to A
7. Both see A's health decrease
```

---

### Phase 4: Polish & Testing (Week 8-9)

**Goal:** Smooth, polished multiplayer experience.

**Tasks:**
1. **VFX & Audio Sync (2 hours)**
   - ClientRpc for all effects
   - RangedAttack trail visible to all
   - Hit sounds synced
   - Particle effects synced

2. **UI Updates (1-2 hours)**
   - Health bars show network health
   - Stamina bars show network stamina
   - Skill UI shows network state
   - Target indicators synced

3. **Lag Compensation (2-3 hours)**
   - Increase interpolation smoothness
   - Add prediction error smoothing
   - Optimize update frequency
   - Test with simulated lag (100-200ms)

4. **Game Flow (1-2 hours)**
   - Scene reset synced
   - Game end synced
   - Respawn system
   - Disconnect handling

5. **Performance Optimization (2-3 hours)**
   - Reduce NetworkVariable update frequency
   - Batch RPCs where possible
   - Profile network traffic
   - Optimize bandwith

6. **Extensive Testing (5-10 hours)**
   - 2-player combat (all interactions)
   - 3-4 player combat (stress test)
   - High latency testing (200ms+)
   - Packet loss simulation
   - Edge cases (disconnect during skill)

7. **Bug Fixes (ongoing)**
   - Fix desyncs
   - Fix rubber-banding
   - Fix visual glitches
   - Polish feel

**Deliverables:**
- ✅ Smooth multiplayer experience
- ✅ All VFX/audio working
- ✅ No major bugs
- ✅ Playable with 100ms+ latency

**Validation:**
```
1. 4-player free-for-all
2. All skills used
3. No desyncs observed
4. Feels responsive (<150ms perceived latency)
5. No crashes/freezes
```

---

### Milestone Checklist

**Phase 1 Complete:**
- [ ] Unity Netcode installed
- [ ] NetworkManager configured
- [ ] Player prefab has NetworkObject
- [ ] 2 clients can connect and see each other

**Phase 2 Complete:**
- [ ] Input abstraction implemented
- [ ] Players can move
- [ ] Other players interpolate smoothly
- [ ] Movement restrictions synced

**Phase 3 Complete:**
- [ ] All 5 skills networked
- [ ] 17 combat interactions working
- [ ] Damage synced correctly
- [ ] No desyncs in testing

**Phase 4 Complete:**
- [ ] VFX/audio synced
- [ ] UI shows network state
- [ ] Feels responsive with 100ms latency
- [ ] 4-player combat stable

---

## Code Patterns & Examples

### Pattern 1: Input Reading (Local vs Network)

```csharp
// Bad (direct keyboard polling)
if (Input.GetKeyDown(KeyCode.Alpha1))
{
    ExecuteSkill(SkillType.Attack);
}

// Good (abstraction)
if (inputProvider.GetSkillInput(SkillType.Attack))
{
    ExecuteSkill(SkillType.Attack);
}
```

### Pattern 2: State Synchronization

```csharp
// Bad (local variable)
private int health = 100;

// Good (network synchronized)
private NetworkVariable<int> networkHealth = new NetworkVariable<int>(100);
public int Health => networkHealth.Value;
```

### Pattern 3: Server-Authoritative Action

```csharp
// Bad (client can cheat)
public void TakeDamage(int damage)
{
    health -= damage; // Any client can call this!
}

// Good (server authority)
public void TakeDamage(int damage)
{
    if (!IsServer) return; // Only server can damage

    networkHealth.Value -= damage;
}
```

### Pattern 4: Client Prediction + Server Validation

```csharp
// Owner client
if (IsOwner)
{
    // Instant feedback (prediction)
    PlayAttackAnimation();

    // Send to server
    ExecuteSkillServerRpc(SkillType.Attack);
}

// Server
[ServerRpc]
private void ExecuteSkillServerRpc(SkillType skill)
{
    // Validate
    if (!CanExecuteSkill(skill))
    {
        RejectSkillClientRpc("Invalid state");
        return;
    }

    // Process
    ProcessSkill(skill);

    // Broadcast
    SkillExecutedClientRpc(skill);
}
```

### Pattern 5: Visual Effect Synchronization

```csharp
// Bad (local only)
void SpawnEffect()
{
    Instantiate(effectPrefab, position, rotation);
}

// Good (all clients see)
void SpawnEffect()
{
    if (IsServer)
    {
        SpawnEffectClientRpc(position, rotation);
    }
}

[ClientRpc]
void SpawnEffectClientRpc(Vector3 pos, Quaternion rot)
{
    Instantiate(effectPrefab, pos, rot);
}
```

### Pattern 6: Network Object Reference

```csharp
// Bad (direct Transform reference - doesn't work across network)
public Transform currentTarget;

// Good (network object reference)
private NetworkVariable<NetworkObjectReference> networkTarget = new NetworkVariable<NetworkObjectReference>();

public void SetTarget(Transform target)
{
    if (!IsServer) return;

    var netObj = target.GetComponent<NetworkObject>();
    if (netObj != null)
    {
        networkTarget.Value = new NetworkObjectReference(netObj);
    }
}

public Transform GetTarget()
{
    if (networkTarget.Value.TryGet(out NetworkObject netObj))
    {
        return netObj.transform;
    }
    return null;
}
```

---

## Challenges & Solutions

### Challenge 1: Timing Windows & Latency

**Problem:**
Your system uses 0.1s window for simultaneous skills. With 100ms network latency, this won't work.

```csharp
// Current code
if (Time.time - execution.timestamp < 0.1f)
{
    offensiveSkills.Add(execution);
}
```

**Why it fails:**
```
Player A presses Attack at T=0
  ↓ 50ms network delay
Server receives at T=50ms
Player B presses Attack at T=60ms
  ↓ 50ms network delay
Server receives at T=110ms

Time difference: 60ms (should be simultaneous, but outside 100ms window!)
```

**Solution:**
```csharp
// Use network time + wider window
float networkTime = (float)NetworkManager.Singleton.ServerTime.Time;
float simultaneousWindow = 0.15f; // Wider tolerance

if (networkTime - execution.timestamp < simultaneousWindow)
{
    offensiveSkills.Add(execution);
}
```

**Additional:** Buffer inputs for 1 frame to batch near-simultaneous actions.

---

### Challenge 2: Charge/Release Skills

**Problem:**
Two-press skills (Smash, Windmill, RangedAttack) have state between presses. Network sync needed.

**Solution:**
```csharp
// Press 1: Start charging
public void StartCharging(SkillType skill)
{
    if (IsOwner)
    {
        // Instant visual feedback
        PlayChargeAnimation();

        // Send to server
        StartChargingServerRpc(skill);
    }
}

[ServerRpc]
private void StartChargingServerRpc(SkillType skill)
{
    // Validate
    if (!CanChargeSkill(skill)) return;

    // Set state
    networkCurrentSkill.Value = skill;
    networkCurrentState.Value = SkillExecutionState.Charging;

    // Start coroutine
    StartCoroutine(ChargeSkill(skill));
}

// Press 2: Execute
public void ExecuteChargedSkill()
{
    if (IsOwner)
    {
        // Instant feedback
        PlayExecuteAnimation();

        // Send to server
        ExecuteChargedSkillServerRpc();
    }
}

[ServerRpc]
private void ExecuteChargedSkillServerRpc()
{
    // Validate state
    if (networkCurrentState.Value != SkillExecutionState.Charged)
    {
        RejectExecutionClientRpc("Skill not charged");
        return;
    }

    // Execute
    ExecuteSkill(networkCurrentSkill.Value);
}
```

---

### Challenge 3: Visual Feedback Lag

**Problem:**
With 100ms ping, players see effects 100ms after pressing button. Feels unresponsive.

**Solution: Client-Side Prediction**
```csharp
public void ExecuteSkill(SkillType skill)
{
    if (IsOwner)
    {
        // INSTANT: Play animation, VFX, audio locally
        PlaySkillAnimation(skill);
        PlaySkillVFX(skill);
        PlaySkillAudio(skill);

        // THEN: Send to server for validation
        ExecuteSkillServerRpc(skill);
    }
}

[ServerRpc]
private void ExecuteSkillServerRpc(SkillType skill)
{
    // Server validates
    bool valid = ValidateSkillExecution(skill);

    if (!valid)
    {
        // Tell client to revert prediction
        RevertSkillPredictionClientRpc(skill);
        return;
    }

    // Process skill...
    // Broadcast to other clients (not owner, they already saw it)
    SkillExecutedClientRpc(skill);
}

[ClientRpc]
private void SkillExecutedClientRpc(SkillType skill)
{
    if (IsOwner) return; // Owner already played locally

    // Remote players see effect
    PlaySkillAnimation(skill);
    PlaySkillVFX(skill);
    PlaySkillAudio(skill);
}

[ClientRpc]
private void RevertSkillPredictionClientRpc(SkillType skill)
{
    if (!IsOwner) return;

    // Server rejected - undo visual prediction
    StopSkillAnimation(skill);
    // Show error message
    Debug.Log($"Skill {skill} rejected by server");
}
```

**Result:** Player sees instant feedback. If valid, continues seamlessly. If invalid, corrects after network delay.

---

### Challenge 4: Knockback & Displacement

**Problem:**
StatusEffectManager applies physical displacement (knockback). Needs physics sync.

**Solution:**
```csharp
// StatusEffectManager.cs

public void ApplyInteractionKnockdown(Vector3 displacement)
{
    if (!IsServer) return; // Server authority

    // Apply on server
    isKnockedDown = true;

    // Sync to clients
    ApplyKnockbackClientRpc(displacement);
}

[ClientRpc]
private void ApplyKnockbackClientRpc(Vector3 displacement)
{
    // ALL CLIENTS apply same displacement
    characterController.Move(displacement);

    // Play knockdown animation
    // Trigger knockdown state
}
```

**Important:** CharacterController.Move() must be called on all clients, not just server, for visual consistency.

---

### Challenge 5: AI Opponents

**Problem:**
AI needs to work in multiplayer. Who controls AI?

**Solution: Server-Controlled AI**
```csharp
public class SimpleTestAI : NetworkBehaviour
{
    private void Update()
    {
        // ONLY SERVER controls AI
        if (!IsServer) return;

        // AI logic runs only on server
        SelectSkill();
        MoveTowardsPlayer();
        ExecuteSkill();
    }
}
```

**Why:** Server is authoritative. AI is part of game state. Clients just see results (via NetworkVariables and ClientRpcs).

**Bonus:** Prevents AI cheating (clients can't see AI "thinking").

---

## Testing Strategy

### Local Testing (Development)

**Tools:**
- Unity Editor Play Mode
- ParrelSync (free Unity package for multiple editor instances)
- Build & Run side-by-side

**Setup:**
```
1. Install ParrelSync: Window → Package Manager → "ParrelSync"
2. Create Clone: ParrelSync → Clones Manager → Create New Clone
3. Open Clone in new editor instance
4. Test: One editor = Host, other = Client
```

**Benefits:**
- ✅ Fast iteration (no build time)
- ✅ Debug both clients simultaneously
- ✅ Easy breakpoint debugging

---

### Network Simulation (Latency/Packet Loss)

**Built-In Simulator:**
```csharp
// Add to NetworkManager
NetworkManager.Singleton.GetComponent<UnityTransport>().SetDebugSimulatorParameters(
    packetDelay: 100,        // 100ms latency
    packetJitter: 20,        // ±20ms variance
    packetDropRate: 5        // 5% packet loss
);
```

**Test Scenarios:**
| Scenario | Latency | Packet Loss | Represents |
|----------|---------|-------------|------------|
| **LAN** | 10ms | 0% | Same network |
| **Good Internet** | 50ms | 0% | Same region |
| **Average Internet** | 100ms | 1% | Cross-region |
| **Bad Internet** | 200ms | 5% | Mobile/WiFi |
| **Terrible** | 500ms | 10% | Stress test |

---

### Combat Interaction Testing

**Test Matrix: All 17 Interactions**

| Test # | Player A | Player B | Expected Result |
|--------|----------|----------|-----------------|
| 1 | Attack | Defense | A stunned, B blocks |
| 2 | Attack | Counter | A knocked down, reflection |
| 3 | Smash | Defense | B knocked down, 75% damage |
| 4 | Smash | Counter | A knocked down, reflection |
| 5 | Windmill | Defense | B blocks, no damage |
| 6 | Windmill | Counter | B knocked down, damage |
| 7 | RangedAttack | Defense | B blocks, 50% damage |
| 8 | RangedAttack | Counter | A gets arrow reflected |
| 9 | Attack | Attack | Speed resolution |
| 10 | Smash | Smash | Speed resolution |
| 11 | RangedAttack | RangedAttack | Simultaneous |
| 12 | Attack | Smash | Speed resolution |
| 13 | Attack | Windmill | Speed resolution |
| 14 | Smash | Windmill | Speed resolution |
| ... | ... | ... | ... |

**Validation:**
```
For each test:
1. Player A executes skill
2. Player B executes skill within window
3. Log server resolution
4. Verify both clients see same result
5. Check health/stamina values match
6. Check status effects match
```

---

### Performance Benchmarking

**Metrics to Track:**
- **Network Traffic:** Bytes sent/received per second
- **Update Frequency:** NetworkVariable updates/second
- **RPC Calls:** ServerRpc/ClientRpc calls/second
- **Frame Rate:** FPS on both server and clients
- **Latency:** Perceived input delay

**Target Values:**
| Metric | Target | Max Acceptable |
|--------|--------|----------------|
| Network Traffic | <50 KB/s | <100 KB/s |
| NetworkVariable Updates | <30/s per player | <60/s |
| RPC Calls | <20/s per player | <50/s |
| Frame Rate | 60 FPS | 30 FPS |
| Perceived Latency | <100ms | <200ms |

---

### Edge Case Testing

**Scenarios:**
1. **Disconnect During Skill**
   - Player executes skill, disconnects mid-execution
   - Expected: Server completes skill, other players see result

2. **Reconnect**
   - Player disconnects, reconnects
   - Expected: State restores correctly

3. **Host Migration** (if implemented)
   - Host quits, new host elected
   - Expected: Game continues, no state loss

4. **Rapid Input**
   - Player mashes skill keys
   - Expected: Server validates, rejects invalid inputs

5. **Simultaneous Targeting**
   - 3 players target same enemy
   - Expected: Combat resolves correctly, no race conditions

6. **Out-of-Range Skills**
   - Player tries to attack out-of-range target
   - Expected: Server rejects, client corrects

---

## Reference Architecture

### Data Flow Diagram

```
┌───────────────────────────────────────────────────────────────┐
│                      MULTIPLAYER COMBAT FLOW                  │
└───────────────────────────────────────────────────────────────┘

CLIENT 1 (Attacker)                                 CLIENT 2 (Defender)
       │                                                   │
       │ [1] Press Attack                                 │ [1] Press Defense
       │                                                   │
       │ [2] Instant: Animation                           │ [2] Instant: Animation
       │                                                   │
       ├──────────── [3] ExecuteSkillServerRpc ──────────>│
       │                        ↓                          │
       │                   ┌─────────┐                    │
       │                   │  SERVER │                    │
       │                   └─────────┘                    │
       │                        │                          │
       │             [4] Validate both skills             │
       │                        │                          │
       │             [5] CombatInteractionManager         │
       │                  .ProcessSkillExecution()        │
       │                        │                          │
       │             [6] DetermineInteraction()           │
       │                  (Attack vs Defense)             │
       │                        │                          │
       │             [7] Result: AttackerStunned          │
       │                        │                          │
       │             [8] Apply effects:                   │
       │                  - Stun player 1                 │
       │                  - Stun player 2 (50%)           │
       │                        │                          │
       │    <──── [9] InteractionResolvedClientRpc ────────
       │                        │                          │
       │ [10] Play stun effect                            │ [10] Play block effect
       │ [11] Update UI                                   │ [11] Update UI
       │                                                   │

ALL CLIENTS SEE SYNCHRONIZED RESULT
```

### Component Interaction Map

```
┌─────────────────────────────────────────────────────────────────┐
│                        PLAYER GAMEOBJECT                        │
│                         (NetworkObject)                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐         ┌──────────────────────┐        │
│  │ CombatController │◄────────┤ SkillSystem          │        │
│  │ (NetworkBehaviour)│         │ (NetworkBehaviour)   │        │
│  │                   │         │                       │        │
│  │ - Target          │         │ - CurrentSkill       │        │
│  │ - IsInCombat      │         │ - CurrentState       │        │
│  │                   │         │ - ChargeProgress     │        │
│  └─────────┬─────────┘         └──────────┬───────────┘        │
│            │                              │                     │
│            │                              │                     │
│  ┌─────────▼─────────┐         ┌─────────▼────────────┐       │
│  │ HealthSystem      │         │ StaminaSystem        │       │
│  │ (NetworkBehaviour)│         │ (NetworkBehaviour)   │       │
│  │                   │         │                       │       │
│  │ - Health          │         │ - Stamina            │       │
│  │ - MaxHealth       │         │ - IsResting          │       │
│  │ - IsDead          │         │                       │       │
│  └───────────────────┘         └──────────────────────┘       │
│                                                                 │
│  ┌───────────────────────────────────────────────────┐        │
│  │ MovementController (NetworkBehaviour)              │        │
│  │ - NetworkPosition                                 │        │
│  │ - NetworkRotation                                 │        │
│  │ - ClientPrediction                                │        │
│  └───────────────────────────────────────────────────┘        │
│                                                                 │
│  ┌───────────────────────────────────────────────────┐        │
│  │ StatusEffectManager (NetworkBehaviour)             │        │
│  │ - ActiveEffects (NetworkList)                     │        │
│  │ - IsStunned                                        │        │
│  │ - IsKnockedDown                                    │        │
│  └───────────────────────────────────────────────────┘        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Communicates via
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│         CombatInteractionManager (Singleton NetworkBehaviour)    │
│                                                                  │
│  - ProcessSkillExecution()  [ServerRpc]                         │
│  - DetermineInteraction()                                       │
│  - ProcessInteractionEffects()                                  │
│  - BroadcastResults()  [ClientRpc]                              │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Authority Responsibility Chart

| Component | Input | State | Logic | Visuals |
|-----------|-------|-------|-------|---------|
| **SkillSystem** | Owner | Server | Server | All Clients |
| **MovementController** | Owner | Server+Owner | Owner (predicted) | All Clients |
| **HealthSystem** | N/A | Server | Server | All Clients |
| **StaminaSystem** | N/A | Server | Server | All Clients |
| **StatusEffectManager** | N/A | Server | Server | All Clients |
| **CombatInteractionManager** | N/A | Server | Server | All Clients |
| **GameManager** | Server | Server | Server | All Clients |

**Legend:**
- **Owner:** Client that owns the NetworkObject
- **Server:** Server/host
- **All Clients:** Every connected client (including server-host)

---

## Appendices

### Appendix A: Quick Reference Tables

#### Unity Netcode API Quick Reference

| Function | Purpose | Usage |
|----------|---------|-------|
| `IsServer` | Check if running on server | `if (IsServer) { ... }` |
| `IsClient` | Check if running on client | `if (IsClient) { ... }` |
| `IsOwner` | Check if local player owns this object | `if (IsOwner) { ... }` |
| `IsHost` | Check if server is also client | `if (IsHost) { ... }` |
| `[ServerRpc]` | Client → Server RPC | `[ServerRpc] void DoSomethingServerRpc()` |
| `[ClientRpc]` | Server → All Clients RPC | `[ClientRpc] void DoSomethingClientRpc()` |
| `NetworkVariable<T>` | Auto-synced variable | `NetworkVariable<int> health = new()` |
| `NetworkList<T>` | Auto-synced list | `NetworkList<StatusEffect> effects` |
| `NetworkObject` | Network identity component | `GetComponent<NetworkObject>()` |
| `NetworkObjectReference` | Reference to network object | `new NetworkObjectReference(netObj)` |
| `OnNetworkSpawn()` | Called when object spawns | `override void OnNetworkSpawn()` |
| `OnNetworkDespawn()` | Called when object despawns | `override void OnNetworkDespawn()` |

---

#### NetworkVariable Read/Write Permissions

| Permission | Description | Use Case |
|------------|-------------|----------|
| `Server` | Only server can write | Health, damage, authoritative state |
| `Owner` | Only owner client can write | Player input, client prediction |
| `Everyone` | Anyone can write (not recommended) | Rarely used, avoid |

---

#### Common Networking Patterns

| Pattern | When to Use | Example |
|---------|-------------|---------|
| **Server Authority** | Game state, damage, rewards | Health system |
| **Client Prediction** | Instant feedback for owner | Movement |
| **Interpolation** | Smooth remote players | Movement of others |
| **Broadcast Events** | Visual effects, audio | Hit VFX |
| **State Sync** | Persistent values | Health, stamina |
| **RPC** | One-time actions | Skill execution |

---

### Appendix B: Glossary

**Authority:** Who has control over a piece of game state (usually server).

**Client-Side Prediction:** Technique where client simulates action instantly, then corrects if server disagrees.

**ClientRpc:** Remote Procedure Call from server to all clients.

**Desync:** When clients have different game state than server (bad!).

**Host:** Server that is also a player (client and server in one).

**Interpolation:** Smoothly moving remote objects between network updates.

**Latency:** Network delay (ping). Time for data to travel client ↔ server.

**NetworkBehaviour:** Unity Netcode component (like MonoBehaviour but networked).

**NetworkObject:** Component that gives GameObject a network identity.

**NetworkVariable:** Variable that auto-synchronizes across network.

**Ownership:** Which client "owns" a NetworkObject (usually the player controlling it).

**Packet Loss:** When network data fails to arrive (simulate for testing).

**RPC (Remote Procedure Call):** Function call that executes on another machine.

**Server-Authoritative:** Server makes final decisions (prevents cheating).

**ServerRpc:** Remote Procedure Call from client to server.

**Simultaneity Window:** Time window for actions to be considered simultaneous.

**Tick:** Fixed timestep for network updates (e.g., 60 ticks/second).

---

### Appendix C: Troubleshooting

**Problem:** "Cannot find type NetworkBehaviour"
**Solution:** Add `using Unity.Netcode;` at top of file.

---

**Problem:** "NetworkVariable cannot be modified outside of ownership"
**Solution:** Add `if (!IsServer) return;` before modifying server-authoritative variables.

---

**Problem:** Players see different health values
**Solution:** Ensure health is NetworkVariable and only modified by server.

---

**Problem:** Skill executes twice
**Solution:** Check IsOwner before executing locally, only send ServerRpc once.

---

**Problem:** Remote players rubber-band (jump around)
**Solution:** Increase interpolation speed or reduce NetworkVariable update frequency.

---

**Problem:** Combat interactions don't resolve
**Solution:** Check CombatInteractionManager runs on server, uses network time.

---

**Problem:** RangedAttack hit/miss different on clients
**Solution:** Use server-rolled RNG or seeded NetworkRandom.

---

**Problem:** "Failed to spawn NetworkObject"
**Solution:** Ensure prefab is in NetworkManager's prefabs list.

---

### Appendix D: Performance Optimization Tips

1. **Reduce NetworkVariable Update Frequency**
   ```csharp
   // Don't update every frame if unnecessary
   if (Time.time - lastUpdateTime > 0.1f) // Update 10x/second instead of 60
   {
       networkPosition.Value = transform.position;
       lastUpdateTime = Time.time;
   }
   ```

2. **Batch RPCs**
   ```csharp
   // Bad: Multiple RPCs
   PlayHitSoundClientRpc();
   SpawnHitVFXClientRpc();
   UpdateHealthUIClientRpc();

   // Good: One RPC
   OnDamageTakenClientRpc(damage, hitPosition);
   // Inside: Play sound, spawn VFX, update UI
   ```

3. **Use NetworkList Sparingly**
   ```csharp
   // NetworkList is expensive - use for small collections only
   // If you have 100+ items, consider custom serialization
   ```

4. **Compress Data**
   ```csharp
   // Use smallest data type needed
   NetworkVariable<byte> health; // 0-255 (1 byte)
   // vs
   NetworkVariable<int> health;  // (4 bytes)
   ```

5. **Cull Irrelevant Updates**
   ```csharp
   // Don't send updates to clients who can't see this object
   // Use interest management / relevance system
   ```

---

### Appendix E: Resources

**Official Unity Netcode:**
- [Documentation](https://docs-multiplayer.unity3d.com/)
- [API Reference](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest)
- [Unity Learn Tutorials](https://learn.unity.com/search?k=%5B%22q%3ANetcode%22%5D)
- [GitHub](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- [Discord](https://discord.gg/unity)

**Community Resources:**
- [Unity Forums - Multiplayer](https://forum.unity.com/forums/multiplayer.26/)
- [r/unity3d](https://www.reddit.com/r/Unity3D/)
- [Stack Overflow - unity-netcode](https://stackoverflow.com/questions/tagged/unity-netcode)

**Recommended YouTube Channels:**
- [Dapper Dino](https://www.youtube.com/@DapperDino) - Unity Netcode tutorials
- [Code Monkey](https://www.youtube.com/@CodeMonkeyUnity) - Game dev tutorials
- [Game Dev Guide](https://www.youtube.com/@GameDevGuide) - Advanced networking

---

## Conclusion

Your FairyGate combat system is **well-positioned for multiplayer** due to:
- ✅ Component-based architecture
- ✅ Separated input handling
- ✅ Event-driven communication
- ✅ Deterministic combat logic

**Primary Changes Needed:**
1. Input abstraction (IInputProvider)
2. State synchronization (NetworkVariables)
3. Authority validation (IsServer checks)
4. Combat interaction network sync
5. Deterministic RNG

**Estimated Effort:** 45-70 hours over 6-9 weeks part-time

**Recommended Approach:**
1. Install Unity Netcode for GameObjects
2. Follow phased roadmap (Foundation → Movement → Combat → Polish)
3. Test extensively at each phase
4. Use client-side prediction for responsive feel

**Key Insight:** Your rock-paper-scissors system is **perfect** for server-authoritative multiplayer. The timing-based resolution maps naturally to network architecture.

---

**Good luck with your multiplayer implementation!** 🎮🌐

This guide will serve as your reference throughout the process. Return to it whenever you need patterns, examples, or troubleshooting tips.

**Next Step:** Start with Phase 1 (Foundation) when you're ready to begin multiplayer development.

---

**End of Multiplayer Implementation Guide**
