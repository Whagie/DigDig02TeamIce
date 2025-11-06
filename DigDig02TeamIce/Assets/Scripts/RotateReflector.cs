using UnityEngine;

public class RotateReflector : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 20f;      // Base speed
    public float boostMultiplier = 2f;     // Speed multiplier when Shift is held
    public float detectionRadius = 4.5f;     // Overlap sphere radius

    [Header("Player Settings")]
    public LayerMask playerLayer;          // Assign the Player layer in inspector

    private bool inRadius = false;
    void Start()
    {
        playerLayer = LayerMask.GetMask("Player");
    }
    void Update()
    {
        // Check if the player is inside the overlap sphere
        Collider[] players = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (players.Length == 0)
        {
            inRadius = false;
            return; // No player nearby, do nothing
        }

        inRadius = true;
        // Determine rotation input
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) input += 1f;

        if (input != 0f)
        {
            // Apply speed multiplier if Shift is held
            float speed = rotationSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? boostMultiplier : 1f);
            transform.Rotate(Vector3.up, input * speed * Time.deltaTime, Space.World);
        }
    }

    // Optional: visualize the detection sphere in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (inRadius)
        {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
