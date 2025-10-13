using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvilCube : Enemy, IHurtbox
{
    public Collider MainCollider;

    public float alertRadius = 4f;
    public float visionLength = 5f;
    public float visionAngle = 90f;
    public Vector3 visionRotation = Vector3.zero;

    protected override void OnAwake()
    {
        Health = 10;
        AlertRadius = alertRadius;
        ProjectileDamage = 2;

        VisionCones.Add(new VisionCone(Vector3.zero, Vector3.zero, visionAngle, visionLength));
    }

    protected override void OnStart()
    {
        if (MainCollider != null)
        {
            Collider = MainCollider;
        }
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        VisionCones[0].angle = visionAngle;
        VisionCones[0].length = visionLength;
        VisionCones[0].rotation = visionRotation;

        if (DetectedPlayer)
        {
            RotateTowardsY(transform, player.transform.position, 90f);

            OnInterval(2f, () =>
            {
                FireProjectile(player.transform);
            });
        }
    }
}
