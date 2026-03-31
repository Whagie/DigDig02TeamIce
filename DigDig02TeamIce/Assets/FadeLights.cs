using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeLights : MonoBehaviourID
{
    public List<ParticleSystem> particleSystems = new();
    public List<Light> lights = new();

    public float FadeOutDuration = 1f;

    [HideInInspector] public bool FadedOut = false;

    private SessionSaveData.SingleBoolData fadedData;

    private void Start()
    {
        if (SessionSaveData.Instance.TryGet(ID, out fadedData))
        {
            FadedOut = fadedData.IsTrue;
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, FadedOut);
        }

        if (FadedOut)
        {
            foreach (var light in lights)
            {
                Destroy(light.gameObject);
            }

            foreach (var particles in particleSystems)
            {
                Destroy(particles.gameObject);
            }
        }
    }

    public void FadeOutParticles()
    {
        if (FadedOut)
            return;

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        FadedOut = true;
        SessionSaveData.Instance.AddOrUpdateData(ID, FadedOut);

        float time = 0f;

        // Cache materials once (important so we don't create new instances every frame)
        var materials = new List<Material>();

        List<float> lightStartValues = new List<float>();

        foreach (var system in particleSystems)
        {
            if (system == null) continue;

            // Stop emitting new particles
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) continue;

            // This creates an instance (what you want)
            materials.Add(renderer.material);
        }

        foreach (var light in lights)
        {
            if (light == null) continue;

            lightStartValues.Add(light.intensity);
        }

        // Store initial values
        var startColors = new List<Color>();
        var startEmissionColors = new List<Color>();
        var hasEmission = new List<bool>();

        foreach (var mat in materials)
        {
            startColors.Add(mat.HasProperty("_Color") ? mat.color : Color.white);

            if (mat.HasProperty("_EmissionColor"))
            {
                startEmissionColors.Add(mat.GetColor("_EmissionColor"));
                hasEmission.Add(true);
            }
            else
            {
                startEmissionColors.Add(Color.black);
                hasEmission.Add(false);
            }
        }

        // Fade loop
        while (time < FadeOutDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / FadeOutDuration);

            for (int i = 0; i < materials.Count; i++)
            {
                var mat = materials[i];

                // Fade alpha
                if (mat.HasProperty("_Color"))
                {
                    Color c = startColors[i];
                    c.a = Mathf.Lerp(1f, 0f, t);
                    mat.color = c;
                }

                // Fade emission
                if (hasEmission[i])
                {
                    Color emission = Color.Lerp(startEmissionColors[i], Color.black, t);
                    mat.SetColor("_EmissionColor", emission);
                }
            }

            for (int i = 0; i < lights.Count; i++)
            {
                float intensity = Mathf.Lerp(lightStartValues[i], 0f, t);
                lights[i].intensity = intensity;
            }

            yield return null;
        }
    }
}
