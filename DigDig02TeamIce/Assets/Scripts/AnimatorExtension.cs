using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimatorExtension
{
    public static void SetTriggerOneFrame(this Animator anim, MonoBehaviour coroutineRunner, string trigger)
    {
        coroutineRunner.StartCoroutine(TriggerOneFrame(anim, trigger));
    }

    private static IEnumerator TriggerOneFrame(Animator anim, string trigger)
    {
        anim.SetTrigger(trigger);
        yield return null;
        if (anim != null)
        {
            anim.ResetTrigger(trigger);
        }
    }
}
