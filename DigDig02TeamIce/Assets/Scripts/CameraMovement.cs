using System.Collections;
using System.Linq;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement Instance;
    private Player player;
    private Transform target;

    [SerializeField] private GameObject cameraObject;
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
    }
    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();

        player.OnPlayerDie += OnPlayerDie;

        Vector3 euler = transform.localEulerAngles;
        startX = NormalizeAngle(euler.x);
        rotationX = startX;
        rotationY = NormalizeAngle(euler.y);

        cameraStartDistanceZ = cameraObject.transform.localPosition.z;
        cameraStartRotationX = cameraObject.transform.rotation.x;
        cameraStartPositionY = cameraObject.transform.localPosition.y;

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
        float delay = 0.5f;
        float totalTime = player.animator.runtimeAnimatorController.animationClips.Where(c => c.name == "Die").FirstOrDefault().length;

        float xAngle = 15f;
        float yOffset = 135f;

        float childMoveZ = 85f;
        float childMoveY = -1f;

        float parentMoveZ = 1.25f;

        yield return new WaitForSecondsRealtime(delay);

        // --- Start state
        Quaternion startRot = transform.rotation;

        // --- Target rotation (relative to player)
        Quaternion targetRot =
            Quaternion.AngleAxis(player.transform.eulerAngles.y + yOffset, Vector3.up) *
            Quaternion.AngleAxis(xAngle, Vector3.right);

        // --- Child movement
        Transform child = transform.GetChild(0);
        Vector3 childStart = child.localPosition;
        Vector3 childTarget = childStart + new Vector3(0f, childMoveY, childMoveZ);

        Vector3 parentStartPos = transform.localPosition;
        Vector3 parentTargetPos = parentStartPos + new Vector3(0f, 0f, parentMoveZ);

        float timer = 0f;

        while (timer < totalTime)
        {
            timer += Time.unscaledDeltaTime;

            AmountThroughDeathAnim = Mathf.Clamp01(timer / totalTime);

            float t = Mathf.SmoothStep(0f, 1f, AmountThroughDeathAnim);

            // --- Rotation
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // --- Child movement starts halfway
            if (t >= 0.4f)
            {
                float tHalf = Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f);
                child.localPosition = Vector3.Lerp(childStart, childTarget, tHalf);
                transform.localPosition = Vector3.Lerp(parentStartPos, parentTargetPos, tHalf);
            }

            yield return null;
        }

        // Snap to final state
        transform.rotation = targetRot;
        child.localPosition = childTarget;
    }
    private IEnumerator ReturnCam(float duration = 2f, bool cancelDeathCam = false)
    {
        Transform child = transform.GetChild(0);

        Quaternion startRot = transform.rotation;
        Vector3 startParentPos = transform.localPosition;
        Vector3 startChildPos = child.localPosition;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);

            transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);
            transform.localPosition = Vector3.Lerp(startParentPos, originalParentLocalPos, t);
            child.localPosition = Vector3.Lerp(startChildPos, originalChildLocalPos, t);

            yield return null;
        }

        transform.rotation = originalRotation;
        transform.localPosition = originalParentLocalPos;
        child.localPosition = originalChildLocalPos;

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
}
