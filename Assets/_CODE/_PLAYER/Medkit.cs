using UnityEngine;

public class Medkit : MonoBehaviour
{
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private bool destroyOnUse = true;
    [Tooltip("Optional explicit target to heal. If null, tries to find the player by tag 'Player'.")]
    [SerializeField] private Health targetOverride;

    /// <summary>
    /// Call this from Usable.OnUse (no parameters).
    /// </summary>
    public void Heal()
    {
        HealTarget(GetTarget());
    }

    /// <summary>
    /// Heals a specific target (for code-driven use).
    /// </summary>
    public void HealTarget(Health target)
    {
        if (target == null) return;

        target.Heal(healAmount);
        if (destroyOnUse)
        {
            Destroy(gameObject);
        }
    }

    public void SetTarget(Health health)
    {
        targetOverride = health;
    }

    private Health GetTarget()
    {
        if (targetOverride != null)
            return targetOverride;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent(out Health h))
            return h;

        return null;
    }
}
