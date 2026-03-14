using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement Instance;
    private Player player;
    private Transform target;

    public Camera _camera;
    [HideInInspector] public AudioListener audioListener;
    private float cameraStartDistanceZ;
    private float cameraStartRotationX;
    private float cameraStartPositionY;

    [Header("Follow")]
    public float maxDistance = 5f;
    public float moveSmoothSpeed = 2f;
    public float recenterSmoothSpeed = 4f;

    private float rotationX; // signed -180..180
    private float rotationY; // signed -180..180
    private float startX;    // signed start pitch, used as center for clamp

    private Vector3 velocity = Vector3.zero;
    private bool recentering = false;

    public bool followY = false;
    public bool StaticCamera = false;

    private bool playingDeathAnim = false;
    public float AmountThroughDeathAnim { get; private set; }
    public event System.Action OnResurrectionRespawn;

    private Quaternion originalRotation;
    private Vector3 originalParentLocalPos;
    private Vector3 originalChildLocalPos;
    private float originalFOV;

    private float? targetZoomDistance = null;
    private float? prevDistance;
    float zoomT = 0f;
    float zoomDuration = 0.4f;
    float zoomStart;
    private bool zoomingBack = false;

    public CameraActions Actions { get; private set; }

    private void OnEnable()
    {
        if (player != null)
        {
            player.OnPlayerDie += OnPlayerDie;
            //player.OnPlayerResurrect += OnPlayerResurrect;
        }
    }
    private void OnDisable()
    {
        if (player != null)
        {
            player.OnPlayerDie -= OnPlayerDie;
            //player.OnPlayerResurrect -= OnPlayerResurrect;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        Actions = _camera.GetComponent<CameraActions>();

        audioListener = _camera.GetComponent<AudioListener>();
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            audioListener.enabled = false;
        }
        else
        {
            audioListener.enabled = true;
        }
    }
    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();
        if (player != null)
        {
            if (player._camera == null)
            {
                player._camera = this;
            }
        }

        player.OnPlayerDie += OnPlayerDie;

        Vector3 euler = transform.localEulerAngles;
        startX = NormalizeAngle(euler.x);
        rotationX = startX;
        rotationY = NormalizeAngle(euler.y);

        cameraStartDistanceZ = _camera.transform.localPosition.z;
        cameraStartRotationX = _camera.transform.rotation.x;
        cameraStartPositionY = _camera.transform.localPosition.y;
        originalFOV = _camera.GetComponent<Camera>().fieldOfView;

        prevDistance = _camera.transform.localPosition.z;

        transform.position = player.transform.position;

        GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
        if (spawnPoint != null)
        {
            float startXAngle = transform.localEulerAngles.x;

            transform.position = spawnPoint.transform.position;
            transform.rotation = Quaternion.Euler(
                startXAngle,
                spawnPoint.transform.eulerAngles.y,
                0f
            );
        }
    }

    void LateUpdate()
    {
        if (StaticCamera) return;
        if (!player) target = null;
        else target = player.transform;
        
        if (!target)
        {
            GameObject obj = new GameObject();
            obj.transform.position = transform.position;
            target = obj.transform;
        }

        if (playingDeathAnim)
        {
            return;
        }

        // === Follow logic ===
        Vector3 desiredPosition;
        if (followY)
        {
            if (!player.Jumping)
            {
                desiredPosition = target.position;
            }
            else
            {
                desiredPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
            }
        }
        else
        {
            desiredPosition = new Vector3(target.position.x, 0f, target.position.z);
        }
        Vector3 offset = desiredPosition - transform.position;
        float distance = offset.magnitude;

        if (distance > maxDistance) recentering = true;
        if (distance < 0.1f) recentering = false;

        if (recentering)
        {
            float smooth = (distance > maxDistance) ? moveSmoothSpeed : recenterSmoothSpeed;
            Vector3 newPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / smooth);

            transform.position = newPosition;
        }

        if (targetZoomDistance.HasValue)
        {
            Vector3 camPos = _camera.transform.localPosition;

            zoomT += Time.deltaTime / zoomDuration;
            zoomT = Mathf.Clamp01(zoomT);

            float t = Mathf.SmoothStep(0f, 1f, zoomT);

            float posZ = Mathf.Lerp(zoomStart, targetZoomDistance.Value, t);
            _camera.transform.localPosition = new Vector3(camPos.x, camPos.y, posZ);

            if (zoomT >= 1f)
            {
                _camera.transform.localPosition =
                    new Vector3(camPos.x, camPos.y, targetZoomDistance.Value);

                targetZoomDistance = null;
                if (zoomingBack)
                    prevDistance = null;
            }
        }
    }

    // Normalize angle 0..360 -> -180..180 and keep numbers small for stable math
    float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    float MapToRange(float x, float startX, float fullRange)
    {
        if (fullRange == 0f) return 0f; // avoid divide-by-zero
        return (x - startX) / (fullRange * 0.5f);
    }

    private void OnPlayerDie()
    {
        if (!playingDeathAnim)
        {
            CacheOriginalState();
            StartCoroutine(DeathCam());
            playingDeathAnim = true;
        }
    }
    public void PlayerResurrect()
    {
        if (playingDeathAnim)
        {
            StopAllCoroutines();
            StartCoroutine(ReturnCam(2f, true));
        }
    }

    private IEnumerator DeathCam()
    {
        _camera.GetComponent<Camera>().fieldOfView = originalFOV;

        float delay = 0.5f;
        float totalTime = player.animator.runtimeAnimatorController.animationClips.Where(c => c.name == "Die_Weapon").FirstOrDefault().length;
        float constructAnimTotalTime = player.Companion._animator.runtimeAnimatorController.animationClips.Where(c => c.name == "DeathRest").FirstOrDefault().length;

        float xAngle = 15f;
        float yOffset = 135f;

        float childMoveZ = 50f;
        float childMoveY = -1f;

        yield return new WaitForSecondsRealtime(delay);

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.AngleAxis(player.transform.eulerAngles.y + yOffset, Vector3.up) * Quaternion.AngleAxis(xAngle, Vector3.right);

        Transform child = transform.GetChild(0);
        Vector3 childStart = child.localPosition;
        Vector3 childTarget = childStart + new Vector3(0f, childMoveY, childMoveZ);

        Vector3 parentStartPos = transform.localPosition;
        Vector3 parentTargetPos = player.transform.localPosition;

        Vector3 offset = new Vector3(2f, 3f, -2f);

        Vector3 constructStartPos = player.Companion.transform.position;
        Vector3 constructTargetPos = player.transform.TransformPoint(offset);

        Quaternion constructStartRot = player.Companion.transform.rotation;
        Quaternion constructTargetRot = player.transform.rotation * Quaternion.Euler(0f, 180f, 0f);

        Vector3 playerPos = player.transform.position;
        playerPos.y = 0f;

        Vector3 startOffset = constructStartPos - playerPos;
        Vector3 targetOffset = constructTargetPos - playerPos;

        startOffset.y = 0f;
        targetOffset.y = 0f;

        float startAngle = Mathf.Atan2(startOffset.z, startOffset.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(targetOffset.z, targetOffset.x) * Mathf.Rad2Deg;

        float clockwiseDelta = Mathf.Repeat(startAngle - targetAngle, 360f);
        if (clockwiseDelta < 45f)
            clockwiseDelta += 360f;

        bool constructStartedAnim = false;

        float timer = 0f;
        float constructAnimTimer = 0f;

        while (timer < totalTime)
        {
            timer += Time.unscaledDeltaTime;
            constructAnimTimer += Time.unscaledDeltaTime;

            AmountThroughDeathAnim = Mathf.Clamp01(timer / totalTime);
            float amountThroughConstructAnim = Mathf.Clamp01(constructAnimTimer / constructAnimTotalTime);

            float t = Mathf.SmoothStep(0f, 1f, AmountThroughDeathAnim);
            float t2 = Mathf.Clamp01(t / 0.4f);
            float t3 = Mathf.Clamp01((t - 0.92f) / 0.06f);
            float t4 = Mathf.Clamp01(t / 0.8f);

            float cubicT2 = t2 * t2 * t2;
            float quadraticT4 = t4 * t4;

            float currentAngle = startAngle - clockwiseDelta * t2;
            float currentRadius = Mathf.Lerp(startOffset.magnitude, targetOffset.magnitude, t2);

            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 finalPos = playerPos + new Vector3(Mathf.Cos(rad) * currentRadius, 0f, Mathf.Sin(rad) * currentRadius);
            finalPos.y = Mathf.Lerp(constructStartPos.y, constructTargetPos.y, t2);

            Vector3 smoothed = Vector3.SmoothDamp(
                player.Companion.transform.position, 
                finalPos, 
                ref player.Companion.followVelocity, 
                0.15f
            );
            finalPos = Vector3.Lerp(smoothed, finalPos, quadraticT4);

            player.Companion.transform.position = finalPos;

            if (!constructStartedAnim)
            {
                Vector3 companionOffset = finalPos - player.transform.position;
                float orbitAngle = Mathf.Atan2(companionOffset.z, companionOffset.x);
                Vector3 forward = player.Companion.GetOrbitTangent(orbitAngle, -1f);

                if (forward.sqrMagnitude > 0.001f)
                {
                    Quaternion orbitRot = Quaternion.LookRotation(forward.normalized);
                    player.Companion.transform.rotation = Quaternion.Lerp(orbitRot, constructTargetRot, cubicT2);
                }
            }

            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (t >= 0.4f)
            {
                float tHalf = Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f);

                child.localPosition = Vector3.Lerp(childStart, childTarget, tHalf);
                transform.localPosition = Vector3.Lerp(parentStartPos, parentTargetPos, tHalf);

                if (!constructStartedAnim)
                {
                    player.Companion.transform.rotation = player.transform.rotation;
                    player.Companion._animator.SetBool("Died", true);
                    constructStartedAnim = true;
                }
            }

            if (t >= 0.92f)
                player.Companion._animator.SetLayerWeight(1, 1f - t3);

            yield return null;
        }

        float duration2 = 1.2f;
        float timer2 = 0f;

        while (timer2 < duration2)
        {
            timer2 += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer2 / duration2));
            Color newColor = Color.Lerp(player.Companion.OrigCrystalColor, player.Companion.DeadCrystalColor, t);

            player.Companion.CrystalBallMaterial.SetColor("_EmissionColor", newColor);

            yield return null;
        }

        transform.rotation = targetRot;
        child.localPosition = childTarget;
        player.Companion.CrystalBallMaterial.SetColor("_EmissionColor", player.Companion.DeadCrystalColor);
    }
    private IEnumerator ReturnCam(float duration = 2f, bool cancelDeathCam = false)
    {
        Transform child = transform.GetChild(0);

        Quaternion startRot = transform.rotation;
        Vector3 startParentPos = transform.localPosition;
        Vector3 startChildPos = child.localPosition;

        Color prevColor = player.Companion.CrystalBallMaterial.GetColor("_EmissionColor");

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            float t2 = Mathf.SmoothStep(0f, 1f, timer / 0.8f);

            transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);
            transform.localPosition = Vector3.Lerp(startParentPos, originalParentLocalPos, t);
            child.localPosition = Vector3.Lerp(startChildPos, originalChildLocalPos, t);

            Color newColor = Color.Lerp(prevColor, player.Companion.OrigCrystalColor, t2);

            player.Companion.CrystalBallMaterial.SetColor("_EmissionColor", newColor);

            yield return null;
        }

        transform.rotation = originalRotation;
        transform.localPosition = originalParentLocalPos;
        child.localPosition = originalChildLocalPos;
        player.Companion.CrystalBallMaterial.SetColor("_EmissionColor", player.Companion.OrigCrystalColor);

        if (cancelDeathCam)
        {
            playingDeathAnim = false;
            OnResurrectionRespawn?.Invoke();
        }
    }

    void CacheOriginalState()
    {
        originalRotation = transform.rotation;
        originalParentLocalPos = transform.localPosition;
        originalChildLocalPos = transform.GetChild(0).localPosition;
    }

    public void ZoomIn(float desiredDistanceZ, float duration)
    {
        zoomStart = _camera.transform.localPosition.z;
        if (!prevDistance.HasValue)
        {
            prevDistance = _camera.transform.localPosition.z;
        }
        targetZoomDistance = desiredDistanceZ;

        zoomingBack = false;
        zoomDuration = duration;
        zoomT = 0f;
    }

    public void ZoomBack(float duration)
    {
        zoomStart = _camera.transform.localPosition.z;
        if (prevDistance.HasValue)
            targetZoomDistance = prevDistance.Value;

        zoomingBack = true;
        zoomDuration = duration;
        zoomT = 0f;
    }
}
