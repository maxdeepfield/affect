using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public event Action<float, float> HealthChanged;

    private float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        NotifyHealthChanged();
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
        NotifyHealthChanged();
        Debug.Log($"{gameObject.name} took {amount} damage. Current health: {currentHealth}");
        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        return false;
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
        NotifyHealthChanged();
        Debug.Log($"{gameObject.name} healed by {amount}. Current health: {currentHealth}");
    }

    public void SetMaxHealth(float value, bool fill = false)
    {
        maxHealth = Mathf.Max(1f, value);
        if (fill || currentHealth > maxHealth)
            currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void Die()
    {
        IsDead = true;
        NotifyHealthChanged();
        // Here you can add logic for what happens when the object dies.
        // For example, destroy the object.
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
