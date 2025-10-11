using Game.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryManager : Entity, IHurtbox
{
    public GameObject Owner => gameObject;
    public Collider Collider => ParryCollider;

    [SerializeField] private LayerMask layers;
    public LayerMask LayerMask => layers;

    private Collider[] overlapBuffer = new Collider[24]; // Adjust size based on max expected hits
    private readonly HashSet<IHitbox> parriedThisSession = new();

    public GameObject ParryAnimation;
    public Collider ParryCollider;

    [SerializeField] private Player player;

    private enum ParryState { Ready, Active, Cooldown }
    private ParryState state = ParryState.Ready;

    public float parryLength = 0.2f;
    public float parryCooldown = 0.5f;

    private float parryLengthTimer;
    private float parryCooldownTimer;

    public bool CanParry { get; private set; } = true;
    private bool parried = false;

    public event System.Action OnParryStart;
    public event System.Action OnParryEnd;
    public event System.Action OnParryCooldownEnd;
    public event System.Action OnParried;

    protected override void OnStart()
    {
        ParryCollider = GetComponent<Collider>();
        player = TrackerHost.Current.Get<Player>();
        ParryCollider.enabled = false;
    }

    public void Parry(InputAction.CallbackContext context)
    {
        if (context.performed && CanParry && !player.Invisible)
        {
            ParryBegin();
            Instantiate(ParryAnimation, transform.position, Quaternion.identity);
        }
    }

    protected override void OnUpdate()
    {
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
                    parried = false;
                    state = ParryState.Ready;
                    OnParryCooldownEnd?.Invoke();
                }
                break;
        }
    }

    public void OnHit(IHitbox source)
    {
        if (!source.CanBeParried)
            return;

        if (!parried)
        {
            parried = true;
            source.OnParried(this);
        }
    }

    private void ParryBegin()
    {
        if (state != ParryState.Ready) return;

        parriedThisSession.Clear();
        state = ParryState.Active;
        parryLengthTimer = parryLength;
        CanParry = false;
        ParryCollider.enabled = true;
        player.Parrying = true;
        OnParryStart?.Invoke();
    }

    private void EndParry()
    {
        ParryCollider.enabled = false;
        player.Parrying = false;
        OnParryEnd?.Invoke();

        parriedThisSession.Clear(); // reset per parry session
        parried = false;

        state = ParryState.Cooldown;
        parryCooldownTimer = parryCooldown;
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
                LayerMask.GetMask("Attack")
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

                    parried = true;
                    OnParried?.Invoke();

                    hitbox.OnParried(this);
                }
            }
        }
    }

    protected override void OnEntityDisable()
    {
        ParryCollider.enabled = false;
        player.Parrying = false;
        CanParry = true;
    }
}
