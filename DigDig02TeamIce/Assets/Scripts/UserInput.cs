using UnityEngine;
using UnityEngine.InputSystem;

public class UserInput : MonoBehaviour
{
    public static PlayerInput PlayerInput;

    public static Vector2 MoveInput;

    public static bool ParryPressed;

    public static bool JumpPressed;
    public static bool JumpHeld;
    public static bool JumpReleased;

    public static bool SprintPressed;
    public static bool SprintHeld;
    public static bool SprintReleased;

    public static bool LockOnPressed;
    public static bool LockOnHeld;
    public static bool LockOnReleased;

    public static bool SpearAttackPressed;
    public static bool SlamAttackPressed;

    public static bool MeleeAttackPressed;

    public static bool InteractPressed;

    public static bool PausePressed;

    public static bool EscapePressed;

    public static bool RunePuzzleLeftPressed;
    public static bool RunePuzzleRightPressed;
    public static bool RunePuzzleNextDiskPressed;
    public static bool RunePuzzlePreviousDiskPressed;

    private InputAction _moveAction;
    private InputAction _parryAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _lockOnAction;
    private InputAction _spearAttackAction;
    private InputAction _slamAttackAction;
    private InputAction _meleeAttackAction;
    private InputAction _interactAction;
    private InputAction _pauseAction;
    private InputAction _escapeAction;
    private InputAction _runePuzzleLeftAction;
    private InputAction _runePuzzleRightAction;
    private InputAction _runePuzzleNextDiskAction;
    private InputAction _runePuzzlePreviousDiskAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();

        _moveAction = PlayerInput.actions["Move"];
        _parryAction = PlayerInput.actions["Parry"];
        _jumpAction = PlayerInput.actions["Jump"];
        _sprintAction = PlayerInput.actions["Sprint"];
        _lockOnAction = PlayerInput.actions["TargetLockOn"];
        _spearAttackAction = PlayerInput.actions["ConstructAttack_01"];
        _slamAttackAction = PlayerInput.actions["ConstructAttack_02"];
        _meleeAttackAction = PlayerInput.actions["MeleeAttack"];
        _interactAction = PlayerInput.actions["Interact"];
        _pauseAction = PlayerInput.actions["Pause"];
        _escapeAction = PlayerInput.actions["Escape"];
        _runePuzzleLeftAction = PlayerInput.actions["RunePuzzleLeft"];
        _runePuzzleRightAction = PlayerInput.actions["RunePuzzleRight"];
        _runePuzzleNextDiskAction = PlayerInput.actions["RunePuzzleNextDisk"];
        _runePuzzlePreviousDiskAction = PlayerInput.actions["RunePuzzlePreviousDisk"];
    }

    private void Update()
    {
        MoveInput = _moveAction.ReadValue<Vector2>();

        ParryPressed = _parryAction.WasPressedThisFrame();

        JumpPressed = _jumpAction.WasPressedThisFrame();
        JumpHeld = _jumpAction.IsPressed();
        JumpReleased = _jumpAction.WasReleasedThisFrame();

        SprintPressed = _sprintAction.WasPressedThisFrame();
        SprintHeld = _sprintAction.IsPressed();
        SprintReleased = _sprintAction.WasReleasedThisFrame();

        LockOnPressed = _lockOnAction.WasPressedThisFrame();
        LockOnHeld = _lockOnAction.IsPressed();
        LockOnReleased = _lockOnAction.WasReleasedThisFrame();

        SpearAttackPressed = _spearAttackAction.WasPressedThisFrame();
        SlamAttackPressed = _slamAttackAction.WasPressedThisFrame();

        MeleeAttackPressed = _meleeAttackAction.WasPressedThisFrame();

        InteractPressed = _interactAction.WasPressedThisFrame();

        PausePressed = _pauseAction.WasPressedThisFrame();

        EscapePressed = _escapeAction.WasPressedThisFrame();

        RunePuzzleLeftPressed = _runePuzzleLeftAction.WasPressedThisFrame();
        RunePuzzleRightPressed = _runePuzzleRightAction.WasPressedThisFrame();
        RunePuzzleNextDiskPressed = _runePuzzleNextDiskAction.WasPressedThisFrame();
        RunePuzzlePreviousDiskPressed = _runePuzzlePreviousDiskAction.WasPressedThisFrame();
    }
}
