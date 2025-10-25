using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FairyGate.Combat
{
    public interface IStatusEffectTarget
    {
        List<StatusEffect> ActiveStatusEffects { get; }
        bool HasStatusEffect(StatusEffectType type);

        void ApplyStatusEffect(StatusEffect effect);
        void RemoveStatusEffect(StatusEffectType type);
        void ClearAllStatusEffects();

        // Events - implemented as fields in concrete classes
    }

    [System.Serializable]
    public class StatusEffect
    {
        public StatusEffectType type;
        public float duration;
        public float remainingTime;
        public bool isActive;

        // Displacement fields for knockdown effects
        public Vector3 displacementVector;
        public bool hasDisplacement;

        public StatusEffect(StatusEffectType effectType, float effectDuration)
        {
            type = effectType;
            duration = effectDuration;
            remainingTime = effectDuration;
            isActive = true;
            displacementVector = Vector3.zero;
            hasDisplacement = false;
        }

        public StatusEffect(StatusEffectType effectType, float effectDuration, Vector3 displacement)
        {
            type = effectType;
            duration = effectDuration;
            remainingTime = effectDuration;
            isActive = true;
            displacementVector = displacement;
            hasDisplacement = displacement != Vector3.zero;
        }

        public void UpdateEffect(float deltaTime)
        {
            if (isActive)
            {
                remainingTime -= deltaTime;
                if (remainingTime <= 0f)
                {
                    isActive = false;
                }
            }
        }
    }
}