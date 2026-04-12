using Game.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GraniteGuard : Enemy
{
    public GameObject Halberd;
    public GameObject CrystalShockwave;
    private MeleeAttack swing;
    private MeleeAttack shockwave;

    public Collider MainCollider;
    public Collider HalberdCollider;
    public Collider CrystalShockwaveCollider;

    public float StabFollowUpChance = 0.4f;

    public GameObject Crystal;
    private Material crystalMaterial;
    public Color origBaseColor;
    public Color origTopColor;

    private Color depletedBaseColor = new Color32(76, 46, 58, 255);
    private Color depletedTopColor = new Color32(153, 99, 123, 255);

    public float StartGlowDuration = 1f;

    public bool Glowing = false;

    public bool MoveCameraOnWake = false;

    private CameraMovement cameraMovement;

    private Coroutine startGlowRoutine;
    private Coroutine stopGlowRoutine;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> footsteps;

    protected override void InitializeActions()
    {
        Actions = new[]
        {
            new EnemyAction
            {
                TriggerName = "Swing",
                Weight = 1f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 6.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(0f)
            },
            new EnemyAction
            {
                TriggerName = "Slam",
                Weight = 0.85f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 6.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(0f)
            },
            new EnemyAction
            {
                TriggerName = "Stab",
                Weight = 0.85f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 6.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(0f)
            },
            new EnemyAction
            {
                TriggerName = "Combo",
                Weight = 0.4f,
                CanUse = () => SeeingPlayer && FacingPlayer && DistanceToPlayer <= 6.5f,
                Modifier = new ActionModifier()
                    .ChangeSpeed(0f)
            }
        };
    }

    protected override void Start()
    {
        cameraMovement = Camera.main.GetComponentInParent<CameraMovement>();

        Renderer renderer1 = Crystal.GetComponent<Renderer>();
        Material[] mats1 = renderer1.materials;
        int matIndex1 = Array.FindIndex(mats1, m => m.name.Contains("RedCrystal"));
        crystalMaterial = mats1[matIndex1];

        origBaseColor = crystalMaterial.GetColor("_BaseColor");
        origTopColor = crystalMaterial.GetColor("_TopColor");

        Collider = MainCollider;

        Halberd.AddComponent<MeleeAttack>();
        CrystalShockwave.AddComponent<MeleeAttack>();

        swing = Halberd.GetComponent<MeleeAttack>();
        swing.hitCollider = HalberdCollider;
        swing.EnemyOwner = this;
        swing.LayerMask = LayerMask.GetMask("Player");

        shockwave = CrystalShockwave.GetComponent<MeleeAttack>();
        shockwave.hitCollider = CrystalShockwaveCollider;
        shockwave.EnemyOwner = this;
        shockwave.LayerMask = LayerMask.GetMask("Player");

        shockwave.Damage = 2;

        HalberdCollider.enabled = false;
        CrystalShockwaveCollider.enabled = false;

        if (IsAwake)
        {
            crystalMaterial.SetColor("_BaseColor", origBaseColor);
            crystalMaterial.SetColor("_TopColor", origTopColor);
            Glowing = true;
        }
        else
        {
            crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
            crystalMaterial.SetColor("_TopColor", depletedTopColor);
            Glowing = false;
        }

        base.Start();
    }

    protected override void Update()
    {
        if (Dead)
            return;

        if (!Glowing || !IsAwake)
        {
            return;
        }

        if (Stunned)
        {
            if (swing.active)
            {
                AlterHalberd();
            }
            if (shockwave.active)
            {
                AlterCrystalShockwave(0);
            }
        }

        float moveValue = Mathf.InverseLerp(0f, ChaseSpeed, NavAgent.velocity.magnitude);
        _animator.SetFloat("Move", moveValue);

        base.Update();
    }

    public override void HandleParried(IHurtbox by)
    {
        base.HandleParried(by);

        AlterHalberd(0);
        AlterCrystalShockwave(0);

        Debug.Log("Parried!");
    }

    protected override void OnStunStart()
    {
        base.OnStunStart();
        AlterHalberd(0);
        AlterCrystalShockwave(0);
        HalberdCollider.enabled = false;
        CrystalShockwaveCollider.enabled = false;
    }
    protected override void OnStunEnd()
    {
        base.OnStunEnd();
    }

    public void AlterHalberd(int activate = 1)
    {
        if (swing == null)
        {
            Debug.LogWarning("HalberdSwing was null!");
            return;
        }

        if (activate == 1)
        {
            swing.Activate();
            swing.gizmoColor = Color.red;

            SoundFXManager.instance.PlaySoundFXClip(FX.FX_swing, transform, 0.75f, 1.05f, 0.85f);
        }
        else
        {
            swing.Deactivate();
            swing.gizmoColor = Color.blue;
        }
    }

    public void AlterCrystalShockwave(int activate = 1)
    {
        if (shockwave == null)
        {
            Debug.LogWarning("HalberdSwing was null!");
            return;
        }

        if (activate == 1)
        {
            shockwave.Activate();
            shockwave.gizmoColor = Color.red;
        }
        else
        {
            shockwave.Deactivate();
            shockwave.gizmoColor = Color.blue;
        }
    }

    public void TryFollowUp()
    {
        if (UnityEngine.Random.value < StabFollowUpChance)
            _animator.SetBool("FollowUp", true);
        else
            _animator.SetBool("FollowUp", false);
    }
    public void ResetFollowUp()
    {
        _animator.SetBool("FollowUp", false);
    }

    public void SetHalberdDamage(int newDamage)
    {
        swing.Damage = newDamage;
    }
    public void SetShockwaveDamage(int newDamage)
    {
        shockwave.Damage = newDamage;
    }

    public void ResetDamage()
    {
        swing.Damage = 1;
        shockwave.Damage = 2;
    }

    public void LungeDistanceDuration(string parameters)
    {
        var parts = parameters.Split(';').Select(float.Parse).ToArray();
        float distance = parts[0], duration = parts[1];

        if (DistanceToPlayer < distance)
        {
            distance -= (distance - DistanceToPlayer);
        }

        Lunge(distance, duration);
    }

    public void ShakeCamera()
    {
        CameraActions.Main.Shake(0.15f, 0.12f, 0.08f);
    }

    public void GuardSlam()
    {
        StartCoroutine(SlamHitRoutine());
    }

    private IEnumerator SlamHitRoutine()
    {
        Stunned = true;

        SphereCollider col = CrystalShockwaveCollider.GetComponent<SphereCollider>();
        col.radius = 1f;

        if (swing.active)
        {
            AlterCrystalShockwave(1);
        }

        AlterHalberd(0);

        ParticleSpawner.Spawn(Particles.P_GuardSlam, transform.position + (transform.forward * 3.25f), transform.rotation);
        CameraActions.Main.Shake(0.4f, 0.22f, 0.12f);

        float time = 0f;
        float duration = 0.35f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            float radius = Mathf.Lerp(1f, 4f, t);
            col.radius = radius;

            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        AlterCrystalShockwave(0);

        yield return new WaitForSeconds(1f);

        OnActionEnd();
        Stunned = false;
    }

    protected override void Die()
    {
        _animator.fireEvents = false;
        _animator.SetBool("Died", true);

        // If dead is set to true before base.Die(), it means the enemy was dead in save data,
        // hence player has already killed them and no death animation
        if (Dead)
        {
            _animator.Play("Die", -1, 1f);
            _animator.Update(0f);
            crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
            crystalMaterial.SetColor("_TopColor", depletedTopColor);
            Glowing = false;
            StopAllCoroutines();
        }
        else
        {
            StopGlow(true);
        }

        AlterHalberd(0);
        AlterCrystalShockwave(0);
        MainCollider.enabled = false;
        HalberdCollider.enabled = false;
        CrystalShockwaveCollider.enabled = false;
        base.Die();
        this.enabled = false;
    }

    public void PlayFootstepSound()
    {
        int randomClip = UnityEngine.Random.Range(0, footsteps.Count);

        //float randomPitch = UnityEngine.Random.Range(0.85f, 1.05f);
        //audioSource.volume = 2.25f;
        //audioSource.pitch = 0.65f;

        SoundFXManager.instance.PlaySoundFXClip(footsteps[randomClip], transform, 0.55f, 0.8f, 2.25f);
        //audioSource.PlayOneShot(footsteps[randomClip]);
    }

    public override void OnActionEnd()
    {
        base.OnActionEnd();
    }

    public void StartGlow()
    {
        if (startGlowRoutine != null)
            StopCoroutine(startGlowRoutine);

        if (stopGlowRoutine != null)
            StopCoroutine(stopGlowRoutine);

        startGlowRoutine = StartCoroutine(StartGlowRoutine());

        if (MoveCameraOnWake)
        {
            if (cameraMovement == null)
                return;

            cameraMovement.SetOverrideTarget(this.transform, 1f);
        }
    }

    public void StopGlow(bool stopAllRoutines = false)
    {
        if (stopGlowRoutine != null)
            StopCoroutine(stopGlowRoutine);

        if (startGlowRoutine != null)
            StopCoroutine(startGlowRoutine);

        stopGlowRoutine = StartCoroutine(StopGlowRoutine(stopAllRoutines));
    }

    private IEnumerator StartGlowRoutine()
    {
        Color startBaseColor = crystalMaterial.GetColor("_BaseColor");
        Color startTopColor = crystalMaterial.GetColor("_TopColor");

        yield return new WaitForSeconds(0.75f);

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newBaseColor = Color.Lerp(startBaseColor, origBaseColor, t2);
            Color newTopColor = Color.Lerp(startTopColor, origTopColor, t2);

            crystalMaterial.SetColor("_BaseColor", newBaseColor);
            crystalMaterial.SetColor("_TopColor", newTopColor);

            yield return null;
        }

        crystalMaterial.SetColor("_BaseColor", origBaseColor);
        crystalMaterial.SetColor("_TopColor", origTopColor);

        _animator.SetBool("Awake", true);

        if (MoveCameraOnWake && cameraMovement != null)
        {
            yield return new WaitForSeconds(0.5f);
            Glowing = true;
            yield return new WaitForSeconds(0.5f);

            cameraMovement.ClearOverrideTarget();
        }
        else
        {
            Glowing = true;
        }

        startGlowRoutine = null;
    }

    private IEnumerator StopGlowRoutine(bool stopAllRoutines = false)
    {
        Glowing = false;
        Color startBaseColor = crystalMaterial.GetColor("_BaseColor");
        Color startTopColor = crystalMaterial.GetColor("_TopColor");

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newBaseColor = Color.Lerp(startBaseColor, depletedBaseColor, t2);
            Color newTopColor = Color.Lerp(startTopColor, depletedTopColor, t2);

            crystalMaterial.SetColor("_BaseColor", newBaseColor);
            crystalMaterial.SetColor("_TopColor", newTopColor);

            yield return null;
        }

        crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
        crystalMaterial.SetColor("_TopColor", depletedTopColor);

        if (stopAllRoutines)
        {
            StopAllCoroutines();
        }

        stopGlowRoutine = null;
    }
}
