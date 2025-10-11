using Game.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEngine.GraphicsBuffer;

public abstract class Enemy : Entity, IHurtbox
{
    public Collider Collider { get; protected set; }
    public LayerMask LayerMask { get; protected set; }
    public GameObject Owner => gameObject;

    public int Health { get; set; } = 10;
    public float AlertRadius { get; set; } = 5;
    public bool DetectedPlayer { get; set; } = false;
    public bool SeeingPlayer { get; set; } = false;
    public bool FacingPlayer { get; set; } = false;
    public float MarginDegrees { get; set; } = 4f;
    public bool Dead { get; set; } = false;

    public List<HitFlash> ChildrenWithFlashEffect;

    [Serializable]
    public class EnemyAction
    {
        public string TriggerName;
        public float Weight = 1f;
        public Func<bool> CanUse; // optional condition
    }

    public EnemyAction[] Actions;
    public float ActionInterval = 3f;

    public Animator _animator;
    private float _timer;


    public List<VisionCone> VisionCones = new();

    public int ProjectileDamage = 1;
    public GameObject projectilePrefab;

    [SerializeField] private string vfxResourcePath = "EnergyEffect";
    private static GameObject defaultVFXAsset;

    private Color sphereColor = Color.blue;
    private Color visionConeColor = Color.blue;


    protected override void OnAwake()
    {
        _animator = GetComponent<Animator>();
        if (_animator != null)
        {
            InitializeActions();
        }
    }

    protected override void OnUpdate()
    {
        if (Dead)
            return;

        Player player = TrackerHost.Current.Get<Player>();
        int playerMask = LayerMask.GetMask("Player");

        // --- Check alert radius
        Collider[] hits = Physics.OverlapSphere(transform.position, AlertRadius, playerMask);
        bool playerInAlertRadius = false;

        foreach (var c in hits)
        {
            if (c.GetComponent<Player>() != null)
            {
                playerInAlertRadius = true;
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

        _timer += Time.deltaTime;
        if (_timer >= ActionInterval)
        {
            _timer = 0f;
            PickAction();
        }

        // --- Draw debug
        foreach (var cone in VisionCones)
            DrawMethods.DrawVisionCone(transform, cone, visionConeColor);

        DrawMethods.WireSphere(transform.position, AlertRadius, sphereColor);
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
                    if (!Physics.Raycast(coneOrigin, toPoint.normalized, toPoint.magnitude, LayerMask.GetMask("Obstacles")))
                        return true;
                }
            }
        }

        return false;
    }

    public void OnHit(IHitbox source)
    {
        TakeDamage(source.Damage);
        if (source.Owner.CompareTag("Projectile"))
        {
            SpawnVFX();
        }

        foreach (var child in ChildrenWithFlashEffect)
        {
            child.Flash();
        }
    }

    public virtual void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
            Die();
    }
    protected virtual void Die()
    {
        Dead = true;
    }

    public virtual void HandleParried(IHurtbox by)
    {
        Collider collider = GetComponent<Collider>();
        if (collider is CapsuleCollider capsule)
        {
            SpawnVFX(4f + capsule.radius);
        }
        else
        {
            SpawnVFX();
        }

        foreach (var child in ChildrenWithFlashEffect)
        {
            child.Flash();
        }
    }

    protected virtual void InitializeActions() { }

    void PickAction()
    {
        float totalWeight = 0f;

        foreach (var action in Actions)
        {
            if (action.CanUse == null || action.CanUse())
                totalWeight += action.Weight;
        }

        if (totalWeight <= 0f)
            return;

        float choice = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var action in Actions)
        {
            if (action.CanUse == null || action.CanUse())
            {
                cumulative += action.Weight;
                if (choice <= cumulative)
                {
                    _animator.SetTrigger(action.TriggerName);
                    break;
                }
            }
        }
    }
    protected void FireProjectile(Transform target, bool seeking = false)
    {
        GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
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
            Vector3 direction = (target.position - transform.position).normalized;
            proj.Direction = direction;
        }
    }

    /// <summary>
    /// Triggers a one-time burst VFX that emits gradually over burstDuration.
    /// </summary>
    // Returns the VFX prefab, loading it if necessary
    private GameObject GetVFXPrefab()
    {
        if (defaultVFXAsset == null)
        {
            defaultVFXAsset = Resources.Load<GameObject>(vfxResourcePath);
            if (defaultVFXAsset == null)
                Debug.LogWarning($"Failed to load VFX prefab at Resources/{vfxResourcePath}");
        }
        return defaultVFXAsset;
    }

    // Call this to spawn the VFX
    public void SpawnVFX(float middlePosDistance = 4f)
    {
        var prefab = GetVFXPrefab();
        if (prefab == null) return;

        var instance = Instantiate(prefab, transform);
        EnergyParticleManager particleManager = instance.GetComponent<EnergyParticleManager>();
        Companion companion = TrackerHost.Current.Get<Companion>();
        if (companion == null)
        {
            Debug.Log("Companion is null!");
        }

        Vector3 enemyPos = transform.position;
        Vector3 playerPos = companion.player.transform.position;

        // Direction *away* from the companion (so the curve bends back)
        Vector3 direction = (enemyPos - playerPos).normalized;

        // Midpoint halfway in Y between the two
        float midY = enemyPos.y + (playerPos.y - enemyPos.y) / 2f;

        // Final middle position = enemy position + offset backward along the direction
        Vector3 middlePos = enemyPos + direction * middlePosDistance;
        middlePos.y = midY;

        GameObject empty = new GameObject("EnergyCurveMidpoint");
        empty.transform.position = middlePos;

        particleManager.StartPos = transform;
        particleManager.EndPos = companion.transform;
        particleManager.MiddlePos = empty.transform;

        // Optional: destroy the VFX prefab after its lifetime
        float maxLifetime = 3; // match your particle lifetime
        Destroy(empty, maxLifetime);
        Destroy(instance, maxLifetime);
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
