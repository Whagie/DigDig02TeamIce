using Dreamteck.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minecart : MonoBehaviour
{
    public SplineFollower splineFollower;
    public MinecartLift subscribedLift;
    public bool travelingToLift = false;
    public bool nextUp = false;
    [SerializeField] private GameObject crystals;
    public TriggerRelay frontCollider;

    private void OnEnable()
    {
        frontCollider.OnEnter += FrontCollider_OnEnter;
        frontCollider.OnExit += FrontCollider_OnExit;
    }
    private void OnDisable()
    {
        frontCollider.OnEnter -= FrontCollider_OnEnter;
        frontCollider.OnExit -= FrontCollider_OnExit;
    }

    private void FrontCollider_OnExit(Collider other)
    {
        if (other.CompareTag("MinecartCollision"))
        {
            Minecart cart = other.GetComponentInParent<Minecart>();
            if (cart.subscribedLift != null)
            {
                if (!nextUp)
                {
                    StartCoroutine(WaitBeforeMove(0.5f * (cart.subscribedLift.cartQueue.IndexOf(this) + 1), splineFollower, 5f));
                }
            }
            else
            {
                // If they collide not related to the lift
                StartCoroutine(MoveUpTo(5f, 1.5f, v => splineFollower.followSpeed = v));
            }
        }
    }

    private void FrontCollider_OnEnter(Collider other)
    {
        if (other.CompareTag("MinecartCollision"))
        {
            Minecart cart = other.GetComponentInParent<Minecart>();

            splineFollower.followSpeed = 0f;
            if (cart.subscribedLift != null)
            {
                this.subscribedLift = cart.subscribedLift;

                subscribedLift.ReceiveCart(cart.splineFollower, 0.3f, false);
                if (!subscribedLift.cartQueue.Contains(this))
                {
                    subscribedLift.cartQueue.Add(this);
                }
            }
        }
    }

    public void RemoveCrystals()
    {
        crystals.SetActive(false);
    }
    public void RestoreCrystals()
    {
        crystals.SetActive(true);
    }

    IEnumerator MoveUpTo(float targetValue, float duration, Action<float> onValueChanged)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = t * t * (3f - 2f * t); // smooth easing in/out
            float value = Mathf.Lerp(0f, targetValue, smoothT);
            onValueChanged(value);
            yield return null;
        }
        onValueChanged(targetValue); // ensure exact final value
    }

    private IEnumerator WaitBeforeMove(float time, SplineFollower follower, float amount)
    {
        yield return new WaitForSeconds(time);
        follower.followSpeed = amount;
    }
}
