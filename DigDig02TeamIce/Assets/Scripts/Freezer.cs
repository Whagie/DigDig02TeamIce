using System.Collections;
using UnityEngine;

public static class Freezer
{
    private static bool isFrozen;
    private static GameObject runnerObject;
    private static MonoBehaviour runner;

    public static void Freeze(float duration = 1f)
    {
        if (isFrozen) return;

        if (runner == null)
        {
            runnerObject = new GameObject("FreezerRunner");
            Object.DontDestroyOnLoad(runnerObject);
            runner = runnerObject.AddComponent<FreezerRunner>();
        }

        runner.StartCoroutine(FreezeRoutine(duration));
    }

    private static IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        float original = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = original;
        isFrozen = false;
    }

    private class FreezerRunner : MonoBehaviour { }
}