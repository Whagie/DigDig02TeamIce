using Game.Core;
using System.Collections;
using UnityEngine;

public class EvilCube : Enemy
{
    public Collider MainCollider;

    public GameObject CrystalBall;

    public GameObject originalObject;
    public GameObject fracturedObject;
    public GameObject explosionVFX;
    public float epxlosionMinForce = 5;
    public float explosionMaxForce = 100;
    public float explosionForceRadius = 10;
    public float ShrinkDuration = 1;
    public float ShrinkDelay = 1;
    private GameObject fractObj;
    private GameObject chargeUpVFX;

    private bool shouldRotate = true;


    [SerializeField] private float shootInterval = 2.5f;

    protected override void Awake()
    {
        base.Awake();

        ShouldWander = false;
        ShouldMove = false;
        ProjectileDamage = 2;
    }

    protected override void Start()
    {
        base.Start();

        if (MainCollider != null)
        {
            Collider = MainCollider;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (Dead)
            return;

        if (DetectedPlayer)
        {
            if (shouldRotate)
            {
                RotateTowardsY(CrystalBall.transform, player.transform.position, RotationSpeed);
            }

            OnInterval(shootInterval, () =>
            {
                StartCoroutine(ChargeAndShoot());
            });
        }
    }

    private IEnumerator ChargeAndShoot()
    {
        chargeUpVFX = Object.Instantiate(Particles.P_EvilBallCharge, Center.position, CrystalBall.transform.rotation);

        yield return new WaitForSeconds(1f);

        chargeUpVFX.transform.rotation = CrystalBall.transform.rotation;
        FireProjectile(Center, player.Center.transform);
    }

    public override void OnHit(IHitbox source)
    {
        base.OnHit(source);

        if (Dead)
            return;

        if (source.Owner.CompareTag("Projectile"))
        {
            if (MainCollider is CapsuleCollider col)
            {
                Vector3 dir = Center.position - source.Owner.gameObject.transform.position;
                dir.Normalize();
                dir.y = 0f;

                Object.Instantiate(Particles.P_PinkMagicHit, Center.position + (dir * col.radius), transform.rotation, transform);
            }
        }
    }

    protected override void Die()
    {
        base.Die();

        shouldRotate = false;
        Destroy(chargeUpVFX);
        StopAllCoroutines();
        Explode();
    }

    private void Explode()
    {
        if (originalObject != null)
        {
            originalObject.SetActive(false);

            if (fracturedObject != null)
            {
                fractObj = Instantiate(fracturedObject, transform.position, transform.rotation) as GameObject;

                foreach (Transform t in fractObj.transform)
                {
                    var rb = t.GetComponent<Rigidbody>();

                    if (rb != null)
                        rb.AddExplosionForce(UnityEngine.Random.Range(epxlosionMinForce, explosionMaxForce), transform.position, explosionForceRadius);

                    StartCoroutine(Shrink(t, ShrinkDelay));
                }

                Destroy(fractObj, 5);

                if (explosionVFX != null)
                {
                    GameObject exploVFX = Instantiate(explosionVFX) as GameObject;
                    Destroy(exploVFX, 7);
                }
            }
        }
    }

    private IEnumerator Shrink(Transform obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 prevScale = obj.localScale;
        float time = 0f;

        while (time < ShrinkDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / ShrinkDuration);

            obj.localScale = Vector3.Lerp(prevScale, Vector3.zero, t);

            yield return null;
        }

        obj.localScale = Vector3.zero;
    }
}
