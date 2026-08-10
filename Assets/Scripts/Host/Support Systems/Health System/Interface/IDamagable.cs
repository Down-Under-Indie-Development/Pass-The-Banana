using UnityEngine;

namespace HealthSystem
{
    public interface IDamageable
    {
        float maxHealth { get; }
        float currentHealth { get; }
        float damage { get; }
        void Damage(float damage);
        void Die();

    }
}