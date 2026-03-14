using Game.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class SpearAttackScript : MeleeAttack
{
    private Companion companion;

    private Transform target;
    private Vector3 targetPos;
    private Quaternion alignedRotation;
    private bool hasStartedAttack;

    public float AttackSpeed = 60f;

    [SerializeField] private LayerMask layers;

    private VisualEffect vfx;
    private bool hit = false;
    private bool triggered = false;

    private float ageOverLifetime = 0f;
    private float lifetimeAmount = 8f;
    private float elapsedLifetime = 0f;
    private float delayBeforeAttack = 0.75f;
    private float playRate = 3f;

    public enum SpearSpawnState
    {
        Left,
        Right,
        Top
    }
    public SpearSpawnState State { get; set; }

    void Start()
    {
        companion = GameObject.FindObjectOfType<Companion>();
        companion.previousSpears.Add(this);
        PlayerAttack = true;
        DestroyOnHit = false;
        hitCollider = GetComponent<Collider>();
        LayerMask = layers;
        Deactivate();

        Damage = 2;

        vfx = GetComponent<VisualEffect>();
        vfx.SetFloat("Lifetime", lifetimeAmount);
        vfx.playRate = playRate;

        target = null;
        targetPos = Vector3.zero;

        if (companion.player.currentTarget != null)
        {
            if (!companion.player.currentTarget.Dead)
            {
                target = companion.player.currentTarget.Center;
                targetPos = target.position;
            }
        }

        Vector3 direction = targetPos - transform.position;
        alignedRotation = Quaternion.LookRotation(direction);
        transform.rotation = alignedRotation;

        //SpawnEnergy();
    }

    void Update()
    {
        elapsedLifetime += Time.deltaTime;
        ageOverLifetime = Mathf.InverseLerp(0, lifetimeAmount, elapsedLifetime);

        if (ageOverLifetime >= 1f)
        {
            StartCoroutine(LifespanTimer(0.1f));
        }

        if (companion.player.currentTarget != null)
        {
            target = companion.player.currentTarget.Center;
            targetPos = target.position;
        }
        else
        {
            Enemy closest = companion.player.FindClosestEnemy();
            if (closest != null)
            {
                if (!closest.Dead)
                {
                    target = closest.Center;
                    targetPos = target.position;
                }
            }
        }

        if (!hasStartedAttack)
        {
            hasStartedAttack = true;
            StartCoroutine(RotateBeforeAttack());
        }

        if (hit)
        {
            vfx.SetBool("Hit", true);
            if (!triggered)
            {
                vfx.SetBool("Triggered", true);
                triggered = true;
            }
        }
    }
    public override void OnHit(IHurtbox target)
    {
        companion.previousSpears.Remove(this);
        vfx.SetFloat("LifetimeAtHit", ageOverLifetime);
        ParticleSpawner.Spawn(Particles.P_SpearExplosion, target.Collider.bounds.center);
        if (target.Owner.layer == LayerMask.NameToLayer("Enemy"))
        {
            target.OnHit(this);
            hit = true;
            Deactivate();
            StartCoroutine(ContinueMovement());
            StopCoroutine(Attack());
            StartCoroutine(LifespanTimer(3f));

            CameraActions.Main.Punch(-0.15f, 0.07f);
            Freezer.Freeze(0.025f);
        }
        else if (target.Owner.layer != LayerMask.NameToLayer("Enemy"))
        {
            hit = true;
            StartCoroutine(LifespanTimer(3f));
        }
    }

    private IEnumerator Attack()
    {
        Activate();
        Vector3 moveDir = transform.forward;

        while (!hit)
        {
            transform.position += AttackSpeed * Time.deltaTime * moveDir;
            yield return null;
        }
    }

    private IEnumerator ContinueMovement()
    {
        Vector3 moveDir = transform.forward;
        float timer = 0.1f;

        while (timer > 0f)
        {
            transform.position += AttackSpeed * Time.deltaTime * moveDir;
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RotateBeforeAttack()
    {
        float elapsed = 0f;
        float duration = delayBeforeAttack;
        Quaternion startRot = transform.rotation;
        Vector3 startPos = transform.position;
        float pullbackDistance = 1.25f;
        float pullbackStartTime = duration * 0.65f; // start after 1/3 of total duration

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (target != null)
            {
                // Rotate smoothly toward target
                targetPos = target.position;
                alignedRotation = Quaternion.LookRotation(targetPos - transform.position);
                transform.rotation = Quaternion.Slerp(startRot, alignedRotation, smoothT);
            }

            // Pullback only after one-third of the duration
            float pullbackT = 0f;
            if (elapsed > pullbackStartTime)
            {
                // Normalize pullback progress (0 -> 1) over remaining time
                pullbackT = (elapsed - pullbackStartTime) / (duration - pullbackStartTime);
                pullbackT = Mathf.SmoothStep(0f, 1f, pullbackT);
            }

            // Apply pullback based on its own progress
            Vector3 pullback = -transform.forward * pullbackDistance * pullbackT;
            transform.position = startPos + pullback;

            yield return null;
        }

        transform.rotation = alignedRotation;
        StartCoroutine(Attack());
    }

    private IEnumerator LifespanTimer(float time)
    {
        yield return new WaitForSeconds(time);
        Deactivate();
        Destroy(gameObject);
    }
}
