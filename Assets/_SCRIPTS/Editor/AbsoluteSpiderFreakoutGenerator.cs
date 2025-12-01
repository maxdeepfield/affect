using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu helper to spawn a ready-to-walk Absolute Spider Freakout (4 legs).
/// </summary>
public static class AbsoluteSpiderFreakoutGenerator
{
    [MenuItem("GameObject/Spider IK/Create Absolute Spider Freakout", false, 11)]
    public static void CreateAbsoluteSpider(MenuCommand menuCommand)
    {
        GameObject spider = new GameObject("Absolute Spider Freakout");
        Undo.RegisterCreatedObjectUndo(spider, "Create Absolute Spider Freakout");

        // Body setup
        Rigidbody rb = spider.AddComponent<Rigidbody>();
        rb.mass = 8f;
        rb.linearDamping = 0.6f;
        rb.angularDamping = 0.25f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        CapsuleCollider capsule = spider.AddComponent<CapsuleCollider>();
        capsule.radius = 0.28f;
        capsule.height = 0.55f;
        capsule.center = Vector3.up * 0.25f;

        // Simple body visual
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(spider.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.6f, 0.4f, 0.8f);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Legs container
        GameObject legsContainer = new GameObject("Legs");
        legsContainer.transform.SetParent(spider.transform);
        legsContainer.transform.localPosition = Vector3.zero;
        legsContainer.transform.localRotation = Quaternion.identity;

        float legLength = 0.9f;
        float hipRatio = 0.55f;
        float upperLen = legLength * hipRatio;
        float midLen = legLength * 0.25f;
        float lowerLen = Mathf.Max(legLength - upperLen - midLen, 0.08f);
        float forwardOffset = legLength * 0.22f;
        float spread = 0.55f;

        string[] names = { "Leg_FL", "Leg_FR", "Leg_BL", "Leg_BR" };
        Vector3[] offsets =
        {
            new Vector3( spread, 0f,  spread),
            new Vector3(-spread, 0f,  spread),
            new Vector3( spread, 0f, -spread),
            new Vector3(-spread, 0f, -spread)
        };

        Transform[] roots = new Transform[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject legRoot = new GameObject(names[i]);
            legRoot.transform.SetParent(legsContainer.transform);
            legRoot.transform.localPosition = offsets[i];
            legRoot.transform.localRotation = Quaternion.identity;
            roots[i] = legRoot.transform;

            GameObject hip = new GameObject("Hip");
            hip.transform.SetParent(legRoot.transform);
            hip.transform.localPosition = Vector3.zero;

            GameObject knee = new GameObject("Knee");
            knee.transform.SetParent(hip.transform);
            knee.transform.localPosition = new Vector3(0f, -upperLen, forwardOffset);

            GameObject ankle = new GameObject("Ankle");
            ankle.transform.SetParent(knee.transform);
            ankle.transform.localPosition = new Vector3(0f, -midLen, 0f);

            GameObject foot = new GameObject("Foot");
            foot.transform.SetParent(ankle.transform);
            foot.transform.localPosition = new Vector3(0f, -lowerLen, 0f);

            AddLegVisual(hip.transform, knee.transform, 0.035f);
            AddLegVisual(knee.transform, ankle.transform, 0.032f);
            AddLegVisual(ankle.transform, foot.transform, 0.03f);
        }

        var freakout = spider.AddComponent<AbsoluteSpiderFreakout>();
        freakout.AssignLegRoots(roots);

        // Place on ground
        Vector3 start = spider.transform.position + Vector3.up * 2f;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 5f))
        {
            spider.transform.position = hit.point + Vector3.up * capsule.height * 0.5f;
        }
        else
        {
            spider.transform.position = Vector3.up * capsule.height * 0.5f;
        }

        GameObjectUtility.SetParentAndAlign(spider, menuCommand.context as GameObject);
        Selection.activeObject = spider;
    }

    private static void AddLegVisual(Transform start, Transform end, float radius)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "Visual";
        cyl.transform.SetParent(start);
        cyl.transform.localPosition = Vector3.zero;
        cyl.transform.localScale = new Vector3(radius, 0.5f, radius);
        Object.DestroyImmediate(cyl.GetComponent<Collider>());

        var connector = cyl.AddComponent<LegConnectorV3>();
        connector.startJoint = start;
        connector.endJoint = end;
        connector.radius = radius;
    }
}
