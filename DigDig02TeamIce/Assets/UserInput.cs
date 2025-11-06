using System.Collections;
using System.Collections.Generic;
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

    public static bool InteractPressed;

    private InputAction _moveAction;
    private InputAction _parryAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _lockOnAction;
    private InputAction _spearAttackAction;
    private InputAction _interactAction;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();

        _moveAction = PlayerInput.actions["Move"];
        _parryAction = PlayerInput.actions["Parry"];
        _jumpAction = PlayerInput.actions["Jump"];
        _sprintAction = PlayerInput.actions["Sprint"];
        _lockOnAction = PlayerInput.actions["TargetLockOn"];
        _spearAttackAction = PlayerInput.actions["ConstructAttack_01"];
        _interactAction = PlayerInput.actions["Interact"];
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

        InteractPressed = _interactAction.WasPressedThisFrame();
    }
}
