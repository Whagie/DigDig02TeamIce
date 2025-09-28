using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryManager : Entity
{
    public GameObject ParryAnimation;
    public Collider ParryCollider;
    private Coroutine parryRoutine;

    private Player player;

    public float parryLength = 0.2f;
    public float parryCooldown = 0.5f;

    private float parryLengthTimer = 0f;
    private float parryCooldownTimer = 0f;    
    public bool CanParry { get; private set; }

    public event System.Action OnParryStart;
    public event System.Action OnParryEnd;
    public event System.Action OnParryCooldownEnd;
    public event System.Action OnParried;

    protected override void OnAwake()
    {
        ParryCollider = GetComponent<Collider>();
        player = TrackerHost.Current.Get<Player>();
    }
    public void Parry(InputAction.CallbackContext context)
    {
        if (context.performed && parryCooldownTimer <= 0f)
        {            
            ParryBegin();

            Instantiate(ParryAnimation, transform.position, Quaternion.identity);
        }
    }

    private void ParryBegin()
    {
        parryLengthTimer = parryLength;
        parryCooldownTimer = parryCooldown;

        if (parryRoutine != null)
            StopCoroutine(parryRoutine);

        parryRoutine = StartCoroutine(ParryRoutine());

        OnParryStart?.Invoke();
    }

    private IEnumerator ParryRoutine()
    {
        CanParry = false;
        ParryCollider.enabled = true;
        player.Parrying = true;

        while (parryLengthTimer > 0f)
        {
            parryLengthTimer -= Time.deltaTime;            
            yield return null;
        }

        ParryCollider.enabled = false;
        player.Parrying = false;
        OnParryEnd?.Invoke();

        while (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -= Time.deltaTime;
            yield return null;
        }

        CanParry = true;
        OnParryCooldownEnd?.Invoke();
        parryRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Attack"))
        {
            OnParried?.Invoke();
        }
    }
}
