using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightPassThrough : MonoBehaviourID
{
    public bool ReceivingLight = false;

    public GameObject Crystal;
    private Material crystalMaterial;
    private Material glowMaterial;
    public Color origBaseColor;
    public Color origTopColor;
    public Color origGlowColor;

    private Color depletedBaseColor = new Color32(51, 51, 128, 255);
    private Color depletedTopColor = new Color32(92, 113, 153, 255);
    private Color depletedGlowColor;

    public float StartGlowDuration = 0.4f;
    public float DropDuration = 0.75f;

    public bool Glowing = false;

    private Coroutine startGlowRoutine;
    private Coroutine stopGlowRoutine;

    private SessionSaveData.LightPuzzleGeneralData lightObjectData;

    private void OnEnable()
    {
        SceneSwapManager.instance.OnStartSceneSwap += SaveData;
    }
    private void OnDisable()
    {
        SceneSwapManager.instance.OnStartSceneSwap -= SaveData;
    }

    private void Start()
    {
        Renderer renderer1 = Crystal.GetComponent<Renderer>();
        Material[] mats1 = renderer1.materials;
        int matIndex1 = Array.FindIndex(mats1, m => m.name.Contains("ReflectorCrystal"));
        int matIndex2 = Array.FindIndex(mats1, m => m.name.Contains("Glow"));
        crystalMaterial = mats1[matIndex1];
        glowMaterial = mats1[matIndex2];
        glowMaterial.EnableKeyword("_EMISSION");

        origBaseColor = crystalMaterial.GetColor("_BaseColor");
        origTopColor = crystalMaterial.GetColor("_TopColor");
        origGlowColor = glowMaterial.GetColor("_EmissionColor");
        depletedGlowColor = origGlowColor * 0.0125f;

        if (SessionSaveData.Instance.TryGet(ID, out lightObjectData))
        {
            Glowing = lightObjectData.Glowing;
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, Glowing);
        }

        if (Glowing)
        {
            crystalMaterial.SetColor("_BaseColor", origBaseColor);
            crystalMaterial.SetColor("_TopColor", origTopColor);
            glowMaterial.SetColor("_EmissionColor", origGlowColor);
        }
        else
        {
            crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
            crystalMaterial.SetColor("_TopColor", depletedTopColor);
            glowMaterial.SetColor("_EmissionColor", depletedGlowColor);
        }
    }

    private void Update()
    {
        if (ReceivingLight && !Glowing)
        {
            StartGlow();
        }

        if (!ReceivingLight && Glowing)
        {
            StopGlow();
        }
    }

    public void StartGlow()
    {
        if (startGlowRoutine != null)
            StopCoroutine(startGlowRoutine);

        if (stopGlowRoutine != null)
            StopCoroutine(stopGlowRoutine);

        startGlowRoutine = StartCoroutine(StartGlowRoutine());
    }

    public void StopGlow()
    {
        if (stopGlowRoutine != null)
            StopCoroutine(stopGlowRoutine);

        if (startGlowRoutine != null)
            StopCoroutine(startGlowRoutine);

        stopGlowRoutine = StartCoroutine(StopGlowRoutine());
    }

    private IEnumerator StartGlowRoutine()
    {
        Glowing = true;

        Color startBaseColor = crystalMaterial.GetColor("_BaseColor");
        Color startTopColor = crystalMaterial.GetColor("_TopColor");
        Color startGlowColor = glowMaterial.GetColor("_EmissionColor");

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newBaseColor = Color.Lerp(startBaseColor, origBaseColor, t2);
            Color newTopColor = Color.Lerp(startTopColor, origTopColor, t2);
            Color newGlowColor = Color.Lerp(startGlowColor, origGlowColor, t2);

            crystalMaterial.SetColor("_BaseColor", newBaseColor);
            crystalMaterial.SetColor("_TopColor", newTopColor);
            glowMaterial.SetColor("_EmissionColor", newGlowColor);

            yield return null;
        }

        crystalMaterial.SetColor("_BaseColor", origBaseColor);
        crystalMaterial.SetColor("_TopColor", origTopColor);
        glowMaterial.SetColor("_EmissionColor", origGlowColor);

        startGlowRoutine = null;
    }

    private IEnumerator StopGlowRoutine()
    {
        Glowing = false;
        Color startBaseColor = crystalMaterial.GetColor("_BaseColor");
        Color startTopColor = crystalMaterial.GetColor("_TopColor");
        Color startGlowColor = glowMaterial.GetColor("_EmissionColor");

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newBaseColor = Color.Lerp(startBaseColor, depletedBaseColor, t2);
            Color newTopColor = Color.Lerp(startTopColor, depletedTopColor, t2);
            Color newGlowColor = Color.Lerp(startGlowColor, depletedGlowColor, t2);

            crystalMaterial.SetColor("_BaseColor", newBaseColor);
            crystalMaterial.SetColor("_TopColor", newTopColor);
            glowMaterial.SetColor("_EmissionColor", newGlowColor);

            yield return null;
        }

        crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
        crystalMaterial.SetColor("_TopColor", depletedTopColor);
        glowMaterial.SetColor("_EmissionColor", depletedGlowColor);

        stopGlowRoutine = null;
    }

    private void SaveData()
    {
        SessionSaveData.Instance.AddOrUpdateData(ID, Glowing);
    }
}
