using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OrbKeyReceiver : MonoBehaviourID
{
    public Transform OrbKeyPosition;
    public GameObject HiddenOrb;

    public OnReceiveKeyEvent OnReceiveKey;

    public float DurationToLockPlayerMovement = 2.3f;

    public bool Activated = false;

    private Player player;
    private Player player2;
    private bool companionIsCarrying = false;

    public Transform UIPos;
    public CanvasGroup InteractBubble;
    public CanvasGroup InputBubble;
    public Transform cam;

    public float IdleBobSpeed = 1f;
    public float IdleBobHeight = 0.5f;
    public float TalkingBobSpeed = 2f;
    public float TalkingBobHeight = 0.15f;
    private float bobSpeed;
    private float bobHeight;

    private Vector3 startPos;

    private Coroutine currentFade;

    public GameObject Receiver;
    private Material glowMaterial;
    public Color origGlowColor;
    private Color depletedGlowColor;
    public float StartGlowDuration = 0.4f;

    private Coroutine receiveKeyRoutine;

    private SessionSaveData.SingleBoolData receivedOrbData;

    private void Start()
    {
        Renderer renderer1 = Receiver.GetComponent<Renderer>();
        Material[] mats1 = renderer1.materials;
        int matIndex1 = Array.FindIndex(mats1, m => m.name.Contains("Glow"));
        glowMaterial = mats1[matIndex1];
        glowMaterial.EnableKeyword("_EMISSION");

        origGlowColor = glowMaterial.GetColor("_EmissionColor");
        origGlowColor *= 1.5f;
        depletedGlowColor = origGlowColor * 0.125f;

        glowMaterial.SetColor("_EmissionColor", depletedGlowColor);

        HiddenOrb.SetActive(false);

        if (SessionSaveData.Instance.TryGet(ID, out receivedOrbData))
        {
            Activated = receivedOrbData.IsTrue;
        }
        else
        {
            SessionSaveData.Instance.AddOrUpdateData(ID, Activated);
        }

        if (Activated)
        {
            glowMaterial.SetColor("_EmissionColor", origGlowColor);
            HiddenOrb.SetActive(true);
        }

        cam = Camera.main.transform;
        startPos = UIPos.localPosition;

        InteractBubble.alpha = 0f;
        InputBubble.alpha = 0f;

        bobHeight = IdleBobHeight;
        bobSpeed = IdleBobSpeed;

        player2 = GameObject.FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (Activated)
            return;

        if (player2 != null)
        {
            if (player2.Companion.isCarrying && !companionIsCarrying)
            {
                companionIsCarrying = true;
                FadeTo(InputBubble, InteractBubble, 0.25f);
            }
            else if (!player2.Companion.isCarrying && companionIsCarrying)
            {
                companionIsCarrying = false;
                FadeTo(InputBubble, InteractBubble, 0.25f, true);
            }
        }

        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        UIPos.localPosition = startPos + new Vector3(0f, yOffset, 0f);
    }

    private void LateUpdate()
    {
        if (cam != null)
        {
            UIPos.LookAt(cam);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Activated || player != null || !companionIsCarrying)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;
            player.CurrentOrbKeyReceiver = this;

            if (player.Companion.isCarrying && player.CurrentOrbKey != null)
            {
                bobHeight = TalkingBobHeight;
                bobSpeed = TalkingBobSpeed;

                FadeTo(InteractBubble, InputBubble, 0.25f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Activated || !companionIsCarrying)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null && p == player)
        {
            player.CurrentOrbKeyReceiver = null;

            bobSpeed = IdleBobSpeed;
            bobHeight = IdleBobHeight;

            FadeTo(InputBubble, InteractBubble, 0.25f);

            player = null;
        }
    }

    public void ReceiveKey(Transform key)
    {
        if (Activated)
            return;

        if (receiveKeyRoutine != null)
            StopCoroutine(receiveKeyRoutine);

        receiveKeyRoutine = StartCoroutine(ReceiveKeyRoutine(key));

        FadeTo(InputBubble, InteractBubble, 0.25f, true);
    }

    private IEnumerator ReceiveKeyRoutine(Transform key)
    {
        Activated = true;
        SessionSaveData.Instance.AddOrUpdateData(ID, Activated);
        key.gameObject.GetComponent<OrbKey>().Activated = true;

        yield return StartCoroutine(StartGlowRoutine());

        OnReceiveKey?.Invoke();
        if (player != null)
        {
            player.CurrentOrbKey = null;
            player.LockMovement(DurationToLockPlayerMovement);
        }
    }

    private IEnumerator StartGlowRoutine()
    {
        Color startGlowColor = glowMaterial.GetColor("_EmissionColor");

        float time = 0f;
        while (time < StartGlowDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / StartGlowDuration);
            float t2 = t * t * t;

            Color newGlowColor = Color.Lerp(startGlowColor, origGlowColor, t2);

            glowMaterial.SetColor("_EmissionColor", newGlowColor);

            yield return null;
        }

        glowMaterial.SetColor("_EmissionColor", origGlowColor);

        receiveKeyRoutine = null;
    }

    public void FadeTo(CanvasGroup from, CanvasGroup to, float duration, bool bothToZero = false)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeRoutine(from, to, duration, bothToZero));
    }

    IEnumerator FadeRoutine(CanvasGroup from, CanvasGroup to, float duration, bool bothToZero = false)
    {
        float t = 0f;

        float fromStart = from.alpha;
        float toStart = to.alpha;

        while (t < duration)
        {
            float a = t / duration;

            if (!bothToZero)
            {
                to.alpha = Mathf.Lerp(toStart, 1f, a);
            }
            from.alpha = Mathf.Lerp(fromStart, 0f, a);

            t += Time.deltaTime;
            yield return null;
        }

        // Final values
        from.alpha = 0f;
        if (!bothToZero)
        {
            to.alpha = 1f;
        }
        else
        {
            to.alpha = 0f;
        }
    }
}

[Serializable]
public class OnReceiveKeyEvent : UnityEvent { }
