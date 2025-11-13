using System;
using UnityEngine;

namespace FairyGate.Combat
{
    public class KnockdownMeterTracker : MonoBehaviour, ICombatUpdatable
    {
        [Header("Knockdown Meter")]
        [SerializeField] private float currentMeter = 0f;
        [SerializeField] private float maxMeter = CombatConstants.KNOCKDOWN_METER_THRESHOLD;
        [SerializeField] private float decayRate = CombatConstants.KNOCKDOWN_METER_DECAY_RATE;

        [Header("Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private bool enableDebugLogs = true;

        private Transform lastAttacker;
        private bool hasTriggeredKnockback = false;

        // Classic Mabinogi: Combo tracking for diminishing returns
        private int currentComboHitNumber = 0;
        private float lastHitTime = -999f;
        private const float COMBO_RESET_TIME = 2.0f; // Reset combo after 2s without hits

        // C# Events (replaces UnityEvents for performance)
        public event Action<float, float> OnMeterChanged; // current, max
        public event Action OnMeterKnockdownTriggered;

        private StatusEffectManager statusEffectManager;

        public float CurrentMeter => currentMeter;
        public float MaxMeter => maxMeter;
        public float MeterPercentage => currentMeter / maxMeter;
        public bool IsAtThreshold => currentMeter >= maxMeter;

        private void Awake()
        {
            statusEffectManager = GetComponent<StatusEffectManager>();

            if (characterStats == null)
            {
                Debug.LogWarning($"KnockdownMeterTracker on {gameObject.name} has no CharacterStats assigned. Using default values.");
                characterStats = CharacterStats.CreateDefaultStats();
            }

            // Register with combat update manager
            CombatUpdateManager.Register(this);
        }

        private void OnDestroy()
        {
            // Unregister to prevent memory leaks
            CombatUpdateManager.Unregister(this);
        }

        // Renamed from Update() to CombatUpdate() for centralized update management
        public void CombatUpdate(float deltaTime)
        {
            // Continuous decay - never resets, only decays
            if (currentMeter > 0f)
            {
                float oldMeter = currentMeter;
                currentMeter = Mathf.Max(0f, currentMeter + (decayRate * deltaTime));

                if (!Mathf.Approximately(oldMeter, currentMeter))
                {
                    OnMeterChanged?.Invoke(currentMeter, maxMeter);
                }

                // Reset knockback flag if meter decays below threshold
                if (hasTriggeredKnockback && currentMeter < CombatConstants.KNOCKBACK_METER_THRESHOLD)
                {
                    hasTriggeredKnockback = false;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"{gameObject.name} knockback flag reset (meter below threshold)");
                    }
                }
            }

            // Classic Mabinogi: Reset combo counter after timeout
            if (currentComboHitNumber > 0 && Time.time - lastHitTime > COMBO_RESET_TIME)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[Classic Mabinogi] {gameObject.name} combo reset (timeout)");
                }
                currentComboHitNumber = 0;
            }
        }

        public void AddMeterBuildup(int attackDamage, CharacterStats attackerStats, Transform attacker = null)
        {
            // Store the last attacker for displacement calculation
            if (attacker != null)
            {
                lastAttacker = attacker;
            }

            // Classic Mabinogi: Track combo for diminishing returns
            float timeSinceLastHit = Time.time - lastHitTime;
            if (timeSinceLastHit > COMBO_RESET_TIME)
            {
                currentComboHitNumber = 0; // Reset combo if timeout occurred
            }

            currentComboHitNumber++; // Increment hit counter
            lastHitTime = Time.time; // Update last hit timestamp

            // Classic Mabinogi: Use flat diminishing returns values
            // Each successive hit applies less knockdown pressure to prevent spam
            float buildup = GetKnockdownBuildupValue(currentComboHitNumber);

            AddToMeter(buildup);

            if (enableDebugLogs)
            {
                Debug.Log($"[Classic Mabinogi] {gameObject.name} knockdown meter: +{buildup:F1} (hit #{currentComboHitNumber}) (total: {currentMeter:F1}/{maxMeter})");
            }
        }

        public void AddToMeter(float amount)
        {
            if (amount <= 0) return;

            float oldMeter = currentMeter;
            currentMeter = Mathf.Min(maxMeter, currentMeter + amount);

            OnMeterChanged?.Invoke(currentMeter, maxMeter);

            // Check for knockback threshold (50%) - triggers once per cycle
            if (!hasTriggeredKnockback && currentMeter >= CombatConstants.KNOCKBACK_METER_THRESHOLD && oldMeter < CombatConstants.KNOCKBACK_METER_THRESHOLD)
            {
                TriggerKnockback();
                hasTriggeredKnockback = true;
            }

            // Check for knockdown threshold (100%)
            if (currentMeter >= maxMeter && oldMeter < maxMeter)
            {
                TriggerMeterKnockdown();
            }
        }

        public void TriggerKnockback()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} knockback meter reached 50% threshold! Triggering knockback.");
            }

            // Apply knockback with displacement
            if (statusEffectManager != null)
            {
                Vector3 displacement = Vector3.zero;

                if (lastAttacker != null)
                {
                    // Calculate displacement away from the last attacker
                    Vector3 knockbackDirection = (transform.position - lastAttacker.position).normalized;
                    displacement = knockbackDirection * CombatConstants.KNOCKBACK_DISPLACEMENT_DISTANCE;
                }
                else
                {
                    // Default backward displacement if no attacker is known
                    displacement = -transform.forward * CombatConstants.KNOCKBACK_DISPLACEMENT_DISTANCE;
                }

                statusEffectManager.ApplyKnockback(displacement);
            }

            // Meter continues to accumulate after knockback
        }

        public void TriggerMeterKnockdown()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} knockdown meter reached 100% threshold! Triggering meter knockdown.");
            }

            // Apply meter-based knockdown with displacement
            if (statusEffectManager != null)
            {
                Vector3 displacement = Vector3.zero;

                if (lastAttacker != null)
                {
                    // Calculate displacement away from the last attacker
                    Vector3 knockbackDirection = (transform.position - lastAttacker.position).normalized;
                    displacement = knockbackDirection * CombatConstants.METER_KNOCKBACK_DISTANCE;
                }
                else
                {
                    // Default backward displacement if no attacker is known
                    displacement = -transform.forward * CombatConstants.METER_KNOCKBACK_DISTANCE;
                }

                statusEffectManager.ApplyMeterKnockdown(displacement);
            }

            OnMeterKnockdownTriggered?.Invoke();

            // IMPORTANT: Meter continues normal decay, does NOT reset to 0
            // This is explicitly stated in the specifications
        }

        public void SetMeter(float value)
        {
            float oldMeter = currentMeter;
            currentMeter = Mathf.Clamp(value, 0f, maxMeter);

            if (!Mathf.Approximately(oldMeter, currentMeter))
            {
                OnMeterChanged?.Invoke(currentMeter, maxMeter);

                // Check for threshold crossing
                if (currentMeter >= maxMeter && oldMeter < maxMeter)
                {
                    TriggerMeterKnockdown();
                }
            }
        }

        public void ResetMeterForTesting()
        {
            // Only for testing/debugging purposes
            // Normal gameplay should NEVER reset the meter
            currentMeter = 0f;
            OnMeterChanged?.Invoke(currentMeter, maxMeter);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} knockdown meter reset for testing purposes");
            }
        }

        // For Smash and Windmill skills that bypass the meter system entirely
        public void TriggerImmediateKnockdown(Vector3 displacement = default)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} received immediate knockdown (Smash/Windmill bypass)");
            }

            if (statusEffectManager != null)
            {
                if (displacement != Vector3.zero)
                {
                    statusEffectManager.ApplyInteractionKnockdown(displacement);
                }
                else
                {
                    statusEffectManager.ApplyInteractionKnockdown();
                }
            }

            // NOTE: Smash/Windmill do NOT affect the knockdown meter system at all
        }

        /// <summary>
        /// Classic Mabinogi: Diminishing returns on knockdown buildup to prevent spam.
        /// Each successive hit in a combo applies less knockdown pressure.
        /// Returns FLAT knockdown values (not multipliers).
        /// </summary>
        private float GetKnockdownBuildupValue(int hitNumber)
        {
            return hitNumber switch
            {
                1 => 30f,  // First hit: 30 knockdown buildup
                2 => 25f,  // Second hit: 25 knockdown buildup
                3 => 20f,  // Third hit: 20 knockdown buildup
                _ => 15f   // Subsequent hits: 15 knockdown buildup
            };
        }

        private void OnValidate()
        {
            currentMeter = Mathf.Clamp(currentMeter, 0f, maxMeter);
        }

        // OnGUI removed - now handled by CharacterInfoDisplay component
    }
}