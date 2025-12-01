using UnityEngine;

/// <summary>
/// Simple spider walker that works in editor without play mode.
/// Just drag the spider and it walks. That's it.
/// </summary>
[ExecuteAlways]
public class SimpleSpiderWalker : MonoBehaviour
{
    [SerializeField] private Transform[] legRoots;
    [SerializeField] private float stepDistance = 0.5f;
    [SerializeField] private float stepHeight = 0.15f;
    [SerializeField] private float stepSpeed = 5f;

    private Vector3 lastPos;
    private Vector3[] legTargets;
    private Vector3[] legPlanted;
    private bool[] legStepping;
    private float[] stepProgress;
    private int activeGroup = 0;

    private void OnEnable()
    {
        if (legRoots == null || legRoots.Length == 0)
            FindLegs();

        lastPos = transform.position;
        InitializeLegData();
    }

    private void FindLegs()
    {
        Transform legsContainer = transform.Find("Legs");
        if (legsContainer == null) return;

        legRoots = new Transform[legsContainer.childCount];
        for (int i = 0; i < legsContainer.childCount; i++)
        {
            legRoots[i] = legsContainer.GetChild(i);
        }
    }

    private void InitializeLegData()
    {
        if (legRoots == null || legRoots.Length == 0) return;

        int count = legRoots.Length;
        legTargets = new Vector3[count];
        legPlanted = new Vector3[count];
        legStepping = new bool[count];
        stepProgress = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (legRoots[i] != null)
            {
                Transform foot = legRoots[i].Find("Hip/Knee/Foot");
                if (foot == null) foot = legRoots[i].Find("Hip/Foot");
                if (foot == null) foot = legRoots[i].Find("Foot");

                if (foot != null)
                {
                    legPlanted[i] = foot.position;
                    legTargets[i] = foot.position;
                }
            }
        }
    }

    private void Update()
    {
        if (legRoots == null || legRoots.Length == 0) return;

        // Calculate movement
        Vector3 currentPos = transform.position;
        Vector3 movement = currentPos - lastPos;
        lastPos = currentPos;

        // Update legs
        UpdateLegs(movement);

        // Animate stepping legs
        AnimateLegs();

        // Apply IK
        ApplyIK();
    }

    private void UpdateLegs(Vector3 movement)
    {
        int count = legRoots.Length;

        // Check if legs need to step
        for (int i = 0; i < count; i++)
        {
            if (legStepping[i]) continue; // Already stepping

            Transform foot = GetFootTransform(i);
            if (foot == null) continue;

            float dist = Vector3.Distance(foot.position, legPlanted[i]);

            // If leg moved too far, step
            if (dist > stepDistance)
            {
                // Determine which group this leg is in
                int legGroup = i % 2;

                // Only step if this is the active group
                if (legGroup == activeGroup)
                {
                    legStepping[i] = true;
                    stepProgress[i] = 0f;

                    // Target is ahead of current position
                    Vector3 direction = movement.normalized;
                    if (direction.sqrMagnitude < 0.001f)
                        direction = transform.forward;

                    legTargets[i] = foot.position + direction * stepDistance * 0.5f;
                }
            }
        }

        // Check if we should switch groups
        bool anySteppingInGroup = false;
        for (int i = 0; i < count; i++)
        {
            if (legStepping[i] && (i % 2) == activeGroup)
            {
                anySteppingInGroup = true;
                break;
            }
        }

        if (!anySteppingInGroup)
        {
            activeGroup = 1 - activeGroup;
        }
    }

    private void AnimateLegs()
    {
        int count = legRoots.Length;
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0) deltaTime = 0.016f;

        for (int i = 0; i < count; i++)
        {
            if (!legStepping[i]) continue;

            stepProgress[i] += stepSpeed * deltaTime;

            if (stepProgress[i] >= 1f)
            {
                stepProgress[i] = 0f;
                legStepping[i] = false;

                Transform foot = GetFootTransform(i);
                if (foot != null)
                {
                    legPlanted[i] = foot.position;
                }
            }
        }
    }

    private void ApplyIK()
    {
        int count = legRoots.Length;

        for (int i = 0; i < count; i++)
        {
            if (legRoots[i] == null) continue;

            Transform hip = legRoots[i].Find("Hip");
            if (hip == null) continue;

            Transform knee = hip.Find("Knee");
            Transform foot = knee != null ? knee.Find("Foot") : hip.Find("Foot");

            if (foot == null) continue;

            // Get target position
            Vector3 target = legStepping[i] ? GetStepPosition(i) : legPlanted[i];

            // Simple IK: move foot toward target
            foot.position = Vector3.Lerp(foot.position, target, 0.1f);

            // Adjust knee if it exists
            if (knee != null)
            {
                Vector3 hipPos = hip.position;
                Vector3 footPos = foot.position;
                Vector3 midpoint = (hipPos + footPos) / 2f;
                midpoint.y -= 0.1f; // Bend knee

                knee.position = Vector3.Lerp(knee.position, midpoint, 0.1f);
            }
        }
    }

    private Vector3 GetStepPosition(int legIndex)
    {
        if (stepProgress[legIndex] <= 0f) return legPlanted[legIndex];
        if (stepProgress[legIndex] >= 1f) return legTargets[legIndex];

        float progress = stepProgress[legIndex];
        Vector3 start = legPlanted[legIndex];
        Vector3 end = legTargets[legIndex];

        // Linear interpolation
        Vector3 pos = Vector3.Lerp(start, end, progress);

        // Add arc
        float arc = 4f * stepHeight * progress * (1f - progress);
        pos.y += arc;

        return pos;
    }

    private Transform GetFootTransform(int legIndex)
    {
        if (legRoots[legIndex] == null) return null;

        Transform hip = legRoots[legIndex].Find("Hip");
        if (hip == null) return null;

        Transform knee = hip.Find("Knee");
        if (knee != null)
        {
            Transform foot = knee.Find("Foot");
            if (foot != null) return foot;
        }

        return hip.Find("Foot");
    }
}
