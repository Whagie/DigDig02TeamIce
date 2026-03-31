using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource AudioSourceA;
    public AudioSource AudioSourceB;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        MusicManager.instance.Play(FX.Music_NoCombat, true);
        MusicManager.instance.AudioSourceA.volume = 1f;
    }

    public void Play(AudioClip clip, bool useAudioSourceA)
    {
        if (clip == null)
            return;

        var source = useAudioSourceA ? AudioSourceA : AudioSourceB;

        source.clip = clip;
        source.Play();
    }

    // Fade A <-> B
    public void Crossfade(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(CrossfadeRoutine(duration));
    }

    private IEnumerator CrossfadeRoutine(float duration)
    {
        float time = 0f;

        float startA = AudioSourceA.volume;
        float startB = AudioSourceB.volume;

        float targetA = startA > 0.5f ? 0f : 1f;
        float targetB = startB > 0.5f ? 0f : 1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            AudioSourceA.volume = Mathf.Lerp(startA, targetA, t);
            AudioSourceB.volume = Mathf.Lerp(startB, targetB, t);

            yield return null;
        }

        AudioSourceA.volume = targetA;
        AudioSourceB.volume = targetB;
    }

    // Fade in B (combat), fade out A
    public void EnterSecondary(AudioClip clip, float duration)
    {
        if (clip != null && AudioSourceB.clip != clip)
        {
            AudioSourceB.clip = clip;
            AudioSourceB.Play();
        }

        StartFade(AudioSourceA, 0f, duration);
        StartFade(AudioSourceB, 1f, duration);
    }

    // Return to A (background)
    public void ExitSecondary(float duration)
    {
        StartFade(AudioSourceA, 1f, duration);
        StartFade(AudioSourceB, 0f, duration);
    }

    private void StartFade(AudioSource source, float target, float duration)
    {
        StartCoroutine(FadeRoutine(source, target, duration));
    }

    private IEnumerator FadeRoutine(AudioSource source, float target, float duration)
    {
        float time = 0f;
        float start = source.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            source.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }

        source.volume = target;
    }
}