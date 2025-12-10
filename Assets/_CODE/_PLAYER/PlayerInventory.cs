using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private HashSet<int> collectedKeycards = new HashSet<int>();

    public UnityEvent<HashSet<int>> OnKeycardsChanged = new UnityEvent<HashSet<int>>();

    public void AddKeycard(int stage)
    {
        if (collectedKeycards.Add(stage))
        {
            // Only notify if it was actually added (not already collected)
            OnKeycardsChanged.Invoke(collectedKeycards);
        }
    }

    public bool HasKeycard(int stage)
    {
        return collectedKeycards.Contains(stage);
    }

    public HashSet<int> GetCollectedKeycards()
    {
        return new HashSet<int>(collectedKeycards);
    }

    public void ClearInventory()
    {
        collectedKeycards.Clear();
        OnKeycardsChanged.Invoke(collectedKeycards);
    }
}