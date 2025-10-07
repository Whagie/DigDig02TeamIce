using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity
{
    public Collider DetectionCollider;
    public CapsuleCollider MainCollider;
    public CapsuleCollider DamageCollider;

    private Collider[] colliders = new Collider[50];
    private Transform currentTarget;
    [SerializeField] private GameObject LockOnIcon;
    private GameObject iconCopy;

    private ParryManager parryManager;

    CharacterController controller;
    private Transform camera1;

    public float groundCheckDistance = 0.2f;

    public bool Grounded;
    public bool Sprinting = false;

    public float Speed;
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float turnSpeed = 8f;
    public float jumpHeight = 1f;

    private bool jumpQueued;

    private float verticalVelocity;    
    public float gravity = 9.82f;

    private Vector3 moveDir;
    private Vector3 moveInput;

    public int Health = 15;

    private float invisibilityTimer = 0f;
    public bool Invisible => invisibilityTimer > 0f;

    public float InvisibilityLength = 0.6f;
    private bool invisibilityColorActive = false;

    [SerializeField] private bool debugInvisible;
    public bool Parrying;

    private Material material;
    public GameObject body;

    protected override void OnEntityEnable()
    {
        Player existing = TrackerHost.Current.Get<Player>();
        if (existing != null && existing != this)
        {
            Debug.Log("Player already exists, cancelling spawn.");
            Destroy(gameObject);
            return;
        }

        base.OnEntityEnable();
    }
    protected override void OnStart()
    {
        controller = GetComponent<CharacterController>();
        material = body.GetComponent<MeshRenderer>().material;

        camera1 = Camera.main.transform;

        parryManager = TrackerHost.Current.Get<ParryManager>();
        if (parryManager == null)
            return;
        parryManager.OnParryStart += HandleParryStart;
        parryManager.OnParryEnd += HandleParryEnd;
        parryManager.OnParryCooldownEnd += HandleParryCooldownEnd;
        parryManager.OnParried += Parried;
    }

    protected override void OnUpdate()
    {
        GroundCheck();
        MovementHandler();
        Turn();

        UpdateInvisibility();
        debugInvisible = Invisible;
    }

    void GroundCheck()
    {
        // origin a little above the bottom of the CharacterController
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float rayLength = (MainCollider.height / 2) + groundCheckDistance;

        // raycast straight down
        Grounded = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength);

        // optional: snap player slightly to ground if needed
        if (Grounded)
        {
            float desiredY = hit.point.y + controller.skinWidth;
            if (transform.position.y < desiredY)
                transform.position = new Vector3(transform.position.x, desiredY, transform.position.z);
        }

        // optional debug
        Debug.DrawRay(origin, Vector3.down * rayLength, Grounded ? Color.green : Color.red);
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            Sprinting = true;
        }
        else
        {
            Sprinting = false;
        }
    }

    void MovementHandler()
    {
        // Decide movement speed
        float targetSpeed = Sprinting && Grounded ? sprintSpeed : walkSpeed;

        // Get movement direction relative to camera
        Vector3 camForward = camera1.forward;
        Vector3 camRight = camera1.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        move.Normalize();

        // Apply horizontal movement
        moveDir = move * targetSpeed;

        // Gravity & jumping
        if (Grounded)
        {
            // Snap to ground
            if (verticalVelocity < -2f)
                verticalVelocity = -2f;

            // Jump
            if (jumpQueued)
            {
                jumpQueued = false;
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
        }
        else
        {
            // Apply gravity over time
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Combine vertical & horizontal
        Vector3 finalMove = moveDir;
        finalMove.y = verticalVelocity;

        // Move the controller
        controller.Move(finalMove * Time.deltaTime);
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Turn()
    {
        if (currentTarget != null)
        {
            Vector3 target = Vector3.Normalize(currentTarget.position - transform.position);
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(target.x, 0, target.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }
        else
        {
            if (Mathf.Abs(moveInput.x) > 0 || Mathf.Abs(moveInput.y) > 0)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && Grounded)
            jumpQueued = true;
    }

    public void LockOn(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0)
        {
            TargetEnemy();
        }
        else
        {
            currentTarget = null;
            if (iconCopy != null)
            {
                Destroy(iconCopy);
            }
        }
    }
    private void TargetEnemy()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, 25f, colliders, LayerMask.GetMask("Enemy"));

        if (count > 0)
        {
            Collider closest = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in colliders
                         .Where(a => a != null && Math.Abs(a.transform.position.y - transform.position.y) < 4))
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }

            currentTarget = closest.transform;

            if (currentTarget != null)
            {
                if (LockOnIcon != null)
                {
                    if (iconCopy == null)
                    {
                        iconCopy = Instantiate(LockOnIcon, currentTarget.transform);
                    }
                    BillboardSprite billboardSprite = iconCopy.GetComponent<BillboardSprite>();
                    billboardSprite.target = currentTarget.position + new Vector3(0, 5, 0);
                }
            }
        }
        else
        {
            currentTarget = null; // nothing in range
        }
    }

    public void TakeDamage(int amount)
    {
        if (Invisible || Parrying)
            return;

        Health -= amount;
        if (Health <= 0)
        {
            Die();
            return;
        }

        StartInvisible(InvisibilityLength, true);
        CameraActions.Main.Shake(0.15f, 0.3f, 0.2f);
    }

    public void StartInvisible(float length = 0.6f, bool changeColor = false)
    {
        invisibilityTimer = length;
        invisibilityColorActive = changeColor;

        if (changeColor)
            material.SetColor("_BaseColor", new Color(0.5f, 0.5f, 1f, 0.25f));
    }

    private void UpdateInvisibility()
    {
        if (invisibilityTimer > 0f)
        {
            invisibilityTimer -= Time.deltaTime;
            if (invisibilityTimer <= 0f)
            {
                invisibilityTimer = 0f;

                if (invisibilityColorActive)
                {
                    material.SetColor("_BaseColor", Color.blue);
                    invisibilityColorActive = false;
                }
            }
        }
    }

    protected virtual void Die()
    {
        material.SetColor("_BaseColor", Color.magenta);
    }

    private void Parried()
    {
        StartInvisible(parryManager.parryLength);

        ParticleSpawner.Spawn(Particles.P_spark, transform.position);
        CameraActions.Main.Punch(-0.75f, 0.1f);
    }
    private void HandleParryStart()
    {
        DamageCollider.enabled = false;
        material.SetColor("_BaseColor", Color.red);
        //Debug.Log("Parry Start!");
    }
    private void HandleParryEnd()
    {
        DamageCollider.enabled = true;
        material.SetColor("_BaseColor", Color.green);
        //Debug.Log("Parry End!");
    }
    private void HandleParryCooldownEnd()
    {
        material.SetColor("_BaseColor", Color.blue);
        //Debug.Log("Parry Cooldown End!");
    }
}
