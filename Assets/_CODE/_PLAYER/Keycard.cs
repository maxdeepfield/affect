using UnityEngine;

public class Keycard : MonoBehaviour
{
    [SerializeField] private int stage = 1;
    [SerializeField] private bool destroyOnCollect = true;

    /// <summary>
    /// Call this from Usable.OnUse (no parameters).
    /// </summary>
    public void Collect()
    {
        PlayerInventory inventory = ResolveInventory();
        if (inventory != null)
        {
            inventory.UpgradeKeycard(stage);
        }
        else
        {
            Debug.LogWarning($"Keycard stage {stage} collected but no PlayerInventory found in the scene.");
        }

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }

    private PlayerInventory ResolveInventory()
    {
        if (PlayerInventory.Instance != null)
            return PlayerInventory.Instance;

        PlayerInventory inventory = FindPlayerInventoryByTag();
        if (inventory != null)
            return inventory;

        return FindObjectOfType<PlayerInventory>();
    }

    private PlayerInventory FindPlayerInventoryByTag()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.GetComponent<PlayerInventory>();
        }
        return null;
    }
}
