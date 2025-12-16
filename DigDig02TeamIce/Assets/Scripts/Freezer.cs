using System.Collections;
using UnityEngine;

public static class Freezer
{
    private static int freezeCount;

    private static GameObject runnerObject;
    private static MonoBehaviour runner;
    private static Coroutine freezeCoroutine;

    private static float previousTimeScale;

    public static bool IsFrozen => freezeCount > 0;

    /// <summary>
    /// Freezes time. If duration > 0, automatically unfreezes after that many real-time seconds.
    /// If duration <= 0, freezes indefinitely until Cancel() is called.
    /// </summary>
    public static void Freeze(float duration = -1f)
    {
        EnsureRunner();

        if (freezeCount == 0)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        freezeCount++;

        if (duration > 0f)
        {
            if (freezeCoroutine != null)
            {
                runner.StopCoroutine(freezeCoroutine);
            }

            freezeCoroutine = runner.StartCoroutine(FreezeRoutine(duration));
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
