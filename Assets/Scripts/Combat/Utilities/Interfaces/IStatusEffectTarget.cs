using System.Collections.Generic;
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

        public StatusEffect(StatusEffectType effectType, float effectDuration)
        {
            type = effectType;
            duration = effectDuration;
            remainingTime = effectDuration;
            isActive = true;
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