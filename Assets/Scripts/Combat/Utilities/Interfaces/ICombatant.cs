using UnityEngine;
using UnityEngine.Events;

namespace FairyGate.Combat
{
    public interface ICombatant : IDamageable, ISkillExecutor, IStatusEffectTarget
    {
        CharacterStats Stats { get; }
        Transform WeaponTransform { get; } // Changed from WeaponController to avoid circular dependency
        Transform Transform { get; }

        bool IsInCombat { get; }
        Transform CurrentTarget { get; }

        void EnterCombat(Transform target);
        void ExitCombat();
        void SetTarget(Transform target);

        bool IsInRangeOf(Transform target);
        float GetDistanceTo(Transform target);

        // Events - implemented as fields in concrete classes
    }
}