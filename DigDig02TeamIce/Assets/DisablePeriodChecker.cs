using UnityEngine;

public class DisablePeriodChecker : MonoBehaviour
{
    [SerializeField] private Component target;

    [Tooltip("If true, tracks how long the component stays ENABLED instead of disabled.")]
    [SerializeField] private bool trackEnabledPeriod = false;

    private bool wasEnabled;
    private int frameCount;
    private float timeCount;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError($"{nameof(DisablePeriodChecker)}: Target is null.");
            enabled = false;
            return;
        }

        wasEnabled = IsEnabled(target);
        frameCount = 0;
        timeCount = 0f;
    }

    private void Update()
    {
        bool isEnabled = IsEnabled(target);

        bool trackingState = trackEnabledPeriod ? isEnabled : !isEnabled;
        bool wasTrackingState = trackEnabledPeriod ? wasEnabled : !wasEnabled;

        // Transition into tracking state
        if (!wasTrackingState && trackingState)
        {
            frameCount = 0;
            timeCount = 0f;
        }

        // Count frames/time while tracking
        if (trackingState)
        {
            frameCount++;
            timeCount += Time.deltaTime;
        }

        // Transition out of tracking state -> log
        if (wasTrackingState && !trackingState)
        {
            string stateName = trackEnabledPeriod ? "enabled" : "disabled";

            Debug.Log(
                $"{target.name} was {stateName} for " +
                $"{frameCount} frames " +
                $"({timeCount:F4} seconds)"
            );

            frameCount = 0;
            timeCount = 0f;
        }

        wasEnabled = isEnabled;
    }

    private static bool IsEnabled(Component c)
    {
        return c switch
        {
            Behaviour b => b.enabled,
            Collider col => col.enabled,
            Renderer r => r.enabled,
            _ => true
        };
    }
}
