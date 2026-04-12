using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LostCastleRat : MonoBehaviour
{
    private Player player;
    public Transform UIPos;
    public Image InteractBubble;
    public Image LostOrbBubble;
    public Image FoundOrbBubble;
    public Transform cam;

    public float IdleBobSpeed = 1f;
    public float IdleBobHeight = 0.5f;
    public float TalkingBobSpeed = 2f;
    public float TalkingBobHeight = 0.15f;
    private float bobSpeed;
    private float bobHeight;

    private Vector3 startPos;
    private Coroutine currentFade;

    public bool Activated = true;

    public static bool FoundOrb = false;

    private void Start()
    {
        cam = Camera.main.transform;
        startPos = UIPos.localPosition;

        InteractBubble.color = new Color(1f, 1f, 1f, 1f);
        LostOrbBubble.color = new Color(1f, 1f, 1f, 0f);
        FoundOrbBubble.color = new Color(1f, 1f, 1f, 0f);

        bobHeight = IdleBobHeight;
        bobSpeed = IdleBobSpeed;
    }

    void Update()
    {
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
        if (!Activated || player != null)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null)
        {
            player = p;

            bobHeight = TalkingBobHeight;
            bobSpeed = TalkingBobSpeed;

            if (player.Companion.isCarrying || FoundOrb)
            {
                FadeTo(InteractBubble, FoundOrbBubble, 0.25f);
                FoundOrb = true;
            }
            else
            {
                FadeTo(InteractBubble, LostOrbBubble, 0.25f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Activated)
            return;

        Player p = other.GetComponentInParent<Player>();

        if (p != null && p == player)
        {
            if (LostOrbBubble.color.a > 0f)
            {
                FadeTo(LostOrbBubble, InteractBubble, 0.25f);
            }
            else if (FoundOrbBubble.color.a > 0f)
            {
                FadeTo(FoundOrbBubble, InteractBubble, 0.25f);
            }

            bobSpeed = IdleBobSpeed;
            bobHeight = IdleBobHeight;

            player = null;
        }
    }

    private void FadeTo(Image from, Image to, float duration)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeRoutine(from, to, duration));
    }

    IEnumerator FadeRoutine(Image from, Image to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            float a = t / duration;

            // Always force white RGB
            from.color = new Color(1f, 1f, 1f, 1f - a);
            to.color = new Color(1f, 1f, 1f, a);

            t += Time.deltaTime;
            yield return null;
        }

        // Final values
        from.color = new Color(1f, 1f, 1f, 0f);
        to.color = new Color(1f, 1f, 1f, 1f);
    }
}
