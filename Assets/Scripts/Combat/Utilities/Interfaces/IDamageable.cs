using UnityEngine;
using UnityEngine.Events;

namespace FairyGate.Combat
{
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsAlive { get; }

        void TakeDamage(int damage, Transform source);
        void Die();

        // Events - implemented as fields in concrete classes
    }
}