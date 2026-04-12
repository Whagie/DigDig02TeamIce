using UnityEngine;
using UnityEngine.Audio;

public class SoundFXAudioSource : MonoBehaviour
{
    public AudioSource audioSource;

    public AudioMixerGroup UIMixerGroup;

    public float DurationBeforeDestroy;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SoundFXManager.instance.SoundFXAudioSources.Add(this);
    }

    private void OnDisable()
    {
        SoundFXManager.instance.SoundFXAudioSources.Remove(this);
    }

    private void Update()
    {
        if (DurationBeforeDestroy > 0f)
        {
            if (audioSource.time >= DurationBeforeDestroy)
            {
                SoundFXManager.instance.SoundFXAudioSources.Remove(this);
                Destroy(gameObject);
            }
        }
    }
}
