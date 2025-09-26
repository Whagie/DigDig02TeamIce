using UnityEngine;
using System.Collections;

public class CameraActions : MonoBehaviour
{
    public static CameraActions Main { get; private set; }
    private Camera cam;

    private Coroutine punchRoutine;
    private Coroutine shakeRoutine;

    private float defaultFOV;
    private Quaternion defaultRotation;

    public static AnimationCurve hitCurve = new AnimationCurve(
    new Keyframe(0f, 0f),       // start
    new Keyframe(0.1f, 1f),     // sharp impact early
    new Keyframe(1f, 0f)        // settle back
    );

    void Awake()
    {
        Main = this;                          // register this as the main
        cam = GetComponent<Camera>();         // get the Camera on same object
        if (cam == null) cam = Camera.main;   // fallback if needed

        defaultFOV = cam.fieldOfView;
        defaultRotation = cam.transform.localRotation;
    }

    /// <summary>
    /// Shake the camera with optional FOV and tilt effects.
    /// </summary>
    /// <param name="duration">How long the shake lasts</param>
    /// <param name="fovIntensity">Max FOV offset during shake</param>
    /// <param name="tiltIntensity">Max tilt offset in degrees</param>
    /// <param name="curve">Animation curve controlling shake over time (0-1 normalized)</param>
    public void Shake(float duration, float fovIntensity = 0f, float tiltIntensity = 0f, AnimationCurve curve = null)
    {
        if (curve == null)
            curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, fovIntensity, tiltIntensity, curve));
    }

    private IEnumerator ShakeRoutine(float duration, float fovIntensity, float tiltIntensity, AnimationCurve curve)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // Evaluate shake amount via curve (0-1)
            float eval = curve.Evaluate(t);

            // Random FOV shake
            if (fovIntensity != 0f)
                cam.fieldOfView = defaultFOV + Random.Range(-fovIntensity, fovIntensity) * eval;

            // Random tilt shake
            if (tiltIntensity != 0f)
            {
                float tiltX = Random.Range(-tiltIntensity, tiltIntensity) * eval;
                float tiltY = Random.Range(-tiltIntensity, tiltIntensity) * eval;
                cam.transform.localRotation = defaultRotation * Quaternion.Euler(tiltX, tiltY, 0f);
            }

            yield return null;
        }

        // Reset camera
        cam.fieldOfView = defaultFOV;
        cam.transform.localRotation = defaultRotation;
        shakeRoutine = null;
    }

    public void Punch(float zoomAmount, float duration, AnimationCurve curve = null)
    {
        if (curve == null) curve = hitCurve;

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);

        punchRoutine = StartCoroutine(PunchRoutine(zoomAmount, duration, curve));
    }

    private IEnumerator PunchRoutine(float zoomAmount, float duration, AnimationCurve curve)
    {
        float startFOV = cam.fieldOfView;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float eval = curve.Evaluate(t);        // goes 0 -> 1
            cam.fieldOfView = startFOV + eval * zoomAmount;
            yield return null;
        }

        cam.fieldOfView = startFOV; // reset
        punchRoutine = null;
    }
}
