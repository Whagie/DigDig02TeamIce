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

        if (DetectedPlayer && CanAttack)
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
        PauseIntervalTimer = true;
        chargeUpVFX = Object.Instantiate(Particles.P_EvilBallCharge, Center.position, CrystalBall.transform.rotation);
        SoundFXManager.instance.PlaySoundFXClip(FX.FX_baneful_ball_charge_up, transform, 1f);

        yield return new WaitForSeconds(1f);

        chargeUpVFX.transform.rotation = CrystalBall.transform.rotation;
        FireProjectile(Center, player.Center.transform);
        PauseIntervalTimer = false;
        SoundFXManager.instance.PlaySoundFXClip(FX.FX_baneful_ball_shoot, transform, 0.9f, 1.1f, 1f);
    }

    public override void OnHit(IHitbox source)
    {
        base.OnHit(source);

        SoundFXManager.instance.PlaySoundFXClip(FX.FX_crystal_hit, transform, 0.85f, 1.15f, 0.75f);

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
        if (Dead) // If already dead (from save data), skip explosion animation
        {
            base.Die();
            StopAllCoroutines();
            Destroy(gameObject);
            return;
        }

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
                fractObj = Instantiate(fracturedObject, Center.position, transform.rotation) as GameObject;

                foreach (Transform t in fractObj.transform)
                {
                    var rb = t.GetComponent<Rigidbody>();

                    if (rb != null)
                        rb.AddExplosionForce(UnityEngine.Random.Range(epxlosionMinForce, explosionMaxForce), transform.position, explosionForceRadius);

                    StartCoroutine(Shrink(rb, ShrinkDelay));
                }

                Destroy(fractObj, 5);
            }
        }
    }

    private IEnumerator Shrink(Rigidbody rb, float delay)
    {
        Transform obj = rb.transform;
        yield return new WaitForSeconds(delay);

        Vector3 prevScale = obj.localScale;
        float time = 0f;

        while (time < ShrinkDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / ShrinkDuration);

            obj.localScale = Vector3.Lerp(prevScale, Vector3.zero, t);

            rb.AddForce(Vector3.down * 0.5f, ForceMode.Acceleration);

            yield return null;
        }

        obj.localScale = Vector3.zero;
    }
}
