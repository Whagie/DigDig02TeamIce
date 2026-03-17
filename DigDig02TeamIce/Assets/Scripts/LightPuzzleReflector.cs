using System;
using System.Collections;
using UnityEngine;

public class LightPuzzleReflector : MonoBehaviour
{
    public GameObject Reflector;
    public Transform LightPos1;
    public Transform LightPos2;

    public TriggerRelay PlayerDetectionTrigger;

    [SerializeField] private float rotationDuration = 0.5f;
    [SerializeField] private LayerMask playerLayer;

    private bool inRadius = false;
    private bool allowForInputs = true;

    public GameObject Crystal;
    private Material crystalMaterial;
    public Color origBaseColor;
    public Color origTopColor;

    private Color depletedBaseColor = new Color32(51, 51, 128, 255);
    private Color depletedTopColor = new Color32(92, 113, 153, 255);

    public float StartGlowDuration = 0.4f;
    public float DropDuration = 0.75f;

    private int activeCrystalHits = 0;
    public bool ReceivingLight => activeCrystalHits > 0;

    public bool Glowing = false;

    private Player player;

    private Coroutine inputCooldownRoutine;
    private Coroutine startGlowRoutine;
    private Coroutine stopGlowRoutine;

    private void OnEnable()
    {
        PlayerDetectionTrigger.OnEnter += TriggerEnter;
        PlayerDetectionTrigger.OnExit += TriggerExit;
    }
    private void OnDisable()
    {
        PlayerDetectionTrigger.OnEnter -= TriggerEnter;
        PlayerDetectionTrigger.OnExit -= TriggerExit;
    }

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
        if (ReceivingLight && !Glowing)
        {
            StartGlow();
        }

        if (!ReceivingLight && Glowing)
        {
            StopGlow();
        }

        if (!inRadius)
            return;

        if (!allowForInputs)
            return;

        if (UserInput.RunePuzzleLeftPressed)
        {
            RotateReflector(-1);
        }
        else if (UserInput.RunePuzzleRightPressed)
        {
            RotateReflector(1);
        }
    }
    private void RotateReflector(int direction = 1)
    {
        if (direction != 1 && direction != -1)
            return;

        allowForInputs = false;
        StartCoroutine(RotateReflectorRoutine(direction));
    }

    private IEnumerator RotateReflectorRoutine(int direction)
    {
        float degrees = 45f * direction;

        Quaternion startRot = Reflector.transform.localRotation;
        Quaternion targetRot = Quaternion.AngleAxis(Reflector.transform.localEulerAngles.y + degrees, Vector3.up);

        float time = 0f;

        while (time < rotationDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / rotationDuration);

            Quaternion rotation = Quaternion.Lerp(startRot, targetRot, t);
            Reflector.transform.localRotation = rotation;

            yield return null;
        }

        Reflector.transform.localRotation = targetRot;

        if (inputCooldownRoutine != null)
            StopCoroutine(inputCooldownRoutine);
        inputCooldownRoutine = StartCoroutine(InputCooldown(0.05f));
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

    public void RegisterCrystalHit()
    {
        activeCrystalHits++;
    }

    public void UnregisterCrystalHit()
    {
        activeCrystalHits = Mathf.Max(0, activeCrystalHits - 1);
    }

    private IEnumerator InputCooldown(float duration)
    {
        allowForInputs = false;
        yield return new WaitForSeconds(duration);
        allowForInputs = true;
        inputCooldownRoutine = null;
    }

    public Transform GetExit(LightReflectorCrystal crystal)
    {
        if (crystal.IsPos1)
            return LightPos2;
        else
            return LightPos1;
    }

    private void TriggerEnter(Collider other)
    {
        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;
            inRadius = true;
        }
    }

    private void TriggerExit(Collider other)
    {
        Player p = other.GetComponentInParent<Player>();

        if (p != null && p == player)
        {
            inRadius = false;
        }
    }

    //private void OnDrawGizmosSelected()
    //{
    //    if (PlayerDetectionTrigger == null)
    //        return;

    //    Gizmos.color = Color.cyan;
    //    if (inRadius)
    //    {
    //        Gizmos.color = Color.red;
    //    }
    //    Gizmos.DrawWireSphere(PlayerDetectionTrigger.gameObject.transform.position, PlayerDetectionTrigger.SphereColliderRadius);
    //}
}