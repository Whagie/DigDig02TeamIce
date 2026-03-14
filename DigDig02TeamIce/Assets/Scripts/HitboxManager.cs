using Game.Core;
using System.Collections.Generic;
using UnityEngine;

public static class HitboxManager
{
    private static readonly List<IHitbox> activeHitboxes = new();
    private static readonly List<IHurtbox> activeHurtboxes = new();

    public static void Register(IHitbox hitbox)
    {
        if (hitbox != null && !activeHitboxes.Contains(hitbox))
            activeHitboxes.Add(hitbox);
    }

    public static void Unregister(IHitbox hitbox) => activeHitboxes.Remove(hitbox);

    public static void Register(IHurtbox hurtbox)
    {
        if (hurtbox != null && !activeHurtboxes.Contains(hurtbox))
            activeHurtboxes.Add(hurtbox);
    }

    public static void Unregister(IHurtbox hurtbox) => activeHurtboxes.Remove(hurtbox);

    public static void Update()
    {
        var hitboxes = activeHitboxes.ToArray();
        var hurtboxes = activeHurtboxes.ToArray();

        foreach (var hb in hitboxes)
        {
            if (hb == null || hb.Collider == null || !hb.Collider.enabled)
                continue;

            foreach (var hurt in hurtboxes)
            {
                if (hurt == null || hurt.Collider == null || !hurt.Collider.enabled)
                    continue;

                if (hurt.Owner == hb.Owner)
                    continue;

                if ((hb.LayerMask & (1 << hurt.Owner.layer)) == 0)
                    continue;

                if (!hb.Collider.bounds.Intersects(hurt.Collider.bounds))
                    continue;

                if (!CheckColliderOverlap(hb.Collider, hurt.Collider))
                    continue;

                hb.OnHit(hurt);
            }
        }
    }

    private static bool CheckColliderOverlap(Collider a, Collider b)
    {
        Vector3 direction;
        float distance;

        return Physics.ComputePenetration(
            a, a.transform.position, a.transform.rotation,
            b, b.transform.position, b.transform.rotation,
            out direction, out distance
        );
    }
}