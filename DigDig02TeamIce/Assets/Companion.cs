using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Companion : Entity
{
    public Transform player;
    public Vector3 Offset;

    protected override void OnEntityEnable()
    {
        Companion existing = TrackerHost.Current.Get<Companion>();
        if (existing != null && existing != this)
        {
            Debug.Log("Companion already exists, cancelling spawn.");
            Destroy(gameObject);
            return;
        }

        base.OnEntityEnable();
    }

    protected override void OnUpdate()
    {
        if (player != null)
        {
            transform.position = player.position + Offset;
        }
    }
}
