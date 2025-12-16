using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwapManager : MonoBehaviour
{
    public static SceneSwapManager instance;

    private static bool _loadFromDoor;

    private Player _player;
    private GameObject _camera;
    private Vector3 _doorSpawnPos;
    private Vector3 _playerSpawnPosition;

    private DoorTriggerInteraction.DoorToSpawnAt _doorToSpawnTo;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _player = GameObject.FindObjectOfType<Player>();
        _camera = GameObject.FindObjectOfType<CameraMovement>().gameObject;
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
        _loadFromDoor = true;
        instance.StartCoroutine(instance.FadeOutThenChangeScene(myScene, doorToSpawnAt));
    }
    public static void UnloadDeathScene(string myScene, GameObject[] respawnObject, Vector3[] spawnPos)
    {
        _loadFromDoor = false;
        instance.StartCoroutine(instance.DeathResurrectSceneSwap(myScene, respawnObject, spawnPos));
    }

    public static void LoadDeathScene()
    {
        instance.StartCoroutine(instance.FreezeUntilLoaded(true));
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
        yield return null;

        yield return new WaitForSecondsRealtime(2f);

        _player.Resurrect();
        Freezer.ForceCancelAll();
        SceneManager.UnloadSceneAsync("DeathScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneFadeManager.instance.StartFadeIn();

        if (_loadFromDoor)
        {
            FindDoor(_doorToSpawnTo);
            _player.transform.position = _playerSpawnPosition;
            _camera.transform.position = _playerSpawnPosition;
            _loadFromDoor = false;
        }
    }

    private void FindDoor(DoorTriggerInteraction.DoorToSpawnAt doorSpawnNumber)
    {
        DoorTriggerInteraction[] doors = FindObjectsOfType<DoorTriggerInteraction>();

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i].CurrentDoorPosition == doorSpawnNumber)
            {
                _doorSpawnPos = doors[i].SpawnPosition.position;

                CalculateSpawnPosition();
                return;
            }
        }
    }

    private IEnumerator FreezeUntilLoaded(bool waitOneFrame = false)
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

    private void CalculateSpawnPosition()
    {
        float colliderHeight = _player.MainCollider.bounds.extents.y;
        Vector3 spawnPos;

        if (Physics.Raycast(_doorSpawnPos, Vector3.down, out RaycastHit info))
        {
            spawnPos = info.point + new Vector3(0f, colliderHeight, 0f);
        }
        else
        {
            spawnPos = _doorSpawnPos;
        }
        _playerSpawnPosition = spawnPos;
    }
}
