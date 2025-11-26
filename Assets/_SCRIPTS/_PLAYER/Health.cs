using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        ApplyDamage(amount);
    }

    // Returns true if this damage instance killed the target
    public bool ApplyDamage(float amount)
    {
        if (IsDead) return false;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}");
        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        return false;
    }

    private void Die()
    {
        IsDead = true;
        // Here you can add logic for what happens when the object dies.
        // For example, destroy the object.
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject);
    }
}
