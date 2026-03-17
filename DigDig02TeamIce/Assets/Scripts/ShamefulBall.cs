using Game.Core;
using System.Collections;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class ShamefulBall : Enemy
{
    public Collider MainCollider;

    public GameObject CrystalBall;

    public GameObject originalObject;
    public GameObject fracturedObject;
    public float epxlosionMinForce = 5;
    public float explosionMaxForce = 100;
    public float explosionForceRadius = 10;
    public float ShrinkDuration = 1;
    public float ShrinkDelay = 1;
    private GameObject fractObj;
    private GameObject chargeUpVFX;

    private bool shouldRotate = true;

    private bool firstDetected = true;


    [SerializeField] private float suckInterval = 2.5f;

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
            if (firstDetected)
            {
                StartCoroutine(ChargeAndSuck(0.5f));
                firstDetected = false;
            }

            if (shouldRotate)
            {
                RotateTowardsY(CrystalBall.transform, player.transform.position, RotationSpeed);
            }

            if (player.Energy <= 0)
                return;

            OnInterval(suckInterval, () =>
            {
                StartCoroutine(ChargeAndSuck());
            });
        }
    }

    private IEnumerator ChargeAndSuck(float extraWait = 0f)
    {
        PauseIntervalTimer = true;

        yield return new WaitForSeconds(extraWait);

        chargeUpVFX = Object.Instantiate(Particles.P_ShamefulBallCharge, Center.position, CrystalBall.transform.rotation);

        yield return new WaitForSeconds(1.2f);

        chargeUpVFX.transform.rotation = CrystalBall.transform.rotation;

        SuckPlayerEnergy();

        if (MainCollider is CapsuleCollider col)
        {
            Vector3 dir = player.Companion.transform.position - Center.position;
            dir.Normalize();
            dir.y = 0f;

            Quaternion rotation = Quaternion.LookRotation(dir);

            var instance = Object.Instantiate(VFX.Construct_GainEnergy, Center.position + (dir * col.radius), rotation, transform);
            Destroy(instance, 1.5f);
        }

        PauseIntervalTimer = false;
    }

    private void SuckPlayerEnergy()
    {
        player.ConsumeEnergy(player.Energy);

        ParticleSpawner.SuckEnergy(Center, true);
        ParticleSpawner.SuckEnergy(Center, true);
        ParticleSpawner.SuckEnergy(Center, true);
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
