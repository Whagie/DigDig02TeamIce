using UnityEngine;

public class CameraDistanceRaycaster : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;        // The actual camera
    [SerializeField] Transform cameraTargetTransform;  // The pivot or player
    [SerializeField] LayerMask layerMask = Physics.AllLayers;

    public float minimumDistanceFromObstacles = 0.1f;
    public float smoothingFactor = 25f;
    public float upwardOffsetSpeed = 3f;   // how fast camera slides upward when blocked
    public float maxUpwardOffset = 2f;     // how high it can go to avoid clipping

    private Transform tr;
    private float currentDistance;
    private float currentUpOffset;

    void Awake()
    {
        tr = transform;
        layerMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        currentDistance = (cameraTargetTransform.position - tr.position).magnitude;
    }

    void LateUpdate()
    {
        Vector3 castDirection = cameraTargetTransform.position - tr.position;
        float targetDistance = castDirection.magnitude;
        float adjustedDistance = GetCameraDistance(castDirection, targetDistance);

        // Smooth distance
        currentDistance = Mathf.Lerp(currentDistance, adjustedDistance, Time.deltaTime * smoothingFactor);

        // Calculate target camera position
        Vector3 desiredPos = cameraTargetTransform.position - castDirection.normalized * currentDistance;
        desiredPos += Vector3.up * currentUpOffset;

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPos, Time.deltaTime * smoothingFactor);
    }

    float GetCameraDistance(Vector3 castDirection, float targetDistance)
    {
        float distance = targetDistance + minimumDistanceFromObstacles;
        float sphereRadius = 0.5f;

        if (Physics.SphereCast(new Ray(cameraTargetTransform.position, -castDirection.normalized), sphereRadius, out RaycastHit hit, targetDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Move upward instead of inward
            currentUpOffset = Mathf.MoveTowards(currentUpOffset, maxUpwardOffset, Time.deltaTime * upwardOffsetSpeed);
            return targetDistance; // keep distance
        }
        else
        {
            // Clear view — reset upward offset smoothly
            currentUpOffset = Mathf.MoveTowards(currentUpOffset, 0f, Time.deltaTime * upwardOffsetSpeed);
            return targetDistance;
        }
    }
}
