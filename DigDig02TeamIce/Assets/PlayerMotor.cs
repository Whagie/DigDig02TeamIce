using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float gravity = 9.82f;
    public float jumpHeight = 1f;
    public float groundCheckDistance = 0.2f;

    private Player player;
    private CharacterController controller;
    private CapsuleCollider mainCollider;

    // Intent
    private Vector3 moveInput;
    private bool sprinting;
    public Vector3 HorizontalVelocity { get; private set; }

    // External forces
    private Vector3 pushVelocity;
    private float pushTimer;

    // Vertical
    private float verticalVelocity;
    private bool jumpQueued;
    private bool jumped;

    public bool Grounded { get; private set; }
    public bool Jumping { get; private set; }

    void Awake()
    {
        player = GetComponent<Player>();
        controller = GetComponent<CharacterController>();
        mainCollider = player.MainCollider;
    }

    // ---------------- PUBLIC API ----------------

    public void SetMoveInput(Vector3 worldDir, bool isSprinting)
    {
        moveInput = worldDir;
        sprinting = isSprinting;
    }

    public void QueueJump()
    {
        jumpQueued = true;
    }

    public void ApplyPushback(Vector3 direction, float force, float duration)
    {
        pushVelocity = direction.normalized * force;
        pushTimer = duration;
    }

    public void Tick(float dt)
    {
        GroundCheck();
        ApplyVerticalMovement(dt);

        // Decide speed based on current grounded state
        float speed = sprinting && Grounded ? sprintSpeed : walkSpeed;
        Vector3 horizontal = moveInput * speed;

        // Apply pushback
        if (pushTimer > 0f)
        {
            pushTimer -= dt;
            horizontal = pushVelocity;
            if (pushTimer <= 0f) pushVelocity = Vector3.zero;
        }

        HorizontalVelocity = horizontal;  // for animation

        // Combine horizontal and vertical
        Vector3 finalMove = horizontal;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * dt);
    }

    // ---------------- INTERNAL ----------------

    void GroundCheck()
    {
        Vector3 dir = mainCollider.direction == 0 ? Vector3.right :
                      mainCollider.direction == 1 ? Vector3.up :
                      Vector3.forward;

        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float radius = mainCollider.radius * scale;
        float height = mainCollider.height * 0.5f * scale;

        Vector3 center = mainCollider.transform.TransformPoint(mainCollider.center);
        Vector3 origin = center - dir * (height - radius) - Vector3.up * radius * 0.5f;

        Grounded = Physics.CheckSphere(origin, radius + groundCheckDistance, player.groundLayers);
    }

    void ApplyVerticalMovement(float dt)
    {
        if (Grounded)
        {
            if (verticalVelocity < -2f) verticalVelocity = -2f;
            if (jumpQueued)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
                jumpQueued = false;
                jumped = true;
            }
            Jumping = jumped;
        }
        else
        {
            verticalVelocity -= gravity * dt;
            if (jumped) Jumping = true;
        }
    }

    void OnLanded()
    {
        // Hook for effects, animation, sound
    }
}
