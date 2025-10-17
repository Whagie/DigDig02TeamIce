using Game.Core;
using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class SpearAttackScript : MeleeAttack
{
    private Companion companion;

    private Transform target;
    private Vector3 targetPos;
    private Vector3 targetOffset;
    private float colliderHeight;
    private Quaternion alignedRotation;
    private bool hasStartedAttack;

    public float AttackSpeed = 30f;

    [SerializeField] private LayerMask layers;

    private VisualEffect vfx;
    private bool hit = false;
    private bool triggered = false;

    private float ageOverLifetime = 0f;
    private float lifetimeAmount = 8f;
    private float elapsedLifetime = 0f;
    private float delayBeforeAttack = 2f;
    private float playRate = 1.5f;

    [SerializeField] private string vfxResourcePath = "EnergyEffect";
    private static GameObject ribbonEffect;

    public enum SpearSpawnState
    {
        Left,
        Right,
        Top
    }
    public SpearSpawnState State { get; set; }

    void Start()
    {
        companion = TrackerHost.Current.Get<Companion>();
        companion.previousSpears.Add(this);
        PlayerAttack = true;
        DestroyOnHit = false;
        hitCollider = GetComponent<Collider>();
        LayerMask = layers;
        Activate();

        vfx = GetComponent<VisualEffect>();
        vfx.SetFloat("Lifetime", lifetimeAmount);
        vfx.playRate = playRate;

        if (Player.currentTarget != null)
        {
            target = Player.currentTarget.transform;
            colliderHeight = Player.currentTarget.GetComponent<Collider>().bounds.extents.y;
            targetOffset = new Vector3(0f, colliderHeight, 0f);
            targetPos = target.position + targetOffset;
        }
        else
        {
            target = null;
            targetPos = Vector3.zero;
            colliderHeight = 0f;
            targetOffset = new Vector3(0f, colliderHeight, 0f);
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
        if (target.Owner.layer == LayerMask.NameToLayer("Enemy"))
        {
            target.OnHit(this);
            hit = true;
            Deactivate();
            StartCoroutine(LifespanTimer(3f));
        }
        else if (target.Owner.layer != LayerMask.NameToLayer("Enemy"))
        {
            hit = true;
            StartCoroutine(LifespanTimer(3f));
        }
    }

    private IEnumerator Attack()
    {
        Vector3 moveDir = transform.forward;

        while (!hit)
        {
            transform.position += AttackSpeed * Time.deltaTime * moveDir;
            yield return null;
        }
    }

    private IEnumerator RotateBeforeAttack()
    {
        if (target == null)
            yield break;

        float elapsed = 0f;
        float duration = delayBeforeAttack;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            targetPos = target.position + targetOffset;
            alignedRotation = Quaternion.LookRotation(targetPos - transform.position);

            transform.rotation = Quaternion.Slerp(startRot, alignedRotation, t);
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

    public void SpawnEnergy(float middlePosDistance = 4f)
    {
        if (ribbonEffect == null)
        {
            ribbonEffect = GetVFXPrefab(vfxResourcePath);
            if (ribbonEffect == null) return;
        }

        GameObject prefab = ribbonEffect;

        var instance = Instantiate(prefab, transform);
        EnergyParticleManager particleManager = instance.GetComponent<EnergyParticleManager>();
        if (companion == null)
        {
            Debug.Log("Companion is null!");
        }

        Vector3 spearPos = transform.position;
        Vector3 constructPos = companion.transform.position;

        Vector3 middlePosOffset = companion.transform.rotation * new Vector3(-3f, 2f, 4f);
        Vector3 middlePos = Vector3.Lerp(spearPos, constructPos, 0.5f) + middlePosOffset;

        GameObject empty = new GameObject("EnergyCurveMidpoint");
        empty.transform.position = middlePos;

        particleManager.StartPos = companion.transform;
        particleManager.EndPos = transform;
        particleManager.MiddlePos = empty.transform;
        particleManager.vfx.playRate = 0.5f;

        // Optional: destroy the VFX prefab after its lifetime
        float maxLifetime = 3; // match your particle lifetime
        Destroy(empty, maxLifetime);
        Destroy(instance, maxLifetime);
    }

    public GameObject GetVFXPrefab(string resourcePath)
    {
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
            Debug.LogWarning($"Failed to load VFX prefab at Resources/{resourcePath}");

        return prefab;
    }
}
