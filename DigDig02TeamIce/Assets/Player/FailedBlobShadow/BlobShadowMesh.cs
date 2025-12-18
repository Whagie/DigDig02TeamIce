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

    Mesh mesh;
    Vector3[] baseVertices;
    Vector3[] deformedVertices;

    Material material;
    float currentAlpha = 1f;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        deformedVertices = new Vector3[baseVertices.Length];

        material = GetComponent<MeshRenderer>().material;
    }

    void LateUpdate()
    {
        if (!target) return;

        // -------------------------------------------------
        // 1) Follow player horizontally
        // -------------------------------------------------
        Vector3 pos = transform.position;
        pos.x = target.position.x;
        pos.z = target.position.z;
        transform.position = pos;

        // -------------------------------------------------
        // 2) Center raycast -> anchor Y
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
            targetAnchorY = centerHit.point.y + 0.01f;
            airHeight = Mathf.Max(0f, target.position.y - centerHit.point.y);
        }

        pos.y = Mathf.Lerp(transform.position.y,
                           targetAnchorY,
                           Time.deltaTime * positionSmoothing);
        transform.position = pos;

        // -------------------------------------------------
        // 3) Airborne fade
        // -------------------------------------------------
        float targetAlpha = Mathf.Clamp01(1f - (airHeight / maxAirHeight));
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 10f);

        Color c = material.color;
        c.a = currentAlpha;
        material.color = c;

        // -------------------------------------------------
        // 4) Vertex deformation
        // -------------------------------------------------
        float anchorY = transform.position.y;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 local = baseVertices[i];

            Vector3 rayOrigin = transform.TransformPoint(
                new Vector3(local.x, raycastHeight, local.z)
            );

            float targetY = -maxDrop;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                                raycastHeight + maxDrop, groundMask))
            {
                targetY = (hit.point.y - anchorY) + 0.05f;
            }

            // Temporal smoothing (Y only)
            deformedVertices[i].x = local.x;
            deformedVertices[i].z = local.z;
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
