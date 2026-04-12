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
    public float Speed = 8f;
    public float Lifespan { get; set; } = 10f;
    public bool Seeking { get; set; } = false;
    public float InvisibleStartTime = 0.4f;

    public GameObject BlobShadow;
    private BlobShadowMesh blobShadow;

    public bool Rebound { get; private set; }
    public bool ShouldRebound = true;
    public Vector3 Direction { get; set; }

    private bool recentlyParried;
    private Vector3 prevPos;

    private bool isInvisible = true;

    private Enemy enemyOwner;

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
        var instance = Object.Instantiate(BlobShadow, transform.position, Quaternion.identity);
        blobShadow = instance.GetComponent<BlobShadowMesh>();
        blobShadow.target = transform;
        blobShadow.groundMask = LayerMask.GetMask("Default", "Water", "Pushable", "NoAO");
        blobShadow.raycastHeight = 0.1f;
        blobShadow.maxDrop = 12f;
        blobShadow.positionSmoothing = 15f;
        blobShadow.vertexSmoothing = 20f;
        blobShadow.maxAirHeight = 16f;

        enemyOwner = Parent.gameObject.GetComponent<Enemy>();

        prevPos = transform.position;
        StartCoroutine(LifespanTimer());
        StartCoroutine(InvisibleTime(InvisibleStartTime));
    }

    private void Update()
    {
        if (enemyOwner.Dead)
        {
            Destroy(blobShadow.gameObject);
            Destroy(gameObject);
            return;
        }

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

        if (!ShouldRebound)
        {
            Player player = GameObject.FindObjectOfType<Player>();
            player.GiveEnergy();
            ParticleSpawner.SpawnEnergy(transform, true, 4f, true);
            ParticleSpawner.Spawn(Particles.P_PinkMagicHit, transform.position);
            Destroy(blobShadow.gameObject);
            Collider.enabled = false;
            this.enabled = false;
            Destroy(gameObject, 0.01f);
        }
    }

    public void OnHit(IHurtbox target)
    {
        if (recentlyParried || isInvisible) return;

        if (target.Owner.layer == LayerMask.NameToLayer("Player") && !Rebound)
        {
            target.OnHit(this);
            ParticleSpawner.Spawn(Particles.P_PinkMagicHit, transform.position);
            Destroy(blobShadow.gameObject);
            Destroy(gameObject);
        }
        else if (target.Owner.layer == LayerMask.NameToLayer("Enemy"))
        {
            if (Rebound)
            {
                target.OnHit(this);
                Destroy(blobShadow.gameObject);
                Destroy(gameObject);
            }
        }
        else
        {
            ParticleSpawner.Spawn(Particles.P_PinkMagicHit, transform.position);
            Destroy(blobShadow.gameObject);
            Destroy(gameObject);
        }
    }

    public void Reflect(Vector3 newDir)
    {
        if (ShouldRebound)
        {
            Direction = newDir.normalized;
            Speed *= 2f;
            Rebound = true;
        }
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
        Destroy(blobShadow.gameObject);
        Destroy(gameObject);
    }

    private IEnumerator InvisibleTime(float duration)
    {
        isInvisible = true;

        yield return new WaitForSeconds(duration);

        isInvisible = false;
    }
}
