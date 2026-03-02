using System.Linq;
using UnityEngine;

public class DeathSceneManager : MonoBehaviour
{
    private static GameObject playerAndSuch;
    public Player player;
    public GameObject[] movedObjects = new GameObject[3];
    public Vector3[] prevPositions = new Vector3[3];

    private CameraMovement _camera;
    private Companion _companion;

    public static Light[] DirLights;

    [SerializeField] private Canvas canvas;

    [SerializeField] private CanvasGroup fadeGroup;
    [Range(0f, 10f), SerializeField] private float fadeOutSpeed = 5f;
    [Range(0f, 10f), SerializeField] private float fadeInSpeed = 5f;

    [SerializeField, Range(0f, 1f)]
    private float interactableAlphaThreshold = 0.5f;

    public bool IsFadingOut { get; private set; } = false;
    public bool IsFadingIn { get; private set; } = false;

    private bool fadeTriggered;

    private void Awake()
    {
        playerAndSuch = GameObject.Find("PERSISTOBJECTS");
        if (playerAndSuch == null)
        {
            playerAndSuch = GameObject.Find("PERSISTOBJECTS(Clone)");
        }

        if (playerAndSuch != null)
        {
            player = GameObject.FindObjectOfType<Player>();
            _camera = GameObject.FindObjectOfType<CameraMovement>();
            _companion = GameObject.FindObjectOfType<Companion>();

            float airHeight = 0f;

            if (Physics.Raycast(
                    player.transform.position,
                    Vector3.down,
                    out RaycastHit hit,
                    40f,
                    player.groundLayers))
            {
                airHeight = player.transform.position.y - hit.point.y;
            }

            movedObjects = new GameObject[3];
            prevPositions = new Vector3[3];

            movedObjects[0] = playerAndSuch.gameObject;
            prevPositions[0] = playerAndSuch.transform.position;
            movedObjects[1] = player.gameObject;
            prevPositions[1] = player.transform.position + new Vector3(0f, -airHeight, 0f);
            movedObjects[2] = _companion.gameObject;
            prevPositions[2] = _companion.transform.position + new Vector3(0f, -airHeight, 0f);

            Vector3 offset = gameObject.transform.position - player.transform.position;
            playerAndSuch.transform.position += offset;
            player.Tail.User_ReposeTail();

            //player.transform.position = gameObject.transform.position;
            //_camera.transform.position = gameObject.transform.position;
            //_companion.transform.position = gameObject.transform.position;

            if (_camera == null)
            {
                Debug.LogError($"Camera is null at {name}!");
            }
            _camera.OnResurrectionRespawn += OnResurrectionRespawn;

            DirLights = new Light[100];
            DirLights = GameObject.FindObjectsOfType<Light>(false).Where(l => l.type == UnityEngine.LightType.Directional).ToArray();
            foreach (Light light in DirLights)
            {
                light.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"Could not find PERSISTOBJECTS at {name}!");
            return;
        }

        fadeGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        _camera.OnResurrectionRespawn -= OnResurrectionRespawn;
        fadeTriggered = false;
        fadeGroup.alpha = 0f;
        IsFadingIn = false;
        IsFadingOut = false;
    }

    private void OnResurrectionRespawn()
    {
        StartFadeIn();
        SceneSwapManager.UnloadDeathScene(player.sceneAtDeath, movedObjects, prevPositions);
    }

    private void Update()
    {
        if (!fadeTriggered && _camera.AmountThroughDeathAnim > 0.8f)
        {
            fadeTriggered = true;
            StartFadeOut();
        }

        if (IsFadingOut)
        {
            fadeGroup.alpha += Time.unscaledDeltaTime * fadeOutSpeed;

            if (fadeGroup.alpha >= 1f)
            {
                fadeGroup.alpha = 1f;
                IsFadingOut = false;
            }

            UpdateInteractivity();
        }

        if (IsFadingIn)
        {
            fadeGroup.alpha -= Time.unscaledDeltaTime * fadeInSpeed;

            if (fadeGroup.alpha <= 0f)
            {
                fadeGroup.alpha = 0f;
                IsFadingIn = false;
            }

            UpdateInteractivity();
        }
    }

    private void UpdateInteractivity()
    {
        bool canInteract = fadeGroup.alpha >= interactableAlphaThreshold;

        fadeGroup.interactable = canInteract;
        fadeGroup.blocksRaycasts = canInteract;
    }

    public void StartFadeOut()
    {
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;

        IsFadingOut = true;
        IsFadingIn = false;
    }

    public void StartFadeIn()
    {
        if (fadeGroup.alpha >= 1f)
        {
            IsFadingIn = true;
            IsFadingOut = false;
        }
    }

    public void Respawn()
    {
        _camera.PlayerResurrect();
        //player.animator.SetBool("Dead", false);
    }
}
