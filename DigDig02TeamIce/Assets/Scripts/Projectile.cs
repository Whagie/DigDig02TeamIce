using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : Entity
{
    public GameObject Parent { get; set; }
    public int Damage { get; set; } = 1;
    public Transform Target { get; set; }
    public Vector3 Direction { get; set; }
    public float Speed { get; set; } = 5f;
    public float Lifespan { get; set; } = 10f;
    public bool Seeking { get; set; } = false;
    public bool Rebound { get; set; } = false;

    private bool recentlyParried = false;

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
                transform.position += Speed * Time.deltaTime * Direction;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }
    private void OnTriggerStay(Collider other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Collider other)
    {
        if (!Rebound && !recentlyParried && other.gameObject.layer == LayerMask.NameToLayer("PlayerDamage"))
        {
            Player player = TrackerHost.Current.Get<Player>();
            player.TakeDamage(Damage);
            Destroy(gameObject);
        }
        if (Rebound && other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("Rebound and hit enemy!");
            Enemy enemy = other.gameObject.GetComponent<Enemy>();
            enemy.SpawnVFX();
            Destroy(gameObject);
        }
    }

    public void Reflect(Vector3 newDirection)
    {
        Direction = newDirection;
        Speed *= 2f;
        Rebound = true;
        recentlyParried = true;
        StartCoroutine(ClearParryFlag());
    }

    private IEnumerator ClearParryFlag()
    {
        yield return new WaitForFixedUpdate(); // wait one physics frame
        recentlyParried = false;
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
