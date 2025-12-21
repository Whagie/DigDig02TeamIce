using FIMSpace.FTail;
using Game.Core;
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Player : Entity, IHurtbox, IPushbackReceiver
{
    public GameObject Owner => gameObject;
    public Collider Collider => DamageCollider;
    public bool UseMeshCollision { get; set; } = false;

    [SerializeField] private LayerMask layers;
    [SerializeField] public LayerMask groundLayers;
    public LayerMask LayerMask => layers;

    public Collider DetectionCollider;
    public CapsuleCollider MainCollider;
    public CapsuleCollider DamageCollider;

    public Transform Center;

    private Collider[] colliders = new Collider[50];
    public static GameObject currentTarget;
    [SerializeField] private GameObject LockOnIcon;
    private GameObject iconCopy;

    public CameraMovement _camera;

    private ParryManager parryManager;

    CharacterController controller;

    public TailAnimator2 Tail;

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

    // Pushback
    private Vector3 pushVelocity = Vector3.zero;
    private float pushTimer = 0f;

    public int Health = 15;
    public int MaxHealth = 15;

    private float airTimeBeforeAnimTransitionTimer = 0f;
    private bool airTimerActive = false;

    public bool Dead { get; private set; } = false;
    public event System.Action OnPlayerDie;
    public event System.Action OnPlayerResurrect;

    public string sceneAtDeath;

    private float invisibilityTimer = 0f;
    public bool Invisible;

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

    public Animator animator;

    public Companion Companion;

    public bool Attacking { get; set; } = false;
    public bool CanAttack { get; set; } = true;
    public bool AllowFollowUpAttack = false;
    public bool AttackBuffered = false;

    public GameObject Wrench;
    public Collider WrenchCollider;
    private MeleeAttack wrenchAttack;

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

        parryManager.OnParryStart -= HandleParryStart;
        parryManager.OnParryEnd -= HandleParryEnd;
        parryManager.OnParryCooldownEnd -= HandleParryCooldownEnd;
        parryManager.OnParried -= Parried;

        base.OnEntityDisable();
    }
    protected override void OnStart()
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
        if (spawnPoint != null)
        {
            gameObject.transform.position = spawnPoint.transform.position;
        }

        if (Health > MaxHealth)
        {
            MaxHealth = Health;
        }
        controller = GetComponent<CharacterController>();
        Companion = FindObjectOfType<Companion>();
        material = Companion.GetComponent<MeshRenderer>().material;

        _camera = GameObject.FindObjectOfType<CameraMovement>();

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

        wrenchAttack = Wrench.GetComponent<MeleeAttack>();
        wrenchAttack.hitCollider = WrenchCollider;
        wrenchAttack.EnemyOwner = null;
        wrenchAttack.LayerMask = LayerMask.GetMask("Enemy");
        WrenchCollider.enabled = false;
    }

    protected override void OnUpdate()
    {
        if (Health <= 0 && !Dead)
        {
            Die();
            return;
        }

        if (Dead) return;

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

        if (Grounded)
        {
            animator.SetFloat("Move", controller.velocity.magnitude);
            animator.SetBool("Sprinting", Sprinting);
            airTimerActive = false;
        }
        else
        {
            if (!airTimerActive)
            {
                airTimerActive = true;
                StartCoroutine(AirTimeTimer(0.075f));
            }
        }

        if (!Parrying)
        {
            animator.SetBool("Blocked", false);
        }

        Attack();
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
        Vector3 camForward = _camera.transform.forward;
        Vector3 camRight = _camera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        move.Normalize();

        // Apply horizontal movement
        moveDir = move * targetSpeed;

        // ----- PUSHBACK -----
        if (pushTimer > 0f)
        {
            // Override normal movement with pushback
            moveDir = pushVelocity;

            // Decrease timer
            pushTimer -= Time.deltaTime;
            if (pushTimer <= 0f)
                pushVelocity = Vector3.zero;
        }

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
        if (parryManager.LastParryFrame == Time.frameCount)
            return;

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
        Invisible = true;

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
                Invisible = false;

                if (invisibilityColorActive)
                {
                    material.SetColor("_BaseColor", Color.blue);
                    invisibilityColorActive = false;
                }
            }
        }
    }

    public virtual void Die()
    {
        Dead = true;
        sceneAtDeath = SceneManager.GetActiveScene().name;
        material.SetColor("_BaseColor", Color.magenta);
        DamageCollider.enabled = false;
        MainCollider.enabled = false;
        parryManager.ParryCollider.enabled = false;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetBool("Dead", true);
        Tail.enabled = false;
        Tail.gameObject.SetActive(false);
        _camera.Actions.CancelAllActions();
        OnPlayerDie?.Invoke();

        SceneSwapManager.LoadDeathScene();
    }
    public virtual void Resurrect()
    {
        Dead = false;
        Health = MaxHealth;
        StartInvisible(1.5f, true);
        material.SetColor("_BaseColor", Color.blue);
        DamageCollider.enabled = true;
        MainCollider.enabled = true;
        parryManager.ParryCollider.enabled = true;
        animator.updateMode = AnimatorUpdateMode.Normal;
        Tail.enabled = true;
        Tail.gameObject.SetActive(true);
        Tail.User_ReposeTail();
        OnPlayerResurrect?.Invoke();
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

    private void Parried(IHitbox hitbox)
    {
        //StartInvisible(parryManager.parryLength);

        animator.SetTrigger("ParriedHit");
        Freezer.Freeze(0.1f);
        ParticleSpawner.Spawn(Particles.P_spark, transform.position);
        CameraActions.Main.Punch(-0.75f, 0.1f);

        Vector3 pushDir = hitbox.Owner.transform.position - transform.position;
        Vector3 final = new Vector3(-pushDir.x, 0, -pushDir.z);
        ApplyPushback(final, 2.5f, 0.15f);
    }
    private void HandleParryStart()
    {
        animator.SetBool("Blocked", true);
        material.SetColor("_BaseColor", Color.red);
    }
    private void HandleParryEnd()
    {
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

    private IEnumerator AirTimeTimer(float length)
    {
        airTimeBeforeAnimTransitionTimer = length;

        while (airTimeBeforeAnimTransitionTimer > 0)
        {
            if (Grounded)
            {
                airTimerActive = false;
                yield break;
            }
            airTimeBeforeAnimTransitionTimer -= Time.deltaTime;
            yield return null;
        }

        animator.SetFloat("Move", 0f);
        animator.SetBool("Sprinting", false);
        airTimerActive = false;
    }

    public void ApplyPushback(Vector3 direction, float force, float duration)
    {
        pushVelocity = direction.normalized * force;
        pushTimer = duration;
    }

    private void Attack()
    {
        var state = animator.GetCurrentAnimatorStateInfo(2);
        bool inTransition = animator.IsInTransition(2);
        AnimatorStateInfo nextState = default;

        if (inTransition)
            nextState = animator.GetNextAnimatorStateInfo(2);

        bool isInAttackState =
            state.IsTag("Attack") ||
            (inTransition && nextState.IsTag("Attack"));

        // --- Authoritative attacking state ---
        Attacking = isInAttackState;

        // --- Input handling ---
        if (UserInput.MeleeAttackPressed && !Parrying)
        {
            if (Attacking)
            {
                if (AllowFollowUpAttack)
                {
                    AllowFollowUpAttack = false;
                    AttackBuffered = true;
                }
            }
            else
            {
                // Starting a brand new attack
                Attacking = true;
                AttackBuffered = false;
                AllowFollowUpAttack = false;

                animator.SetBool("FollowUp", false); // IMPORTANT
            }
        }

        // --- Layer weight ---
        animator.SetLayerWeight(2, Attacking ? 1f : 0f);

        // --- Animator params ---
        animator.SetBool("Attack", Attacking);
        animator.SetBool("FollowUp", AttackBuffered);

        // --- Hard reset when idle ---
        if (!Attacking && !isInAttackState)
        {
            AttackBuffered = false;
            AllowFollowUpAttack = false;
            animator.SetBool("FollowUp", false);
        }
    }

    public void ActivateAttackHitbox(int activate = 1)
    {
        if (activate == 1)
        {
            wrenchAttack.Activate();
            wrenchAttack.gizmoColor = Color.red;
        }
        else
        {
            wrenchAttack.Deactivate();
            wrenchAttack.gizmoColor = Color.blue;
        }
    }
    public void AllowNextAttackBuffer(int allow = 1)
    {
        if (allow == 1)
        {
            AllowFollowUpAttack = true;
        }
        else
        {
            AllowFollowUpAttack = false;
            AttackBuffered = false;
        }
    }
}
