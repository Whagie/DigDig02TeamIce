using UnityEngine;
using Dreamteck.Splines;

[RequireComponent(typeof(SplineFollower))]
public class SplineFollowerPointLock : MonoBehaviour
{
    private float previousLength;
    private SplineFollower follower;
    public bool active = false;

    void Awake()
    {
        follower = GetComponent<SplineFollower>();
        previousLength = follower.spline.CalculateLength();
    }

    void LateUpdate()
    {
        if (!active) return;

        float currentLength = follower.spline.CalculateLength();
        float delta = currentLength - previousLength;

        // Convert the delta into a percent of the spline
        double deltaPercent = delta / currentLength;

        // Nudge the follower to compensate
        double newPercent = follower.result.percent + deltaPercent * -0.4f;

        // Clamp between 0-1
        newPercent = Mathf.Clamp01((float)newPercent);

        // Apply
        follower.SetPercent(newPercent);

        // Update previous length for next frame
        previousLength = currentLength;
    }

    public void Activate()
    {
        active = true;
        previousLength = follower.spline.CalculateLength(); // initialize baseline
    }

    public void Deactivate()
    {
        active = false;
    }
}
