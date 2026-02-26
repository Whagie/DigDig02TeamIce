using System.Linq;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystemForceField))]
public class CustomWindZone : MonoBehaviour
{
    public enum Mode { Directional, Spherical }
    public Mode mode = Mode.Directional;

    [Header("Wind Settings")]
    [Tooltip("Base constant force of the wind.")]
    public float mainForce = 1f;

    [Tooltip("Pulse (gust) magnitude multiplier added on top of mainForce.")]
    public float pulseMagnitude = 0.5f;

    [Tooltip("Pulse interval (seconds per cycle). How often gusts occur on average.")]
    public float pulseInterval = 5f;

    [Tooltip("How long each pulse lasts (seconds).")]
    public float pulseLength = 3f;

    [Range(0, 10), Tooltip("Randomness in gust strength. 0 = fixed, 10 = very random.")]
    public float pulseMagnitudeRandomness = 0f;

    [Range(0, 10), Tooltip("Randomness in pulse frequency (interval). 0 = exact frequency, 10 = highly random).")]
    public float pulseFrequencyRandomness = 0f;

    [Range(0, 10), Tooltip("Randomness in gust length. 0 = fixed length, 10 = very random.")]
    public float pulseLengthRandomness = 0f;

    [Tooltip("Time to fade in a gust (seconds).")]
    public float pulseRampUp = 0.5f;

    [Tooltip("Time to fade out a gust (seconds).")]
    public float pulseFadeOut = 1.0f;

    [Header("Turbulence")]
    public float turbulence = 0.5f;
    public float turbulenceTimeScale = 0.2f;
    public float turbulenceNoiseScale = 0.5f;

    [Header("ForceField shape (Spherical mode only)")]
    public float sphereRadius = 5f;

    [Header("Directional mode helper")]
    public float directionalRadius = 2f;
    public float directionalLength = 10f;

    [Header("Debug / Gizmo")]
    public Color gizmoColor = Color.cyan;
    public bool drawGizmos = true;

    ParticleSystemForceField psForceField;

    // Gust state
    float gustTimer = 0f;
    float gustEndTime = 0f;
    float gustStartTime = 0f;
    float currentGustStrength = 0f;

    /// <summary>
    /// Total length of the upcoming gust of wind (in seconds)
    /// </summary>
    public float TotalGustLength { get; private set; }

    [field: SerializeField]
    public bool Main { get; private set; } = false;
    private bool prevToggle;


    public System.Action OnWindGustStart;
    public System.Action OnWindGustFade;
    public System.Action OnWindGustEnd;

    void Awake()
    {
        EnsureForceFieldExists();
    }

    void OnValidate()
    {
        EnsureForceFieldExists();
        UpdateForceField(true);

        if (Main != prevToggle)
        {
            prevToggle = Main;
            if (Main == true)
            {
                OnToggleChanged();
            }
        }
    }

    void Update()
    {
        UpdateGusts();
        UpdateForceField(false);
    }

    void UpdateGusts()
    {
        float now = Time.time;

        // If gust is active
        if (now < gustEndTime)
            return; // gust continues

        // Gust has ended
        if (gustEndTime > 0f && now >= gustEndTime)
        {
            OnWindGustEnd?.Invoke();
            TotalGustLength = 0f;
            gustEndTime = 0f; // reset to prevent multiple invocations
        }

        // Otherwise, maybe trigger a new gust
        if (gustTimer <= 0f)
        {
            // Start new gust
            float magRand = Mathf.Lerp(1f, Random.Range(0.5f, 1.5f), pulseMagnitudeRandomness / 10f);
            currentGustStrength = pulseMagnitude * magRand;

            float lenRand = Mathf.Lerp(1f, Random.Range(0.5f, 1.5f), pulseLengthRandomness / 10f);
            float gustDuration = Mathf.Max(0.1f, pulseLength * lenRand);

            TotalGustLength = gustDuration;

            gustStartTime = now;
            gustEndTime = now + gustDuration;

            // Reset timer until next gust
            float randFactor = Mathf.Lerp(1f, Random.Range(0.5f, 1.5f), pulseFrequencyRandomness / 10f);
            gustTimer = pulseInterval * randFactor;

            OnWindGustStart?.Invoke();
        }
        else
        {
            gustTimer -= Time.deltaTime;
        }
    }

    float GetGustEnvelope(float now)
    {
        if (now < gustStartTime || now > gustEndTime)
            return 0f;

        float elapsed = now - gustStartTime;
        float duration = gustEndTime - gustStartTime;

        // ramp up
        if (elapsed < pulseRampUp)
            return Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, pulseRampUp));

        // sustain -> start of fade
        if (elapsed >= duration - pulseFadeOut && elapsed < duration - pulseFadeOut + Time.deltaTime)
        {
            // Fire fade start event (once)
            OnWindGustFade?.Invoke();
        }

        // sustain (middle of gust)
        if (elapsed < duration - pulseFadeOut)
            return 1f;

        // fade out
        float fadeElapsed = elapsed - (duration - pulseFadeOut);
        return Mathf.Clamp01(1f - fadeElapsed / Mathf.Max(0.0001f, pulseFadeOut));
    }

    void EnsureForceFieldExists()
    {
        if (psForceField == null)
        {
            psForceField = GetComponent<ParticleSystemForceField>();
            if (psForceField == null)
                psForceField = gameObject.AddComponent<ParticleSystemForceField>();
        }
    }

    void UpdateForceField(bool forceImmediate)
    {
        if (psForceField == null) return;

        // base direction is local +Y
        Vector3 baseDir = (transform.rotation * Vector3.up).normalized;

        // current gust factor with envelope
        float envelope = GetGustEnvelope(Time.time);
        float gustFactor = 1f + currentGustStrength * envelope;

        // turbulence
        Vector3 worldPos = transform.position;
        float t = Time.time * turbulenceTimeScale;
        float nx = (Mathf.PerlinNoise(worldPos.z * turbulenceNoiseScale + t, worldPos.y * turbulenceNoiseScale) - 0.5f) * 2f;
        float nz = (Mathf.PerlinNoise(worldPos.x * turbulenceNoiseScale, worldPos.y * turbulenceNoiseScale + t) - 0.5f) * 2f;

        Vector3 up = transform.up;
        Vector3 right = Vector3.Cross(up, baseDir).normalized;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(Vector3.up, baseDir).normalized;
        Vector3 forward = Vector3.Cross(baseDir, right).normalized;

        Vector3 noiseVec = (right * nx + forward * nz) * turbulence;

        // final wind vector
        Vector3 windVec = (mainForce * gustFactor * baseDir) + noiseVec;

        Vector3 localWind = transform.InverseTransformDirection(windVec);
        psForceField.directionX = localWind.x;
        psForceField.directionY = localWind.y;
        psForceField.directionZ = localWind.z;

        if (mode == Mode.Spherical)
        {
            psForceField.shape = ParticleSystemForceFieldShape.Sphere;
            psForceField.startRange = 0f;
            psForceField.endRange = Mathf.Max(0.001f, sphereRadius);
        }
        else
        {
            psForceField.shape = ParticleSystemForceFieldShape.Cylinder;
            psForceField.startRange = 0f;
            psForceField.endRange = Mathf.Max(0.001f, directionalRadius);
            psForceField.length = Mathf.Max(0.001f, directionalLength);
        }

#if UNITY_EDITOR
        if (forceImmediate)
            UnityEditor.EditorUtility.SetDirty(psForceField);
#endif
    }

    private void OnToggleChanged()
    {
        var winds = EditorObjectFinder.FindWindObjectsInEditor(this);
        if (winds.Count >= 1)
        {
            Main = false;
            prevToggle = false;
            if (winds.Count > 1)
            {
                Debug.LogError($"Somehow found {winds.Count} main wind objects! Listing below...");
                foreach (var w in winds)
                {
                    Debug.LogWarning($" - {w.name}", w);
                }
            }
            Debug.LogWarning($"Wind object {winds[0].name} is already marked as Main!", winds[0]);
        }
        else
        {
            //Debug.Log($"Marked {this.name} as Main wind");
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = gizmoColor;

        // Determine start position
        Vector3 windDir = transform.rotation * Vector3.up;
        Vector3 startPos;

        if (mode == Mode.Directional)
        {
            // start at bottom of the cylinder
            startPos = transform.position - windDir.normalized * (directionalLength * 0.5f);
        }
        else
        {
            // spherical: start at center
            startPos = transform.position;
        }

        float displayRadius = (mode == Mode.Spherical) ? sphereRadius : directionalRadius;
        float arrowLength = displayRadius * 0.5f;

        // draw main arrow line
        Gizmos.DrawLine(startPos, startPos + windDir.normalized * arrowLength);

        // draw arrowhead
        Vector3 head = startPos + windDir.normalized * arrowLength;
        Vector3 r = Quaternion.LookRotation(windDir) * Quaternion.Euler(0, 150f, 0) * Vector3.forward;
        Vector3 l = Quaternion.LookRotation(windDir) * Quaternion.Euler(0, -150f, 0) * Vector3.forward;
        Gizmos.DrawLine(head, head + r * (displayRadius * 0.15f));
        Gizmos.DrawLine(head, head + l * (displayRadius * 0.15f));
    }
}
