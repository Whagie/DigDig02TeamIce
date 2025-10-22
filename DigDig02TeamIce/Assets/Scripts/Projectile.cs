using Game.Core;
using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Projectile : Entity, IHitbox
{
    public GameObject Owner => gameObject;
    public bool CanBeParried => true;
    public Collider Collider => GetComponent<Collider>();
    public bool UseMeshCollision { get; set; } = false;

    [SerializeField] private LayerMask layers;
    public LayerMask LayerMask => layers;

    public GameObject Parent { get; set; }
    public int Damage { get; set; } = 1;
    public Transform Target { get; set; }
    public float Speed { get; set; } = 8f;
    public float Lifespan { get; set; } = 10f;
    public bool Seeking { get; set; } = false;

    public bool Rebound { get; private set; }
    public Vector3 Direction { get; set; }

    private bool recentlyParried;
    private Vector3 prevPos;

    protected override void OnEntityEnable()
    {
        HitboxManager.Register(this);
        base.OnEntityEnable();
    }
    protected override void OnEntityDisable()
    {
        HitboxManager.Unregister(this);
        base.OnEntityDisable();
    }

    protected override void OnStart()
    {
        prevPos = transform.position;
        StartCoroutine(LifespanTimer());
    }

    protected override void OnUpdate()
    {
        Vector3 currentPos = transform.position;

        if (Seeking && Target)
            currentPos = Vector3.MoveTowards(currentPos, Target.position, Speed * Time.deltaTime);
        else
            currentPos += Speed * Time.deltaTime * Direction;

        transform.position = currentPos;
        prevPos = currentPos;
    }

    public void OnParried(IHurtbox by)
    {
        if (!Rebound)
            Reflect(-Direction);
    }

    public void OnHit(IHurtbox target)
    {
        if (recentlyParried) return;

        if (target.Owner.layer == LayerMask.NameToLayer("Player") && !Rebound)
        {
            target.OnHit(this);
            Destroy(gameObject);
        }
        else if (target.Owner.layer == LayerMask.NameToLayer("Enemy") && Rebound)
        {
            target.OnHit(this);
            Destroy(gameObject);
        }
    }

    public void Reflect(Vector3 newDir)
    {
        Direction = newDir.normalized;
        Speed *= 2f;
        Rebound = true;
        recentlyParried = true;
        StartCoroutine(ClearParryFlag());
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Collider.bounds.center, Collider.bounds.size);
    }

    private IEnumerator ClearParryFlag()
    {
        yield return new WaitForFixedUpdate();
        recentlyParried = false;
    }

    private IEnumerator LifespanTimer()
    {
        yield return new WaitForSeconds(Lifespan);
        Destroy(gameObject);
    }
}
