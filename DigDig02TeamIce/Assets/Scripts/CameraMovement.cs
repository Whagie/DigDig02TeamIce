using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Player player;
    private Transform target;

    [SerializeField] private GameObject cameraObject;
    private float cameraStartDistanceZ;
    private float cameraStartRotationX;
    private float cameraStartPositionY;
    [SerializeField] private float cameraDistanceRange1 = 10f;
    [SerializeField] private float cameraDistanceRange2 = 10f;
    [SerializeField] float cameraLocalXRange1 = 20f;
    [SerializeField] float cameraLocalXRange2 = 20f;
    [SerializeField] float cameraLocalYPos1 = 0f;
    [SerializeField] float cameraLocalYPos2 = 0f;

    [Header("Follow")]
    public float maxDistance = 5f;
    public float moveSmoothSpeed = 2f;
    public float recenterSmoothSpeed = 4f;

    [Header("Rotation")]
    public bool Rotate = true;
    public float rotationSensitivity = 3f;
    public float xSensitivityMultiplier = 1f;
    public float ySensitivityMultiplier = 1f;
    public float xRange = 30f;

    [Header("Collision")]
    public Transform rayOrigin;           // pivot from which raycasts are fired
    public LayerMask collisionMask;       // layers that block the camera
    public float rayDistance = 0.5f;      // distance to check for simple down/back checks
    public float pushStrength = 5f;       // how quickly the pitch is pushed back
    public float maxPushPenetration = 1f; // penetration mapped to 0..1 for smoothing

    private float rotationX; // signed -180..180
    private float rotationY; // signed -180..180
    private float startX;    // signed start pitch, used as center for clamp

    private Vector3 velocity = Vector3.zero;
    private bool recentering = false;

    public bool followY = false;

    public bool StaticCamera = false;

    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();

        Vector3 euler = transform.localEulerAngles;
        startX = NormalizeAngle(euler.x);
        rotationX = startX;
        rotationY = NormalizeAngle(euler.y);

        cameraStartDistanceZ = cameraObject.transform.localPosition.z;
        cameraStartRotationX = cameraObject.transform.rotation.x;
        cameraStartPositionY = cameraObject.transform.localPosition.y;
    }

    void LateUpdate()
    {
        if (StaticCamera) return;
        if (!player) target = null;
        else target = player.transform;
        if (!target) return;

        // === Follow logic ===
        Vector3 desiredPosition;
        if (followY)
        {
            if (!player.Jumping)
            {
                desiredPosition = target.position;
            }
            else
            {
                desiredPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
            }
        }
        else
        {
            desiredPosition = new Vector3(target.position.x, 0f, target.position.z);
        }
        Vector3 offset = desiredPosition - transform.position;
        float distance = offset.magnitude;

        if (distance > maxDistance) recentering = true;
        if (distance < 0.1f) recentering = false;

        if (recentering)
        {
            float smooth = (distance > maxDistance) ? moveSmoothSpeed : recenterSmoothSpeed;
            Vector3 newPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / smooth);

            if (CanMoveDown(newPosition))
                transform.position = newPosition;
        }

        // === Rotation input (always apply) ===
        if (Rotate && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            rotationY += mouseX * rotationSensitivity * ySensitivityMultiplier;
            rotationX -= mouseY * rotationSensitivity * xSensitivityMultiplier;

            // keep rotationX/rotationY in signed range for stable clamps
            rotationX = NormalizeAngle(rotationX);
            rotationY = NormalizeAngle(rotationY);

            float halfRange = xRange * 0.5f;
            rotationX = Mathf.Clamp(rotationX, startX - halfRange, startX + halfRange);

            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }

        float distanceAmount = MapToRange(rotationX, startX, xRange);
        float mappedDistance;
        float mappedY;
        float mappedRotation;
        if (distanceAmount < 0f)
        {
            // negative side (-1 to 0) -> move towards Y
            mappedDistance = Mathf.Lerp(0f, cameraDistanceRange1, -distanceAmount); // -distanceAmount goes 0 -> 1
            mappedRotation = Mathf.Lerp(0f, cameraLocalXRange1, -distanceAmount); // -distanceAmount goes 0 -> 1
            mappedY = Mathf.Lerp(0f, cameraLocalYPos1, -distanceAmount); // -distanceAmount goes 0 -> 1
        }
        else
        {
            // positive side (0 to 1) -> move towards Z
            mappedDistance = Mathf.Lerp(0f, cameraDistanceRange2, distanceAmount);
            mappedRotation = Mathf.Lerp(0f, cameraLocalXRange2, distanceAmount);
            mappedY = Mathf.Lerp(0f, cameraLocalYPos2, distanceAmount);
        }

        float amountY = Mathf.InverseLerp(-1f, 1f, distanceAmount);

        cameraObject.transform.SetLocalPositionAndRotation(new Vector3(
            cameraObject.transform.localPosition.x, 
            (amountY * cameraLocalYPos1) + cameraStartPositionY, 
            (mappedDistance) + cameraStartDistanceZ),
            
            Quaternion.Euler(
                (mappedRotation) + cameraStartRotationX,
                cameraObject.transform.rotation.y,
                cameraObject.transform.rotation.z
            ));

        // === Collision push-away (only adjust pitch smoothly) ===
        //if (rayOrigin)
        //{
        //    Vector3 camDir = transform.position - rayOrigin.position;
        //    float camDist = camDir.magnitude;

        //    if (camDist > 0.001f)
        //    {
        //        if (Physics.Raycast(rayOrigin.position, camDir.normalized, out RaycastHit hit, camDist, collisionMask))
        //        {
        //            // only react to mostly-horizontal surfaces (ground/low slopes)
        //            float upDot = Vector3.Dot(hit.normal, Vector3.up);

        //            if (upDot > 0.4f) // tweak threshold if you want steeper slopes to count
        //            {
        //                // penetration: how far camera would be inside the hit surface
        //                float penetration = camDist - hit.distance + 0.05f; // small cushion
        //                penetration = Mathf.Max(0f, penetration);

        //                // map penetration to 0..1 using a configurable max
        //                float t = Mathf.Clamp01(penetration / Mathf.Max(0.0001f, maxPushPenetration));

        //                // Smoothly nudge rotationX toward startX (center) based on t and pushStrength
        //                // This avoids sign confusion: we always move the pitch *toward the safe center angle*
        //                float targetX = Mathf.Lerp(rotationX, startX, t * pushStrength * Time.deltaTime);

        //                // Optionally you might prefer to nudge only partially toward center:
        //                rotationX = Mathf.Lerp(rotationX, targetX, 1f); // immediate copy of computed target
        //                rotationX = Mathf.Clamp(rotationX, startX - xRange * 0.5f, startX + xRange * 0.5f);

        //                transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        //            }
        //        }
        //    }
        //}
    }

    // --- Collision helpers ---
    bool CanMoveDown(Vector3 newPosition)
    {
        if (!rayOrigin) return true;
        return !Physics.Raycast(rayOrigin.position, Vector3.down, rayDistance, collisionMask);
    }

    bool CanMoveBack()
    {
        if (!rayOrigin) return true;
        return !Physics.Raycast(rayOrigin.position, -rayOrigin.forward, rayDistance, collisionMask);
    }

    // Normalize angle 0..360 -> -180..180 and keep numbers small for stable math
    float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    float MapToRange(float x, float startX, float fullRange)
    {
        if (fullRange == 0f) return 0f; // avoid divide-by-zero
        return (x - startX) / (fullRange * 0.5f);
    }

    // Debug visualization
    //void OnDrawGizmosSelected()
    //{
    //    if (!rayOrigin) return;

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + Vector3.down * rayDistance);

    //    Gizmos.color = Color.cyan;
    //    Gizmos.DrawLine(rayOrigin.position, transform.position);
    //}
}
