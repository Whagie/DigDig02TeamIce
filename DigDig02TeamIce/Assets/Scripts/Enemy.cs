using Game.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : MonoBehaviourID, IHurtbox, IPushbackReceiver
{
    public GameObject Owner => gameObject;
    public Collider Collider { get; protected set; }
    public bool UseMeshCollision { get; set; } = false;

    protected static Player player;

    [SerializeField] public EnemyStats stats;

    private SessionSaveData.EnemyDeathData DeathData;

    public Transform Center;

    public LayerMask LayerMask => stats.layers;
    private static LayerMask Obstacles;

    [SerializeField] private GameObject enemyUIObject;
    [SerializeField] public EnemyUI enemyUI;

    public int Health => stats.health;
    public int MaxHealth => stats.maxHealth;
    public float MarginDegrees => stats.marginDegrees;
    public float WanderSpeed => stats.wanderSpeed;
    public float ChaseSpeed => stats.chaseSpeed;
    public float WanderRadius => stats.wanderRadius;
    public float WaitTime => stats.waitTime;
    public float RotationSpeed => stats.rotationSpeed;

    public float AlertRadius { get; set; }
    public bool DetectedPlayer { get; set; } = false;
    public bool LookingForPlayer { get; set; } = false;
    public bool SeeingPlayer { get; set; } = false;
    public bool FacingPlayer { get; set; } = false;
    public bool Dead { get; set; } = false;
    public bool Attacking { get; set; } = false;
    public bool Idle { get; set; } = true;
    public bool InCombat { get; set; } = false;
    public bool IsAwake { get; set; } = true;
    public bool Stunned { get; set; } = false;

    public bool Wandering { get; set; } = false;
    public bool ShouldWander { get; set; } = true;
    public bool ShouldMove { get; set; } = true;

    public bool ShouldRotate = true;
    public bool AllowPushback = true;

    public List<HitFlash> ChildrenWithFlashEffect;

    [Serializable]
    public class EnemyAction
    {
        public string TriggerName;
        public float Weight = 1f;
        public Func<bool> CanUse; // optional condition
        public ActionModifier Modifier;
        public string[] CustomData;
    }

    public EnemyAction[] Actions;
    private EnemyAction _currentAction;
    public float ActionInterval => stats.actionInterval;
    private bool hasAttacked = false;
    private bool tryingFirstAttack = false;
    private bool canForceIdle = false;

    private float intervalTimer = 0f;

    private bool firstDamage = true;

    public Animator _animator;
    private float _timer;

    public NavMeshAgent NavAgent;

    public float tempSpeed = 1f;
    public bool speedOverride = false;

    public List<VisionCone> VisionCones = new();

    public int ProjectileDamage = 1;
    public GameObject projectilePrefab;

    private Color sphereColor = Color.blue;
    private Color visionConeColor = Color.blue;

    public float DistanceToPlayer;

    // Pushback
    private Vector3 pushVelocity = Vector3.zero;
    private float pushTimer = 0f;

    protected virtual void OnEnable()
    {
        HitboxManager.Register(this);
    }
    protected virtual void OnDisable()
    {
        HitboxManager.Unregister(this);
    }
    protected virtual void Awake()
    {
        if (Center == null)
            Center = transform;

        _animator = GetComponent<Animator>();
        if (_animator != null)
        {
            InitializeActions();
        }

        NavAgent = GetComponent<NavMeshAgent>();

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Collider = col;
        }
    }
    protected virtual void Start()
    {
        if (player == null)
        {
            player = GameObject.FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogWarning("Error, player not found! Adding temporary player object...");
                player = new GameObject("tempPlayer").AddComponent<Player>();
            }
        }

        Obstacles = LayerMask.GetMask("Default", "Walls", "Pushable", "LightReflector", "Shrouders");

        VisionCones.Add(new VisionCone(Vector3.zero, Vector3.zero, stats.visionAngle, stats.visionLength));
        foreach (var cone in VisionCones)
        {
            cone.angle = stats.visionAngle;
            cone.rotation = stats.visionRotation;
            cone.length = stats.visionLength;
        }

        if (NavAgent != null)
        {
            NavAgent.angularSpeed = RotationSpeed;
        }

        Vector3 delta = Center.position - transform.position;
        delta.x = 0f;
        delta.z = 0f;
        enemyUI = Instantiate(enemyUIObject, Center.position + delta, transform.rotation, transform).GetComponent<EnemyUI>();

        if (enemyUI.EnemyOwner == null)
        {
            enemyUI.EnemyOwner = this;
        }

        if (_animator != null)
        {
            foreach (var param in _animator.parameters)
            {
                if (param.name == "ForceIdle")
                {
                    canForceIdle = true;
                }
            }
        }

        if (SessionSaveData.Instance.TryGet(ID, out DeathData))
        {
            if (DeathData.Dead)
            {
                Dead = true;
                transform.SetPositionAndRotation(DeathData.Position, DeathData.Rotation);
                Die();
            }
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, Dead, transform.position, transform.rotation);
        }
    }

    protected virtual void Update()
    {
        if (Dead || !IsAwake)
            return;

        if (Stunned)
        {
            if (NavAgent != null)
            {
                HandlePushback();
            }
            return;
        }

        if (player == null)
        {
            player = GameObject.FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogWarning("Error, player not found! Adding temporary player object...");
                player = new GameObject("tempPlayer").AddComponent<Player>();
            }
        }

        foreach (var cone in VisionCones)
        {
            cone.angle = stats.visionAngle;
            cone.rotation = stats.visionRotation;
            if (SeeingPlayer)
            {
                cone.length = stats.chaseVisionLength;
            }
            else
            {
                cone.length = stats.visionLength;
            }
        }

        if (DetectedPlayer)
        {
            AlertRadius = stats.chaseAlertRadius;
        }
        else
        {
            AlertRadius = stats.alertRadius;
        }

        int playerMask = LayerMask.GetMask("Player");
        Collider[] hits = Physics.OverlapSphere(transform.position, AlertRadius, playerMask);
        bool playerInAlertRadius = false;
        Vector3 dirToPlayer = player.transform.position - transform.position;

        foreach (var c in hits)
        {
            if (c.GetComponent<Player>() != null)
            {
                if (Physics.Raycast(transform.position, dirToPlayer, out RaycastHit hit, AlertRadius + 1.5f, LayerMask.GetMask("Default", "Walls", "Player", "Pushable", "LightReflector", "Shrouders")))
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        playerInAlertRadius = true;
                    }
                }
                break;
            }
        }

        // --- Check vision
        bool playerInVision = IsTargetInVision(player.DetectionCollider);

        // --- Combine results
        DetectedPlayer = playerInAlertRadius || playerInVision;
        SeeingPlayer = playerInVision;

        // --- Set colors
        sphereColor = playerInAlertRadius ? Color.red : Color.blue;
        visionConeColor = playerInVision ? Color.red : Color.blue;

        // --- Check facing
        Vector3 toTarget = player.transform.position - transform.position;
        toTarget.y = 0f;
        toTarget.Normalize();

        Vector3 forwardXZ = transform.forward;
        forwardXZ.y = 0f;
        forwardXZ.Normalize();

        float dot = Vector3.Dot(forwardXZ, toTarget);
        float cosMargin = Mathf.Cos(MarginDegrees * Mathf.Deg2Rad);
        FacingPlayer = dot >= cosMargin;

        DistanceToPlayer = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(player.transform.position.x, 0f, player.transform.position.z));

        if (_animator != null)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsTag("Attack"))
            {
                Attacking = true;
            }
            else
            {
                Attacking = false;
            }
            if (info.IsTag("Idle"))
            {
                Idle = true;
            }
            else
            {
                Idle = false;
            }

            _timer += Time.deltaTime;

            // normal behavior after first attack
            if (hasAttacked)
            {
                if (_timer >= ActionInterval && !Attacking)
                {
                    _timer = 0f;
                    PickAction();
                }
            }
            else if (!tryingFirstAttack)
            {
                tryingFirstAttack = true;
                TryFirstAttack();
            }

            if (canForceIdle && Attacking && !DetectedPlayer)
            {
                CancelCurrentAction();
            }
        }

        if (SeeingPlayer)
        {
            // If enemy sees the player, always be in combat state
            InCombat = true;
        }
        else if (DetectedPlayer && InCombat)
        {
            // If enemy doesn't see player, but still detect them,
            // remain in combat ONLY IF they were in combat state before
            InCombat = true;
        }
        else if (!canForceIdle && Attacking)
        {
            // If enemy doesn't see or detect player,
            // but canForceIdle is false and they're in an attack, remain in combat.
            // If canForceIdle is false, it means it shouldn't be able to exit attacks prematurely
            InCombat = true;
        }
        else
        {
            // If enemy doesn't see player, and they aren't already in combat state,
            // they should exit combat state regardless of if they detected the player.
            // 
            InCombat = false;
        }

        if (DetectedPlayer && ShouldRotate)
        {
            if (NavAgent.updateRotation)
            {
                RotateTowardsY(transform, player.transform.position, RotationSpeed * 3f);
            }
        }

        if (NavAgent != null)
        {
            HandlePushback();

            if (ShouldMove)
            {
                if (!NavAgent.isOnNavMesh)
                {
                    Debug.Log($"{name} is not on a NavMesh!");
                    return;
                }
                if (InCombat)
                {
                    // Stop Wandering when in combat
                    if (Wandering)
                    {
                        StopCoroutine(WanderRoutine());
                        Wandering = false;
                    }
                    if (DistanceToPlayer > NavAgent.stoppingDistance)
                    {
                        NavAgent.isStopped = false;
                        NavAgent.SetDestination(player.transform.position);
                    }
                    else
                    {
                        NavAgent.isStopped = true;
                        NavAgent.ResetPath();
                        NavAgent.velocity = Vector3.zero;
                    }
                    if (!Attacking)
                    {
                        SetSpeed(ChaseSpeed);
                    }
                    return;
                }
                if (!InCombat && !Wandering && ShouldWander)
                {
                    StartCoroutine(WanderRoutine());
                    SetSpeed(WanderSpeed);
                }
            }
        }
    }

    private IEnumerator WanderRoutine()
    {
        Wandering = true;

        while (!InCombat)
        {
            // Try to find valid random point
            if (TryGetRandomNavmeshPoint(transform.position, WanderRadius, NavMesh.AllAreas, out Vector3 newPos))
            {
                // Only set destination if valid
                if (NavAgent.SetDestination(newPos))
                {
                    // Wait for path to complete but avoid infinite wait
                    yield return new WaitUntil(() =>
                        !NavAgent.pathPending &&
                        NavAgent.hasPath &&
                        NavAgent.remainingDistance <= NavAgent.stoppingDistance
                    );
                }
            }
            else
            {
                // No valid point found – avoid log spam
                Debug.LogWarning($"{name} could not find a valid wander point.");
            }

            yield return new WaitForSeconds(WaitTime);
        }

        Wandering = false;
    }

    bool TryGetRandomNavmeshPoint(Vector3 origin, float radius, int areaMask, out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius + origin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 1f, areaMask))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    public void OnDrawGizmos()
    {
        if (Dead || !IsAwake)
            return;

        // --- Draw debug
        foreach (var cone in VisionCones)
            DrawMethods.DrawVisionCone(transform, cone, visionConeColor);

        DrawMethods.WireSphere(transform.position, AlertRadius, sphereColor);

        //float dist = Vector3.Distance(transform.position, player.transform.position);
        //Color lineColor;
        //if (Mathf.Abs(dist) <= 4)
        //{
        //    lineColor = Color.magenta;
        //}
        //else
        //{
        //    lineColor = Color.yellow;
        //}

        //DrawMethods.Line(transform.position, player.transform.position, lineColor);
    }

    public static void RotateTowardsY(Transform obj, Vector3 targetPosition, float rotationSpeed)
    {
        // direction to target, flattened
        Vector3 direction = targetPosition - obj.position;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f) return; // avoid zero-length

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        obj.rotation = Quaternion.RotateTowards(obj.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    protected void OnInterval(float interval, Action action)
    {
        intervalTimer += Time.deltaTime;

        if (intervalTimer >= interval)
        {
            intervalTimer -= interval; // keep leftover time
            action?.Invoke();
        }
    }

    public bool IsTargetInVision(Collider target)
    {
        Vector3 enemyPos = transform.position;

        foreach (var cone in VisionCones)
        {
            Vector3 coneOrigin = enemyPos + cone.offset;

            // Sample points depending on collider type
            List<Vector3> pointsToTest = new();

            if (target is SphereCollider sphere)
            {
                // center + points on sphere surface along cardinal directions
                pointsToTest.Add(sphere.transform.position + sphere.center); // center
                float r = sphere.radius;
                pointsToTest.Add(sphere.transform.position + sphere.center + Vector3.up * r);
                pointsToTest.Add(sphere.transform.position + sphere.center + Vector3.down * r);
                pointsToTest.Add(sphere.transform.position + sphere.center + Vector3.left * r);
                pointsToTest.Add(sphere.transform.position + sphere.center + Vector3.right * r);
                pointsToTest.Add(sphere.transform.position + sphere.center + Vector3.forward * r);
                pointsToTest.Add(sphere.transform.position + sphere.center + Vector3.back * r);
            }
            else
            {
                // fallback to bounds corners for other collider types
                Bounds bounds = target.bounds;
                pointsToTest.Add(bounds.center);
                pointsToTest.Add(bounds.min);
                pointsToTest.Add(bounds.max);
                pointsToTest.Add(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));
                pointsToTest.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
                pointsToTest.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
                pointsToTest.Add(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));
                pointsToTest.Add(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z));
                pointsToTest.Add(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
            }

            foreach (var point in pointsToTest)
            {
                Vector3 coneForward = transform.rotation * cone.GetRotation() * Vector3.forward;
                Vector3 toPoint = point - (transform.position + cone.offset);

                if (toPoint.magnitude > cone.length)
                    continue;

                float halfAngle = cone.angle * 0.5f;
                float angleToPoint = Vector3.Angle(coneForward, toPoint);

                if (angleToPoint <= halfAngle)
                {
                    if (!Physics.Raycast(coneOrigin, toPoint.normalized, toPoint.magnitude, Obstacles))
                        return true;
                }
            }
        }

        return false;
    }

    public virtual void OnHit(IHitbox source)
    {
        TakeDamage(source.Damage);
        if (source.Owner.CompareTag("Projectile"))
        {
            SpawnEnergy();
        }
        foreach (var child in ChildrenWithFlashEffect)
        {
            child.Flash();
        }
    }

    public virtual void TakeDamage(int amount)
    {
        if (amount < 0)
            return;

        if (firstDamage)
        {
            enemyUI.group.alpha = 1f;
            firstDamage = false;
        }

        int healthAfterDamage = stats.health - amount;

        if (healthAfterDamage < 0)
        {
            stats.health -= (amount - Math.Abs(healthAfterDamage));
        }
        else if (healthAfterDamage >= 0)
        {
            stats.health -= amount;
        }

        if (Health <= 0)
            Die();
    }
    protected virtual void Die()
    {
        Dead = true;
        SessionSaveData.Instance.AddOrUpdateData(ID, Dead, transform.position, transform.rotation);
        if (player.currentTarget == this.gameObject)
        {
            player.currentTarget = null;
        }
    }

    public virtual void HandleParried(IHurtbox by)
    {
        Collider collider = GetComponent<Collider>();
        if (collider is CapsuleCollider capsule)
        {
            SpawnEnergy(4f + capsule.radius);
        }
        else
        {
            SpawnEnergy();
        }

        foreach (var child in ChildrenWithFlashEffect)
        {
            child.Flash();
        }
    }

    protected virtual void InitializeActions() { }

    void TryFirstAttack()
    {
        if (FacingPlayer)
        {
            if (PickAction())
            {
                hasAttacked = true;
                tryingFirstAttack = false;
            }
            else
            {
                // retry later if too far or invalid
                tryingFirstAttack = false;
            }
        }
        else
        {
            tryingFirstAttack = false;
        }
    }
    public bool PickAction()
    {
        float totalWeight = 0f;
        foreach (var action in Actions)
        {
            if (action.CanUse == null || action.CanUse())
                totalWeight += action.Weight;
        }

        if (totalWeight <= 0f)
            return false;

        float choice = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var action in Actions)
        {
            if (action.CanUse == null || action.CanUse())
            {
                cumulative += action.Weight;
                if (choice <= cumulative)
                {
                    // Store the chosen action
                    _currentAction = action;

                    // Apply its modifiers
                    _currentAction.Modifier?.Evaluate(this);

                    _animator.SetTriggerOneFrame(this, action.TriggerName);

                    OnActionStart(_currentAction);
                    return true;
                }
            }
        }

        return false;
    }
    public virtual void OnActionStart(EnemyAction action)
    {

    }
    public virtual void OnActionEnd() // THIS IS NOT USED! IT SHOULD BE USED! FIX THIS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    {
        _currentAction?.Modifier?.Revert(this);
        _currentAction = null;
    }
    public void CancelCurrentAction()
    {
        if (_currentAction != null)
        {
            _currentAction.Modifier?.Revert(this);
            _currentAction = null;
        }

        _animator.SetTrigger("ForceIdle");
    }

    public virtual void Lunge(float distance, float duration)
    {
        StartCoroutine(LungeRoutine(distance, duration));
    }

    private IEnumerator LungeRoutine(float distance, float duration)
    {
        NavAgent.isStopped = true;
        Vector3 start = transform.position;
        Vector3 target = start + transform.forward * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        NavAgent.isStopped = false;
    }
    public void SetSpeed(float speed, bool overrideSpeed = false)
    {
        if (!speedOverride || overrideSpeed)
        {
            NavAgent.speed = speed;
        }
    }
    protected void FireProjectile(Transform spawnPoint, Transform target, bool seeking = false)
    {
        GameObject projObj = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        proj.Parent = gameObject;
        proj.Damage = ProjectileDamage;

        if (seeking)
        {
            proj.Seeking = true;
            proj.Target = target;
        }
        else
        {
            proj.Seeking = false;
            Vector3 direction = (target.position - spawnPoint.position).normalized;
            proj.Direction = direction;
        }
    }

    public void ApplyPushback(Vector3 direction, float force, float duration)
    {
        if (!AllowPushback)
            return;

        pushVelocity += direction.normalized * force;
        pushTimer = Mathf.Max(pushTimer, duration);

        if (NavAgent != null)
        {
            NavAgent.isStopped = true;      // stop pathing
            NavAgent.updateRotation = false;
        }
    }
    void HandlePushback()
    {
        if (pushTimer <= 0f)
            return;

        // Manual movement
        NavAgent.Move(pushVelocity * Time.deltaTime);

        pushTimer -= Time.deltaTime;

        if (pushTimer <= 0f)
        {
            pushVelocity = Vector3.zero;

            NavAgent.isStopped = false;
            NavAgent.updateRotation = true;

            // Optional but recommended
            NavAgent.ResetPath();
        }
    }

    public void Stun(float duration)
    {
        if (NavAgent == null)
            return;

        StartCoroutine(StunRoutine(duration));
    }
    private IEnumerator StunRoutine(float duration)
    {
        Stunned = true;

        if (_animator != null)
        {
            _animator.SetBool("Stunned", true);

            yield return null;
            _animator.SetBool("AnyStateLock", true);
        }

        yield return new WaitForSeconds(duration);

        Stunned = false;
        
        if (_animator != null)
        {
            _animator.SetBool("Stunned", false);
            _animator.SetBool("AnyStateLock", false);
        }
    }

    /// <summary>
    /// Triggers a one-time burst VFX that emits gradually over burstDuration.
    /// </summary>
    // Returns the VFX prefab, loading it if necessary

    public void SpawnEnergy(float middlePosDistance = 4f)
    {
        player.GiveEnergy();

        ParticleSpawner.SpawnEnergy(Center, true, middlePosDistance);
    }
}

[System.Serializable]
public class VisionCone
{
    public Vector3 offset;
    public Vector3 rotation;
    public float angle;
    public float length;

    public VisionCone(Vector3 offset, Vector3 rotation, float angle, float length)
    {
        this.offset = offset;
        this.rotation = rotation;
        this.angle = angle;
        this.length = length;
    }

    public Quaternion GetRotation() => Quaternion.Euler(rotation);
}

[System.Serializable]
public class EnemyStats
{
    [SerializeField] public LayerMask layers;

    [SerializeField] public int health = 5;
    [SerializeField] public int maxHealth = 5;

    [SerializeField] public float alertRadius = 5f;
    [SerializeField] public float chaseAlertRadius = 8f;
    [SerializeField] public float visionLength = 8f;
    [SerializeField] public float chaseVisionLength = 16f;
    [SerializeField] public float visionAngle = 120f;
    [SerializeField] public float marginDegrees = 4f;
    [SerializeField] public Vector3 visionRotation = Vector3.zero;

    [SerializeField] public float wanderSpeed = 3f;
    [SerializeField] public float chaseSpeed = 6f;
    [SerializeField] public float wanderRadius = 25f;
    [SerializeField] public float waitTime = 1f;
    [SerializeField] public float rotationSpeed = 75f;

    [SerializeField] public float actionInterval = 2f;
}
