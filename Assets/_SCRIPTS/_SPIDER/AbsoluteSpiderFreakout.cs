using UnityEngine;

/// <summary>
/// Absolute Spider Freakout – focused 4-leg IK walker with calmer gait,
/// arc steps, reach clamping, gentle wall taps, and a simple shot nudge hook.
/// Works in editor and play mode.
/// </summary>
[ExecuteAlways]
public class AbsoluteSpiderFreakout : MonoBehaviour
{
    [Header("Legs")]
    [SerializeField] private Transform[] legRoots;
    [SerializeField] private LayerMask groundLayers = -1;
    [SerializeField] private float maxLegLength = 1.1f;
    [SerializeField] private float raycastUp = 1.2f;
    [SerializeField] private float raycastDown = 2.2f;
    [SerializeField] private float wallProbeDistance = 0.65f;
    [SerializeField] private float wallProbeHeight = 0.35f;
    [SerializeField, Range(0f, 0.7f)] private float wallMaxUpDot = 0.35f; // limit to near-vertical surfaces

    [Header("Stepping")]
    [SerializeField] private float stepDistance = 0.55f;
    [SerializeField] private float stepHeight = 0.18f;
    [SerializeField] private float stepSpeed = 4.5f;
    [SerializeField] private float strideForward = 0.35f;
    [SerializeField] private float strideSide = 0.18f;
    [SerializeField] private float minStepCooldown = 0.12f;

    [Header("Behavior")]
    [SerializeField] private float calmSpeedLimit = 3.5f;
    [SerializeField] private float velocitySmoothing = 10f;
    [SerializeField] private float plantedStiffness = 18f;
    [SerializeField] private float jointStiffness = 18f;
    [SerializeField] private float footRotateSpeed = 12f;
    [SerializeField, Range(0.15f, 0.85f)] private float upperLegFraction = 0.55f;
    [SerializeField, Range(0.05f, 0.7f)] private float midLegFraction = 0.25f;
    [SerializeField, Range(0f, 0.8f)] private float kneeOutwardFactor = 0.3f;
    [SerializeField, Range(-0.5f, 0.5f)] private float knee1UpOffset = 0.18f;
    [SerializeField, Range(-0.5f, 0.5f)] private float knee2UpOffset = 0.08f;
    [SerializeField, Min(0f)] private float hipOutwardOffset = 0.25f;
    [SerializeField] private float bodyHeight = 0.6f;
    [SerializeField] private float bodyKneeOffset = -0.12f;
    [SerializeField, Range(0f, 1f)] private float bodyKneeBlend = 1f;
    [SerializeField] private float bodySpring = 120f;
    [SerializeField] private float bodyDamping = 18f;

    [Header("Hit Reaction")]
    [SerializeField] private float shotImpulse = 2.5f;

    [Header("Debug/Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoSphere = 0.04f;
    [SerializeField] private float gizmoLineHeight = 0.1f;

    private LegState[] _legs;
    private Vector3 _lastBodyPos;
    private Vector3 _smoothedVelocity;
    private int _activeGroup;
    private Rigidbody _rigidbody;
    private float _editorDelta = 0.016f;

    private class LegState
    {
        public Transform root;
        public Transform hip;
        public Transform knee;
        public Transform ankle;
        public Transform foot;
        public Vector3 restLocal;
        public Vector3 planted;
        public Vector3 target;
        public Vector3 start;
        public Vector3 normal;
        public bool stepping;
        public float t;
        public float lastStepTime;
        public int group;
    }

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _lastBodyPos = transform.position;
        TryAutoAssignLegRoots();
        BuildLegStates();
    }

    private void Reset()
    {
        TryAutoAssignLegRoots();
    }

    private void Update()
    {
        if (_legs == null || _legs.Length == 0) return;

        float dt = Time.deltaTime > 0f ? Time.deltaTime : _editorDelta;
        UpdateVelocity(dt);
        UpdateStepping(dt);
        ApplyIK(dt);
    }

    private void FixedUpdate()
    {
        MaintainBodyHeight(Time.fixedDeltaTime);
    }

    private void UpdateVelocity(float dt)
    {
        Vector3 pos = transform.position;
        Vector3 rawVel = (pos - _lastBodyPos) / Mathf.Max(dt, 0.0001f);
        _lastBodyPos = pos;

        rawVel = Vector3.ClampMagnitude(rawVel, calmSpeedLimit);
        float lerpFactor = 1f - Mathf.Exp(-velocitySmoothing * dt);
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, rawVel, lerpFactor);
    }

    private void UpdateStepping(float dt)
    {
        bool groupHasStepping = false;
        for (int i = 0; i < _legs.Length; i++)
        {
            LegState leg = _legs[i];
            if (leg.stepping && leg.group == _activeGroup)
            {
                groupHasStepping = true;
                break;
            }
        }

        if (!groupHasStepping)
        {
            _activeGroup = 1 - _activeGroup;
        }

        for (int i = 0; i < _legs.Length; i++)
        {
            LegState leg = _legs[i];
            if (leg.foot == null) continue;

            if (leg.stepping)
            {
                AnimateStep(leg, dt);
                continue;
            }

            Vector3 desired = CalculateDesiredTarget(leg);
            Vector3 normal = leg.normal;
            desired = ClampToReach(leg, desired, out normal);

            float dist = Vector3.Distance(leg.planted, desired);
            bool groupBlocked = _activeGroup != leg.group;
            bool cooledDown = (Time.time - leg.lastStepTime) > minStepCooldown;

            if (dist > stepDistance && cooledDown && !groupBlocked)
            {
                BeginStep(leg, desired, normal);
            }
            else
            {
                // Pull planted point back to ground in case body moved vertically
                Vector3 grounded = ProjectToGround(leg.planted, out Vector3 groundNormal);
                if (Vector3.Distance(grounded, leg.planted) > 0.02f)
                {
                    leg.planted = grounded;
                    leg.normal = groundNormal;
                }

                // Keep planted foot locked with small damping to avoid jitter
                leg.foot.position = Vector3.Lerp(leg.foot.position, leg.planted, dt * plantedStiffness);
                RotateFoot(leg, dt, leg.normal.sqrMagnitude > 0.01f ? leg.normal : Vector3.up);
            }
        }
    }

    private Vector3 CalculateDesiredTarget(LegState leg)
    {
        Vector3 restWorld = transform.TransformPoint(leg.restLocal);
        Vector3 moveDir = _smoothedVelocity.sqrMagnitude > 0.01f ? _smoothedVelocity.normalized : transform.forward;

        // Bias outward for stance width
        float sideSign = Mathf.Sign(leg.restLocal.x);
        Vector3 sideOffset = transform.right * sideSign * strideSide;
        Vector3 forwardOffset = moveDir * strideForward;

        Vector3 probe = restWorld + forwardOffset + sideOffset;

        if (TryFindSurface(probe, moveDir, leg, out Vector3 hitPoint, out Vector3 hitNormal))
        {
            leg.normal = hitNormal;
            return hitPoint;
        }

        // Fallback: project rest to ground directly under it
        Vector3 grounded = ProjectToGround(restWorld, out Vector3 groundNormal);
        leg.normal = groundNormal;
        return grounded;
    }

    private bool TryFindSurface(Vector3 probe, Vector3 moveDir, LegState leg, out Vector3 point, out Vector3 normal)
    {
        Vector3 rayStart = probe + Vector3.up * raycastUp;
        float rayDistance = raycastUp + raycastDown;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit downHit, rayDistance, groundLayers))
        {
            point = downHit.point;
            normal = downHit.normal;
            return true;
        }

        // Wall tap: cast forward at knee height, but keep body on ground
        Vector3 hipPos = leg.hip != null ? leg.hip.position : leg.root.position;
        Vector3 wallOrigin = hipPos + Vector3.up * wallProbeHeight;
        Vector3 dir = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : transform.forward;

        if (Physics.Raycast(wallOrigin, dir, out RaycastHit wallHit, wallProbeDistance, groundLayers))
        {
            float upDot = Mathf.Abs(Vector3.Dot(wallHit.normal, Vector3.up));
            if (upDot <= wallMaxUpDot)
            {
                // Keep vertical offset reasonable so we don't climb
                float verticalDelta = Mathf.Abs(wallHit.point.y - hipPos.y);
                if (verticalDelta <= maxLegLength * 0.6f)
                {
                    point = wallHit.point;
                    normal = wallHit.normal;
                    return true;
                }
            }
        }

        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    private Vector3 ClampToReach(LegState leg, Vector3 target, out Vector3 normal)
    {
        Vector3 hipPos = leg.hip != null ? leg.hip.position : (leg.root != null ? leg.root.position : transform.position);
        Vector3 toTarget = target - hipPos;
        float dist = toTarget.magnitude;

        normal = leg.normal.sqrMagnitude > 0.01f ? leg.normal : Vector3.up;

        if (dist > maxLegLength && dist > 0.0001f)
        {
            target = hipPos + toTarget.normalized * maxLegLength;
        }

        return target;
    }

    private void BeginStep(LegState leg, Vector3 desired, Vector3 normal)
    {
        leg.stepping = true;
        leg.t = 0f;
        leg.start = leg.foot.position;
        leg.target = desired;
        leg.normal = normal;
        leg.lastStepTime = Time.time;
        _activeGroup = leg.group;
    }

    private void AnimateStep(LegState leg, float dt)
    {
        leg.t += stepSpeed * dt;
        float progress = Mathf.Clamp01(leg.t);
        float eased = Mathf.SmoothStep(0f, 1f, progress);

        Vector3 pos = Vector3.Lerp(leg.start, leg.target, eased);
        float arc = 4f * stepHeight * eased * (1f - eased);
        Vector3 up = Vector3.Lerp(Vector3.up, leg.normal, 0.6f);
        pos += up * arc;

        leg.foot.position = pos;
        RotateFoot(leg, dt, leg.normal);

        if (progress >= 1f)
        {
            leg.stepping = false;
            leg.planted = leg.target;
            leg.foot.position = leg.planted;
        }
    }

    private void RotateFoot(LegState leg, float dt, Vector3 normal)
    {
        Transform foot = leg.foot;
        if (foot == null) return;

        Vector3 forward = transform.forward;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

        Quaternion targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, normal).normalized, normal);
        foot.rotation = Quaternion.Slerp(foot.rotation, targetRot, dt * footRotateSpeed);
    }

    private void ApplyIK(float dt)
    {
        // Feet are positioned directly in UpdateStepping, so here we only keep knees bending outward
        foreach (LegState leg in _legs)
        {
            if (leg.knee == null || leg.hip == null || leg.foot == null) continue;

            Vector3 hipPos = leg.hip.position;
            Vector3 footPos = leg.foot.position;
            Vector3 dir = footPos - hipPos;
            if (dir.sqrMagnitude < 0.0001f) continue;

            // Bend knee outward relative to body center
            Vector3 bodyCenter = transform.position;
            Vector3 outward = (leg.hip.position - bodyCenter);
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.001f)
            {
                outward = Vector3.Cross(dir, Vector3.up);
            }
            outward.Normalize();

            float upperLen = maxLegLength * upperLegFraction;
            float midLen = maxLegLength * midLegFraction;
            float total = upperLen + midLen;
            if (total > maxLegLength * 0.98f)
            {
                float scale = (maxLegLength * 0.98f) / total;
                upperLen *= scale;
                midLen *= scale;
            }
            float bendOut = upperLen * kneeOutwardFactor;
            Vector3 kneePos = hipPos + dir.normalized * upperLen + outward * bendOut + Vector3.up * knee1UpOffset;

            // Position second knee/ankle partway toward foot with softer outward bend
            Vector3 anklePos;
            if (leg.ankle != null)
            {
                Vector3 ankleOutward = outward * (bendOut * 0.6f);
                anklePos = kneePos + dir.normalized * midLen + ankleOutward + Vector3.up * knee2UpOffset;
                leg.ankle.position = Vector3.Lerp(leg.ankle.position, anklePos, dt * jointStiffness);
            }
            else
            {
                anklePos = kneePos + dir.normalized * midLen;
            }

            leg.knee.position = Vector3.Lerp(leg.knee.position, kneePos, dt * jointStiffness);

            leg.hip.rotation = Quaternion.LookRotation((leg.knee.position - hipPos).normalized, Vector3.up);
            if (leg.ankle != null)
            {
                leg.knee.rotation = Quaternion.LookRotation((leg.ankle.position - leg.knee.position).normalized, Vector3.up);
                leg.ankle.rotation = Quaternion.LookRotation((footPos - leg.ankle.position).normalized, Vector3.up);
            }
            else
            {
                leg.knee.rotation = Quaternion.LookRotation((footPos - leg.knee.position).normalized, Vector3.up);
            }
        }
    }

    private void BuildLegStates()
    {
        if (legRoots == null || legRoots.Length == 0)
        {
            _legs = System.Array.Empty<LegState>();
            return;
        }

        _legs = new LegState[legRoots.Length];
        for (int i = 0; i < legRoots.Length; i++)
        {
            Transform root = legRoots[i];
            if (root == null) continue;

            LegState leg = new LegState();
            leg.root = root;
            leg.hip = FindChild(root, "Hip");
            if (leg.hip != null)
            {
                leg.knee = FindChild(leg.hip, "Knee");
                if (leg.knee != null)
                {
                    leg.ankle = FindChild(leg.knee, "Ankle");
                    if (leg.ankle != null)
                    {
                        leg.foot = FindChild(leg.ankle, "Foot");
                    }
                    if (leg.foot == null)
                    {
                        // fallback if hierarchy missing ankle
                        leg.foot = FindChild(leg.knee, "Foot");
                    }
                }
                else
                {
                    leg.foot = FindChild(leg.hip, "Foot");
                }
            }
            else
            {
                leg.foot = FindChild(root, "Foot");
            }

            if (leg.foot != null)
            {
                Vector3 startWorld = leg.foot.position;
                Vector3 grounded = ProjectToGround(startWorld, out Vector3 groundNormal);

                leg.restLocal = transform.InverseTransformPoint(grounded);
                // Push rest target outward for clearer stance width
                float side = Mathf.Sign(leg.restLocal.x == 0f ? 1f : leg.restLocal.x);
                leg.restLocal.x += side * hipOutwardOffset;
                leg.planted = grounded;
                leg.target = grounded;
                leg.start = grounded;
                leg.normal = groundNormal;
            }

            leg.group = DetermineGroup(root);
            _legs[i] = leg;
        }
    }

    private int DetermineGroup(Transform root)
    {
        Vector3 local = transform.InverseTransformPoint(root.position);
        return local.x * local.z >= 0f ? 0 : 1;
    }

    private void TryAutoAssignLegRoots()
    {
        if (legRoots != null && legRoots.Length > 0) return;

        Transform legsContainer = transform.Find("Legs");
        if (legsContainer == null) return;

        int childCount = legsContainer.childCount;
        legRoots = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            legRoots[i] = legsContainer.GetChild(i);
        }
    }

    private Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform deeper = FindChild(child, name);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private Vector3 ProjectToGround(Vector3 world, out Vector3 normal)
    {
        Vector3 rayStart = world + Vector3.up * raycastUp;
        float rayDistance = raycastUp + raycastDown;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayers))
        {
            normal = hit.normal;
            return hit.point;
        }

        normal = Vector3.up;
        return world;
    }

    private void MaintainBodyHeight(float dt)
    {
        if (_rigidbody == null || bodySpring <= 0f) return;

        Vector3 up = Vector3.up;
        Vector3 support = Vector3.zero;
        int count = 0;
        Vector3 kneeSum = Vector3.zero;
        int kneeCount = 0;

        if (_legs != null)
        {
            foreach (LegState leg in _legs)
            {
                if (leg == null) continue;
                Vector3 p = leg.planted;
                if (p == Vector3.zero && leg.foot != null) p = leg.foot.position;
                support += p;
                count++;

                if (leg.knee != null)
                {
                    kneeSum += leg.knee.position;
                    kneeCount++;
                }
            }
        }

        if (count == 0)
        {
            support = transform.position - up * bodyHeight;
            count = 1;
        }
        support /= count;

        // Desired height: base bodyHeight, optionally blended with knee-relative offset
        float desiredHeight = bodyHeight;
        if (kneeCount > 0 && bodyKneeBlend > 0f)
        {
            Vector3 kneeAvg = kneeSum / kneeCount;
            float kneeHeight = Vector3.Dot(kneeAvg - support, up);
            float kneeBasedHeight = Mathf.Max(0.05f, kneeHeight + bodyKneeOffset);
            desiredHeight = Mathf.Lerp(bodyHeight, kneeBasedHeight, bodyKneeBlend);
        }

        float currentHeight = Vector3.Dot(transform.position - support, up);
        float velocityUp = Vector3.Dot(_rigidbody.linearVelocity, up);
        float error = desiredHeight - currentHeight;

        float force = error * bodySpring - velocityUp * bodyDamping;
        _rigidbody.AddForce(up * force, ForceMode.Acceleration);
    }

    /// <summary>
    /// External hook for weapon hits. Adds a small impulse and calms velocity spikes.
    /// </summary>
    public void ApplyShotImpulse(Vector3 direction, float strength = 1f)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y);
        }
        direction.Normalize();

        Vector3 impulse = direction * shotImpulse * Mathf.Max(0.1f, strength);
        if (_rigidbody != null)
        {
            _rigidbody.AddForce(impulse, ForceMode.Impulse);
        }
        else
        {
            transform.position += impulse * 0.05f;
        }

        _smoothedVelocity += impulse * 0.1f;
    }

    /// <summary>
    /// Assigns leg roots at runtime/editor and rebuilds cached leg data.
    /// </summary>
    /// <param name="roots">Array of leg root transforms</param>
    public void AssignLegRoots(Transform[] roots)
    {
        legRoots = roots;
        BuildLegStates();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || _legs == null) return;

        Color plantedColor = Color.green;
        Color targetColor = Color.cyan;
        Color footColor = Color.yellow;
        Color rayColor = new Color(1f, 0.65f, 0f, 0.9f);
        Color wallColor = new Color(1f, 0.25f, 0.25f, 0.9f);

        foreach (LegState leg in _legs)
        {
            if (leg == null) continue;

            // Planted and target
            Gizmos.color = plantedColor;
            Gizmos.DrawSphere(leg.planted, gizmoSphere);
            Gizmos.color = targetColor;
            Gizmos.DrawSphere(leg.target, gizmoSphere * 0.85f);

            // Current foot
            if (leg.foot != null)
            {
                Gizmos.color = footColor;
                Gizmos.DrawSphere(leg.foot.position, gizmoSphere * 0.8f);
                Gizmos.DrawLine(leg.foot.position, leg.planted);
            }

            // Desired probe rays (down + wall)
            Vector3 restWorld = transform.TransformPoint(leg.restLocal);
            Vector3 moveDir = _smoothedVelocity.sqrMagnitude > 0.01f ? _smoothedVelocity.normalized : transform.forward;
            float sideSign = Mathf.Sign(leg.restLocal.x);
            Vector3 sideOffset = transform.right * sideSign * strideSide;
            Vector3 forwardOffset = moveDir * strideForward;
            Vector3 probe = restWorld + forwardOffset + sideOffset;

            // Down ray
            Vector3 rayStart = probe + Vector3.up * raycastUp;
            Vector3 rayEnd = rayStart + Vector3.down * (raycastUp + raycastDown);
            Gizmos.color = rayColor;
            Gizmos.DrawLine(rayStart, rayEnd);
            Gizmos.DrawSphere(rayEnd, gizmoSphere * 0.6f);

            // Wall ray
            Vector3 hipPos = leg.hip != null ? leg.hip.position : (leg.root != null ? leg.root.position : transform.position);
            Vector3 wallOrigin = hipPos + Vector3.up * wallProbeHeight;
            Vector3 wallDir = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : transform.forward;
            Gizmos.color = wallColor;
            Gizmos.DrawLine(wallOrigin, wallOrigin + wallDir * wallProbeDistance);
            Gizmos.DrawSphere(wallOrigin + wallDir * wallProbeDistance, gizmoSphere * 0.6f);
        }

        // Body velocity indicator
        Gizmos.color = Color.blue;
        Vector3 start = transform.position + Vector3.up * gizmoLineHeight;
        Gizmos.DrawLine(start, start + _smoothedVelocity);
    }
#endif
}
