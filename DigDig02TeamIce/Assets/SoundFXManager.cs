using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [SerializeField] private AudioSource soundFXObject;

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

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.PlayOneShot(audioClip);

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
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

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        float randomPitch = Random.Range(pitchLowerLimit, pitchUpperLimit);
        audioSource.pitch = randomPitch;

        audioSource.PlayOneShot(audioClip);

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlaySoundFXClipLooping(AudioClip audioClip, Transform spawnTransform, out AudioSource source, float volume = 1f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"AudioClip was null for {spawnTransform.gameObject}!");
            source = null;
            return;
        }

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, spawnTransform);

        audioSource.loop = true;

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.Play();

        source = audioSource;
    }
}
