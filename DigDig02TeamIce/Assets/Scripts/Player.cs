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

    public Vector3 moveDir;
    public Vector3 moveInput;

    private bool stopMovement = false;

    // Pushback
    private Vector3 pushbackVelocity = Vector3.zero;
    private float pushbackTimer = 0f;

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

    public int Energy = 8;
    public int MaxEnergy = 8;
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

    private float timeUntilPushStart = 0.3f;
    private float pushStartTimer = 0.3f;
    private float timeUntilPushMove = 0.75f;
    public bool Pushing = false;
    private bool prePushing;
    private PushableObject objectToPush;
    private Coroutine pushCoroutine;
    private Vector3 pushDirection;
    private RaycastHit pushHit;

    private float prevTurnSpeed = 8f;
    private float prevMoveSpeed = 5f;

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
        if (Energy > MaxEnergy)
        {
            MaxEnergy = Energy;
        }
        controller = GetComponent<CharacterController>();
        Companion = FindObjectOfType<Companion>();
        material = Companion.Body.GetComponent<Renderer>().sharedMaterials.Where(m => m.name == "CrystalBall").FirstOrDefault();

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

        prevTurnSpeed = turnSpeed;
        prevMoveSpeed = walkSpeed;
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
            if (!Attacking)
            {
                animator.SetBool("Sprinting", Sprinting);
            }
            else
            {
                animator.SetBool("Sprinting", false);
            }

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

        ConstructCarry();

        //DrawUI.Draw(pushStartTimer.ToString(), new Vector2(Screen.width * 0.8f, Screen.height * 0.1f), Color.white, 8);

        bool hitPushable = Physics.Raycast(Center.position, transform.forward, out RaycastHit hit, 0.75f, LayerMask.GetMask("Pushable", "LightReflector"));

        PushableObject hitObj = hitPushable
            ? hit.collider.GetComponent<PushableObject>()
            : null;

        bool moving = moveInput.magnitude > 0.05f;

        if (!hitObj || !moving || hitObj.Moving && hitObj.MovesUntilStop)
        {
            if (stopMovement)
                return;

            ResetPushState();
            return;
        }

        if (!prePushing && !Pushing)
        {
            prePushing = true;
            objectToPush = hitObj;
            pushStartTimer = timeUntilPushStart;
            prevTurnSpeed = turnSpeed;
            prevMoveSpeed = walkSpeed;
            pushHit = hit;
            animator.SetBool("Pushing", true);
        }

        if (prePushing && !Pushing)
        {
            pushStartTimer -= Time.deltaTime;

            turnSpeed = 0f;
            walkSpeed = 0.5f;
            SnapRotationToObject();

            if (pushStartTimer <= 0f)
            {
                EnterPushState();
            }
        }
    }

    void GroundCheck()
    {
        Vector3 dir = MainCollider.direction == 0 ? Vector3.right : MainCollider.direction == 1 ? Vector3.up : Vector3.forward;
        float radius = MainCollider.radius * Mathf.Max(MainCollider.transform.lossyScale.x, MainCollider.transform.lossyScale.y, MainCollider.transform.lossyScale.z);
        float height = MainCollider.height * 0.5f * Mathf.Max(MainCollider.transform.lossyScale.x, MainCollider.transform.lossyScale.y, MainCollider.transform.lossyScale.z);
        Vector3 center = MainCollider.transform.TransformPoint(MainCollider.center);
        Vector3 origin = center - dir * (height - radius) - new Vector3(0f, (radius / 2), 0f);

        float rayLength = MainCollider.radius + groundCheckDistance;

        Grounded = false;
        if (Physics.CheckSphere(origin, rayLength, groundLayers))
        {
            Grounded = true;
        }

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
    }

    void MovementHandler()
    {
        float targetSpeed = Sprinting && Grounded ? sprintSpeed : walkSpeed;
        if (Attacking)
        {
            targetSpeed = walkSpeed * 0.5f;
        }

        Vector3 camForward = _camera.transform.forward;
        Vector3 camRight = _camera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        move.Normalize();

        if (!stopMovement)
        {
            moveDir = move * targetSpeed;
        }
        else
        {
            moveDir = Vector3.zero;
        }

        // ----- PUSHBACK -----
        if (pushbackTimer > 0f)
        {
            // Override normal movement with pushback
            moveDir = pushbackVelocity;

            pushbackTimer -= Time.deltaTime;
            if (pushbackTimer <= 0f)
                pushbackVelocity = Vector3.zero;
        }

        if (Grounded)
        {
            jumped = false;
            Jumping = false;

            if (verticalVelocity < -2f)
                verticalVelocity = -2f;

            if (jumpQueued)
            {
                jumpQueued = false;
                jumped = true;
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
            if (jumped)
            {
                Jumping = true;
            }
        }

        Vector3 finalMove = moveDir;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    public void Move()
    {
        moveInput = UserInput.MoveInput;
    }

    void Turn()
    {
        if (stopMovement)
            return;

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
                iconCopy = null;
            }
        }
    }

    private void TargetEnemy()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            25f,
            colliders,
            LayerMask.GetMask("Enemy")
        );

        if (count == 0)
        {
            currentTarget = null;
            return;
        }

        Collider closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var enemy = colliders[i];
            if (enemy == null)
                continue;

            // Vertical filtering
            float yDiff = Mathf.Abs(enemy.transform.position.y - transform.position.y);
            if (yDiff >= 4f)
                continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        if (closest != null)
        {
            currentTarget = closest.gameObject;
        }
        else
        {
            currentTarget = null;
        }

        if (currentTarget != null && LockOnIcon != null)
        {
            if (iconCopy == null)
            {
                iconCopy = Instantiate(LockOnIcon, currentTarget.transform);
            }

            var billboard = iconCopy.GetComponent<BillboardSprite>();
            billboard.target = currentTarget.transform.position + new Vector3(0, 5f, 0);
        }
        else if (iconCopy != null)
        {
            Destroy(iconCopy);
            iconCopy = null;
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
        animator.SetBool("Attack", false);
        animator.SetBool("FollowUp", false);
        animator.SetLayerWeight(2, 0f);
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
        ApplyPushback(final, 2.5f, 0.125f);
    }
    private void HandleParryStart()
    {
        animator.SetBool("Blocked", true);
        material.SetColor("_BaseColor", Color.red);
    }
    private void HandleParryEnd()
    {
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
        pushbackVelocity = direction.normalized * force;
        pushbackTimer = duration;
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

                animator.SetBool("FollowUp", false);
            }
        }

        animator.SetLayerWeight(2, Attacking ? 1f : 0f);

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

    public void ConstructCarry()
    {
        if (Companion == null)
            return;

        if (UserInput.InteractPressed && !Parrying)
        {
            if (!Companion.isCarrying)
            {
                Companion.StartCarry(GameObject.Find("Pickup").transform);
                return;
            }
            else
            {
                Companion.StopCarry();
                return;
            }
        }
    }

    void EnterPushState()
    {
        if (Pushing)
            return;

        Pushing = true;
        prePushing = false;
        animator.SetBool("Pushing", true);

        pushCoroutine = StartCoroutine(PushRoutine(objectToPush));
    }
    void ResetPushState()
    {
        if (!prePushing && !Pushing)
            return;

        prePushing = false;
        Pushing = false;

        if (pushCoroutine != null)
        {
            StopCoroutine(pushCoroutine);
            pushCoroutine = null;
        }

        animator.SetBool("Pushing", false);

        objectToPush = null;
        pushStartTimer = timeUntilPushStart;
        turnSpeed = prevTurnSpeed;
        walkSpeed = prevMoveSpeed;
    }

    IEnumerator PushRoutine(PushableObject pushable)
    {
        SnapRotationToObject();
        turnSpeed = 0f;

        Vector2Int moveDir = PushableObject.PushDirToGrid(pushDirection);

        bool canPush =
            (moveDir.x != 0 && pushable.CanPushX) ||
            (moveDir.y != 0 && pushable.CanPushZ);

        while (!canPush)
        {
            yield return null;
        }

        yield return new WaitForSeconds(timeUntilPushMove);

        // One-shot push
        if (pushable.MovesUntilStop)
        {
            int steps = pushable.GetMaxSteps(moveDir);
            float moveDuration = pushable.MoveDurationPerStep * steps;

            pushable.MoveSteps(moveDir, steps, moveDuration, delta => { });

            animator.SetTrigger("PushStumble");
            float animDuration = animator.runtimeAnimatorController.animationClips.Where(c => c.name == "PushStumble").FirstOrDefault().length / 1.2f;

            stopMovement = true;
            yield return new WaitForSeconds(animDuration);
            stopMovement = false;

            ResetPushState();

            yield break;
        }

        // Continuous push
        while (Pushing)
        {
            int steps;
            if (pushable.StepsToMove != 0)
            {
                steps = pushable.StepsToMove;
            }
            else
            {
                steps = 1;
            }

            float moveDuration = pushable.MoveDurationPerStep * steps;

            pushable.MoveSteps(moveDir, steps, moveDuration, delta => { transform.position += delta; });
            // Add pushable object's movement to player, so it stays connected to it, without parenting the player

            walkSpeed = 0f;

            yield return new WaitForSeconds(moveDuration);

            walkSpeed = 0.5f;

            yield return new WaitForSeconds(timeUntilPushMove);
        }

        ResetPushState();
    }

    void SnapRotationToObject()
    {
        Vector3 dir = -pushHit.normal;
        dir.y = 0f;
        pushDirection = SnapToAxis(dir);

        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(pushDirection);
    }
    Vector3 SnapToAxis(Vector3 dir)
    {
        return Mathf.Abs(dir.x) > Mathf.Abs(dir.z)
            ? new Vector3(Mathf.Sign(dir.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(dir.z));
    }
}
