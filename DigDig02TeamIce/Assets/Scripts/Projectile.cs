using Game.Core;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour, IHitbox
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

    private void OnEnable()
    {
        HitboxManager.Register(this);
    }
    private void OnDisable()
    {
        HitboxManager.Unregister(this);
    }

    private void Start()
    {
        prevPos = transform.position;
        StartCoroutine(LifespanTimer());
    }

    private void Update()
    {
        Vector3 currentPos = transform.position;
        Quaternion rotation = Quaternion.LookRotation(Direction);

        if (Seeking && Target)
            currentPos = Vector3.MoveTowards(currentPos, Target.position, Speed * Time.deltaTime);
        else
            currentPos += Speed * Time.deltaTime * Direction;

        transform.position = currentPos;
        transform.rotation = rotation;
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
