using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FairyGate.Combat
{
    public class StatusEffectManager : MonoBehaviour, IStatusEffectTarget
    {
        [Header("Status Effects")]
        [SerializeField] private List<StatusEffect> activeStatusEffects = new List<StatusEffect>();
        [SerializeField] private CharacterStats characterStats;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showStatusEffectGUI = true;

        [Header("Events")]
        public UnityEvent<StatusEffect> OnStatusEffectApplied = new UnityEvent<StatusEffect>();
        public UnityEvent<StatusEffectType> OnStatusEffectRemoved = new UnityEvent<StatusEffectType>();
        public UnityEvent<StatusEffectType> OnStatusEffectExpired = new UnityEvent<StatusEffectType>();

        private MovementController movementController;
        private SkillSystem skillSystem;

        public List<StatusEffect> ActiveStatusEffects => activeStatusEffects;

        // Status effect queries
        public bool IsStunned => HasStatusEffect(StatusEffectType.Stun);
        public bool IsKnockedDown => HasStatusEffect(StatusEffectType.InteractionKnockdown) || HasStatusEffect(StatusEffectType.MeterKnockdown);
        public bool IsResting => HasStatusEffect(StatusEffectType.Rest);
        public bool CanMove => !IsStunned && !IsKnockedDown;
        public bool CanAct => !IsKnockedDown; // Can charge skills while stunned
        public StatusEffectType CurrentPrimaryEffect => GetPrimaryStatusEffect();

        private void Awake()
        {
            movementController = GetComponent<MovementController>();
            skillSystem = GetComponent<SkillSystem>();

            if (characterStats == null)
            {
                Debug.LogWarning($"StatusEffectManager on {gameObject.name} has no CharacterStats assigned. Using default values.");
                characterStats = CharacterStats.CreateDefaultStats();
            }
        }

        private void Update()
        {
            UpdateStatusEffects();
        }

        public bool HasStatusEffect(StatusEffectType type)
        {
            return activeStatusEffects.Any(effect => effect.type == type && effect.isActive);
        }

        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (effect == null) return;

            // Handle status effect stacking rules
            ProcessStatusEffectStacking(effect);

            // Apply the effect
            var existingEffect = activeStatusEffects.FirstOrDefault(e => e.type == effect.type);
            if (existingEffect != null)
            {
                // Reset duration for existing effect (don't extend)
                existingEffect.remainingTime = effect.duration;
                existingEffect.duration = effect.duration;
                existingEffect.isActive = true;

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} status effect {effect.type} duration reset to {effect.duration:F1}s");
                }
            }
            else
            {
                // Add new effect
                activeStatusEffects.Add(effect);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} received status effect: {effect.type} for {effect.duration:F1}s");
                }
            }

            OnStatusEffectApplied.Invoke(effect);
            UpdateMovementRestrictions();

            // Apply physical displacement if this effect has it
            if (effect.hasDisplacement)
            {
                ApplyPhysicalDisplacement(effect.displacementVector);
            }
        }

        public void RemoveStatusEffect(StatusEffectType type)
        {
            var effect = activeStatusEffects.FirstOrDefault(e => e.type == type);
            if (effect != null)
            {
                effect.isActive = false;
                OnStatusEffectRemoved.Invoke(type);
                UpdateMovementRestrictions();

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} status effect removed: {type}");
                }
            }
        }

        public void ClearAllStatusEffects()
        {
            foreach (var effect in activeStatusEffects.ToList())
            {
                if (effect.isActive)
                {
                    RemoveStatusEffect(effect.type);
                }
            }

            activeStatusEffects.Clear();
            UpdateMovementRestrictions();

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} all status effects cleared");
            }
        }

        private void UpdateStatusEffects()
        {
            for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeStatusEffects[i];
                if (effect.isActive)
                {
                    effect.UpdateEffect(Time.deltaTime);

                    if (!effect.isActive) // Effect expired
                    {
                        OnStatusEffectExpired.Invoke(effect.type);
                        OnStatusEffectRemoved.Invoke(effect.type);

                        if (enableDebugLogs)
                        {
                            Debug.Log($"{gameObject.name} status effect expired: {effect.type}");
                        }

                        UpdateMovementRestrictions();
                    }
                }
                else
                {
                    activeStatusEffects.RemoveAt(i);
                }
            }
        }

        private void ProcessStatusEffectStacking(StatusEffect newEffect)
        {
            switch (newEffect.type)
            {
                case StatusEffectType.Stun:
                    if (IsKnockedDown)
                    {
                        // Cannot be stunned while knocked down
                        if (enableDebugLogs)
                        {
                            Debug.Log($"{gameObject.name} cannot be stunned while knocked down");
                        }
                        return;
                    }
                    break;

                case StatusEffectType.InteractionKnockdown:
                case StatusEffectType.MeterKnockdown:
                    if (IsStunned)
                    {
                        // Knockdown overrides stun, cancel stun effect
                        RemoveStatusEffect(StatusEffectType.Stun);

                        if (enableDebugLogs)
                        {
                            Debug.Log($"{gameObject.name} knockdown overrides stun effect");
                        }
                    }
                    break;
            }
        }

        private StatusEffectType GetPrimaryStatusEffect()
        {
            // Priority Order: Knockdown > Stun > Rest > None
            if (IsKnockedDown)
            {
                if (HasStatusEffect(StatusEffectType.InteractionKnockdown))
                    return StatusEffectType.InteractionKnockdown;
                if (HasStatusEffect(StatusEffectType.MeterKnockdown))
                    return StatusEffectType.MeterKnockdown;
            }

            if (IsStunned) return StatusEffectType.Stun;
            if (IsResting) return StatusEffectType.Rest;

            return StatusEffectType.None;
        }

        private void UpdateMovementRestrictions()
        {
            if (movementController != null)
            {
                movementController.SetCanMove(CanMove);
            }

            if (skillSystem != null)
            {
                skillSystem.SetCanAct(CanAct);
            }
        }

        public float GetRemainingDuration(StatusEffectType type)
        {
            var effect = activeStatusEffects.FirstOrDefault(e => e.type == type && e.isActive);
            return effect?.remainingTime ?? 0f;
        }

        public void ExtendStatusEffect(StatusEffectType type, float additionalTime)
        {
            var effect = activeStatusEffects.FirstOrDefault(e => e.type == type && e.isActive);
            if (effect != null)
            {
                effect.remainingTime += additionalTime;
                effect.duration += additionalTime;

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} status effect {type} extended by {additionalTime:F1}s (remaining: {effect.remainingTime:F1}s)");
                }
            }
        }

        // GUI Debug Display
        private void OnGUI()
        {
            if (showStatusEffectGUI && Application.isPlaying && activeStatusEffects.Any(e => e.isActive))
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
                screenPos.y = Screen.height - screenPos.y;

                string statusText = "";
                foreach (var effect in activeStatusEffects.Where(e => e.isActive))
                {
                    statusText += $"{effect.type}: {effect.remainingTime:F1}s\n";
                }

                GUI.Label(new Rect(screenPos.x - 60, screenPos.y, 120, 80), statusText);
            }
        }

        // Helper methods for common status effects
        public void ApplyStun(float duration)
        {
            float modifiedDuration = DamageCalculator.CalculateStunDuration(duration, characterStats);
            ApplyStatusEffect(new StatusEffect(StatusEffectType.Stun, modifiedDuration));
        }

        public void ApplyInteractionKnockdown()
        {
            float duration = DamageCalculator.CalculateKnockdownDuration(characterStats);
            ApplyStatusEffect(new StatusEffect(StatusEffectType.InteractionKnockdown, duration));
        }

        public void ApplyInteractionKnockdown(Vector3 displacement)
        {
            float duration = DamageCalculator.CalculateKnockdownDuration(characterStats);
            ApplyStatusEffect(new StatusEffect(StatusEffectType.InteractionKnockdown, duration, displacement));
        }

        public void ApplyMeterKnockdown()
        {
            float duration = DamageCalculator.CalculateKnockdownDuration(characterStats);
            ApplyStatusEffect(new StatusEffect(StatusEffectType.MeterKnockdown, duration));
        }

        public void ApplyMeterKnockdown(Vector3 displacement)
        {
            float duration = DamageCalculator.CalculateKnockdownDuration(characterStats);
            ApplyStatusEffect(new StatusEffect(StatusEffectType.MeterKnockdown, duration, displacement));
        }

        public void ApplyRest()
        {
            ApplyStatusEffect(new StatusEffect(StatusEffectType.Rest, float.MaxValue)); // Indefinite until cancelled
        }

        private void ApplyPhysicalDisplacement(Vector3 displacementVector)
        {
            var characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                // Apply displacement using CharacterController.Move for proper collision
                characterController.Move(displacementVector);

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} displaced by {displacementVector}");
                }
            }
            else
            {
                // Fallback to transform movement if no CharacterController
                transform.position += displacementVector;

                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} displaced by {displacementVector} (transform fallback)");
                }
            }
        }
    }
}