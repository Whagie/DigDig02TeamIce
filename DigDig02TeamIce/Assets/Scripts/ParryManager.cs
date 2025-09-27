using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParryManager : MonoBehaviour
{
    public GameObject ParryAnimation;

    private Coroutine parryRoutine;

    public const float parryLength = 0.2f;
    public const float parryCooldown = 1f;

    public float parryLengthTimer = 0f;
    public float parryCooldownTimer = 0f;
    public bool Parrying { get; private set; }
    public bool CanParry { get; private set; }

    public event System.Action OnParryStart;
    public event System.Action OnParryEnd;
    public event System.Action OnParryCooldownEnd;

    public void Parry(InputAction.CallbackContext context)
    {
        if (context.performed && !Parrying && parryCooldownTimer <= 0f)
        {            
            ParryBegin();

            Instantiate(ParryAnimation, transform.position, Quaternion.identity);
            ParticleSpawner.Spawn(Particles.P_spark, transform.position);

            CameraActions.Main.Punch(-0.8f, 0.15f);
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
        Parrying = true;
        CanParry = false;

        while (parryLengthTimer > 0f)
        {
            parryLengthTimer -= Time.deltaTime;            
            yield return null;
        }

        Parrying = false;
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
}
