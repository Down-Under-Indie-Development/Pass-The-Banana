using UnityEditor.EditorTools;
using UnityEngine;

namespace HealthSystem
{
    public class Health : MonoBehaviour, IDamageable
    {
        [field: Header("Health Settings")]
        [field: SerializeField] public float maxHealth { get; protected set; } = 100f;
        public float currentHealth { get; protected set; }

        [field: Header("Damage Settings")]
        [field: SerializeField] public float damage { get; protected set; } = 1f;

        private void Start()
        {
            currentHealth = maxHealth;

        }


        public void Damage(float damage)
        {
            if (currentHealth <= 0) Die();
            currentHealth -= damage;

        }

        public void Die()
        {
            // GUARD: Prevent negatives
            currentHealth = 0;

            Destroy(gameObject);


        }

    }
}