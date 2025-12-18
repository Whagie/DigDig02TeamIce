using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BlobShadowMesh : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Raycasting")]
    public LayerMask groundMask;
    public float raycastHeight = 2f;
    public float maxDrop = 2f;

    [Header("Smoothing")]
    public float positionSmoothing = 15f;
    public float vertexSmoothing = 20f;

    [Header("Air Fade")]
    public float maxAirHeight = 2.5f;

    [Header("Steep Handling")]
    public float dropThreshold = 0.4f;     // below center = cliff
    public float riseThreshold = 0.4f;     // above center = ledge
    public float inwardStrength = 0.45f;   // pull in on drops
    public float outwardStrength = 0.15f;  // gentle push out on rises

    Mesh mesh;
    Vector3[] baseVertices;
    Vector3[] deformedVertices;

    Material material;
    float currentAlpha = 1f;

    float maxRadius;            // furthest vertex from center (XZ)
    float centerGroundY;        // ground height under player

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        deformedVertices = new Vector3[baseVertices.Length];

        material = GetComponent<MeshRenderer>().material;

        // Cache maximum radius for bias scaling
        maxRadius = 0f;
        foreach (var v in baseVertices)
        {
            float r = new Vector2(v.x, v.z).magnitude;
            if (r > maxRadius)
                maxRadius = r;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // -------------------------------------------------
        // 1) Follow player horizontally (XZ only)
        // -------------------------------------------------
        Vector3 pos = transform.position;
        pos.x = target.position.x;
        pos.z = target.position.z;
        transform.position = pos;

        // -------------------------------------------------
        // 2) Center raycast -> anchor Y + air height
        // -------------------------------------------------
        float targetAnchorY = transform.position.y;
        float airHeight = maxAirHeight;

        Vector3 centerRayOrigin = new Vector3(
            target.position.x,
            target.position.y + raycastHeight,
            target.position.z
        );

        if (Physics.Raycast(centerRayOrigin, Vector3.down, out RaycastHit centerHit,
                            raycastHeight + maxDrop, groundMask))
        {
            centerGroundY = centerHit.point.y;
            targetAnchorY = centerGroundY + 0.01f; // z-fighting offset
            airHeight = Mathf.Max(0f, target.position.y - centerGroundY);
        }

        pos.y = Mathf.Lerp(transform.position.y,
                           targetAnchorY,
                           Time.deltaTime * positionSmoothing);
        transform.position = pos;

        float anchorY = transform.position.y;

        // -------------------------------------------------
        // 3) Airborne fade
        // -------------------------------------------------
        float targetAlpha = Mathf.Clamp01(1f - (airHeight / maxAirHeight));
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 10f);

        Color col = material.color;
        col.a = currentAlpha;
        material.color = col;

        // -------------------------------------------------
        // 4) Vertex deformation + steep bias
        // -------------------------------------------------
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 local = baseVertices[i];
            Vector2 localXZ = new Vector2(local.x, local.z);

            float radius = localXZ.magnitude;
            float t = (maxRadius > 0f) ? radius / maxRadius : 0f;

            Vector3 rayOrigin = transform.TransformPoint(
                new Vector3(local.x, raycastHeight, local.z)
            );

            float targetY = -maxDrop;
            float radialBias = 0f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                                raycastHeight + maxDrop, groundMask))
            {
                float deltaFromCenter = hit.point.y - centerGroundY;
                targetY = (hit.point.y - anchorY) + 0.1f;

                if (deltaFromCenter < -dropThreshold)
                {
                    // Steep drop
                    radialBias = -inwardStrength;
                }
                else if (deltaFromCenter > riseThreshold)
                {
                    // Steep rise / ledge
                    radialBias = outwardStrength;
                }
            }
            else
            {
                // No support at all -> strong inward pull
                radialBias = -inwardStrength;
            }

            // Apply radial bias scaled by distance from center
            float scale = Mathf.Lerp(1f, 1f + radialBias, t);
            Vector2 biasedXZ = localXZ * scale;

            deformedVertices[i].x = biasedXZ.x;
            deformedVertices[i].z = biasedXZ.y;
            deformedVertices[i].y = Mathf.Lerp(
                deformedVertices[i].y,
                targetY,
                Time.deltaTime * vertexSmoothing
            );
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateBounds();
    }
}
