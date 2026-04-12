using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class NoteBook : MonoBehaviour
{
    public bool Activated;
    public bool ShowInteractBubble = true;

    public DialogueData NoteText;

    public Transform UIPos;
    public CanvasGroup InteractBubble;
    public CanvasGroup InputBubble;

    public float IdleBobSpeed = 1f;
    public float IdleBobHeight = 0.5f;
    public float TalkingBobSpeed = 2f;
    public float TalkingBobHeight = 0.15f;

    private float bobSpeed;
    private float bobHeight;

    private Player player;
    private Transform cam;
    private Vector3 startPos;

    private Coroutine currentFade;

    private void Start()
    {
        cam = Camera.main.transform;
        startPos = UIPos.localPosition;

        if (NoteText == null)
        {
            NoteText = GetComponent<DialogueData>();
        }

        InteractBubble.alpha = 1f;
        InputBubble.alpha = 0f;

        if (!ShowInteractBubble)
        {
            InteractBubble.alpha = 0f;
        }

        bobHeight = IdleBobHeight;
        bobSpeed = IdleBobSpeed;

        if (Activated)
        {
            InteractBubble.alpha = 0f;
            InputBubble.alpha = 0f;
        }
    }

    private void Update()
    {
        if (Activated)
            return;

        if (player != null)
        {
            if (UserInput.InteractPressed)
            {
                Activated = true;
                MenuManager.instance.ApplyDialogue(NoteText);
                StartCoroutine(NoteRoutine());
            }
        }

        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        UIPos.localPosition = startPos + new Vector3(0f, yOffset, 0f);
    }

    private void LateUpdate()
    {
        if (Activated)
            return;

        if (cam != null)
        {
            UIPos.LookAt(cam);
        }
    }

    private IEnumerator NoteRoutine()
    {
        player.MovementOverride = true;

        player.animator.SetFloat("MoveX", 0f);
        player.animator.SetFloat("MoveZ", 0f);
        player.animator.SetFloat("Move", 0f);

        FadeTo(InputBubble, InteractBubble, 0.25f, true);

        MenuManager.instance.FadeGroup(MenuManager.instance.NoteGroup, 1f, 1.25f);

        yield return new WaitForSeconds(1f);

        bool pressedEscape = false;

        while (!pressedEscape)
        {
            if (UserInput.InteractPressed)
            {
                pressedEscape = true;
            }

            yield return null;
        }

        MenuManager.instance.FadeGroup(MenuManager.instance.NoteGroup, 0f, 0.75f);

        yield return new WaitForSeconds(0.75f);

        FadeTo(InteractBubble, InputBubble, 0.5f);

        yield return new WaitForSeconds(0.5f);

        player.MovementOverride = false;
        Activated = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Activated || player != null)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;

            bobHeight = TalkingBobHeight;
            bobSpeed = TalkingBobSpeed;

            FadeTo(InteractBubble, InputBubble, 0.25f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Activated)
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
