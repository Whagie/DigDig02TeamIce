using UnityEngine;
using System.Collections;

public class BlinkController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string blinkParameter = "BlinkValue";

    [Header("Blink Settings")]
    public float blinksPerMinute = 17f;
    public float tickInterval = 0.5f;
    public float blinkRateMultiplier = 1f;

    [Header("Baseline Open Value (0–100)")]
    [Range(0f, 100f)]
    public float baselineOpenValue = 0f;

    [Header("Blink Timing")]
    public Vector2 blinkDurationRange = new Vector2(0.3f, 0.4f);
    [SerializeField] private float closeDuration = 0.07f;
    [SerializeField] private float openDuration = 0.12f;

    private bool blinking;

    void Start()
    {
        ApplyBlinkValue(0f); // start at baseline
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float blinkChance =
                (blinksPerMinute / 60f) *
                tickInterval *
                blinkRateMultiplier;

            if (!blinking && Random.value < blinkChance)
            {
                yield return StartCoroutine(DoBlink());
            }

            yield return new WaitForSeconds(tickInterval);
        }
    }

    IEnumerator DoBlink()
    {
        blinking = true;

        // Close
        yield return StartCoroutine(LerpBlink(0f, 1f, closeDuration));

        // Hold closed
        yield return new WaitForSeconds(
            Random.Range(blinkDurationRange.x, blinkDurationRange.y)
        );

        // Open
        yield return StartCoroutine(LerpBlink(1f, 0f, openDuration));

        blinking = false;
    }

    IEnumerator LerpBlink(float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float progress = Mathf.SmoothStep(0f, 1f, t / duration);
            float blinkT = Mathf.Lerp(from, to, progress);

            ApplyBlinkValue(blinkT);

            yield return null;
        }

        ApplyBlinkValue(to);
    }

    void ApplyBlinkValue(float t)
    {
        // Convert internal 0–1 blink to animator 0–1
        float animatorValue =
            (baselineOpenValue + (100f - baselineOpenValue) * t) / 100f;

        animator.SetFloat(blinkParameter, animatorValue);
    }
}