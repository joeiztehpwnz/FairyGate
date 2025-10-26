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
            }
        }

        public void AddMeterBuildup(int attackDamage, CharacterStats attackerStats, Transform attacker = null)
        {
            // Store the last attacker for displacement calculation
            if (attacker != null)
            {
                lastAttacker = attacker;
            }

            // Calculate buildup: Base + (Attacker Strength / 10) - (Defender Focus / 30)
            float buildup = CombatConstants.ATTACK_KNOCKDOWN_BUILDUP;
            buildup += (attackerStats.strength / CombatConstants.STRENGTH_KNOCKDOWN_DIVISOR);
            buildup -= (characterStats.focus / CombatConstants.FOCUS_STATUS_RECOVERY_DIVISOR);

            // Ensure minimum buildup of 1
            buildup = Mathf.Max(1f, buildup);

            AddToMeter(buildup);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} knockdown meter: +{buildup:F1} (total: {currentMeter:F1}/{maxMeter})");
            }
        }

        public void AddToMeter(float amount)
        {
            if (amount <= 0) return;

            float oldMeter = currentMeter;
            currentMeter = Mathf.Min(maxMeter, currentMeter + amount);

            OnMeterChanged?.Invoke(currentMeter, maxMeter);

            // Check for knockdown threshold
            if (currentMeter >= maxMeter && oldMeter < maxMeter)
            {
                TriggerMeterKnockdown();
            }
        }

        public void TriggerMeterKnockdown()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} knockdown meter reached threshold! Triggering meter knockdown.");
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

        private void OnValidate()
        {
            currentMeter = Mathf.Clamp(currentMeter, 0f, maxMeter);
        }

        // Debug visualization
        private void OnGUI()
        {
            if (enableDebugLogs && Application.isPlaying)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
                screenPos.y = Screen.height - screenPos.y; // Flip Y coordinate

                GUI.Label(new Rect(screenPos.x - 50, screenPos.y, 100, 20),
                    $"Meter: {currentMeter:F1}/{maxMeter:F0}");
            }
        }
    }
}