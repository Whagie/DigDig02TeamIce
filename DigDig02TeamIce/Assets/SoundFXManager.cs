using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [SerializeField] private SoundFXAudioSource soundFXObject;

    public List<SoundFXAudioSource> SoundFXAudioSources = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume = 1f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"AudioClip was null for {spawnTransform.gameObject}!");
            return;
        }

        SoundFXAudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.audioSource.clip = audioClip;

        audioSource.audioSource.volume = volume;

        audioSource.audioSource.Play();

        float clipLength = audioSource.audioSource.clip.length;

        audioSource.DurationBeforeDestroy = clipLength;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="audioClip"></param>
    /// <param name="spawnTransform"></param>
    /// <param name="pitchLowerLimit">Minimum random pitch possible, clamped to [-3..3]</param>
    /// <param name="pitchUpperLimit">Maximum random pitch possible, clamped to [-3..3]</param>
    /// <param name="volume"></param>
    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float pitchLowerLimit = 1f, float pitchUpperLimit = 1f, float volume = 1f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"AudioClip was null for {spawnTransform.gameObject}!");
            return;
        }

        SoundFXAudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.audioSource.clip = audioClip;

        audioSource.audioSource.volume = volume;

        float randomPitch = Random.Range(pitchLowerLimit, pitchUpperLimit);
        audioSource.audioSource.pitch = randomPitch;

        audioSource.audioSource.Play();

        float clipLength = audioSource.audioSource.clip.length;

        audioSource.DurationBeforeDestroy = clipLength;
    }

    public void PlaySoundFXClipLooping(AudioClip audioClip, Transform spawnTransform, out AudioSource source, float volume = 1f, float pitch = 1f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"AudioClip was null for {spawnTransform.gameObject}!");
            source = null;
            return;
        }

        SoundFXAudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, spawnTransform);

        audioSource.audioSource.loop = true;

        audioSource.audioSource.clip = audioClip;

        audioSource.audioSource.volume = volume;

        audioSource.audioSource.pitch = pitch;

        audioSource.audioSource.Play();

        source = audioSource.audioSource;
    }

    public void PlayUISoundFX(AudioClip audioClip, bool addToUIGroup = false, float volume = 1f, float pitch = 1f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"AudioClip was null!");
            return;
        }

        SoundFXAudioSource audioSource = Instantiate(soundFXObject, transform.position, Quaternion.identity);

        if (addToUIGroup)
        {
            audioSource.audioSource.outputAudioMixerGroup = audioSource.UIMixerGroup;
        }

        audioSource.audioSource.clip = audioClip;
        audioSource.audioSource.volume = volume;
        audioSource.audioSource.pitch = pitch;

        audioSource.audioSource.Play();

        float clipLength = audioSource.audioSource.clip.length;

        audioSource.DurationBeforeDestroy = clipLength;
    }

    public void PauseSoundEffects()
    {
        foreach (var fx in SoundFXAudioSources)
        {
            if (fx != null)
            {
                fx.audioSource.Pause();
            }
        }
    }

    public void UnpauseSoundEffects()
    {
        foreach (var fx in SoundFXAudioSources)
        {
            if (fx != null)
            {
                fx.audioSource.UnPause();
            }
        }
    }
}
