using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : Entity
{
    public int Damage { get; set; } = 1;
    public Transform Target { get; set; }
    public Vector3 Direction { get; set; }
    public float Speed { get; set; } = 5f;
    public float Lifespan { get; set; } = 10f;
    public bool Seeking { get; set; } = false;

    protected override void OnStart()
    {
        StartCoroutine(LifespanTimer());
    }
    protected override void OnUpdate()
    {
        if (Seeking)
        {
            if (Target != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, Target.position, Speed * Time.deltaTime);
            }
        }
        else
        {
            if (Direction != null)
            {
                transform.position += Direction * Speed * Time.deltaTime;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Player player = TrackerHost.Current.Get<Player>();
            player.TakeDamage(Damage);
            Destroy(gameObject);
        }
    }

    private IEnumerator LifespanTimer()
    {
        while (Lifespan > 0f)
        {
            Lifespan -= Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
        yield return null;
    }
}
