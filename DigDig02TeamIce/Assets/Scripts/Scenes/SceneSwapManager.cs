using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.EventTrigger;

public class SceneSwapManager : MonoBehaviour
{
    public static SceneSwapManager instance;

    public static bool LoadFromDoor { get; private set; }
    public static bool LoadFromDeathScene { get; set; } = false;

    public event System.Action OnStartSceneSwap;

    private Player _player;
    private CameraMovement cameraObject;
    private Companion _construct;

    private Transform _doorSpawnPos;
    private Transform _constructDoorTargetSpinPos;
    private Transform _constructDoorTargetPos;
    private Vector3 _playerSpawnPosition;
    private float amountToWalk;
    private float cameraRotY;
    private float cameraDistanceZ;
    private bool allowSpinEntrance;
    private bool forceSpinEntrance;

    private bool playerSprinted = false;

    private DoorTriggerInteraction.DoorToSpawnAt _doorToSpawnTo;

    public static bool IntroSequencing = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _player = GameObject.FindObjectOfType<Player>();
        cameraObject = GameObject.FindObjectOfType<CameraMovement>();
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

    public static void SwapSceneFromDoorUse(SceneField myScene, DoorTriggerInteraction.DoorToSpawnAt doorToSpawnAt, DoorTriggerInteraction.DoorToSpawnAt fromDoor = DoorTriggerInteraction.DoorToSpawnAt.None)
    {
        LoadFromDoor = true;
        instance.StartCoroutine(instance.FadeOutThenChangeScene(myScene, doorToSpawnAt, fromDoor));
    }
    public static void EnterSceneThroughSaveData()
    {
        LoadFromDoor = true;
        instance.StartCoroutine(instance.LoadSceneThroughSaveDataRoutine());
    }
    public static void UnloadDeathScene(string myScene, GameObject[] respawnObject, Vector3[] spawnPos)
    {
        LoadFromDoor = false;
        instance.StartCoroutine(instance.DeathResurrectSceneSwap(myScene, respawnObject, spawnPos));
    }
    public static void UnloadDeathSceneDoorVersion(string myScene, DoorTriggerInteraction.DoorToSpawnAt doorToSpawnAt)
    {
        LoadFromDoor = true;
        LoadFromDeathScene = true;
        instance.StartCoroutine(instance.FadeOutThenChangeScene(null, doorToSpawnAt, DoorTriggerInteraction.DoorToSpawnAt.None, myScene));
    }
    public static void LoadDeathScene()
    {
        instance.StartCoroutine(instance.FreezeAndLoadDeathSceneRoutine(true));
    }

    private IEnumerator FadeOutThenChangeScene(SceneField myScene, DoorTriggerInteraction.DoorToSpawnAt doorToSpawnAt = DoorTriggerInteraction.DoorToSpawnAt.None, DoorTriggerInteraction.DoorToSpawnAt fromDoor = DoorTriggerInteraction.DoorToSpawnAt.None, string sceneToLoad = null)
    {
        FindDoor(fromDoor);
        Vector3 dir = Vector3.forward;

        if (_doorSpawnPos != null && _constructDoorTargetPos != null)
        {
            dir = _doorSpawnPos.position - _constructDoorTargetPos.position;
            dir.Normalize();
            dir.y = 0f;
        }

        _player.MovementOverride = true;
        _player.animator.SetLayerWeight(2, 0f);
        _player.Parrying = false;
        _player.Attacking = false;
        _player.AllowFollowUpAttack = false;
        _player.AttackBuffered = false;
        _player.animator.SetBool("Blocked", false);
        _player.animator.SetBool("Attack", false);
        _player.animator.SetBool("FollowUp", false);
        _player.animator.SetBool("SlamAttacking", false);
        _player.animator.SetBool("Pushing", false);
        _player.animator.ResetTrigger("GotHit");
        _player.wrenchAttack.Deactivate();

        float speed;
        if (!LoadFromDeathScene)
        {
            if (_player.Sprinting)
            {
                _player.animator.SetBool("Sprinting", true);
                if (!_player.animator.GetCurrentAnimatorStateInfo(0).IsName("RunTree"))
                {
                    _player.animator.Play("RunTree");
                }
                speed = _player.sprintSpeed;
                playerSprinted = true;
            }
            else
            {
                _player.animator.SetBool("Sprinting", false);
                if (!_player.animator.GetCurrentAnimatorStateInfo(0).IsName("WalkTree"))
                {
                    _player.animator.Play("WalkTree");
                }
                speed = _player.walkSpeed;
                playerSprinted = false;
            }
        }
        else
        {
            speed = _player.walkSpeed;
            playerSprinted = false;
        }

        Vector3 startPos = _player.transform.position;
        Quaternion startRot = _player.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 downForce= new Vector3(0f, playerSprinted ? -4f : -2f, 0f);

        SceneFadeManager.instance.StartFadeOut();
        while (SceneFadeManager.instance.IsFadingOut)
        {
            if (!LoadFromDeathScene)
            {
                float delta = Vector3.Distance(_player.transform.position, startPos);
                if (delta < 6f)
                {
                    float t = delta / 4f;
                    Vector3 finalMove = (dir * speed) + downForce;
                    Vector3 localMove = transform.InverseTransformDirection(finalMove);
                    Vector2 animDir = new Vector2(localMove.x, localMove.z).normalized;

                    _player.animator.SetFloat("MoveX", 0f);
                    _player.animator.SetFloat("MoveZ", 1f);
                    _player.animator.SetFloat("Move", _player.controller.velocity.magnitude);
                    _player.controller.Move(finalMove * Time.deltaTime);
                    _player.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
                }
                else
                {
                    _player.animator.speed = 0f;
                }
            }

            yield return null;
        }
        _player.animator.SetFloat("MoveX", 0f);
        _player.animator.SetFloat("MoveZ", 0f);
        _player.animator.SetFloat("Move", 0f);
        _player.animator.speed = 0f;

        if (LoadFromDeathScene)
        {
            _construct._animator.SetBool("Died", false);
            _construct._animator.SetLayerWeight(1, 1f);
            _player.animator.updateMode = AnimatorUpdateMode.Normal;
            _player.animator.Play("WalkTree");
            _player.animator.Update(0f);
            _player.animator.SetBool("Dead", false);
            _player.animator.Update(0f);

            if (_player.Dead)
            {
                foreach (Light light in DeathSceneManager.DirLights)
                {
                    light.gameObject.SetActive(true);
                }
                DeathSceneManager.DirLights = null;
            }
        }

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
            StopCoroutine(_construct.PutDownRoutine);
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
        yield return null;

        _construct.previousSpears.Clear();

        _doorToSpawnTo = doorToSpawnAt;
        _player.lastExitedDoor = doorToSpawnAt;

        if (LoadFromDeathScene)
        {
            int deadEnemyCount = 0;
            foreach (var enemy in _player.EnemiesInRoom)
            {
                if (enemy.Dead)
                {
                    deadEnemyCount++;
                }
            }

            if (deadEnemyCount < _player.EnemiesInRoom.Count)
            {
                Debug.Log($"{deadEnemyCount}, {_player.EnemiesInRoom.Count}");

                foreach (var enemy in _player.EnemiesInRoom)
                {
                    enemy.DeleteSaveData();
                }

                OnStartSceneSwap?.Invoke();
            }
        }

        foreach (var proj in _player.ProjectilesInRoom)
        {
            Destroy(proj.BlobShadow);
            Destroy(proj);
        }

        _player.EnemiesInCombat.Clear();
        _player.EnemiesInRoom.Clear();

        if (!LoadFromDeathScene || !_player.Dead)
        {
            SceneManager.LoadScene(myScene);
        }
        else
        {
            _player.Resurrect();
            cameraObject.RestoreToOriginal();
            Freezer.ForceCancelAll();

            var op = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(_player.sceneAtDeath));
            while (!op.isDone)
            {
                yield return null;
            }

            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private IEnumerator DeathResurrectSceneSwap(string myScene, GameObject[] respawnObject, Vector3[] spawnPositions)
    {
        _player.animator.SetFloat("MoveX", 0f);
        _player.animator.SetFloat("MoveZ", 0f);
        _player.animator.SetFloat("Move", 0f);

        _construct._animator.SetBool("Died", false);
        _construct._animator.SetLayerWeight(1, 1f);


        Vector3 constructStartPos = _construct.transform.position;
        Vector3 constructTargetPos = spawnPositions[2] + respawnObject[0].transform.position;
        bool startedPlayerAnim = false;
        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / 1.5f);

            Vector3 pos = Vector3.Slerp(constructStartPos, constructTargetPos, t);
            _construct.transform.position = pos;

            if (t >= 0.33f && !startedPlayerAnim)
            {
                _player.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _player.animator.SetBool("Dead", false);
                duration += 0.5f;
                startedPlayerAnim = true;
            }

            yield return null;
        }

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

        SceneFadeManager.instance.StartFadeIn(false);

        while (SceneFadeManager.instance.IsFadingIn)
        {
            yield return null;
        }

        _player.Resurrect();
        Freezer.ForceCancelAll();
        SceneManager.UnloadSceneAsync("DeathScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (cameraObject.audioListener == null)
        {
            cameraObject.audioListener = cameraObject.GetComponent<AudioListener>();
        }
        if (scene.name == "MainMenu" || scene.name == "Credits")
        {
            cameraObject.audioListener.enabled = false;
        }
        else
        {
            cameraObject.audioListener.enabled = true;

            // Editor logic only! Creates data if you don't start from main menu
            if (SaveSystem.Data == null)
            {
                SaveSystem.Load();

                SaveSystem.Data.currentRoom = SceneManager.GetActiveScene().name;
                SaveSystem.Data.lastExitedDoor = 0;
                SaveSystem.Data.energy = _player.Energy;
                SaveSystem.Data.cameraRotationY = cameraRotY;
                SaveSystem.Data.cameraChildLocalPosition = cameraObject.transform.localPosition;
                SaveSystem.Data.constructCarriedItem = _construct.carriedObjectID;
            }
        }

        if (scene.name != "DeathScene")
        {
            if (!LoadFromDoor)
            {
                GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
                if (spawnPoint != null)
                {
                    _player.transform.position = spawnPoint.transform.position;
                    cameraObject.transform.position = spawnPoint.transform.position;
                    _construct.transform.position = _player.transform.position + _construct.Offset;
                }
            }

            SceneFadeManager.instance._fadeOutStartColor.a = 1f;
            SceneFadeManager.instance._fadeOutImage.color = SceneFadeManager.instance._fadeOutStartColor;
            SceneFadeManager.instance.StartFadeIn(true, 0.25f);
        }

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
                amountToWalk = doors[i].AmountToWalkOut;
                cameraRotY = doors[i].CameraRotationY;
                cameraDistanceZ = doors[i].CameraDistanceZ;
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
        bool noDoor = false;

        if (_doorToSpawnTo != 0)
        {
            FindDoor(_doorToSpawnTo);
        }
        else
        {
            noDoor = true;

            GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
            if (spawnPoint != null)
            {
                _playerSpawnPosition = spawnPoint.transform.position;
                _constructDoorTargetPos = spawnPoint.transform;
                _constructDoorTargetSpinPos = spawnPoint.transform;
                amountToWalk = 0f;
                cameraRotY = spawnPoint.transform.eulerAngles.y;
                cameraDistanceZ = -85f;
                allowSpinEntrance = false;
                forceSpinEntrance = false;
            }
        }

        _player.Tail.enabled = false;
        _player.Tail.gameObject.SetActive(false);
        _player.transform.position = _playerSpawnPosition;
        _player.Tail.enabled = true;
        _player.Tail.gameObject.SetActive(true);
        _player.Tail.User_ReposeTail();

        cameraObject.transform.position = _playerSpawnPosition;
        Vector3 angles = cameraObject.transform.localEulerAngles;
        angles.y = cameraRotY;
        cameraObject.transform.localEulerAngles = angles;
        Vector3 camPos = cameraObject._camera.transform.localPosition;
        cameraObject._camera.transform.localPosition = new Vector3(camPos.x, camPos.y, cameraDistanceZ);

        Vector3 pos = _playerSpawnPosition + (Vector3.up * 2f);

        string current = SceneManager.GetActiveScene().name;

        if (current != "MainMenu" && current != "DeathScene")
        {
            SaveSystem.Data.currentRoom = current;
            SaveSystem.Data.lastExitedDoor = (int)_player.lastExitedDoor;
            SaveSystem.Data.cameraRotationY = cameraRotY;
            SaveSystem.Data.cameraChildLocalPosition = cameraObject.transform.localPosition;
            SaveSystem.Data.constructCarriedItem = _construct.carriedObjectID;
            SaveSystem.Data.energy = _player.Energy;

            MenuManager.instance.CanPause = true;
        }

        SaveSystem.Save();

        Debug.Log(SaveSystem.Data.currentRoom);

        if (!noDoor)
        {
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

            if (_construct.DoorEntranceAnimRoutine != null)
                StopCoroutine(_construct.DoorEntranceAnimRoutine);

            _construct.DoorEntranceAnimRoutine = StartCoroutine(_construct.DoorEntranceAnimation(pos, targetPos, dir.normalized, doSpin));

            Vector3 playerDir = _doorSpawnPos.position - _constructDoorTargetPos.position;
            dir.Normalize();
            dir.y = 0f;

            Vector3 playerStartPos = _player.transform.position;
            _player.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            float speed = playerSprinted ? _player.sprintSpeed : _player.walkSpeed;
            Vector3 downForce = new Vector3(0f, playerSprinted ? -4f : -2f, 0f);

            yield return new WaitForSeconds(0.25f);

            _player.animator.speed = 1f;

            if (Physics.SphereCast(_player.Center.position, 0.5f, dir, out RaycastHit hit, amountToWalk + 2f, LayerMask.GetMask("Default", "Pushable", "LightReflector", "NoAO"), QueryTriggerInteraction.Ignore))
            {
                float distance = Vector3.Distance(hit.point, playerStartPos);
                if (distance < amountToWalk)
                {
                    amountToWalk = distance - 1f;
                }
            }

            while (true)
            {
                float delta = Vector3.Distance(_player.transform.position, playerStartPos);
                if (delta >= amountToWalk)
                    break;

                Vector3 finalMove = dir * speed + downForce;
                _player.controller.Move(finalMove * Time.deltaTime);

                Vector3 localMove = transform.InverseTransformDirection(finalMove);
                Vector2 animDir = new Vector2(localMove.x, localMove.z).normalized;

                _player.animator.SetFloat("MoveX", 0f);
                _player.animator.SetFloat("MoveZ", 1f);

                _player.animator.SetFloat("Move", _player.controller.velocity.magnitude);

                yield return null;
            }

            _player.animator.SetFloat("Move", _player.controller.velocity.magnitude);
        }

        _player.animator.speed = 1f;
        _player.animator.SetLayerWeight(2, 1f);
        _player.MovementOverride = false;

        yield return null;
        LoadFromDoor = false;
        LoadFromDeathScene = false;
    }

    private void CalculateSpawnPosition()
    {
        float colliderHeight = _player.MainCollider.bounds.extents.y;
        Vector3 spawnPos;

        if (Physics.Raycast(_doorSpawnPos.position, Vector3.down, out RaycastHit info, 15f, LayerMask.GetMask("Default", "Pushable", "NoAO")))
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

    private IEnumerator LoadSceneThroughSaveDataRoutine()
    {
        var data = SaveSystem.Data;

        _player.MovementOverride = true;
        _player.animator.SetLayerWeight(2, 0f);
        _player.Parrying = false;
        _player.Attacking = false;
        _player.AllowFollowUpAttack = false;
        _player.AttackBuffered = false;
        _player.animator.SetBool("Blocked", false);
        _player.animator.SetBool("Attack", false);
        _player.animator.SetBool("FollowUp", false);
        _player.animator.SetBool("SlamAttacking", false);
        _player.animator.SetBool("Pushing", false);
        _player.animator.ResetTrigger("GotHit");
        _player.wrenchAttack.Deactivate();

        SceneFadeManager.instance.StartFadeOut();
        while (SceneFadeManager.instance.IsFadingOut)
        {
            yield return null;
        }

        _player.Energy = SaveSystem.Data.energy;

        if (string.IsNullOrEmpty(data.constructCarriedItem))
        {

        }
        else
        {
            _construct.EnterNavMove(_player.transform.position);
            _construct.isCarrying = true;

            if (_construct.playingIdleAnim)
            {
                _construct._animator.SetBool("Looking", true);
                _construct.ResetAnimValues();
                yield return null;
                _construct._animator.SetBool("Looking", false);
                _construct.playingIdleAnim = false;
            }

            _construct.carriedObjectExtentsY = 0.4798695f;

            _construct._animator.SetBool("Grabbing", true);

            _construct.GhostOrbKey.gameObject.SetActive(true);
            _construct.GhostOrbKey.ID = data.constructCarriedItem;
            _construct.heldObject = _construct.GhostOrbKey.gameObject;
            _construct.heldObjectPrevLayer = _construct.heldObject.layer;
            _construct.heldObject.layer = LayerMask.NameToLayer("Player");

            _player.CurrentOrbKey = _construct.GhostOrbKey;
            _player.CurrentOrbKey.FadeTo(_player.CurrentOrbKey.InputBubble, _player.CurrentOrbKey.InteractBubble, 0.25f, true);
            _player.CurrentOrbKey.IsCarried = true;

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

        MenuManager.instance.RunesAquired = data.runesFound;
        MenuManager.instance.TurnOnRuneUI();

        yield return null;

        _construct.previousSpears.Clear();

        _doorToSpawnTo = (DoorTriggerInteraction.DoorToSpawnAt)data.lastExitedDoor;
        _player.lastExitedDoor = (DoorTriggerInteraction.DoorToSpawnAt)data.lastExitedDoor;

        MusicManager.instance.Play(FX.Music_NoCombat, true);
        MusicManager.instance.AudioSourceA.time = 0f;
        MusicManager.instance.FadeOutPrimary(2f, 1f);
        SceneManager.LoadScene(data.currentRoom);
    }

    public static void CallSceneSwapEvent()
    {
        instance.OnStartSceneSwap?.Invoke();
    }
}
