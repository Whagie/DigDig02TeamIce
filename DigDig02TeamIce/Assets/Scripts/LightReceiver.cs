using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LightReceiver : MonoBehaviour
{
    public List<LightPassThrough> PassThroughs = new();

    [HideInInspector] public bool AllPassThroughsHit = false;
    [HideInInspector] public bool ReceivingLight = false;

    public bool Activated = false;

    public GameObject Crystal;
    private Material crystalMaterial;
    public Color origBaseColor;
    public Color origTopColor;

    private Color depletedBaseColor = new Color32(51, 51, 128, 255);
    private Color depletedTopColor = new Color32(92, 113, 153, 255);

    public float StartGlowDuration = 2f;

    public bool Glowing = false;

    private Coroutine startGlowRoutine;
    private Coroutine stopGlowRoutine;

    [SerializeField] private List<GameObject> destroyOnRecieve;

    public LightReceiverEvent OnReceiveLight;

    private void Start()
    {
        Renderer renderer1 = Crystal.GetComponent<Renderer>();
        Material[] mats1 = renderer1.materials;
        int matIndex1 = Array.FindIndex(mats1, m => m.name.Contains("ReflectorCrystal"));
        crystalMaterial = mats1[matIndex1];
        //crystalMaterial.EnableKeyword("_EMISSION");

        origBaseColor = crystalMaterial.GetColor("_BaseColor");
        origTopColor = crystalMaterial.GetColor("_TopColor");

        crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
        crystalMaterial.SetColor("_TopColor", depletedTopColor);
    }
    private void Update()
    {
        if (Activated)
            return;

        AllPassThroughsHit = true;
        foreach (var passThrough in PassThroughs)
        {
            if (!passThrough.ReceivingLight)
            {
                AllPassThroughsHit = false;
                break;
            }
        }

        if (ReceivingLight && !Glowing && AllPassThroughsHit)
        {
            StartGlow();
        }

        if ((!ReceivingLight || !AllPassThroughsHit) && Glowing)
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

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newBaseColor = Color.Lerp(startBaseColor, origBaseColor, t2);
            Color newTopColor = Color.Lerp(startTopColor, origTopColor, t2);

            crystalMaterial.SetColor("_BaseColor", newBaseColor);
            crystalMaterial.SetColor("_TopColor", newTopColor);

            yield return null;
        }

        crystalMaterial.SetColor("_BaseColor", origBaseColor);
        crystalMaterial.SetColor("_TopColor", origTopColor);

        if (AllPassThroughsHit)
        {
            ReceivedLight();
        }
        startGlowRoutine = null;
    }

    private IEnumerator StopGlowRoutine()
    {
        Glowing = false;
        Color startBaseColor = crystalMaterial.GetColor("_BaseColor");
        Color startTopColor = crystalMaterial.GetColor("_TopColor");

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newBaseColor = Color.Lerp(startBaseColor, depletedBaseColor, t2);
            Color newTopColor = Color.Lerp(startTopColor, depletedTopColor, t2);

            crystalMaterial.SetColor("_BaseColor", newBaseColor);
            crystalMaterial.SetColor("_TopColor", newTopColor);

            yield return null;
        }

        crystalMaterial.SetColor("_BaseColor", depletedBaseColor);
        crystalMaterial.SetColor("_TopColor", depletedTopColor);

        stopGlowRoutine = null;
    }

    public void ReceivedLight()
    {
        OnReceiveLight?.Invoke();
        Activated = true;
        foreach (var obj in destroyOnRecieve)
        {
            Destroy(obj);
        }
    }
}

[Serializable]
public class LightReceiverEvent : UnityEvent { }
