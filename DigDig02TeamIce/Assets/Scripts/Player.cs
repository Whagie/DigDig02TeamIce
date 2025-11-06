using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;

public class Player : Entity, IHurtbox
{
    public GameObject Owner => gameObject;
    public Collider Collider => DamageCollider;
    public bool UseMeshCollision { get; set; } = false;

    [SerializeField] private LayerMask layers;
    [SerializeField] private LayerMask groundLayers;
    public LayerMask LayerMask => layers;

    public Collider DetectionCollider;
    public CapsuleCollider MainCollider;
    public CapsuleCollider DamageCollider;

    private Collider[] colliders = new Collider[50];
    public static GameObject currentTarget;
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
    private bool jumped;
    public bool Jumping;
    //private bool lungeQueued;
    //private Vector3 lungeDir;

    private float verticalVelocity;    
    public float gravity = 9.82f;

    private Vector3 moveDir;
    private Vector3 moveInput;

    public int Health = 15;
    public int MaxHealth = 15;

    private float invisibilityTimer = 0f;
    public bool Invisible => invisibilityTimer > 0f;

    public float InvisibilityLength = 0.6f;
    private bool invisibilityColorActive = false;

    [SerializeField] private bool debugInvisible;
    public bool Parrying;

    private Material material;
    public GameObject body;

    public event System.Action<int> OnPlayerTakeDamage;

    public int Energy = 0;
    public int MaxEnergy = 6;
    [SerializeField] private float energyTimer = 0.6f;
    public event System.Action<int> OnChangeEnergy;

    protected override void OnEntityEnable()
    {
        HitboxManager.Register(this);
        Player existing = GameObject.FindObjectOfType<Player>();
        if (existing != null && existing != this)
        {
            Debug.Log("Player already exists, cancelling spawn.");
            Destroy(gameObject);
            return;
        }

        base.OnEntityEnable();
    }
    protected override void OnEntityDisable()
    {
        HitboxManager.Unregister(this);
        base.OnEntityDisable();
    }
    protected override void OnStart()
    {
        if (Health > MaxHealth)
        {
            MaxHealth = Health;
        }
        controller = GetComponent<CharacterController>();
        material = body.GetComponent<MeshRenderer>().material;

        camera1 = Camera.main.transform;

        parryManager = GameObject.FindObjectOfType<ParryManager>();
        if (parryManager == null)
        {
            Debug.LogWarning("ParryManager is null!");
            return;
        }
        parryManager.OnParryStart += HandleParryStart;
        parryManager.OnParryEnd += HandleParryEnd;
        parryManager.OnParryCooldownEnd += HandleParryCooldownEnd;
        parryManager.OnParried += Parried;
    }

    protected override void OnUpdate()
    {
        GroundCheck();
        Move();
        Jump();
        Sprint();
        LockOn();

        if (!Parrying)
        {
            MovementHandler();
            Turn();
        }

        UpdateInvisibility();
        debugInvisible = Invisible;
    }

    void GroundCheck()
    {
        Vector3 dir = MainCollider.direction == 0 ? Vector3.right : MainCollider.direction == 1 ? Vector3.up : Vector3.forward;
        float radius = MainCollider.radius * Mathf.Max(MainCollider.transform.lossyScale.x, MainCollider.transform.lossyScale.y, MainCollider.transform.lossyScale.z);
        float height = MainCollider.height * 0.5f * Mathf.Max(MainCollider.transform.lossyScale.x, MainCollider.transform.lossyScale.y, MainCollider.transform.lossyScale.z);
        Vector3 center = MainCollider.transform.TransformPoint(MainCollider.center);
        Vector3 origin = center - dir * (height - radius) - new Vector3(0f, (radius / 2), 0f);

        float rayLength = MainCollider.radius + groundCheckDistance;

        //// origin a little above the bottom of the CharacterController
        //Vector3 origin = transform.position + Vector3.up * 0.1f;
        //float rayLength = (MainCollider.height / 2) + groundCheckDistance;

        Grounded = false;
        if (Physics.CheckSphere(origin, rayLength, groundLayers))
        {
            Grounded = true;
        }
        //Grounded = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength);

        // optional: snap player slightly to ground if needed
        //if (Grounded)
        //{
        //    float desiredY = info.point.y + controller.skinWidth;
        //    if (transform.position.y < desiredY)
        //        transform.position = new Vector3(transform.position.x, desiredY, transform.position.z);
        //}

        // optional debug
        DrawMethods.WireSphere(origin, rayLength, Grounded ? Color.green : Color.red);
    }

    public void Sprint()
    {
        if (UserInput.SprintHeld)
        {
            Sprinting = true;
        }
        else
        {
            Sprinting = false;
        }

        if (Parrying && UserInput.SprintPressed)
        {
            //lungeQueued = true;
            //lungeDir = moveDir.normalized;
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
            jumped = false;
            Jumping = false;
            // Snap to ground
            if (verticalVelocity < -2f)
                verticalVelocity = -2f;

            // Jump
            if (jumpQueued)
            {
                jumpQueued = false;
                jumped = true;
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
        }
        else
        {
            // Apply gravity over time
            verticalVelocity -= gravity * Time.deltaTime;
            if (jumped)
            {
                Jumping = true;
            }
        }

        // Combine vertical & horizontal
        Vector3 finalMove = moveDir;
        finalMove.y = verticalVelocity;

        // Move the controller
        controller.Move(finalMove * Time.deltaTime);
    }

    public void Move()
    {
        moveInput = UserInput.MoveInput;
    }

    void Turn()
    {
        if (currentTarget != null)
        {
            Vector3 target = Vector3.Normalize(currentTarget.transform.position - transform.position);
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

    public void Jump()
    {
        if (UserInput.JumpPressed && Grounded)
            jumpQueued = true;
    }

    public void LockOn()
    {
        if (UserInput.LockOnHeld)
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

            if (closest.gameObject != null)
            {
                currentTarget = closest.gameObject;
            }

            if (currentTarget != null)
            {
                if (LockOnIcon != null)
                {
                    if (iconCopy == null)
                    {
                        iconCopy = Instantiate(LockOnIcon, currentTarget.transform);
                    }
                    BillboardSprite billboardSprite = iconCopy.GetComponent<BillboardSprite>();
                    billboardSprite.target = currentTarget.transform.position + new Vector3(0, 5, 0);
                }
            }
        }
        else
        {
            currentTarget = null; // nothing in range
        }
    }

    public void OnHit(IHitbox source)
    {
        if (!Parrying)
        {
            TakeDamage(source.Damage);
        }
    }
    public void TakeDamage(int amount)
    {
        if (Invisible || Parrying)
            return;

        if (Health > 0)
        {
            Health -= amount;
        }
        if (Health <= 0)
        {
            Die();
            return;
        }

        OnPlayerTakeDamage?.Invoke(Health);

        StartInvisible(InvisibilityLength, true);
        CameraActions.Main.Shake(0.15f, 0.3f, 0.2f);
        Freezer.Freeze(0.05f);
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

    public void GiveEnergy()
    {
        StartCoroutine(EnergyCoroutine());
    }
    private IEnumerator EnergyCoroutine()
    {
        yield return new WaitForSeconds(energyTimer);

        GetEnergy();
    }
    public void GetEnergy(int amount = 1)
    {
        if (Energy < MaxEnergy)
        {
            Energy += amount;
            OnChangeEnergy?.Invoke(Energy);
        }
    }
    public void ConsumeEnergy(int amount)
    {
        if (amount < 0)
            return;

        int energyAfterConsumption = Energy - amount;

        if (energyAfterConsumption < 0)
        {
            Energy -= (amount - Math.Abs(energyAfterConsumption));
        }
        else if (energyAfterConsumption >= 0)
        {
            Energy -= amount;
        }

        OnChangeEnergy?.Invoke(Energy);
    }

    private void Parried()
    {
        StartInvisible(parryManager.parryLength);

        Freezer.Freeze(0.05f);
        ParticleSpawner.Spawn(Particles.P_spark, transform.position);
        CameraActions.Main.Punch(-0.75f, 0.1f);
    }
    private void HandleParryStart()
    {
        DamageCollider.enabled = false;
        material.SetColor("_BaseColor", Color.red);
    }
    private void HandleParryEnd()
    {
        DamageCollider.enabled = true;
        //if (lungeQueued)
        //{
        //    lungeQueued = false;
        //    Lunge(lungeDir, 3f, 0.1f);
        //}

        material.SetColor("_BaseColor", Color.green);
    }
    private void HandleParryCooldownEnd()
    {
        material.SetColor("_BaseColor", Color.blue);
    }

    public virtual void Lunge(Vector3 direction, float distance, float duration)
    {
        StartCoroutine(LungeRoutine(direction, distance, duration));
    }

    private IEnumerator LungeRoutine(Vector3 direction, float distance, float duration)
    {
        Vector3 start = transform.position;
        Vector3 target = start + direction * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }
}
