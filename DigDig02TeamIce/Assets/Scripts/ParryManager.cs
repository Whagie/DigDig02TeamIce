using Game.Core;
using System.Collections.Generic;
using UnityEngine;

public class ParryManager : MonoBehaviour, IHurtbox
{
    public GameObject Owner => gameObject;
    public Collider Collider => ParryCollider;
    public bool UseMeshCollision { get; set; } = false;

    [SerializeField] private LayerMask layers;
    public LayerMask LayerMask => layers;

    private Collider[] overlapBuffer = new Collider[24]; // Adjust size based on max expected hits
    private readonly HashSet<IHitbox> parriedThisSession = new();

    public GameObject ParryAnimation;
    public Collider ParryCollider;

    [SerializeField] private Player player;

    private enum ParryState { Ready, Active, Cooldown }
    private ParryState state = ParryState.Ready;

    public float parryLength;
    public float parryCooldown;
    public float shortenedCooldownMultiplier = 0.4f;

    private float parryLengthTimer;
    private float parryCooldownTimer;
    private float cooldownMultiplier = 1f;

    public int LastParryFrame { get; private set; } = -1;
    public bool CanParry { get; private set; } = true;
    private bool parryResolvedThisFrame = false;

    public event System.Action OnParryStart;
    public event System.Action OnParryEnd;
    public event System.Action OnParryCooldownEnd;
    public event System.Action<IHitbox> OnParried;

    private void Start()
    {
        ParryCollider = GetComponent<Collider>();
        player = GameObject.FindObjectOfType<Player>();
        ParryCollider.enabled = false;
    }

    public void Parry()
    {
        if (UserInput.ParryPressed && CanParry && !player.Invisible)
        {
            UserInput.ConsumeParry();
            ParryBegin();
            Instantiate(ParryAnimation, transform.position, Quaternion.identity);
        }
    }

    private void Update()
    {
        Parry();

        switch (state)
        {
            case ParryState.Active:
                parryLengthTimer -= Time.deltaTime;
                if (parryLengthTimer <= 0f)
                {
                    EndParry();
                }
                else
                {
                    CheckOverlaps(); // check for hits each frame
                }
                break;

            case ParryState.Cooldown:
                parryCooldownTimer -= Time.deltaTime;
                if (parryCooldownTimer <= 0f)
                {
                    CanParry = true;
                    state = ParryState.Ready;
                    OnParryCooldownEnd?.Invoke();
                }
                break;
        }
    }

    public void OnHit(IHitbox source)
    {

    }

    private void ParryBegin()
    {
        if (state != ParryState.Ready) return;

        parriedThisSession.Clear();
        state = ParryState.Active;
        parryLengthTimer = parryLength;
        CanParry = false;
        ParryCollider.enabled = true;
        parryResolvedThisFrame = false;
        player.DamageCollider.enabled = false;
        player.Parrying = true;
        player.wrenchAttack.Deactivate();
        OnParryStart?.Invoke();
    }

    private void EndParry()
    {
        ParryCollider.enabled = false;
        player.DamageCollider.enabled = true;
        player.Parrying = false;
        OnParryEnd?.Invoke();

        parriedThisSession.Clear(); // reset per parry session

        state = ParryState.Cooldown;
        parryCooldownTimer = parryCooldown * cooldownMultiplier;

        cooldownMultiplier = 1f;
    }

    public void TakeDamage(int dmg)
    {
        // not used here, but needed for IHurtbox
    }

    /// <summary>
    /// Actively checks for overlapping IHitboxes — catches melee colliders spawning inside.
    /// </summary>
    private void CheckOverlaps()
    {
        int hitCount = 0;

        if (ParryCollider is BoxCollider box)
        {
            hitCount = Physics.OverlapBoxNonAlloc(
                box.bounds.center,
                box.bounds.extents,
                overlapBuffer,
                box.transform.rotation,
                layers
            );
        }
        else
        {
            return;
        }

        for (int i = 0; i < hitCount; i++)
        {
            var hit = overlapBuffer[i];
            if (hit == null) continue;

            if (hit.TryGetComponent<IHitbox>(out var hitbox))
            {
                if (!hitbox.CanBeParried || !hitbox.Collider.enabled)
                    continue;

                if (!parriedThisSession.Contains(hitbox))
                {
                    parriedThisSession.Add(hitbox);

                    if (!parryResolvedThisFrame)
                    {
                        parryResolvedThisFrame = true;
                        LastParryFrame = Time.frameCount;

                        cooldownMultiplier = shortenedCooldownMultiplier;
                        EndParry();
                    }

                    OnParried?.Invoke(hitbox);
                    hitbox.OnParried(this);
                }
            }
        }
    }

    private void OnDisable()
    {
        ParryCollider.enabled = false;
        player.Parrying = false;
        CanParry = true;
    }
}
