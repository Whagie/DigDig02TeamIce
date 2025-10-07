using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryManager : Entity
{
    public GameObject ParryAnimation;
    public Collider ParryCollider;

    [SerializeField] private Player player;

    private enum ParryState
    {
        Ready,
        Active,
        Cooldown
    }

    private ParryState state = ParryState.Ready;

    public float parryLength = 0.2f;
    public float parryCooldown = 0.5f;

    private float parryLengthTimer = 0f;
    private float parryCooldownTimer = 0f;
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
                    // End parry
                    ParryCollider.enabled = false;
                    player.Parrying = false;
                    OnParryEnd?.Invoke();

                    // Start cooldown
                    state = ParryState.Cooldown;
                    parryCooldownTimer = parryCooldown;
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

    private void ParryBegin()
    {
        if (state != ParryState.Ready) return;

        parryLengthTimer = parryLength;
        CanParry = false;

        ParryCollider.enabled = true;
        player.Parrying = true;
        OnParryStart?.Invoke();

        state = ParryState.Active;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryParry(other);
    }
    private void OnTriggerStay(Collider other)
    {
        TryParry(other);
    }

    private void TryParry(Collider other)
    {
        if (!ParryCollider.enabled)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Attack"))
        {
            if (!parried)
            {
                OnParried?.Invoke();
                parried = true;
            }

            if (other.CompareTag("Projectile"))
            {
                Projectile proj = other.GetComponent<Projectile>();
                if (!proj.Rebound) // prevent double parry
                {
                    proj.Reflect(-proj.Direction);
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
