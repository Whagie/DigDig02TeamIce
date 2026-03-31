using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OrbKey : MonoBehaviourID
{
    public bool Activated;
    public bool ShowInteractBubble = true;
    public bool IsCarried;
    private Player player;

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

    private SessionSaveData.SingleBoolData receivedOrbData;

    private void Start()
    {
        cam = Camera.main.transform;
        startPos = UIPos.localPosition;

        InteractBubble.alpha = 1f;
        InputBubble.alpha = 0f;

        if (!ShowInteractBubble)
        {
            InteractBubble.alpha = 0f;
        }

        bobHeight = IdleBobHeight;
        bobSpeed = IdleBobSpeed;

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
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Activated)
            return;

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
        if (Activated || player != null || IsCarried)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;
            player.CurrentOrbKey = this;

            bobHeight = TalkingBobHeight;
            bobSpeed = TalkingBobSpeed;

            FadeTo(InteractBubble, InputBubble, 0.25f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Activated || IsCarried)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null && p == player)
        {
            player.CurrentOrbKey = null;

            bobSpeed = IdleBobSpeed;
            bobHeight = IdleBobHeight;

            FadeTo(InputBubble, InteractBubble, 0.25f);

            player = null;
        }
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

    public void FadeInInteractBubble()
    {
        FadeTo(InputBubble, InteractBubble, 0.25f);
    }
}
