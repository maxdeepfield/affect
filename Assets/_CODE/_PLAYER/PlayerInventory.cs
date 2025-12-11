using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Keycard")]
    [SerializeField, Min(0)] private int keycardLevel = 0;

    public UnityEvent<int> OnKeycardLevelChanged = new UnityEvent<int>();
    [System.Obsolete("Use OnKeycardLevelChanged")]
    public UnityEvent<HashSet<int>> OnKeycardsChanged = new UnityEvent<HashSet<int>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PlayerInventory instances found. Using the first one.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int KeycardLevel => keycardLevel;

    /// <summary>
    /// Upgrades the single keycard to the provided stage if it is higher than the current stage.
    /// </summary>
    public void UpgradeKeycard(int stage)
    {
        int newLevel = Mathf.Max(0, stage);
        if (newLevel <= keycardLevel)
            return;

        keycardLevel = newLevel;
        InvokeKeycardEvents();
    }

    /// <summary>
    /// Compatibility helper for older calls. Delegates to UpgradeKeycard.
    /// </summary>
    public void AddKeycard(int stage) => UpgradeKeycard(stage);

    /// <summary>
    /// Returns true if the player holds a keycard of at least the required stage.
    /// </summary>
    public bool HasKeycard(int stage)
    {
        return keycardLevel >= stage;
    }

    /// <summary>
    /// Legacy compatibility: returns a set containing only the current keycard level (or empty if none).
    /// </summary>
    public HashSet<int> GetCollectedKeycards()
    {
        HashSet<int> result = new HashSet<int>();
        if (keycardLevel > 0)
        {
            result.Add(keycardLevel);
        }
        return result;
    }

    public void ClearInventory()
    {
        if (keycardLevel == 0)
            return;

        keycardLevel = 0;
        InvokeKeycardEvents();
    }

    private void InvokeKeycardEvents()
    {
        OnKeycardLevelChanged.Invoke(keycardLevel);
        OnKeycardsChanged.Invoke(GetCollectedKeycards());
    }
}
