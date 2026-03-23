using System.Collections;
using UnityEngine;

public static class Freezer
{
    private static int freezeCount;

    private static GameObject runnerObject;
    private static MonoBehaviour runner;
    private static Coroutine freezeCoroutine;

    private static float previousTimeScale;

    private static Coroutine timeScaleCoroutine;
    private static bool isTimeScaling;
    public static bool IsTimeScaling => isTimeScaling;

    public static bool IsFrozen => freezeCount > 0;

    /// <summary>
    /// Freezes time. If duration > 0, automatically unfreezes after that many real-time seconds.
    /// If duration <= 0, freezes indefinitely until Cancel() is called.
    /// </summary>
    public static void Freeze(float duration = -1f)
    {
        if (isTimeScaling)
            return;

        EnsureRunner();

        if (freezeCount == 0)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        freezeCount++;

        if (duration > 0f)
        {
            runner.StartCoroutine(FreezeRoutine(duration));
        }
    }

    /// <summary>
    /// Cancels one freeze request. Time resumes only when all freezes are canceled.
    /// </summary>
    public static void Cancel()
    {
        if (freezeCount <= 0)
            return;

        freezeCount--;

        if (freezeCount > 0)
            return;

        if (freezeCoroutine != null)
        {
            runner.StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        Time.timeScale = previousTimeScale;
        freezeCount = 0;
    }

    /// <summary>
    /// Immediately removes all freezes and restores time.
    /// </summary>
    public static void ForceCancelAll()
    {
        freezeCount = 0;

        if (freezeCoroutine != null && runner != null)
        {
            runner.StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        Time.timeScale = previousTimeScale;
    }

    private static IEnumerator FreezeRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        Cancel();
    }

    public static void LerpTimeScale(float targetScale, float fadeOutDuration, float waitDuration, float fadeInDuration)
    {
        EnsureRunner();

        // Stop freeze system completely
        ForceCancelAll();

        // Stop any existing lerp
        if (timeScaleCoroutine != null)
        {
            runner.StopCoroutine(timeScaleCoroutine);
        }

        timeScaleCoroutine = runner.StartCoroutine(TimeScaleRoutine(targetScale, fadeOutDuration, waitDuration, fadeInDuration));
    }

    private static IEnumerator TimeScaleRoutine(float target, float fadeOut, float wait, float fadeIn)
    {
        isTimeScaling = true;

        float start = Time.timeScale;

        float t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float lerp = fadeOut > 0f ? t / fadeOut : 1f;
            Time.timeScale = Mathf.Lerp(start, target, lerp);
            yield return null;
        }

        Time.timeScale = target;

        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float lerp = fadeIn > 0f ? t / fadeIn : 1f;
            Time.timeScale = Mathf.Lerp(target, 1f, lerp);
            yield return null;
        }

        Time.timeScale = 1f;

        isTimeScaling = false;
        timeScaleCoroutine = null;
    }

    private static void EnsureRunner()
    {
        if (runner != null)
            return;

        runnerObject = new GameObject("FreezerRunner");
        Object.DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<FreezerRunner>();
    }

    private class FreezerRunner : MonoBehaviour { }
}
