using UnityEngine;
using UnityEngine.Events;

public class Usable : MonoBehaviour
{
    [SerializeField] private string prompt = "Press F";
    [SerializeField] private float useDistance = 3f;
    [SerializeField] private UnityEvent onUse = new UnityEvent();
    [SerializeField] private LayerMask raycastMask = ~0;

    private static Usable current;

    public string Prompt => prompt;
    public float UseDistance => useDistance;
    public static Usable Current => current;
    public LayerMask RaycastMask => raycastMask;

    public void SetPrompt(string text)
    {
        prompt = text;
    }

    public void SetDistance(float distance)
    {
        useDistance = Mathf.Max(0.1f, distance);
    }

    public void ClearListeners()
    {
        onUse.RemoveAllListeners();
    }

    public void AddListener(UnityAction action)
    {
        onUse.AddListener(action);
    }

    public bool CanUse(Vector3 fromPosition)
    {
        return Vector3.Distance(fromPosition, transform.position) <= useDistance;
    }

    public void Use()
    {
        onUse?.Invoke();
    }

    public static Usable CheckRaycast(Camera cam, float maxDistance)
    {
        current = null;
        if (cam == null) return null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            Usable usable = hit.collider.GetComponentInParent<Usable>();
            if (usable != null)
            {
                float reach = Mathf.Max(maxDistance, usable.UseDistance);
                if (hit.distance <= reach)
                {
                    current = usable;
                }
            }
        }

        return current;
    }

    public static bool TryUseCurrent()
    {
        if (current == null) return false;
        current.Use();
        return true;
    }
}
