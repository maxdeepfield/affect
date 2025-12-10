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
        PlayerInventory inventory = FindPlayerInventory();
        if (inventory != null)
        {
            inventory.AddKeycard(stage);
        }

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }

    private PlayerInventory FindPlayerInventory()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.GetComponent<PlayerInventory>();
        }
        return null;
    }
}