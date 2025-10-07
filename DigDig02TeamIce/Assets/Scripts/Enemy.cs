using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public abstract class Enemy : Entity
{
    public int Health { get; set; } = 1;

    public float AlertRadius { get; set; } = 5;
    public bool DetectedPlayer { get; set; } = false;

    public List<VisionCone> VisionCones = new();

    public int ProjectileDamage = 1;
    public GameObject projectilePrefab;

    [Header("VFX Settings")]
    [SerializeField] private string vfxResourcePath = "EnergyEffect";
    private static GameObject defaultVFXAsset;

    public bool IsTargetInVision(Collider target)
    {
        Vector3 enemyPos = transform.position;
        Vector3 forward = transform.forward;

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

    public virtual void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
            Die();
    }
    protected virtual void Die()
    {
        // Default death logic
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
    public void SpawnVFX()
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
        Vector3 middlePos = enemyPos + direction * 4f;
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
