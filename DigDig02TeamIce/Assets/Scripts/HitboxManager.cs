using Game.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public static class HitboxManager
{
    private static readonly List<IHitbox> activeHitboxes = new();
    private static readonly List<IHurtbox> activeHurtboxes = new();
    private static readonly Collider[] overlapBuffer = new Collider[32];

    public static void Register(IHitbox hitbox)
    {
        if (!activeHitboxes.Contains(hitbox))
            activeHitboxes.Add(hitbox);
    }

    public static void Unregister(IHitbox hitbox) => activeHitboxes.Remove(hitbox);
    public static void Register(IHurtbox hurtbox)
    {
        if (!activeHurtboxes.Contains(hurtbox))
            activeHurtboxes.Add(hurtbox);
    }

    public static void Unregister(IHurtbox hurtbox) => activeHurtboxes.Remove(hurtbox);

    public static void Update()
    {
        var hitboxCopy = activeHitboxes.ToList();
        var hurtboxCopy = activeHurtboxes.ToList();

        foreach (var hb in hitboxCopy)
        {
            if (hb == null || hb.Collider == null) continue;

            foreach (var hurt in hurtboxCopy)
            {
                if (hurt == null || hb.Collider == null) continue;

                if (hurt.Owner == hb.Owner) continue; // skip self

                // check if this hurtbox is a valid target for the hitbox
                if ((hb.LayerMask & (1 << hurt.Owner.layer)) == 0) continue;

                if (hb.UseMeshCollision || hurt.UseMeshCollision)
                {
                    // Check collider overlap
                    if (!CheckMeshColliderOverlap(hb.Collider, hurt.Collider))
                        continue;
                }
                else
                {
                    // Check collider overlap
                    if (!CheckOverlap(hb.Collider, hurt.Collider))
                        continue;
                }

                // Apply hit
                hb.OnHit(hurt);
            }
        }
    }

    private static bool CheckOverlap(Collider hitbox, Collider hurtbox)
    {
        // Box
        if (hitbox is BoxCollider hbBox)
        {
            Vector3 halfExtents = hbBox.size * 0.5f;
            Vector3 center = hbBox.transform.TransformPoint(hbBox.center);
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer, hbBox.transform.rotation);
            for (int i = 0; i < count; i++)
                if (overlapBuffer[i] == hurtbox) return true;
        }
        // Sphere
        else if (hitbox is SphereCollider hbSphere)
        {
            Vector3 center = hbSphere.transform.TransformPoint(hbSphere.center);
            float radius = hbSphere.radius * Mathf.Max(hbSphere.transform.lossyScale.x, hbSphere.transform.lossyScale.y, hbSphere.transform.lossyScale.z);
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer);
            for (int i = 0; i < count; i++)
                if (overlapBuffer[i] == hurtbox) return true;
        }
        // Capsule
        else if (hitbox is CapsuleCollider hbCap)
        {
            Vector3 dir = hbCap.direction == 0 ? Vector3.right : hbCap.direction == 1 ? Vector3.up : Vector3.forward;
            float radius = hbCap.radius * Mathf.Max(hbCap.transform.lossyScale.x, hbCap.transform.lossyScale.y, hbCap.transform.lossyScale.z);
            float height = hbCap.height * 0.5f * Mathf.Max(hbCap.transform.lossyScale.x, hbCap.transform.lossyScale.y, hbCap.transform.lossyScale.z);
            Vector3 center = hbCap.transform.TransformPoint(hbCap.center);
            Vector3 point0 = center - dir * (height - radius);
            Vector3 point1 = center + dir * (height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, overlapBuffer);
            for (int i = 0; i < count; i++)
                if (overlapBuffer[i] == hurtbox) return true;
        }

        return false;
    }

    private static bool CheckMeshColliderOverlap(Collider hitbox, Collider hurtbox)
    {
        Vector3 direction;
        float distance;
        return Physics.ComputePenetration(
            hitbox, hitbox.transform.position, hitbox.transform.rotation,
            hurtbox, hurtbox.transform.position, hurtbox.transform.rotation,
            out direction, out distance
        );
    }
}
