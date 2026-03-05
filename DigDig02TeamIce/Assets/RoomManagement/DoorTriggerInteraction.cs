using UnityEngine;

public class DoorTriggerInteraction : TriggerInteractionBase
{
    public enum DoorToSpawnAt
    {
        None,
        One,
        Two,
        Three,
        Four,
        Five,
        Six
    }

    [Header("Spawn TO")]
    [SerializeField] private DoorToSpawnAt DoorToSpawnTo;
    [SerializeField] private SceneField _sceneToLoad;

    [Space(10f)]
    [Header("THIS Door")]
    public DoorToSpawnAt CurrentDoorPosition;
    public Transform SpawnPosition;
    public Transform ConstructTargetPos;
    public Transform ConstructTargetSpinPos;
    public float AmountToWalkOut = 6.5f;
    public float CameraRotationY = 45f;
    public bool AllowSpinEntrance = true;
    public bool ForceSpinEntrance = false;

    private bool haveExited = false;
    private bool firstEnter = true;

    public override void OnEnter()
    {
        if (haveExited || firstEnter && !SceneSwapManager.LoadFromDoor)
        {
            SceneSwapManager.SwapSceneFromDoorUse(_sceneToLoad, DoorToSpawnTo, CurrentDoorPosition);
            haveExited = false;
            firstEnter = false;
        }
    }

    public override void OnExit()
    {
        if (!haveExited && !SceneSwapManager.LoadFromDoor)
        {
            haveExited = true;
        }
    }
}
