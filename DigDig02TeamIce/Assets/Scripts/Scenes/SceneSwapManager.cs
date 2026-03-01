using FIMSpace.Basics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwapManager : MonoBehaviour
{
    public static SceneSwapManager instance;

    public static bool LoadFromDoor { get; private set; }

    private Player _player;
    private GameObject _camera;
    private Companion _construct;

    private Transform _doorSpawnPos;
    private Transform _constructDoorTargetSpinPos;
    private Transform _constructDoorTargetPos;
    private Vector3 _playerSpawnPosition;
    private float cameraRotY;
    private bool allowSpinEntrance;
    private bool forceSpinEntrance;

    private DoorTriggerInteraction.DoorToSpawnAt _doorToSpawnTo;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _player = GameObject.FindObjectOfType<Player>();
        _camera = GameObject.FindObjectOfType<CameraMovement>().gameObject;
        _construct = GameObject.FindObjectOfType<Companion>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void SwapSceneFromDoorUse(SceneField myScene, DoorTriggerInteraction.DoorToSpawnAt doorToSpawnAt)
    {
        LoadFromDoor = true;
        instance.StartCoroutine(instance.FadeOutThenChangeScene(myScene, doorToSpawnAt));
    }
    public static void UnloadDeathScene(string myScene, GameObject[] respawnObject, Vector3[] spawnPos)
    {
        LoadFromDoor = false;
        instance.StartCoroutine(instance.DeathResurrectSceneSwap(myScene, respawnObject, spawnPos));
    }

    public static void LoadDeathScene()
    {
        instance.StartCoroutine(instance.FreezeAndLoadDeathSceneRoutine(true));
    }

    private IEnumerator FadeOutThenChangeScene(SceneField myScene, DoorTriggerInteraction.DoorToSpawnAt doorToSpawnAt = DoorTriggerInteraction.DoorToSpawnAt.None)
    {
        SceneFadeManager.instance.StartFadeOut();

        while (SceneFadeManager.instance.IsFadingOut)
        {
            yield return null;
        }

        _doorToSpawnTo = doorToSpawnAt;
        SceneManager.LoadScene(myScene);
    }

    private IEnumerator DeathResurrectSceneSwap(string myScene, GameObject[] respawnObject, Vector3[] spawnPositions)
    {
        SceneFadeManager.instance.StartFadeOut();

        while (SceneFadeManager.instance.IsFadingOut)
        {
            yield return null;
        }

        for (int i = 0; i < respawnObject.Length; i++)
        {
            respawnObject[i].transform.position = spawnPositions[i];
        }
        //_player.Tail.enabled = true;
        //_player.Tail.gameObject.SetActive(true);
        //_player.Tail.User_ReposeTail();
        foreach (Light light in DeathSceneManager.DirLights)
        {
            light.gameObject.SetActive(true);
        }
        DeathSceneManager.DirLights = null;

        SceneFadeManager.instance.StartFadeIn();

        while (SceneFadeManager.instance.IsFadingIn)
        {
            yield return null;
        }

        _player.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        _player.animator.SetBool("Dead", false);

        _construct._animator.SetBool("Died", false);
        _construct._animator.SetLayerWeight(1, 1f);
        yield return null;

        yield return new WaitForSecondsRealtime(2f);

        _player.Resurrect();
        Freezer.ForceCancelAll();
        SceneManager.UnloadSceneAsync("DeathScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneFadeManager.instance.StartFadeIn();

        if (LoadFromDoor)
        {
            StartCoroutine(PlayerDoorPositioning());
        }
    }

    private void FindDoor(DoorTriggerInteraction.DoorToSpawnAt doorSpawnNumber)
    {
        DoorTriggerInteraction[] doors = FindObjectsOfType<DoorTriggerInteraction>();

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i].CurrentDoorPosition == doorSpawnNumber)
            {
                _doorSpawnPos = doors[i].SpawnPosition;
                _constructDoorTargetPos = doors[i].ConstructTargetPos;
                _constructDoorTargetSpinPos = doors[i].ConstructTargetSpinPos;
                cameraRotY = doors[i].CameraRotationY;
                allowSpinEntrance = doors[i].AllowSpinEntrance;
                forceSpinEntrance = doors[i].ForceSpinEntrance;

                CalculateSpawnPosition();
                return;
            }
        }
    }

    private IEnumerator FreezeAndLoadDeathSceneRoutine(bool waitOneFrame = false)
    {
        if (waitOneFrame)
        {
            yield return null;
        }

        // Start freeze immediately
        Freezer.Freeze(99999f);

        AsyncOperation loadOp =
            SceneManager.LoadSceneAsync("DeathScene", LoadSceneMode.Additive);

        // Optional but recommended:
        loadOp.allowSceneActivation = true;

        // Wait until loading finishes (independent of Time.timeScale)
        while (!loadOp.isDone)
        {
            yield return null;
        }

        // Scene is now fully loaded and activated
        //Freezer.Cancel();
    }

    private IEnumerator PlayerDoorPositioning()
    {
        FindDoor(_doorToSpawnTo);

        _player.Tail.enabled = false;
        _player.Tail.gameObject.SetActive(false);
        _player.transform.position = _playerSpawnPosition;
        _player.Tail.enabled = true;
        _player.Tail.gameObject.SetActive(true);
        _player.Tail.User_ReposeTail();

        _camera.transform.position = _playerSpawnPosition;
        Vector3 angles = _camera.transform.localEulerAngles;
        angles.y = cameraRotY;
        _camera.transform.localEulerAngles = angles;

        Vector3 pos = _playerSpawnPosition + (Vector3.up * 2f);

        bool doSpin = Random.value < 0.33f;
        if (!allowSpinEntrance && !forceSpinEntrance)
            doSpin = false;

        Vector3 dir;
        Vector3 targetPos;
        if (doSpin || forceSpinEntrance)
        {
            if (!_construct.isCarrying)
            {
                doSpin = true;
                dir = _constructDoorTargetSpinPos.position - _doorSpawnPos.position;
                targetPos = _constructDoorTargetSpinPos.position;
            }
            else
            {
                doSpin = false;
                dir = _constructDoorTargetPos.position - _doorSpawnPos.position;
                targetPos = _constructDoorTargetPos.position;
                pos += Vector3.up * 0.75f;
            }
        }
        else
        {
            doSpin = false;
            dir = _constructDoorTargetPos.position - _doorSpawnPos.position;
            targetPos = _constructDoorTargetPos.position;
            pos += Vector3.up * 0.75f;
        }

        dir.y = 0f;

        if (_construct.PickUpRoutine != null)
        {
            StopCoroutine(_construct.PickUpRoutine);
            _construct.PickUpRoutine = null;

            if (_construct.heldObject == null)
            {
                _construct.isCarrying = false;
                _construct.isPlayingGrabAnim = false;
                _construct.movementOverride = false;
                _construct.agent.isStopped = true;
                _construct.agent.ResetPath();
                _construct.agent.enabled = false;
                _construct._animator.SetBool("Grabbing", false);
            }
            else
            {
                _construct.isPlayingGrabAnim = false;
                _construct.movementOverride = false;
                _construct.agent.enabled = true;
                _construct.agent.isStopped = false;

                _construct.agentSpeedBeforeGrab = _construct.agent.speed;
                _construct.agentAccelerationBeforeGrab = _construct.agent.acceleration;

                _construct.agent.stoppingDistance = 3f;
                _construct.agent.speed = 12f;
                _construct.agent.acceleration = 12f;
                _construct.carryBobTime = 0f;
            }
        }

        if (_construct.PutDownRoutine != null)
        {
            StopCoroutine( _construct.PutDownRoutine);
            _construct.PutDownRoutine = null;

            if (_construct.heldObject == null)
            {
                _construct.isCarrying = false;
                _construct.isPlayingGrabAnim = false;
                _construct.movementOverride = false;
                _construct._animator.SetBool("Grabbing", false);

                _construct.carriedObjectExtentsY = null;
                _construct.agent.enabled = true;
                _construct.agent.isStopped = false;
                _construct.agent.stoppingDistance = 0f;
                _construct.agent.speed = _construct.agentSpeedBeforeGrab;
                _construct.agent.acceleration = _construct.agentAccelerationBeforeGrab;
                _construct.disableAgentOnReachTarget = true;
                _construct.ExitNavMove(false);
            }
            else
            {
                _construct.isCarrying = true;
                _construct.isPlayingGrabAnim = false;
                _construct.movementOverride = false;

                _construct.agent.isStopped = true;
                _construct.agent.ResetPath();
                _construct.agent.enabled = false;

                _construct._animator.SetBool("Grabbing", true);
            }
        }

        if (_construct.DoorEntranceAnimRoutine != null)
            StopCoroutine(_construct.DoorEntranceAnimRoutine);

        _construct.DoorEntranceAnimRoutine = StartCoroutine(_construct.DoorEntranceAnimation(pos, targetPos, dir.normalized, doSpin));

        yield return null;
        LoadFromDoor = false;
    }

    private void CalculateSpawnPosition()
    {
        float colliderHeight = _player.MainCollider.bounds.extents.y;
        Vector3 spawnPos;

        if (Physics.Raycast(_doorSpawnPos.position, Vector3.down, out RaycastHit info, 15f, LayerMask.GetMask("Default")))
        {
            //spawnPos = info.point + new Vector3(0f, colliderHeight, 0f);
            spawnPos = info.point;
        }
        else
        {
            spawnPos = _doorSpawnPos.position;
        }
        _playerSpawnPosition = spawnPos;
    }
}
